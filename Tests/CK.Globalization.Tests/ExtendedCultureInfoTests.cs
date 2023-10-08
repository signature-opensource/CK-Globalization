using CK.Core;
using CommunityToolkit.HighPerformance;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Globalization;
using System.Linq;

namespace CK.Globalization.Tests
{
    [TestFixture]
    public partial class ExtendedCultureInfoTests
    {
        [SetUp]
        [TearDown]
        public void ClearCache()
        {
            typeof( NormalizedCultureInfo )
                .GetMethod( "ClearCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static )!
                .Invoke( null, null );
        }

        // Single entry.
        [TestCase( "jp", "jp", false, "jp" )]
        // Basic list.
        [TestCase( " jp, fr ", "jp,fr", true, "jp,fr" )]
        [TestCase( "en, jp, fr", "en,jp,fr", true , "en,jp,fr" )]
        // Basic list (with duplicates).
        [TestCase(" jp, fr, jp, fr ", "jp,fr", true, "jp,fr")]
        [TestCase("en, jp, fr, fr, jp, en", "en,jp,fr", true, "en,jp,fr")]
        // A NormalizedCultureInfo expressed as its fallbacks.
        [TestCase( "FR-FR,FR", "fr-fr,fr", false, "fr-fr" )]
        // A NormalizedCultureInfo expressed as its fallbacks (with duplicates).
        [TestCase( "FR-FR,FR,fr-fr", "fr-fr,fr", false, "fr-fr" )]
        // Reordering leading to the NormalizedCultureInfo.
        [TestCase( "fr, fr-FR", "fr-fr,fr", false, "fr-fr" )]
        // Simple reordering.
        [TestCase("fr, fr-fr, en", "fr-fr,fr,en", true, "fr-fr,en")]
        [TestCase("fr, en, fr-fr", "fr-fr,fr,en", true, "fr-fr,en")]
        // Multiple reordering.
        [TestCase("fr, fr-fr, en, fr-ca, en-CA, fr-CH,en-BB", "fr-fr,fr-ca,fr-ch,fr,en-ca,en-bb,en", true, "fr-fr,fr-ca,fr-ch,en-ca,en-bb")]
        // 3-levels.
        [TestCase( "pa-Guru-IN,az-Cyrl-AZ", "pa-guru-in,pa-guru,pa,az-cyrl-az,az-cyrl,az", true, "pa-guru-in,az-cyrl-az" )]
        [TestCase( "pa-Guru,az-Cyrl", "pa-guru,pa,az-cyrl,az", true, "pa-guru,az-cyrl" )]
        [TestCase( "az, pa-Guru-IN, az-Cyrl, en, pa", "az-cyrl,az,pa-guru-in,pa-guru,pa,en", true, "az-cyrl,pa-guru-in,en" )]
        [TestCase( "az, pa, az-Cyrl-AZ, az-Cyrl, pa-Guru-IN", "az-cyrl-az,az-cyrl,az,pa-guru-in,pa-guru,pa", true, "az-cyrl-az,pa-guru-in" )]
        [TestCase( "pa-Guru-IN,en,pa-Guru", "pa-guru-in,pa-guru,pa,en", true, "pa-guru-in,en" )]
        //
        [TestCase( "st-ls,sl-si", "st-ls,st,sl-si,sl", true, "st-ls,sl-si" )]
        public void ExtendedCultureInfo_normalization( string names, string expectedFullName, bool isExtended, string expectedName )
        {
            var n = ExtendedCultureInfo.GetExtendedCultureInfo( names );
            n.FullName.Should().Be( expectedFullName );
            n.Name.Should().Be( expectedName );
            (isExtended == n is not NormalizedCultureInfo).Should().BeTrue( $"'{names}' => '{n.Name}'" );
            var n1 = ExtendedCultureInfo.GetExtendedCultureInfo( n.FullName );
            n1.Should().BeSameAs( n );
            var n2 = ExtendedCultureInfo.GetExtendedCultureInfo( n.Name );
            n2.Should().BeSameAs( n );
            var n3 = ExtendedCultureInfo.GetExtendedCultureInfo( n.Name.ToUpperInvariant() );
            n3.Should().BeSameAs( n );
            var n4 = ExtendedCultureInfo.GetExtendedCultureInfo( n.FullName.ToUpperInvariant() );
            n4.Should().BeSameAs( n );
        }

        [Test]
        public void inventing_cultures()
        {
            FluentActions.Invoking( () => new CultureInfo( "fr-fr-development" ) )
                .Should()
                .Throw<CultureNotFoundException>( "An invalid name (the 'development' subtag is longer than 8 characters) is the only way to not found a culture." );

            // Not cached CultureInfo can be created by newing it.
            {
                var cValid = new CultureInfo( "a-valid-name" );

                var cDevFR = new CultureInfo( "fr-fr-dev" );
                cDevFR.IsReadOnly.Should().BeFalse( "A non cached CultureInfo is mutable." );
                cDevFR.Name.Should().Be( "fr-FR-DEV", "Name is normalized accorcding to BCP47 rules..." );
                cDevFR.Parent.Name.Should().Be( "fr-FR", "The Parent is derived from the - separated names." );
                var cDevFRBack = CultureInfo.GetCultureInfo( "fr-fr-dev" );
                cDevFRBack.Should().NotBeSameAs( cDevFR, "Not cached." );
            }
            // CultureInfo is cached when CultureInfo.GetCultureInfo is used.
            {
                var cValid = CultureInfo.GetCultureInfo( "a-valid-name" );

                var cDevFR = CultureInfo.GetCultureInfo( "fr-fr-dev" );
                cDevFR.IsReadOnly.Should().BeTrue( "A cached culture info is read only." );
                cDevFR.Name.Should().Be( "fr-FR-DEV", "Name is normalized accorcding to BCP47 rules..." );
                cDevFR.Parent.Name.Should().Be( "fr-FR", "The Parent is derived from the - separated names." );
                var cDevFRBack = CultureInfo.GetCultureInfo( "fR-fR-dEv" );
                cDevFRBack.Should().BeSameAs( cDevFR, "...but lookup is case insensitive: this is why our ExtendedCultureInfo.Name is always lowered invariant." );
            }
        }

        [TestCase( -1149234858, "es-ec,fr-bj,fr-bl,ta-my,ff-latn-ng,tg-tj,aa-er", "kk,ms-sg,bs-latn,zh-hant,lag-tz,th,ru-kg")]
        [TestCase( 263746875, "en-mo,en-er,en-na,wae-ch,gsw,qu-bo,ff,ha-ne,ta-lk", "en-to,en-at,es-co,es-ph,az-latn-az,tr-cy,kkj,jmc" )]
        [TestCase( 783236085, "ksh,eo-001,en-fm,ebu-ke,tt-ru", "nnh,fr-gq,pt-br,th-th,qu-pe,prg,sma,mk,ru-md" )]
        [TestCase( -1634777652, "en-150,en-um,en-tt,haw-us,yo,pt-br,fy-nl,kok-in", "es-pa,gu,ee-tg,gsw" )]
        public void id_clash_detection_test( int idClash, string name1, string name2 )
        {
            // Listen to the issues. There must be only IdentifierClash.
            GlobalizationIssues.CultureIdentifierClash? clashDetected = null;
            PerfectEvent.SequentialEventHandler<GlobalizationIssues.Issue> detector = ( m, issue ) => clashDetected = (GlobalizationIssues.CultureIdentifierClash)issue;
            GlobalizationIssues.OnNewIssue.Sync += detector;
            try
            {
                var c1 = ExtendedCultureInfo.GetExtendedCultureInfo( name1 );
                c1.Id.Should().Be( idClash );
                c1.Name.GetDjb2HashCode().Should().Be( idClash );
                var c2 = ExtendedCultureInfo.GetExtendedCultureInfo( name2 );
                c2.Name.GetDjb2HashCode().Should().Be( idClash );
                c2.Id.Should().Be( idClash + 1 );
                // Wait for detection.
                while( clashDetected == null ) ;
                var clash = GlobalizationIssues.IdentifierClashes.Single( i => i.Name == name2 );
                clash.Id.Should().Be( idClash + 1 );
                clash.Clashes.Should().BeEquivalentTo( new[] { name1 } );
                clash.Should().BeSameAs( clashDetected );
            }
            finally
            {
                GlobalizationIssues.OnNewIssue.Sync -= detector;
            }
        }

    }
}
