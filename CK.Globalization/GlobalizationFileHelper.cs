using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CK.Core;

/// <summary>
/// Implements simple translations file loader.
/// </summary>
public static class GlobalizationFileHelper
{
    /// <summary>
    /// Loads ".json" or ".jsonc" translation files from a root directory.
    /// Files must be located in their respective culture directory:
    /// <code>
    /// fr/
    ///   fr.json
    ///   fr-FR/
    ///     fr-FR.json
    ///   fr-CA/
    ///     fr-CA.json
    /// en/
    ///   en-us/
    ///     en-US.jsonc
    /// de/
    ///   de.jsonc
    /// </code>
    /// A parent folder MUST contain a translation file otherwise its children folders are skipped.
    /// This rules enforces the fact that a specific culture ("fr-FR") cannot be defined if its neutral
    /// culture ("fr") has no translations.
    /// <para>
    /// This rule doesn't apply to "en": "en" culture has no translation by design since it is the <see cref="NormalizedCultureInfo.CodeDefault"/>.
    /// </para>
    /// </summary>
    /// <param name="monitor">The monitor to use.</param>
    /// <param name="localeRootPath">The root path (must be <see cref="NormalizedPath.IsRooted"/>).</param>
    /// <param name="loadOnlyExisting">
    /// Optionnally restricts the load to already existing cultures.
    /// By default, cultures defined by files are created.
    /// </param>
    public static void SetLocaleTranslationFiles( IActivityMonitor monitor, NormalizedPath localeRootPath, bool loadOnlyExisting = false )
    {
        Throw.CheckArgument( localeRootPath.IsRooted );
        foreach( var d in Directory.GetDirectories( localeRootPath ) )
        {
            HandleLocalFolder( monitor, localeRootPath, d, loadOnlyExisting );
        }
    }

    static void HandleLocalFolder( IActivityMonitor monitor, NormalizedPath localeRootPath, NormalizedPath subPath, bool loadOnlyExisting )
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
        // Don't try to load a "en.json" file.
        if( !cName.Equals( "en", StringComparison.OrdinalIgnoreCase ) )
        {
            // If load fails, skip more specific cultures.
            if( !HandleTranslationFiles( monitor, subPath, cName, loadOnlyExisting ) )
            {
                return;
            }
        }
        foreach( var sub in Directory.GetDirectories( localeRootPath ) )
        {
            HandleLocalFolder( monitor, subPath, sub, loadOnlyExisting );
        }
    }

    static bool HandleTranslationFiles( IActivityMonitor monitor, NormalizedPath subPath, string cName, bool loadOnlyExisting )
    {
        var expectedFile = subPath.AppendPart( cName );
        var pJ = expectedFile + ".json";
        if( !File.Exists( pJ ) )
        {
            pJ = expectedFile + ".jsonc";
            if( !File.Exists( pJ ) )
            {
                monitor.Warn( $"Expected file '{pJ}.json' or '.jsonc'. Skipped directory." );
                return false;
            }
        }
        try
        {
            // Starts by loading the file before ensuring the Culture to avoid
            // a useless culture.
            Dictionary<string, string>? d;
            using( var content = File.OpenRead( pJ ) )
            {
                d = JsonSerializer.Deserialize<Dictionary<string, string>>( content );
                if( d == null )
                {
                    monitor.Error( $"Invalid file '{pJ}'. Null has been deserialized. Skipping directory." );
                    return false;
                }
            }
            var c = loadOnlyExisting
                        ? ExtendedCultureInfo.FindBestExtendedCultureInfo( cName, NormalizedCultureInfo.Invariant ).PrimaryCulture
                        : NormalizedCultureInfo.EnsureNormalizedCultureInfo( cName );
            // Name must match otherwise there's a problem.
            if( !cName.Equals( c.Name, StringComparison.OrdinalIgnoreCase ) )
            {
                string allNames = ExtendedCultureInfo.All.Select( c => c.Name ).Concatenate();
                if( loadOnlyExisting )
                {
                    monitor.Warn( $"File '{pJ}' does not correspond to an existing culture and loadOnlyExisting parameter is true. Skipping file.{Environment.NewLine}" +
                                  $"Existing cultures are: {allNames}." );
                }
                else
                {
                    monitor.Warn( $"File '{pJ}' resolved to the culture '{c.Name}'. Name must match. Skipping file.{Environment.NewLine}" +
                                  $"Existing cultures are: {allNames}." );
                }
                return false;
            }
            var issues = c.SetCachedTranslations( d );
            if( issues.Any() )
            {
                using( monitor.OpenWarn( $"File '{pJ}' has issues:" ) )
                {
                    monitor.Warn( String.Join( Environment.NewLine, issues.Select( i => i.ToString() ) ) );
                }
            }
        }
        catch( Exception ex )
        {
            monitor.Error( $"While processing file '{pJ}'.", ex );
            return false;
        }
        return true;
    }
}
