//using System;
//using System.Diagnostics;
//using System.Globalization;
//using System.Net.NetworkInformation;
//using System.Runtime.CompilerServices;

//namespace CK.Core
//{
//    /// <summary>
//    /// Captures (once for all!) a info/warning/error <see cref="Type"/> and a <see cref="Message"/> that
//    /// can be used for globalization with the limitation of the settled <see cref="FormattedString.Culture"/> that
//    /// has been used to format the message's <see cref="FormattedString.Placeholders"/> contents.
//    /// </summary>
//    [SerializationVersion( 0 )]
//    public readonly struct ResultMessage : ICKSimpleBinarySerializable, ICKVersionedBinarySerializable
//    {
//        readonly FormattedString _message;
//        readonly string? _messageCode;
//        readonly ResultMessageType _type;

//        /// <summary>
//        /// Initializes a new <see cref="ResultMessage"/> with a message that must not be <see cref="FormattedString.IsEmpty"/>
//        /// otherwise an <see cref="ArgumentException"/> is thrown.
//        /// </summary>
//        /// <param name="type">The result message's type (<see cref="ResultMessageType.Info"/>, <see cref="ResultMessageType.Warn"/>
//        /// or <see cref="ResultMessageType.Error"/>). Cannot be <see cref="ResultMessageType.None"/>.</param>
//        /// <param name="message">The message. Cannot be <see cref="FormattedString.IsEmpty"/>.</param>
//        /// <param name="messageCode">Optional <see cref="MessageCode"/>.</param>
//        public ResultMessage( ResultMessageType type, FormattedString message, string? messageCode )
//        {
//            Throw.CheckArgument( type != ResultMessageType.None );
//            Throw.CheckArgument( message != null && !message.IsEmpty );
//            _type = type;
//            _message = message;
//            _messageCode = messageCode;
//        }

//        /// <summary>
//        /// Gets whether this message is valid.
//        /// Invalid message is the <c>default</c> value.
//        /// </summary>
//        public bool IsValid => _message != null;

//        /// <summary>
//        /// Gets this result message's type (<see cref="ResultMessageType.Info"/>, <see cref="ResultMessageType.Warn"/>
//        /// or <see cref="ResultMessageType.Error"/>).
//        /// <para>
//        /// This is <see cref="ResultMessageType.None"/> when <see cref="IsValid"/> is false.
//        /// </para>
//        /// </summary>
//        public ResultMessageType Type => _type;

//        /// <summary>
//        /// Gets this message's code if it has been provided. It should be like a resource name: "UserManagent.BadEmail",
//        /// "Security.AccessDenied", etc.
//        /// <para>
//        /// When no message code is provided, the <see cref="FormattedString.GetFormatString()"/> can be used as a key for
//        /// globalization process (but it is more efficient and maintainable to use a code).
//        /// </para>
//        /// </summary>
//        public string? MessageCode => _messageCode;

