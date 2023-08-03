using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Code implementation of <see cref="MCString"/> based on interpolated strings (see <see cref="FormattedString"/>).
    /// The <see cref="FormatCulture"/> is always "en-US": format strings must always be written in american English.
    /// </summary>
    public sealed class CodeString : ICKSimpleBinarySerializable, ICKVersionedBinarySerializable
    {
        readonly FormattedString _f;
        readonly string _resName;

        /// <summary>
        /// Initializes a <see cref="CodeString"/> with a plain string (no <see cref="Placeholders"/>)
        /// that is bound to the <see cref="NormalizedCultureInfo.Current"/>.
        /// <para>
        /// This should be avoided: the culture should be provided explicitly.
        /// </para>
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">Optional associated resource name. When null, a "SHA." automatic resource name is computed.</param>
        public CodeString( string plainText, string? resName = null )
            : this( NormalizedCultureInfo.Current, plainText, resName )
        {
        }

        /// <summary>
        /// Initializes a <see cref="CodeString"/> with a plain string (no <see cref="Placeholders"/>).
        /// <para>
        /// The <see cref="ExtendedCultureInfo.PrimaryCulture"/> is used to format the placeholders, but this
        /// captures the whole <see cref="ExtendedCultureInfo.Fallbacks"/> so that the best translation of the
        /// "enveloppe" can be found when the <paramref name="culture"/> is a "user preference" (a mere ExtendedCultureInfo) rather
        /// than a specialized <see cref="NormalizedCultureInfo"/> with is default fallbacks.
        /// </para>
        /// </summary>
        /// <param name="culture">The culture of this formatted string.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">Optional associated resource name. When null, a "SHA." automatic resource name is computed.</param>
        public CodeString( NormalizedCultureInfo culture, string plainText, string? resName = null )
        {
            _f = new FormattedString( culture, plainText );
            _resName = resName ?? _f.GetSHA1BasedResName();
        }

        /// <summary>
        /// Initializes a <see cref="CodeString"/> with <see cref="Placeholders"/> using
        /// the <see cref="NormalizedCultureInfo.Current"/> to format the placeholder contents.
        /// <para>
        /// This should be avoided: the culture should be provided explicitly.
        /// </para>
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">Optional associated resource name.</param>
        public CodeString( [InterpolatedStringHandlerArgument] FormattedStringHandler text, string? resName = null )
        {
            _f = FormattedString.Create( ref text, NormalizedCultureInfo.Current );
            _resName = resName ?? _f.GetSHA1BasedResName();
        }

        /// <summary>
        /// Initializes a <see cref="FormattedString"/> with <see cref="Placeholders"/> using
        /// the provided <paramref name="culture"/>.
        /// <para>
        /// The <see cref="ExtendedCultureInfo.PrimaryCulture"/> is used to format the placeholders, but this
        /// captures the whole <see cref="ExtendedCultureInfo.Fallbacks"/> so that the best translation of the
        /// "enveloppe" can be found when the <paramref name="culture"/> is a "user preference" (a mere ExtendedCultureInfo) rather
        /// than a specialized <see cref="NormalizedCultureInfo"/> with is default fallbacks.
        /// </para>
        /// </summary>
        /// <param name="culture">The culture used to format placeholders' content.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">Optional associated resource name.</param>
        public CodeString( NormalizedCultureInfo culture, [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text, string? resName = null )
        {
            _f = FormattedString.Create( ref text, culture );
            _resName = resName ?? _f.GetSHA1BasedResName();
        }

        /// <summary>
        /// Gets the formatted text.
        /// </summary>
        public string Text => _f.Text;

        /// <summary>
        /// Gets the resource name that identifies this string.
        /// <para>
        /// The prefix "SHA." is reserved: it is the prefix for Base64Url SHA1 of the <see cref="GetFormatString"/>
        /// used when no resource name is provided.
        /// </para>
        /// </summary>
        public string ResName => _resName;

        /// <summary>
        /// Gets the culture that has been used to format the <see cref="Placeholders"/>.
        /// </summary>
        public ExtendedCultureInfo ContentCulture => _f.Culture;

        /// <summary>
        /// Gets the placeholders' occurrence in this <see cref="Text"/>.
        /// </summary>
        public IReadOnlyList<(int Start, int Length)> Placeholders => _f.Placeholders;

        /// <summary>
        /// Gets the placeholders' content.
        /// </summary>
        /// <returns>The <see cref="ContentCulture"/> formatted contents for each placeholders.</returns>
        public IEnumerable<ReadOnlyMemory<char>> GetPlaceholderContents() => _f.GetPlaceholderContents();

        /// <summary>
        /// Gets whether this <see cref="GetFormatString"/> is empty: <see cref="Text"/> is empty and there is no <see cref="Placeholders"/>.
        /// <para>
        /// Note that:
        /// <list type="bullet">
        ///   <item>
        ///     Text can be empty and there may be one or more Placeholders. For instance, the format string <c>{0}{1}</c>
        ///     with 2 empty placeholders content leads to an empty Text but this doesn't mean that this <see cref="MCString"/> is empty.
        ///   </item>
        ///   <item>
        ///     When this is true, <see cref="ContentCulture"/> can be any culture, not necessarily the <see cref="NormalizedCultureInfo.Invariant"/>).
        ///   </item>
        /// </list>
        /// </para>
        /// </summary>
        public bool IsEmptyFormat => _f.IsEmptyFormat;

        /// <inheritdoc cref="FormattedString.GetFormatString"/>
        public string GetFormatString() => _f.GetFormatString();

        #region Serialization
        /// <summary>
        /// Simple deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        public CodeString( ICKBinaryReader r )
            : this( r, r.ReadNonNegativeSmallInt32() )
        {
        }

        /// <inheritdoc />
        public void Write( ICKBinaryWriter w )
        {
            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( GetType() ) == 0 );
            w.WriteNonNegativeSmallInt32( 0 );
            WriteData( w );
        }

        /// <summary>
        /// Versioned deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        /// <param name="version">The saved version number.</param>
        public CodeString( ICKBinaryReader r, int version )
        {
            Throw.CheckData( version == 0 );
            _f = new FormattedString( r, 0 );
            _resName = r.ReadString();
        }

        /// <inheritdoc />
        public void WriteData( ICKBinaryWriter w )
        {
            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( typeof( FormattedString ) ) == 0 );
            // 0 version also for FormattedString: let's use the more efficient versioned serializable interface.
            // This is called by tests. The 2 versions should always be aligned.
            Throw.DebugAssert( SerializationVersionAttribute.GetRequiredVersion( typeof( FormattedString ) ) == 0 );
            _f.WriteData( w );
            w.Write( _resName );
        }
        #endregion
    }
}
