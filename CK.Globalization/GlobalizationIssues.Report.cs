using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Core;

public static partial class GlobalizationIssues
{
    /// <summary>
    /// Reported issue for "SHA.XXX" resource name that can reuse an existing explicit <see cref="CodeString.ResName"/>.
    /// </summary>
    /// <param name="Definer">The CodeString source location that explicitly defines its ResName.</param>
    /// <param name="AutomaticNames">Identical CodeString source location with automatic resource names.</param>
    public sealed record AutomaticResourceNamesCanUseExistingResName( CodeStringSourceLocation Definer,
                                                                      IReadOnlyList<CodeStringSourceLocation> AutomaticNames ) : Issue
    {
        /// <summary>
        /// Provides the description.
        /// </summary>
        /// <returns>This issue description.</returns>
        public override string ToString()
            => $"CodeString defined at '{AutomaticNames.Select( loc => loc.ToString() ).Concatenate( "', '" )}' can use " +
               $"the ResName '{Definer.ResName}' defined at '{Definer}'.";
    }

    /// <summary>
    /// Reported issue when different resource names are used for the same CodeString format: they may be merged.
    /// </summary>
    /// <param name="Duplicates">Identical CodeString source with different resource names.</param>
    public sealed record ResourceNamesCanBeMerged( IReadOnlyList<CodeStringSourceLocation> Duplicates ) : Issue
    {
        /// <summary>
        /// Provides the description.
        /// </summary>
        /// <returns>This issue description.</returns>
        public override string ToString()
            => $"Identical CodeString with different ResName detected: {Duplicates.Select( loc => $"'{loc.ResName}' at {loc}" ).Concatenate()}." +
               $" They can use the same ResName.";
    }

    /// <summary>
    /// Reported issue when the same resource name identifies different CodeString formats. This is bad and should be corrected.
    /// </summary>
    /// <param name="ResName">The ambiguous ResName.</param>
    /// <param name="Polysemics">At least 2 different CodeString that define the <see cref="ResName"/>.</param>
    public sealed record SameResNameWithDifferentFormat( string ResName, IReadOnlyList<CodeStringSourceLocation> Polysemics ) : Issue
    {
        /// <summary>
        /// Provides the description.
        /// </summary>
        /// <returns>This issue description.</returns>
        public override string ToString()
            => $"ResName '{ResName}' is used by different CodeString: {Polysemics.Select( loc => loc.ToString() ).Concatenate()}.";
    }

    /// <summary>
    /// Captures collected issues.
    /// </summary>
    public sealed class Report
    {
        internal Report( IReadOnlyList<CultureIdentifierClash> identifierClashes,
                         IReadOnlyList<MissingTranslationResource> missingTranslations,
                         IReadOnlyList<FormatArgumentCountError> formatArgumentCountErrors,
                         IReadOnlyList<AutomaticResourceNamesCanUseExistingResName> automaticResourceNamesCanUseExistingResName,
                         IReadOnlyList<ResourceNamesCanBeMerged> resourceNamesCanBeMerged,
                         IReadOnlyList<SameResNameWithDifferentFormat> sameResNameWithDifferentFormat )
        {
            CultureIdentifierClash = identifierClashes;
            MissingTranslationResource = missingTranslations;
            FormatArgumentCountError = formatArgumentCountErrors;
            AutomaticResourceNamesCanUseExistingResName = automaticResourceNamesCanUseExistingResName;
            ResourceNamesCanBeMerged = resourceNamesCanBeMerged;
            SameResNameWithDifferentFormat = sameResNameWithDifferentFormat;
        }

        /// <summary>
        /// See <see cref="GlobalizationIssues.CultureIdentifierClash"/>.
        /// </summary>
        public IReadOnlyList<CultureIdentifierClash> CultureIdentifierClash { get; }

        /// <summary>
        /// See <see cref="GlobalizationIssues.MissingTranslationResource"/>.
        /// </summary>
        public IReadOnlyList<MissingTranslationResource> MissingTranslationResource { get; }

        /// <summary>
        /// See <see cref="GlobalizationIssues.FormatArgumentCountError"/>.
        /// </summary>
        public IReadOnlyList<FormatArgumentCountError> FormatArgumentCountError { get; }

        /// <summary>
        /// See <see cref="GlobalizationIssues.AutomaticResourceNamesCanUseExistingResName"/>.
        /// </summary>
        public IReadOnlyList<AutomaticResourceNamesCanUseExistingResName> AutomaticResourceNamesCanUseExistingResName { get; }

        /// <summary>
        /// See <see cref="GlobalizationIssues.ResourceNamesCanBeMerged"/>.
        /// </summary>
        public IReadOnlyList<ResourceNamesCanBeMerged> ResourceNamesCanBeMerged { get; }

        /// <summary>
        /// See <see cref="GlobalizationIssues.SameResNameWithDifferentFormat"/>.
        /// </summary>
        public IReadOnlyList<SameResNameWithDifferentFormat> SameResNameWithDifferentFormat { get; }
    }

