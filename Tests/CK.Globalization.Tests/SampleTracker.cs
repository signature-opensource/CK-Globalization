using CK.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace CK.Globalization.Tests;

sealed class SampleTracker : ExtendedCultureInfoTracker
{
    readonly Dictionary<string, ExtendedCultureInfo> _table;

    public SampleTracker()
    {
        _table = new Dictionary<string, ExtendedCultureInfo>();
    }

    public IReadOnlyDictionary<string, ExtendedCultureInfo> Table => _table;

    protected override Task InitializeAsync( IActivityMonitor monitor, AllCultureSnapshot allCultures, CancellationToken cancellationToken )
    {
        _table.Clear();
        foreach( var c in allCultures )
        {
            _table.Add( c.Name, c );
        }
        return Task.CompletedTask;
    }

    protected override Task OnCultureCreatedAsync( IActivityMonitor monitor, ExtendedCultureInfoCreatedEvent e, CancellationToken cancellationToken )
    {
        _table.ShouldNotBeEmpty();
        // Ensures that fallbacks are registered.
        foreach( var f in e.NewOne.Fallbacks )
        {
            _table[f.Name] = f;
        }
        if( e.NewOne is not NormalizedCultureInfo )
        {
            // Ensures that the PrimaryCulture is registered.
            _table[e.NewOne.PrimaryCulture.Name] = e.NewOne.PrimaryCulture;
        }
        // This MUST be a brand new culture: use Add to throw if it's not the case.
        _table.Add( e.NewOne.Name, e.NewOne );
        return Task.CompletedTask;
    }

    protected override Task OnStopAsync( IActivityMonitor monitor )
    {
        return Task.CompletedTask;
    }
}
