using System.Diagnostics;

namespace CK.Core
{
    /// <summary>
    /// Simplified version of a <see cref="UserMessage"/>: without <see cref="UserMessage.ResName"/> nor culture related information
    /// since the <see cref="Message"/> is a mere string instead of a <see cref="MCString"/>.
    /// <para>
    /// Any <see cref="UserMessage"/> can be automatically cast into this simplified form.
    /// </para>
    /// </summary>
    [SerializationVersion(0)]
    public readonly struct SimpleUserMessage : ICKSimpleBinarySerializable, ICKVersionedBinarySerializable
    {
        readonly string _message;
        readonly byte _level;
        readonly byte _depth;

        public SimpleUserMessage( UserMessageLevel level, string message, byte depth = 0 )
        {
            Throw.CheckArgument( level != UserMessageLevel.None );
            Throw.CheckNotNullArgument( message );
            _level = (byte)level;
            _message = message;
            _depth = depth;
        }


        SimpleUserMessage( byte depth, string message, byte level )
        {
            _level = level;
            _message = message;
            _depth = depth;
        }

        /// <summary>
        /// Returns a new message with a given <see cref="Level"/>.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <returns>The same <see cref="Message"/> with the <paramref name="level"/>.</returns>
        public SimpleUserMessage With( UserMessageLevel level )
        {
            Throw.CheckArgument( level != UserMessageLevel.None );
            return new SimpleUserMessage( _depth, _message, (byte)level );
        }

        /// <summary>
        /// Returns a new message with a given <see cref="Depth"/>.
        /// </summary>
        /// <param name="depth">The message depth.</param>
        /// <returns>The same <see cref="Message"/> with the <paramref name="depth"/>.</returns>
        public SimpleUserMessage With( byte depth )
        {
            return new SimpleUserMessage( depth, _message, _level );
        }


        /// <summary>
        /// Gets whether this message is valid.
        /// Invalid message is the <c>default</c> value.
        /// </summary>
        public bool IsValid => _message != null;

        /// <summary>
        /// Gets this result message's level (<see cref="UserMessageLevel.Info"/>, <see cref="UserMessageLevel.Warn"/>
        /// or <see cref="UserMessageLevel.Error"/>).
        /// <para>
        /// This is <see cref="UserMessageLevel.None"/> when <see cref="IsValid"/> is false.
        /// </para>
        /// </summary>
        public UserMessageLevel Level => (UserMessageLevel)_level;

        /// <summary>
        /// Gets the depth of this message.
        /// </summary>
        public byte Depth => _depth;

        /// <summary>
        /// Gets this message.
        /// </summary>
        public string Message => _message ?? string.Empty;

        /// <summary>
        /// Gets the <c>"Level - Message"</c> string.
        /// </summary>
        /// <returns>This message's level and text.</returns>
        public override string ToString() => $"{Level} - {Message}";

        #region Serialization
        /// <summary>
        /// Simple deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        public SimpleUserMessage( ICKBinaryReader r )
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
        public SimpleUserMessage( ICKBinaryReader r, int version )
        {
            Throw.CheckData( version == 0 );
            _level = r.ReadByte();
            if( _level != 0 )
            {
                _depth = r.ReadByte();
                _message = r.ReadString();
            }
            else
            {
                _message = string.Empty;
            }
        }


        /// <inheritdoc />
        public void WriteData( ICKBinaryWriter w )
        {
            w.Write( _level );
            if( _level != 0 )
            {
                w.Write( _depth );
                w.Write( _message );
            }
        }
        #endregion


    }
}
