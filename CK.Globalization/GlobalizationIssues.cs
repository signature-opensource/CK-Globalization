using CK.PerfectEvent;
using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.Core
{
    public static class GlobalizationIssues
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
        /// Gets the list of <see cref="IdentifierClash"/> that occurred.
        /// These are always collected, regardless of whether <see cref="Track"/> is opened or not.
        /// </summary>
        public static IReadOnlyList<IdentifierClash> IdentifierClashes => _identifierClashes;

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
        static PerfectEventSender<Issue> _onNewIssue;
        // Internal for tests (ClearCache).
        internal static IdentifierClash[] _identifierClashes;
        internal static readonly ConcurrentDictionary<byte[], CodeStringSourceLocation[]> _codeSringOccurrence;

        static GlobalizationIssues()
        {
            Track = new StaticGate( "CK.Core.GlobalizationIssues.Track", false );
            _channel = Channel.CreateUnbounded<Issue?>( new UnboundedChannelOptions { SingleReader = true } );
            _monitor = new ActivityMonitor( nameof( GlobalizationIssues ) );
            _onNewIssue = new PerfectEventSender<Issue>();
            _identifierClashes = Array.Empty<IdentifierClash>();
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
                                continue;
                            case PrivateMissingTranslationResource m:
                                HandleMissingTranslationResource( m.Format, m.CodeString );
                                continue;
                            case IdentifierClash c:
                                Util.InterlockedAdd( ref _identifierClashes, c );
                                _monitor.UnfilteredLog( LogLevel.Warn | LogLevel.IsFiltered,
                                                        ActivityMonitor.Tags.ToBeInvestigated,
                                                        $"CultureInfo name identifier clash: '{c.Name}' has been associated to '{c.Id}' because of '{c.Clashes.Concatenate( "', '" )}'.",
                                                        null );
                                break;
                        }
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
                    _monitor.Warn( $"Missing Resource Name for CodeString at '{filePath}@{lineNumber}'." );
                }
            }
        }

        static void HandleMissingTranslationResource( PositionalCompositeFormat? format, CodeString s )
        {
            throw new NotImplementedException();
        }

        internal static void OnIdentifierClash( string name, int finalId, List<string> clashes )
        {
            _channel.Writer.TryWrite( new IdentifierClash( name, finalId, clashes.ToArray() ) );
        }

        internal static void OnResourceFormatError( string n, string f, string error )
        {
            _channel.Writer.TryWrite( new ResourceFormatError( n, f, error ) );
        }

        internal static void OnResourceFormatDuplicate( string n, PositionalCompositeFormat s, PositionalCompositeFormat r )
        {
            _channel.Writer.TryWrite( new ResourceFormatDuplicate( n, s, r ) );
        }

        internal static void OnCodeStringCreated( CodeString s, string? filePath, int lineNumber )
        {
            _channel.Writer.TryWrite( new PrivateCodeStringCreated( s, filePath, lineNumber ) );
        }

        internal static void OnMCStringCreated( in PositionalCompositeFormat format, MCString mc )
        {
            if( mc.TranslationLevel < MCString.Quality.Good )
            {
                _channel.Writer.TryWrite( new PrivateMissingTranslationResource( format, mc.CodeString ) );
            }
            else
            {
                _channel.Writer.TryWrite( new PrivateCheckFormat( format, mc.CodeString ) );
            }
        }

        internal static void OnMCStringCreated( MCString mc )
        {
            if( mc.TranslationLevel < MCString.Quality.Good )
            {
                _channel.Writer.TryWrite( new PrivateMissingTranslationResource( null, s ) );
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
        public sealed record IdentifierClash( string Name, int Id, IReadOnlyList<string> Clashes ) : Issue;

        /// <summary>
        /// Describes a resource format error emitted by <see cref="NormalizedCultureInfo.SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/>.
        /// This issue is collected only if the <see cref="Track"/> static gate is opened.
        /// </summary>
        /// <param name="ResName">The resource name.</param>
        /// <param name="Format">The invalid format.</param>
        /// <param name="Error">The error message.</param>
        public sealed record ResourceFormatError( string ResName, string Format, string Error ) : Issue;

        /// <summary>
        /// Describes a duplicate resource. Emitted by <see cref="NormalizedCultureInfo.SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/>.
        /// This issue is collected only if the <see cref="Track"/> static gate is opened.
        /// </summary>
        /// <param name="ResName">The resource name.</param>
        /// <param name="Skipped">The skipped format.</param>
        /// <param name="Registered">The already registered format.</param>
        public sealed record ResourceFormatDuplicate( string ResName, PositionalCompositeFormat Skipped, PositionalCompositeFormat Registered ) : Issue;

        /// <summary>
        /// A missing resource has been detected.
        /// </summary>
        /// <param name="MissingCulture">The culture in which the resource should be defined.</param>
        /// <param name="Instance">
        /// The first instance with the <see cref="FormattedString"/> that lacks a resource.
        /// The same format may be shared by multiple <see cref="CodeStringSourceLocation"/>.
        /// </param>
        public sealed record MissingTranslationResource( NormalizedCultureInfo MissingCulture, CodeString Instance ) : Issue;

        // Private only.
        sealed record PrivateCodeStringCreated( CodeString String, string? FilePath, int LineNumber ) : Issue;
        sealed record PrivateMissingTranslationResource( PositionalCompositeFormat? Format, CodeString CodeString ) : Issue;
        sealed record PrivateCheckFormat( PositionalCompositeFormat? Format, CodeString CodeString ) : Issue;

    }
}
