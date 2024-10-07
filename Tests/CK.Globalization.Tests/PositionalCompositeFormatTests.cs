using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CK.Globalization.Tests;

[TestFixture]
public class PositionalCompositeFormatTests
{
    [Test]
    public void check_memory_size()
    {
        if( Environment.Is64BitProcess )
        {
            Unsafe.SizeOf<PositionalCompositeFormat>().Should().Be( 24 );
        }
        else
        {
            Unsafe.SizeOf<PositionalCompositeFormat>().Should().Be( 16 );
        }
    }

    [Test]
    public void successful_parse()
    {
        Unsafe.SizeOf<PositionalCompositeFormat>().Should().Be( 24 );

        {
            PositionalCompositeFormat f = CreateAndCheckFormat( "A" );
            f.ExpectedArgumentCount.Should().Be( 0 );
            f.Format( "Any", "thing" ).Should().Be( "A" );
        }
        {
            var f = CreateAndCheckFormat( "{0}" );
            f.ExpectedArgumentCount.Should().Be( 1 );
            f.Format( "One", "nop" ).Should().Be( "One" );
        }
        {
            var f = CreateAndCheckFormat( "{99}" );
            f.ExpectedArgumentCount.Should().Be( 100 );
            f.Format( "Not", "enough" ).Should().Be( "" );
        }
        {
            var f = CreateAndCheckFormat( " {10}" );
            f.ExpectedArgumentCount.Should().Be( 11 );
            f.Format( "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "Got it!" ).Should().Be( " Got it!" );
        }
        {
            var f = CreateAndCheckFormat( "ab{1}" );
            f.ExpectedArgumentCount.Should().Be( 2 );
            f.Format( "0", "Got it!", "in excess" ).Should().Be( "abGot it!" );
        }
        {
            var f = CreateAndCheckFormat( "{2}ab" );
            f.ExpectedArgumentCount.Should().Be( 3 );
            f.Format( "0", "1", "Got it!", "in excess" ).Should().Be( "Got it!ab" );
        }
        {
            var f = CreateAndCheckFormat( "{{}}" );
            f.ExpectedArgumentCount.Should().Be( 0 );
            f.Format().Should().Be( "{}" );
        }
        {
            var f = CreateAndCheckFormat( "{{{0}}}" );
            f.ExpectedArgumentCount.Should().Be( 1 );
            f.Format( "Got it!" ).Should().Be( "{Got it!}" );
        }
        {
            var f = CreateAndCheckFormat( "{3}{{{2}}}{{{1}}}-{{{{{0}}}}}={0}{1}{2}{3}" );
            f.ExpectedArgumentCount.Should().Be( 4 );
            f.Format( "A", "B", "C" ).Should().Be( "{C}{B}-{{A}}=ABC" );
        }
    }

    static PositionalCompositeFormat CreateAndCheckFormat( string format )
    {
        var f = PositionalCompositeFormat.Parse( format );
        f.GetFormatString().Should().Be( format );
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
        PositionalCompositeFormat.TryParse( format, out var f, out var error ).Should().BeFalse();
        f.Should().BeEquivalentTo( PositionalCompositeFormat.Invalid );
        error.Should().Be( expectedError );
    }
}
