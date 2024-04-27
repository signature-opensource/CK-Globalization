using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace CK.Core
{
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
    /// This is an ubiquitous service: all DI container must be able to provide a scoped instance
    /// (the <see cref="NormalizedCultureInfoUbiquitousServiceDefault"/> singleton is always able to
    /// provide a default value). To avoid another default provider (for this base type), this is also
    /// a IAutoService: the NormalizedCultureInfoUbiquitousServiceDefault also works for it (by covariance)
    /// and any DI provided ExtendedCultureInfo is necessarily a specialized NormalizedCultureInfo.
    /// </para>
    /// </summary>
    [EndpointScopedService( isUbiquitousEndpointInfo:  true )]
    public class ExtendedCultureInfo : IAutoService, IFormatProvider
    {
        readonly string _name;
        readonly NormalizedCultureInfo _primary;
        readonly NormalizedCultureInfo[] _fallbacks;
        readonly string _fullName;
        readonly int _id;

        internal ExtendedCultureInfo( string name, int id, NormalizedCultureInfo? enCulture )
        {
            Throw.DebugAssert( name != null && name.Length == 0 || name == "en" || name == "en-us" );
            Throw.DebugAssert( (name == "en-us") == (enCulture != null && enCulture.Name == "en") );
            _name = name;
            // Exception here: "en-us" has no fallbacks and its FullName is "en-us", not "en-us,en".
            _fullName = name;
            _primary = (NormalizedCultureInfo)this;
            _fallbacks = enCulture == null
                         ? Array.Empty<NormalizedCultureInfo>()
                         : new NormalizedCultureInfo[] { enCulture };
            _id = id;
        }

        /// <summary>
        /// Constructor for NormalizedCultureInfo.
        /// </summary>
        /// <param name="name">The normalized name.</param>
        /// <param name="id">The hash identifier.</param>
        /// <param name="fallbacks">Fallbacks.</param>
        internal ExtendedCultureInfo( string name, int id, NormalizedCultureInfo[] fallbacks )
        {
            Throw.DebugAssert( name.Length > 0 && !name.Contains(',') && !fallbacks.Contains( this ) );
            _name = name;
            _fullName = fallbacks.Length > 0
                            ? string.Join( ',', fallbacks.Select( n => n.Name ).Prepend( name ) )
                            : name;
            _primary = (NormalizedCultureInfo)this;
            _fallbacks = fallbacks;
            _id = id;
        }

        internal ExtendedCultureInfo( List<NormalizedCultureInfo> allCultures, string names, int id )
        {
            Throw.DebugAssert( allCultures.Count > 1 );
            _name = names;
            _fullName = string.Join( ',', allCultures.Select( n => n.Name ) );
            _id = id;
            _primary = allCultures[0];
            _fallbacks = allCultures.Skip( 1 ).ToArray();
        }

        /// <summary>
        /// Gets the shortest comma separated names normalized in lowered invariant that produces the <see cref="Fallbacks"/>.
        /// This is the single <see cref="CultureInfo.Name"/> for a <see cref="NormalizedCultureInfo"/>.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the comma separated names normalized in lowered invariant with the <see cref="PrimaryCulture"/> and <see cref="Fallbacks"/>.
        /// </summary>
        public string FullName => _fullName;

        /// <summary>
        /// Gets the primary culture.
        /// </summary>
        public NormalizedCultureInfo PrimaryCulture => _primary;

        /// <summary>
        /// Gets the fallbacks. This is empty for <see cref="NormalizedCultureInfo.Invariant"/> and any neutral culture (like "fr").
        /// </summary>
        public IReadOnlyList<NormalizedCultureInfo> Fallbacks => _fallbacks;

        /// <summary>
        /// Gets whether this is a default culture: the <see cref="NormalizedCultureInfo.Invariant"/>, "en" or "en-us" culture.
        /// </summary>
        public bool IsDefault => _name.Length == 0 || ReferenceEquals( _name, "en" ) || ReferenceEquals( _name, "en-us" );

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
        public static ExtendedCultureInfo GetExtendedCultureInfo( string commaSeparatedNames ) => NormalizedCultureInfo.DoGetExtendedCultureInfo( commaSeparatedNames );

        /// <summary>
        /// Tries to retrieve an already registered <see cref="ExtendedCultureInfo"/> from its <see cref="ExtendedCultureInfo.Name"/>
        /// or returns null.
        /// </summary>
        /// <param name="commaSeparatedNames">Comma separated culture names.</param>
        /// <returns>The culture if found, null otherwise.</returns>
        public static ExtendedCultureInfo? FindExtendedCultureInfo( string commaSeparatedNames ) => NormalizedCultureInfo.DoFindExtendedCultureInfo( ref commaSeparatedNames );

        /// <summary>
        /// Tries to retrieve an already registered <see cref="ExtendedCultureInfo"/> from its identifier (the <see cref="ExtendedCultureInfo.Id"/>)
        /// or returns null.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The culture if found, null otherwise.</returns>
        public static ExtendedCultureInfo? FindExtendedCultureInfo( int id ) => NormalizedCultureInfo.DoFindExtendedCultureInfo( id );

        /// <summary>
        /// Overridden to return the <see cref="Name"/>.
        /// </summary>
        /// <returns>This culture's Name.</returns>
        public override string ToString() => _name;

        object? IFormatProvider.GetFormat( Type? formatType ) => _primary.Culture.GetFormat( formatType );
    }
}
