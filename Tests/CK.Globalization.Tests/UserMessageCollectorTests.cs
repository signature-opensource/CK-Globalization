using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Globalization.Tests
{
    [TestFixture]
    public class UserMessageCollectorTests
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
        public void collector_test()
        {
            var culture = NormalizedCultureInfo.GetNormalizedCultureInfo( "fr-FR" );
            var current = new CurrentCultureInfo( new TranslationService(), culture );
            var c = new UserMessageCollector( current );
            using( TestHelper.Monitor.CollectTexts( out var logs ) )
            {
                c.DumpLogs( TestHelper.Monitor );
                logs.Should().BeEmpty();
            }
            int v = -42;
            FluentActions.Invoking( () => c.Add( UserMessageLevel.None, "nop" ) ).Should().Throw<ArgumentException>();
            FluentActions.Invoking( () => c.Add( UserMessageLevel.None, $"v = {v}" ) ).Should().Throw<ArgumentException>();
            FluentActions.Invoking( () => c.OpenGroup( UserMessageLevel.None, "nop" ) ).Should().Throw<ArgumentException>();
            FluentActions.Invoking( () => c.OpenGroup( UserMessageLevel.None, $"v = {v}" ) ).Should().Throw<ArgumentException>();

            c.Depth.Should().Be( 0 );

            NormalizedCultureInfo.GetNormalizedCultureInfo( "fr" ).SetCachedTranslations( new Dictionary<string, string>()
            {
                { "Validation.PositiveValueExpected", "La valeur {0} doit être positive." },
                { "Done", "Fait." }
            } );

            c.Error( $"Value {v} should be positive.", "Validation.PositiveValueExpected" );
            c.Warn( $"Value {v} should be positive.", "Validation.PositiveValueExpected" );
            c.Info( $"Value {v} should be positive.", "Validation.PositiveValueExpected" );

            c.Depth.Should().Be( 0 );
            c.ErrorCount.Should().Be( 1 );
            c.UserMessages.Should().HaveCount( 3 ).And.AllSatisfy( m => m.Text.Should().Be( "La valeur -42 doit être positive." ) );
            c.UserMessages[0].Level.Should().Be( UserMessageLevel.Error );
            c.UserMessages[1].Level.Should().Be( UserMessageLevel.Warn );
            c.UserMessages[2].Level.Should().Be( UserMessageLevel.Info );
            using( TestHelper.Monitor.CollectEntries( out var logs, LogLevelFilter.Info ) )
            {
                c.DumpLogs( TestHelper.Monitor );
                logs.Should().HaveCount( 3 );
                logs[0].MaskedLevel.Should().Be( LogLevel.Error );
                logs[0].Text.Should().Be( "Value -42 should be positive." );
                logs[1].MaskedLevel.Should().Be( LogLevel.Warn );
                logs[1].Text.Should().Be( "Value -42 should be positive." );
                logs[2].MaskedLevel.Should().Be( LogLevel.Info );
                logs[2].Text.Should().Be( "Value -42 should be positive." );
            }

            // Unclosed group.
            c.OpenError( "NoShow" );

            c.Clear();
            using( c.OpenError( "E" ) )
            {
                using( c.OpenWarn( "W" ) )
                {
                    using( c.OpenInfo( "I" ) )
                    {
                        c.Add( UserMessageLevel.Error, $"Value {v} should be positive.", "Validation.PositiveValueExpected" );
                        c.Add( UserMessageLevel.Warn, $"Value {v} should be positive.", "Validation.PositiveValueExpected" );
                        c.Add( UserMessageLevel.Info, $"Value {v} should be positive.", "Validation.PositiveValueExpected" );
                    }
                }
                using( c.OpenGroup( UserMessageLevel.Error, "E2" ) )
                {
                    using( c.OpenGroup( UserMessageLevel.Warn, "W2" ) )
                    {
                        using( c.OpenGroup( UserMessageLevel.Info, "I2" ) )
                        {
                            c.Info( "Done.", "Done" );
                            c.Depth.Should().Be( 4 );
                        }
                        c.Depth.Should().Be( 3 );
                    }
                    c.Depth.Should().Be( 2 );
                }
                c.Depth.Should().Be( 1 );
            }
            c.Depth.Should().Be( 0 );
            c.ErrorCount.Should().Be( 3 );
            c.UserMessages.Should().HaveCount( 10 );
            c.UserMessages.Select( m => m.Depth ).Should().BeEquivalentTo( new[] { 0, 1, 2, 3, 3, 3, 1, 2, 3, 4 } );

            using( TestHelper.Monitor.CollectEntries( out var logs, LogLevelFilter.Info ) )
            {
                c.DumpLogs( TestHelper.Monitor );
                logs.Should().HaveCount( 10 );
                logs[0].MaskedLevel.Should().Be( LogLevel.Error );
                logs[0].Text.Should().Be( "E" );
                logs[1].MaskedLevel.Should().Be( LogLevel.Warn );
                logs[1].Text.Should().Be( "W" );
                logs[2].MaskedLevel.Should().Be( LogLevel.Info );
                logs[2].Text.Should().Be( "I" );

                logs[3].MaskedLevel.Should().Be( LogLevel.Error );
                logs[3].Text.Should().Be( "Value -42 should be positive." );
                logs[4].MaskedLevel.Should().Be( LogLevel.Warn );
                logs[4].Text.Should().Be( "Value -42 should be positive." );
                logs[5].MaskedLevel.Should().Be( LogLevel.Info );
                logs[5].Text.Should().Be( "Value -42 should be positive." );

                logs[6].MaskedLevel.Should().Be( LogLevel.Error );
                logs[6].Text.Should().Be( "E2" );
                logs[7].MaskedLevel.Should().Be( LogLevel.Warn );
                logs[7].Text.Should().Be( "W2" );
                logs[8].MaskedLevel.Should().Be( LogLevel.Info );
                logs[8].Text.Should().Be( "I2" );

                logs[9].MaskedLevel.Should().Be( LogLevel.Info );
                logs[9].Text.Should().Be( "Done." );
            }

            c.Clear();
            c.Depth.Should().Be( 0 );
            c.ErrorCount.Should().Be( 0 );

        }
    }
}
