using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using static CK.Testing.MonitorTestHelper;

namespace CK.Globalization.Tests
{

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
            def.Message.IsTranslatable.Should().BeFalse( "default user message is not translatable." );
            SimpleUserMessage sDef = default;
            TestHelper.JsonIdempotenceCheck( sDef, GlobalizationJsonHelper.WriteAsJsonArray, GlobalizationJsonHelper.ReadSimpleUserMessageFromJsonArray );
        }

        [Test]
        public void non_translatable_MCString_is_Perfect_since_it_cannot_be_translated()
        {
            var s = MCString.CreateNonTranslatable( NormalizedCultureInfo.EnsureNormalizedCultureInfo("ar-tn"), "I'm a non translatable string." );
            s.IsTranslatable.Should().BeFalse();
            s.TranslationQuality.Should().Be( MCString.Quality.Perfect );
        }

        [Test]
        public void UserMessage_Error()
        {
            var aaCulture = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "aa" );

            var m1 = UserMessage.Error( aaCulture, "text" );
            m1.IsTranslationWelcome.Should().BeTrue();
            m1.Message.Text.Should().Be( "text" );
            m1.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m1.Level.Should().Be( UserMessageLevel.Error );
            m1.ResName.Should().StartWith( "SHA." );
            CheckSerializations( m1 );

            var m2 = UserMessage.Error( aaCulture, "text", "Res.Name" );
            m2.IsTranslationWelcome.Should().BeTrue();
            m2.Message.Text.Should().Be( "text" );
            m2.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m2.Level.Should().Be( UserMessageLevel.Error );
            m2.ResName.Should().Be( "Res.Name" );
            CheckSerializations( m2 );

            int v = 3712;

            var m3 = UserMessage.Error( aaCulture, $"Hello {v}!" );
            m3.IsTranslationWelcome.Should().BeTrue();
            m3.Message.Text.Should().Be( "Hello 3712!" );
            m3.Message.CodeString.FormattedString.GetFormatString().Should().Be( "Hello {0}!" );
            m3.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m3.Level.Should().Be( UserMessageLevel.Error );
            m3.ResName.Should().StartWith( "SHA." );
            CheckSerializations( m3 );

            var m4 = UserMessage.Error( aaCulture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
            m4.IsTranslationWelcome.Should().BeTrue();
            m4.Message.Text.Should().Be( "3712 Goodbye 3712" );
            m4.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m4.Message.CodeString.TargetCulture.Should().BeSameAs( aaCulture );
            m4.Level.Should().Be( UserMessageLevel.Error );
            m4.ResName.Should().Be( "Policy.Salutation" );
            CheckSerializations( m4 );

            aaCulture.SetCachedTranslations( new Dictionary<string, string> { { "Policy.Salutation", "AH! {0} H'lo {1}" } } );
            var current = new CurrentCultureInfo( new TranslationService(), aaCulture );

            var m5 = UserMessage.Error( current, $"{v} Goodbye {v}", "Policy.Salutation" );
            m5.IsTranslationWelcome.Should().BeFalse();
            m5.Message.Text.Should().Be( "AH! 3712 H'lo 3712" );
            m5.Message.FormatCulture.Should().BeSameAs( aaCulture );
            m5.Message.CodeString.TargetCulture.Should().BeSameAs( aaCulture );
            m5.Level.Should().Be( UserMessageLevel.Error );
            m5.ResName.Should().Be( "Policy.Salutation" );
            CheckSerializations( m5 );

        }

        [Test]
        public void UserMessage_Warn()
        {
            var aaCulture = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "aa" );

            var m1 = UserMessage.Warn( aaCulture, "text" );
            m1.IsTranslationWelcome.Should().BeTrue();
            m1.Message.Text.Should().Be( "text" );
            m1.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m1.Level.Should().Be( UserMessageLevel.Warn );
            m1.ResName.Should().StartWith( "SHA." );
            CheckSerializations( m1 );

            var m2 = UserMessage.Warn( aaCulture, "text", "Res.Name" );
            m2.IsTranslationWelcome.Should().BeTrue();
            m2.Message.Text.Should().Be( "text" );
            m2.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m2.Message.CodeString.FormattedString.GetFormatString().Should().Be( m2.Text );
            m2.Level.Should().Be( UserMessageLevel.Warn );
            m2.ResName.Should().Be( "Res.Name" );
            CheckSerializations( m2 );

            int v = 3712;

            var m3 = UserMessage.Warn( aaCulture, $"Hello {v}!" );
            m3.IsTranslationWelcome.Should().BeTrue();
            m3.Message.Text.Should().Be( "Hello 3712!" );
            m3.Message.CodeString.FormattedString.GetFormatString().Should().Be( "Hello {0}!" );
            m3.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m3.Level.Should().Be( UserMessageLevel.Warn );
            m3.ResName.Should().StartWith( "SHA." );
            CheckSerializations( m3 );

            var m4 = UserMessage.Warn( aaCulture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
            m4.IsTranslationWelcome.Should().BeTrue();
            m4.Message.Text.Should().Be( "3712 Goodbye 3712" );
            m4.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m4.Level.Should().Be( UserMessageLevel.Warn );
            m4.ResName.Should().Be( "Policy.Salutation" );
            CheckSerializations( m4 );

            var name = "Albert";
            aaCulture.SetCachedTranslations( new Dictionary<string, string> { { "Policy.Salutation", "Hello {0}." } } );
            var current = new CurrentCultureInfo( new TranslationService(), aaCulture );

            var m5 = UserMessage.Warn( current, $"Hi {name}!", "Policy.Salutation" );
            m5.IsTranslationWelcome.Should().BeFalse();
            m5.Message.Text.Should().Be( "Hello Albert." );
            m5.Message.FormatCulture.Should().BeSameAs( aaCulture );
            m5.Level.Should().Be( UserMessageLevel.Warn );
            m5.ResName.Should().Be( "Policy.Salutation" );
            CheckSerializations( m5 );
        }

        [Test]
        public void SimpleUserMessage_can_be_deconstruct()
        {
            var (l1, m1, d1) = new SimpleUserMessage();
            l1.Should().Be( UserMessageLevel.None );
            m1.Should().BeEmpty();
            d1.Should().Be( 0 );

            var (l2, m2, d2) = new SimpleUserMessage( UserMessageLevel.Info, "Hop!", 42 );
            l2.Should().Be( UserMessageLevel.Info );
            m2.Should().Be( "Hop!" );
            d2.Should().Be( 42 );
        }

        [Test]
        public void UserMessage_Info()
        {
            var aaCulture = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "aa" );

            var m1 = UserMessage.Info( aaCulture, "text" );
            m1.IsTranslationWelcome.Should().BeTrue();
            m1.Message.Text.Should().Be( "text" );
            m1.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m1.Level.Should().Be( UserMessageLevel.Info );
            m1.ResName.Should().StartWith( "SHA." );
            CheckSerializations( m1 );

            var m2 = UserMessage.Info( aaCulture, "text", "Res.Name" );
            m2.IsTranslationWelcome.Should().BeTrue();
            m2.Message.Text.Should().Be( "text" );
            m2.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m2.Level.Should().Be( UserMessageLevel.Info );
            m2.ResName.Should().Be( "Res.Name" );
            CheckSerializations( m2 );

            int v = 3712;

            var m3 = UserMessage.Info( aaCulture, $"Hello {v}!" );
            m3.IsTranslationWelcome.Should().BeTrue();
            m3.Message.Text.Should().Be( "Hello 3712!" );
            m3.Message.CodeString.FormattedString.GetFormatString().Should().Be( "Hello {0}!" );
            m3.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m3.Level.Should().Be( UserMessageLevel.Info );
            m3.ResName.Should().StartWith( "SHA." );
            CheckSerializations( m3 );

            var m4 = UserMessage.Info( aaCulture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
            m4.IsTranslationWelcome.Should().BeTrue();
            m4.Message.Text.Should().Be( "3712 Goodbye 3712" );
            m4.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m4.Level.Should().Be( UserMessageLevel.Info );
            m4.ResName.Should().Be( "Policy.Salutation" );
            CheckSerializations( m4 );

            aaCulture.SetCachedTranslations( new Dictionary<string, string> { { "Policy.Salutation", "AH! {0} H'lo {1}" } } );
            var current = new CurrentCultureInfo( new TranslationService(), aaCulture );

            var m5 = UserMessage.Info( current, $"{v} Goodbye {v}", "Policy.Salutation" );
            m5.IsTranslationWelcome.Should().BeFalse();
            m5.Message.Text.Should().Be( "AH! 3712 H'lo 3712" );
            m5.Message.FormatCulture.Should().BeSameAs( aaCulture );
            m5.Level.Should().Be( UserMessageLevel.Info );
            m5.ResName.Should().Be( "Policy.Salutation" );
            CheckSerializations( m5 );
        }

        static void CheckSerializations( UserMessage m )
        {
            var cS = m.DeepClone();
            cS.Message.Text.Should().Be( m.Message.Text );
            cS.Message.TranslationQuality.Should().Be( m.Message.TranslationQuality );
            cS.ResName.Should().Be( m.ResName );
            cS.Level.Should().Be( m.Level );
            cS.ToString().Should().Be( m.ToString() );

            var cV = SimpleSerializable.DeepCloneVersioned( m );
            cV.Message.Text.Should().Be( m.Message.Text );
            cV.Message.TranslationQuality.Should().Be( m.Message.TranslationQuality );
            cV.ResName.Should().Be( m.ResName );
            cV.Level.Should().Be( m.Level );
            cV.ToString().Should().Be( m.ToString() );

            TestHelper.JsonIdempotenceCheck( m, GlobalizationJsonHelper.WriteAsJsonArray, GlobalizationJsonHelper.ReadUserMessageFromJsonArray );
            TestHelper.JsonIdempotenceCheck( m.Message, GlobalizationJsonHelper.WriteAsJsonArray, GlobalizationJsonHelper.ReadMCStringFromJsonArray );

            TestHelper.JsonIdempotenceCheck( m.AsSimpleUserMessage(), GlobalizationJsonHelper.WriteAsJsonArray, GlobalizationJsonHelper.ReadSimpleUserMessageFromJsonArray );
        }
    }

}
