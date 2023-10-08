using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Basic translation service implementation that relies on the <see cref="NormalizedCultureInfo"/>' cached
    /// translations.
    /// This may be specialized to support asynchronous translations or other synchronous caches.
    /// </summary>
    public class TranslationService : ISingletonAutoService
    {
        /// <summary>
        /// Does its best to ensure that the returned <see cref="MCString.FormatCulture"/> is aligned with
        /// the <see cref="CodeString.TargetCulture"/> based on the available memory cached translations.
        /// <para>
        /// This is a synchronous method that works on the cached memory translations.
        /// </para>
        /// </summary>
        /// <param name="s">The string to translate.</param>
        /// <returns>A string with a format culture aligned to its content culture if possible.</returns>
        public virtual MCString Translate( CodeString s )
        {
            var r = TryTranslate( s.TargetCulture.PrimaryCulture, s );
            if( r == null )
            {
                foreach( var c in s.TargetCulture.Fallbacks )
                {
                    r = TryTranslate( c, s );
                    if( r != null ) break;
                }
                r ??= MCString.Create( s );
            }
            return r;
        }

        /// <summary>
        /// Gets whether <see cref="TranslateAsync(CodeString)"/> should be called because
        /// external translations repository may be exploited.
        /// <para>
        /// Always false for this default implementation.
        /// </para>
        /// </summary>
        public virtual bool SupportAsyncTranslation => false;

        /// <summary>
        /// Tries to get a good translation only. If a translation with a culture
        /// that <see cref="NormalizedCultureInfo.HasSameNeutral(NormalizedCultureInfo)"/> as the
        /// primary content culture cannot be found, null is returned.
        /// <para>
        /// This is an helper that can be called by a specialized <see cref="TranslateAsync(CodeString)"/>.
        /// </para>
        /// </summary>
        /// <param name="s">The code string to translate.</param>
        /// <returns></returns>
        protected static MCString? TryTranslateGood( CodeString s )
        {
            var primary = s.TargetCulture.PrimaryCulture;
            var r = TryTranslate( primary, s );
            if( r == null )
            {
                foreach( var c in s.TargetCulture.Fallbacks )
                {
                    if( !c.HasSameNeutral( primary ) ) break;
                    r = TryTranslate( c, s );
                    if( r != null ) break;
                }
            }
            return r;
        }

        static MCString? TryTranslate( NormalizedCultureInfo c, CodeString s )
        {
            if( c.IsDefault )
            {
                return MCString.Create( s );
            }
            if( c.TryGetCachedTranslation( s.ResName, out var translation ) )
            {
                return MCString.Create( c, translation, s );
            }
            return null;
        }

        /// <summary>
        /// Asynchronous translation that can use external translations repository to retrieve a missing translation.
        /// <para>
        /// This default implementation simply calls the synchronous <see cref="Translate(CodeString)"/>.
        /// This may be overridden if translations may be obtained from external
        /// repositories (and an async call is made to this method before returning
        /// a MCString).
        /// </para>
        /// </summary>
        /// <param name="s">The code string to translate.</param>
        /// <returns>A string with a format culture aligned to its content culture if possible.</returns>
        public virtual ValueTask<MCString> TranslateAsync( CodeString s )
        {
            return new ValueTask<MCString>( Translate( s ) );
        }

    }
}
