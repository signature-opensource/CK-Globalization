using CK.Core;
using Shouldly;
using NUnit.Framework;
using System.Collections.Generic;
using static CK.Testing.MonitorTestHelper;

namespace CK.Globalization.Tests;


// Testing UserMessage tests MCString.
[TestFixture]
public class UserMessageAndMCStringTests
{
    [SetUp]
    [TearDown]
    public void ClearCache()
    {
        typeof( NormalizedCultureInfo )
            .GetMethod( "ClearCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static )!
            .Invoke( null, null );
    }

    [Test]
    public void default_UserMessage_and_SimpleUserMessage_serialization()
    {
        UserMessage def = default;
        CheckSerializations( def );
        def.Message.IsTranslatable.ShouldBeFalse( "default user message is not translatable." );
        SimpleUserMessage sDef = default;
        TestHelper.JsonIdempotenceCheck( sDef, ( w, m ) => GlobalizationJsonHelper.WriteAsJsonArray( w, ref m ), GlobalizationJsonHelper.ReadSimpleUserMessageFromJsonArray );
    }

    [Test]
    public void non_translatable_MCString_is_Perfect_since_it_cannot_be_translated()
    {
        var s = MCString.CreateNonTranslatable( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "ar-tn" ), "I'm a non translatable string." );
        s.IsTranslatable.ShouldBeFalse();
        s.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
    }

    [Test]
    public void UserMessage_Error()
    {
        var aaCulture = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "aa" );

        var m1 = UserMessage.Error( aaCulture, "text" );
        m1.IsTranslationWelcome.ShouldBeTrue();
        m1.Message.Text.ShouldBe( "text" );
        m1.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m1.Level.ShouldBe( UserMessageLevel.Error );
        m1.ResName.ShouldStartWith( "SHA." );
        CheckSerializations( m1 );

        var m2 = UserMessage.Error( aaCulture, "text", "Res.Name" );
        m2.IsTranslationWelcome.ShouldBeTrue();
        m2.Message.Text.ShouldBe( "text" );
        m2.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m2.Level.ShouldBe( UserMessageLevel.Error );
        m2.ResName.ShouldBe( "Res.Name" );
        CheckSerializations( m2 );

        int v = 3712;

        var m3 = UserMessage.Error( aaCulture, $"Hello {v}!" );
        m3.IsTranslationWelcome.ShouldBeTrue();
        m3.Message.Text.ShouldBe( "Hello 3712!" );
        m3.Message.CodeString.FormattedString.GetFormatString().ShouldBe( "Hello {0}!" );
        m3.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m3.Level.ShouldBe( UserMessageLevel.Error );
        m3.ResName.ShouldStartWith( "SHA." );
        CheckSerializations( m3 );

        var m4 = UserMessage.Error( aaCulture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
        m4.IsTranslationWelcome.ShouldBeTrue();
        m4.Message.Text.ShouldBe( "3712 Goodbye 3712" );
        m4.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m4.Message.CodeString.TargetCulture.ShouldBeSameAs( aaCulture );
        m4.Level.ShouldBe( UserMessageLevel.Error );
        m4.ResName.ShouldBe( "Policy.Salutation" );
        CheckSerializations( m4 );

        aaCulture.SetCachedTranslations( new Dictionary<string, string> { { "Policy.Salutation", "AH! {0} H'lo {1}" } } );
        var current = new CurrentCultureInfo( new TranslationService(), aaCulture );

        var m5 = UserMessage.Error( current, $"{v} Goodbye {v}", "Policy.Salutation" );
        m5.IsTranslationWelcome.ShouldBeFalse();
        m5.Message.Text.ShouldBe( "AH! 3712 H'lo 3712" );
        m5.Message.FormatCulture.ShouldBeSameAs( aaCulture );
        m5.Message.CodeString.TargetCulture.ShouldBeSameAs( aaCulture );
        m5.Level.ShouldBe( UserMessageLevel.Error );
        m5.ResName.ShouldBe( "Policy.Salutation" );
        CheckSerializations( m5 );

    }

    [Test]
    public void UserMessage_Warn()
    {
        var aaCulture = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "aa" );

        var m1 = UserMessage.Warn( aaCulture, "text" );
        m1.IsTranslationWelcome.ShouldBeTrue();
        m1.Message.Text.ShouldBe( "text" );
        m1.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m1.Level.ShouldBe( UserMessageLevel.Warn );
        m1.ResName.ShouldStartWith( "SHA." );
        CheckSerializations( m1 );

        var m2 = UserMessage.Warn( aaCulture, "text", "Res.Name" );
        m2.IsTranslationWelcome.ShouldBeTrue();
        m2.Message.Text.ShouldBe( "text" );
        m2.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m2.Message.CodeString.FormattedString.GetFormatString().ShouldBe( m2.Text );
        m2.Level.ShouldBe( UserMessageLevel.Warn );
        m2.ResName.ShouldBe( "Res.Name" );
        CheckSerializations( m2 );

        int v = 3712;

        var m3 = UserMessage.Warn( aaCulture, $"Hello {v}!" );
        m3.IsTranslationWelcome.ShouldBeTrue();
        m3.Message.Text.ShouldBe( "Hello 3712!" );
        m3.Message.CodeString.FormattedString.GetFormatString().ShouldBe( "Hello {0}!" );
        m3.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m3.Level.ShouldBe( UserMessageLevel.Warn );
        m3.ResName.ShouldStartWith( "SHA." );
        CheckSerializations( m3 );

