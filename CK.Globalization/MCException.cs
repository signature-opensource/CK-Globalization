using System;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// A simple exception with a <see cref="MCString"/> message instead of a mere string.
    /// <para>
    /// This is not serializable and this is intended: <see cref="AsUserMessage"/> should be
    /// exchanged, not the full exception.
    /// </para>
    /// </summary>
    public sealed class MCException : Exception
    {
        /// <summary>
        /// Initializes a new <see cref="MCException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        public MCException( MCString message, Exception? innerException = null )
            : base( message.Text, innerException )
        {
            Message = message;
        }

        /// <summary>
        /// Initializes a new <see cref="MCException"/> based on the <see cref="NormalizedCultureInfo.Current"/>.
        /// <para>
        /// This should be avoided. Instead provide the culture explictly and even better the <see cref="CurrentCultureInfo"/>.
        /// </para>
        /// </summary>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this exception.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public MCException( string plainText,
                            string? resName = null,
                            Exception? innerException = null,
                            [CallerFilePath] string? filePath = null,
                            [CallerLineNumber] int lineNumber = 0 )
            : this( MCString.CreateUntracked( new CodeString( plainText, resName, filePath, lineNumber ) ), innerException )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="MCException"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this exception.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public MCException( ExtendedCultureInfo culture,
                            string plainText,
                            string? resName = null,
                            Exception? innerException = null,
                            [CallerFilePath] string? filePath = null,
                            [CallerLineNumber] int lineNumber = 0 )
            : this( MCString.CreateUntracked( new CodeString( culture, plainText, resName, filePath, lineNumber ) ), innerException )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="MCException"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="plainText">The plain text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this exception.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public MCException( CurrentCultureInfo culture,
                            string plainText,
                            string? resName = null,
                            Exception? innerException = null,
                            [CallerFilePath] string? filePath = null,
                            [CallerLineNumber] int lineNumber = 0 )
            : this( MCString.Create( culture, plainText, resName, filePath, lineNumber ), innerException )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="MCException"/> based on the <see cref="NormalizedCultureInfo.Current"/>.
        /// <para>
        /// This should be avoided. Instead provide the culture explictly and even better the <see cref="CurrentCultureInfo"/>.
        /// </para>
        /// </summary>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public MCException( [InterpolatedStringHandlerArgument] FormattedStringHandler text,
                            string? resName = null,
                            Exception? innerException = null,
                            [CallerFilePath] string? filePath = null,
                            [CallerLineNumber] int lineNumber = 0 )
            : this( MCString.CreateUntracked( CodeString.Create( ref text, NormalizedCultureInfo.Current, resName, filePath, lineNumber ) ), innerException )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="MCException"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public MCException( ExtendedCultureInfo culture,
                            [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                            string? resName = null,
                            Exception? innerException = null,
                            [CallerFilePath] string? filePath = null,
                            [CallerLineNumber] int lineNumber = 0 )
            : this( MCString.CreateUntracked( CodeString.Create( ref text, culture, resName, filePath, lineNumber ) ), innerException )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="MCException"/>.
        /// </summary>
        /// <param name="culture">The current culture.</param>
        /// <param name="text">The interpolated text.</param>
        /// <param name="resName">The optional <see cref="ResName"/>.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        public MCException( CurrentCultureInfo culture,
                            [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                            string? resName = null,
                            Exception? innerException = null,
                            [CallerFilePath] string? filePath = null,
                            [CallerLineNumber] int lineNumber = 0 )
            : this( MCString.Create( culture, ref text, resName, filePath, lineNumber ), innerException )
        {
        }

        /// <summary>
        /// Gets the exception message.
        /// </summary>
        public new MCString Message { get; }

        /// <summary>
        /// Returns a <see cref="UserMessageLevel.Error"/> message.
        /// </summary>
        /// <returns>A <see cref="UserMessageLevel.Error"/> message.</returns>
        public UserMessage AsUserMessage() => new UserMessage( UserMessageLevel.Error, Message );

    }
}