    /// <summary>
    /// Obtains a <see cref="Report"/> with the detected issues so far and clears the
    /// collected information or keeps them.
    /// </summary>
    /// <param name="reset">True to reset the collected information.</param>
    /// <returns>The current issues.</returns>
    public static Task<Report> GetReportAsync( bool reset )
    {
        var tcs = new TaskCompletionSource<Report>();
        _channel.Writer.TryWrite( new PrivateGetReport( tcs, reset ) );
        return tcs.Task;
    }

    static void HandleGetReport( PrivateGetReport report )
    {
        var missingTranslations = _missingTranslations?.Values.Select( c => new MissingTranslationResource( c ) ).ToArray() ?? Array.Empty<MissingTranslationResource>();
        var formatArgumentCountErrors = _formatArgumentError?.Values.ToArray() ?? Array.Empty<FormatArgumentCountError>();

        // Potential issues.
        List<AutomaticResourceNamesCanUseExistingResName>? automaticResourceNamesCanUseExistingResName = null;
        List<ResourceNamesCanBeMerged>? resourceNamesCanBeMerged = null;
        Dictionary<string, CodeStringSourceLocation[]>? sameResNameWithDifferentFormat = null;

        // This dictionary registers all ResName => Location. When there is more than one location
        // the sameResNameWithDifferentFormat is updated.
        var byDefinedResNameWithDifferentFormat = new Dictionary<string, CodeStringSourceLocation[]>();
        // Reusable variable and buffers.
        List<CodeStringSourceLocation> automaticResNames = new List<CodeStringSourceLocation>();
        List<CodeStringSourceLocation> definedResNames = new List<CodeStringSourceLocation>();
        foreach( var locations in _codeSringOccurrence.Values )
        {
            // If there's only one CodeString for the format, we only need to handle
            // its ResName.
            if( locations.Length == 1 )
            {
                AddByResName( ref sameResNameWithDifferentFormat, byDefinedResNameWithDifferentFormat, locations[0], locations );
                continue;
            }
            automaticResNames.Clear();
            definedResNames.Clear();
            foreach( var location in locations )
            {
                if( location.ResName.StartsWith( "SHA." ) )
                {
                    automaticResNames.Add( location );
                }
                else
                {
                    // Ignores the ones with the same ResName.
                    if( !definedResNames.Any( l => l.ResName == location.ResName ) )
                    {
                        definedResNames.Add( location );
                    }
                }
            }
            if( definedResNames.Count > 0 )
            {
                if( automaticResNames.Count > 0 )
                {
                    automaticResourceNamesCanUseExistingResName ??= new List<AutomaticResourceNamesCanUseExistingResName>();
                    automaticResourceNamesCanUseExistingResName.Add( new AutomaticResourceNamesCanUseExistingResName( definedResNames[0], automaticResNames.ToArray() ) );
                }
                if( definedResNames.Count >= 2 )
                {
                    resourceNamesCanBeMerged ??= new List<ResourceNamesCanBeMerged>();
                    resourceNamesCanBeMerged.Add( new ResourceNamesCanBeMerged( definedResNames.ToArray() ) );
                }
                foreach( var l in definedResNames )
                {
                    AddByResName( ref sameResNameWithDifferentFormat, byDefinedResNameWithDifferentFormat, l, null );
                }
            }
        }

        var sameResNameWithDifferentFormatList = sameResNameWithDifferentFormat == null
                                                    ? Array.Empty<SameResNameWithDifferentFormat>()
                                                    : sameResNameWithDifferentFormat.Select( kv => new SameResNameWithDifferentFormat( kv.Key, kv.Value ) )
                                                                                    .ToArray();

        report.TCS.SetResult( new Report( _identifierClashes,
                                          missingTranslations,
                                          formatArgumentCountErrors,
                                          automaticResourceNamesCanUseExistingResName?.ToArray() ?? Array.Empty<AutomaticResourceNamesCanUseExistingResName>(),
                                          resourceNamesCanBeMerged?.ToArray() ?? Array.Empty<ResourceNamesCanBeMerged>(),
                                          sameResNameWithDifferentFormatList ) );

        if( report.Reset ) ClearIssueCache();

        static void AddByResName( ref Dictionary<string, CodeStringSourceLocation[]>? sameResNameWithDifferentFormat,
                                  Dictionary<string, CodeStringSourceLocation[]> byDefinedResNameWithDifferentFormat,
                                  CodeStringSourceLocation loc,
                                  CodeStringSourceLocation[]? availableSingleArray )
        {
            if( byDefinedResNameWithDifferentFormat.TryGetValue( loc.ResName, out var otherFormats ) )
            {
                Array.Resize( ref otherFormats, otherFormats.Length + 1 );
                otherFormats[otherFormats.Length - 1] = loc;
                byDefinedResNameWithDifferentFormat[loc.ResName] = otherFormats;
                sameResNameWithDifferentFormat ??= new Dictionary<string, CodeStringSourceLocation[]>();
                sameResNameWithDifferentFormat[loc.ResName] = otherFormats;
            }
            else
            {
                byDefinedResNameWithDifferentFormat.Add( loc.ResName, availableSingleArray ?? new[] { loc } );
            }
        }
    }
}