        var m4 = UserMessage.Warn( aaCulture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
        m4.IsTranslationWelcome.ShouldBeTrue();
        m4.Message.Text.ShouldBe( "3712 Goodbye 3712" );
        m4.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m4.Level.ShouldBe( UserMessageLevel.Warn );
        m4.ResName.ShouldBe( "Policy.Salutation" );
        CheckSerializations( m4 );

        var name = "Albert";
        aaCulture.SetCachedTranslations( new Dictionary<string, string> { { "Policy.Salutation", "Hello {0}." } } );
        var current = new CurrentCultureInfo( new TranslationService(), aaCulture );

        var m5 = UserMessage.Warn( current, $"Hi {name}!", "Policy.Salutation" );
        m5.IsTranslationWelcome.ShouldBeFalse();
        m5.Message.Text.ShouldBe( "Hello Albert." );
        m5.Message.FormatCulture.ShouldBeSameAs( aaCulture );
        m5.Level.ShouldBe( UserMessageLevel.Warn );
        m5.ResName.ShouldBe( "Policy.Salutation" );
        CheckSerializations( m5 );
    }

    [Test]
    public void SimpleUserMessage_can_be_deconstruct()
    {
        var (l1, m1, d1) = new SimpleUserMessage();
        l1.ShouldBe( UserMessageLevel.None );
        m1.ShouldBeEmpty();
        d1.ShouldBe( 0 );

        var (l2, m2, d2) = new SimpleUserMessage( UserMessageLevel.Info, "Hop!", 42 );
        l2.ShouldBe( UserMessageLevel.Info );
        m2.ShouldBe( "Hop!" );
        d2.ShouldBe( 42 );
    }

    [Test]
    public void UserMessage_Info()
    {
        var aaCulture = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "aa" );

        var m1 = UserMessage.Info( aaCulture, "text" );
        m1.IsTranslationWelcome.ShouldBeTrue();
        m1.Message.Text.ShouldBe( "text" );
        m1.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m1.Level.ShouldBe( UserMessageLevel.Info );
        m1.ResName.ShouldStartWith( "SHA." );
        CheckSerializations( m1 );

        var m2 = UserMessage.Info( aaCulture, "text", "Res.Name" );
        m2.IsTranslationWelcome.ShouldBeTrue();
        m2.Message.Text.ShouldBe( "text" );
        m2.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m2.Level.ShouldBe( UserMessageLevel.Info );
        m2.ResName.ShouldBe( "Res.Name" );
        CheckSerializations( m2 );

        int v = 3712;

        var m3 = UserMessage.Info( aaCulture, $"Hello {v}!" );
        m3.IsTranslationWelcome.ShouldBeTrue();
        m3.Message.Text.ShouldBe( "Hello 3712!" );
        m3.Message.CodeString.FormattedString.GetFormatString().ShouldBe( "Hello {0}!" );
        m3.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m3.Level.ShouldBe( UserMessageLevel.Info );
        m3.ResName.ShouldStartWith( "SHA." );
        CheckSerializations( m3 );

        var m4 = UserMessage.Info( aaCulture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
        m4.IsTranslationWelcome.ShouldBeTrue();
        m4.Message.Text.ShouldBe( "3712 Goodbye 3712" );
        m4.Message.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        m4.Level.ShouldBe( UserMessageLevel.Info );
        m4.ResName.ShouldBe( "Policy.Salutation" );
        CheckSerializations( m4 );

        aaCulture.SetCachedTranslations( new Dictionary<string, string> { { "Policy.Salutation", "AH! {0} H'lo {1}" } } );
        var current = new CurrentCultureInfo( new TranslationService(), aaCulture );

        var m5 = UserMessage.Info( current, $"{v} Goodbye {v}", "Policy.Salutation" );
        m5.IsTranslationWelcome.ShouldBeFalse();
        m5.Message.Text.ShouldBe( "AH! 3712 H'lo 3712" );
        m5.Message.FormatCulture.ShouldBeSameAs( aaCulture );
        m5.Level.ShouldBe( UserMessageLevel.Info );
        m5.ResName.ShouldBe( "Policy.Salutation" );
        CheckSerializations( m5 );
    }

    static void CheckSerializations( UserMessage m )
    {
        var cS = m.DeepClone();
        cS.Message.Text.ShouldBe( m.Message.Text );
        cS.Message.TranslationQuality.ShouldBe( m.Message.TranslationQuality );
        cS.ResName.ShouldBe( m.ResName );
        cS.Level.ShouldBe( m.Level );
        cS.ToString().ShouldBe( m.ToString() );

        var cV = SimpleSerializable.DeepCloneVersioned( m );
        cV.Message.Text.ShouldBe( m.Message.Text );
        cV.Message.TranslationQuality.ShouldBe( m.Message.TranslationQuality );
        cV.ResName.ShouldBe( m.ResName );
        cV.Level.ShouldBe( m.Level );
        cV.ToString().ShouldBe( m.ToString() );

        TestHelper.JsonIdempotenceCheck( m, ( w, m ) => GlobalizationJsonHelper.WriteAsJsonArray( w, ref m ), GlobalizationJsonHelper.ReadUserMessageFromJsonArray );
        TestHelper.JsonIdempotenceCheck( m.Message, GlobalizationJsonHelper.WriteAsJsonArray, GlobalizationJsonHelper.ReadMCStringFromJsonArray );

        TestHelper.JsonIdempotenceCheck( m.AsSimpleUserMessage(), ( w, m ) => GlobalizationJsonHelper.WriteAsJsonArray( w, ref m ), GlobalizationJsonHelper.ReadSimpleUserMessageFromJsonArray );
    }
}
