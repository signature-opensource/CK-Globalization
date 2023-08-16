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

        /// <inheritdoc cref="GetUserMessages(Exception, CurrentCultureInfo)"/>
        public static List<UserMessage> GetUserMessages( this Exception ex, ExtendedCultureInfo culture )
        {
            Throw.CheckNotNullArgument( culture != null );
            List<UserMessage> messages = new List<UserMessage>();
            Collect( messages.Add, 0, ex, culture, null );
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

        /// <inheritdoc cref="GetUserMessages(Exception, CurrentCultureInfo, Action{UserMessage})"/>
        public static void GetUserMessages( this Exception ex, ExtendedCultureInfo culture, Action<UserMessage> collector)
        {
            Throw.CheckNotNullArgument( culture != null );
            Throw.CheckNotNullArgument( collector );
            Collect( collector, 0, ex, culture, null );
        }

        static void Collect( Action<UserMessage> collector, byte depth, Exception e, ExtendedCultureInfo? culture, CurrentCultureInfo? current )
        {
            if( e is MCException mC )
            {
                collector( mC.AsUserMessage().With( depth ) );
                if( mC.InnerException != null ) Collect( collector, ++depth, mC.InnerException, culture, current );
            }
            else if( e is AggregateException a )
            {
                AddUserMessage( collector, depth++, culture, current, a.Message );
                foreach( var sub in a.InnerExceptions ) Collect( collector, depth, sub, culture, current );
            }
            else
            {
                AddUserMessage( collector, depth, culture, current, e.Message );
                if( e.InnerException != null ) Collect( collector, ++depth, e.InnerException, culture, current );
            }

            static void AddUserMessage( Action<UserMessage> collector, byte depth, ExtendedCultureInfo? culture, CurrentCultureInfo? current, string text )
            {
                if( culture != null ) collector( UserMessage.Error( culture, text ).With( depth ) );
                else
                {
                    Throw.DebugAssert( current != null );
                    collector( UserMessage.Error( current, text ).With( depth ) );
                }
            }
        }
    }
}
