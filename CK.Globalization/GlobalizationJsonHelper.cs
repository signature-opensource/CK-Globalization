using System;
using System.Collections.Generic;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Core;

/// <summary>
/// Static helper that serializes SimpleUserMessage, UserMessage, MCString, CodeString and FormattedString as JSON arrays.
/// </summary>
public static partial class GlobalizationJsonHelper
{
    static void ReadEndArray( ref Utf8JsonReader r, IUtf8JsonReaderContext context, string typeName )
    {
        if( r.TokenType != JsonTokenType.EndArray ) r.ThrowExpectedJsonException( $"{typeName}'s end array" );
        r.ReadWithMoreData( context );
    }

    static void ReadStartArray( ref Utf8JsonReader r, IUtf8JsonReaderContext context, string typeName )
    {
        if( r.TokenType == JsonTokenType.None ) r.ReadWithMoreData( context );
        if( r.TokenType != JsonTokenType.StartArray ) r.ThrowExpectedJsonException( $"{typeName} array" );
        r.ReadWithMoreData( context );
    }

    /// <summary>
    /// Use <see cref="IMCDeserializationOptions"/> that should be supported by <paramref name="context"/> to resolve
    /// a culture.
    /// <para>
    /// To resolve a <see cref="NormalizedCultureInfo"/>, use <see cref="ExtendedCultureInfo.PrimaryCulture"/> on this result.
    /// </para>
    /// </summary>
    /// <param name="name">The desrialized culture name.</param>
    /// <param name="context">The context.</param>
    /// <returns>The culture.</returns>
    public static ExtendedCultureInfo ResolveCulture( string name, IUtf8JsonReaderContext context )
    {
        if( context is IMCDeserializationOptions o )
        {
            if( o.CreateUnexistingCultures ) return NormalizedCultureInfo.EnsureNormalizedCultureInfo( name );
            return ExtendedCultureInfo.All.FindBestExtendedCultureInfo( name, o.DefaultCulture ?? NormalizedCultureInfo.CodeDefault );
        }
        return ExtendedCultureInfo.All.FindBestExtendedCultureInfo( name, NormalizedCultureInfo.CodeDefault );
    }

    #region SimpleUserMessage

    /// <summary>
    /// Writes the ultimate simplified form of a <see cref="SimpleUserMessage"/>:
    /// "Level - Text" string value where the Depth indents the Text (uses <see cref="SimpleUserMessage.ToString()"/>.
    /// <para>
    /// <see cref="ReadSimpleUserMessage(ref Utf8JsonReader, IUtf8JsonReaderContext)"/> can read it back.
    /// </para>
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="v">The value to write.</param>
    public static void WriteAsString( Utf8JsonWriter w, ref readonly SimpleUserMessage v )
    {
        w.WriteStringValue( v.ToString() );
    }

    /// <summary>
    /// Writes a 3-cells Json array [level,text,depth].
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="v">The value to write.</param>
    public static void WriteAsJsonArray( Utf8JsonWriter w, ref readonly SimpleUserMessage v )
    {
        w.WriteStartArray();
        WriteJsonArrayContent( w, in v );
        w.WriteEndArray();
    }

    /// <inheritdoc cref="WriteAsJsonArray(Utf8JsonWriter, SimpleUserMessage)"/>.
    public static void WriteJsonArrayContent( Utf8JsonWriter w, ref readonly SimpleUserMessage v )
    {
        w.WriteNumberValue( (int)v.Level );
        if( v.Level != UserMessageLevel.None )
        {
            w.WriteStringValue( v.Message );
            w.WriteNumberValue( (int)v.Depth );
        }
    }

    /// <summary>
    /// Reads a SimpleUserMessage either from a string, a 3-cells array or from the bigger 8-cells array of a UserMessage.
    /// The string form must have been written by <see cref="WriteAsString(Utf8JsonWriter, ref readonly SimpleUserMessage)"/>.
    /// <para>
    public static SimpleUserMessage ReadSimpleUserMessage( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        if( r.TokenType == JsonTokenType.None ) r.ReadWithMoreData( context );
        if( r.TokenType == JsonTokenType.StartArray )
        {
            r.ReadWithMoreData( context );
            var m = ReadSimpleUserMessageFromJsonArrayContent( ref r, context );
            ReadEndArray( ref r, context, "SimpleUserMessage" );
            return m;
        }
        if( r.TokenType != JsonTokenType.String )
        {
            r.ThrowExpectedJsonException( $"string or SimpleUserMessage array" );
        }
        var text = r.GetString() ?? string.Empty;
        r.ReadWithMoreData( context );
        if( !SimpleUserMessage.TryParse( text, null, out var message ) )
        {
            r.ThrowExpectedJsonException( $"Invalid SimpleUserMessage string" );
        }
        return message;
    }

