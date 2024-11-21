namespace CK.Core;

/// <summary>
/// Options for deserializations that secures the creation of cultures.
/// This is agnostic of the serialization format.
/// </summary>
public interface IMCDeserializationOptions
{
    /// <summary>
    /// Gets whether <see cref="ExtendedCultureInfo.EnsureExtendedCultureInfo(string)"/> is used instead
    /// of the safer <see cref="ExtendedCultureInfo.FindBestExtendedCultureInfo(string, NormalizedCultureInfo)"/>.
    /// <para>
    /// Setting this to true must be used only for trusted inputs.
    /// </para>
    /// </summary>
    bool CreateUnexistingCultures { get; }

    /// <summary>
    /// Gets the default culture to use when <see cref="CreateUnexistingCultures"/> is false and a
    /// culture is not already locally defined.
    /// Defaults to <see cref="NormalizedCultureInfo.CodeDefault"/>.
    /// </summary>
    NormalizedCultureInfo? DefaultCulture { get; }
}
