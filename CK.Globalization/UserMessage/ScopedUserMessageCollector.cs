namespace CK.Core;

/// <summary>
/// Injectable reusable <see cref="UserMessageCollector"/> available as a Scoped service.
/// </summary>
public class ScopedUserMessageCollector : UserMessageCollector, IScopedAutoService
{
    /// <inheritdoc />
    public ScopedUserMessageCollector( CurrentCultureInfo culture )
        : base( culture )
    {
    }
}
