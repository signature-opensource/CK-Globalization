namespace CK.Core
{
    /// <summary>
    /// Provides the <see cref="NormalizedCultureInfo.CodeDefault"/> to endpoint that don't resolve
    /// any ambient culture.
    /// </summary>
    public sealed class ExtendedCultureInfoUbiquitousServiceDefault : IEndpointUbiquitousServiceDefault<ExtendedCultureInfo>
    {
        /// <summary>
        /// Gets the <see cref="NormalizedCultureInfo.CodeDefault"/>.
        /// </summary>
        public ExtendedCultureInfo Default => NormalizedCultureInfo.CodeDefault;
    }
}
