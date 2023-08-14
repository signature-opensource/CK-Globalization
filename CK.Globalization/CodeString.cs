using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Core
{
    /// <summary>
    /// Code implementation of <see cref="MCString"/> based on interpolated strings (see <see cref="FormattedString"/>).
    /// The <see cref="FormatCulture"/> is always "en-US": format strings must always be written in american English.
    /// </summary>
    [SerializationVersion(0)]
    public sealed class CodeString : ICKSimpleBinarySerializable, ICKVersionedBinarySerializable
    {
        readonly FormattedString _f;
        readonly string _resName;

        CodeString()
        {
            // FormattedString.Empy (empty text, no placeholders and Invariant).
            _resName = String.Empty;
        }

        /// <summary>
        /// Gets the empty code string: empty <see cref="ResName"/>, empty text, no placeholders and <see cref="NormalizedCultureInfo.Invariant"/>.
        /// </summary>
        public readonly static CodeString Empty = new CodeString();

        /// <summary>
        /// Initializes a <see cref="CodeString"/> with a plain string (no <see cref="Placeholders"/>)
        /// that is bound to the <see cref="NormalizedCultureInfo.Current"/>.
        /// <para>
        /// This should be avoided: the culture should be provided explicitly.
        /// </para>
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">Optional associated resource name. When null, a "SHA." automatic resource name is computed.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public CodeString( string plainText,
                           string? resName = null,
                           [CallerFilePath]string? filePath = null,
                           [CallerLineNumber]int lineNumber = 0 )
            : this( NormalizedCultureInfo.Current, plainText, resName, filePath, lineNumber )
        {
        }

        /// <summary>
        /// Initializes a <see cref="CodeString"/> with a plain string (no <see cref="Placeholders"/>).
        /// <para>
        /// The <see cref="ExtendedCultureInfo.PrimaryCulture"/> is used to format the placeholders, but this
        /// captures the whole <see cref="ExtendedCultureInfo.Fallbacks"/> so that the best translation of the
        /// "enveloppe" can be found when the <paramref name="culture"/> is a "user preference" (a mere ExtendedCultureInfo) rather
        /// than a specialized <see cref="NormalizedCultureInfo"/> with its default fallbacks.
        /// </para>
        /// </summary>
        /// <param name="culture">The culture of this formatted string.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">Optional associated resource name. When null, a "SHA." automatic resource name is computed.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public CodeString( ExtendedCultureInfo culture,
                           string plainText,
                           string? resName = null,
                           [CallerFilePath] string? filePath = null,
                           [CallerLineNumber] int lineNumber = 0 )
        {
            _f = new FormattedString( culture, plainText );
            _resName = resName ?? _f.GetSHA1BasedResName();
            if( GlobalizationIssues.Track.IsOpen ) GlobalizationIssues.OnCodeStringCreated( this, filePath, lineNumber );
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
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public CodeString( [InterpolatedStringHandlerArgument] FormattedStringHandler text,
                           string? resName = null,
                           [CallerFilePath] string? filePath = null,
                           [CallerLineNumber] int lineNumber = 0 )
            : this( NormalizedCultureInfo.Current, ref text, resName, filePath, lineNumber )
        {
        }

        /// <summary>
        /// Initializes a <see cref="CodeString"/> with <see cref="Placeholders"/> using
        /// the provided <paramref name="culture"/>.
        /// <para>
        /// The <see cref="ExtendedCultureInfo.PrimaryCulture"/> is used to format the placeholders, but this
        /// captures the whole <see cref="ExtendedCultureInfo.Fallbacks"/> so that the best translation of the
        /// "enveloppe" can be found when the <paramref name="culture"/> is a "user preference" (a mere ExtendedCultureInfo) rather
        /// than a specialized <see cref="NormalizedCultureInfo"/> with its default fallbacks.
        /// </para>
        /// </summary>
        /// <param name="culture">The culture used to format placeholders' content.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">Optional associated resource name.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public CodeString( ExtendedCultureInfo culture,
                           [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                           string? resName = null,
                           [CallerFilePath] string? filePath = null,
                           [CallerLineNumber] int lineNumber = 0 )
            : this( culture, ref text, resName, filePath, lineNumber )
        {
        }

        CodeString( ExtendedCultureInfo culture,
                    ref FormattedStringHandler text,
                    string? resName,
                    string? filePath,
                    int lineNumber )
        {
            _f = FormattedString.Create( ref text, culture );
            _resName = resName ?? _f.GetSHA1BasedResName();
            if( GlobalizationIssues.Track.IsOpen ) GlobalizationIssues.OnCodeStringCreated( this, filePath, lineNumber );
        }

        CodeString( in FormattedString formattedString, string resName )
        {
            _f = formattedString;
            _resName = resName;
        }

        /// <summary>
        /// Gets the formatted text.
        /// </summary>
        public string Text => _f.Text;

        /// <summary>
        /// Gets the resource name that identifies this string.
        /// This is the empty string for the <see cref="Empty"/> string.
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
        public ReadOnlyMemory<char>[] GetPlaceholderContents() => _f.GetPlaceholderContents();

        /// <summary>
        /// Gets the formatted string.
        /// </summary>
        public FormattedString FormattedString => _f;

        /// <summary>
        /// Intended for wrappers that capture the interpolated string handler.
        /// </summary>
        /// <param name="handler">The interpolated string handler.</param>
        /// <param name="culture">The culture.</param>
        /// <param name="resName">Optional associated resource name.</param>
        /// <param name="filePath">Source file path.</param>
        /// <param name="lineNumber">Source line number.</param>
        /// <returns>A new code string.</returns>
        public static CodeString Create( ref FormattedStringHandler handler,
                                         ExtendedCultureInfo culture,
                                         string? resName,
                                         string? filePath,
                                         int lineNumber )
        {
            return new CodeString( culture, ref handler, resName, filePath, lineNumber );
        }

        /// <summary>
        /// Intended to restore an instance from its component: this can typically be used by serializers/deserializers.
        /// </summary>
        /// <param name="formattedString">The <see cref="FormattedString"/>.</param>
        /// <param name="resName">The <see cref="ResName"/>.</param>
        /// <returns>A new code string.</returns>
        public static CodeString CreateFromProperties( in FormattedString formattedString, string resName )
        {
            Throw.CheckNotNullArgument( resName );
            return new CodeString( formattedString, resName );
        }

        /// <summary>
        /// Implicit cast into string: <see cref="Text"/>.
        /// </summary>
        /// <param name="f">This Text.</param>
        public static implicit operator string( CodeString f ) => f.Text;

        /// <summary>
        /// Overridden to return this <see cref="Text"/>.
        /// </summary>
        /// <returns>This text.</returns>
        public override string ToString() => _f.Text;

        #region Binary serialization
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
