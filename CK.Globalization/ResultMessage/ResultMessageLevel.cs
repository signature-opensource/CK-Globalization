namespace CK.Core
{
    /// <summary>
    /// Defines the <see cref="ResultMessage.Level"/>.
    /// </summary>
    public enum ResultMessageLevel
    {
        /// <summary>
        /// Not applicable.
        /// </summary>
        None,

        /// <summary>
        /// Information message.
        /// </summary>
        Info = 4,

        /// <summary>
        /// Warning.
        /// </summary>
        Warn = 8,

        /// <summary>
        /// Error message.
        /// </summary>
        Error = 16
    }
}
