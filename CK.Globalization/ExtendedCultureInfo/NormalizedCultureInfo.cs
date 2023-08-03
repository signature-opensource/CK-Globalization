using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace CK.Core
{
    public sealed class NormalizedCultureInfo : ExtendedCultureInfo
    {
        readonly CultureInfo _culture;
        Dictionary<string, PositionalCompositeFormat> _translations;

        /// <summary>
        /// The invariant culture has only itself as a fallback.
        /// </summary>
        public static readonly NormalizedCultureInfo Invariant;

        /// <summary>
        /// Simple relay that calls <see cref="GetNormalizedCultureInfo(CultureInfo)"/> with the <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        public static NormalizedCultureInfo Current => GetNormalizedCultureInfo( CultureInfo.CurrentCulture );

        /// <summary>
        /// Gets the <see cref="CultureInfo"/>.
        /// </summary>
        public CultureInfo Culture => _culture;

        /// <summary>
        /// Sets a cached set of resource translation formats.
        /// This must not be called for <see cref="ExtendedCultureInfo.IsDefault"/> otherwise
        /// an <see cref="InvalidOperationException"/> is thrown.
        /// <para>
        /// Duplicates can exist in the <paramref name="map"/>: the last resource name is kept, the previous
        /// ones are discarded.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use.</param>
        /// <param name="map">The map.</param>
        public void SetCachedTranslations( IActivityMonitor monitor, IEnumerable<(string ResName, string Format)> map )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckState( IsDefault is false );
            var d = new Dictionary<string, PositionalCompositeFormat>();
            foreach( var (n, f) in map )
            {
                if( !PositionalCompositeFormat.TryParse( f, out var p, out var error ) )
                {
                    monitor.Warn( $"Invalid format string in '{Name}' for resource '{n}': {error}" );
                }
                else
                {
                    d[n] = p;
                }
            }
            _translations = d;
        }

        public bool TryGetCachedTranslation( string resourceName, out PositionalCompositeFormat format )
        {
            return _translations.TryGetValue( resourceName, out format );
        }

        NormalizedCultureInfo( Dictionary<string, PositionalCompositeFormat> noTranslations )
        {
            _culture = CultureInfo.InvariantCulture;
            _translations = noTranslations;
        }

        internal NormalizedCultureInfo( CultureInfo culture, string name, List<NormalizedCultureInfo> fallbacks )
            : base( name, fallbacks )
        {
            _culture = culture;
            _translations = _noTranslations;
        }

        static Dictionary<object, ExtendedCultureInfo> _all;
        static readonly Dictionary<string, PositionalCompositeFormat> _noTranslations;

        static NormalizedCultureInfo()
        {
            _noTranslations = new Dictionary<string, PositionalCompositeFormat>();
            Invariant = new NormalizedCultureInfo( _noTranslations );
            _all = new Dictionary<object, ExtendedCultureInfo>()
            {
                { "", Invariant },
                { Invariant, Invariant }
            };
        }

        /// <summary>
        /// Gets a cached normalized culture info from its name or tries to create it.
        /// </summary>
        /// <param name="name">The culture name.</param>
        /// <returns>The culture.</returns>
        public static NormalizedCultureInfo GetNormalizedCultureInfo( string name )
        {
            Throw.CheckNotNullArgument( name );
            // Fast path.
            if( _all.TryGetValue( name, out var e ) )
            {
                return e.PrimaryCulture;
            }
            // Let the CultureInfo.GetCultureInfo does its job on the culture name.
            // We don't try to optimize here. Either the name is from our normalization
            // or it is an external name that must be fully handle.
            return GetNormalizedCultureInfo( CultureInfo.GetCultureInfo( name ) );
        }

        /// <summary>
        /// Gets a cached normalized culture info from its name or creates it.
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        /// <returns>The culture.</returns>
        public static NormalizedCultureInfo GetNormalizedCultureInfo( CultureInfo cultureInfo )
        {
            Throw.CheckNotNullArgument( cultureInfo );
            if( _all.TryGetValue( cultureInfo, out var e ) )
            {
                return e.PrimaryCulture;
            }
            var name = cultureInfo.Name.ToLowerInvariant();
            if( _all.TryGetValue( name, out e ) )
            {
                return e.PrimaryCulture;
            }
            // The culture must now be readonly since we register it.
            // We also reject any specialization.
            Throw.CheckArgument( cultureInfo.IsReadOnly && cultureInfo.GetType() == typeof( CultureInfo ) );
            lock( _all )
            {
                var all = new Dictionary<object, ExtendedCultureInfo>( _all );
                var c = DoRegister( name, cultureInfo, all );
                _all = all;
                return c;
            }
        }

        static NormalizedCultureInfo DoRegister( string name, CultureInfo cultureInfo, Dictionary<object, ExtendedCultureInfo> all )
        {
            Throw.DebugAssert( Monitor.IsEntered( _all ) );
            Throw.DebugAssert( name.ToLowerInvariant() == name );
            if( all.TryGetValue( name, out var exist ) )
            {
                return exist.PrimaryCulture;
            }
            var fallbacks = new List<NormalizedCultureInfo>();
            var parent = cultureInfo.Parent;
            while( parent != CultureInfo.InvariantCulture )
            {
                fallbacks.Add( DoRegister( parent.Name.ToLowerInvariant(), parent, all ) );
                parent = parent.Parent;
            }
            var newOne = new NormalizedCultureInfo( cultureInfo, name, fallbacks );
            all.Add( name, newOne );
            if( name != cultureInfo.Name ) all.Add( cultureInfo.Name, newOne );
            all.Add( cultureInfo, newOne );
            return newOne;
        }

        public static ExtendedCultureInfo GetExtendedCultureInfo( string fullName )
        {
            Throw.CheckNotNullOrEmptyArgument( fullName );
            // Fast path.
            if( _all.TryGetValue( fullName, out var e ) )
            {
                return e.PrimaryCulture;
            }
            // Very basic preprocessing should be enough.
            var fullNames = fullName.ToLowerInvariant()
                                    .Split( ',', StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries );
            Throw.CheckArgument( "fullName must contain at least one culture name.", fullNames.Length == 0 );
            // Don't use GetNormalizedCultureInfo( name ) if there's only one culture as it may
            // consider a "-XXXX" name and return its primary culture.
            lock( _all )
            {
                var fallbacks = new List<NormalizedCultureInfo>();
                var all = new Dictionary<object, ExtendedCultureInfo>( _all );
                foreach( var name in fullNames )
                {
                    if( all.TryGetValue( name, out var exists ) )
                    {
                        AddFallbacks( fallbacks, exists );
                    }
                    else
                    {
                        // Our "name" may differ here from the returned cultureInfo.Name.
                        var cultureInfo = CultureInfo.GetCultureInfo( name );
                        var finalName = cultureInfo.Name.ToLowerInvariant();
                        if( all.TryGetValue( finalName, out exists ) )
                        {
                            AddFallbacks( fallbacks, exists );
                        }
                        else
                        {
                            var c = DoRegister( finalName, cultureInfo, all );
                            fallbacks.Add( c );
                        }
                    }
                }
                if( fallbacks.Count == 1 ) return fallbacks[0];
                e = new ExtendedCultureInfo( fallbacks );
                _all.Add( e.Name, e );
                _all.Add( e.FullName, e );
                _all = all;
                return e;
            }

            static void AddFallbacks( List<NormalizedCultureInfo> fallbacks, ExtendedCultureInfo? exists )
            {
                if( exists is NormalizedCultureInfo c )
                {
                    fallbacks.Add( c );
                }
                else
                {
                    fallbacks.AddRange( exists.Fallbacks );
                }
            }
        }
    }
}
