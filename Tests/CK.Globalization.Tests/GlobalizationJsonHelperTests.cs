using CK.Core;
using Shouldly;
using Microsoft.IO;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Buffers;
using System.Text;
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
        var current = new CurrentCultureInfo( new TranslationService(), NormalizedCultureInfo.CodeDefault );
        var message = UserMessage.Warn( current, $"The {nameof(mem)} text with {current} placeholders.", resName: "Test.Res" ).With( 37 );
        using( var w = new Utf8JsonWriter( (IBufferWriter<byte>)mem ) )
        {
            GlobalizationJsonHelper.WriteAsJsonArray( w, ref message );
        }
        Encoding.UTF8.GetString( mem.GetReadOnlySequence() ).ShouldBe( """
            [8,37,"The mem text with CK.Core.CurrentCultureInfo placeholders.","en","Test.Res","The mem text with CK.Core.CurrentCultureInfo placeholders.","en",[4,3,18,26]]
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
