using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Core
{
    /// <summary>
    /// Automatic scoped service that captures the <see cref="ExtendedCultureInfo"/> ubiquitous service
    /// and the singleton <see cref="TranslationService"/>.
    /// <para>
    /// It provides <see cref="Core.MCString"/>, <see cref="Core.UserMessage"/> and <see cref="Core.MCException"/> factory methods.
    /// </para>
    /// </summary>
    public sealed class CurrentCultureInfo : IScopedAutoService, IFormatProvider
    {
        /// <summary>
        /// Initializes a new scoped <see cref="ExtendedCultureInfo"/>.
        /// </summary>
        /// <param name="translationService">The translation service.</param>
        /// <param name="currentCulture">The current culture.</param>
        public CurrentCultureInfo( TranslationService translationService, ExtendedCultureInfo currentCulture )
        {
            TranslationService = translationService;
            CurrentCulture = currentCulture;
        }

        /// <summary>
        /// Gets the translation service.
        /// </summary>
        public TranslationService TranslationService { get; }

        /// <summary>
        /// Gets the current culture.
        /// </summary>
        public ExtendedCultureInfo CurrentCulture { get; }

        /// <summary>
        /// Creates a new <see cref="CK.Core.MCString"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="resName">Optional associated resource name.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A string, hopefully translated by the <see cref="TranslationService"/>.</returns>
        public MCString MCString( string text,
                                  string? resName = null,
                                  [CallerFilePath] string? filePath = null,
                                  [CallerLineNumber] int lineNumber = 0 )
        {
            return CK.Core.MCString.Create( this, text, resName, filePath, lineNumber );
        }

        /// <inheritdoc cref="MCString(string, string?, string?, int)"/>
        public MCString MCString( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text,
                                  string? resName = null,
                                  [CallerFilePath] string? filePath = null,
                                  [CallerLineNumber] int lineNumber = 0 )
        {
            return Core.MCString.Create( this, ref text, resName, filePath, lineNumber );
        }

        /// <summary>
        /// Creates a user message from a plain text (no placeholders).
        /// </summary>
        /// <param name="level">The message level. Must not be <see cref="UserMessageLevel.None"/>.</param>
        /// <param name="plainText">The english plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the message.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public UserMessage UserMessage( UserMessageLevel level,
                                        string plainText,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
        {
            return Core.UserMessage.Create( this, level, plainText, resName, filePath, lineNumber );
        }

        /// <summary>
        /// Creates a user message from an interpolated text.
        /// </summary>
        /// <param name="level">The message level. Must not be <see cref="UserMessageLevel.None"/>.</param>
        /// <param name="text">The english plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the message.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public UserMessage UserMessage( UserMessageLevel level,
                                        [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
        {
            return new UserMessage( level, Core.MCString.Create( this, ref text, resName, filePath, lineNumber ) );
        }

        /// <summary>
        /// Creates a error message from a plain text (no placeholders).
        /// </summary>
        /// <param name="plainText">The english plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the message.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public UserMessage ErrorMessage( string plainText,
                                         string? resName = null,
                                         [CallerFilePath] string? filePath = null,
                                         [CallerLineNumber] int lineNumber = 0 )
        {
            return Core.UserMessage.Create( this, UserMessageLevel.Error, plainText, resName, filePath, lineNumber );
        }

        /// <summary>
        /// Creates a error message from an interpolated text.
        /// </summary>
        /// <param name="text">The english plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the message.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public UserMessage ErrorMessage( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text,
                                         string? resName = null,
                                         [CallerFilePath] string? filePath = null,
                                         [CallerLineNumber] int lineNumber = 0 )
        {
            return new UserMessage( UserMessageLevel.Error, Core.MCString.Create( this, ref text, resName, filePath, lineNumber ) );
        }

        /// <summary>
        /// Creates a warn message from a plain text (no placeholders).
        /// </summary>
        /// <param name="plainText">The english plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the message.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public UserMessage WarnMessage( string plainText,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
        {
            return Core.UserMessage.Create( this, UserMessageLevel.Warn, plainText, resName, filePath, lineNumber );
        }

        /// <summary>
        /// Creates a warn message from an interpolated text.
        /// </summary>
        /// <param name="text">The english plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the message.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public UserMessage WarnMessage( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
        {
            return new UserMessage( UserMessageLevel.Warn, Core.MCString.Create( this, ref text, resName, filePath, lineNumber ) );
        }

        /// <summary>
        /// Creates a info message from a plain text (no placeholders).
        /// </summary>
        /// <param name="plainText">The english plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the message.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public UserMessage InfoMessage( string plainText,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
        {
            return Core.UserMessage.Create( this, UserMessageLevel.Info, plainText, resName, filePath, lineNumber );
        }

        /// <summary>
        /// Creates a info message from an interpolated text.
        /// </summary>
        /// <param name="text">The english plain text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the message.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new Result message.</returns>
        public UserMessage InfoMessage( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text,
                                        string? resName = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
        {
            return new UserMessage( UserMessageLevel.Info, Core.MCString.Create( this, ref text, resName, filePath, lineNumber ) );
        }


        /// <summary>
        /// Creates a new <see cref="Core.MCException"/>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="resName">The optional <see cref="CodeString.ResName"/> of the exception's message.</param>
        /// <param name="innerException">Optional inner exception.</param>
        /// <param name="filePath">Automatically set by the compiler.</param>
        /// <param name="lineNumber">Automatically set by the compiler.</param>
        /// <returns>A new MCException.</returns>
        public MCException MCException( string text,
                                        string? resName = null,
                                        Exception? innerException = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
        {
            return new MCException( this, text, resName, innerException, filePath, lineNumber );
        }

        /// <inheritdoc cref="MCException(string, string?, Exception?, string?, int)"/>
        public MCException MCException( [InterpolatedStringHandlerArgument( "" )] FormattedStringHandler text,
                                        string? resName = null,
                                        Exception? innerException = null,
                                        [CallerFilePath] string? filePath = null,
                                        [CallerLineNumber] int lineNumber = 0 )
        {
            return new MCException( Core.MCString.Create( this, ref text, resName, filePath, lineNumber ), innerException );
        }

        object? IFormatProvider.GetFormat( Type? formatType ) => CurrentCulture.PrimaryCulture.Culture.GetFormat( formatType );
    }
}
