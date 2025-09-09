using CK.PerfectEvent;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Micro Agent that handles background works and events for cultures.
/// This tracks and collects globalization issues:
/// <list type="bullet">
///     <item>
///     Calls to <see cref="NormalizedCultureInfo.SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/> can raise <see cref="TranslationDuplicateResource"/>
///     and <see cref="TranslationFormatError"/> these issues are only emitted by <see cref="OnNewIssue"/> and logged. They are not collected.
///     </item>
///     <item>
///     <see cref="MissingTranslationResource"/> is emitted whenever a <see cref="MCString.Quality.Bad"/> or <see cref="MCString.Quality.Awful"/> translation
///     is detected.
///     </item>
///     <item>
///     <see cref="FormatArgumentCountError"/> is emitted whenever a translation format expects less or more arguments than a CodeString placeholders contains.
///     </item>
///     <item>
///     The worst case: <see cref="CultureIdentifierClash"/> is always raised, even if the static gate <see cref="Track"/> is closed. This is a serious issue
///     that must be urgently adressed.
///     </item>
/// </list>
/// </summary>
public static partial class GlobalizationAgent
{
    /// <summary>
    /// The "CK.Core.GlobalizationIssues.Track" static gate is closed by default.
    /// </summary>
    public static readonly StaticGate Track;

    /// <summary>
    /// Raised whenever a new <see cref="ExtendedCultureInfo"/> is registered. This event is raised sequentially: this
    /// allows to track culture registrations without concurrency issues. See <see cref="ExtendedCultureInfoTracker"/>.
    /// <para>
    /// When a new Culture appears, more than one culture can be created under the hood. The <see cref="Fallbacks"/>
    /// or the <see cref="PrimaryCulture"/> may have been created but only the registered one surfaces here.
    /// </para>
    /// </summary>
    public static PerfectEvent<ExtendedCultureInfoCreatedEvent> CultureCreated => _onNewCulture.PerfectEvent;

    /// <summary>
    /// Raised whenever a new issue occurs.
    /// </summary>
    public static PerfectEvent<Issue> OnNewIssue => _onNewIssue.PerfectEvent;

    /// <summary>
    /// Gets the list of <see cref="CultureIdentifierClash"/> that occurred.
    /// These are always collected, regardless of whether <see cref="Track"/> is opened or not.
    /// </summary>
    public static IReadOnlyList<CultureIdentifierClash> IdentifierClashes => _identifierClashes;

    /// <summary>
    /// Waits for any pending internal work. 
    /// </summary>
    /// <returns>The awaitable.</returns>
    public static Task WaitForPendingWorkAsync()
    {
        var tcs = new TaskCompletionSource();
        _channel.Writer.TryWrite( tcs );
        return tcs.Task;
    }

    /// <summary>
    /// Gets the source locations where a <see cref="CodeString"/> format has been created.
    /// This is tracked only when <see cref="Track"/> is open.
    /// </summary>
    /// <param name="s">The code string.</param>
    /// <returns>0 or more locations where the string has been emitted.</returns>
    public static IReadOnlyList<CodeStringSourceLocation> GetSourceLocation( CodeString s )
    {
        byte[] sha = new byte[20];
        s.FormattedString.WriteFormatSHA1( sha );
        return _codeSringOccurrence.GetValueOrDefault( sha, Array.Empty<CodeStringSourceLocation>() );
    }

    sealed class SHAComparer : IEqualityComparer<byte[]>
    {
        public bool Equals( byte[]? x, byte[]? y ) => x.AsSpan().SequenceEqual( y );

        public int GetHashCode( byte[] obj ) => obj.GetDjb2HashCode();
    }

