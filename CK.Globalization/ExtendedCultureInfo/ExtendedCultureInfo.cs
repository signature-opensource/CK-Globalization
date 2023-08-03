using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
    ///     "-XXXXX" where XXXXX is the Djb2 hash code of the <see cref="SerializationName"/>
    ///     (the comma separated normalized culture names).
    ///     </item>
    /// </list>
    /// </summary>
    public class ExtendedCultureInfo
    {
        readonly string _name;
        NormalizedCultureInfo[] _fallbacks;

        internal ExtendedCultureInfo()
        {
            _name = string.Empty;
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
        }

        /// <summary>
        /// Gets the normalized, lowered invariant, <see cref="CultureInfo.Name"/> for a <see cref="NormalizedCultureInfo"/>
        /// or an automatic "-xxx" name for a purely extended culture.
        /// </summary>
        public string Name => _name;

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

        /// <summary>
        /// Sets this <see cref="Fallbacks"/>.
        /// <list type="bullet">
        ///     <item>
        ///     When this is a <see cref="NormalizedCultureInfo"/>, the invariant that the <see cref="PrimaryCulture"/>
        ///     is this instance is automatically enforced. The <paramref name="fallbacks"/> can be empty.
        ///     </item>
        ///     <item>
        ///     When this is a <see cref="ExtendedCultureInfo"/> the <paramref name="fallbacks"/> must not be empty
        ///     or an <see cref="ArgumentException"/> is thrown.
        ///     </item>
        /// </list>
        /// </summary>
        /// <param name="fallbacks">The new fallbacks for this culture.</param>
        public void SetFallbacks( IEnumerable<NormalizedCultureInfo> fallbacks )
        {
            Throw.CheckNotNullArgument( fallbacks );
            var f = fallbacks.ToList();
            if( this is NormalizedCultureInfo n )
            {
                f.Remove( n );
                if( f.Count == 0 || f[0] != n ) f.Insert( 0, n );
            }
            else
            {
                Throw.CheckNotNullOrEmptyArgument( fallbacks );
            }
            _fallbacks = f.ToArray();
        }

    }
}
