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
    public sealed class CodeString : MCString, ICKSimpleBinarySerializable, ICKVersionedBinarySerializable
    {
        readonly FormattedString _f;
        readonly string _resName;

        /// <summary>
        /// Initializes a <see cref="CodeString"/> with a plain string (no <see cref="Placeholders"/>)
        /// that is bound to the <see cref="CultureInfo.CurrentCulture"/> (that is a thread static property).
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">Optional associated resource name. When null, a "SHA." automatic resource name is computed.</param>
        public CodeString( string plainText, string? resName = null )
            : this( CultureInfo.CurrentCulture, plainText, resName )
        {
        }

        /// <summary>
        /// Initializes a <see cref="CodeString"/> with a plain string (no <see cref="Placeholders"/>).
        /// </summary>
        /// <param name="culture">The culture of this formatted string.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">Optional associated resource name. When null, a "SHA." automatic resource name is computed.</param>
        public CodeString( CultureInfo culture, string plainText, string? resName = null )
        {
            _f = new FormattedString( culture, plainText );
            _resName = resName ?? _f.GetSHA1BasedResName();
        }

        /// <summary>
        /// Initializes a <see cref="CodeString"/> with <see cref="Placeholders"/> using
        /// the thread static <see cref="CultureInfo.CurrentCulture"/> to format the placeholder contents.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">Optional associated resource name.</param>
        public CodeString( [InterpolatedStringHandlerArgument] FormattedStringHandler text, string? resName = null )
        {
            _f = FormattedString.Create( ref text, CultureInfo.CurrentCulture );
            _resName = resName ?? _f.GetSHA1BasedResName();
        }

        /// <summary>
        /// Initializes a <see cref="FormattedString"/> with <see cref="Placeholders"/> using
        /// the provided <paramref name="culture"/>.
        /// </summary>
        /// <param name="culture">The culture used to format placeholders' content.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">Optional associated resource name.</param>
        public CodeString( CultureInfo culture, [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text, string? resName = null )
        {
            _f = FormattedString.Create( ref text, culture );
            _resName = resName ?? _f.GetSHA1BasedResName();
        }

        /// <inheritdoc />
        public override string Text => _f.Text;

        /// <inheritdoc />
        public override string ResName => _resName;

        /// <inheritdoc />
        public override string ContentCulture => _f.Culture.Name;

        /// <summary>
        /// Always "en-US": a <see cref="CodeString"/> format must always be written in american English.
        /// </summary>
        public override string FormatCulture => "en-US";

        /// <inheritdoc />
        public override IReadOnlyList<(int Start, int Length)> Placeholders => _f.Placeholders;

        /// <inheritdoc />
        public override bool IsEmptyFormat => _f.IsEmptyFormat;

        /// <inheritdoc />
        public override string GetFormatString() => _f.GetFormatString();

        /// <inheritdoc />
        public override IEnumerable<ReadOnlyMemory<char>> GetPlaceholderContents() => _f.GetPlaceholderContents();

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
            // 0 versions for both: let's use the more efficient versioned serializable interface.
            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( typeof( FormattedString ) ) == 0 );
            _f = new FormattedString( r, 0 );
            _resName = r.ReadString();
        }

        /// <inheritdoc />
        public void WriteData( ICKBinaryWriter w )
        {
            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( typeof( FormattedString ) ) == 0 );
            _f.WriteData( w );
            w.Write( _resName );
        }
        #endregion
    }
}
