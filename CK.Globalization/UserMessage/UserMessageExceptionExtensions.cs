using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace CK.Core;

/// <summary>
/// Provides user messages to exceptions.
/// </summary>
public static class UserMessageExceptionExtensions
{
    /// <summary>
    /// See <see cref="GetUserMessages(Exception, Action{UserMessage}, CurrentCultureInfo?, byte, string?, bool?)"/>.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <param name="culture">The current culture. This can be null when no culture is available in the context.</param>
    /// <param name="depth">Depth of the root exception.</param>
    /// <param name="defaultGenericMessage">Message used when <paramref name="leakAll"/> is false and there is no <see cref="MCException"/> available.</param>
    /// <param name="leakAll">
    /// Whether all exceptions must be exposed or only the <see cref="MCException"/> ones.
    /// Defaults to <see cref="CoreApplicationIdentity.IsDevelopmentAndInitialized"/>: we want to be sure to be in "#Dev" environment
    /// to leak the exceptions.
    /// </param>
    /// <returns>A list of one or more messages.</returns>
    public static List<UserMessage> GetUserMessages( this Exception ex,
                                                     CurrentCultureInfo? culture,
                                                     string? defaultGenericMessage = "An unhandled error occurred.",
                                                     byte depth = 0,
                                                     bool? leakAll = null )
    {
        Throw.CheckNotNullArgument( culture != null );
        List<UserMessage> messages = new List<UserMessage>();
        GetUserMessages( ex, messages.Add, culture, depth, defaultGenericMessage, leakAll );
        return messages;
    }

    /// <summary>
    /// Collects the <see cref="Exception.Message"/> texts recursively (following <see cref="Exception.InnerException"/>
    /// and <see cref="AggregateException.InnerExceptions"/>).
    /// <para>
    /// When a <see cref="MCException"/> is found, its <see cref="MCException.AsUserMessage"/> is used but for any other exception types
    /// (when <paramref name="leakAll"/> is true), we try to translate the exception's message by using
    /// the <see cref="CurrentCultureInfo.TranslationService"/> as if the text was in <see cref="NormalizedCultureInfo.CodeDefault"/>.
    /// If a "SHA." translation resource exists, then we can translate exception messages...
    /// </para>
    /// <para>
    /// If <paramref name="leakAll"/> is false and there is not MCException (we then have no messages to collect), the
    /// <paramref name="defaultGenericMessage"/>, when not null, is added.
    /// Set defaultGenericMessage to null if you have already added a more precise "head" message.
    /// </para>
    /// <para>
    /// The message's <see cref="UserMessage.Depth"/> reflects the exception tree.
    /// </para>
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <param name="collector">Message collector.</param>
    /// <param name="culture">The current culture. This can be null when no culture is available in the context.</param>
    /// <param name="depth">Depth of the root exception.</param>
    /// <param name="defaultGenericMessage">Message used when <paramref name="leakAll"/> is false and there is no <see cref="MCException"/> available.</param>
    /// <param name="leakAll">
    /// Whether all exceptions must be exposed or only the <see cref="MCException"/> ones.
    /// Defaults to <see cref="CoreApplicationIdentity.IsDevelopmentAndInitialized"/>: we want to be sure to be in "#Dev" environment
    /// to leak the exceptions.
    /// </param>
    /// <returns>The number of messages that have been added.</returns>
    public static int GetUserMessages( this Exception ex,
                                       Action<UserMessage> collector,
                                       CurrentCultureInfo? culture,
                                       byte depth = 0,
                                       string? defaultGenericMessage = "An unhandled error occurred.",
                                       bool? leakAll = null )
    {
        Throw.CheckNotNullArgument( culture != null );
        Throw.CheckNotNullArgument( collector );

        var all = leakAll ?? CoreApplicationIdentity.IsDevelopmentAndInitialized;

        if( all )
        {
            return Collect( collector, depth, ex, culture );
        }
        int mcOnly = CollectMCOnly( collector, depth, ex, culture );
        if( mcOnly == 0 && defaultGenericMessage != null )
        {
            AddUserMessage( collector, depth++, culture, defaultGenericMessage );
            mcOnly = 1;
        }
        return mcOnly;
    }

    static int CollectMCOnly( Action<UserMessage> collector, byte depth, Exception e, CurrentCultureInfo? culture )
    {
        int added = 0;
        if( e is AggregateException a )
        {
            // Aggregated exception's Message concatenates the inner messages: "base message (inner1) (inner2)..."
            // This is useless here (and unless with awful reflection, the base message is not reachable).
            // We prefer to lose the specific message here as AggregateException is almost always
            // the default.
            // One may create a MCAggregateException once if needed that will have MCString mesage.
            AddUserMessage( collector, depth++, culture, "One or more errors occurred." );
            foreach( var sub in a.InnerExceptions ) added += CollectMCOnly( collector, depth, sub, culture );
        }
        else
        {
            if( e is MCException mC )
            {
                added = 1;
                collector( mC.AsUserMessage().With( depth ) );
            }
            if( e.InnerException != null ) added += CollectMCOnly( collector, ++depth, e.InnerException, culture );
        }
        return added;
    }

    static int Collect( Action<UserMessage> collector, byte depth, Exception e, CurrentCultureInfo? culture )
    {
        int added;
        if( e is MCException mC )
        {
            added = 1;
            collector( mC.AsUserMessage().With( depth ) );
            if( mC.InnerException != null ) added += Collect( collector, ++depth, mC.InnerException, culture );
        }
        else if( e is AggregateException a )
        {
            added = 1;
            // See CollectMCOnly.
            AddUserMessage( collector, depth++, culture, "One or more errors occurred." );
            foreach( var sub in a.InnerExceptions ) added += Collect( collector, depth, sub, culture );
        }
        else
        {
            added = 1;
            AddUserMessage( collector, depth, culture, e.Message );
            if( e.InnerException != null ) added += Collect( collector, ++depth, e.InnerException, culture );
        }
        return added;

    }

    static void AddUserMessage( Action<UserMessage> collector, byte depth, CurrentCultureInfo? current, string text )
    {
        // We CANNOT know the content culture of text. Even by looking at the CultureInfo.Current[UI]Culture:
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
