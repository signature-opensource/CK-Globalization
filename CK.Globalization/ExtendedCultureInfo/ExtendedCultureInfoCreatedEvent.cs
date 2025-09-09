using System.Collections.Generic;

namespace CK.Core;

/// <summary>
/// Event arguments for <see cref="ExtendedCultureInfo.CultureCreated"/> event.
/// <para>
/// This event contains the snapshot of the current existing cultures. This can be used to track
/// cultures in a thread safe manner because this event is raised sequentially. The <see cref="ExtendedCultureInfoTracker"/>
/// implements tracking and should be used to mirror cultures in external repositories (typically in a database).
/// </para>
/// </summary>
/// <param name="All">The snapshot of the current existing cultures, inculding <paramref name="NewOne"/>.</param>
/// <param name="NewOne">The newly registered culture.</param>
public sealed record ExtendedCultureInfoCreatedEvent( AllCultureSnapshot All, ExtendedCultureInfo NewOne );
