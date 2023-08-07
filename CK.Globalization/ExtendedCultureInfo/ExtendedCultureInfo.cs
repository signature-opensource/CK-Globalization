using CommunityToolkit.HighPerformance;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

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
    /// </summary>
    public class ExtendedCultureInfo : IFormatProvider
    {
        readonly string _name;
        readonly NormalizedCultureInfo _primary;
        readonly NormalizedCultureInfo[] _fallbacks;
        readonly int _id;

        internal ExtendedCultureInfo( string name, int id )
        {
            Throw.DebugAssert( name != null && name.Length == 0 || name == "en" || name == "en-us" );
            _name = name;
            _primary = (NormalizedCultureInfo)this;
            _fallbacks = Array.Empty<NormalizedCultureInfo>();
            _id = id;
        }

        /// <summary>
        /// Constructor for NormalizedCultureInfo.
        /// </summary>
        /// <param name="name">The normalized name.</param>
        /// <param name="fallbacks">Fallbacks.</param>
        internal ExtendedCultureInfo( string name, int id, NormalizedCultureInfo[] fallbacks )
        {
            Throw.DebugAssert( name.Length > 0 && !name.Contains(',') && !fallbacks.Contains( this ) );
            _name = name;
            _primary = (NormalizedCultureInfo)this;
            _fallbacks = fallbacks;
            _id = id;
        }

        internal ExtendedCultureInfo( List<NormalizedCultureInfo> allCultures, string names, int id )
        {
            Throw.DebugAssert( allCultures.Count > 1 );
            _name = names;
            _id = id;
            _primary = allCultures[0];
            _fallbacks = allCultures.Skip( 1 ).ToArray();
        }

        /// <summary>
        /// Gets the normalized, lowered invariant, <see cref="CultureInfo.Name"/> for a <see cref="NormalizedCultureInfo"/>
        /// or the comma separated names of <see cref="Fallbacks"/> for a pure extended culture.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the primary culture.
        /// </summary>
        public NormalizedCultureInfo PrimaryCulture => _primary;

        /// <summary>
        /// Gets the fallbacks. This is empty for <see cref="NormalizedCultureInfo.Invariant"/>, any neutral culture (like "fr")
        /// and the <see cref="NormalizedCultureInfo.CodeDefault"/>.
        /// </summary>
        public IReadOnlyList<NormalizedCultureInfo> Fallbacks => _fallbacks;

        /// <summary>
        /// Gets whether this is the default culture: the <see cref="NormalizedCultureInfo.Invariant"/>, "en" or "en-us" culture.
        /// </summary>
        public bool IsDefault => _name.Length == 0 || ReferenceEquals( _name, "en" ) || ReferenceEquals( _name, "en-us" );

        /// <summary>
        /// Gets a unique identifier for this culture.
        /// It is the <see cref="StringExtensions.GetDjb2HashCode(string)"/> hash value.
        /// </summary>
        public int Id => _id;

        /// <summary>
        /// Gets a <see cref="ExtendedCultureInfo"/> for a comma separated fallback names.
        /// If normalization results in a single <see cref="NormalizedCultureInfo"/>, it is returned.
        /// </summary>
        /// <param name="commaSeparatedNames">Comma separated culture names.</param>
        /// <returns>The extended culture.</returns>
        public static ExtendedCultureInfo GetExtendedCultureInfo( string commaSeparatedNames ) => NormalizedCultureInfo.DoGetExtendedCultureInfo( commaSeparatedNames );

        /// <summary>
        /// Tries to retrive an already registered <see cref="ExtendedCultureInfo"/> from its identifier (the <see cref="ExtendedCultureInfo.Id"/>)
        /// or returns null.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The culture if found, null otherwise.</returns>
        public static ExtendedCultureInfo? GetExtendedCultureInfo( int id ) => NormalizedCultureInfo.DoGetExtendedCultureInfo( id );

        /// <summary>
        /// Overridden to return the <see cref="Name"/>.
        /// </summary>
        /// <returns>This culture's Name.</returns>
        public override string ToString() => _name;

        object? IFormatProvider.GetFormat( Type? formatType ) => _primary.Culture.GetFormat( formatType );
    }
}
