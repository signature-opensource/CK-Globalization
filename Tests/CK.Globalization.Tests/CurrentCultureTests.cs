using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CK.Globalization.Tests;

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
    public void demo()
    {
        var frFR = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-FR" );
        var currentCulture = new CurrentCultureInfo( new TranslationService(), frFR );

        int percent = 54;
        DateTime estimatedEnd = new DateTime( 2023, 11, 8, 12, 5, 0 );

        var msg = currentCulture.MCString( $"Transfer progress is {percent}%. It should end on {estimatedEnd:F}." );

        msg.Text.Should().Be( "Transfer progress is 54%. It should end on mercredi 8 novembre 2023 12:05:00." );
        msg.TranslationQuality.Should().Be( MCString.Quality.Awful );

        msg.CodeString.ResName.Should().Be( "SHA.V55R2QdiE4w1O82f1Ig5R7kklCc" );

        var fr = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" );
        // We could have used the first fallback.
        fr.Should().BeSameAs( frFR.Fallbacks[0] );
        fr.SetCachedTranslations( new[] { ("SHA.V55R2QdiE4w1O82f1Ig5R7kklCc", "Le transfert se terminera le {1} ({0}%).") } );

        var goodMsg = currentCulture.TranslationService.Translate( msg.CodeString );
        goodMsg.Text.Should().Be( "Le transfert se terminera le mercredi 8 novembre 2023 12:05:00 (54%)." );
        goodMsg.TranslationQuality.Should().Be( MCString.Quality.Good );

        frFR.SetCachedTranslations( new[] { ("SHA.V55R2QdiE4w1O82f1Ig5R7kklCc", "Le transfert se terminera le {1} ({0}%).") } );

        var perfectMsg = currentCulture.TranslationService.Translate( msg.CodeString );
        perfectMsg.Text.Should().Be( "Le transfert se terminera le mercredi 8 novembre 2023 12:05:00 (54%)." );
        perfectMsg.TranslationQuality.Should().Be( MCString.Quality.Perfect );
    }


    [Test]
    public void factory_methods()
    {
        var fr = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" );
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
