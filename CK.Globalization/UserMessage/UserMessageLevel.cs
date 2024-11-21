namespace CK.Core;

/// <summary>
/// Defines the <see cref="UserMessage.Level"/>.
/// </summary>
public enum UserMessageLevel
{
    /// <summary>
    /// Not applicable.
    /// </summary>
    None,

    /// <summary>
    /// Information message.
    /// </summary>
    Info = LogLevel.Info,

    /// <summary>
    /// Warning.
    /// </summary>
    Warn = LogLevel.Warn,

    /// <summary>
    /// Error message.
    /// </summary>
    Error = LogLevel.Error
}