//        /// <summary>
//        /// Gets this result message's text. Necessarily not <see cref="FormattedString.IsEmpty"/> if <see cref="IsValid"/> is true.
//        /// </summary>
//        public FormattedString Message => _message ?? FormattedString.Empty;

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Error"/> result message. See <see cref="FormattedString(string)"/>.
//        /// </summary>
//        /// <param name="plainText">The plain text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Error( string plainText, string? messageCode = null ) => new ResultMessage( ResultMessageType.Error, new FormattedString( plainText ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Error"/> result message. See <see cref="FormattedString(CultureInfo,string)"/>.
//        /// </summary>
//        /// <param name="culture">The culture of this formatted string.</param>
//        /// <param name="plainText">The plain text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Error( CultureInfo culture, string plainText, string? messageCode = null ) => new ResultMessage( ResultMessageType.Error, new FormattedString( culture, plainText ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Error"/> result message. See <see cref="FormattedString(FormattedStringHandler)"/>.
//        /// </summary>
//        /// <param name="text">The interpolated text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Error( [InterpolatedStringHandlerArgument] FormattedStringHandler text, string? messageCode = null ) => new ResultMessage( ResultMessageType.Error, FormattedString.Create( ref text, CultureInfo.CurrentCulture ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Error"/> result message. See <see cref="FormattedString(CultureInfo,FormattedStringHandler)"/>.
//        /// </summary>
//        /// <param name="culture">The culture used to format placeholders' content.</param>
//        /// <param name="text">The interpolated text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Error( CultureInfo culture, [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text, string? messageCode = null ) => new ResultMessage( ResultMessageType.Error, FormattedString.Create( ref text, culture ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Warn"/> result message. See <see cref="FormattedString(string)"/>.
//        /// </summary>
//        /// <param name="plainText">The plain text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Warn( string plainText, string? messageCode = null ) => new ResultMessage( ResultMessageType.Warn, new FormattedString( plainText ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Warn"/> result message. See <see cref="FormattedString(CultureInfo,string)"/>.
//        /// </summary>
//        /// <param name="culture">The culture of this formatted string.</param>
//        /// <param name="plainText">The plain text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Warn( CultureInfo culture, string plainText, string? messageCode = null ) => new ResultMessage( ResultMessageType.Warn, new FormattedString( culture, plainText ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Warn"/> result message. See <see cref="FormattedString(FormattedStringHandler)"/>.
//        /// </summary>
//        /// <param name="text">The interpolated text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Warn( [InterpolatedStringHandlerArgument] FormattedStringHandler text, string? messageCode = null ) => new ResultMessage( ResultMessageType.Warn, FormattedString.Create( ref text, CultureInfo.CurrentCulture ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Warn"/> result message. See <see cref="FormattedString(CultureInfo,FormattedStringHandler)"/>.
//        /// </summary>
//        /// <param name="culture">The culture used to format placeholders' content.</param>
//        /// <param name="text">The interpolated text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Warn( CultureInfo culture, [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text, string? messageCode = null ) => new ResultMessage( ResultMessageType.Warn, FormattedString.Create( ref text, culture ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Info"/> result message. See <see cref="FormattedString(string)"/>.
//        /// </summary>
//        /// <param name="plainText">The plain text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Info( string plainText, string? messageCode = null ) => new ResultMessage( ResultMessageType.Info, new FormattedString( plainText ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Info"/> result message. See <see cref="FormattedString(CultureInfo,string)"/>.
//        /// </summary>
//        /// <param name="culture">The culture of this formatted string.</param>
//        /// <param name="plainText">The plain text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Info( CultureInfo culture, string plainText, string? messageCode = null ) => new ResultMessage( ResultMessageType.Info, new FormattedString( culture, plainText ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Info"/> result message. See <see cref="FormattedString(FormattedStringHandler)"/>.
//        /// </summary>
//        /// <param name="text">The interpolated text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Info( [InterpolatedStringHandlerArgument] FormattedStringHandler text, string? messageCode = null ) => new ResultMessage( ResultMessageType.Info, FormattedString.Create( ref text, CultureInfo.CurrentCulture ), messageCode );

//        /// <summary>
//        /// Creates a <see cref="ResultMessageType.Info"/> result message. See <see cref="FormattedString(CultureInfo,FormattedStringHandler)"/>.
//        /// </summary>
//        /// <param name="culture">The culture used to format placeholders' content.</param>
//        /// <param name="text">The interpolated text.</param>
//        /// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
//        /// <returns>A new Result message.</returns>
//        public static ResultMessage Info( CultureInfo culture, [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text, string? messageCode = null ) => new ResultMessage( ResultMessageType.Info, FormattedString.Create( ref text, culture ), messageCode );


//        #region Serialization
//        /// <summary>
//        /// Simple deserialization constructor.
//        /// </summary>
//        /// <param name="r">The reader.</param>
//        public ResultMessage( ICKBinaryReader r )
//            : this( r, r.ReadNonNegativeSmallInt32() )
//        {
//        }

//        /// <inheritdoc />
//        public void Write( ICKBinaryWriter w )
//        {
//            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( GetType() ) == 0 );
//            w.WriteNonNegativeSmallInt32( 0 );
//            WriteData( w );
//        }

//        /// <summary>
//        /// Versioned deserialization constructor.
//        /// </summary>
//        /// <param name="r">The reader.</param>
//        /// <param name="version">The saved version number.</param>
//        public ResultMessage( ICKBinaryReader r, int version )
//        {
//            Throw.CheckData( version == 0 );
//            // 0 versions for both: let's use the more efficient versioned serializable interface.
//            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( typeof(FormattedString) ) == 0 );
//            _message = new FormattedString( r, 0 );
//            _type = (ResultMessageType)r.ReadByte();
//            _messageCode = r.ReadNullableString();
//        }


//        /// <inheritdoc />
//        public void WriteData( ICKBinaryWriter w )
//        {
//            Debug.Assert( SerializationVersionAttribute.GetRequiredVersion( typeof( FormattedString ) ) == 0 );
//            _message.WriteData( w );
//            w.Write( (byte)_type );
//            w.WriteNullableString( _messageCode );
//        }
//        #endregion

//        /// <summary>
//        /// Gets the <c>"Type - Message"</c> string or
//        /// the <c>"Type - MessageCode - Message"</c> if <see cref="MessageCode"/> is not null.
//        /// </summary>
//        /// <returns>This message's type and text.</returns>
//        public override string ToString() => $"{Type} - {(_messageCode != null ? $"{_messageCode} -" : "")} {_message.Text}";
//    }
//}
