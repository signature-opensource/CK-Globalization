using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Thread and concurrent safe tracker for existing an new culture: the abstract <see cref="InitializeAsync"/>
/// and <see cref="OnCultureCreatedAsync"/> are called sequentially.
/// </summary>
public abstract class ExtendedCultureInfoTracker : IDisposable
{
    int _active;

    /// <summary>
    /// Gets whether this tracker is active.
    /// To activate this tracker, call <see cref="ExtendedCultureInfo.StartTrackerAsync"/>.
    /// </summary>
    public bool IsActive => _active != 0;

    /// <summary>
    /// Stops tracking new culture. <see cref="IsActive"/> transitions to false.
    /// Calling <see cref="ExtendedCultureInfo.StartTrackerAsync"/> reinitializes it.
    /// </summary>
    public void Dispose()
    {
        if( Interlocked.CompareExchange( ref _active, 0, 1 ) == 0 )
        {
            ExtendedCultureInfo.CultureCreated.Async -= OnCultureCreatedAsync;
        }
    }

    internal Task DoInitializeAsync( IActivityMonitor monitor, AllCultureSnapshot allCultures, CancellationToken cancellationToken )
    {
        if( Interlocked.CompareExchange( ref _active, 1, 0 ) == 0 )
        {
            ExtendedCultureInfo.CultureCreated.Async += OnCultureCreatedAsync;
            return InitializeAsync( monitor, allCultures, cancellationToken );
        }
        return Task.CompletedTask;
    }

    protected abstract Task InitializeAsync( IActivityMonitor monitor, AllCultureSnapshot allCultures, CancellationToken cancellationToken );

    protected abstract Task OnCultureCreatedAsync( IActivityMonitor monitor, ExtendedCultureInfoCreatedEvent e, CancellationToken cancellationToken );



}
