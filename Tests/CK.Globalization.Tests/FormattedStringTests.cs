using CK.Core;
using CK.Testing;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using static CK.Testing.MonitorTestHelper;

namespace CK.Globalization.Tests
{
    [TestFixture]
    public class FormattedStringTests
    {
        [Test]
        public void plain_string_overload_no_placeholders()
        {
            var fStringOverload = new FormattedString( NormalizedCultureInfo.CodeDefault, "No placeholders" );
            fStringOverload.Placeholders.Should().BeEmpty();

            var fStringOverloadAlso = new FormattedString( NormalizedCultureInfo.CodeDefault, $"No placeholders" );
            fStringOverloadAlso.Placeholders.Should().BeEmpty();

            // Compile-time resolution.
            // We cannot do much about this: a formatted string with constants
            // will not have the same FormatString as the same formatted string with non-constants.
            var fStringOverloadAgain = new FormattedString( NormalizedCultureInfo.CodeDefault, $"A{"B"}C" );
            fStringOverloadAgain.Placeholders.Should().BeEmpty();
        }

        [TestCase( "A" )]
        [TestCase( "" )]
        [TestCase( null )]
        public void one_string_placeholder( string? s )
        {
            var fOneSlot = new FormattedString( NormalizedCultureInfo.CodeDefault, $"{s}" );
            fOneSlot.ToString().Should().Be( s ?? "" );
            fOneSlot.Placeholders.Should().HaveCount( 1 );
            fOneSlot.GetFormatString().Should().Be( "{0}" );
            fOneSlot.GetPlaceholderContents().Single().ToString().Should().Be( s ?? "" );
        }

        [Test]
        public void string_and_Guid_tests()
        {
            var g = Guid.NewGuid();
            var sG = g.ToString();

            Check( sG, new FormattedString( NormalizedCultureInfo.CodeDefault, $"Hop {sG}..." ) );
            Check( sG, new FormattedString( NormalizedCultureInfo.CodeDefault, $"Hop {g}..." ) );

            static void Check( string sG, FormattedString f )
            {
                f.ToString().Should().Be( $"Hop {sG}..." );
                var fmt = f.GetFormatString();
                fmt.Should().Be( "Hop {0}..." );
                var args = f.GetPlaceholderContents();
                args.Should().HaveCount( 1 );
                args.Single().ToString().Should().Be( sG );
                string.Format( fmt, args.Single() ).Should().Be( f.ToString() );
            }
        }

        class OString { public override string ToString() => "Here"; }

        [Test]
        public void string_and_object_tests()
        {
            object o = new OString();
            var sO = o.ToString()!;

            Check( sO, new FormattedString( NormalizedCultureInfo.CodeDefault, $"{sO}...{sO}" ) );
            Check( sO, new FormattedString( NormalizedCultureInfo.CodeDefault, $"{o}...{o}" ) );

            static void Check( string sO, FormattedString f )
            {
                f.ToString().Should().Be( $"{sO}...{sO}" );
                var fmt = f.GetFormatString();
                fmt.Should().Be( "{0}...{1}" );
                var args = f.GetPlaceholderContents();
                args.Should().HaveCount( 2 );
                args.ElementAt( 0 ).ToString().Should().Be( sO );
                args.ElementAt( 1 ).ToString().Should().Be( sO );
                string.Format( fmt, args.Select( a => a.ToString() ).ToArray() ).Should().Be( f.ToString() );
            }
        }

        [Test]
        public void string_and_DateTime_with_format_and_alignments()
        {
            var d = new DateTime( 2023, 07, 27, 23, 59, 59, 999, DateTimeKind.Utc );
            var sD = d.ToString( "O" );
            Debug.Assert( sD.Length == 28 );
            sD = new string( ' ', 10 ) + sD; // => Padding 38.

            Check( sD, new FormattedString( NormalizedCultureInfo.CodeDefault, $"Date {sD}!" ) );
            Check( sD, new FormattedString( NormalizedCultureInfo.CodeDefault, $"Date {d,38:O}!" ) );

            static void Check( string sD, FormattedString f )
            {
                f.ToString().Should().Be( $"Date {sD}!" );
                var fmt = f.GetFormatString();
                fmt.Should().Be( "Date {0}!" );
                var args = f.GetPlaceholderContents();
                args.Should().HaveCount( 1 );
                args.Single().ToString().Should().Be( sD );
                string.Format( fmt, args.Single() ).Should().Be( f.ToString() );
            }
        }

