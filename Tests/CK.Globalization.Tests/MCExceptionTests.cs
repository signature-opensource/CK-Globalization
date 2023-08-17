using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CK.Globalization.Tests
{
    [TestFixture]
    [SetCulture( "fr-FR" )]
    public class MCExceptionTests
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
        public void inner_exception_test()
        {
            var fr = NormalizedCultureInfo.GetNormalizedCultureInfo( "fr" );
            fr.SetCachedTranslations( new Dictionary<string, string>
            {
                { "Error.AnError", "Une erreur." },
                { "Error.AnotherError", "Une autre erreur." }
            } );

            var current = new CurrentCultureInfo( new TranslationService(), fr );
            var e = new MCException( current, "An error.", "Error.AnError",
                        new Exception( "Another error.",
                            new MCException( current, "Another error.", "Error.AnotherError" ) ) );
            {
                var messages = e.GetUserMessages();
                messages.Count.Should().Be( 3 );
                messages[0].Text.Should().Be( "Une erreur." );
                messages[0].Depth.Should().Be( 0 );
                messages[1].Text.Should().Be( "Another error." );
                messages[1].Depth.Should().Be( 1 );
                messages[2].Text.Should().Be( "Une autre erreur." );
                messages[2].Depth.Should().Be( 2 );
            }
            // Setting a translation for "Another error." 
            ((MCException)e.InnerException!.InnerException!).Message.CodeString.FormattedString.GetSHA1BasedResName().Should().Be( "SHA.2Rtfhnwa9NE1ZH5-uPu4SiTLJdw" );
            fr.SetCachedTranslations( new Dictionary<string, string>
            {
                { "Error.AnError", "Une erreur." },
                { "Error.AnotherError", "Une autre erreur." },
                { "SHA.2Rtfhnwa9NE1ZH5-uPu4SiTLJdw", "Une autre erreur (from 'SHA.')" }
            } );
            {
                var messages = e.GetUserMessages( current );
                messages.Count.Should().Be( 3 );
                messages[0].Text.Should().Be( "Une erreur." );
                messages[0].Depth.Should().Be( 0 );
                messages[1].Text.Should().Be( "Une autre erreur (from 'SHA.')" );
                messages[1].Depth.Should().Be( 1 );
                messages[2].Text.Should().Be( "Une autre erreur." );
                messages[2].Depth.Should().Be( 2 );
            }
        }

        [Test]
        public void aggregate_exception_test()
        {
            var fr = NormalizedCultureInfo.GetNormalizedCultureInfo( "fr" );
            fr.SetCachedTranslations( new Dictionary<string, string>
            {
                { "Error.AnError", "Une erreur." },
                { "Error.AnotherError", "Une autre erreur." }
            } );

            var current = new CurrentCultureInfo( new TranslationService(), fr );
            var e = new AggregateException // Depth 0
                        (
                            new MCException( current, "An error.", "Error.AnError" ),  // Depth 1
                            new Exception( "Another error.", // Depth 1
                                new MCException( current, "Another error.", "Error.AnotherError" )  ), // Depth 2
                            new AggregateException( "Agg!", // Depth 1
                                new MCException( current, "An error.", "Error.AnError" ), // Depth 2
                                new Exception( "Another error.", // Depth 2
                                    new MCException( current, "Another error.", "Error.AnotherError" ) ) ) // Depth 3
                        );
            var messages = e.GetUserMessages( current );
            messages.Count.Should().Be( 8 );
            messages[0].Text.Should().Be( "One or more errors occurred. (Une erreur.) (Another error.) (Agg! (Une erreur.) (Another error.))" );
            messages[0].Depth.Should().Be( 0 );
            messages[1].Text.Should().Be( "Une erreur." );
            messages[1].Depth.Should().Be( 1 );
            messages[2].Text.Should().Be( "Another error." );
            messages[2].Depth.Should().Be( 1 );
            messages[3].Text.Should().Be( "Une autre erreur." );
            messages[3].Depth.Should().Be( 2 );
            messages[4].Text.Should().Be( "Agg! (Une erreur.) (Another error.)" );
            messages[4].Depth.Should().Be( 1 );
            messages[5].Text.Should().Be( "Une erreur." );
            messages[5].Depth.Should().Be( 2 );
            messages[6].Text.Should().Be( "Another error." );
            messages[6].Depth.Should().Be( 2 );
            messages[7].Text.Should().Be( "Une autre erreur." );
            messages[7].Depth.Should().Be( 3 );
        }

    }

}
