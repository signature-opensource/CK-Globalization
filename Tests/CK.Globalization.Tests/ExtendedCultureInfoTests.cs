using CK.Core;
using CommunityToolkit.HighPerformance;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

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
                .GetMethod( "ClearCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static )
                .Invoke( null, null );
        }

        // Single entry.
        [TestCase( "jp", "jp", false )]
        // Basic list.
        [TestCase( " jp, fr ", "jp,fr", true )]
        [TestCase( "en, jp, fr", "en,jp,fr", true )]
        // Basic list (with duplicates).
        [TestCase( " jp, fr, jp, fr ", "jp,fr", true )]
        [TestCase( "en, jp, fr, fr, jp, en", "en,jp,fr", true )]
        // A NormalizedCultureInfo expressed as its fallbacks.
        [TestCase( "FR-FR,FR", "fr-fr", false )]
        // A NormalizedCultureInfo expressed as its fallbacks (with duplicates).
        [TestCase( "FR-FR,FR,fr-fr", "fr-fr", false )]
        // Reordering leading to the NormalizedCultureInfo.
        [TestCase( "fr, fr-FR", "fr-fr", false )]
        // Simple reordering.
        [TestCase( "fr, fr-fr, en", "fr-fr,fr,en", true )]
        [TestCase( "fr, en, fr-fr", "fr-fr,fr,en", true )]
        // Multiple reordering.
        [TestCase( "fr, fr-fr, en, fr-ca, en-CA, fr-CH,en-BB", "fr-fr,fr-ca,fr-ch,fr,en-ca,en-bb,en", true )]
        // 3-levels.
        [TestCase( "pa-Guru-IN,az-Cyrl-AZ", "pa-guru-in,pa-guru,pa,az-cyrl-az,az-cyrl,az", true )]
        [TestCase( "pa-Guru,az-Cyrl", "pa-guru,pa,az-cyrl,az", true )]
        [TestCase( "az, pa-Guru-IN, az-Cyrl, en, pa", "az-cyrl,az,pa-guru-in,pa-guru,pa,en", true )]
        [TestCase( "az, pa, az-Cyrl-AZ, az-Cyrl, pa-Guru-IN", "az-cyrl-az,az-cyrl,az,pa-guru-in,pa-guru,pa", true )]
        [TestCase( "pa-Guru-IN,en,pa-Guru", "pa-guru-in,pa-guru,pa,en", true )]
        //
        [TestCase( "st-ls,sl-si", "st-ls,st,sl-si,sl", true )]
        public void ExtendedCultureInfo_normalization( string names, string expected, bool isExtended )
        {
            var n = ExtendedCultureInfo.GetExtendedCultureInfo( names );
            n.Name.Should().Be( expected );
            (isExtended == n is not NormalizedCultureInfo).Should().BeTrue( $"'{names}' => '{expected}'" );
            var n2 = ExtendedCultureInfo.GetExtendedCultureInfo( n.Name );
            n2.Should().BeSameAs( n );
            var n3 = ExtendedCultureInfo.GetExtendedCultureInfo( n.Name.ToUpperInvariant() );
            n3.Should().BeSameAs( n );
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

        [TestCase( 707030051, "ca-ad,ca,jv,bo-in,bo,kea,teo-ke,teo,en-vi,en-tt,en", "ebu,es-gq,es,brx-in,brx,hu,fr-fr,fr,en-cc,en")]
        [TestCase( -1361060459, "kam,shi-latn,shi,vi-vn,vi,uk,kea-cv,kea,vai", "en-gu,en,nmg,ln-cd,ln,sr-cyrl-rs,sr-cyrl,sr" )]
        [TestCase( -256573857, "en-ch,en,es-ec,es,zh,kln", "as,gu,bn,is-is,is,fr-rw,fr,sw-cd,sw,so-ke,so")]
        public void id_clash_detection_test( int idClash, string name1, string name2 )
        {
            // Listen to the issues. There must be only IdentifierClash.
            GlobalizationIssues.IdentifierClash? clashDetected = null;
            PerfectEvent.SequentialEventHandler<GlobalizationIssues.Issue> detector = ( m, issue ) => clashDetected = (GlobalizationIssues.IdentifierClash)issue;
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
