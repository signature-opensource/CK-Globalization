# Cultures: NormalizedCultureInfo and ExtendedCultureInfo

This folder contains the culture model of [CK.Globalization](../README.md): a normalized, cached and
comparable representation of cultures and of their fallbacks.

## About CultureInfo
CultureInfo have a [`Parent`](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.parent),
a more "general culture". This forms a tree with the [`CultureInfo.Invariant`](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.invariantculture)
at its root.

The path from a CultureInfo to the Invariant root acts as fallback chain:
```
pa-Guru-IN - Punjabi (Gurmukhi, India)
pa-Guru - Punjabi (Gurmukhi)
pa - Punjabi
Invariant
```
The fallbacks of a culture are that path stripped at both ends - without the culture itself and without
the Invariant root: the ordered `CultureInfo.Parent` chain from the most specific parent down to the
neutral culture. `"pa-Guru-IN"` has `pa-guru, pa`; `"fr-FR"` has just `fr`; and a neutral culture like
`"fr"` has none, *"the Invariant never appears in this list"*. That last case is worth noting because
the chain above ends on `Invariant`, which makes it look like a fallback. It is not.

That is the rule for one culture. The [`ExtendedCultureInfo`](ExtendedCultureInfo.cs) introduced below can also be a *pure* one,
defined by a list of cultures rather than derived from a single name - and there a neutral primary does
have fallbacks: `"fr,es"` falls back to `es`.
To capture this fallbacks and unify the API, we introduce the `ExtendedCultureInfo` that is a generalization
of the [`NormalizedCultureInfo`](NormalizedCultureInfo.cs). The former normalizes the culture name (as a lower invariant string: culture names
MUST be considered case insensitive) and holds the fallbacks, the latter carries a basic memory cache of
available translations (see [Translation](../Translation/README.md)).

Note that for us, the 2 cultures on the path "en" and "" (Invariant) are *de facto* the same and cannot
have any cached translation dictionary: they are the `NormalizedCultureInfo.CodeDefault` and
`NormalizedCultureInfo.Invariant`, and `ExtendedCultureInfo.IsDefault` is true for both.

### Inventing CultureInfo
CultureInfo can be created freely as long as the name is valid according to the BCP47 rules:
- By calling: `new CultureInfo( "a-valid-name" )`: the CultureInfo can then be mutated.
- By calling `CultureInfo.GetCultureInfo( "a-valid-name" )`: in this case, `CultureInfo.IsReadOnly` is true and it cannot be mutated.

The .NET cache behavior is far from perfect. One cannot create a new CultureInfo out of the blue, configure it and then cache it (which
should freeze it). The name management is surprising: the name casing is "normalized" (and this normalization varies across
platforms: "fr-fr-dev" becomes "fr-FR-DEV" or "fr-FR-dev") but cache lookup is always case insensitive.

The `static bool NormalizedCultureInfo.IsValidCultureName( ReadOnlySpan<char> name )` helper is available to check culture
name syntax but ultimately the framework's `CultureInfo.GetCultureInfo( string name )` decides (and may throw
the badly named `CultureNotFoundException`: InvalidCultureNameException would be better).

