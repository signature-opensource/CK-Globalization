using System.Threading.Tasks;

namespace CK.Core
{

    /// <summary>
    /// Basic translation service implementation that relies on the <see cref="NormalizedCultureInfo"/>' cached
    /// translations. This can be specialized and the <see cref="OnTranslationNotFoundAsync"/> method can be overridden.
    /// </summary>
    public class DefaultTranslationService : ITranslationService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public ValueTask<TransString> TranslateAsync( CodeString s )
        {
            ValueTask<TransString> result = default;
            if( TryTranslate( s.ContentCulture.PrimaryCulture, s, ref result ) )
            {
                return result;
            }
            foreach( var c in s.ContentCulture.Fallbacks )
            {
                if( TryTranslate( c, s, ref result ) )
                {
                    return result;
                }
            }
            return OnTranslationNotFoundAsync( s );

            static bool TryTranslate( NormalizedCultureInfo c, CodeString s, ref ValueTask<TransString> result )
            {
                if( c.IsDefault )
                {
                    result = new ValueTask<TransString>( new TransString( s ) );
                    return true;
                }
                if( c.TryGetCachedTranslation( s.ResName, out var translation ) )
                {
                    var t = translation.Format( s.GetPlaceholderContents() );
                    result = new ValueTask<TransString>( new TransString( t, s, c ) );
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Extension point called when a translation has not been found in <see cref="NormalizedCultureInfo"/>
        /// cached translations for the <see cref="CodeString.ContentCulture"/>'s fallbacks.
        /// </summary>
        /// <param name="s">The string to translate.</param>
        /// <returns>The resulting translated string.</returns>
        protected virtual ValueTask<TransString> OnTranslationNotFoundAsync( CodeString s )
        {
            return new ValueTask<TransString>( new TransString( s ) );
        }
    }
}
