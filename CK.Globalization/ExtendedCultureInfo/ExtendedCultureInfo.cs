using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Generalizes <see cref="NormalizedCultureInfo"/> by supporting fallbacks.
/// This is either:
/// <list type="bullet">
///     <item>
///     A <see cref="NormalizedCultureInfo"/>: the <see cref="Name"/> is the
///     lowered invariant <see cref="CultureInfo.Name"/> and the fallbacks are given
///     by the <see cref="CultureInfo.Parent"/> path.
///     </item>
///     <item>
///     A pure ExtendedCultureInfo is defined by its <see cref="Fallbacks"/>. Its Name is
///     the comma separated names of its fallback cultures.
///     </item>
/// </list>
/// <para>
/// This is an ambient service: all DI container can provide a scoped instance
/// (the <see cref="NormalizedCultureInfoAmbientServiceDefault"/> singleton is always able to
/// provide a default value).
/// </para>
/// </summary>
public class ExtendedCultureInfo : IAmbientAutoService, IFormatProvider
{
    readonly string _name;
    readonly NormalizedCultureInfo _primary;
    readonly ImmutableArray<NormalizedCultureInfo> _fallbacks;
    readonly string _fullName;
    readonly int _id;

    /// <summary>
    /// Constructor for Invariant (Name="") and "en" only.
    /// </summary>
    /// <param name="name">"" or "en".</param>
    /// <param name="id">Computed identifier.</param>
    internal ExtendedCultureInfo( string name, int id )
    {
        Throw.DebugAssert( name != null && name.Length == 0 || name == "en" );
        _name = name;
        _fullName = name;
        _primary = (NormalizedCultureInfo)this;
        _fallbacks = ImmutableArray<NormalizedCultureInfo>.Empty;
        _id = id;
    }

    /// <summary>
    /// Constructor for NormalizedCultureInfo.
    /// </summary>
    /// <param name="name">The normalized name.</param>
    /// <param name="id">The hash identifier.</param>
    /// <param name="fallbacks">Fallbacks.</param>
    internal ExtendedCultureInfo( string name, int id, ImmutableArray<NormalizedCultureInfo> fallbacks )
    {
        Throw.DebugAssert( name.Length > 0 && !name.Contains( ',' ) && !fallbacks.Contains( this ) );
        Throw.DebugAssert( !fallbacks.Contains( this ) && !fallbacks.Contains( NormalizedCultureInfo.Invariant ) );
        _name = name;
        _fullName = fallbacks.Length > 0
                        ? string.Join( ',', fallbacks.Select( n => n.Name ).Prepend( name ) )
                        : name;
        _primary = (NormalizedCultureInfo)this;
        _fallbacks = fallbacks;
        _id = id;
    }

    /// <summary>
    /// Constructor for ExtendedCultureInfo.
    /// </summary>
    /// <param name="allCultures">Primary followed by Fallbacks.</param>
    /// <param name="names">Heads of the preference list (like "fr-fr, de-de")</param>
    /// <param name="id">Computed identifier.</param>
    internal ExtendedCultureInfo( List<NormalizedCultureInfo> allCultures, string names, int id )
    {
        Throw.DebugAssert( allCultures.Count > 1 );
        _name = names;
        _fullName = string.Join( ',', allCultures.Select( n => n.Name ) );
        _id = id;
        _primary = allCultures[0];
        _fallbacks = allCultures.Skip( 1 ).ToImmutableArray();
    }

    /// <summary>
    /// Gets the shortest comma separated names normalized in lowered invariant that produces the <see cref="Fallbacks"/>.
    /// This is the single <see cref="CultureInfo.Name"/> for a <see cref="NormalizedCultureInfo"/>.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the comma separated names normalized in lowered invariant with the <see cref="PrimaryCulture"/> first
    /// followed by the <see cref="Fallbacks"/>.
    /// </summary>
    public string FullName => _fullName;

    /// <summary>
    /// Gets the primary, preferred, culture.
    /// For a ExtendedCultureInfo this is the first prefered culture, for a NormalizedCultureInfo it is itself.
    /// </summary>
    public NormalizedCultureInfo PrimaryCulture => _primary;

