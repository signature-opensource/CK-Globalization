using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;

namespace CK.Core
{
    static class GlobalizationFile
    {
        public static void SetLocaleTranslationFiles( IActivityMonitor monitor, NormalizedPath localeRootPath )
        {
            Throw.CheckArgument( localeRootPath.IsRooted );
            foreach( var d in Directory.GetDirectories( localeRootPath ) )
            {
                HandleLocalFolder( monitor, localeRootPath, d );
            }
        }

        static void HandleLocalFolder( IActivityMonitor monitor, NormalizedPath localeRootPath, NormalizedPath subPath )
        {
            var cName = subPath.LastPart;
            if( !NormalizedCultureInfo.IsValidCultureName( cName ) )
            {
                monitor.Warn( $"Skipping directory '{subPath}' that has an invalid culture name." );
                return;
            }
            int specificDepth = subPath.Parts.Count - localeRootPath.Parts.Count - 1;
            if( specificDepth > 0 )
            {
                var cParentName = subPath.Parts[^2];
                if( cName.Length < cParentName.Length + 2 || cName[cParentName.Length] != '-' || !cName.StartsWith( cParentName, StringComparison.OrdinalIgnoreCase ) )
                {
                    monitor.Warn( $"Skipping directory '{subPath}'. Its name must start with: '{cParentName}-'." );
                    return;
                }
            }
            var expectedFile = subPath.AppendPart( cName );
            var pJ = expectedFile + ".json";
            if( !File.Exists( pJ ) )
            {
                pJ = expectedFile + ".jsonc";
                if( !File.Exists( pJ ) )
                {
                    monitor.Warn( $"Expected file '{pJ}.json' or '.jsonc'. Skipped directory." );
                    return;
                }
            }
            try
            {
                Dictionary<string, string>? d;
                using( var content = File.OpenRead( pJ ) )
                {
                    d = JsonSerializer.Deserialize<Dictionary<string, string>>( pJ );
                    if( d == null )
                    {
                        monitor.Error( $"Invalid file '{pJ}'. Null has been deserialized. Skipping directory." );
                        return;
                    }
                }
                var c = NormalizedCultureInfo.GetNormalizedCultureInfo( cName );
                var issues = c.SetCachedTranslations( d );
                if( issues.Any() )
                {
                    using( monitor.OpenWarn( $"File '{pJ}' has issues:" ) )
                    {
                        monitor.Warn( String.Join( Environment.NewLine, issues.Select( i => i.ToString() ) ) );
                    }
                }
                foreach( var sub in Directory.GetDirectories( localeRootPath ) )
                {
                    HandleLocalFolder( monitor, subPath, sub );
                }
            }
            catch( Exception ex )
            {
                monitor.Error( $"While processing file '{pJ}'.", ex );
            }
        }

    }
}
