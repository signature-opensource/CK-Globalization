# CK-Globalization

Managing cultures is never easy and is complicated by the legacy issues (see this good
[SO answer](https://stackoverflow.com/a/71388328/190380) for an example and refer to https://www.rfc-editor.org/rfc/bcp/bcp47.txt
for the BCP47 norm that culture namig follows).

This library is an opinionated one that aims to define a simple, good-enough, i18n workflow that minimizes
the developer's burden. 

It is based on a Code-first approach: the developer writes and emits en-US texts directly in its code, using
interpolated strings. Placeholders are rendered immediately in the current culture. This current culture
being if possible an ubiquitous injected scoped service or fallbacks to the thread static [CultureInfo.CurrentCulture](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentculture).

We currently don't exploit the .NET 8 [CompositeFormat](https://learn.microsoft.com/en-us/dotnet/api/system.text.compositeformat).
Using it requires more work from the developper: to emit a text, a CompositeFormat in the appropriate culture (the "current" one)
must be located first and then written/formatted with an array of objects. These objects must be compatible in terms of types
and cardinality with the CompositeFormat (possible runtime errors here).
Even if solutions that involve code generation exist like [TypealizR](https://github.com/earloc/TypealizR)
that secures this process by enforcing type safety, this is always more work for the developper.

Our approach is different. Instead of trying to obtain a format (the "enveloppe" of the text) *before* formatting,
we always format a text with a en-US format but with placeholders rendered in the "current" culture and captures
the resulting `Text`, the "current" `ContentCulture` and the placeholders text ranges. Armed with this, we can
*later* applies another format/enveloppe to this text and obtains the "translated" text.

An interesting side-effect of this deferred translation is that the "translation" is not required to be executed on
the original system: the dictionary of translations can reside on a different system in a distributed system, freeing the
"edge agent" of the dictionaries/map cost.

## CultureInfo, NormalizedCultureInfo and ExtendedCultureInfo.
### About CultureInfo. 
CultureInfo have a [`Parent`](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.parent),
a more "general culture". This forms a tree with the [`CultureInfo.Invariant`](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.invariantculture)
at its root.

The path from a CutureInfo to the Invariant root acts as fallback chain:
```
pa-Guru-IN - Punjabi (Gurmukhi, India)
pa-Guru - Punjabi (Gurmukhi)
pa - Punjabi
Invariant
```
Every culture has 0 or more fallbacks ("fr-FR" fallbacks to "fr").
To capture this fallbacks and unify the API, we introduce the `ExtendedCultureInfo` that is a generalization
of the `NormalizedCultureInfo`. The former normalizes the culture name (as a lower invariant string: culture names
MUST be considered case insensitive) and holds the fallbacks, the latter carries a basic memory cache of
available translations.

Note that for us, the 3 cultures on the path "en-us" - "en" - "" (Invariant) are *de facto* the same and cannot
have any cached translation dictionary.

#### Inventing CultureInfo
CultureInfo can be created freely as long as the name is valid according to the BCP47 rules:
- By calling: `new CultureInfo( "a-valid-name" )`: the CultureInfo can then be mutated.
- By calling `CultureInfo.GetCultureInfo( "a-valid-name" )`: in this case, `CultureInfo.IsReadOnly` is true and it cannot be mutated.

The .NET cache behavior is far from perfect. One cannot create a new CultureInfo out of the blue, configure it and then cache it (which
should freeze it). The name management is surprising: the above name is "normalized" to 'a-VALID-NAME' but cache lookup is always case
insentitive.

```csharp
[Test]
public void inventing_cultures()
{
    FluentActions.Invoking( () => new CultureInfo( "fr-fr-development" ) )
        .Should()
        .Throw<CultureNotFoundException>( "An invalid name (the 'development' subtag is longer than 8 characters) is the only way to not found a culture." );

    // Not cached CultureInfo can be created by newing it.
    {
        var cValid = new CultureInfo( "a-valid-name" );

        var cDevFR = new CultureInfo( "fr-fr-dev" );
        cDevFR.IsReadOnly.Should().BeFalse( "A non cached CultureInfo is mutable." );
        cDevFR.Name.Should().Be( "fr-FR-DEV", "Name is normalized accorcding to BCP47 rules..." );
        cDevFR.Parent.Name.Should().Be( "fr-FR", "The Parent is derived from the - separated subtags." );
        var cDevFRBack = CultureInfo.GetCultureInfo( "fr-fr-dev" );
        cDevFRBack.Should().NotBeSameAs( cDevFR, "Not cached." );
    }
    // CultureInfo is cached when CultureInfo.GetCultureInfo is used.
    {
        var cValid = CultureInfo.GetCultureInfo( "a-valid-name" );

        var cDevFR = CultureInfo.GetCultureInfo( "fr-fr-dev" );
        cDevFR.IsReadOnly.Should().BeTrue( "A cached culture info is read only." );
        cDevFR.Name.Should().Be( "fr-FR-DEV", "Name is normalized accorcding to BCP47 rules..." );
        cDevFR.Parent.Name.Should().Be( "fr-FR", "The Parent is derived from the - separated subtags." );
        var cDevFRBack = CultureInfo.GetCultureInfo( "fR-fR-dEv" );
        cDevFRBack.Should().BeSameAs( cDevFR, "...but lookup is case insensitive: this is why our ExtendedCultureInfo.Name is always lowered invariant." );
    }
}
```

We introduce our own cache (reads are lock-free) of `ExtendedCultureInfo`/`NormalizedCultureInfo` that may be used to register
fully "invented cultures" but this is not its primary goal: its primary objective is efficiency and sound normalization of
culture names and fallbacks management.

#### The CultureInfo.LCID obsolescence
This property is very convenient (notably for database mapping) as it provides an integer identifier. Unfortunately, it should
no more be used as it was bound to Windows NLS sub-system implementation. ICU (that replaces NLS on .NET) has no integer identifier.

We introduce a new identifier, the `ExtendedCultureInfo.Id` that is the DBJ2 hash of our normalized culture name. This is somehow fragile
since collisions can occur but our tests have shown that they are quite exceptional and for very "artificial" fallbacks. Clashes are
detected and an explicit identifier assignation is done in such case.

> If this happens, this HAS TO BE HANDLED quickly and the clash must be hard coded in this library order
for the identifier to be shared accross systems.

This mechanism is a bet. Clashes should occur if a really big number of cultures are used. If managing the
"exceptions" happens to be unbearable, then we may extend the int identifier to a long (64 bits).

## Culture fallbacks
### ExtendedCultureInfo and NormalizedCultureInfo 
For (regular) `NormalizedCultureInfo` fallbacks are based on the `CultureInfo.Parent` path provided by the
.NET framework and cannot be altered: these are "intrinsic fallbacks".

"Intrinsic fallbacks" must be understood as "I'd prefer a translation adapted to my region (.NET Specific Culture
concept) if there's one, but I'm fine with any translation in my language (.NET Neutral Culture)".

We call "pure" `ExtendedCultureInfo` the cultures that are not `NormalizedCultureInfo`: these ones are
defined by their fallbacks and represent a "user preference list" (comma separated list of normalized
culture names): "jp,es,fr" is japanese first, then spanish and then french before giving up and use
the en-US default. This capability introduces some complexity and needs some design decisions discussed
below.

For translations, the `NormalizedCultureInfo` and its intrinsic fallbacks can be "good enough": the placeholders
are rendered in the primary culture ("es-ES" for instance). If we can't find a composite format from the
"en-US" code text to "es-ES" nor to "es" then we simply don't translate/reformat and expose the "en-US" text
with its "es-ES" placeholders. Translations SHOULD exist: the failure to find a translation is an exception,
a bug or an issue that must be fixed. Translations are a compact set of resources: there should be no "holes"
in them.

The "user preference list" notion supports another usage: it enables the **selection** of the "best" resource among
a possibly scarce set of translated resources. A typical use case is a document library that contains some
translated documents (but not all are translated). A pure `ExtendedCultureInfo` is identified by its comma separated
fallback names.

### Discussion: Translation vs. Selection, fallbacks normalization 
Our goal with the `ExtendedCultureInfo` is to support both scenario, even if our primary focus is about
translations. Unfortunately, these two usages possibly require/imply different interprations of the "user
preference list".

#### Fallbacks semantics
In both scenario, it is obvious that a parent culture (intrinsic fallback) appearing before a culture is stupid:
"es,fr,fr-ca" is either "es,fr-ca,fr" or "es,fr" because whatever is the resource (a document or a translated
resource), a "fr-ca" resource IS-A "fr" resource.

But then interpretations differ. About the "en-US code default" for instance:

- For translation, due to:
  - Our choice of "en-US"/"en" code defaults,
  - **and**, as we are NOT implementing a "general purpose i18n" library, we don't support
    translating an already translated string (translations always start from a "en-US", code emitted text),
    there is no point to have any of these defaults in a "user preference list": "en" and "en-us" are
    automatically removed.
- For selection, "fr,en,es" is a perfectly valid preference list: our "en-US code default" doesn't make sense in a document library.

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
consider the more than one specific case: "fr-ca, fr-ch". This makes sense: before falling back to the general "fr", the user would
like to have a canadian or switzerland specific resource. But what about this: "fr-ca, es, fr-ch"? This doesn't make sense
for translations (and is rather strange for selection) but "fr-ch" appears, it would be annoying to purely ignore it.

The normalization described below handles all these case.

#### Normalization
Normalization of a preference list can be done by applying the following rules:
- First, cultures are grouped by common parent, preserving the relative order of the children: groups are ordered based on
  the first occurence of itself or one of its more specific cultures.
- Then, groups are written, specific culture names coming first, ending with the group's name.

_Note:_ This normalization process is not based on the string names but on the hierarchy provided by the
CultureInfo objects. 

Examples:
- "fr,fr-ch,es,fr-ca" is normalized into "fr-ch,fr-ca,fr,es".
- "fr-fr,es,en-gb,es-bo,pa-guru" becomes "fr-fr,fr,es-bo,es,en-gb,en,pa-guru,pa"

This process is sound: it corrects what can be seen as "stupid input" (without losing any specified cultures) and is consistent
for translations and selections provided that for translations, we consider the "en" culture to
end the list (only "fr-fr,fr,es-bo,es,en-gb" will be considered for translations).

## NormalizedCulture cached transtations
All NormalizedCulture (except the "en", "en-us" and Invariant default ones) can have a cached translation set of resources.
It can always be set (the new one replaces the current one if any). This is an atomic operation (thread safe):

```csharp
/// <summary>
/// Sets a cached set of resource translation formats.
/// This must not be called for <see cref="ExtendedCultureInfo.IsDefault"/> otherwise
/// an <see cref="InvalidOperationException"/> is thrown.
/// <para>
/// When the static gate <see cref="GlobalizationIssues.Track"/> is opened, <see cref="GlobalizationIssues.ResourceFormatError"/>
/// are emitted for invalid format strings.
/// </para>
/// <para>
/// Duplicates can exist in the <paramref name="map"/>: the first resource name is kept, the subsequent
/// ones are discarded and a <see cref="GlobalizationIssues.ResourceFormatDuplicate"/> is emitted (when
/// the static gate <see cref="GlobalizationIssues.Track"/> is opened).
/// </para>
/// </summary>
/// <param name="map">The map.</param>
public void SetCachedTranslations( IEnumerable<(string ResName, string Format)> map )
```
The `Format` string is a positional-only composite format (see https://learn.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting):
only `{0}`, `{1}` etc. placeholders are allowed, without alignement nor format specifier. This format string is parsed and kept as a structure
optimized to generate strings. If the format cannot be parsed successfully, a `GlobalizationIssues.ResourceFormatError` is emitted.

As discussed before, translations must be "compact": a resource defined in "es-es" MUST be defined in "es" but because translations can be
set in any order (specific "es-es" before more generic "es") and setting the set is a lock-free operation, we don't check this
requirement. If a neutral (top culture like "es") misses a resource, a `GlobalizationIssues.MissingTranslationResource` will be emitted.

## ITranslationService and MCString.
The translation is primarily synchronous and relies on the basic cached translations carried by
the `NormalizedCultureInfo`. It can be specialized with more advanced caching capabilities and may
support asynchronous translation.

```csharp
public class TranslationService : ISingletonAutoService
{
    /// <summary>
    /// Does its best to ensure that the returned <see cref="MCString.FormatCulture"/> is aligned with
    /// the <see cref="CodeString.ContentCulture"/> based on the available memory cached translations.
    /// <para>
    /// This is a synchronous method that works on the cached memory translations.
    /// </para>
    /// </summary>
    /// <param name="s">The string to translate.</param>
    /// <returns>A string with a format culture aligned to its content culture if possible.</returns>
    public virtual MCString Translate( CodeString s ) { ... }

    /// <summary>
    /// Gets whether <see cref="TranslateAsync(CodeString)"/> should be called because
    /// external translations repository may be exploited.
    /// <para>
    /// Always false for this default implementation.
    /// </para>
    /// </summary>
    public virtual bool SupportAsyncTranslation => false;

    /// <summary>
    /// Asynchronous translation that can use external translations repository to retrieve a missing translation.
    /// <para>
    /// This default implementation simply calls the synchronous <see cref="Translate(CodeString)"/>.
    /// This may be overridden if translations may be obtained from external
    /// repositories (and an async call is made to this method before returning
    /// a MCString).
    /// </para>
    /// </summary>
    /// <param name="s">The code string to translate.</param>
    /// <returns>A string with a format culture aligned to its content culture if possible.</returns>
    public virtual ValueTask<MCString> TranslateAsync( CodeString s )
    {
        return new ValueTask<MCString>( Translate( s ) );
    }
}
```
The translated `MCString` wraps its source `CodeString` and exposes the `FormatCulture`, the translated `Text`
and a translation quality.

## The ExtendedCultureInfo is an Ubiquitous Endpoint Service.
The notion of "current culture" should better be named "ambient culture". As usual, this "ambient" notion
should be handled explicitly rather than relying on thread static, async-local-is-evil and other global
state management.

A `ExtendedCultureInfo` exists in any DI container and methods that need it have simply to require this
service. An there is more: the `CurrentCultureInfo` is a scoped service that captures the ubiquitous
`ExtendedCultureInfo` and the `TranslationService`: this CurrentCultureInfo enables a `MCString` to be
directly translated:

```csharp
return MCString.Create( culture, $"Hello {name}!", "SayHello" );
```
If the `culture` is a `CurrentCultureInfo` with a french culture and the cached translation on the 'fr'
`NormalizedCulture` contains the mapping {"SayHello","Bonjour {0}!"}, then the string is immediately
translated.

Note that this can only use the synchronous `TranslationService.Translate` method. If async translations
must be done, they have to be deferred (in an async context).

## The ResName is optional but important.
When a developper is in a hurry, he may not have time to choose and set a resource name for a CodeString.
In this case, an automatic resource name is computed: "SHA.v8xu6U8beqBaBHUJA-Jfk6cYiuA" for instance
is the default resource name of the plain text "text" (it is the base64url encoding of the SHA1 of the
format string).

This resource name can perfectly be used in the translation resources: the developper is not required
to choose a resource name. Translations can come later and provided without necessarily updating the
source code... or never if English is fine.

SHA1 automatic resource names never clash by design. When multiple locations in source code create
the same CodeString, their SHA1 are equal and the same resource will apply to all of them. If a developper
changes one of them, it will become "untranslated" until a dedicated resource is provided.

Explicit resource names seems better, but the same name can be defined by 2 different developpers (in 2
modules) with a totally different format string.

SHA1 or explicit, managing resource names requires to take care of: 
- A change in the number of placeholders (the user will see messages with "holes" in them).
- The removal of a CodeString (the resource will be defined for ever, polluting the system).

The good news is that all these issues *can be* tracked automatically. The bad news is that it requires
some work and not all the kind issues are covered.

## The GlobalizationIssues.




