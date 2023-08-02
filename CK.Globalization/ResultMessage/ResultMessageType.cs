namespace CK.Core
{
    /// <summary>
    /// Defines the <see cref="ResultMessage.Type"/>.
    /// </summary>
    public enum ResultMessageType
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