        [Test]
        public void with_braces()
        {
            var s = "b";
            var full = new FormattedString( NormalizedCultureInfo.CodeDefault, $"no {{ pro{s}lem }}" );
            full.Placeholders.Count().Should().Be( 1 );
            full.ToString().Should().Be( "no { problem }" );
            full.GetFormatString().Should().Be( "no {{ pro{0}lem }}" );
        }

        [Test]
        public void full_braces()
        {
            var full = new FormattedString( NormalizedCultureInfo.CodeDefault, $"{{}}{{}}{{}}}}}}}}{{{{{{{{{{{{{{{{{{{{{{{{{{" );
            full.Placeholders.Count().Should().Be( 0 );
            full.ToString().Should().Be( "{}{}{}}}}{{{{{{{{{{{{{" );
            full.GetFormatString().Should().Be( "{{}}{{}}{{}}}}}}}}{{{{{{{{{{{{{{{{{{{{{{{{{{" );
        }

        [Test]
        public void worst_braces_case()
        {
            var s = "";
            var full = new FormattedString( NormalizedCultureInfo.CodeDefault, $"{{{{{{{{{s}}}{{}}{{{{}}}}" );
            full.Placeholders.Count().Should().Be( 1 );
            full.GetPlaceholderContents().ElementAt( 0 ).Length.Should().Be( 0 );
            full.ToString().Should().Be( "{{{{}{}{{}}" );
            full.GetFormatString().Should().Be( "{{{{{{{{{0}}}{{}}{{{{}}}}" );
        }

        [Test]
        public void fully_empty_patterns()
        {
            var s = "";
            var empty = new FormattedString( NormalizedCultureInfo.CodeDefault, $"{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}{s}" );
            empty.Placeholders.Count().Should().Be( 24 );
            empty.GetPlaceholderContents().All( a => a.Length == 0 ).Should().BeTrue();
            empty.Text.Should().Be( "" );
            empty.GetFormatString().Should().Be( "{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}{15}{16}{17}{18}{19}{20}{21}{22}{23}" );
            empty.IsEmptyFormat.Should().BeFalse();
        }

        [Test]
        public void with_culture_info()
        {
            var enUS = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "en-US" );
            var frFR = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-FR" );

            var d = new DateTime( 2023, 07, 27, 23, 59, 59, 999, DateTimeKind.Utc );
            var value = 37.12;

            var inAmerica = new FormattedString( enUS, $"Date: {d:F}, V: {value:C}" );
            var inFrance = new FormattedString( frFR, $"Date: {d:F}, V: {value:C}" );

            inAmerica.Text.Should().Be( "Date: Thursday, July 27, 2023 11:59:59 PM, V: $37.12" );
            inFrance.Text.Should().Be( "Date: jeudi 27 juillet 2023 23:59:59, V: 37,12 €" );
            inAmerica.GetFormatString().Should().Be( "Date: {0}, V: {1}" )
                .And.Be( inFrance.GetFormatString() );

            inAmerica.GetPlaceholderContents().Select( a => a.ToString() )
                    .Should().BeEquivalentTo( new[] { "Thursday, July 27, 2023 11:59:59 PM", "$37.12" } );

            inFrance.GetPlaceholderContents().Select( a => a.ToString() )
                    .Should().BeEquivalentTo( new[] { "jeudi 27 juillet 2023 23:59:59", "37,12 €" } );

            inFrance.Culture.Should().BeSameAs( frFR );
            inAmerica.Culture.Should().BeSameAs( enUS );
        }

        [Test]
        public void serializations_tests()
        {
            CheckSerializations( FormattedString.Empty ).Should().Be( """["","",[]]""" );
            CheckSerializations( new FormattedString( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "ar-tn" ), "plain text" ) )
                .Should().Be( $"""["plain text","ar-tn",[]]""" ); ;

