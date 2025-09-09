using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CK.Core;

public static partial class GlobalizationAgent
{
    /// <summary>
    /// Generalizes all globalization issues.
    /// </summary>
    public abstract record Issue { }

    /// <summary>
    /// Identifier clashes are always tracked (the static gate <see cref="Track"/> is ignored).
    /// This MUST be handled by specifically hard coding the exception and release a new version of this library. 
    /// </summary>
    /// <param name="Name">
    /// The culture name or names that couldn't be identified by the DBJ2 hash code of its name because its hash
    /// is the same as the first <paramref name="Clashes"/> name.
    /// </param>
    /// <param name="Id">The final identifier that has been eventually assigned to <paramref name="Name"/>.</param>
    /// <param name="Clashes">One or more clashing names that shifted the <paramref name="Id"/> by 1.</param>
    public sealed record CultureIdentifierClash( string Name, int Id, IReadOnlyList<string> Clashes ) : Issue
    {
        /// <summary>
        /// Provides the description.
        /// </summary>
        /// <returns>This issue description.</returns>
        public override string ToString()
            => $"CultureInfo name identifier clash: '{Name}' has been associated to '{Id}' because of '{Clashes.Concatenate( "', '" )}'.";

    }

    /// <summary>
    /// Describes a resource format error emitted by <see cref="NormalizedCultureInfo.SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/>.
    /// This issue is logged only if the <see cref="Track"/> static gate is opened. It deosn't appear in the <see cref="IssuesReport"/>.
    /// </summary>
    /// <param name="Culture">The culture that contains the resource.</param>
    /// <param name="ResName">The resource name.</param>
    /// <param name="Format">The invalid format.</param>
    /// <param name="Error">The error message that contains the invalid <see cref="Format"/>.</param>
    public sealed record TranslationFormatError( NormalizedCultureInfo Culture, string ResName, string Format, string Error ) : Issue
    {
        /// <summary>
        /// Provides the description.
        /// </summary>
        /// <returns>This issue description.</returns>
        public override string ToString() => $"Invalid format for '{ResName}' in '{Culture.Name}': {Error}";
    }

    /// <summary>
    /// Describes a duplicate resource. Emitted by <see cref="NormalizedCultureInfo.SetCachedTranslations(IEnumerable{ValueTuple{string, string}})"/>.
    /// This issue is logged only if the <see cref="Track"/> static gate is opened. It deosn't appear in the <see cref="IssuesReport"/>.
    /// </summary>
    /// <param name="Culture">The culture that duplicates the resource.</param>
    /// <param name="ResName">The resource name.</param>
    /// <param name="Skipped">The skipped format.</param>
    /// <param name="Registered">The already registered format.</param>
    public sealed record TranslationDuplicateResource( NormalizedCultureInfo Culture,
                                                       string ResName,
                                                       PositionalCompositeFormat Skipped,
                                                       PositionalCompositeFormat Registered ) : Issue
    {
        /// <summary>
        /// Provides the description.
        /// </summary>
        /// <returns>This issue description.</returns>
        public override string ToString() => $"Duplicate resource '{ResName}' in '{Culture.Name}'. Skipped: '{Skipped.GetFormatString()}'.";
    }

    /// <summary>
        /// A missing resource has been detected.
        /// This is emitted when quality is <see cref="MCString.Quality.Bad"/> or <see cref="MCString.Quality.Awful"/>.
        /// </summary>
        /// <param name="Instance">
        /// The first instance with the <see cref="FormattedString"/> that lacks a translation.
        /// The same format may be shared by multiple <see cref="CodeStringSourceLocation"/>.
        /// </param>
    public sealed record MissingTranslationResource( CodeString Instance ) : Issue
    {
        /// <summary>
        /// The culture in which the resource should be defined.
        /// </summary>
        public NormalizedCultureInfo MissingCulture => Instance.TargetCulture.PrimaryCulture;

        /// <summary>
        /// The resource name to be defined.
        /// </summary>
        public string ResName => Instance.ResName;

