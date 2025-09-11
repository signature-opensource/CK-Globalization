using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core;

/// <summary>
/// Captures the existing cultures: <see cref="ExtendedCultureInfo.All"/> exposes them
/// and an up-to-date snapshot is also sent in the <see cref="ExtendedCultureInfoCreatedEvent.All"/> event.
/// </summary>
public readonly struct AllCultureSnapshot : IEnumerable<ExtendedCultureInfo>
{
    readonly Dictionary<object, ExtendedCultureInfo>? _all;

    internal AllCultureSnapshot( Dictionary<object, ExtendedCultureInfo> all )
    {
        _all = all;
    }

    /// <summary>
    /// Finds the best <see cref="ExtendedCultureInfo"/> from a comma separated list of culture names.
    /// <para>
    /// Currently, this ony returns NormalizedCultureInfo but this can be enhanced in the future.
    /// The order of the entries matters: "fr-CA, es-ES" with existing "fr-fr" and "es-es" cultures will select "fr".
    /// </para>
    /// </summary>
    /// <param name="commaSeparatedNames">Comma separated culture names.</param>
    /// <param name="defaultCulture">Ultimate default to consider.</param>
    /// <returns>The best existing culture.</returns>
    public ExtendedCultureInfo FindBestExtendedCultureInfo( string commaSeparatedNames, NormalizedCultureInfo defaultCulture )
    {
        Throw.CheckNotNullArgument( defaultCulture );
        if( _all == null ) return defaultCulture;

        var best = DoFindExtendedCultureInfo( ref commaSeparatedNames );
        if( best != null ) return best;

        var fullNames = commaSeparatedNames.Split( ',', StringSplitOptions.RemoveEmptyEntries );
        for( int i = 0; i < fullNames.Length; i++ )
        {
            string? one = fullNames[i];
            if( _all.TryGetValue( one, out best ) ) return best;
            var idx = one.LastIndexOf( '-' );
            while( idx > 1 )
            {
                one = one.Substring( 0, idx );
                if( _all.TryGetValue( one, out best ) ) return best;
                idx = one.LastIndexOf( '-' );
            }
        }
        return defaultCulture;
    }

    /// <summary>
    /// Tries to retrieve an already registered <see cref="ExtendedCultureInfo"/> from its identifier (the <see cref="ExtendedCultureInfo.Id"/>)
    /// or returns null.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The culture if found, null otherwise.</returns>
    public ExtendedCultureInfo? FindExtendedCultureInfo( int id ) => _all?.GetValueOrDefault( id );

    /// <summary>
    /// Tries to retrieve an already registered <see cref="ExtendedCultureInfo"/> from its <see cref="Name"/>
    /// or returns null.
    /// </summary>
    /// <param name="commaSeparatedNames">Comma separated culture names.</param>
    /// <returns>The culture if found, null otherwise.</returns>
    public ExtendedCultureInfo? FindExtendedCultureInfo( string commaSeparatedNames ) => DoFindExtendedCultureInfo( ref commaSeparatedNames );

    /// <summary>
    /// Tries to retrieve an already registered <see cref="NormalizedCultureInfo"/> from its <see cref="Name"/>
    /// or returns null.
    /// </summary>
    /// <param name="name">Culture name.</param>
    /// <returns>The culture if found, null otherwise.</returns>
    public NormalizedCultureInfo? FindNormalizedCultureInfo( string name ) => DoFindExtendedCultureInfo( ref name ) as NormalizedCultureInfo;


    internal ExtendedCultureInfo? DoFindExtendedCultureInfo( ref string commaSeparatedNames )
    {
        Throw.CheckNotNullArgument( commaSeparatedNames );
        ExtendedCultureInfo? e = null;
        // Fast path.
        if( _all != null && !_all.TryGetValue( commaSeparatedNames, out e ) )
        {
            // Let a chance to a very basic preprocessing.
            commaSeparatedNames = commaSeparatedNames.ToLowerInvariant().Replace( " ", "" );
            _all.TryGetValue( commaSeparatedNames, out e );
        }
        return e;
    }

    /// <summary>
    /// Gets the enumerator for all the cultures.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<ExtendedCultureInfo> GetEnumerator()
    {
        return _all?.Where( kv => ReferenceEquals( kv.Key, kv.Value.Name ) )
                    .Select( kv => kv.Value )
                    .GetEnumerator()
                ?? Enumerable.Empty<ExtendedCultureInfo>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
