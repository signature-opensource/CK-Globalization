using System.Diagnostics;
using System.Linq;

namespace CK.Core
{
    /// <summary>
    /// Cpatures a translated <see cref="CodeString"/>.
    /// </summary>
    public sealed class MCString
    {
        readonly string _text;
        readonly CodeString _code;
        readonly NormalizedCultureInfo _format;

        /// <summary>
        /// Qualifies the translation from the <see cref="CodeString"/> to <see cref="FormatCulture"/>.
        /// </summary>
        public enum Quality
        {
            /// <summary>
            /// The <see cref="FormatCulture"/> perfectly matches the <see cref="CodeString.ContentCulture"/>'s primary culture.
            /// </summary>
            Perfect,

            /// <summary>
            /// The <see cref="FormatCulture"/> is a parent of the <see cref="CodeString.ContentCulture"/>'s primary culture,
            /// or one of its siblings. The latter case implies that the ContentCulture is pure ExtendedCultureInfo: this sibling
            /// explicitly appears in the "user preference list".
            /// </summary>
            Good,

            /// <summary>
            /// The <see cref="FormatCulture"/> and the <see cref="CodeString.ContentCulture"/>'s primary culture
            /// are unrelated.
            /// <para>
            /// This only applies when the ContentCulture is a pure <see cref="ExtendedCultureInfo"/>
            /// (a "user preference list"): the FormatCulture is one the fallbacks, but not in the primary culture group.
            /// We found a translation in the preferred list but not in the language (in the sense of the neutral culture) that
            /// has been used to format the placeholder. At least the user can understand the text.
            /// </para>
            /// </summary>
            Bad,

            /// <summary>
            /// The <see cref="FormatCulture"/> is in the default "en-us" and the <see cref="CodeString.ContentCulture"/>
            /// doesn't contain any "en" or English specific fallback.
            /// </summary>
            Awful
        }

        /// <summary>
        /// Initializes a non translated string. The <see cref="FormatCulture"/> is the
        /// <see cref="NormalizedCultureInfo.CodeDefault"/>: no translation has been done.
        /// </summary>
        /// <param name="code">The string from source code.</param>
        public MCString( CodeString code )
        {
            _text = code.Text;
            _code = code;
            _format = NormalizedCultureInfo.CodeDefault;
        }

        /// <summary>
        /// Initializes a translated string.
        /// </summary>
        /// <param name="text">The translated text.</param>
        /// <param name="code">The string from source code.</param>
        /// <param name="format">The format's culture.</param>
        public MCString( string text, CodeString code, NormalizedCultureInfo format )
        {
            _text = text;
            _code = code;
            _format = format;
        }

        /// <summary>
        /// Gets the translated text.
        /// </summary>
        public string Text => _text;

        /// <summary>
        /// Gets the original string from source code.
        /// </summary>
        public CodeString CodeString => _code;

        /// <summary>
        /// Gets the format culture.
        /// </summary>
        public NormalizedCultureInfo FormatCulture => _format;

        /// <summary>
        /// Implicit cast into string: <see cref="Text"/>.
        /// </summary>
        /// <param name="f">This Text.</param>
        public static implicit operator string( MCString f ) => f.Text;

        /// <summary>
        /// Gets the translation quality. See <see cref="Quality"/>.
        /// </summary>
        public Quality TranslationLevel
        {
            get
            {
                var c = _code.ContentCulture;
                var f = _format;
                // Perfect: We found the exact culture.
                if( c.PrimaryCulture == f ) return Quality.Perfect;
                // Good: either we found a parent culture, or a sibling culture. The latter case
                //       implies that c is a pure ExtendedCultureInfo: this sibling has been
                //       explicitly chosen by the user.
                if( c.PrimaryCulture.Fallbacks.Contains( f )
                    || c.PrimaryCulture.Fallbacks.Intersect( f.Fallbacks ).Any() ) return Quality.Good;
                // Bad: we found a translation in the preferred list but not in the language (in the sense
                //      of the neutral culture) that has been used to format the placeholder.
                //      At least the user can understand the text.
                if( c is not NormalizedCultureInfo && c.Fallbacks.Contains( f ) ) return Quality.Bad;
                // Awful: no match, using en-us Code Default AND the user has no "en" in its preference.
                return Quality.Awful;
            }
        }

        /// <summary>
        /// Overridden to return this <see cref="Text"/>.
        /// </summary>
        /// <returns>This text.</returns>
        public override string ToString() => _text;

        #region Serialization
        /// <summary>
        /// Simple deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        public MCString( ICKBinaryReader r )
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
        public MCString( ICKBinaryReader r, int version )
        {
            Throw.CheckData( version == 0 );
            _code = new CodeString( r, 0 );
            _text = r.ReadString();
            _format = NormalizedCultureInfo.GetNormalizedCultureInfo( r.ReadString() );
        }

        /// <inheritdoc />
        public void WriteData( ICKBinaryWriter w )
        {
            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( typeof( FormattedString ) ) == 0 );
            // 0 version also for CodeString: let's use the more efficient versioned serializable interface.
            // This is called by tests. The 2 versions should always be aligned.
            Throw.DebugAssert( SerializationVersionAttribute.GetRequiredVersion( typeof( CodeString ) ) == 0 );
            _code.WriteData( w );
            w.Write( _text );
            w.Write( _format.Name );
        }
        #endregion

    }
}
