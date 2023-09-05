using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Captures (once for all!) a info/warning/error <see cref="Level"/> and a <see cref="Message"/> that
    /// is a <see cref="MCString"/>.
    /// <para>
    /// The simplified projection of a UserMessage is a <see cref="SimpleUserMessage"/>: this is implicitly castable as a SimpleUserMessage.
    /// </para>
    /// </summary>
    [SerializationVersion( 0 )]
    public readonly struct UserMessage : ICKSimpleBinarySerializable, ICKVersionedBinarySerializable
    {
        readonly MCString _message;
        readonly byte _level;
        readonly byte _depth;

        /// <summary>
        /// Initializes a new <see cref="UserMessage"/>.
        /// </summary>
        /// <param name="level">The result message's type (<see cref="UserMessageLevel.Info"/>, <see cref="UserMessageLevel.Warn"/>
        /// or <see cref="UserMessageLevel.Error"/>). Cannot be <see cref="UserMessageLevel.None"/>.</param>
        /// <param name="message">The message.</param>
        /// <param name="depth">Optional depth.</param>
        public UserMessage( UserMessageLevel level, MCString message, byte depth = 0 )
        {
            Throw.CheckArgument( level != UserMessageLevel.None );
            _level = (byte)level;
            _message = message;
            _depth = depth;
        }

        /// <summary>
        /// Gets whether this message is valid.
        /// Invalid message is the <c>default</c> value.
        /// </summary>
        public bool IsValid => _message != null;

        /// <summary>
        /// Returns a new message with a given <see cref="Level"/>.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <returns>The same <see cref="Message"/> with the <paramref name="level"/>.</returns>
        public UserMessage With( UserMessageLevel level )
        {
            Throw.CheckArgument( level != UserMessageLevel.None );
            return new UserMessage( level, Message, _depth );
        }

        /// <summary>
        /// Returns a new message with a given <see cref="Depth"/>.
        /// </summary>
        /// <param name="depth">The message depth.</param>
        /// <returns>The same <see cref="Message"/> with the <paramref name="depth"/>.</returns>
        public UserMessage With( byte depth )
        {
            return new UserMessage( (UserMessageLevel)_level, Message, depth );
        }

        /// <summary>
        /// Gets this result message's level (<see cref="UserMessageLevel.Info"/>, <see cref="UserMessageLevel.Warn"/>
        /// or <see cref="UserMessageLevel.Error"/>).
        /// <para>
        /// This is <see cref="UserMessageLevel.None"/> when <see cref="IsValid"/> is false.
        /// </para>
        /// </summary>
        public UserMessageLevel Level => (UserMessageLevel)_level;

        /// <summary>
        /// Gets this message's text.
        /// </summary>
        public string Text => _message?.Text ?? String.Empty;

        /// <summary>
        /// Gets the depth of this message.
        /// </summary>
        public byte Depth => _depth;

        /// <summary>
        /// Gets this message's resource name. It should be like a resource name: "UserManagent.BadEmail",
        /// "Security.AccessDenied", etc. or starts with "SHA." when inferred from the format string.
        /// </summary>
        public string ResName => _message?.CodeString.ResName ?? String.Empty;

        /// <summary>
        /// Gets this <see cref="MCString"/> message.
        /// </summary>
        public MCString Message => _message ?? MCString.Empty;

        /// <summary>
        /// Gets whether a translation is welcome. See <see cref="MCString.IsTranslationWelcome"/>.
        /// </summary>
        public bool IsTranslationWelcome => _message?.IsTranslationWelcome ?? false;

        /// <summary>
        /// Simplifies this message by returning a <see cref="SimpleUserMessage"/>.
        /// <para>
        /// Note that this method is exposed to ease its discovery but can be omitted since
        /// a UserMessage is implicitly castable into a SimpleUserMessage.
        /// </para>
        /// </summary>
        /// <returns>A <see cref="SimpleUserMessage"/>.</returns>
        public SimpleUserMessage AsSimpleUserMessage() => IsValid ? new SimpleUserMessage( Level, Message, Depth ) : default;

        /// <summary>
        /// Implicit cast into <see cref="SimpleUserMessage"/>.
        /// </summary>
        /// <param name="m">This UserMessage.</param>
        public static implicit operator SimpleUserMessage( UserMessage m ) => m.AsSimpleUserMessage();


        #region Create (with level).
        /// <summary>
        /// Creates a user message from a plain text (no placeholders).
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Create( UserMessageLevel level,
                                          ExtendedCultureInfo culture,
                                          string plainText,
                                          string? resName = null,
                                          [CallerFilePath] string? filePath = null,
                                          [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( level, MCString.CreateUntracked( new CodeString( culture, plainText, resName, filePath, lineNumber ) ) );

        /// <summary>
        /// Creates a directly translated result message thanks to the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Create( UserMessageLevel level,
                                          CurrentCultureInfo culture,
                                          string plainText,
                                          string? resName = null,
                                          [CallerFilePath] string? filePath = null,
                                          [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( level, MCString.Create( culture, plainText, resName, filePath, lineNumber ) );


        /// <summary>
        /// Creates a user message.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="MessageCode"/>.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Create( UserMessageLevel level,
                                          ExtendedCultureInfo culture,
                                          [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                          string? resName = null,
                                          [CallerFilePath] string? filePath = null,
                                          [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( level, MCString.CreateUntracked( CodeString.Create( ref text, culture, resName, filePath, lineNumber ) ) );

        /// <summary>
        /// Creates a directly translated result message thanks to the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/>.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Create( UserMessageLevel level,
                                          CurrentCultureInfo culture,
                                          [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                          string? resName = null,
                                          [CallerFilePath] string? filePath = null,
                                          [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( level, MCString.Create( culture, ref text, resName, filePath, lineNumber ) );

        #endregion

        #region Error
        /// <summary>
        /// Creates a user message.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Error( ExtendedCultureInfo culture,
                                         string plainText,
                                         string? resName = null,
                                         [CallerFilePath] string? filePath = null,
                                         [CallerLineNumber] int lineNumber = 0 )
            => Create( UserMessageLevel.Error, culture, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Creates a directly translated result message thanks to the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Error( CurrentCultureInfo culture,
                                         string plainText,
                                         string? resName = null,
                                         [CallerFilePath] string? filePath = null,
                                         [CallerLineNumber] int lineNumber = 0 )
            => Create( UserMessageLevel.Error, culture, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Creates a user message.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Error( ExtendedCultureInfo culture,
                                         [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                         string? resName = null,
                                         [CallerFilePath] string? filePath = null,
                                         [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( UserMessageLevel.Error, MCString.CreateUntracked( CodeString.Create( ref text, culture, resName, filePath, lineNumber ) ) );

        /// <summary>
        /// Creates a directly translated result message thanks to the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/>.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Error( CurrentCultureInfo culture,
                                         [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                         string? resName = null,
                                         [CallerFilePath] string? filePath = null,
                                         [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( UserMessageLevel.Error, MCString.Create( culture, ref text, resName, filePath, lineNumber ) );

        #endregion

        #region Warn
        /// <summary>
        /// Creates a user message.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Warn( ExtendedCultureInfo culture,
                                        string plainText,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
            => Create( UserMessageLevel.Warn, culture, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Creates a directly translated result message thanks to the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Warn( CurrentCultureInfo culture,
                                        string plainText,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
            => Create( UserMessageLevel.Warn, culture, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Creates a user message.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Warn( ExtendedCultureInfo culture,
                                        [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( UserMessageLevel.Warn, MCString.CreateUntracked( CodeString.Create( ref text, culture, resName, filePath, lineNumber ) ) );

        /// <summary>
        /// Creates a directly translated result message thanks to the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/>.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Warn( CurrentCultureInfo culture,
                                        [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( UserMessageLevel.Warn, MCString.Create( culture, ref text, resName, filePath, lineNumber ) );

        #endregion

        #region Info
        /// <summary>
        /// Creates a user message.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Info( ExtendedCultureInfo culture,
                                        string plainText,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
            => Create( UserMessageLevel.Info, culture, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Creates a directly translated result message thanks to the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Info( CurrentCultureInfo culture,
                                        string plainText,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
            => Create( UserMessageLevel.Info, culture, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Creates a user message.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Info( ExtendedCultureInfo culture,
                                        [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( UserMessageLevel.Info, MCString.CreateUntracked( CodeString.Create( ref text, culture, resName, filePath, lineNumber ) ) );

        /// <summary>
        /// Creates a directly translated result message thanks to the <see cref="CurrentCultureInfo.CurrentCulture"/>
        /// and <see cref="CurrentCultureInfo.TranslationService"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/>.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public static UserMessage Info( CurrentCultureInfo culture,
                                        [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
            => new UserMessage( UserMessageLevel.Info, MCString.Create( culture, ref text, resName, filePath, lineNumber ) );

        #endregion

        #region Serialization
        /// <summary>
        /// Simple deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        public UserMessage( ICKBinaryReader r )
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
        public UserMessage( ICKBinaryReader r, int version )
        {
            Throw.CheckData( version == 0 );
            // 0 versions for both: let's use the more efficient versioned serializable interface.
            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( typeof( FormattedString ) ) == 0 );
            _level = r.ReadByte();
            if( _level != 0 )
            {
                _depth = r.ReadByte();
                _message = new MCString( r, 0 );
            }
            else
            {
                _message = MCString.Empty;
            }
        }


        /// <inheritdoc />
        public void WriteData( ICKBinaryWriter w )
        {
            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( typeof( FormattedString ) ) == 0 );
            w.Write( _level );
            if( _level != 0 )
            {
                w.Write( _depth );
                _message.WriteData( w );
            }
        }
        #endregion

        /// <summary>
        /// Gets the <c>"Level - ResName - Text message"</c> string.
        /// </summary>
        /// <returns>This message's Level, ResName and Text.</returns>
        public override string ToString() => $"{Level} - {ResName} - {Text}";

    }
}
