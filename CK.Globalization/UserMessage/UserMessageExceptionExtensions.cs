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
            List<UserMessage> messages = new List<UserMessage>();
            var e = ex;
            Collect( messages, 0, e, null, culture );
            return messages;
        }

        /// <inheritdoc cref="GetUserMessages(Exception, CurrentCultureInfo)"/>
        public static List<UserMessage> GetUserMessages( this Exception ex, ExtendedCultureInfo culture )
        {
            List<UserMessage> messages = new List<UserMessage>();
            var e = ex;
            Collect( messages, 0, e, culture, null );
            return messages;
        }

        static void Collect( List<UserMessage> messages, byte depth, Exception e, ExtendedCultureInfo? culture, CurrentCultureInfo? current )
        {
            if( e is MCException mC )
            {
                messages.Add( mC.AsUserMessage().With( depth ) );
                if( mC.InnerException != null ) Collect( messages, ++depth, mC.InnerException, culture, current );
            }
            else if( e is AggregateException a )
            {
                AddUserMessage( messages, depth++, culture, current, a.Message );
                foreach( var sub in a.InnerExceptions ) Collect( messages, depth, sub, culture, current );
            }
            else
            {
                AddUserMessage( messages, depth, culture, current, e.Message );
                if( e.InnerException != null ) Collect( messages, ++depth, e.InnerException, culture, current );
            }

            static void AddUserMessage( List<UserMessage> messages, byte depth, ExtendedCultureInfo? culture, CurrentCultureInfo? current, string text )
            {
                if( culture != null ) messages.Add( UserMessage.Error( culture, text ).With( depth ) );
                else
                {
                    Throw.DebugAssert( current != null );
                    messages.Add( UserMessage.Error( current, text ).With( depth ) );
                }
            }
        }
    }
}
