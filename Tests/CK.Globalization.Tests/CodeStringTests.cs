using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using static CK.Testing.MonitorTestHelper;

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
                .GetMethod( "ClearCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static )!
                .Invoke( null, null );
        }

        static string ThisFile( [CallerFilePath] string? f = null ) => f!;

        [Test]
        public void source_location_tests()
        {
            GlobalizationIssues.Track.IsOpen = true;

            var c1 = new CodeString( NormalizedCultureInfo.Invariant, "plaintext" );
            // Let the async loop process the event. 
            Thread.Sleep( 40 );
            var c1Loc = GlobalizationIssues.GetSourceLocation( c1 );
            c1Loc[0].FilePath.Should().Be( ThisFile() );

            var c2 = new CodeString( NormalizedCultureInfo.Invariant, "plaintext" );
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
            CheckSerializations( new CodeString( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "ar-tn" ), "plain text" ) );
            CheckSerializations( new CodeString( NormalizedCultureInfo.Invariant, "" ) );
            CheckSerializations( new CodeString( NormalizedCultureInfo.EnsureNormalizedCultureInfo( "ar-tn" ), $"This {GetType().Name}." ) );
            CheckSerializations( CodeString.Empty );

            foreach( var culture in CultureInfo.GetCultures( CultureTypes.AllCultures ).Select( c => NormalizedCultureInfo.EnsureNormalizedCultureInfo( c ) ) )
            {
                var d = new DateTime( 2023, 07, 27, 23, 59, 59, 999, DateTimeKind.Utc );
                var value = 37.12;
                var f = new CodeString( culture, $"{culture.Name} - {culture.Culture.EnglishName} - {culture.Culture.NativeName} - Date: {d:F}, V: {value:C}" );
                // Just for fun:
                // Console.WriteLine( f );
                CheckSerializations( f );
            }
            static string CheckSerializations( CodeString c )
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
                // Json
                string? text = null;
                TestHelper.JsonIdempotenceCheck( c, GlobalizationJsonHelper.WriteAsJsonArray, GlobalizationJsonHelper.ReadCodeStringFromJsonArray, null, t => text = t );
                Debug.Assert( text != null );
                return text;
            }

            static void CheckEquals( CodeString backC, CodeString c )
            {
                backC.Text.Should().Be( c.Text );
                backC.Placeholders.Should().BeEquivalentTo( c.Placeholders );
                backC.FormattedString.GetFormatString().Should().Be( c.FormattedString.GetFormatString() );
                backC.TargetCulture.Should().BeSameAs( c.TargetCulture );
                backC.GetPlaceholderContents().Select( c => c.ToString() ).Should().BeEquivalentTo( c.GetPlaceholderContents().Select( c => c.ToString() ) );
            }
        }

    }
}
