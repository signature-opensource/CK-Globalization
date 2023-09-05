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
        static void ReadEndArray( ref Utf8JsonReader r, string typeName )
        {
            if( r.TokenType != JsonTokenType.EndArray ) throw new JsonException( $"Expected {typeName}'s end array." );
            r.Read();
        }

        static void ReadStartArray( ref Utf8JsonReader r, string typeName )
        {
            if( r.TokenType == JsonTokenType.None ) r.Read();
            if( r.TokenType != JsonTokenType.StartArray ) throw new JsonException( $"Expected {typeName} array." );
            r.Read();
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
                w.WriteNumberValue( (int)v.Depth );
                w.WriteStringValue( v.Message );
            }
        }

        public static SimpleUserMessage ReadSimpleUserMessageFromJsonArray( ref Utf8JsonReader r )
        {
            ReadStartArray( ref r, "UserMessage" );
            var s = ReadSimpleUserMessageFromJsonArrayContent( ref r );
            ReadEndArray( ref r, "UserMessage" );
            return s;
        }

        public static SimpleUserMessage ReadSimpleUserMessageFromJsonArrayContent( ref Utf8JsonReader r )
        {
            var level = (UserMessageLevel)r.GetInt32();
            r.Read();
            if( level == UserMessageLevel.None ) return default;
            var depth = (byte)r.GetInt32();
            r.Read();
            var s = r.GetString() ?? string.Empty;
            r.Read();
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

        public static UserMessage ReadUserMessageFromJsonArray( ref Utf8JsonReader r )
        {
            ReadStartArray( ref r, "UserMessage" );
            var s = ReadUserMessageFromJsonArrayContent( ref r );
            ReadEndArray( ref r, "UserMessage" );
            return s;
        }

        public static UserMessage ReadUserMessageFromJsonArrayContent( ref Utf8JsonReader r )
        {
            var level = (UserMessageLevel)r.GetInt32();
            r.Read();
            if( level == UserMessageLevel.None ) return default;
            var depth = (byte)r.GetInt32();
            r.Read();
            var s = ReadMCStringFromJsonArrayContent( ref r );
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

        public static MCString ReadMCStringFromJsonArray( ref Utf8JsonReader r )
        {
            ReadStartArray( ref r, "MCString" );
            var s = ReadMCStringFromJsonArrayContent( ref r );
            ReadEndArray( ref r, "MCString" );
            return s;
        }

        public static MCString ReadMCStringFromJsonArrayContent( ref Utf8JsonReader r )
        {
            var text = r.GetString() ?? string.Empty;
            r.Read();
            var formatCulture = r.GetString() ?? string.Empty;
            r.Read();
            var c = ReadCodeStringFromJsonArrayContent( ref r );
            return MCString.CreateFromProperties( text, c, NormalizedCultureInfo.GetNormalizedCultureInfo( formatCulture ) );
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

        public static CodeString ReadCodeStringFromJsonArray( ref Utf8JsonReader r )
        {
            ReadStartArray( ref r, "CodeString" );
            var s = ReadCodeStringFromJsonArrayContent( ref r );
            ReadEndArray( ref r, "CodeString" );
            return s;
        }

        public static CodeString ReadCodeStringFromJsonArrayContent( ref Utf8JsonReader r )
        {
            var resName = r.GetString() ?? string.Empty;
            r.Read();
            var f = ReadFormattedStringFromJsonArrayContent( ref r );
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

        public static FormattedString ReadFormattedStringFromJsonArray( ref Utf8JsonReader r )
        {
            ReadStartArray( ref r, "FormattedString" );
            var f = ReadFormattedStringFromJsonArrayContent( ref r );
            ReadEndArray( ref r, "FormattedString" );
            return f;
        }

        public static FormattedString ReadFormattedStringFromJsonArrayContent( ref Utf8JsonReader r )
        {
            var text = r.GetString() ?? String.Empty;
            r.Read();
            var cultureName = r.GetString() ?? String.Empty;
            r.Read();
            ReadStartArray( ref r, "FormattedString's Placeholders" );
            var placeholders = new List<(int, int)>();
            while( r.TokenType == JsonTokenType.Number )
            {
                var start = r.GetInt32();
                r.Read();
                var length = r.GetInt32();
                r.Read();
                placeholders.Add( (start, length) );
            }
            ReadEndArray( ref r, "FormattedString's Placeholders" );
            return FormattedString.CreateFromProperties( text, placeholders.ToArray(), ExtendedCultureInfo.GetExtendedCultureInfo( cultureName ) );
        }

        #endregion
    }
}
