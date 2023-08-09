using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Globalization.Tests
{
    [TestFixture]
    public class CodeStringTests
    {

        [SetUp]
        [TearDown]
        public void ClearCache()
        {
            typeof( NormalizedCultureInfo )
                .GetMethod( "ClearCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static )
                .Invoke( null, null );
        }

        static string ThisFile( [CallerFilePath] string? f = null ) => f;

        [Test]
        public void source_location_tests()
        {
            GlobalizationIssues.Track.IsOpen = true;
            Thread.Yield();

            var c1 = new CodeString( "plaitext" );
            // Let the async loop process the event. 
            Thread.Sleep( 20 );
            var c1Loc = GlobalizationIssues.GetSourceLocation( c1 );
            c1Loc[0].FilePath.Should().Be( ThisFile() );

            var c2 = new CodeString( "plaitext" );
            Thread.Sleep( 20 );
            var c1AndC2Loc = GlobalizationIssues.GetSourceLocation( c1 );
            c1AndC2Loc.Should().HaveCount( 2 );
            c1AndC2Loc.Should().BeEquivalentTo( GlobalizationIssues.GetSourceLocation( c2 ) );
            c1AndC2Loc[1].FilePath.Should().Be( c1AndC2Loc[0].FilePath ).And.Be( ThisFile() );
            c1AndC2Loc[1].LineNumber.Should().Be( c1AndC2Loc[0].LineNumber + 6 );
        }

        [Test]
        public void serialization_tests()
        {
            CheckSerialization( new CodeString( "" ) );
            CheckSerialization( new CodeString( "plain text" ) );
            CheckSerialization( new CodeString( NormalizedCultureInfo.GetNormalizedCultureInfo( "ar-tn" ), "plain text" ) );
            CheckSerialization( new CodeString( $"This {GetType().Name}." ) );
            CheckSerialization( new CodeString( NormalizedCultureInfo.GetNormalizedCultureInfo( "ar-tn" ), $"This {GetType().Name}." ) );

            foreach( var culture in CultureInfo.GetCultures( CultureTypes.AllCultures ).Select( c => NormalizedCultureInfo.GetNormalizedCultureInfo( c ) ) )
            {
                var d = new DateTime( 2023, 07, 27, 23, 59, 59, 999, DateTimeKind.Utc );
                var value = 37.12;
                var f = new CodeString( culture, $"{culture.Name} - {culture.Culture.EnglishName} - {culture.Culture.NativeName} - Date: {d:F}, V: {value:C}" );
                // Just for fun:
                // Console.WriteLine( f );
                CheckSerialization( f );
            }
            static void CheckSerialization( CodeString c )
            {
                // Versioned serializable.
                {
                    var bytes = c.SerializeVersioned();
                    var backC = SimpleSerializable.DeserializeVersioned<CodeString>( bytes );
                    CheckEquals( backC, c );
                    CheckEquals( SimpleSerializable.DeepCloneVersioned( c ), c );
                }
                // Simple serializable.
                {
                    var bytes = c.SerializeSimple();
                    var backC = SimpleSerializable.DeserializeSimple<CodeString>( bytes );
                    CheckEquals( backC, c );

                    CheckEquals( SimpleSerializable.DeepCloneSimple( c ), c );
                    CheckEquals( c.DeepClone(), c );
                }
            }

            static void CheckEquals( CodeString backC, CodeString c )
            {
                backC.Text.Should().Be( c.Text );
                backC.Placeholders.Should().BeEquivalentTo( c.Placeholders );
                backC.FormattedString.GetFormatString().Should().Be( c.FormattedString.GetFormatString() );
                backC.ContentCulture.Should().BeSameAs( c.ContentCulture );
                backC.GetPlaceholderContents().Select( c => c.ToString() ).Should().BeEquivalentTo( c.GetPlaceholderContents().Select( c => c.ToString() ) );
            }
        }

    }
}
