using CK.Core;
using Microsoft.IO;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CK.Globalization.Tests;

[TestFixture]
public class GlobalizationJsonHelperTests
{
    [Test]
    public void SimpleUserMessage_tests()
    {
        var message = new SimpleUserMessage( UserMessageLevel.Warn, "The text.", 37 );
        using var mem = Util.RecyclableStreamManager.GetStream();
        using( var w = new Utf8JsonWriter( (IBufferWriter<byte>)mem ) )
        {
            GlobalizationJsonHelper.WriteAsJsonArray( w, ref message );
        }
        Encoding.UTF8.GetString( mem.GetReadOnlySequence() ).ShouldBe( """[8,"The text.",37]""" );
        var r = new Utf8JsonReader( mem.GetReadOnlySequence() );
        var messageBack = GlobalizationJsonHelper.ReadSimpleUserMessageFromJsonArray( ref r, IUtf8JsonReaderContext.Empty );
        messageBack.ShouldBe( message );
    }

    static UserMessage WriteTestUserMessage( RecyclableMemoryStream mem )
    {
        var frFR = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-FR" );
        frFR.Fallbacks[0].SetCachedTranslations( [("Test.Res", "S'il n'y pas {1}, alors il n'y a pas {0}.")] );

        var c1 = "Bird";
        var c2 = "Animal";
        var current = new CurrentCultureInfo( new TranslationService(), frFR );
        var message = UserMessage.Warn( current, $"Concept {c1} requires {c2}.", resName: "Test.Res" ).With( 37 );

        // First, the SimpleUserMessage form:
        using( var w = new Utf8JsonWriter( (IBufferWriter<byte>)mem, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } ) )
        {
            var simple = message.AsSimpleUserMessage();
            GlobalizationJsonHelper.WriteAsJsonArray( w, ref simple );
        }
        Encoding.UTF8.GetString( mem.GetReadOnlySequence() ).ShouldBe( """
            [8,"S'il n'y pas Animal, alors il n'y a pas Bird.",37]
            """ );
        // Now in the buffer, the full message:
        mem.SetLength( 0 );
        using( var w = new Utf8JsonWriter( (IBufferWriter<byte>)mem, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } ) )
        {
            GlobalizationJsonHelper.WriteAsJsonArray( w, ref message );
        }
        Encoding.UTF8.GetString( mem.GetReadOnlySequence() ).ShouldBe( """
            [8,37,"S'il n'y pas Animal, alors il n'y a pas Bird.","fr","Test.Res","Concept Bird requires Animal.","fr-fr",[8,4,22,6]]
            """ );
        return message;
    }

    [Test]
    public void UserMessage_tests()
    {
        using var mem = Util.RecyclableStreamManager.GetStream();
        UserMessage message = WriteTestUserMessage( mem );
        var r = new Utf8JsonReader( mem.GetReadOnlySequence() );
        var messageBack = GlobalizationJsonHelper.ReadUserMessageFromJsonArray( ref r, IUtf8JsonReaderContext.Empty );
        messageBack.ShouldBe( message );
    }

    [Test]
    public void SimpleUserMessage_can_be_read_back_from_the_UserMessage_serialized_array()
    {
        using var mem = Util.RecyclableStreamManager.GetStream();
        UserMessage message = WriteTestUserMessage( mem );

        var r = new Utf8JsonReader( mem.GetReadOnlySequence() );
        var simpleMessageBack = GlobalizationJsonHelper.ReadSimpleUserMessageFromJsonArray( ref r, IUtf8JsonReaderContext.Empty );
        simpleMessageBack.ShouldBe( message.AsSimpleUserMessage() );
    }
}
