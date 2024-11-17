using CK.Core;
using FluentAssertions;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;
using static System.Net.Mime.MediaTypeNames;

namespace CK.Globalization.Tests;

[TestFixture]
public class GlobalizationFileHelperTests
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
    public void SetLocaleTranslationFiles_tests()
    {
        GlobalizationFileHelper.SetLocaleTranslationFiles( TestHelper.Monitor, TestHelper.TestProjectFolder.AppendPart( "TestLocales" ), loadOnlyExisting: false );

        CurrentCultureInfo en = CreateFor( "en" );
        CurrentCultureInfo de = CreateFor( "de" );
        CurrentCultureInfo enUS = CreateFor( "en-US" );
        CurrentCultureInfo fr = CreateFor( "fr" );
        CurrentCultureInfo frCA = CreateFor( "fr-CA" );
        CurrentCultureInfo frFR = CreateFor( "fr-FR" );

        var sDefault = MCString.Create( en, "Headline", "RootTitle" );
        var sDE = MCString.Create( de, "Headline", "RootTitle" );
        var sENUS = MCString.Create( enUS, "Headline", "RootTitle" );
        var sFR = MCString.Create( fr, "Headline", "RootTitle" );
        var sFRCA = MCString.Create( frCA, "Headline", "RootTitle" );
        var sFRFR = MCString.Create( frFR, "Headline", "RootTitle" );

        sDefault.Text.Should().Be( "Headline" );
        sDE.Text.Should().Be( "Schlagzeile in Deutschland" );
        sENUS.Text.Should().Be( "Headline in the States" );
        sFR.Text.Should().Be( "Gros titre" );
        sFRCA.Text.Should().Be( "Gros titre au Quebec" );
        sFRFR.Text.Should().Be( "Gros titre en France" );

        MCString.Create( frFR, "a page title", "Page.Title" ).Text.Should().Be( "Titre de la page en France" );
        MCString.Create( frCA, "a page title", "Page.Title" ).Text.Should().Be( "Titre de la page" );
        MCString.Create( de, "a page title", "Page.Title" ).Text.Should().Be( "a page title" );


        static CurrentCultureInfo CreateFor( string name )
        {
            ExtendedCultureInfo? c = ExtendedCultureInfo.FindExtendedCultureInfo( name );
            Throw.DebugAssert( c != null );
            return new CurrentCultureInfo( new TranslationService(), c );
        }
    }
}
