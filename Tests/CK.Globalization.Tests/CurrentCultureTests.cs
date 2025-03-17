using CK.Core;
using Shouldly;
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

        msg.Text.ShouldBe( "Transfer progress is 54%. It should end on mercredi 8 novembre 2023 12:05:00." );
        msg.TranslationQuality.ShouldBe( MCString.Quality.Awful );

        msg.CodeString.ResName.ShouldBe( "SHA.V55R2QdiE4w1O82f1Ig5R7kklCc" );

        var fr = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" );
        // We could have used the first fallback.
        fr.ShouldBeSameAs( frFR.Fallbacks[0] );
        fr.SetCachedTranslations( new[] { ("SHA.V55R2QdiE4w1O82f1Ig5R7kklCc", "Le transfert se terminera le {1} ({0}%).") } );

        var goodMsg = currentCulture.TranslationService.Translate( msg.CodeString );
        goodMsg.Text.ShouldBe( "Le transfert se terminera le mercredi 8 novembre 2023 12:05:00 (54%)." );
        goodMsg.TranslationQuality.ShouldBe( MCString.Quality.Good );

        frFR.SetCachedTranslations( new[] { ("SHA.V55R2QdiE4w1O82f1Ig5R7kklCc", "Le transfert se terminera le {1} ({0}%).") } );

        var perfectMsg = currentCulture.TranslationService.Translate( msg.CodeString );
        perfectMsg.Text.ShouldBe( "Le transfert se terminera le mercredi 8 novembre 2023 12:05:00 (54%)." );
        perfectMsg.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
    }


    [Test]
    public void factory_methods()
    {
        var fr = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" );
        var c = new CurrentCultureInfo( new TranslationService(), fr );


        var s1 = c.MCString( "Pouf!" );
        s1.CodeString.TargetCulture.ShouldBeSameAs( fr );
        s1.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        s1.TranslationQuality.ShouldBe( MCString.Quality.Awful );
        s1.CodeString.ResName.ShouldBe( "SHA.mkw6dgd8KPM4cVj9gc914qW5wD0" );

        var s2 = c.MCString( $"I'm {GetType().Name}" );
        s2.CodeString.TargetCulture.ShouldBeSameAs( fr );
        s2.FormatCulture.ShouldBeSameAs( NormalizedCultureInfo.CodeDefault );
        s2.TranslationQuality.ShouldBe( MCString.Quality.Awful );
        s2.Text.ShouldBe( "I'm CurrentCultureTests" );
        s2.CodeString.ResName.ShouldBe( "SHA.mV9fOR5Q9kHFEQ8uhcMQlk1BG90" );

        fr.SetCachedTranslations( new Dictionary<string, string>
        {
            { "SHA.mkw6dgd8KPM4cVj9gc914qW5wD0", "Paf!" },
            { "SHA.mV9fOR5Q9kHFEQ8uhcMQlk1BG90", "Je suis {0}..." },
        } );
        var s3 = c.MCString( "Pouf!" );
        s3.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        s3.Text.ShouldBe( "Paf!" );
        var s4 = c.MCString( $"I'm {GetType().Name}" );
        s4.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        s4.Text.ShouldBe( "Je suis CurrentCultureTests..." );

        var m1 = c.UserMessage( UserMessageLevel.Info, "Pouf!" );
        m1.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        m1.Text.ShouldBe( "Paf!" );
        m1.Level.ShouldBe( UserMessageLevel.Info );

        var m2 = c.UserMessage( UserMessageLevel.Error, $"I'm {GetType().Name}" );
        m2.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        m2.Text.ShouldBe( "Je suis CurrentCultureTests..." );
        m2.Level.ShouldBe( UserMessageLevel.Error );

        var m3 = c.InfoMessage( "Pouf!" );
        m3.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        m3.Text.ShouldBe( "Paf!" );
        m3.Level.ShouldBe( UserMessageLevel.Info );
        var m4 = c.InfoMessage( $"I'm {GetType().Name}" );
        m4.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        m4.Text.ShouldBe( "Je suis CurrentCultureTests..." );
        m4.Level.ShouldBe( UserMessageLevel.Info );

        var m5 = c.WarnMessage( "Pouf!" );
        m5.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        m5.Text.ShouldBe( "Paf!" );
        m5.Level.ShouldBe( UserMessageLevel.Warn );
        var m6 = c.WarnMessage( $"I'm {GetType().Name}" );
        m6.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        m6.Text.ShouldBe( "Je suis CurrentCultureTests..." );
        m6.Level.ShouldBe( UserMessageLevel.Warn );

        var m7 = c.ErrorMessage( "Pouf!" );
        m7.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        m7.Text.ShouldBe( "Paf!" );
        m7.Level.ShouldBe( UserMessageLevel.Error );
        var m8 = c.ErrorMessage( $"I'm {GetType().Name}" );
        m8.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        m8.Text.ShouldBe( "Je suis CurrentCultureTests..." );
        m8.Level.ShouldBe( UserMessageLevel.Error );

        var e1 = c.MCException( "Pouf!" );
        e1.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        e1.Message.Text.ShouldBe( "Paf!" );
        var e2 = c.MCException( $"I'm {GetType().Name}", null, e1 );
        e2.Message.TranslationQuality.ShouldBe( MCString.Quality.Perfect );
        e2.Message.Text.ShouldBe( "Je suis CurrentCultureTests..." );
        e2.InnerException.ShouldBeSameAs( e1 );
    }

}
