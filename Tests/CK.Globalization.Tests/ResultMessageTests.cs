using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace CK.Globalization.Tests
{
    [TestFixture]
    public class ResultMessageTests
    {
        [SetUp]
        [TearDown]
        public void ClearCache()
        {
            typeof( NormalizedCultureInfo )
                .GetMethod( "ClearCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static )
                .Invoke( null, null );
        }

        [Test]
        public void ResultMessage_Error()
        {
            var m1 = ResultMessage.Error( "text" );
            m1.IsTranslationWelcome.Should().BeTrue();
            m1.Message.Text.Should().Be( "text" );
            m1.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m1.Level.Should().Be( ResultMessageLevel.Error );
            m1.ResName.Should().StartWith( "SHA." );
            CheckSerialization( m1 );

            var m2 = ResultMessage.Error( "text", "Res.Name" );
            m2.IsTranslationWelcome.Should().BeTrue();
            m2.Message.Text.Should().Be( "text" );
            m2.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m2.Level.Should().Be( ResultMessageLevel.Error );
            m2.ResName.Should().Be( "Res.Name" );
            CheckSerialization( m2 );

            int v = 3712;

            var m3 = ResultMessage.Error( $"Hello {v}!", "Policy.Salutation" ); //==> "Hello {0}!"
            m3.IsTranslationWelcome.Should().BeTrue();
            m3.Message.Text.Should().Be( "Hello 3712!" );
            m3.Message.CodeString.FormattedString.GetFormatString().Should().Be( "Hello {0}!" );
            m3.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m3.Level.Should().Be( ResultMessageLevel.Error );
            m3.ResName.Should().Be( "Policy.Salutation" );
            CheckSerialization( m3 );

            var aaCulture = NormalizedCultureInfo.GetNormalizedCultureInfo( "aa" );

            var m4 = ResultMessage.Error( aaCulture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
            m4.IsTranslationWelcome.Should().BeTrue();
            m4.Message.Text.Should().Be( "3712 Goodbye 3712" );
            m4.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m4.Level.Should().Be(ResultMessageLevel.Error);
            m4.ResName.Should().Be( "Policy.Salutation" );
            CheckSerialization( m4 );

            aaCulture.SetCachedTranslations( new Dictionary<string, string> { { "Policy.Salutation", "AH! {0} H'lo {1}" } } );
            var current = new CurrentCultureInfo( new TranslationService(), aaCulture );

            var m5 = ResultMessage.Error( current, $"{v} Goodbye {v}", "Policy.Salutation" );
            m5.IsTranslationWelcome.Should().BeFalse();
            m5.Message.Text.Should().Be( "AH! 3712 H'lo 3712" );
            m5.Message.FormatCulture.Should().BeSameAs( aaCulture );
            m5.Level.Should().Be( ResultMessageLevel.Error );
            m5.ResName.Should().Be( "Policy.Salutation" );
            CheckSerialization( m5 );
        }

        [Test]
        public void ResultMessage_Warn()
        {
            var m1 = ResultMessage.Warn( "text" );
            m1.IsTranslationWelcome.Should().BeTrue();
            m1.Message.Text.Should().Be( "text" );
            m1.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m1.Level.Should().Be( ResultMessageLevel.Warn );
            m1.ResName.Should().StartWith( "SHA." );
            CheckSerialization( m1 );

            var m2 = ResultMessage.Warn( "text", "Res.Name" );
            m2.IsTranslationWelcome.Should().BeTrue();
            m2.Message.Text.Should().Be( "text" );
            m2.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m2.Level.Should().Be( ResultMessageLevel.Warn );
            m2.ResName.Should().Be( "Res.Name" );
            CheckSerialization( m2 );

            int v = 3712;

            var m3 = ResultMessage.Warn( $"Hello {v}!", "Policy.Salutation" ); //==> "Hello {0}!"
            m3.IsTranslationWelcome.Should().BeTrue();
            m3.Message.Text.Should().Be( "Hello 3712!" );
            m3.Message.CodeString.FormattedString.GetFormatString().Should().Be( "Hello {0}!" );
            m3.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m3.Level.Should().Be( ResultMessageLevel.Warn );
            m3.ResName.Should().Be( "Policy.Salutation" );
            CheckSerialization( m3 );

            var aaCulture = NormalizedCultureInfo.GetNormalizedCultureInfo( "aa" );

            var m4 = ResultMessage.Warn( aaCulture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
            m4.IsTranslationWelcome.Should().BeTrue();
            m4.Message.Text.Should().Be( "3712 Goodbye 3712" );
            m4.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m4.Level.Should().Be(ResultMessageLevel.Warn);
            m4.ResName.Should().Be( "Policy.Salutation" );
            CheckSerialization( m4 );

            aaCulture.SetCachedTranslations( new Dictionary<string, string> { { "Policy.Salutation", "AH! {0} H'lo {1}" } } );
            var current = new CurrentCultureInfo( new TranslationService(), aaCulture );

            var m5 = ResultMessage.Warn( current, $"{v} Goodbye {v}", "Policy.Salutation" );
            m5.IsTranslationWelcome.Should().BeFalse();
            m5.Message.Text.Should().Be( "AH! 3712 H'lo 3712" );
            m5.Message.FormatCulture.Should().BeSameAs( aaCulture );
            m5.Level.Should().Be( ResultMessageLevel.Warn );
            m5.ResName.Should().Be( "Policy.Salutation" );
            CheckSerialization( m5 );
        }

        [Test]
        public void ResultMessage_Info()
        {
            var m1 = ResultMessage.Info( "text" );
            m1.IsTranslationWelcome.Should().BeTrue();
            m1.Message.Text.Should().Be( "text" );
            m1.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m1.Level.Should().Be( ResultMessageLevel.Info );
            m1.ResName.Should().StartWith( "SHA." );
            CheckSerialization( m1 );

            var m2 = ResultMessage.Info( "text", "Res.Name" );
            m2.IsTranslationWelcome.Should().BeTrue();
            m2.Message.Text.Should().Be( "text" );
            m2.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m2.Level.Should().Be( ResultMessageLevel.Info );
            m2.ResName.Should().Be( "Res.Name" );
            CheckSerialization( m2 );

            int v = 3712;

            var m3 = ResultMessage.Info( $"Hello {v}!", "Policy.Salutation" ); //==> "Hello {0}!"
            m3.IsTranslationWelcome.Should().BeTrue();
            m3.Message.Text.Should().Be( "Hello 3712!" );
            m3.Message.CodeString.FormattedString.GetFormatString().Should().Be( "Hello {0}!" );
            m3.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m3.Level.Should().Be( ResultMessageLevel.Info );
            m3.ResName.Should().Be( "Policy.Salutation" );
            CheckSerialization( m3 );

            var aaCulture = NormalizedCultureInfo.GetNormalizedCultureInfo( "aa" );

            var m4 = ResultMessage.Info( aaCulture, $"{v} Goodbye {v}", "Policy.Salutation" ); //==> "{0} Goodbye {1}"
            m4.IsTranslationWelcome.Should().BeTrue();
            m4.Message.Text.Should().Be( "3712 Goodbye 3712" );
            m4.Message.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            m4.Level.Should().Be(ResultMessageLevel.Info);
            m4.ResName.Should().Be( "Policy.Salutation" );
            CheckSerialization( m4 );

            aaCulture.SetCachedTranslations( new Dictionary<string, string> { { "Policy.Salutation", "AH! {0} H'lo {1}" } } );
            var current = new CurrentCultureInfo( new TranslationService(), aaCulture );

            var m5 = ResultMessage.Info( current, $"{v} Goodbye {v}", "Policy.Salutation" );
            m5.IsTranslationWelcome.Should().BeFalse();
            m5.Message.Text.Should().Be( "AH! 3712 H'lo 3712" );
            m5.Message.FormatCulture.Should().BeSameAs( aaCulture );
            m5.Level.Should().Be( ResultMessageLevel.Info );
            m5.ResName.Should().Be( "Policy.Salutation" );
            CheckSerialization( m5 );
        }

        static void CheckSerialization( ResultMessage m )
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
        }


    }
}
