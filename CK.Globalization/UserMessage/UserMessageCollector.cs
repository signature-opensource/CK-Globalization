using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Helper that collects multiple user messages.
    /// <para>
    /// This user message builder is designed to be reusable: calling <see cref="Clear"/> resets it.
    /// Note that the <see cref="UserMessages"/> is a mutable list.
    /// </para>
    /// <para>
    /// The <see cref="ScopedUserMessageCollector"/> is available in a scope DI context (a "unit of work").
    /// </para>
    /// </summary>
    public class UserMessageCollector : IScopedAutoService
    {
        readonly CurrentCultureInfo _culture;
        readonly MList _messages;
        byte _depth;

        /// <summary>
        /// The real goal of this list is to throw when a !<see cref="UserMessage.IsValid"/> is added.
        /// The error count tracking is a bonus.
        /// </summary>
        sealed class MList : IList<UserMessage>
        {
            readonly List<UserMessage> _messages;
            internal int _errorCount;

            public MList()
            {
                _messages = new List<UserMessage>();
            }

            public UserMessage this[int index]
            {
                get => _messages[index];
                set
                {
                    Throw.CheckArgument( value.IsValid );
                    if( _messages[index].Level == UserMessageLevel.Error ) --_errorCount;
                    _messages[index] = value;
                    if( value.Level == UserMessageLevel.Error ) ++_errorCount;
                }
            }

            public int Count => _messages.Count;

            public bool IsReadOnly => false;

            public void Add( UserMessage item )
            {
                Throw.CheckArgument( item.IsValid );
                _messages.Add( item );
                if( item.Level == UserMessageLevel.Error ) ++_errorCount;
            }

            public void Clear()
            {
                _messages.Clear();
                _errorCount = 0;
            }

            public bool Contains( UserMessage item ) => _messages.Contains( item );

            public void CopyTo( UserMessage[] array, int arrayIndex ) => _messages.CopyTo( array, arrayIndex );

            public IEnumerator<UserMessage> GetEnumerator() => _messages.GetEnumerator();

            public int IndexOf( UserMessage item ) => _messages.IndexOf( item );

            public void Insert( int index, UserMessage item )
            {
                Throw.CheckArgument( item.IsValid );
                _messages.Insert( index, item );
                if( item.Level == UserMessageLevel.Error ) ++_errorCount;
            }

            public bool Remove( UserMessage item )
            {
                if( _messages.Remove( item ) )
                {
                    if( item.Level == UserMessageLevel.Error ) --_errorCount;
                    return true;
                }
                return false;
            }

            public void RemoveAt( int index )
            {
                if( _messages[index].Level == UserMessageLevel.Error ) --_errorCount;
                _messages.RemoveAt( index );
            }

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_messages).GetEnumerator();
        }

        /// <summary>
        /// Initializes a new message collector.
        /// </summary>
        /// <param name="culture">The current culture to use.</param>
        public UserMessageCollector( CurrentCultureInfo culture )
        {
            Throw.CheckNotNullArgument( culture );
            _culture = culture;
            _messages = new MList();
        }

        /// <summary>
        /// Gets the culture used to initialize the messages.
        /// </summary>
        public CurrentCultureInfo CurrentCultureInfo => _culture;

        /// <summary>
        /// Gets the culture used to initialize the messages.
        /// </summary>
        public ExtendedCultureInfo Culture => _culture.CurrentCulture;

        /// <summary>
        /// Gets the colected messages so far.
        /// This list is mutable: order can be changed, messages can be removed or added but
        /// when doing this, note that <see cref="ErrorCount"/> is not updated.
        /// </summary>
        public IList<UserMessage> UserMessages => _messages;

        /// <summary>
        /// Gets the current group depth.
        /// <para>
        /// This can be set but should be used only if the <see cref="UserMessages"/> list is manually updated.
        /// </para>
        /// </summary>
        public int Depth
        {
            get => _depth;
            set => _depth = (byte)value;
        }

        /// <summary>
        /// Gets the number of <see cref="UserMessageLevel.Error"/> collected so far.
        /// This is dynamically updated when messages are added or removed from the <see cref="UserMessages"/>.
        /// </summary>
        public int ErrorCount => _messages._errorCount;

        /// <summary>
        /// Clears all collected messages so far, resets depth and error count.
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
            _depth = 0;
        }

        /// <summary>
        /// Writes these <see cref="UserMessages"/> as logs.
        /// The logged text is the <see cref="CodeString.Text"/> and Error/Warn/Info and Open/Close groups
        /// reflect the user messages structure.
        /// </summary>
        /// <param name="monitor">The target monitor.</param>
        public void DumpLogs( IActivityMonitor monitor )
        {
            if( _messages.Count == 0 ) return;
            Throw.DebugAssert( _messages[0].Depth == 0 );
            int d = 0;
            foreach( var m in _messages )
            {
                if( d < m.Depth )
                {
                    Throw.DebugAssert( d == m.Depth - 1 );
                    monitor.OpenGroup( (LogLevel)m.Level, m.Message.CodeString );
                    ++d;
                }
                else
                {
                    while( d > m.Depth )
                    {
                        monitor.CloseGroup();
                        --d;
                    }
                    monitor.Log( (LogLevel)m.Level, m.Message.CodeString );
                }
            }
            while( d > 0 )
            {
                monitor.CloseGroup();
                --d;
            }
        }

        /// <summary>
        /// Adds all the exception's messages.
        /// See <see cref="UserMessageExceptionExtensions.GetUserMessages(Exception, Action{UserMessage}, CurrentCultureInfo?, byte, string?, bool?)"/>.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="defaultGenericMessage">Message used when <paramref name="leakAll"/> is false and there is no <see cref="MCException"/> available.</param>
        /// <param name="leakAll">
        /// Whether all exceptions must be exposed or only the <see cref="MCException"/> ones.
        /// Defaults to <see cref="CoreApplicationIdentity.EnvironmentName"/> == "#Dev".
        /// </param>
        /// <returns>The number of messages that have been added.</returns>
        public int AppendErrors( Exception ex, string? defaultGenericMessage = "An unhandled error occurred.", bool? leakAll = null )
        {
            Throw.CheckNotNullArgument( ex );
            return ex.GetUserMessages( _messages.Add, _culture, _depth, defaultGenericMessage, leakAll: leakAll );
        }

        /// <summary>
        /// Adds all the exception's messages at the top of the existing messages.
        /// See <see cref="UserMessageExceptionExtensions.GetUserMessages(Exception, Action{UserMessage}, CurrentCultureInfo?, byte, string?, bool?)"/>.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="defaultGenericMessage">Message used when <paramref name="leakAll"/> is false and there is no <see cref="MCException"/> available.</param>
        /// <param name="leakAll">
        /// Whether all exceptions must be exposed or only the <see cref="MCException"/> ones.
        /// Defaults to <see cref="CoreApplicationIdentity.EnvironmentName"/> == "#Dev".
        /// </param>
        /// <returns>The number of messages that have been added.</returns>
        public int PrependErrors( Exception ex, string? defaultGenericMessage = "An unhandled error occurred.", bool? leakAll = null )
        {
            Throw.CheckNotNullArgument( ex );
            int idx = 0;
            return ex.GetUserMessages( m => _messages.Insert( idx++, m ), _culture, 0, defaultGenericMessage, leakAll: leakAll );
        }

        /// <summary>
        /// Adds a new user message.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the message.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The added message.</returns>
        public UserMessage Add( UserMessageLevel level, string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
        {
            var m = new UserMessage( level, MCString.Create( _culture, plainText, resName, filePath, lineNumber ), _depth );
            _messages.Add( m );
            return m;
        }

        UserMessage DoAdd( UserMessageLevel level, ref FormattedStringHandler text, string? resName, string? filePath, int lineNumber )
        {
            var m = new UserMessage( level, MCString.Create( _culture, ref text, resName, filePath, lineNumber ), _depth );
            _messages.Add( m );
            return m;
        }

        /// <summary>
        /// Adds a new user message.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The added message.</returns>
        public UserMessage Add( UserMessageLevel level, [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => DoAdd( level, ref text, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new user message and increment the <see cref="UserMessage.Depth"/> for the future message until
        /// the returned <see cref="IDisposable"/> is disposed.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The disposable to dispose to close this group.</returns>
        public IDisposable OpenGroup( UserMessageLevel level, string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
        {
            var m = new UserMessage( level, MCString.Create( _culture, plainText, resName, filePath, lineNumber ), _depth );
            ++_depth;
            _messages.Add( m );
            return Util.CreateDisposableAction( CloseGroup );
        }

        /// <summary>
        /// Adds a new user message and increment the <see cref="UserMessage.Depth"/> for the future messages until
        /// the returned <see cref="IDisposable"/> is disposed.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The disposable to dispose to close this group.</returns>
        public IDisposable OpenGroup( UserMessageLevel level, [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
        {
            return DoOpenGroup( level, ref text, resName, filePath, lineNumber );
        }

        IDisposable DoOpenGroup( UserMessageLevel level, ref FormattedStringHandler text, string? resName, string? filePath, int lineNumber )
        {
            var m = new UserMessage( level, MCString.Create( _culture, ref text, resName, filePath, lineNumber ), _depth );
            ++_depth;
            _messages.Add( m );
            return Util.CreateDisposableAction( CloseGroup );
        }

        void CloseGroup()
        {
            if( _depth > 0 ) --_depth;
        }

        /// <summary>
        /// Adds a new Error message.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The added message.</returns>
        public UserMessage Error( string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => Add( UserMessageLevel.Error, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Warn message.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The added message.</returns>
        public UserMessage Warn( string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => Add( UserMessageLevel.Warn, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Info message.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The added message.</returns>
        public UserMessage Info( string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => Add( UserMessageLevel.Info, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Error message.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The added message.</returns>
        public UserMessage Error( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => DoAdd( UserMessageLevel.Error, ref text, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Warn message.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The added message.</returns>
        public UserMessage Warn( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => DoAdd( UserMessageLevel.Warn, ref text, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Info message.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The added message.</returns>
        public UserMessage Info( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => DoAdd( UserMessageLevel.Info, ref text, resName, filePath, lineNumber );

        /// <summary>
        /// Opens a new Error group. See <see cref="OpenGroup(UserMessageLevel, string, string?, string?, int)"/>.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The disposable to dispose to close this group.</returns>
        public IDisposable OpenError( string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => OpenGroup( UserMessageLevel.Error, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Opens a new Warn group. See <see cref="OpenGroup(UserMessageLevel, string, string?, string?, int)"/>.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The disposable to dispose to close this group.</returns>
        public IDisposable OpenWarn( string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => OpenGroup( UserMessageLevel.Warn, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Opens a new Info group. See <see cref="OpenGroup(UserMessageLevel, string, string?, string?, int)"/>.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The disposable to dispose to close this group.</returns>
        public IDisposable OpenInfo( string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => OpenGroup( UserMessageLevel.Info, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Opens a new Error group. See <see cref="OpenGroup(UserMessageLevel, FormattedStringHandler, string?, string?, int)"/>.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The disposable to dispose to close this group.</returns>
        public IDisposable OpenError( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => DoOpenGroup( UserMessageLevel.Error, ref text, resName, filePath, lineNumber );

        /// <summary>
        /// Opens a new Warn group. See <see cref="OpenGroup(UserMessageLevel, FormattedStringHandler, string?, string?, int)"/>.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The disposable to dispose to close this group.</returns>
        public IDisposable OpenWarn( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => DoOpenGroup( UserMessageLevel.Warn, ref text, resName, filePath, lineNumber );

        /// <summary>
        /// Opens a new Info group. See <see cref="OpenGroup(UserMessageLevel, FormattedStringHandler, string?, string?, int)"/>.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>The disposable to dispose to close this group.</returns>
        public IDisposable OpenInfo( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => DoOpenGroup( UserMessageLevel.Info, ref text, resName, filePath, lineNumber );

    }
}