    static readonly Channel<object?> _channel;
    static readonly IActivityMonitor _monitor;
    static readonly PerfectEventSender<Issue> _onNewIssue;
    static readonly PerfectEventSender<ExtendedCultureInfoCreatedEvent> _onNewCulture;
    internal readonly record struct ResKey( NormalizedCultureInfo Culture, string ResName );
    static Dictionary<ResKey, CodeString>? _missingTranslations;
    static Dictionary<ResKey, FormatArgumentCountError>? _formatArgumentError;
    static CultureIdentifierClash[] _identifierClashes;
    static readonly ConcurrentDictionary<byte[], CodeStringSourceLocation[]> _codeSringOccurrence;

    // Internal for tests (ClearCache).
    internal static void ClearIssueCache()
    {
        _identifierClashes = Array.Empty<CultureIdentifierClash>();
        _codeSringOccurrence.Clear();
        _missingTranslations = null;
        _formatArgumentError = null;
    }

    static GlobalizationAgent()
    {
        Track = new StaticGate( "CK.Core.GlobalizationIssues.Track", false );
        _channel = Channel.CreateUnbounded<object?>( new UnboundedChannelOptions { SingleReader = true } );
        _monitor = new ActivityMonitor( "CK.Globalization Micro Agent" );
        _monitor.AutoTags = ActivityMonitor.Tags.Register( "Globalization" );
        _onNewIssue = new PerfectEventSender<Issue>();
        _onNewCulture = new PerfectEventSender<ExtendedCultureInfoCreatedEvent>();
        _identifierClashes = Array.Empty<CultureIdentifierClash>();
        _codeSringOccurrence = new ConcurrentDictionary<byte[], CodeStringSourceLocation[]>( new SHAComparer() );
        _ = Task.Run( RunAsync );
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

        static void OnProcessExit( object? sender, EventArgs e ) => _channel.Writer.TryWrite( null );

        static async Task RunAsync()
        {
            object? o;
            while( (o = await _channel.Reader.ReadAsync().ConfigureAwait( false )) != null )
            {
                try
                {
                    if( o is Issue issue )
                    {
                        switch( issue )
                        {
                            case PrivateCodeStringCreated s:
                                HandleStringCreated( s.String, s.FilePath, s.LineNumber );
                                // This one emits no issue.
                                continue;
                            case PrivateMissingTranslationResource m:
                                // May emit one MissingTranslationResource issue and may be one FormatArgumentCountError issue.
                                await HandleMissingTranslationResourceAsync( m.Format, m.MCString );
                                continue;
                            case PrivateFormatArgumentCountError d:
                                // May emit a FormatArgumentCountError issue.
                                await HandleFormatArgumentCountErrorAsync( d.Format, d.MCString );
                                continue;
                            case CultureIdentifierClash c:
                                Util.InterlockedAdd( ref _identifierClashes, c );
                                _monitor.UnfilteredLog( LogLevel.Warn | LogLevel.IsFiltered,
                                                        ActivityMonitor.Tags.ToBeInvestigated,
                                                        c.ToString(),
                                                        null );
                                break;
                            default:
                                Throw.DebugAssert( issue is TranslationFormatError || issue is TranslationDuplicateResource );
                                _monitor.Warn( issue.ToString() );
                                break;
                        }
                        Throw.DebugAssert( issue is TranslationFormatError || issue is TranslationDuplicateResource || issue is CultureIdentifierClash );
                        await _onNewIssue.SafeRaiseAsync( _monitor, issue ).ConfigureAwait( false );
                    }
                    else
                    {
                        switch( o )
                        {
                            case ExtendedCultureInfoCreatedEvent newOne:
                                await _onNewCulture.SafeRaiseAsync( _monitor, newOne ).ConfigureAwait( false );
                                break;
                            case StartTrackerRequest tracker:
                                try
                                {
                                    await tracker.Tracker.DoStartAsync( _monitor, ExtendedCultureInfo.All, tracker.CancellationToken ).ConfigureAwait( false );
                                    tracker.TCS.SetResult();
                                }
                                catch( OperationCanceledException ) when (tracker.CancellationToken.IsCancellationRequested)
                                {
                                    tracker.TCS.SetCanceled();
                                }
                                catch( Exception ex )
                                {
                                    tracker.TCS.SetException( ex );
                                }
                                break;
                            case StopTrackerRequest disposeTracker:
                                try
                                {
                                    await disposeTracker.Tracker.DoStopAsync( _monitor ).ConfigureAwait( false );
                                    disposeTracker.TCS.SetResult();
                                }
                                catch( Exception ex )
                                {
                                    disposeTracker.TCS.SetException( ex );
                                }
                                break;
                            case IssuesReportRequest report:
                                HandleGetIssuesReport( report );
                                break;
                            case TaskCompletionSource sync:
                                sync.SetResult();
                                break;
                            default:
                                Throw.NotSupportedException( o.ToString() );
                                break;
                        }
                    }
                }
                catch( Exception ex )
                {
                    _monitor.Error( "Unhandled exception in GlobalizationIssues.", ex );
                }
            }
            _channel.Writer.Complete();
            // Free any WaitForPendingWorkAsync.
            while( _channel.Reader.TryRead( out o ) )
            {
                if( o is TaskCompletionSource tcs ) tcs.SetResult();
            }
        }
    }

