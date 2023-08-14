using System;
using System.Collections.Generic;
using System.Text.Json;

namespace CK.Core
{
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

        #region ResultMessage
        public static void WriteAsJsonArray( Utf8JsonWriter w, ResultMessage v )
        {
            w.WriteStartArray();
            WriteJsonArrayContent( w, v );
            w.WriteEndArray();
        }

        public static void WriteJsonArrayContent( Utf8JsonWriter w, ResultMessage v )
        {
            w.WriteNumberValue( (int)v.Level );
            if( v.Level != ResultMessageLevel.None ) WriteJsonArrayContent( w, v.Message );
        }

        public static ResultMessage ReadResultMessageFromJsonArray( ref Utf8JsonReader r )
        {
            ReadStartArray( ref r, "ResultMessage" );
            var s = ReadResultMessageFromJsonArrayContent( ref r );
            ReadEndArray( ref r, "ResultMessage" );
            return s;
        }

        public static ResultMessage ReadResultMessageFromJsonArrayContent( ref Utf8JsonReader r )
        {
            var level = (ResultMessageLevel)r.GetInt32();
            r.Read();
            if( level == ResultMessageLevel.None ) return default;
            var s = ReadMCStringFromJsonArrayContent( ref r );
            return new ResultMessage( level, s );
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
