using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CK.Core
{
    /// <summary>
    /// Static helper that serializes SimpleUserMessage, UserMessage, MCString, CodeString and FormattedString as JSON arrays.
    /// </summary>
    public static class GlobalizationJsonHelper
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        #region SimpleUserMessage
        public static void WriteAsJsonArray( Utf8JsonWriter w, SimpleUserMessage v )
        {
            w.WriteStartArray();
            WriteJsonArrayContent( w, v );
            w.WriteEndArray();
        }

        public static void WriteJsonArrayContent( Utf8JsonWriter w, SimpleUserMessage v )
        {
            w.WriteNumberValue( (int)v.Level );
            if( v.Level != UserMessageLevel.None )
            {
                w.WriteStringValue( v.Message );
                w.WriteNumberValue( (int)v.Depth );
            }
        }

        public static SimpleUserMessage ReadSimpleUserMessageFromJsonArray( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
        {
            ReadStartArray( ref r, context, "SimpleUserMessage" );
            var s = ReadSimpleUserMessageFromJsonArrayContent( ref r, context );
            ReadEndArray( ref r, context, "SimpleUserMessage" );
            return s;
        }

        public static SimpleUserMessage ReadSimpleUserMessageFromJsonArrayContent( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
        {
            r.SkipComments( context );
            var level = (UserMessageLevel)r.GetInt32();
            r.ReadWithMoreData( context );
            if( level == UserMessageLevel.None ) return default;
            r.SkipComments( context );
            var s = r.GetString() ?? string.Empty;
            r.ReadWithMoreData( context );
            r.SkipComments( context );
            var depth = (byte)r.GetInt32();
            r.ReadWithMoreData( context );
            return new SimpleUserMessage( level, s, depth );
        }
        #endregion

        #region UserMessage
        public static void WriteAsJsonArray( Utf8JsonWriter w, UserMessage v )
        {
            w.WriteStartArray();
            WriteJsonArrayContent( w, v );
            w.WriteEndArray();
        }

        public static void WriteJsonArrayContent( Utf8JsonWriter w, UserMessage v )
        {
            w.WriteNumberValue( (int)v.Level );
            if( v.Level != UserMessageLevel.None )
            {
                w.WriteNumberValue( (int)v.Depth );
                WriteJsonArrayContent( w, v.Message );
            }
        }

        public static UserMessage ReadUserMessageFromJsonArray( ref Utf8JsonReader r, IUtf8JsonReaderContext context )
        {
            ReadStartArray( ref r, context, "UserMessage" );
            var s = ReadUserMessageFromJsonArrayContent( ref r, context );
            ReadEndArray( ref r, context, "UserMessage" );
            return s;
        }

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
            return MCString.CreateFromProperties( text, c, NormalizedCultureInfo.EnsureNormalizedCultureInfo( formatCulture ) );
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
            WriteJsonArrayContent( w, v.FormattedString );
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

        public static void WriteAsJsonArray( Utf8JsonWriter w, FormattedString v )
        {
            w.WriteStartArray();
            WriteJsonArrayContent( w, v );
            w.WriteEndArray();
        }

        public static void WriteJsonArrayContent( Utf8JsonWriter w, in FormattedString v )
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
            return FormattedString.CreateFromProperties( text, placeholders.ToArray(), ExtendedCultureInfo.EnsureExtendedCultureInfo( cultureName ) );
        }

        #endregion
    }
}
