using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Provides user messages to exceptions.
    /// </summary>
    public static class UserMessageExceptionExtensions
    {
        /// <summary>
        /// Gets a list of error user messages with the messages of this exception and
        /// any <see cref="Exception.InnerException"/> or <see cref="AggregateException.InnerExceptions"/>.
        /// <para>
        /// Inner exceptions are indented by <see cref="UserMessage.Depth"/>.
        /// </para>
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="culture">The current culture.</param>
        /// <returns>A list of one or more messages.</returns>
        public static List<UserMessage> GetUserMessages( this Exception ex, CurrentCultureInfo culture )
        {
            Throw.CheckNotNullArgument( culture != null );
            List<UserMessage> messages = new List<UserMessage>();
            Collect( messages.Add, 0, ex, null, culture );
            return messages;
        }

        public static List<UserMessage> GetUserMessages( this Exception ex )
        {
            List<UserMessage> messages = new List<UserMessage>();
            Collect( messages.Add, 0, ex, null );
            return messages;
        }

        /// <summary>
        /// Gets a list of error user messages with the messages of this exception and
        /// any <see cref="Exception.InnerException"/> or <see cref="AggregateException.InnerExceptions"/>.
        /// <para>
        /// Inner exceptions are indented by <see cref="UserMessage.Depth"/>.
        /// </para>
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="culture">The current culture.</param>
        /// <param name="collector">Message collector.</param>
        /// <returns>A list of one or more messages.</returns>
        public static void GetUserMessages( this Exception ex, CurrentCultureInfo culture, Action<UserMessage> collector )
        {
            Throw.CheckNotNullArgument( culture != null );
            Throw.CheckNotNullArgument( collector );
            Collect( collector, 0, ex, null, culture );
        }

        public static void GetUserMessages( this Exception ex, Action<UserMessage> collector)
        {
            Throw.CheckNotNullArgument( collector );
            Collect( collector, 0, ex, null );
        }

        static void Collect( Action<UserMessage> collector, byte depth, Exception e, CurrentCultureInfo? current )
        {
            if( e is MCException mC )
            {
                collector( mC.AsUserMessage( current ).With( depth ) );
                if( mC.InnerException != null ) Collect( collector, ++depth, mC.InnerException, current );
            }
            else if( e is AggregateException a )
            {
                AddUserMessage( collector, depth++, current, a.Message );
                foreach( var sub in a.InnerExceptions ) Collect( collector, depth, sub, current );
            }
            else
            {
                AddUserMessage( collector, depth, current, e.Message );
                if( e.InnerException != null ) Collect( collector, ++depth, e.InnerException, current );
            }

            static void AddUserMessage( Action<UserMessage> collector, byte depth, CurrentCultureInfo? current, string text )
            {
                if( current != null )
                {
                    collector( UserMessage.Error( current, text ).With( depth ) );
                }
                else
                {
                    // We CANNOT know the content culture of text. Even by looking up the current culture:
                    // this depends on the existence of the resx.
                    new UserMessage(UserMessageLevel.Error, MCString.CreateNonTranslatable( NormalizedCultureInfo.CodeDefault, text ) ).With( depth );
                }
            }
        }
    }
}
