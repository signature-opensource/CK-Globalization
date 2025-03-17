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
            HandleLocaleFolder( monitor, true, localeRootPath, d, loadOnlyExisting );
        }
    }

    /// <summary>
    /// Read an Utf8 json stream of translations and throws on any error.
    /// <para>
    /// Json comments are skipped, trailing commas are allowed.
    /// The stream must contain an object with properties that can be other objects or strings.
    /// Subordinated objects are mapped to dot seprated property names in the result.
    /// </para>
    /// </summary>
    /// <param name="s">A Utf8 json stream.</param>
    /// <param name="skipComments">Whether comments are allowed and skipped (.jsonc) or forbidden (.json).</param>
    /// <returns>The translations.</returns>
    public static Dictionary<string, string> ReadJsonTranslationFile( Stream s, bool skipComments )
    {
        var options = new JsonReaderOptions
        {
            CommentHandling = skipComments ? JsonCommentHandling.Skip : JsonCommentHandling.Disallow,
            AllowTrailingCommas = true
        };
        using var context = Utf8JsonStreamReaderContext.Create( s, options, out var reader );
        var result = new Dictionary<string, string>();
        ReadJson( ref reader, context, result );
        return result;

        static void ReadJson( ref Utf8JsonReader r, IUtf8JsonReaderContext context, Dictionary<string, string> target )
        {
            if( r.TokenType == JsonTokenType.None && !r.Read() )
            {
                Throw.InvalidDataException( $"Expected a json object." );
            }
            Throw.CheckData( r.TokenType == JsonTokenType.StartObject );
            ReadObject( ref r, context, target, "" );

            static void ReadObject( ref Utf8JsonReader r, IUtf8JsonReaderContext context, Dictionary<string, string> target, string parentPath )
            {
                Throw.DebugAssert( r.TokenType == JsonTokenType.StartObject );
                Throw.DebugAssert( parentPath.Length == 0 || parentPath[^1] == '.' );

                r.ReadWithMoreData( context );
                while( r.TokenType == JsonTokenType.PropertyName )
                {
                    var propertyName = r.GetString();
                    Throw.CheckData( "Expected non empty property name.", !string.IsNullOrWhiteSpace( propertyName ) );
                    Throw.CheckData( "Property name cannot end or start with '.' nor contain '..'.",
                                     propertyName[0] != '.' && propertyName[^1] != '.' && !propertyName.Contains( ".." ) );
                    propertyName = parentPath + propertyName;
                    r.ReadWithMoreData( context );
                    if( r.TokenType == JsonTokenType.StartObject )
                    {
                        ReadObject( ref r, context, target, parentPath + propertyName + '.' );
                    }
                    else
                    {
                        Throw.CheckData( "Expected a string or an object.", r.TokenType == JsonTokenType.String );
                        if( !target.TryAdd( propertyName, r.GetString()! ) )
                        {
                            Throw.InvalidDataException( $"Duplicate key '{propertyName}' found." );
                        }
                    }
                    r.ReadWithMoreData( context );
                }
            }
        }
    }

    static void HandleLocaleFolder( IActivityMonitor monitor,
                                    bool isRoot,
                                    NormalizedPath localeRootPath,
                                    NormalizedPath subPath,
                                    bool loadOnlyExisting )
    {
        var cName = subPath.LastPart;
        if( !NormalizedCultureInfo.IsValidCultureName( cName ) )
        {
            monitor.Warn( $"Skipping directory '{subPath}' that has an invalid culture name." );
            return;
        }
        if( !isRoot )
        {
            int specificDepth = subPath.Parts.Count - localeRootPath.Parts.Count;
            if( specificDepth > 0 )
            {
                var cParentName = subPath.Parts[^2];
                if( cName.Length < cParentName.Length + 2 || cName[cParentName.Length] != '-' || !cName.StartsWith( cParentName, StringComparison.OrdinalIgnoreCase ) )
                {
                    monitor.Warn( $"Skipping directory '{subPath}'. Its name must start with: '{cParentName}-'." );
                    return;
                }
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
        foreach( var sub in Directory.GetDirectories( localeRootPath.AppendPart( cName ) ) )
        {
            HandleLocaleFolder( monitor, false, subPath, sub, loadOnlyExisting );
        }
    }

    static bool HandleTranslationFiles( IActivityMonitor monitor, NormalizedPath subPath, string cName, bool loadOnlyExisting )
    {
        var expectedFile = subPath.AppendPart( cName );
        bool isJsonC = false;
        var pJ = expectedFile + ".json";
        if( !File.Exists( pJ ) )
        {
            pJ = expectedFile + ".jsonc";
            if( !File.Exists( pJ ) )
            {
                monitor.Warn( $"Expected file '{pJ}.json' or '.jsonc'. Skipped directory." );
                return false;
            }
            isJsonC = true;
        }
        try
        {
            // Starts by loading the file before ensuring the Culture to avoid
            // a useless culture.
            Dictionary<string, string>? d;
            using( var content = File.OpenRead( pJ ) )
            {
                d = ReadJsonTranslationFile( content, skipComments: isJsonC );
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
