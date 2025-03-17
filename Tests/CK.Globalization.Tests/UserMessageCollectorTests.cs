using CK.Core;
using Shouldly;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Globalization.Tests;

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
        var culture = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-FR" );
        var current = new CurrentCultureInfo( new TranslationService(), culture );
        var c = new UserMessageCollector( current );
        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            c.DumpLogs( TestHelper.Monitor );
            logs.ShouldBeEmpty();
        }
        int v = -42;
        Util.Invokable( () => c.Add( UserMessageLevel.None, "nop" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => c.Add( UserMessageLevel.None, $"v = {v}" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => c.OpenGroup( UserMessageLevel.None, "nop" ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => c.OpenGroup( UserMessageLevel.None, $"v = {v}" ) ).ShouldThrow<ArgumentException>();

        c.Depth.ShouldBe( 0 );

        NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" ).SetCachedTranslations( new Dictionary<string, string>()
        {
            { "Validation.PositiveValueExpected", "La valeur {0} doit être positive." },
            { "Done", "Fait." }
        } );

        c.Error( $"Value {v} should be positive.", "Validation.PositiveValueExpected" );
        c.Warn( $"Value {v} should be positive.", "Validation.PositiveValueExpected" );
        c.Info( $"Value {v} should be positive.", "Validation.PositiveValueExpected" );

        c.Depth.ShouldBe( 0 );
        c.ErrorCount.ShouldBe( 1 );
        c.UserMessages.Count.ShouldBe( 3 );
        foreach( var m in c.UserMessages )
        {
            m.Text.ShouldBe( "La valeur -42 doit être positive." );
        }
        c.UserMessages[0].Level.ShouldBe( UserMessageLevel.Error );
        c.UserMessages[1].Level.ShouldBe( UserMessageLevel.Warn );
        c.UserMessages[2].Level.ShouldBe( UserMessageLevel.Info );
        using( TestHelper.Monitor.CollectEntries( out var logs, LogLevelFilter.Info ) )
        {
            c.DumpLogs( TestHelper.Monitor );
            logs.Count.ShouldBe( 3 );
            logs[0].MaskedLevel.ShouldBe( LogLevel.Error );
            logs[0].Text.ShouldBe( "Value -42 should be positive." );
            logs[1].MaskedLevel.ShouldBe( LogLevel.Warn );
            logs[1].Text.ShouldBe( "Value -42 should be positive." );
            logs[2].MaskedLevel.ShouldBe( LogLevel.Info );
            logs[2].Text.ShouldBe( "Value -42 should be positive." );
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
                        c.Depth.ShouldBe( 4 );
                    }
                    c.Depth.ShouldBe( 3 );
                }
                c.Depth.ShouldBe( 2 );
            }
            c.Depth.ShouldBe( 1 );
        }
        c.Depth.ShouldBe( 0 );
        c.ErrorCount.ShouldBe( 3 );
        c.UserMessages.Count.ShouldBe( 10 );
        c.UserMessages.Select( m => m.Depth.ToString() ).Concatenate().ShouldBe( "0, 1, 2, 3, 3, 3, 1, 2, 3, 4" );

        using( TestHelper.Monitor.CollectEntries( out var logs, LogLevelFilter.Info ) )
        {
            c.DumpLogs( TestHelper.Monitor );
            logs.Count.ShouldBe( 10 );
            logs[0].MaskedLevel.ShouldBe( LogLevel.Error );
            logs[0].Text.ShouldBe( "E" );
            logs[1].MaskedLevel.ShouldBe( LogLevel.Warn );
            logs[1].Text.ShouldBe( "W" );
            logs[2].MaskedLevel.ShouldBe( LogLevel.Info );
            logs[2].Text.ShouldBe( "I" );

            logs[3].MaskedLevel.ShouldBe( LogLevel.Error );
            logs[3].Text.ShouldBe( "Value -42 should be positive." );
            logs[4].MaskedLevel.ShouldBe( LogLevel.Warn );
            logs[4].Text.ShouldBe( "Value -42 should be positive." );
            logs[5].MaskedLevel.ShouldBe( LogLevel.Info );
            logs[5].Text.ShouldBe( "Value -42 should be positive." );

            logs[6].MaskedLevel.ShouldBe( LogLevel.Error );
            logs[6].Text.ShouldBe( "E2" );
            logs[7].MaskedLevel.ShouldBe( LogLevel.Warn );
            logs[7].Text.ShouldBe( "W2" );
            logs[8].MaskedLevel.ShouldBe( LogLevel.Info );
            logs[8].Text.ShouldBe( "I2" );

            logs[9].MaskedLevel.ShouldBe( LogLevel.Info );
            logs[9].Text.ShouldBe( "Done." );
        }

        c.Clear();
        c.Depth.ShouldBe( 0 );
        c.ErrorCount.ShouldBe( 0 );

    }

    [Test]
    public void no_invalid_UserMessage_can_appear()
    {
        var current = new CurrentCultureInfo( new TranslationService(), NormalizedCultureInfo.CodeDefault );
        var c = new UserMessageCollector( current );
        Util.Invokable( () => c.UserMessages.Add( new UserMessage() ) ).ShouldThrow<ArgumentException>();
        Util.Invokable( () => c.UserMessages.Insert( 0, new UserMessage() ) ).ShouldThrow<ArgumentException>();
    }

    [Test]
    public void ErrorCount_is_dynamically_tracked()
    {
        var current = new CurrentCultureInfo( new TranslationService(), NormalizedCultureInfo.CodeDefault );
        var c = new UserMessageCollector( current );
        c.ErrorCount.ShouldBe( 0 );
        c.Info( "Pop" );
        c.Warn( "Pop" );
        var e1 = c.Error( "Pop" );
        c.ErrorCount.ShouldBe( 1 );
        c.UserMessages.Add( e1 );
        c.ErrorCount.ShouldBe( 2 );
        c.UserMessages.Remove( e1 );
        c.ErrorCount.ShouldBe( 1 );
        c.UserMessages.Remove( e1 );
        c.ErrorCount.ShouldBe( 0 );
        c.UserMessages.Count.ShouldBe( 2 );

        c.UserMessages.Insert( 1, e1 );
        c.ErrorCount.ShouldBe( 1 );

        c.UserMessages.Insert( 3, e1 );
        c.ErrorCount.ShouldBe( 2 );

        c.UserMessages.Insert( 0, e1 );
        c.ErrorCount.ShouldBe( 3 );

        c.UserMessages.RemoveAt( 0 );
        c.ErrorCount.ShouldBe( 2 );

        c.UserMessages.RemoveAt( 1 );
        c.ErrorCount.ShouldBe( 1 );
    }
}