    /// <summary>
    /// Captures the source declaration of a <see cref="CodeString"/>.
    /// </summary>
    /// <param name="ResName">The <see cref="CodeString.ResName"/>.</param>
    /// <param name="FilePath">File path.</param>
    /// <param name="LineNumber">Line number.</param>
    public readonly record struct CodeStringSourceLocation( string ResName, string FilePath, int LineNumber )
    {
        /// <summary>
        /// Overridden to return "FilePath@LineNumber".
        /// </summary>
        /// <returns>The source location.</returns>
        public override string ToString() => $"{FilePath}@{LineNumber}";
    }

    static void HandleStringCreated( CodeString s, string? filePath, int lineNumber )
    {
        if( string.IsNullOrWhiteSpace( filePath ) )
        {
            _monitor.Warn( ActivityMonitor.Tags.ToBeInvestigated, $"Null or empty filePath for '{s}' CodeString ." );
            return;
        }
        byte[] sha = new byte[20];
        s.FormattedString.WriteFormatSHA1( sha );
        bool isNew = false;
        _codeSringOccurrence.AddOrUpdate(
            sha,
            _ =>
            {
                isNew = true;
                return new[] { new CodeStringSourceLocation( s.ResName, filePath, lineNumber ) };
            },
            ( _, exist ) =>
            {
                // If 2 MCString appears on the same source line, we lose the second one
                // unless its resource same differ. This is "by design" and not a big deal.
                var o = new CodeStringSourceLocation( s.ResName, filePath, lineNumber );
                if( !exist.Contains( o ) )
                {
                    Array.Resize( ref exist, exist.Length + 1 );
                    exist[exist.Length - 1] = o;
                }
                return exist;
            } );
        if( isNew )
        {
            if( s.FormattedString.IsEmptyFormat )
            {
                _monitor.Warn( $"Empty CodeString created at '{filePath}@{lineNumber}'." );
            }
            else if( s.ResName.StartsWith( "SHA." ) )
            {
                // This may also be a Warn...
                _monitor.Trace( $"Missing Resource Name for CodeString at '{filePath}@{lineNumber}'." );
            }
        }
    }

    static async Task HandleMissingTranslationResourceAsync( PositionalCompositeFormat? format, MCString s )
    {
        _missingTranslations ??= new Dictionary<ResKey, CodeString>();
        var c = s.CodeString;
        var primary = c.TargetCulture.PrimaryCulture;
        if( _missingTranslations.TryAdd( new ResKey( primary, c.ResName ), c ) )
        {
            var issue = new MissingTranslationResource( c );
            _monitor.Warn( issue.ToString() );
            await _onNewIssue.SafeRaiseAsync( _monitor, issue );
        }
        if( format.HasValue && format.Value.ExpectedArgumentCount != c.FormattedString.Placeholders.Count )
        {
            await HandleFormatArgumentCountErrorAsync( format.Value, s );
        }
    }

