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
        /// See <see cref="GetUserMessages(Exception, CurrentCultureInfo, Action{UserMessage})"/>.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="culture">The current culture.</param>
        /// <returns>A list of one or more messages.</returns>
        public static List<UserMessage> GetUserMessages( this Exception ex, CurrentCultureInfo culture )
        {
            Throw.CheckNotNullArgument( culture != null );
            List<UserMessage> messages = new List<UserMessage>();
            Collect( messages.Add, 0, ex, culture );
            return messages;
        }

        /// <summary>
        /// See <see cref="GetUserMessages(Exception, Action{UserMessage})"/>.
        /// </summary>
        /// <param name="ex">This exception.</param>
        /// <returns>A list of all the exception messages.</returns>
        public static List<UserMessage> GetUserMessages( this Exception ex )
        {
            List<UserMessage> messages = new List<UserMessage>();
            Collect( messages.Add, 0, ex, null );
            return messages;
        }

        /// <summary>
        /// Collects the <see cref="Exception.Message"/> texts recursively (following <see cref="Exception.InnerException"/>
        /// and <see cref="AggregateException.InnerExceptions"/>). When a <see cref="MCException"/> is found, its <see cref="MCException.AsUserMessage"/>
        /// is used but for any other exception types, we try to translate the exception's message by using the <see cref="CurrentCultureInfo.TranslationService"/>
        /// as if the text was in <see cref="NormalizedCultureInfo.CodeDefault"/>. If a "SHA." translation resource exists, then we can translate exception messages...
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
            Collect( collector, 0, ex, culture );
        }

        /// <summary>
        /// Collects the <see cref="Exception.Message"/> texts recursively (following <see cref="Exception.InnerException"/>
        /// and <see cref="AggregateException.InnerExceptions"/>). When a <see cref="MCException"/> is found, its <see cref="MCException.AsUserMessage"/>
        /// is used but for any other exception types, the message is transformed in a non translatable string (<see cref="MCString.CreateNonTranslatable(NormalizedCultureInfo, string)"/>
        /// with the <see cref="NormalizedCultureInfo.Invariant"/> format culture and the <see cref="CodeString.Empty"/> code string).
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="collector">The collector.</param>
        public static void GetUserMessages( this Exception ex, Action<UserMessage> collector )
        {
            Throw.CheckNotNullArgument( collector );
            Collect( collector, 0, ex, null );
        }

        static void Collect( Action<UserMessage> collector, byte depth, Exception e, CurrentCultureInfo? current )
        {
            if( e is MCException mC )
            {
                collector( mC.AsUserMessage().With( depth ) );
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
                // We CANNOT know the content culture of text. Even by looking up the current culture:
                // this depends on the existence of the resx.
                // If there is a CurrentCultureInfo, we can try to use the TranslationService: if the text is fixed, it MAY
                // be tranaslated by its "SHA." resource by considering that the text is in "CodeDefault" (whatever its actual
                // language is).
                // If we have no CurrentCultureInfo, instead of creating a totally artificial CodeString, we create a non translatable
                // MCString (that is bound to CodeString.Empty) with the Invariant culture.
                var m = current != null
                        ? UserMessage.Error( current, text )
                        : new UserMessage( UserMessageLevel.Error, MCString.CreateNonTranslatable( NormalizedCultureInfo.Invariant, text ) );

                collector( m.With( depth ) );
            }
        }
    }
}
