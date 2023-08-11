using System;

namespace CK.Core
{
    /// <summary>
    /// Automatic scoped service that captures the <see cref="ExtendedCultureInfo"/> ubiquitous service
    /// and the singleton <see cref="ITranslationService"/>.
    /// </summary>
    public sealed class CurrentCultureInfo : IScopedAutoService, IFormatProvider
    {
        /// <summary>
        /// Initializes a new scoped <see cref="ExtendedCultureInfo"/>.
        /// </summary>
        /// <param name="translationService">The translation service.</param>
        /// <param name="currentCulture">The current culture.</param>
        public CurrentCultureInfo( TranslationService translationService, ExtendedCultureInfo currentCulture )
        {
            TranslationService = translationService;
            CurrentCulture = currentCulture;
        }

        /// <summary>
        /// Gets the translation service.
        /// </summary>
        public TranslationService TranslationService { get; }

        /// <summary>
        /// Gets the current culture.
        /// </summary>
        public ExtendedCultureInfo CurrentCulture { get; }

        object? IFormatProvider.GetFormat( Type? formatType ) => CurrentCulture.PrimaryCulture.Culture.GetFormat( formatType );
    }
}
