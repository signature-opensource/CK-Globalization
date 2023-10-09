namespace CK.Core
{
    /// <summary>
    /// Provides the <see cref="NormalizedCultureInfo.CodeDefault"/> to endpoint that don't resolve
    /// any ambient culture.
    /// <para>
    /// This also satisfies <see cref="ExtendedCultureInfo"/> ambient service.
    /// </para>
    /// </summary>
    public sealed class NormalizedCultureInfoUbiquitousServiceDefault : IEndpointUbiquitousServiceDefault<NormalizedCultureInfo>
    {
        /// <summary>
        /// Gets the <see cref="NormalizedCultureInfo.CodeDefault"/>.
        /// </summary>
        public NormalizedCultureInfo Default => NormalizedCultureInfo.CodeDefault;
    }
}
