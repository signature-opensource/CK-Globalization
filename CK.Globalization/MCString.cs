using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Captures a translated <see cref="CodeString"/>.
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
            /// The <see cref="FormatCulture"/> is in the default "en-us" and the <see cref="CodeString.ContentCulture"/>
            /// doesn't contain any "en" or English specific fallback.
            /// </summary>
            Awful,

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
            /// The <see cref="FormatCulture"/> is a parent of the <see cref="CodeString.ContentCulture"/>'s primary culture,
            /// or one of its siblings. The latter case implies that the ContentCulture is pure ExtendedCultureInfo: this sibling
            /// explicitly appears in the "user preference list".
            /// </summary>
            Good,

            /// <summary>
            /// The <see cref="FormatCulture"/> perfectly matches the <see cref="CodeString.ContentCulture"/>'s primary culture.
            /// </summary>
            Perfect
        }

        /// <summary>
        /// Gets the empty string: <see cref="FormatCulture"/> is <see cref="NormalizedCultureInfo.Invariant"/>
        /// and bound to <see cref="CodeString.Empty"/>.
        /// </summary>
        public readonly static MCString Empty = new MCString();

        MCString()
        {
            _text = string.Empty;
            _code = CodeString.Empty;
            _format = NormalizedCultureInfo.Invariant;
        }

        MCString( CodeString code )
        {
            _text = code.Text;
            _code = code;
            _format = NormalizedCultureInfo.CodeDefault;
        }

        MCString( string text, CodeString code, NormalizedCultureInfo format )
        {
            _text = text;
            _code = code;
            _format = format;
        }

        /// <summary>
        /// Directly creates a translated string using the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="culture">The culture used to format placeholders' content.</param>
        /// <param name="text">The text.</param>
        /// <param name="resName">Optional associated resource name.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public static MCString Create( CurrentCultureInfo culture,
                                       string text,
                                       string? resName = null,
                                       [CallerFilePath] string? filePath = null,
                                       [CallerLineNumber] int lineNumber = 0 )
        {
            var c = new CodeString( culture.CurrentCulture, text, resName, filePath, lineNumber );
            return culture.TranslationService.Translate( c );
        }

        /// <summary>
        /// Directly creates a translated string using the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="culture">The culture used to format placeholders' content.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">Optional associated resource name.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public static MCString Create( CurrentCultureInfo culture,
                                       [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                       string? resName = null,
                                       [CallerFilePath] string? filePath = null,
                                       [CallerLineNumber] int lineNumber = 0 )
        {
            return Create( culture, ref text, resName, filePath, lineNumber );
        }

        /// <summary>
        /// Directly creates a translated string using the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// Intended for wrappers that capture the interpolated string handler. 
        /// </summary>
        /// <param name="culture">The culture used to format placeholders' content.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">Optional associated resource name.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public static MCString Create( CurrentCultureInfo culture,
                                       ref FormattedStringHandler text,
                                       string? resName = null,
                                       [CallerFilePath] string? filePath = null,
                                       [CallerLineNumber] int lineNumber = 0 )
        {
            var c = CodeString.Create( ref text, culture.CurrentCulture, resName, filePath, lineNumber );
            return culture.TranslationService.Translate( c );
        }

        /// <summary>
        /// Creates a new translated string.
        /// <para>
        /// There's no real reason to create a MCString directly: only a translation service
        /// should call this.
        /// Translation issues are tracked if <see cref="GlobalizationIssues.Track"/> is opened.
        /// </para>
        /// </summary>
        /// <param name="formatCulture">The format's culture.</param>
        /// <param name="format">The format to apply.</param>
        /// <param name="s">The string from source code to translate.</param>
        /// <returns>The translated string.</returns>
        public static MCString Create( NormalizedCultureInfo formatCulture, in PositionalCompositeFormat format, CodeString s )
        {
            var text = s.FormattedString.Format( format );
            var mc = new MCString( text, s, formatCulture );
            if( GlobalizationIssues.Track.IsOpen ) GlobalizationIssues.OnMCStringCreated( in format, mc );
            return mc;
        }

        /// <summary>
        /// Initializes a non translated string. The <see cref="FormatCulture"/> is the
        /// <see cref="NormalizedCultureInfo.CodeDefault"/>: no translation has been done.
        /// Translation issues are tracked if <see cref="GlobalizationIssues.Track"/> is opened.
        /// <para>
        /// There's no real reason to create a MCString directly: only a translation service
        /// should call this.
        /// </para>
        /// </summary>
        /// <param name="code">The string from source code.</param>
        /// <returns>The untranslated string.</returns>
        public static MCString Create( CodeString s )
        {
            var mc = new MCString( s );
            if( GlobalizationIssues.Track.IsOpen ) GlobalizationIssues.OnMCStringCreated( mc );
            return mc;
        }

        /// <summary>
        /// Initializes a non translated string that is a simple wrapper around <paramref name="s"/>
        /// without tracking any translation issue.
        /// </summary>
        /// <param name="code">The string from source code.</param>
        /// <returns>The untranslated string.</returns>
        public static MCString CreateUntracked( CodeString s ) => new MCString( s );    

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
        public Quality TranslationQuality
        {
            get
            {
                var c = _code.ContentCulture;
                var primary = c.PrimaryCulture;
                var f = _format;
                // Perfect: We found the exact culture.
                if( primary == f || (f.IsDefault && primary.IsDefault)) return Quality.Perfect;
                // Good: either we found a parent culture, or a sibling culture. The latter case
                //       implies that c is a pure ExtendedCultureInfo: this sibling has been
                //       explicitly chosen by the user.
                if( primary.HasSameNeutral( f ) ) return Quality.Good;
                // Bad: we found a translation in the preferred list but not in the language (in the sense
                //      of the neutral culture) that has been used to format the placeholder.
                //      At least the user can understand the text.
                if( c is not NormalizedCultureInfo && c.Fallbacks.Any( c => c.HasSameNeutral( f ) ) ) return Quality.Bad;
                // Awful: no match, using en-us Code Default AND the user has no "en" in its preference.
                return Quality.Awful;
            }
        }

        /// <summary>
        /// Gets whether a translation is welcome: the <see cref="TranslationQuality"/> is <see cref="Quality.Bad"/> or <see cref="Quality.Awful"/>.
        /// </summary>
        public bool IsTranslationWelcome
        {
            get
            {
                Throw.DebugAssert( !_code.ContentCulture.PrimaryCulture.HasSameNeutral( _format ) == TranslationQuality < Quality.Good );
                return !_code.ContentCulture.PrimaryCulture.HasSameNeutral( _format );
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
