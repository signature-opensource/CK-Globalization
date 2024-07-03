namespace CK.Core
{
    /// <summary>
    /// Provides the <see cref="NormalizedCultureInfo.CodeDefault"/> to endpoint that don't resolve
    /// any ambient culture.
    /// <para>
    /// This satisfies the <see cref="ExtendedCultureInfo"/> ambient service.
    /// </para>
    /// </summary>
    public sealed class NormalizedCultureInfoUbiquitousServiceDefault : IAmbientServiceDefaultProvider<NormalizedCultureInfo>
    {
        /// <summary>
        /// Gets the <see cref="NormalizedCultureInfo.CodeDefault"/>.
        /// </summary>
        public NormalizedCultureInfo Default => NormalizedCultureInfo.CodeDefault;
    }
}
