using System.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Cpatures a translated <see cref="CodeString"/>.
    /// </summary>
    public sealed class TransString
    {
        readonly string _text;
        readonly CodeString _code;
        readonly NormalizedCultureInfo _format;

        /// <summary>
        /// Initializes a non translated string. The <see cref="FormatCulture"/> is the
        /// <see cref="NormalizedCultureInfo.CodeDefault"/>: no translation has been done.
        /// </summary>
        /// <param name="code">The string from source code.</param>
        public TransString( CodeString code )
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
        public TransString( string text, CodeString code, NormalizedCultureInfo format )
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
        public static implicit operator string( TransString f ) => f.Text;

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
        public TransString( ICKBinaryReader r )
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
        public TransString( ICKBinaryReader r, int version )
        {
            Throw.CheckData( version == 0 );
            _code = new CodeString( r, 0 );
            _text = r.ReadString();
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
        }
        #endregion

    }
}
