using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CK.Core;

/// <summary>
/// Sealed <see cref="ExtendedCultureInfo"/> that corresponds to an actual <see cref="CultureInfo"/>
/// that is its own <see cref="ExtendedCultureInfo.PrimaryCulture"/> and whose <see cref="ExtendedCultureInfo.Fallbacks"/>
/// are computed from <see cref="CultureInfo.Parent"/> path.
/// </summary>
public sealed partial class NormalizedCultureInfo : ExtendedCultureInfo
{
    readonly CultureInfo _culture;
    // The neutral for Invariant is "en".
    readonly NormalizedCultureInfo _neutral;
    Dictionary<string, PositionalCompositeFormat> _translations;

    /// <summary>
    /// The invariant culture has no fallback and its <see cref="NeutralCulture"/> is the <see cref="CodeDefault"/>.
    /// <para>
    /// It exists almost only to represent the <see cref="CultureInfo.InvariantCulture"/> in the NormalizedCulture world.
    /// </para>
    /// </summary>
    public static readonly NormalizedCultureInfo Invariant;

    /// <summary>
    /// The default culture is bound to the "en" culture by convention.
    /// "en" and Invariant ("") cannot have cached translations.
    /// </summary>
    public static readonly NormalizedCultureInfo CodeDefault;

    /// <summary>
    /// Gets the <see cref="CultureInfo"/>.
    /// </summary>
    public CultureInfo Culture => _culture;

    /// <summary>
    /// Gets the top-most neutral culture (the same base language).
    /// <para>
    /// For the <see cref="Invariant"/> (""), this is the <see cref="CodeDefault"/> ("en"),
    /// for any regular culture, it is this culture if it is a neutral one or the last culture of
    /// the <see cref="ExtendedCultureInfo.Fallbacks"/>.
    /// </para>
    /// </summary>
    public NormalizedCultureInfo NeutralCulture => _neutral;

    /// <summary>
    /// Sets a cached set of resource translation formats from a dictionary of resource name to positional composite
    /// string formats. See <see cref="SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/>.
    /// </summary>
    /// <param name="map">The map.</param>
    /// <returns>0 issues or one or more <see cref="GlobalizationIssues.TranslationFormatError"/> or <see cref="GlobalizationIssues.TranslationDuplicateResource"/>.</returns>
    public IReadOnlyList<GlobalizationIssues.Issue> SetCachedTranslations( IEnumerable<KeyValuePair<string, string>> map )
    {
        return SetCachedTranslations( map.Select( kv => (kv.Key, kv.Value) ) );
    }