```csharp
[Test]
public void inventing_cultures()
{
    Util.Invokable( () => new CultureInfo( "fr-fr-development" ) )
        .ShouldThrow<CultureNotFoundException>( "An invalid name (the 'development' subtag is longer than 8 characters) is the only way to not found a culture." );

    // Not cached CultureInfo can be created by newing it.
    {
        // No exception.
        var cValid = new CultureInfo( "a-valid-name" );

        var cDevFR = new CultureInfo( "fr-fr-dev" );
        cDevFR.IsReadOnly.ShouldBeFalse( "A non cached CultureInfo is mutable." );
        if( cDevFR.Name == "fr-FR-DEV" || cDevFR.Name == "fr-FR-dev" )
        {
            // Name is normalized according to BCP47 rules and the tag DEV should be uppercase...
            // ...but on Appveyor, DEV is not uppercase (but FR is)...
            if( cDevFR.Name == "fr-FR-dev" )
            {
                TestHelper.Monitor.Warn( $"fr-fr-dev has been normalized by new CultureInfo() to {cDevFR.Name}." );
            }
        }
        else
        {
            cDevFR.Name.ShouldBe( "fr-fr-dev", "At least, the name must not be tampered (regardless casing)." );
        }
        cDevFR.Parent.Name.ShouldBe( "fr-FR", "The Parent is derived from the - separated names." );
        var cDevFRBack = CultureInfo.GetCultureInfo( "fr-fr-dev" );
        cDevFRBack.ShouldNotBeSameAs( cDevFR, "Not cached. Got another instance." );
    }
    // CultureInfo is cached when CultureInfo.GetCultureInfo is used.
    {
        var cDevFR = CultureInfo.GetCultureInfo( "fr-fr-dev" );
        cDevFR.IsReadOnly.ShouldBeTrue( "A cached culture info is read only." );
        if( cDevFR.Name == "fr-FR-DEV" || cDevFR.Name == "fr-FR-dev" )
        {
            // Name is normalized according to BCP47 rules and the tag DEV should be uppercase...
            // ...but on Appveyor, DEV is not uppercase (but FR is)...
            if( cDevFR.Name == "fr-FR-dev" )
            {
                TestHelper.Monitor.Warn( $"fr-fr-dev has been normalized by CultureInfo.GetCultureInfo to {cDevFR.Name}." );
            }
        }
        else
        {
            cDevFR.Name.ShouldBe( "fr-fr-dev", "At least, the name must not be tampered (regardless casing)." );
        }
        cDevFR.Parent.Name.ShouldBe( "fr-FR", "The Parent is derived from the - separated names." );
        var cDevFRBack = CultureInfo.GetCultureInfo( "fR-fR-dEv" );
        cDevFRBack.ShouldBeSameAs( cDevFR, "...but lookup is case insensitive: this is why our ExtendedCultureInfo.Name is always lowered invariant." );
    }
}
```

We introduce our own cache (reads are lock-free) of `ExtendedCultureInfo`/`NormalizedCultureInfo` that may be used to register
fully "invented cultures" but this is not its primary goal: its primary objective is efficiency and sound normalization of
culture names and fallbacks management.

The cultures are obtained (found or created) with:
- `NormalizedCultureInfo.EnsureNormalizedCultureInfo( string name )` or `EnsureNormalizedCultureInfo( CultureInfo cultureInfo )`;
- `ExtendedCultureInfo.EnsureExtendedCultureInfo( string commaSeparatedNames )`: if normalization results in a single
  culture, the `NormalizedCultureInfo` is returned.

`ExtendedCultureInfo.All` is an [`AllCultureSnapshot`](AllCultureSnapshot.cs) of the registered cultures that can find an
existing culture by name or `Id` without creating one (`FindBestExtendedCultureInfo` falls back to a default culture).
The `GlobalizationAgent.CultureCreated` event is raised for each new culture and an
[`ExtendedCultureInfoTracker`](ExtendedCultureInfoTracker.cs) can follow the existing and new cultures sequentially.

## The CultureInfo.LCID obsolescence
This property is very convenient (notably for database mapping) as it provides an integer identifier. Unfortunately, it should
no more be used as it was bound to Windows NLS sub-system implementation. ICU (that replaces NLS on .NET) has no integer identifier.

We introduce a new identifier, the `ExtendedCultureInfo.Id` that is the DJB2 hash of our normalized culture name. This is somehow fragile
since collisions can occur but our tests have shown that they are quite exceptional and for very "artificial" fallbacks. Clashes are
detected: the identifier is incremented until it is free. The final identifier then depends on the order in which the cultures
have been registered, so it is not shared across systems: a `GlobalizationAgent.CultureIdentifierClash` issue is always raised
and collected in `GlobalizationAgent.IdentifierClashes`, regardless of the `GlobalizationAgent.Track` static gate.

> If this happens, this HAS TO BE HANDLED quickly.

This mechanism is a bet: clashes should only occur if a really big number of cultures are used.

*Note:* The `Id` is the DJB2 hash minus 5381 (`ExtendedCultureInfo.GetZeroBasedDbj2HashCode`) so that the empty string is 0
(and this is the invariant). It is made positive for `NormalizedCultureInfo` and negative for "pure" `ExtendedCultureInfo` (see below).

## Culture fallbacks
### ExtendedCultureInfo and NormalizedCultureInfo
For (regular) `NormalizedCultureInfo` fallbacks are based on the `CultureInfo.Parent` path provided by the
.NET framework and cannot be altered: these are "intrinsic fallbacks".

"Intrinsic fallbacks" must be understood as "I'd prefer a translation adapted to my region (.NET Specific Culture
concept) if there's one, but I'm fine with any translation in my language (.NET Neutral Culture)".