    static Task HandleFormatArgumentCountErrorAsync( in PositionalCompositeFormat format, MCString s )
    {
        _formatArgumentError ??= new Dictionary<ResKey, FormatArgumentCountError>();
        // Use ContainsKey to avoid a useless issue allocation.
        var key = new ResKey( s.FormatCulture, s.CodeString.ResName );
        if( !_formatArgumentError.ContainsKey( key ) )
        {
            var issue = new FormatArgumentCountError( format, s );
            _formatArgumentError.Add( key, issue );
            _monitor.Warn( issue.ToString() );
            return _onNewIssue.SafeRaiseAsync( _monitor, issue );
        }
        return Task.CompletedTask;
    }

    internal static void OnIdentifierClash( string name, int finalId, List<string> clashes )
    {
        _channel.Writer.TryWrite( new CultureIdentifierClash( name, finalId, clashes.ToArray() ) );
    }

    internal static void OnTranslation( Issue i )
    {
        Throw.DebugAssert( i is TranslationDuplicateResource || i is TranslationFormatError );
        _channel.Writer.TryWrite( i );
    }

    internal static void OnCodeStringCreated( CodeString s, string? filePath, int lineNumber )
    {
        _channel.Writer.TryWrite( new PrivateCodeStringCreated( s, filePath, lineNumber ) );
    }

    internal static void OnMCStringCreated( in PositionalCompositeFormat format, MCString mc )
    {
        if( mc.IsTranslationWelcome )
        {
            _channel.Writer.TryWrite( new PrivateMissingTranslationResource( format, mc ) );
        }
        else if( format.ExpectedArgumentCount != mc.CodeString.FormattedString.Placeholders.Count )
        {
            _channel.Writer.TryWrite( new PrivateFormatArgumentCountError( format, mc ) );
        }
    }

    internal static void OnMCStringCreated( MCString mc )
    {
        if( mc.IsTranslationWelcome )
        {
            _channel.Writer.TryWrite( new PrivateMissingTranslationResource( null, mc ) );
        }
    }

    internal static void OnNewCulture( AllCultureSnapshot snapshot, ExtendedCultureInfo e )
    {
        _channel.Writer.TryWrite( new ExtendedCultureInfoCreatedEvent( snapshot, e ) );
    }

    internal static Task StartTrackerAsync( ExtendedCultureInfoTracker tracker, CancellationToken cancellationToken = default )
    {
        var tcs = new TaskCompletionSource();
        _channel.Writer.TryWrite( new StartTrackerRequest( tcs, tracker, cancellationToken ) );
        return tcs.Task;
    }

    internal static Task StopTrackerAsync( ExtendedCultureInfoTracker tracker )
    {
        var tcs = new TaskCompletionSource();
        _channel.Writer.TryWrite( new StopTrackerRequest( tcs, tracker ) );
        return tcs.Task;
    }

    // Private only.
    sealed record PrivateCodeStringCreated( CodeString String, string? FilePath, int LineNumber ) : Issue;
    sealed record PrivateMissingTranslationResource( PositionalCompositeFormat? Format, MCString MCString ) : Issue;
    sealed record PrivateFormatArgumentCountError( PositionalCompositeFormat Format, MCString MCString ) : Issue;
    sealed record IssuesReportRequest( TaskCompletionSource<IssuesReport> TCS, bool Reset );
    sealed record StartTrackerRequest( TaskCompletionSource TCS, ExtendedCultureInfoTracker Tracker, CancellationToken CancellationToken );
    sealed record StopTrackerRequest( TaskCompletionSource TCS, ExtendedCultureInfoTracker Tracker );
}