    /// <summary>
    /// Reads a SimpleUserMessage from a 3-cells array or from the bigger 8-cells array of a UserMessage.
    /// <para>
    /// The trick is that the regular 3-cells is [level,text,depth] and the UserMessage's one is
    /// [level,depth,text,...]: when the 2nd position is a number, we know that this is a UserMessage
    /// we can skip the remaining [..., MCString formatCulture, CodeString resName, FormattedString text, FormattedString cultureName,
    /// FormattedString's Placeholders array].
    /// </para>
    /// </summary>
    /// <param name="r">The reader.</param>
    /// <param name="context">The reader context (can be <see cref="IUtf8JsonReaderContext.Empty"/>).</param>
    /// <returns>The simple message.</returns>
    public static SimpleUserMessage ReadSimpleUserMessageFromJsonArray( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        ReadStartArray( ref r, context, "SimpleUserMessage" );
        var s = ReadSimpleUserMessageFromJsonArrayContent( ref r, context );
        ReadEndArray( ref r, context, "SimpleUserMessage" );
        return s;
    }

    /// <inheritdoc cref="ReadSimpleUserMessageFromJsonArray(ref Utf8JsonReader, IUtf8JsonReaderContext)"/>
    public static SimpleUserMessage ReadSimpleUserMessageFromJsonArrayContent( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        r.SkipComments( context );
        var level = (UserMessageLevel)r.GetInt32();
        r.ReadWithMoreData( context );
        if( level == UserMessageLevel.None ) return default;
        r.SkipComments( context );

        byte depth;
        string text;
        // If we have a number at the 2nd position, then it is the UserMessage's Depth.
        if( r.TokenType == JsonTokenType.Number )
        {
            depth = (byte)r.GetInt32();
            r.ReadWithMoreData( context );
            r.SkipComments( context );
            text = r.GetString() ?? string.Empty;
            // Skip the MCString formatCulture.
            r.ReadWithMoreData( context ); r.SkipWithMoreData( context );
            // Skip the CodeString resName.
            r.ReadWithMoreData( context ); r.SkipWithMoreData( context );
            // Skip the FormattedString text.
            r.ReadWithMoreData( context ); r.SkipWithMoreData( context );
            // Skip the FormattedString cultureName.
            r.ReadWithMoreData( context ); r.SkipWithMoreData( context );
            // Skip the FormattedString's Placeholders array.
            r.ReadWithMoreData( context ); r.SkipWithMoreData( context );
        }
        else
        {
            text = r.GetString() ?? string.Empty;
            r.ReadWithMoreData( context );
            r.SkipComments( context );
            depth = (byte)r.GetInt32();
            r.ReadWithMoreData( context );
        }
        return new SimpleUserMessage( level, text, depth );
    }
    #endregion

    #region UserMessage
    /// <summary>
    /// Writes a 8-cells Json array.
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="v">The value to write.</param>
    public static void WriteAsJsonArray( Utf8JsonWriter w, ref readonly UserMessage v )
    {
        w.WriteStartArray();
        WriteJsonArrayContent( w, in v );
        w.WriteEndArray();
    }

    /// <summary>
    /// Writes a UserMessage as a full 8-cells array or as a <see cref="SimpleUserMessage"/> (3-cells).
    /// </summary>
    /// <param name="w">The writer.</param>
    /// <param name="v">The value to write.</param>
    /// <param name="asSimpleUserMessage">Whether to write a SimpleUserMessage.</param>
    public static void WriteAsJsonArray( Utf8JsonWriter w, ref readonly UserMessage v, bool asSimpleUserMessage )
    {
        w.WriteStartArray();
        if( asSimpleUserMessage ) 
        {
            var s = v.AsSimpleUserMessage();
            WriteJsonArrayContent( w, in s );
        }
        else
        {
            WriteJsonArrayContent( w, in v );
        }
        w.WriteEndArray();
    }

    /// <inheritdoc cref="WriteAsJsonArray(Utf8JsonWriter, UserMessage)"/>
    public static void WriteJsonArrayContent( Utf8JsonWriter w, ref readonly UserMessage v )
    {
        w.WriteNumberValue( (int)v.Level );
        if( v.Level != UserMessageLevel.None )
        {
            w.WriteNumberValue( (int)v.Depth );
            WriteJsonArrayContent( w, v.Message );
        }
    }

    /// <summary>
    /// Reads a UserMessage from a 8-cells array.
    /// </summary>
    /// <param name="r">The reader.</param>
    /// <param name="context">The reader context (can be <see cref="IUtf8JsonReaderContext.Empty"/>).</param>
    /// <returns>The message.</returns>
    public static UserMessage ReadUserMessageFromJsonArray( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        ReadStartArray( ref r, context, "UserMessage" );
        var s = ReadUserMessageFromJsonArrayContent( ref r, context );
        ReadEndArray( ref r, context, "UserMessage" );
        return s;
    }