        /// <summary>
        /// Provides the description.
        /// </summary>
        /// <returns>This issue description.</returns>
        public override string ToString()
            => $"Missing translation for '{ResName}' in '{MissingCulture.FullName}' at {GetSourceLocation( Instance ).Select( l => l.ToString() ).Concatenate()}.";
    }

    /// <summary>
    /// A <see cref="PositionalCompositeFormat"/> has not the same number of expected arguments as the code
    /// string has <see cref="FormattedString.Placeholders"/>.
    /// </summary>
    /// <param name="Format">The invalid format.</param>
    /// <param name="Instance">
    /// The first translated that raised the issue.
    /// The same format may be shared by multiple <see cref="CodeStringSourceLocation"/>.
    /// </param>
    public sealed record FormatArgumentCountError( PositionalCompositeFormat Format, MCString Instance ) : Issue
    {
        /// <summary>
        /// The resource culture.
        /// </summary>
        public NormalizedCultureInfo FormatCulture => Instance.FormatCulture;

        /// <summary>
        /// The resource name.
        /// </summary>
        public string ResName => Instance.CodeString.ResName;

        /// <summary>
        /// The number of arguments that <see cref="Format"/> expects.
        /// </summary>
        public int ExpectedArgumentCount => Format.ExpectedArgumentCount;

        /// <summary>
        /// The number of actual placeholders in the CodeString.
        /// </summary>
        public int PlaceholderCount => Instance.CodeString.FormattedString.Placeholders.Count;

        /// <summary>
        /// Provides the description.
        /// </summary>
        /// <returns>This issue description.</returns>
        public override string ToString()
            => $"Translation '{ResName}' in '{FormatCulture}' expects {ExpectedArgumentCount} arguments " +
               $"but CodeString has {PlaceholderCount} placeholders at {GetSourceLocation( Instance.CodeString ).Select( l => l.ToString() ).Concatenate()}.";
    }

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
    public sealed class IssuesReport
    {
        internal IssuesReport( IReadOnlyList<CultureIdentifierClash> identifierClashes,
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
        /// See <see cref="CultureIdentifierClash"/>.
        /// </summary>
        public IReadOnlyList<CultureIdentifierClash> CultureIdentifierClash { get; }

        /// <summary>
        /// See <see cref="MissingTranslationResource"/>.
        /// </summary>
        public IReadOnlyList<MissingTranslationResource> MissingTranslationResource { get; }

        /// <summary>
        /// See <see cref="FormatArgumentCountError"/>.
        /// </summary>
        public IReadOnlyList<FormatArgumentCountError> FormatArgumentCountError { get; }

        /// <summary>
        /// See <see cref="AutomaticResourceNamesCanUseExistingResName"/>.
        /// </summary>
        public IReadOnlyList<AutomaticResourceNamesCanUseExistingResName> AutomaticResourceNamesCanUseExistingResName { get; }

        /// <summary>
        /// See <see cref="ResourceNamesCanBeMerged"/>.
        /// </summary>
        public IReadOnlyList<ResourceNamesCanBeMerged> ResourceNamesCanBeMerged { get; }

        /// <summary>
        /// See <see cref="SameResNameWithDifferentFormat"/>.
        /// </summary>
        public IReadOnlyList<SameResNameWithDifferentFormat> SameResNameWithDifferentFormat { get; }
    }

    /// <summary>
    /// Obtains a <see cref="IssuesReport"/> with the detected issues so far and clears the
    /// collected information or keeps them.
    /// </summary>
    /// <param name="reset">True to reset the collected information.</param>
    /// <returns>The current issues.</returns>
    public static Task<IssuesReport> GetIssuesReportAsync( bool reset )
    {
        var tcs = new TaskCompletionSource<IssuesReport>();
        _channel.Writer.TryWrite( new IssuesReportRequest( tcs, reset ) );
        return tcs.Task;
    }

    static void HandleGetIssuesReport( IssuesReportRequest report )
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

        report.TCS.SetResult( new IssuesReport( _identifierClashes,
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
