using CK.PerfectEvent;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Tracks and collects globalization issues:
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
    public static partial class GlobalizationIssues
    {
        /// <summary>
        /// The "CK.Core.GlobalizationIssues.Track" static gate is closed by default.
        /// </summary>
        public static readonly StaticGate Track;

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

        static readonly Channel<Issue?> _channel;
        static readonly IActivityMonitor _monitor;
        static readonly PerfectEventSender<Issue> _onNewIssue;
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

        static GlobalizationIssues()
        {
            Track = new StaticGate( "CK.Core.GlobalizationIssues.Track", false );
            _channel = Channel.CreateUnbounded<Issue?>( new UnboundedChannelOptions { SingleReader = true } );
            _monitor = new ActivityMonitor( nameof( GlobalizationIssues ) );
            _monitor.AutoTags = ActivityMonitor.Tags.Register( "Globalization" );
            _onNewIssue = new PerfectEventSender<Issue>();
            _identifierClashes = Array.Empty<CultureIdentifierClash>();
            _codeSringOccurrence = new ConcurrentDictionary<byte[], CodeStringSourceLocation[]>( new SHAComparer() );
            _ = Task.Run( RunAsync );
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            static void OnProcessExit( object? sender, EventArgs e ) => _channel.Writer.TryWrite( null );

            static async Task RunAsync()
            {
                Issue? o;
                while( (o = await _channel.Reader.ReadAsync().ConfigureAwait( false )) != null )
                {
                    try
                    {
                        switch( o )
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
                                Throw.DebugAssert( o is TranslationFormatError || o is TranslationDuplicateResource );
                                _monitor.Warn( o.ToString() );
                                break;
                        }
                        Throw.DebugAssert( o is TranslationFormatError || o is TranslationDuplicateResource || o is CultureIdentifierClash );
                        await _onNewIssue.SafeRaiseAsync( _monitor, o ).ConfigureAwait( false );
                    }
                    catch( Exception ex )
                    {
                        _monitor.Error( "Unhandled exception in GlobalizationIssues.", ex );
                    }
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
                        exist[exist.Length-1] = o;
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

        /// <summary>
        /// Generalizes all globalization issues.
        /// </summary>
        public abstract record Issue { }

        /// <summary>
        /// Identifier clashes are always tracked (the static gate <see cref="Track"/> is ignored).
        /// This MUST be handled by specifically registering the exception. 
        /// </summary>
        /// <param name="Name">
        /// The culture name or names that couldn't be identified by the DBJ2 hash code of its name because its hash
        /// is the same as the first <paramref name="Clashes"/> name.
        /// </param>
        /// <param name="Id">The final identifier that has been eventually assigned to <paramref name="Name"/>.</param>
        /// <param name="Clashes">One or more clashing names that shifted the <paramref name="Id"/> by 1.</param>
        public sealed record CultureIdentifierClash( string Name, int Id, IReadOnlyList<string> Clashes ) : Issue
        {
            /// <summary>
            /// Provides the description.
            /// </summary>
            /// <returns>This issue description.</returns>
            public override string ToString()
                => $"CultureInfo name identifier clash: '{Name}' has been associated to '{Id}' because of '{Clashes.Concatenate( "', '" )}'.";

        }

        /// <summary>
        /// Describes a resource format error emitted by <see cref="NormalizedCultureInfo.SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/>.
        /// This issue is logged (but not collected) only if the <see cref="Track"/> static gate is opened.
        /// </summary>
        /// <param name="Culture">The culture that contains the resource.</param>
        /// <param name="ResName">The resource name.</param>
        /// <param name="Format">The invalid format.</param>
        /// <param name="Error">The error message that contains the invalid <see cref="Format"/>.</param>
        public sealed record TranslationFormatError( NormalizedCultureInfo Culture, string ResName, string Format, string Error ) : Issue
        {
            /// <summary>
            /// Provides the description.
            /// </summary>
            /// <returns>This issue description.</returns>
            public override string ToString() => $"Invalid format for '{ResName}' in '{Culture.Name}': {Error}";
        }

        /// <summary>
        /// Describes a duplicate resource. Emitted by <see cref="NormalizedCultureInfo.SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/>.
        /// This issue is logged (but not collected) only if the <see cref="Track"/> static gate is opened.
        /// </summary>
        /// <param name="Culture">The culture that duplicates the resource.</param>
        /// <param name="ResName">The resource name.</param>
        /// <param name="Skipped">The skipped format.</param>
        /// <param name="Registered">The already registered format.</param>
        public sealed record TranslationDuplicateResource( NormalizedCultureInfo Culture,
                                                           string ResName,
                                                           PositionalCompositeFormat Skipped,
                                                           PositionalCompositeFormat Registered ) : Issue
        {
            /// <summary>
            /// Provides the description.
            /// </summary>
            /// <returns>This issue description.</returns>
            public override string ToString() => $"Duplicate resource '{ResName}' in '{Culture.Name}'. Skipped: '{Skipped.GetFormatString()}'.";
        }

        /// <summary>
        /// A missing resource has been detected.
        /// </summary>
        /// <param name="Instance">
        /// The first instance with the <see cref="FormattedString"/> that lacks a translation.
        /// The same format may be shared by multiple <see cref="CodeStringSourceLocation"/>.
        /// </param>
        public sealed record MissingTranslationResource( CodeString Instance ) : Issue
        {
            /// <summary>
            /// The culture in which the resource should be defined.
            /// </summary>
            public NormalizedCultureInfo MissingCulture => Instance.TargetCulture.PrimaryCulture;

            /// <summary>
            /// The resource name to be defined.
            /// </summary>
            public string ResName => Instance.ResName;

            /// <summary>
            /// Provides the description.
            /// </summary>
            /// <returns>This issue description.</returns>
            public override string ToString()
                => $"Missing translation for '{ResName}' in '{MissingCulture.FullName}' at {GetSourceLocation( Instance ).Select( l => l.ToString() ).Concatenate()}.";
        }

        /// <summary>
        /// A <see cref="PositionalCompositeFormat"/> has not the same number of expected arguments as the code
        /// string has <see cref="FormattedString.Placeholders"/>.
        /// </summary>
        /// <param name="Format">The invalid format.</param>
        /// <param name="Instance">
        /// The first translated that raised the issue.
        /// The same format may be shared by multiple <see cref="CodeStringSourceLocation"/>.
        /// </param>
        public sealed record FormatArgumentCountError( PositionalCompositeFormat Format, MCString Instance ) : Issue
        {
            /// <summary>
            /// The resource culture.
            /// </summary>
            public NormalizedCultureInfo FormatCulture => Instance.FormatCulture;

            /// <summary>
            /// The resource name.
            /// </summary>
            public string ResName => Instance.CodeString.ResName;

            /// <summary>
            /// The number of arguments that <see cref="Format"/> expects.
            /// </summary>
            public int ExpectedArgumentCount => Format.ExpectedArgumentCount;

            /// <summary>
            /// The number of actual placeholders in the CodeString.
            /// </summary>
            public int PlaceholderCount => Instance.CodeString.FormattedString.Placeholders.Count;

            /// <summary>
            /// Provides the description.
            /// </summary>
            /// <returns>This issue description.</returns>
            public override string ToString()
                => $"Translation '{ResName}' in '{FormatCulture}' expects {ExpectedArgumentCount} arguments " +
                   $"but CodeString has {PlaceholderCount} placeholders at {GetSourceLocation( Instance.CodeString ).Select( l => l.ToString() ).Concatenate()}.";
        }

        // Private only.
        sealed record PrivateCodeStringCreated( CodeString String, string? FilePath, int LineNumber ) : Issue;
        sealed record PrivateMissingTranslationResource( PositionalCompositeFormat? Format, MCString MCString ) : Issue;
        sealed record PrivateFormatArgumentCountError( PositionalCompositeFormat Format, MCString MCString ) : Issue;
        sealed record PrivateGetReport( TaskCompletionSource<Report> TCS, bool Reset ) : Issue;

    }
}
