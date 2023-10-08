using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;

namespace CK.Globalization.Tests
{
    [TestFixture]
    public class CurrentCultureTests
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
        public void factory_methods()
        {
            var fr = NormalizedCultureInfo.GetNormalizedCultureInfo( "fr" );
            var c = new CurrentCultureInfo( new TranslationService(), fr );

            var s1 = c.MCString( "Pouf!" );
            s1.CodeString.TargetCulture.Should().BeSameAs( fr );
            s1.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            s1.TranslationQuality.Should().Be( MCString.Quality.Awful );
            s1.CodeString.ResName.Should().Be( "SHA.mkw6dgd8KPM4cVj9gc914qW5wD0" );

            var s2 = c.MCString( $"I'm {GetType().Name}" );
            s2.CodeString.TargetCulture.Should().BeSameAs( fr );
            s2.FormatCulture.Should().BeSameAs( NormalizedCultureInfo.CodeDefault );
            s2.TranslationQuality.Should().Be( MCString.Quality.Awful );
            s2.Text.Should().Be( "I'm CurrentCultureTests" );
            s2.CodeString.ResName.Should().Be( "SHA.mV9fOR5Q9kHFEQ8uhcMQlk1BG90" );

            fr.SetCachedTranslations( new Dictionary<string, string>
            {
                { "SHA.mkw6dgd8KPM4cVj9gc914qW5wD0", "Paf!" },
                { "SHA.mV9fOR5Q9kHFEQ8uhcMQlk1BG90", "Je suis {0}..." },
            } );
            var s3 = c.MCString( "Pouf!" );
            s3.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            s3.Text.Should().Be( "Paf!" );
            var s4 = c.MCString( $"I'm {GetType().Name}" );
            s4.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            s4.Text.Should().Be( "Je suis CurrentCultureTests..." );

            var m1 = c.UserMessage( UserMessageLevel.Info, "Pouf!" );
            m1.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            m1.Text.Should().Be( "Paf!" );
            m1.Level.Should().Be( UserMessageLevel.Info );

            var m2 = c.UserMessage( UserMessageLevel.Error, $"I'm {GetType().Name}" );
            m2.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            m2.Text.Should().Be( "Je suis CurrentCultureTests..." );
            m2.Level.Should().Be( UserMessageLevel.Error );

            var m3 = c.InfoMessage( "Pouf!" );
            m3.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            m3.Text.Should().Be( "Paf!" );
            m3.Level.Should().Be( UserMessageLevel.Info );
            var m4 = c.InfoMessage( $"I'm {GetType().Name}" );
            m4.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            m4.Text.Should().Be( "Je suis CurrentCultureTests..." );
            m4.Level.Should().Be( UserMessageLevel.Info );

            var m5 = c.WarnMessage( "Pouf!" );
            m5.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            m5.Text.Should().Be( "Paf!" );
            m5.Level.Should().Be( UserMessageLevel.Warn );
            var m6 = c.WarnMessage( $"I'm {GetType().Name}" );
            m6.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            m6.Text.Should().Be( "Je suis CurrentCultureTests..." );
            m6.Level.Should().Be( UserMessageLevel.Warn );

            var m7 = c.ErrorMessage( "Pouf!" );
            m7.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            m7.Text.Should().Be( "Paf!" );
            m7.Level.Should().Be( UserMessageLevel.Error );
            var m8 = c.ErrorMessage( $"I'm {GetType().Name}" );
            m8.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            m8.Text.Should().Be( "Je suis CurrentCultureTests..." );
            m8.Level.Should().Be( UserMessageLevel.Error );

            var e1 = c.MCException( "Pouf!" );
            e1.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            e1.Message.Text.Should().Be( "Paf!" );
            var e2 = c.MCException( $"I'm {GetType().Name}", null, e1 );
            e2.Message.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            e2.Message.Text.Should().Be( "Je suis CurrentCultureTests..." );
            e2.InnerException.Should().BeSameAs( e1 );
        }

    }

}
