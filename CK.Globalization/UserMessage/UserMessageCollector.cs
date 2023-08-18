using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Helper that collects multiple user messages.
    /// </summary>
    public sealed class UserMessageCollector
    {
        readonly CurrentCultureInfo _culture;
        readonly List<UserMessage> _messages;
        int _errorCount;
        byte _depth;

        /// <summary>
        /// Initializes a new message collector.
        /// </summary>
        /// <param name="culture">The current culture to use.</param>
        public UserMessageCollector( CurrentCultureInfo culture )
        {
            Throw.CheckNotNullArgument( culture );
            _culture = culture;
            _messages = new List<UserMessage>();
        }

        /// <summary>
        /// Gets the <see cref="CurrentCultureInfo"/> used to initialize the messages.
        /// </summary>
        public CurrentCultureInfo CurrentCulture => _culture;

        /// <summary>
        /// Gets the colected messages so far.
        /// </summary>
        public IReadOnlyList<UserMessage> UserMessages => _messages;

        /// <summary>
        /// Gets the current group depth.
        /// </summary>
        public int Depth => _depth;

        /// <summary>
        /// Gets the number of <see cref="UserMessageLevel.Error"/> collected so far.
        /// </summary>
        public int ErrorCount => _errorCount;

        /// <summary>
        /// Clears all collected messages so far, resets depth and error count.
        /// </summary>
        public void Clear()
        {
            _messages.Clear();
            _errorCount = 0;
            _depth = 0;
        }

        /// <summary>
        /// Writes these <see cref="UserMessages"/> as logs.
        /// The logges text is the <see cref="CodeString.FormattedString"/> and Error/Warn/Info and Open/Close groups
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
                    monitor.OpenGroup( (LogLevel)m.Level, m.Message.CodeString.FormattedString.Text );
                    ++d;
                }
                else
                {
                    while( d > m.Depth )
                    {
                        monitor.CloseGroup();
                        --d;
                    }
                    monitor.Log( (LogLevel)m.Level, m.Message.CodeString.FormattedString.Text );
                }
            }
            while( d > 0 )
            {
                monitor.CloseGroup();
                --d;
            }
        }

        /// <summary>
        /// Adds a new user message.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public void Add( UserMessageLevel level, string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
        {
            var m = new UserMessage( level, MCString.Create( _culture, plainText, resName, filePath, lineNumber ), _depth );
            if( level == UserMessageLevel.Error ) ++_errorCount;
            _messages.Add( m );
        }

        void DoAdd( UserMessageLevel level, ref FormattedStringHandler text, string? resName, string? filePath, int lineNumber )
        {
            var m = new UserMessage( level, MCString.Create( _culture, ref text, resName, filePath, lineNumber ), _depth );
            if( level == UserMessageLevel.Error ) ++_errorCount;
            _messages.Add( m );
        }

        /// <summary>
        /// Adds a new user message.
        /// </summary>
        /// <param name="level">The message level.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public void Add( UserMessageLevel level, [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
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
            if( level == UserMessageLevel.Error ) ++_errorCount;
            _messages.Add( m );
            return Util.CreateDisposableAction( CloseGroup );
        }

        /// <summary>
        /// Adds a new user message and increment the <see cref="UserMessage.Depth"/> for the future message until
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
            if( level == UserMessageLevel.Error ) ++_errorCount;
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
        public void Error( string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => Add( UserMessageLevel.Error, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Warn message.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public void Warn( string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => Add( UserMessageLevel.Warn, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Info message.
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public void Info( string plainText, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => Add( UserMessageLevel.Info, plainText, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Error message.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public void Error( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => DoAdd( UserMessageLevel.Error, ref text, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Warn message.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public void Warn( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
            => DoAdd( UserMessageLevel.Warn, ref text, resName, filePath, lineNumber );

        /// <summary>
        /// Adds a new Info message.
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of this result.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public void Info( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text, string? resName = null, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNumber = 0 )
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
