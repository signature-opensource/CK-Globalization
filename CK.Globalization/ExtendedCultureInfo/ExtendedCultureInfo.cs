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
    ///     lowered invariant <see cref="CultureInfo.Name"/> and the fallbacks are immutable and 
    ///     given by the <see cref="CultureInfo.Parent"/> path.
    ///     </item>
    ///     <item>
    ///     A pure ExtendedCultureInfo is defined by its <see cref="Fallbacks"/>. Its Name is
    ///     "-XXX" where XXX is the Djb2 hash code of the <see cref="FullName"/>
    ///     (the comma separated normalized culture names).
    ///     </item>
    /// </list>
    /// </summary>
    public class ExtendedCultureInfo : IFormatProvider
    {
        readonly string _name;
        readonly string _fullName;
        readonly NormalizedCultureInfo[] _fallbacks;

        internal ExtendedCultureInfo()
        {
            _name = string.Empty;
            _fullName = string.Empty;
            _fallbacks = new NormalizedCultureInfo[] { (NormalizedCultureInfo)this };
        }

        /// <summary>
        /// Constructor for NormalizedCultureInfo, this instance is inserted as the first fallback:
        /// this is the PrimaryCulture.
        /// </summary>
        /// <param name="name">The normalized name.</param>
        /// <param name="fallbacks">Fallbacks.</param>
        internal ExtendedCultureInfo( string name, List<NormalizedCultureInfo> fallbacks )
        {
            Throw.DebugAssert( name.Length > 0 && name[0] != '-' );
            _name = name;
            fallbacks.Insert( 0, (NormalizedCultureInfo)this );
            _fallbacks = fallbacks.ToArray();
            _fullName = name;
        }

        internal ExtendedCultureInfo( List<NormalizedCultureInfo> fallbacks )
        {
            Throw.DebugAssert( fallbacks.Count > 1 );
            _fallbacks = fallbacks.ToArray();
            _fullName = string.Join( ',', fallbacks.Select( c => c.Name ) );
            int hash = _fullName.GetDjb2HashCode();
            _name = '-' + Base64UrlHelper.ToBase64UrlString( MemoryMarshal.CreateReadOnlySpan( ref hash, 1 ).AsBytes() );
        }

        /// <summary>
        /// Gets the normalized, lowered invariant, <see cref="CultureInfo.Name"/> for a <see cref="NormalizedCultureInfo"/>
        /// or an automatic "-xxx" name for a purely extended culture.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Gets the comma separated <see cref="Fallbacks"/> names. This identifies a pure <see cref="ExtendedCultureInfo"/>
        /// as well as a <see cref="NormalizedCultureInfo"/> since a NormalizedCultureInfo's full name is its name. 
        /// </summary>
        public string FullName => _fullName;

        /// <summary>
        /// Gets the primary culture.
        /// </summary>
        public NormalizedCultureInfo PrimaryCulture => _fallbacks[0];

        /// <summary>
        /// Gets the fallbacks. When this is a <see cref="NormalizedCultureInfo"/>, this instance
        /// is the first item.
        /// </summary>
        public IReadOnlyList<NormalizedCultureInfo> Fallbacks => _fallbacks;

        /// <summary>
        /// Gets whether this is the default culture: the <see cref="NormalizedCultureInfo.Invariant"/> or the "en" or "en-us" culture.
        /// </summary>
        public bool IsDefault => _name.Length == 0 || _name == "en" || _name == "en-us";

        object? IFormatProvider.GetFormat( Type? formatType ) => _fallbacks[0].Culture.GetFormat( formatType );
    }
}
