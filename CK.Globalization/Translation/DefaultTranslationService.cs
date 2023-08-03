using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Core
{
    public class DefaultTranslationService : ITranslationService
    {
        public DefaultTranslationService()
        {
        }

        public ValueTask<MCString> TranslateAsync( MCString s )
        {
            return new ValueTask<MCString>( s );
        }
    }
}
