using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Minimal translation service.
    /// <para>
    /// There is currently no fallback management and no user preference management.
    /// </para>
    /// </summary>
    public interface ITranslationService : ISingletonAutoService
    {
        /// <summary>
        /// Does its best to ensure that the <see cref="TransString.FormatCulture"/> is aligned with
        /// the <see cref="CodeString.ContentCulture"/>.
        /// </summary>
        /// <param name="s">The string to translate.</param>
        /// <returns>A string with a format culture aligned to its content culture if possible.</returns>
        ValueTask<TransString> TranslateAsync( CodeString s );
    }
}
