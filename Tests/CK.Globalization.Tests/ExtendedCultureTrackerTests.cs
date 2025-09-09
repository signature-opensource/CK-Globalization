using CK.Core;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CK.Globalization.Tests;

[TestFixture]
public partial class ExtendedCultureTrackerTests
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
    public async Task ExtendedCulture_tracking_Async()
    {
        var t = new SampleTracker();
        await t.StartAsync();
        ExtendedCultureInfo.EnsureExtendedCultureInfo( "fr-fr, es, de-de" );
        // We use StopAsync to wait for the events to be consumed.
        await t.StopAsync();
        t.Table.Keys.ShouldBe( ["", "en", "fr", "fr-fr", "es", "de", "de-de", "fr-fr,es,de-de"], ignoreOrder: true );
    }

    [Test]
    public async Task NormalizedCulture_tracking_Async()
    {
        var t = new SampleTracker();
        await t.StartAsync();
        NormalizedCultureInfo.EnsureNormalizedCultureInfo( "pa-Guru-IN" );
        await t.StopAsync();
        t.Table.Keys.ShouldBe( ["", "en", "pa-guru", "pa", "pa-guru-in"], ignoreOrder: true );
    }

    [Test]
    public async Task SpecificCulture_tracking_Async()
    {
        var t = new SampleTracker();
        await t.StartAsync();
        var fr = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr" );
        await t.StopAsync();
        t.Table.Keys.ShouldBe( ["", "en", "fr"], ignoreOrder: true );

        // Accessing the SpecificCulture ensures it.
        await t.StartAsync();
        fr.SpecificCulture.Name.ShouldBe( "fr-fr" );
        await t.StopAsync();
        t.Table.Keys.ShouldBe( ["", "en", "fr", "fr-fr"], ignoreOrder: true );
    }

}
