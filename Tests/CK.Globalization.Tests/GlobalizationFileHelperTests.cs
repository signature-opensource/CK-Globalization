using CK.Core;
using NUnit.Framework;
using static CK.Testing.MonitorTestHelper;

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

        sDefault.Text.ShouldBe( "Headline" );
        sDE.Text.ShouldBe( "Schlagzeile in Deutschland" );
        sENUS.Text.ShouldBe( "Headline in the States" );
        sFR.Text.ShouldBe( "Gros titre" );
        sFRCA.Text.ShouldBe( "Gros titre au Quebec" );
        sFRFR.Text.ShouldBe( "Gros titre en France" );

        MCString.Create( frFR, "a page title", "Page.Title" ).Text.ShouldBe( "Titre de la page en France" );
        MCString.Create( frCA, "a page title", "Page.Title" ).Text.ShouldBe( "Titre de la page" );
        MCString.Create( de, "a page title", "Page.Title" ).Text.ShouldBe( "a page title" );
        MCString.Create( fr, "no place holder!", "Page.SubPage.Title" ).Text.ShouldBe( "Titre de la section." );


        static CurrentCultureInfo CreateFor( string name )
        {
            ExtendedCultureInfo? c = ExtendedCultureInfo.All.FindExtendedCultureInfo( name );
            Throw.DebugAssert( c != null );
            return new CurrentCultureInfo( new TranslationService(), c );
        }
    }
}