            foreach( var culture in CultureInfo.GetCultures( CultureTypes.AllCultures ).Select( c => NormalizedCultureInfo.EnsureNormalizedCultureInfo( c ) ) )
            {
                var d = new DateTime( 2023, 07, 27, 23, 59, 59, 999, DateTimeKind.Utc );
                var value = 37.12;
                var f = new FormattedString( culture, $"{culture.Name} - {culture.Culture.EnglishName} - {culture.Culture.NativeName} - Date: {d:F}, V: {value:C}" );
                // Just for fun:
                // Console.WriteLine( f );
                CheckSerializations( f );
            }

            static string CheckSerializations( FormattedString f )
            {
                // Versioned serializable.
                {
                    var bytes = f.SerializeVersioned();
                    var backF = SimpleSerializable.DeserializeVersioned<FormattedString>( bytes );
                    CheckEquals( backF, f );
                    CheckEquals( SimpleSerializable.DeepCloneVersioned( f ), f );
                }
                // Simple serializable.
                {
                    var bytes = f.SerializeSimple();
                    var backF = SimpleSerializable.DeserializeSimple<FormattedString>( bytes );
                    CheckEquals( backF, f );

                    CheckEquals( SimpleSerializable.DeepCloneSimple( f ), f );
                    CheckEquals( f.DeepClone(), f );
                }
                // Json
                string? text = null;
                TestHelper.JsonIdempotenceCheck( f,
                                                 GlobalizationJsonHelper.WriteAsJsonArray,
                                                 GlobalizationJsonHelper.ReadFormattedStringFromJsonArray,
                                                 null,
                                                 t => text = t );
                Debug.Assert( text != null );
                return text;
            }

            static void CheckEquals( FormattedString backF, FormattedString f )
            {
                backF.Text.Should().Be( f.Text );
                backF.Placeholders.Should().BeEquivalentTo( f.Placeholders );
                backF.GetFormatString().Should().Be( f.GetFormatString() );
                // Unfortunately, this is not guaranteed to the the same instance because
                // of the UseUserOverride flag: https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.useuseroverride?view=net-7.0#remarks
                if( backF.Culture != f.Culture )
                {
                    backF.Culture.Should().BeEquivalentTo( f.Culture );
                }
                backF.GetPlaceholderContents().Select( c => c.ToString() ).Should().BeEquivalentTo( f.GetPlaceholderContents().Select( c => c.ToString() ) );
            }
        }

        [Test]
        public void FormattedString_CreateFromProperties()
        {
            var c = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "ar-TN" );
            var f = FormattedString.CreateFromProperties( "ABCDEF", new[] { (0, 1), (1, 1), (2, 0), (4, 2) }, c );
            var args = f.GetPlaceholderContents().Select( c => c.ToString() ).ToArray();
            args[0].Should().Be( "A" );
            args[1].Should().Be( "B" );
            args[2].Should().Be( "" );
            args[3].Should().Be( "EF" );
            f.GetFormatString().Should().Be( "{0}{1}{2}CD{3}" );

            string? text = null;
            TestHelper.JsonIdempotenceCheck( f,
                                             GlobalizationJsonHelper.WriteAsJsonArray,
                                             GlobalizationJsonHelper.ReadFormattedStringFromJsonArray,
                                             null,
                                             t => text = t );
            text.Should().Be( """["ABCDEF","ar-tn",[0,1,1,1,2,0,4,2]]""" );

            FluentActions.Invoking( () => FormattedString.CreateFromProperties( "ABCDEF", new[] { (-1, 1) }, c ) )
                .Should().Throw<ArgumentException>();
            FluentActions.Invoking( () => FormattedString.CreateFromProperties( "ABCDEF", new[] { (100, 1) }, c ) )
                .Should().Throw<ArgumentException>();
            FluentActions.Invoking( () => FormattedString.CreateFromProperties( "ABCDEF", new[] { (0, 7) }, c ) )
                .Should().Throw<ArgumentException>();
            FluentActions.Invoking( () => FormattedString.CreateFromProperties( "ABCDEF", new[] { (0, 2), (1, 2) }, c ) )
                .Should().Throw<ArgumentException>();
        }

    }
}
