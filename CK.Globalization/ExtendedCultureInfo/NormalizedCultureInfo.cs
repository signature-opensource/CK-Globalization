using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace CK.Core
{
    /// <summary>
    /// Sealed <see cref="ExtendedCultureInfo"/> that corresponds to an actual <see cref="CultureInfo"/>
    /// that is its own <see cref="ExtendedCultureInfo.PrimaryCulture"/> and whose <see cref="ExtendedCultureInfo.Fallbacks"/>
    /// are computed from <see cref="CultureInfo.Parent"/> path.
    /// </summary>
    public sealed class NormalizedCultureInfo : ExtendedCultureInfo
    {
        readonly CultureInfo _culture;
        Dictionary<string, PositionalCompositeFormat> _translations;

        /// <summary>
        /// The invariant culture has only itself as a fallback.
        /// </summary>
        public static readonly NormalizedCultureInfo Invariant;

        /// <summary>
        /// The default culture is bound to the "en-US" culture by convention.
        /// Its fallback is empty since "en-US", "en" and Invariant are the same for us
        /// and cannot have cached translations.
        /// </summary>
        public static readonly NormalizedCultureInfo CodeDefault;

        /// <summary>
        /// Simple relay that calls <see cref="GetNormalizedCultureInfo(CultureInfo)"/> with the <see cref="CultureInfo.CurrentCulture"/>.
        /// </summary>
        public static NormalizedCultureInfo Current => GetNormalizedCultureInfo( CultureInfo.CurrentCulture );

        /// <summary>
        /// Gets the <see cref="CultureInfo"/>.
        /// </summary>
        public CultureInfo Culture => _culture;

        /// <summary>
        /// Sets a cached set of resource translation formats from a dictionary of resource name to positional composite
        /// string formats. See <see cref="SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/>.
        /// </summary>
        /// <param name="map">The map.</param>
        public void SetCachedTranslations( IEnumerable<KeyValuePair<string,string>> map )
        {
            SetCachedTranslations( map.Select( kv => (kv.Key,kv.Value) ) );
        }

        /// <summary>
        /// Sets a cached set of resource translation formats.
        /// This must not be called for <see cref="ExtendedCultureInfo.IsDefault"/> otherwise
        /// an <see cref="InvalidOperationException"/> is thrown.
        /// <para>
        /// When the static gate <see cref="GlobalizationIssues.Track"/> is opened, <see cref="GlobalizationIssues.ResourceFormatError"/>
        /// are emitted for invalid format strings.
        /// </para>
        /// <para>
        /// Duplicates can exist in the <paramref name="map"/>: the first resource name is kept, the subsequent
        /// ones are discarded and a <see cref="GlobalizationIssues.ResourceFormatDuplicate"/> is emitted (when
        /// the static gate <see cref="GlobalizationIssues.Track"/> is opened).
        /// </para>
        /// </summary>
        /// <param name="map">The map.</param>
        public void SetCachedTranslations( IEnumerable<(string ResName, string Format)> map )
        {
            Throw.CheckState( IsDefault is false );
            var d = new Dictionary<string, PositionalCompositeFormat>();
            foreach( var (n, f) in map )
            {
                if( !PositionalCompositeFormat.TryParse( f, out var p, out var error ) )
                {
                    if( GlobalizationIssues.Track.IsOpen )
                    {
                        GlobalizationIssues.OnResourceFormatError( n, f, error );
                    }
                }
                else if( !d.TryAdd( n, p ) )
                {
                    if( GlobalizationIssues.Track.IsOpen )
                    {
                        GlobalizationIssues.OnResourceFormatDuplicate( n, f, p, d[n] );
                    }
                }
            }
            _translations = d;
        }

        /// <summary>
        /// Tries to obtain a format in this culture for a given resource name.
        /// </summary>
        /// <param name="resourceName">The resource name.</param>
        /// <param name="format">The format to apply on sucess.</param>
        /// <returns>False if the <paramref name="resourceName"/> has an associated format, false otherwise.</returns>
        public bool TryGetCachedTranslation( string resourceName, out PositionalCompositeFormat format )
        {
            return _translations.TryGetValue( resourceName, out format );
        }

        NormalizedCultureInfo( Dictionary<string, PositionalCompositeFormat> definitelyNoTranslations, string name, int id, CultureInfo c )
            : base( name, id )
        {
            _culture = c;
            _translations = definitelyNoTranslations;
        }

        internal NormalizedCultureInfo( CultureInfo culture, string name, int id, NormalizedCultureInfo[] fallbacks )
            : base( name, id, fallbacks )
        {
            _culture = culture;
            _translations = _noTranslations;
        }

        static Dictionary<object, ExtendedCultureInfo> _all;
        static readonly Dictionary<string, PositionalCompositeFormat> _noTranslations;

        static NormalizedCultureInfo()
        {
            _noTranslations = new Dictionary<string, PositionalCompositeFormat>();
            var cInv = CultureInfo.InvariantCulture;
            Invariant = new NormalizedCultureInfo( _noTranslations, string.Empty, 0, cInv );
            var cEn = CultureInfo.GetCultureInfo( "en" );
            bool isInvariantModeWithPredefinedOnly = cEn == cInv;
            var cEnUS = isInvariantModeWithPredefinedOnly ? cInv : CultureInfo.GetCultureInfo( "en-US" );
            Throw.DebugAssert( "en".GetDjb2HashCode() == 221277614 );
            Throw.DebugAssert( "en-us".GetDjb2HashCode() == -1255733531 );
            var en = new NormalizedCultureInfo( _noTranslations, "en", 221277614, cEn );
            var enUS = new NormalizedCultureInfo( _noTranslations, "en-us", -1255733531, cEnUS );
            _all = new Dictionary<object, ExtendedCultureInfo>()
            {
                { "", Invariant },
                { 0, Invariant },
                { Invariant, Invariant },
                { "en", en },
                { 221277614, en },
                { "en-US", enUS },
                { "en-us", enUS },
                { "en-us,en", enUS },
                { -1255733531, enUS },
            };
            if( !isInvariantModeWithPredefinedOnly )
            {
                _all.Add( cEn, en );
                _all.Add( cEnUS, enUS );
            }
            CodeDefault = enUS;
        }

        // This is for tests only. Tests use reflection to call this.
        // Even if there should be no differences between Debug and Release builds
        // we also test in Release (no #if DEBUG).
        static void ClearCache()
        {
            var all = new Dictionary<object, ExtendedCultureInfo>( _all.Where( IsUnremovable ) );
            _all = all;
            GlobalizationIssues._identifierClashes = Array.Empty<GlobalizationIssues.IdentifierClash>();

            Throw.DebugAssert( "en".GetDjb2HashCode() == 221277614 );
            Throw.DebugAssert( "en-us".GetDjb2HashCode() == -1255733531 );

            static bool IsUnremovable( KeyValuePair<object, ExtendedCultureInfo> c )
            {
                return (c.Key is string k && (k == "" || k == "en" || k == "en-us" || k == "en-us,en" || k == "en-US"))
                       ||
                       (c.Key is CultureInfo i && (i.Name == "" || i.Name == "en" || i.Name == "en-US"))
                       ||
                       (c.Key is int id && (id == 0 || id == 221277614 || id == -1255733531));
            }
        }

        /// <summary>
        /// Gets a cached normalized culture info from its name or creates it.
        /// The name must be a valid BCP47 language tag otherwise a <see cref="CultureNotFoundException"/> is raised.
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
        /// Gets a cached normalized culture info from a CultureInfo or registers it.
        /// Note that if a <see cref="NormalizedCultureInfo"/> has already been registered
        /// with the same normalized <see cref="CultureInfo.Name"/>, the cached instance
        /// is returned: the <paramref name="cultureInfo"/> parameter is not referenced.
        /// <para>
        /// Note that <see cref="CultureInfo.IsReadOnly"/> can be false (this is a <c>new CultureInfo(...)</c>).
        /// No check is done. Using non cached (ie. non read only) CultureInfo instances should be avoided.
        /// </para>
        /// </summary>
        /// <param name="cultureInfo">The culture info.</param>
        /// <returns>The culture.</returns>
        public static NormalizedCultureInfo GetNormalizedCultureInfo( CultureInfo cultureInfo )
        {
            Throw.CheckNotNullArgument( cultureInfo );
            // Fast paths: lookup the cultureInfo instance and the its name ToLowerInvariant.
            if( _all.TryGetValue( cultureInfo, out var e ) )
            {
                return e.PrimaryCulture;
            }
            var name = cultureInfo.Name.ToLowerInvariant();
            if( _all.TryGetValue( name, out e ) )
            {
                return e.PrimaryCulture;
            }
            // First idea was: the culture must now be readonly since we register it. And we also reject any specialization.
            //
            //      Throw.CheckArgument( cultureInfo.IsReadOnly && cultureInfo.GetType() == typeof( CultureInfo ) );
            //
            // Unfortunately, non read only CultureInfo seems common: the NUnit [SetCulture(...)] attribute for instance
            // sets a non readonly culture.
            // So we drop any check here (and pray).
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
            // This is required her for recursion and as a double check lock when coming
            // from unlocked code.
            if( all.TryGetValue( name, out var exist ) )
            {
                return exist.PrimaryCulture;
            }
            List<NormalizedCultureInfo>? fallbacks = null;
            var parent = cultureInfo.Parent;
            while( parent != CultureInfo.InvariantCulture )
            {
                fallbacks ??= new List<NormalizedCultureInfo>();
                fallbacks.Add( DoRegister( parent.Name.ToLowerInvariant(), parent, all ) );
                parent = parent.Parent;
            }
            int id = ComputeId( all, name );
            var newOne = new NormalizedCultureInfo( cultureInfo, name, id, fallbacks?.ToArray() ?? Array.Empty<NormalizedCultureInfo>() );
            // Register with the single normalized name.
            all.Add( name, newOne );
            // If the CultureInfo.Name differs ("fr-FR" vs. "fr-fr") also register it.
            if( name != cultureInfo.Name ) all.Add( cultureInfo.Name, newOne );
            // Register the CultureInfo instance itself.
            all.Add( cultureInfo, newOne );
            // Register with id Id.
            all.Add( id, newOne );
            // Register the "full name" if it differs from the name:
            // the fallback names is as alias of the NormalizedCultureInfo name.
            if( newOne.Fallbacks.Count > 0 )
            {
                var fullName = string.Join( ',', newOne.Fallbacks.Select( c => c.Name ).Prepend( newOne.Name ) );
                all.Add( fullName, newOne );
            }
            return newOne;
        }

        internal static ExtendedCultureInfo? DoGetExtendedCultureInfo( int id ) => _all.GetValueOrDefault( id );

        internal static ExtendedCultureInfo DoGetExtendedCultureInfo( string commaSeparatedNames )
        {
            Throw.CheckNotNullArgument( commaSeparatedNames );
            // Fast path.
            if( _all.TryGetValue( commaSeparatedNames, out var e ) )
            {
                return e;
            }
            // Before locking/adding we let a chance to a very basic preprocessing.
            commaSeparatedNames = commaSeparatedNames.ToLowerInvariant().Replace( " ", "" );
            if( _all.TryGetValue( commaSeparatedNames, out e ) )
            {
                return e;
            }
            var fullNames = commaSeparatedNames.Split( ',', StringSplitOptions.RemoveEmptyEntries );
            // Single name: use the GetNormalizedCultureInfo.
            if( fullNames.Length == 1 )
            {
                return GetNormalizedCultureInfo( fullNames[0] );
            }
            lock( _all )
            {
                var allCultures = new List<NormalizedCultureInfo>();
                var all = new Dictionary<object, ExtendedCultureInfo>( _all );
                foreach( var name in fullNames )
                {
                    NormalizedCultureInfo c = OneRegister( all, name );
                    int idxFound = -1;
                    int idxLenGen = 0;
                    for( int i = 0; i < allCultures.Count; i++ )
                    {
                        var exists = allCultures[i];
                        // Skips duplicates ("fr", "fr"). We may have a duplicate because of
                        // the CultureInfo.GetCultureInfo name normalization.
                        if( exists == c )
                        {
                            // Skip insertion.
                            idxFound = -2;
                            break;
                        }
                        // if c generalizes exists, then it necessarily
                        // already appears after in current allCultures.
                        // We are done.
                        if( FallbackDepth( exists, c ) >= 0 )
                        {
                            Throw.DebugAssert( allCultures.Contains( c ) );
                            idxFound = -2;
                            break;
                        }
                        // If c is more specific than exists, c must precede exists
                        // with its generalizations up to exists and we are done.
                        idxLenGen = FallbackDepth( c, exists );
                        if( idxLenGen >= 0 )
                        {
                            idxFound = i;
                            break;
                        }
                    }
                    if( idxFound >= 0 )
                    {
                        allCultures.Insert( idxFound, c );
                        foreach( var g in c.Fallbacks.Take( idxLenGen ) )
                        {
                            allCultures.Insert( ++idxFound, g );
                        }
                    }
                    else if( idxFound == -1 )
                    {
                        allCultures.Add( c );
                        allCultures.AddRange( c.Fallbacks );
                    }
                }
                Throw.DebugAssert( allCultures.Count > 0 );
                // This may not be a pure ExtendedCultureInfo ("fr, fr-fr" => "fr-fr, fr" => "fr-fr").
                // The fallback names is registered as an alias of NormalizedCultureInfo name.
                var names = string.Join( ',', allCultures.Select( c => c.Name ) );
                if( !all.TryGetValue( names, out e ) )
                {
                    int id = ComputeId( all, names );
                    e = new ExtendedCultureInfo( allCultures, names, id );
                    all.Add( e.Name, e );
                    all.Add( id, e );
                }
                _all = all;
                return e;
            }

            static int FallbackDepth( NormalizedCultureInfo spec, NormalizedCultureInfo gen )
            {
                return Array.IndexOf( (NormalizedCultureInfo[])spec.Fallbacks, gen );
            }

            static NormalizedCultureInfo OneRegister( Dictionary<object, ExtendedCultureInfo> all, string name )
            {
                Throw.DebugAssert( !string.IsNullOrWhiteSpace( name ) && !name.Contains( ',' ) );
                NormalizedCultureInfo c;
                // Avoid calling CultureInfo.GetCultureInfo if possible.
                if( all.TryGetValue( name, out var exists ) )
                {
                    // This is necessarily a NormalizedCultureInfo because
                    // there is no comma in the name.
                    c = (NormalizedCultureInfo)exists;
                }
                else
                {
                    // Our "name" may differ here from the returned cultureInfo.Name.
                    var cultureInfo = CultureInfo.GetCultureInfo( name );
                    var finalName = cultureInfo.Name.ToLowerInvariant();
                    c = DoRegister( finalName, cultureInfo, all );
                }

                return c;
            }
        }

        static int ComputeId( Dictionary<object,ExtendedCultureInfo> all, string name )
        {
            int id = name.GetDjb2HashCode();
            if( all.TryGetValue( id, out var clash ) )
            {
                var clashes = new List<string>();
                do
                {
                    clashes.Add( clash.Name );
                    ++id;
                }
                while( all.TryGetValue( id, out clash ) );
                GlobalizationIssues.OnIdentifierClash( name, id, clashes );
            }
            return id;
        }
    }
}
