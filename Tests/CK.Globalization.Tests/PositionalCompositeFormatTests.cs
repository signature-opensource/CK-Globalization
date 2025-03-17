using CK.Core;
using Shouldly;
using NUnit.Framework;
using System;
using System.Runtime.CompilerServices;

namespace CK.Globalization.Tests;

[TestFixture]
public class PositionalCompositeFormatTests
{
    [Test]
    public void check_memory_size()
    {
        if( Environment.Is64BitProcess )
        {
            Unsafe.SizeOf<PositionalCompositeFormat>().ShouldBe( 24 );
        }
        else
        {
            Unsafe.SizeOf<PositionalCompositeFormat>().ShouldBe( 16 );
        }
    }

    [Test]
    public void successful_parse()
    {
        Unsafe.SizeOf<PositionalCompositeFormat>().ShouldBe( 24 );

        {
            PositionalCompositeFormat f = CreateAndCheckFormat( "A" );
            f.ExpectedArgumentCount.ShouldBe( 0 );
            f.Format( "Any", "thing" ).ShouldBe( "A" );
        }
        {
            var f = CreateAndCheckFormat( "{0}" );
            f.ExpectedArgumentCount.ShouldBe( 1 );
            f.Format( "One", "nop" ).ShouldBe( "One" );
        }
        {
            var f = CreateAndCheckFormat( "{99}" );
            f.ExpectedArgumentCount.ShouldBe( 100 );
            f.Format( "Not", "enough" ).ShouldBe( "" );
        }
        {
            var f = CreateAndCheckFormat( " {10}" );
            f.ExpectedArgumentCount.ShouldBe( 11 );
            f.Format( "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "Got it!" ).ShouldBe( " Got it!" );
        }
        {
            var f = CreateAndCheckFormat( "ab{1}" );
            f.ExpectedArgumentCount.ShouldBe( 2 );
            f.Format( "0", "Got it!", "in excess" ).ShouldBe( "abGot it!" );
        }
        {
            var f = CreateAndCheckFormat( "{2}ab" );
            f.ExpectedArgumentCount.ShouldBe( 3 );
            f.Format( "0", "1", "Got it!", "in excess" ).ShouldBe( "Got it!ab" );
        }
        {
            var f = CreateAndCheckFormat( "{{}}" );
            f.ExpectedArgumentCount.ShouldBe( 0 );
            f.Format().ShouldBe( "{}" );
        }
        {
            var f = CreateAndCheckFormat( "{{{0}}}" );
            f.ExpectedArgumentCount.ShouldBe( 1 );
            f.Format( "Got it!" ).ShouldBe( "{Got it!}" );
        }
        {
            var f = CreateAndCheckFormat( "{3}{{{2}}}{{{1}}}-{{{{{0}}}}}={0}{1}{2}{3}" );
            f.ExpectedArgumentCount.ShouldBe( 4 );
            f.Format( "A", "B", "C" ).ShouldBe( "{C}{B}-{{A}}=ABC" );
        }
    }

    static PositionalCompositeFormat CreateAndCheckFormat( string format )
    {
        var f = PositionalCompositeFormat.Parse( format );
        f.GetFormatString().ShouldBe( format );
        return f;
    }

    [TestCase( "{", "Unexpected end of string: '{'." )]
    [TestCase( "}", "Unexpected end of string: '}'." )]
    [TestCase( "ok {..not", "Expected argument index between 0 and 99 in 'ok {..not' at 4." )]
    [TestCase( "ok {}.not", "Unexpected '}' in 'ok {}.not' at 4." )]
    [TestCase( "{01}", "Argument number must not start with 0 in '{01}' at 1." )]
    [TestCase( "{0} - {12...", "Expected '}' in '{0} - {12...' at 9." )]
    [TestCase( "{0}{1,5}", "No alignment nor format specifier are allowed, expected '}' in '{0}{1,5}' at 5." )]
    [TestCase( "{0}{1:G}", "No alignment nor format specifier are allowed, expected '}' in '{0}{1:G}' at 5." )]
    [TestCase( "-{0}-{10,5}", "No alignment nor format specifier are allowed, expected '}' in '-{0}-{10,5}' at 8." )]
    [TestCase( "-{0}-{85:G}", "No alignment nor format specifier are allowed, expected '}' in '-{0}-{85:G}' at 8." )]
    public void error_parse( string format, string expectedError )
    {
        PositionalCompositeFormat.TryParse( format, out var f, out var error ).ShouldBeFalse();
        f.ShouldBe( PositionalCompositeFormat.Invalid );
        error.ShouldBe( expectedError );
    }
}