    /// <summary>
    /// Gets the fallbacks.
    /// The <see cref="NormalizedCultureInfo.Invariant"/> never appears in this list: this is empty
    /// for the Invariant and any neutral culture (like "fr").
    /// <para>
    /// For <see cref="NormalizedCultureInfo"/>, this is the ordered list of <see cref="CultureInfo.Parent"/> cultures from the
    /// most specific to the most general one (the <see cref="NormalizedCultureInfo.NeutralCulture"/>).
    /// </para>
    /// </summary>
    public ImmutableArray<NormalizedCultureInfo> Fallbacks => _fallbacks;

    /// <summary>
    /// Gets whether this is a default culture: the <see cref="NormalizedCultureInfo.Invariant"/> ("")
    /// or the <see cref="NormalizedCultureInfo.CodeDefault"/> "en" culture.
    /// </summary>
    public bool IsDefault => _name.Length == 0 || ReferenceEquals( _name, "en" );

    /// <summary>
    /// Gets a unique identifier for this culture.
    /// It is the <see cref="StringExtensions.GetDjb2HashCode(string)"/> hash value.
    /// </summary>
    public int Id => _id;

    /// <summary>
    /// Finds or creates a <see cref="ExtendedCultureInfo"/> for a comma separated fallback names.
    /// If normalization results in a single <see cref="NormalizedCultureInfo"/>, it is returned.
    /// </summary>
    /// <param name="commaSeparatedNames">Comma separated culture names.</param>
    /// <returns>The extended culture.</returns>
    public static ExtendedCultureInfo EnsureExtendedCultureInfo( string commaSeparatedNames ) => NormalizedCultureInfo.DoEnsureExtendedCultureInfo( commaSeparatedNames );

    /// <summary>
    /// Tries to retrieve an already registered <see cref="ExtendedCultureInfo"/> from its <see cref="Name"/>
    /// or returns null.
    /// </summary>
    /// <param name="commaSeparatedNames">Comma separated culture names.</param>
    /// <returns>The culture if found, null otherwise.</returns>
    public static ExtendedCultureInfo? FindExtendedCultureInfo( string commaSeparatedNames ) => NormalizedCultureInfo.DoFindExtendedCultureInfo( ref commaSeparatedNames );

    /// <summary>
    /// Finds the best <see cref="ExtendedCultureInfo"/> from a comma separated list of culture names.
    /// <para>
    /// Currently, this ony returns NormalizedCultureInfo but this can be enhanced in the future.
    /// The order of the entries matters: "fr-CA, es-ES" with existing "fr-fr" and "es-es" cultures will select "fr".
    /// </para>
    /// </summary>
    /// <param name="commaSeparatedNames">Comma separated culture names.</param>
    /// <param name="defaultCulture">Ultimate default to consider.</param>
    /// <returns>The best existing culture.</returns>
    public static ExtendedCultureInfo FindBestExtendedCultureInfo( string commaSeparatedNames, NormalizedCultureInfo defaultCulture ) => NormalizedCultureInfo.DoFindBestExtendedCultureInfo( commaSeparatedNames, defaultCulture );

    /// <summary>
    /// Tries to retrieve an already registered <see cref="ExtendedCultureInfo"/> from its identifier (the <see cref="ExtendedCultureInfo.Id"/>)
    /// or returns null.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The culture if found, null otherwise.</returns>
    public static ExtendedCultureInfo? FindExtendedCultureInfo( int id ) => NormalizedCultureInfo.DoFindExtendedCultureInfo( id );

    /// <summary>
    /// Gets all the registered cultures.
    /// </summary>
    public static IEnumerable<ExtendedCultureInfo> All => NormalizedCultureInfo.GetAll();

    /// <summary>
    /// Overridden to return the <see cref="Name"/>.
    /// </summary>
    /// <returns>This culture's Name.</returns>
    public override string ToString() => _name;

    object? IFormatProvider.GetFormat( Type? formatType ) => _primary.Culture.GetFormat( formatType );
}