    /// <summary>
    /// Sets a cached set of resource translation formats.
    /// This must not be called for <see cref="ExtendedCultureInfo.IsDefault"/> otherwise
    /// an <see cref="InvalidOperationException"/> is thrown.
    /// <para>
    /// When the static gate <see cref="GlobalizationIssues.Track"/> is opened, the returned issues are also emitted and logged.
    /// </para>
    /// <para>
    /// Duplicates can exist in the <paramref name="map"/>: the first resource name is kept, the subsequent
    /// ones are discarded and a <see cref="GlobalizationIssues.TranslationDuplicateResource"/> is emitted (when
    /// the static gate <see cref="GlobalizationIssues.Track"/> is opened).
    /// </para>
    /// </summary>
    /// <param name="map">The map.</param>
    /// <returns>0 issues or one or more <see cref="GlobalizationIssues.TranslationFormatError"/> or <see cref="GlobalizationIssues.TranslationDuplicateResource"/>.</returns>
    public IReadOnlyList<GlobalizationIssues.Issue> SetCachedTranslations( IEnumerable<(string ResName, string Format)> map )
    {
        Throw.CheckState( IsDefault is false );
        List<GlobalizationIssues.Issue>? issues = null;
        var d = new Dictionary<string, PositionalCompositeFormat>();
        foreach( var (n, f) in map )
        {
            if( !PositionalCompositeFormat.TryParse( f, out var p, out var error ) )
            {
                issues = AddIssue( issues, new GlobalizationIssues.TranslationFormatError( this, n, f, error ) );
            }
            else if( !d.TryAdd( n, p ) )
            {
                issues = AddIssue( issues, new GlobalizationIssues.TranslationDuplicateResource( this, n, p, d[n] ) );
            }
        }
        _translations = d;
        return issues ?? new List<GlobalizationIssues.Issue>();

        static List<GlobalizationIssues.Issue> AddIssue( List<GlobalizationIssues.Issue>? issues, GlobalizationIssues.Issue issue )
        {
            issues ??= new List<GlobalizationIssues.Issue>();
            issues.Add( issue );
            if( GlobalizationIssues.Track.IsOpen )
            {
                GlobalizationIssues.OnTranslation( issue );
            }

            return issues;
        }
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

    // Constructor for defaults ("en" first and then Invariant: the Invariant._neutral is "en").
    NormalizedCultureInfo( Dictionary<string, PositionalCompositeFormat> definitelyNoTranslations,
                           string name,
                           int id,
                           CultureInfo c,
                           NormalizedCultureInfo? enForInvariant )
        : base( name, id )
    {
        Throw.DebugAssert( name == "en" || name == "" );
        Throw.DebugAssert( (name == "en") == (enForInvariant == null) );
        _culture = c;
        _neutral = enForInvariant ?? this;
        _translations = definitelyNoTranslations;
    }

    internal NormalizedCultureInfo( CultureInfo culture, string name, int id, ImmutableArray<NormalizedCultureInfo> fallbacks )
        : base( name, id, fallbacks )
    {
        _culture = culture;
        _neutral = fallbacks.Length > 0 ? fallbacks[0]._neutral : this;
        Throw.DebugAssert( _neutral == this || _neutral == fallbacks[^1] );
        _translations = _noTranslations;
    }

    static Dictionary<object, ExtendedCultureInfo> _all;
    static readonly Dictionary<string, PositionalCompositeFormat> _noTranslations;

    static NormalizedCultureInfo()
    {
        _noTranslations = new Dictionary<string, PositionalCompositeFormat>();
        var cInv = CultureInfo.InvariantCulture;
        var cEn = CultureInfo.GetCultureInfo( "en" );
        var en = new NormalizedCultureInfo( _noTranslations, "en", 221277614, cEn, null );
        Invariant = new NormalizedCultureInfo( _noTranslations, string.Empty, 0, cInv, en );
        bool isInvariantModeWithPredefinedOnly = cEn == cInv;

        Throw.DebugAssert( "en".GetDjb2HashCode() == 221277614 );
        _all = new Dictionary<object, ExtendedCultureInfo>()
        {
            { "", Invariant },
            { 0, Invariant },
            { Invariant, Invariant },
            { "en", en },
            { 221277614, en }
        };
        if( !isInvariantModeWithPredefinedOnly )
        {
            _all.Add( cEn, en );
        }
        CodeDefault = en;
    }

    // This is for tests only. Tests use reflection to call this.
    // Even if there should be no differences between Debug and Release builds
    // we also test in Release (no #if DEBUG).
    static void ClearCache()
    {
        GlobalizationIssues.Track.IsOpen = false;
        var all = new Dictionary<object, ExtendedCultureInfo>( _all.Where( IsUnremovable ) );
        _all = all;
        GlobalizationIssues.ClearIssueCache();

        Throw.DebugAssert( "en".GetDjb2HashCode() == 221277614 );

        static bool IsUnremovable( KeyValuePair<object, ExtendedCultureInfo> c )
        {
            return (c.Key is string k && (k == "" || k == "en"))
                   ||
                   (c.Key is CultureInfo i && (i.Name == "" || i.Name == "en"))
                   ||
                   (c.Key is int id && (id == 0 || id == 221277614));
        }
    }

    /// <summary>
    /// Finds or creates a cached normalized culture info from its name.
    /// The name must be a valid BCP47 language tag otherwise a <see cref="CultureNotFoundException"/> is raised.
    /// <para>
    /// This doesn't use <see cref="IsValidCultureName(string)"/>, this relies solely on <see cref="CultureInfo.GetCultureInfo(string)"/>
    /// that validates and normalizes the casing as much as it can. If GetCultureInfo accepts the name, it is fine.
    /// </para>
    /// </summary>
    /// <param name="name">The culture name.</param>
    /// <returns>The culture.</returns>
    public static NormalizedCultureInfo EnsureNormalizedCultureInfo( string name )
    {
        Throw.CheckNotNullArgument( name );
        // Fast path.
        if( _all.TryGetValue( name, out var e ) )
        {
            return e.PrimaryCulture;
        }
        // Let the CultureInfo.GetCultureInfo does its job on the culture name.
        // We don't try to optimize here. Either the name is from our normalization
        // or it is an external name that must be fully handled.
        return EnsureNormalizedCultureInfo( CultureInfo.GetCultureInfo( name ) );
    }

    /// <summary>
    /// Finds or creates a cached normalized culture info from a CultureInfo.
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
    public static NormalizedCultureInfo EnsureNormalizedCultureInfo( CultureInfo cultureInfo )
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

    /// <summary>
    /// Tries to retrieve an already registered <see cref="NormalizedCultureInfo"/> from its <see cref="Name"/>
    /// or returns null.
    /// </summary>
    /// <param name="name">Culture name.</param>
    /// <returns>The culture if found, null otherwise.</returns>
    public static NormalizedCultureInfo? FindNormalizedCultureInfo( string name ) => DoFindExtendedCultureInfo( ref name ) as NormalizedCultureInfo;

    /// <summary>
    /// Basic check of a BCP47 language tag (see https://www.rfc-editor.org/rfc/rfc5646.txt).
    /// <para>
    /// This can be used by external code to avoid creating NormalizedCultureInfo with uncontrolled names,
    /// but eventually <see cref="CultureInfo.GetCultureInfo(string)"/> decides.
    /// </para>
    /// </summary>
    /// <param name="name">A potential culture name.</param>
    /// <returns>True if this name is a vald name, false otherwise.</returns>
    public static bool IsValidCultureName( ReadOnlySpan<char> name )
    {
        return SimpleBCP47Name().IsMatch( name );
    }

    static NormalizedCultureInfo DoRegister( string name, CultureInfo cultureInfo, Dictionary<object, ExtendedCultureInfo> all )
    {
        Throw.DebugAssert( Monitor.IsEntered( _all ) );
        Throw.DebugAssert( name.ToLowerInvariant() == name );
        // This is required here for recursion and as a double check lock when coming
        // from unlocked code.
        if( all.TryGetValue( name, out var exist ) )
        {
            return exist.PrimaryCulture;
        }
        ImmutableArray<NormalizedCultureInfo>.Builder? fallbacks = null;
        var parent = cultureInfo.Parent;
        while( parent != CultureInfo.InvariantCulture )
        {
            fallbacks ??= ImmutableArray.CreateBuilder<NormalizedCultureInfo>();
            fallbacks.Add( DoRegister( parent.Name.ToLowerInvariant(), parent, all ) );
            parent = parent.Parent;
        }
        int id = ComputeId( all, name );
        var newOne = new NormalizedCultureInfo( cultureInfo, name, id, fallbacks != null ? fallbacks.DrainToImmutable() : ImmutableArray<NormalizedCultureInfo>.Empty );
        // Register with the single normalized name.
        all.Add( name, newOne );
        // If the CultureInfo.Name differs ("fr-FR" vs. "fr-fr") also register it.
        if( name != cultureInfo.Name ) all.Add( cultureInfo.Name, newOne );
        // Register the CultureInfo instance itself.
        all.Add( cultureInfo, newOne );
        // Register with id Id.
        all.Add( id, newOne );
        // Register the FullName if it differs from the name.
        // The FullName is not the same as the Name if and only if there are fallbacks.
        Throw.DebugAssert( (newOne.Fallbacks.Length > 0) == (newOne.FullName != newOne.Name) );
        if( newOne.Fallbacks.Length > 0 )
        {
            all.Add( newOne.FullName, newOne );
        }
        return newOne;
    }

    internal static ExtendedCultureInfo? DoFindExtendedCultureInfo( int id ) => _all.GetValueOrDefault( id );

    internal static IEnumerable<ExtendedCultureInfo> GetAll() => _all.Values;

    internal static ExtendedCultureInfo? DoFindExtendedCultureInfo( ref string commaSeparatedNames )
    {
        Throw.CheckNotNullArgument( commaSeparatedNames );
        // Fast path.
        if( !_all.TryGetValue( commaSeparatedNames, out var e ) )
        {
            // Let a chance to a very basic preprocessing.
            commaSeparatedNames = commaSeparatedNames.ToLowerInvariant().Replace( " ", "" );
            _all.TryGetValue( commaSeparatedNames, out e );
        }
        return e;
    }

    internal static ExtendedCultureInfo DoFindBestExtendedCultureInfo( string commaSeparatedNames, NormalizedCultureInfo defaultCulture )
    {
        Throw.CheckNotNullArgument( defaultCulture );

        var best = DoFindExtendedCultureInfo( ref commaSeparatedNames );
        if( best != null ) return best;

        var fullNames = commaSeparatedNames.Split( ',', StringSplitOptions.RemoveEmptyEntries );
        for( int i = 0; i < fullNames.Length; i++ )
        {
            string? one = fullNames[i];
            if( _all.TryGetValue( one, out best ) ) return best;
            var idx = one.LastIndexOf( '-' );
            while( idx > 1 )
            {
                one = one.Substring( 0, idx );
                if( _all.TryGetValue( one, out best ) ) return best;
                idx = one.LastIndexOf( '-' );
            }
        }
        return defaultCulture;
    }

    internal static ExtendedCultureInfo DoEnsureExtendedCultureInfo( string commaSeparatedNames )
    {
        var e = DoFindExtendedCultureInfo( ref commaSeparatedNames );
        if( e != null ) return e;
        var fullNames = commaSeparatedNames.Split( ',', StringSplitOptions.RemoveEmptyEntries );
        // Single name: use the GetNormalizedCultureInfo.
        if( fullNames.Length == 1 )
        {
            return EnsureNormalizedCultureInfo( fullNames[0] );
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
                if( idxFound >= -1 )
                {
                    if( idxFound == -1 )
                    {
                        allCultures.Add( c );
                        allCultures.AddRange( c.Fallbacks );
                    }
                    else
                    {
                        allCultures.Insert( idxFound, c );
                        foreach( var g in c.Fallbacks.Take( idxLenGen ) )
                        {
                            allCultures.Insert( ++idxFound, g );
                        }
                    }
                }
            }
            Throw.DebugAssert( allCultures.Count > 0 );
            // Unfortunately, I did'nt find a clean way to compute the Name in the previous pass,
            // we need another pass. It's easy: culture that are not right after one of their
            // specializations must appear in the final name.
            var previous = allCultures[0];
            var nameBuilder = new StringBuilder( previous.Name );
            foreach( var c in allCultures.Skip( 1 ) )
            {
                if( FallbackDepth( previous, c ) < 0 )
                {
                    nameBuilder.Append( ',' ).Append( c.Name );
                }
                previous = c;
            }
            // This may not be a pure ExtendedCultureInfo ("fr, fr-fr" => "fr-fr, fr" => "fr-fr")
            // so we lookup the dictionary for a normalized culture first (the fallback names is
            // registered as an alias of NormalizedCultureInfo name).
            var names = nameBuilder.ToString();
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

        static int FallbackDepth( NormalizedCultureInfo spec, NormalizedCultureInfo gen ) => spec.Fallbacks.IndexOf( gen );

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

    static int ComputeId( Dictionary<object, ExtendedCultureInfo> all, string name )
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

    [GeneratedRegex( @"^(?!-)[0-9a-zA-Z]{0,8}((-[0-9a-zA-Z]{1,8})+)*$" )]
    private static partial Regex SimpleBCP47Name();
}
