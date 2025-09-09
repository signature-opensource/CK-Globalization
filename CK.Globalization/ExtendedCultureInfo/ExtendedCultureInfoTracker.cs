using System.Threading;
using System.Threading.Tasks;

namespace CK.Core;

/// <summary>
/// Thread and concurrent safe tracker for existing and new culture: the abstract <see cref="InitializeAsync"/>
/// and <see cref="OnCultureCreatedAsync"/> and <see cref="OnStopAsync"/> are called sequentially.
/// </summary>
public abstract class ExtendedCultureInfoTracker
{
    int _isStarted;

    /// <summary>
    /// Gets whether this tracker is active.
    /// </summary>
    public bool IsStarted => _isStarted != 0;

    /// <summary>
    /// Starts this tracker.
    /// </summary>
    /// <returns>The awaitable.</returns>
    public Task StartAsync( CancellationToken cancellationToken = default )
    {
        if( Interlocked.CompareExchange( ref _isStarted, 1, 0 ) == 0 )
        {
            return GlobalizationAgent.StartTrackerAsync( this, cancellationToken );
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops tracking new culture. <see cref="IsStarted"/> transitions to false.
    /// Calling <see cref="StartAsync"/> reinitializes it.
    /// </summary>
    public Task StopAsync()
    {
        if( Interlocked.CompareExchange( ref _isStarted, 0, 1 ) == 1 )
        {
            return GlobalizationAgent.StopTrackerAsync( this );
        }
        return Task.CompletedTask;
    }

    internal Task DoStopAsync( IActivityMonitor monitor )
    {
        GlobalizationAgent.CultureCreated.Async -= OnCultureCreatedAsync;
        return OnStopAsync( monitor );
    }

    internal Task DoStartAsync( IActivityMonitor monitor, AllCultureSnapshot allCultures, CancellationToken cancellationToken )
    {
        GlobalizationAgent.CultureCreated.Async += OnCultureCreatedAsync;
        return InitializeAsync( monitor, allCultures, cancellationToken );
    }

    /// <summary>
    /// Initalizes this tracker with the all the currently existing cultures.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="allCultures">The current cultures snapshot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The awaitable.</returns>
    protected abstract Task InitializeAsync( IActivityMonitor monitor, AllCultureSnapshot allCultures, CancellationToken cancellationToken );

    /// <summary>
    /// Updates this tracker with a new registered culture.
    /// <para>
    /// The <see cref="ExtendedCultureInfoCreatedEvent.NewOne"/> is necessarily a new ExtendedCultureInfo.
    /// Its <see cref="ExtendedCultureInfo.Fallbacks"/> may already exist or not (they must be handled).
    /// The <see cref="ExtendedCultureInfo.PrimaryCulture"/> must also be handled if the new culture is a pure ExtendedCultureInfo
    /// (not a <see cref="NormalizedCultureInfo"/> one).
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="e">The new culture and the current snapshot.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The awaitable.</returns>
    protected abstract Task OnCultureCreatedAsync( IActivityMonitor monitor, ExtendedCultureInfoCreatedEvent e, CancellationToken cancellationToken );

    /// <summary>
    /// Called when stopped. Does nothing by default.
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <returns>The awaitable.</returns>
    protected virtual Task OnStopAsync( IActivityMonitor monitor ) => Task.CompletedTask;

}
