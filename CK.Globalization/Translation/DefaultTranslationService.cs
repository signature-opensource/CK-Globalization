using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Core
{

    public class DefaultTranslationService : ITranslationService
    {
        SimpleMapping _mapping;

        public DefaultTranslationService()
        {
            _mapping = new SimpleMapping( Array.Empty<SimpleMap>() );
        }

        public ValueTask<MCString> TranslateAsync( MCString s )
        {
            if( s.FormatCulture == s.ContentCulture ) return new ValueTask<MCString>( s );
            if( s.FormatCulture == "en" || s.FormatCulture == "en-us" )
        }

        public void SetTranslations( IActivityMonitor monitor, string cultureName, IEnumerable<(string ResName, string Format)> map )
        {
            Throw.CheckNotNullArgument( monitor );
            Throw.CheckArgument( cultureName != null
                                 && !cultureName.Equals( "en", StringComparison.OrdinalIgnoreCase )
                                 && !cultureName.Equals( "en-US", StringComparison.OrdinalIgnoreCase ) );
            var d = new Dictionary<string,PositionalCompositeFormat>();
            foreach( var (n,f) in map )
            {
                if( !PositionalCompositeFormat.TryParse( f, out var p, out var error ) )
                {
                    monitor.Warn( $"Invalid format string in '{cultureName}' for resource '{n}': {error}" );
                    continue;
                }
                d[n] = p;
            }
            Util.InterlockedSet( ref _mapping, exist => exist.AddOrReplace( cultureName, d) );
        }

        sealed class SimpleMap
        {
            public readonly Dictionary<string, PositionalCompositeFormat> Map;
            public readonly string Name;
            public readonly SimpleMap? Fallback;

            public SimpleMap( string name, Dictionary<string, PositionalCompositeFormat> map, SimpleMap? fallback )
            {
                Name = name;
                Map = map;
                Fallback = fallback;
            }
        }

        sealed class SimpleMapping
        {
            readonly SimpleMap[] _maps;

            public SimpleMapping( SimpleMap[] maps )
            {
                _maps = maps;
            }

            public SimpleMapping AddOrReplace( string name, Dictionary<string, PositionalCompositeFormat> map )
            {
                int idx = _maps.IndexOf( e => e.Name == name );
                SimpleMap[] newMap;
                if( idx >= 0 )
                {
                    newMap = (SimpleMap[])_maps.Clone();
                    newMap[idx] = new SimpleMap( name, map, _maps[idx].Fallback );
                }
                else
                {
                    SimpleMap? best = null;
                    var enumerator = GetFallbacks( name ).GetEnumerator();
                    bool hasMore;
                    while( hasMore = enumerator.MoveNext() )
                    {
                        var f = _maps.FirstOrDefault( m => m.Name == enumerator.Current );
                        if( f == null ) break;
                        best = f;
                    }
                    if( hasMore )
                    {
                        var multi = new List<SimpleMap>( _maps );
                        do
                        {
                            multi.Add( best = new SimpleMap( enumerator.Current, map, best ) );
                        }
                        while( enumerator.MoveNext() );
                        multi.Add( new SimpleMap( name, map, best ) );
                        newMap = multi.ToArray();
                    }
                    else
                    {
                        newMap = new SimpleMap[_maps.Length + 1];
                        _maps.CopyTo( newMap, 0 );
                        newMap[_maps.Length] = new SimpleMap( name, map, best );
                    }
                }
                return new SimpleMapping( newMap );
            }

            static IEnumerable<string> GetFallbacks( string name )
            {
                var f = name;
                var idx = f.LastIndexOf( '-' );
                if( idx < 0 ) yield break;
                f = f.Substring( 0, idx );
                yield return f;
            }
        }
    }
}