    /// <inheritdoc cref="ReadUserMessageFromJsonArray(ref Utf8JsonReader, IUtf8JsonReaderContext)"/>
    public static UserMessage ReadUserMessageFromJsonArrayContent( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        r.SkipComments( context );
        var level = (UserMessageLevel)r.GetInt32();
        r.ReadWithMoreData( context );
        if( level == UserMessageLevel.None ) return default;
        r.SkipComments( context );
        var depth = (byte)r.GetInt32();
        r.ReadWithMoreData( context );
        var s = ReadMCStringFromJsonArrayContent( ref r, context );
        return new UserMessage( level, s, depth );
    }

    #endregion

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    #region MCString
    public static void WriteAsJsonArray( Utf8JsonWriter w, MCString v )
    {
        w.WriteStartArray();
        WriteJsonArrayContent( w, v );
        w.WriteEndArray();
    }

    public static void WriteJsonArrayContent( Utf8JsonWriter w, MCString v )
    {
        w.WriteStringValue( v.Text );
        w.WriteStringValue( v.FormatCulture.Name );
        WriteJsonArrayContent( w, v.CodeString );
    }

    public static MCString ReadMCStringFromJsonArray( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        ReadStartArray( ref r, context, "MCString" );
        var s = ReadMCStringFromJsonArrayContent( ref r, context );
        ReadEndArray( ref r, context, "MCString" );
        return s;
    }

    public static MCString ReadMCStringFromJsonArrayContent( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        r.SkipComments( context );
        var text = r.GetString() ?? string.Empty;
        r.ReadWithMoreData( context );
        var formatCulture = r.GetString() ?? string.Empty;
        r.ReadWithMoreData( context );
        var c = ReadCodeStringFromJsonArrayContent( ref r, context );
        return MCString.CreateFromProperties( text, c, ResolveCulture( formatCulture, context ).PrimaryCulture );
    }

    #endregion

    #region CodeString
    public static void WriteAsJsonArray( Utf8JsonWriter w, CodeString v )
    {
        w.WriteStartArray();
        WriteJsonArrayContent( w, v );
        w.WriteEndArray();
    }

    public static void WriteJsonArrayContent( Utf8JsonWriter w, CodeString v )
    {
        w.WriteStringValue( v.ResName );
        WriteJsonArrayContent( w, in v.FormattedString );
    }

    public static CodeString ReadCodeStringFromJsonArray( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        ReadStartArray( ref r, context, "CodeString" );
        var s = ReadCodeStringFromJsonArrayContent( ref r, context );
        ReadEndArray( ref r, context, "CodeString" );
        return s;
    }

    public static CodeString ReadCodeStringFromJsonArrayContent( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        r.SkipComments( context );
        var resName = r.GetString() ?? string.Empty;
        r.ReadWithMoreData( context );
        var f = ReadFormattedStringFromJsonArrayContent( ref r, context );
        return CodeString.CreateFromProperties( f, resName );
    }

    #endregion

    #region FormattedString

    public static void WriteAsJsonArray( Utf8JsonWriter w, ref readonly FormattedString v )
    {
        w.WriteStartArray();
        WriteJsonArrayContent( w, in v );
        w.WriteEndArray();
    }

    public static void WriteJsonArrayContent( Utf8JsonWriter w, ref readonly FormattedString v )
    {
        w.WriteStringValue( v.Text );
        w.WriteStringValue( v.Culture.Name );
        w.WriteStartArray();
        foreach( var p in v.Placeholders )
        {
            w.WriteNumberValue( p.Start );
            w.WriteNumberValue( p.Length );
        }
        w.WriteEndArray();
    }

    public static FormattedString ReadFormattedStringFromJsonArray( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        ReadStartArray( ref r, context, "FormattedString" );
        var f = ReadFormattedStringFromJsonArrayContent( ref r, context );
        ReadEndArray( ref r, context, "FormattedString" );
        return f;
    }

    public static FormattedString ReadFormattedStringFromJsonArrayContent( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
    {
        r.SkipComments( context );
        var text = r.GetString() ?? String.Empty;
        r.ReadWithMoreData( context );
        r.SkipComments( context );
        var cultureName = r.GetString() ?? String.Empty;
        r.ReadWithMoreData( context );
        r.SkipComments( context );
        ReadStartArray( ref r, context, "FormattedString's Placeholders" );
        var placeholders = new List<(int, int)>();
        while( r.TokenType == JsonTokenType.Number )
        {
            r.SkipComments( context );
            var start = r.GetInt32();
            r.ReadWithMoreData( context );
            r.SkipComments( context );
            var length = r.GetInt32();
            r.ReadWithMoreData( context );
            r.SkipComments( context );
            placeholders.Add( (start, length) );
        }
        ReadEndArray( ref r, context, "FormattedString's Placeholders" );
        return FormattedString.CreateFromProperties( text, placeholders.ToArray(), ResolveCulture( cultureName, context ) );
    }

    #endregion
}
