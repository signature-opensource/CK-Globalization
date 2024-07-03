using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CK.Core
{
    /// <summary>
    /// Stupid helper that should be used temporary... unless the application is definitely in french,
    /// and you'll never localize it.
    /// </summary>
    public static class FrenchHelper
    {
        static NormalizedCultureInfo? _french;

        /// <summary>
        /// Gets the "fr" culture.
        /// </summary>
        public static NormalizedCultureInfo French => _french ?? NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" );

        /// <summary>
        /// Helper that should be used temporary... unless the application is definitely in french. And you won't localize it.
        /// </summary>
        /// <param name="messages">This list of messages.</param>
        /// <param name="message">The message.</param>
        /// <param name="level">The message level.</param>
        public static void AddNonTranslatableFrenchMessage( this IList<UserMessage> messages, string message, UserMessageLevel level )
        {
            messages.Add( new UserMessage( level, MCString.CreateNonTranslatable( French, message ) ) );
        }

        /// <summary>
        /// Helper that should be used temporary... unless the application is definitely in french. And you won't localize it.
        /// </summary>
        /// <param name="messages">This list of messages.</param>
        /// <param name="message">The message.</param>
        public static void AddNonTranslatableFrenchError( this IList<UserMessage> messages, string message )
        {
            AddNonTranslatableFrenchMessage( messages, message, UserMessageLevel.Error );
        }

        /// <inheritdoc cref="AddNonTranslatableFrenchError(IList{UserMessage}, string)"/>
        public static void AddNonTranslatableFrenchWarn( this IList<UserMessage> messages, string message )
        {
            AddNonTranslatableFrenchMessage( messages, message, UserMessageLevel.Warn );
        }

        /// <inheritdoc cref="AddNonTranslatableFrenchError(IList{UserMessage}, string)"/>
        public static void AddNonTranslatableFrenchInfo( this IList<UserMessage> messages, string message )
        {
            AddNonTranslatableFrenchMessage( messages, message, UserMessageLevel.Info );
        }
    }
}
