using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CK.Globalization.Tests
{
    [TestFixture]
    public class TranslationServiceTests
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
        public async Task without_translations_Async()
        {
            var s = new TranslationService();
            var c = new CodeString( NormalizedCultureInfo.Invariant, "Hop" );
            var t = await s.TranslateAsync( c );
            t.Text.Should().BeSameAs( c.Text );
            t.FormatCulture.Should().Be( NormalizedCultureInfo.CodeDefault );
        }

        [Test]
        public async Task simple_translations_using_Hashcode_based_resname_Async()
        {
            var s = new TranslationService();
            var date = new DateTime( 2023, 8, 4, 18, 38, 47 );
            var c = new CodeString( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-FR" ), $"Hop {date:F}." );
            c.Text.Should().Be( "Hop vendredi 4 août 2023 18:38:47." );
            c.TargetCulture.Name.Should().Be( "fr-fr" );

            var t = await s.TranslateAsync( c );
            t.Text.Should().Be( c.Text );
            t.FormatCulture.Name.Should().Be( "en" );
            t.TranslationQuality.Should().Be( MCString.Quality.Awful );

            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ).SetCachedTranslations( new Dictionary<string, string> { { c.ResName, "C'est Hop {0} (en français)." } } );

            t = await s.TranslateAsync( c );
            t.Text.Should().Be( "C'est Hop vendredi 4 août 2023 18:38:47 (en français)." );
            t.FormatCulture.Name.Should().Be( "fr" );
            t.TranslationQuality.Should().Be( MCString.Quality.Good );

            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-fr" ).SetCachedTranslations( new Dictionary<string, string> { { c.ResName, "C'est {0} en France!" } } );
            t = await s.TranslateAsync( c );
            t.Text.Should().Be( "C'est vendredi 4 août 2023 18:38:47 en France!" );
            t.FormatCulture.Name.Should().Be( "fr-fr" );
            t.TranslationQuality.Should().Be( MCString.Quality.Perfect );

        }

        [Test]
        public async Task translations_with_extended_culture_Async()
        {
            var s = new TranslationService();
            var date = new DateTime( 2023, 8, 4, 18, 38, 47 );
            var preferences = ExtendedCultureInfo.EnsureExtendedCultureInfo( "fr-ch,fr-ca,de" );

            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ).SetCachedTranslations( new Dictionary<string, string>
            {
                { "Res.Name", "France {0} le {1}." }
            } );
            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-ch" ).SetCachedTranslations( new Dictionary<string, string>
            {
                { "Res.Name", "Suisse {0} le {1}." }
            } );
            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-ca" ).SetCachedTranslations( new Dictionary<string, string>
            {
                { "Res.Name", "Canada {0} le {1}." }
            } );
            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "de" ).SetCachedTranslations( new Dictionary<string, string>
            {
                { "Res.Name", "German {0} am {1}." }
            } );

            var c = new CodeString( preferences, $"Hello from {preferences.PrimaryCulture.Name} on {date:F}.", "Res.Name" );
            {
                var t = await s.TranslateAsync( c );
                t.Text.Should().Be( "Suisse fr-ch le vendredi, 4 août 2023 18:38:47." );
                t.FormatCulture.Name.Should().Be( "fr-ch" );
                t.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            }
            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-ch" ).SetCachedTranslations( new Dictionary<string, string>() );
            {
                var t = await s.TranslateAsync( c );
                t.Text.Should().Be( "Canada fr-ch le vendredi, 4 août 2023 18:38:47." );
                t.FormatCulture.Name.Should().Be( "fr-ca" );
                t.TranslationQuality.Should().Be( MCString.Quality.Good );
            }
            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-ca" ).SetCachedTranslations( new Dictionary<string, string>() );
            {
                var t = await s.TranslateAsync( c );
                t.Text.Should().Be( "France fr-ch le vendredi, 4 août 2023 18:38:47." );
                t.FormatCulture.Name.Should().Be( "fr" );
                t.TranslationQuality.Should().Be( MCString.Quality.Good );
            }
            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ).SetCachedTranslations( new Dictionary<string, string>() );
            {
                var t = await s.TranslateAsync( c );
                t.Text.Should().Be( "German fr-ch am vendredi, 4 août 2023 18:38:47." );
                t.FormatCulture.Name.Should().Be( "de" );
                t.TranslationQuality.Should().Be( MCString.Quality.Bad );
            }
            NormalizedCultureInfo.EnsureNormalizedCultureInfo( "de" ).SetCachedTranslations( new Dictionary<string, string>() );
            {
                var t = await s.TranslateAsync( c );
                t.Text.Should().Be( "Hello from fr-ch on vendredi, 4 août 2023 18:38:47." );
                t.FormatCulture.Name.Should().Be( "en" );
                t.TranslationQuality.Should().Be( MCString.Quality.Awful );
            }
        }

        [Test]
        public async Task translations_quality_with_default_Async()
        {
            var s = new TranslationService();

            {
                var preferences = ExtendedCultureInfo.EnsureExtendedCultureInfo( "fr" );
                var c = new CodeString( preferences, $"Hello from {preferences.PrimaryCulture.Name}.", "Res.Name" );
                var t = await s.TranslateAsync( c );
                t.TranslationQuality.Should().Be( MCString.Quality.Awful );
            }
            {
                var preferences = ExtendedCultureInfo.EnsureExtendedCultureInfo( "en" );
                var c = new CodeString( preferences, $"Hello from {preferences.PrimaryCulture.Name}.", "Res.Name" );
                var t = await s.TranslateAsync( c );
                t.TranslationQuality.Should().Be( MCString.Quality.Perfect );
            }
            {
                var preferences = ExtendedCultureInfo.EnsureExtendedCultureInfo( "en-us" );
                var c = new CodeString( preferences, $"Hello from {preferences.PrimaryCulture.Name}.", "Res.Name" );
                var t = await s.TranslateAsync( c );
                t.TranslationQuality.Should().Be( MCString.Quality.Good );
            }
            {
                var preferences = ExtendedCultureInfo.EnsureExtendedCultureInfo( "fr-fr,en-us" );
                var c = new CodeString( preferences, $"Hello from {preferences.PrimaryCulture.Name}.", "Res.Name" );
                var t = await s.TranslateAsync( c );
                t.TranslationQuality.Should().Be( MCString.Quality.Bad );
            }
            {
                var preferences = ExtendedCultureInfo.EnsureExtendedCultureInfo( "fr-fr,en" );
                var c = new CodeString( preferences, $"Hello from {preferences.PrimaryCulture.Name}.", "Res.Name" );
                var t = await s.TranslateAsync( c );
                t.TranslationQuality.Should().Be( MCString.Quality.Bad );
            }
        }

    }
}
