using CK.PerfectEvent;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CK.Core
{
    public static class GlobalizationIssues
    {
        public static readonly StaticGate Track;

        public static PerfectEvent<Issue> OnNewIssue => _onNewIssue.PerfectEvent;

        public static IReadOnlyList<IdentifierClash> IdentifierClashes => _identifierClashes;

        static readonly Channel<Issue?> _channel;
        static readonly IActivityMonitor _monitor;
        static PerfectEventSender<Issue> _onNewIssue;
        // Internal for tests (ClearCache).
        internal static IdentifierClash[] _identifierClashes;

        static GlobalizationIssues()
        {
            Track = new StaticGate( "CK.Core.GlobalizationIssues.Track", true );
            _channel = Channel.CreateUnbounded<Issue?>( new UnboundedChannelOptions { SingleReader = true } );
            _monitor = new ActivityMonitor( nameof( GlobalizationIssues ) );
            _onNewIssue = new PerfectEventSender<Issue>();
            _identifierClashes = Array.Empty<IdentifierClash>();

            _ = Task.Run( RunAsync );
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            static void OnProcessExit( object? sender, EventArgs e ) => _channel.Writer.TryWrite( null );

            static async Task RunAsync()
            {
                Issue? o;
                while( (o = await _channel.Reader.ReadAsync().ConfigureAwait( false )) != null )
                {
                    if( o is IdentifierClash c )
                    {
                        Util.InterlockedAdd( ref _identifierClashes, c );
                        _monitor.UnfilteredLog( LogLevel.Warn|LogLevel.IsFiltered,
                                                ActivityMonitor.Tags.ToBeInvestigated,
                                                $"CultureInfo name identifier clash: '{c.Name}' has been associated to '{c.Id}' because of '{c.Clashes.Concatenate("', '")}'.",
                                                null );
                    }
                    await _onNewIssue.SafeRaiseAsync( _monitor, o ).ConfigureAwait( false );
                }
            }
        }

        internal static void OnIdentifierClash( string name, int finalId, List<string> clashes )
        {
            _channel.Writer.TryWrite( new IdentifierClash( name, finalId, clashes.ToArray() ) );
        }

        internal static void OnResourceFormatError( string n, string f, string error )
        {
            _channel.Writer.TryWrite( new ResourceFormatError( n, f, error ) );
        }

        internal static void OnResourceFormatDuplicate( string n, string f, PositionalCompositeFormat p, PositionalCompositeFormat positionalCompositeFormat )
        {
            throw new NotImplementedException();
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
        /// Describes a duplicate resource emitted by <see cref="NormalizedCultureInfo.SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/>.
        /// This issue is collected only if the <see cref="Track"/> static gate is opened.
        /// </summary>
        /// <param name="ResName">The resource name.</param>
        /// <param name="Skipped">The skipped format.</param>
        /// <param name="Registered">The already registered format.</param>
        public sealed record ResourceFormatDuplicate( string ResName, PositionalCompositeFormat Skipped, PositionalCompositeFormat Registered ) : Issue;
    }
}