We call "pure" `ExtendedCultureInfo` the cultures that are not `NormalizedCultureInfo`: these ones are
defined by their fallbacks and represent a "user preference list" (comma separated list of normalized
culture names): "ja,es,fr" is Japanese first, then Spanish and then French before giving up and use
the "en" default. This capability introduces some complexity and needs some design decisions discussed
below.

For translations, the `NormalizedCultureInfo` and its intrinsic fallbacks can be "good enough": the placeholders
are rendered in the primary culture ("es-ES" for instance). If we can't find a composite format from the
"en" code text to "es-ES" nor to "es" then we simply don't translate/reformat and expose the "en" text
with its "es-ES" placeholders. Translations SHOULD exist: the failure to find a translation is an exception,
a bug or an issue that must be fixed. Translations are a compact set of resources: there should be no "holes"
in them.

The "user preference list" notion supports another usage: it enables the **selection** of the "best" resource among
a possibly scarce set of translated resources. A typical use case is a document library that contains some
translated documents (but not all are translated). A pure `ExtendedCultureInfo` is identified by its comma separated
fallback names.

### Discussion: Translation vs. Selection, fallbacks normalization
Our goal with the `ExtendedCultureInfo` is to support both scenario, even if our primary focus is about
translations. Unfortunately, these two usages possibly require/imply different interpretations of the "user
preference list".

#### Fallbacks semantics
In both scenario, it is obvious that a parent culture (intrinsic fallback) appearing before a culture is stupid:
"es,fr,fr-ca" is either "es,fr-ca,fr" or "es,fr" because whatever is the resource (a document or a translated
resource), a "fr-ca" resource IS-A "fr" resource.

But then interpretations differ. About the "en" code default for instance:

- For translation, due to:
  - Our choice of "en" code defaults,
  - **and**, as we are NOT implementing a "general purpose i18n" library, we don't support
    translating an already translated string (translations always start from a "en", code emitted text),
    there is no point to have the default in a "user preference list": "en" ends the lookup.
- For selection, "fr,en,es" is a perfectly valid preference list: our "en" code default doesn't make sense
  in the context of a "document library".

> Fallbacks for translation and selection actually differ (at least for the "en" handling).

Let's consider this user preference: "pa-guru-in,es,fr-ca".
- For translation:
  - The placeholders have been rendered in "pa-guru-in" (the PrimaryCulture). It doesn't make sense to lookup for "es"
    if a translation in "pa-guru" or "pa" exists: intrinsic fallbacks should be honored first, at least for the PrimaryCulture.
  - The lookup list is: "pa-guru-in,pa-guru,pa,es,fr-ca,fr"
- For selection:
  - It is less obvious. What is the user intent? The lookup list may be the same as above or should we strictly honor the
    list by putting the intrinsic fallbacks after it, leading to "pa-guru-in,es,fr-ca,pa-guru,pa,fr"?.
  - This can be discussed but we'll keep the translation proposal here.

We previously stated that a "more general" culture cannot precede a specific one ("fr,fr-ca" is "fr"). Now let's
consider the "more than one specific cultures" case: "fr-ca, fr-ch". This makes sense: before falling back to the general "fr", the user would
like to have a Canadian or Switzerland specific resource. But what about this: "fr-ca, es, fr-ch"? This doesn't make sense
for translations (and is rather strange for selection) but "fr-ch" appears, it would be annoying to purely ignore it.

The normalization described below handles all these case.

#### Normalization
Normalization of a preference list can be done by applying the following rules:
- First, cultures are grouped by common parent, preserving the relative order of the children: groups are ordered based on
  the first occurrence of itself or one of its more specific cultures.
- Then, groups are written, specific culture names coming first, ending with the parent cultures' name.

_Note:_ This normalization process is not based on the string names but on the hierarchy provided by the
CultureInfo objects.

Examples:
- "fr,fr-ch,es,fr-ca" is normalized into "fr-ch,fr-ca,fr,es".
- "fr-fr,es,en-gb,es-bo,pa-guru" becomes "fr-fr,fr,es-bo,es,en-gb,en,pa-guru,pa"

This process is sound: it corrects what can be seen as "stupid input" (without losing any specified cultures) and is consistent
for translations and selections provided that for translations, we consider the "en" culture to
end the list (only "fr-fr,fr,es-bo,es,en-gb" will be considered for translations).

The resulting `ExtendedCultureInfo` exposes its `PrimaryCulture` (the first culture) and its `Fallbacks` (the following ones).
Its `FullName` is the whole normalized list while its `Name` is the shortest list that produces it ("fr-ch,fr-ca,es"
for the first example above).
