using CK.Core;
using CK.Testing;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CK.Core
{

    /// <summary>
    /// Useful delegate for typed read from a <see cref="Utf8JsonReader"/>.
    /// </summary>
    /// <typeparam name="T">The type to read.</typeparam>
    /// <param name="r">The reader.</param>
    /// <returns>The read instance or null.</returns>
    public delegate T? Utf8JsonReaderDelegate<T>( ref Utf8JsonReader r );

}

namespace CK.Testing
{ 
    public static class TestHelperExtension
    {
        /// <summary>
        /// Writes a <typeparamref name="T"/>, reads it back and writes the result, ensuring that
        /// the two json string are equals. Throws a <see cref="CKException"/> if the texts differ.
        /// </summary>
        /// <typeparam name="T">The type of the instance to check.</typeparam>
        /// <param name="o">The instance.</param>
        /// <param name="write">Writer function. This is called twice unless the first write or the read fails.</param>
        /// <param name="read">Reader function is called once.</param>
        /// <param name="jsonText">Optional hook that provides the Json text.</param>
        /// <returns>A clone of <paramref name="o"/>.</returns>
        public static T JsonIdempotenceCheck<T>( this IBasicTestHelper @this,
                                                 T o,
                                                 Action<Utf8JsonWriter, T> write,
                                                 Utf8JsonReaderDelegate<T> read,
                                                 Action<string>? jsonText = null )
        {
            using( var m = (RecyclableMemoryStream)Util.RecyclableStreamManager.GetStream() )
            using( Utf8JsonWriter w = new Utf8JsonWriter( (IBufferWriter<byte>)m ) )
            {
                write( w, o );
                w.Flush();
                string? text1 = Encoding.UTF8.GetString( m.GetReadOnlySequence() );
                jsonText?.Invoke( text1 );
                var reader = new Utf8JsonReader( m.GetReadOnlySequence() );
                var oBack = read( ref reader );
                if( oBack == null )
                {
                    Throw.CKException( $"A null has been read back from '{text1}' for a non null instance of '{typeof( T ).ToCSharpName()}'." );
                }
                string? text2 = null;
                m.Position = 0;
                using( var w2 = new Utf8JsonWriter( (IBufferWriter<byte>)m ) )
                {
                    write( w2, oBack );
                    w2.Flush();
                    text2 = Encoding.UTF8.GetString( m.GetReadOnlySequence() );
                }
                if( text1 != text2 )
                {
                    Throw.CKException( $"""
                            Json idempotence failure between first write:
                            {text1}

                            And second write of the read back {typeof( T ).ToCSharpName()} instance:
                            {text2}

                            """ );
                }
                return oBack;
            }
        }
    }
}
