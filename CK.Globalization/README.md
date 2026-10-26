# CK.Globalization

Managing cultures is never easy and is complicated by the legacy issues (see this good
[SO answer](https://stackoverflow.com/a/71388328/190380) for an example and refer to https://www.rfc-editor.org/rfc/bcp/bcp47.txt
for the BCP47 norm that culture naming follows).

This library is an opinionated one that aims to define a simple, good-enough, i18n workflow that minimizes
the developer's burden.

It is based on a Code-first approach: the developer writes and emits en-US texts directly in its code, using
interpolated strings. Placeholders are rendered immediately in the culture that MUST be provided: there is **NO
implicit default culture**.

The "current" culture is an injected scoped service (the `CurrentCultureInfo`) or an explicit `ExtendedCultureInfo`.

> **Important:** This library doesn't use the static (and async local) [CultureInfo.CurrentCulture](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentculture).

We currently don't exploit the .NET 8 [CompositeFormat](https://learn.microsoft.com/en-us/dotnet/api/system.text.compositeformat).
Using it requires more work from the developer: to emit a text, a CompositeFormat in the appropriate culture (the "current" one)
must be located first and then written/formatted with an array of objects. These objects must be compatible in terms of types
and cardinality with the CompositeFormat (possible runtime errors here).
Even if solutions that involve code generation exist like [TypealizR](https://github.com/earloc/TypealizR)
that secures this process by enforcing type safety, this is always more work for the developer.

Our approach is different. Instead of trying to obtain a format (the "envelope" of the text) *before* formatting,
we always format a text with a "en" (our default) format but with placeholders rendered in the culture (provided by the DI)
and captures the resulting `Text`, the "current" `TargetCulture` and the placeholders text ranges. Armed with this, we can
*later* applies another format/envelope to this text and obtains the "translated" text.

An interesting side-effect of this deferred translation is that the "translation" is not required to be executed on
the original system: the dictionary of translations can reside on a different system in a distributed system, freeing the
"edge agent" of the dictionaries/map cost.

## Features

| Feature | Key types | Documentation |
|---------|-----------|---------------|
| Cultures, fallbacks and culture identifiers | `NormalizedCultureInfo`, `ExtendedCultureInfo`, `AllCultureSnapshot`, `ExtendedCultureInfoTracker` | [ExtendedCultureInfo](ExtendedCultureInfo/README.md) |
| Captured interpolated strings | `FormattedString`, `FormattedStringHandler` | [FormattedString](FormattedString/README.md) |
| Cached translations, translation service and resource names | `PositionalCompositeFormat`, `TranslationService`, `CodeString`, `MCString` | [Translation](Translation/README.md) |
| User messages and validation context | `UserMessage`, `SimpleUserMessage`, `UserMessageCollector`, `ScopedUserMessageCollector` | [UserMessage](UserMessage/README.md) |
| Ambient culture | `CurrentCultureInfo` | [below](#the-extendedcultureinfo-is-an-ambient-service) |
| Marshalling | `GlobalizationJsonHelper`, `IMCDeserializationOptions` | [below](#globalization-types-marshalling) |
| Culture aware exceptions | `MCException`, `UserMessageExceptionExtensions` | [below](#mcexception) |
| Globalization issues | `GlobalizationAgent` | [below](#the-globalizationagent-and-the-globalization-issues) |

## The ExtendedCultureInfo is an Ambient Service.
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
If the `culture` is a `CurrentCultureInfo` with a French culture and the cached translation on the 'fr'
`NormalizedCultureInfo` contains the mapping {"SayHello","Bonjour {0}!"}, then the string is immediately
translated.

Note that this can only use the synchronous `TranslationService.Translate` method. If async translations
must be done, they have to be deferred (in an async context).

This immediate translation capability is available on the `UserMessage` helper, the `MCString.Create`
factory method, but the `CurrentCultureInfo` itself exposes helpers that can easily create `MCString`,
`UserMessage` and `MCException`: these are the ones that should be used more often.

## Globalization types marshalling.
The `FormattedString`, `CodeString`, `MCString`, `UserMessage` and `SimpleUserMessage` types are serializable both
with the simple and versioned simple binary serialization.

However, we don't always want to carry all the information these types hold. In such case, these types
can be "simplified":
- The culture information are always marshalled by their name (simple string): calling
  `ExtendedCultureInfo.EnsureExtendedCultureInfo( commaSeparatedNames )` restores the corresponding culture info (be it
  a `NormalizedCultureInfo` or not).
- The formatted and code strings can be considered as simple strings, they can be implicitly cast into string
  (that is their respective `Text` property).
- `UserMessage` can be implicitly cast into `SimpleUserMessage` that holds the `Level`, `Depth` and
  the `Message` as a mere string.

With these projected types, all culture related information on strings is lost. They can typically be used
when exchanging with a "front" application that doesn't have to worry about translations.

Json support is available for all these objects in the static `GlobalizationJsonHelper` helper (Json
serialization doesn't pollute the API). The Json format uses array of values to be as compact as
possible. When reading, the cultures are resolved by `GlobalizationJsonHelper.ResolveCulture`: by default, only
already registered cultures are used (see `IMCDeserializationOptions`).

## MCException
The `MCException` has a `MCString Message { get; }` (hides the base `Exception.Message` property that is the `MCString.Text`).
It has numerous constructors. This type enables to throw culture aware exceptions and support a simple feature that is to secure
a little bit the _exception leak_ issue.

An extension method can recursively extract all `UserMessage` (with their depth) from an exception's message and its children.

```csharp
public static List<UserMessage> GetUserMessages( this Exception ex,
                                                 CurrentCultureInfo? culture,
                                                 string? defaultGenericMessage = "An unhandled error occurred.",
                                                 byte depth = 0,
                                                 bool? leakAll = null )
```

The `leakAll` parameter states whether all exceptions must be exposed or only the `MCException` ones. When null, it defaults
to `CoreApplicationIdentity.IsDevelopmentAndInitialized`: we want to be sure to be in "#Dev" environment to leak the exceptions.

The idea here is that if the developer took the time to emit a `MCException` rather than a mere exception, this exception is then
"safe enough" to be displayed to an end user.

## The GlobalizationAgent and the globalization issues.
The `GlobalizationAgent` static class centralizes the collect of issues and raises `OnNewIssue` event.
It is driven by a StaticGate:
```csharp
/// <summary>
/// The "CK.Core.GlobalizationIssues.Track" static gate is closed by default.
/// </summary>
public static readonly StaticGate Track;
```
Issues that are dynamically analyzed are:

- Calls to `NormalizedCultureInfo.SetCachedTranslations` can raise `TranslationDuplicateResource`
  and `TranslationFormatError`: these issues are returned to the caller of `SetCachedTranslations` and,
  when `Track` is opened, emitted by `OnNewIssue` and logged. They are not collected in the `IssuesReport`.
- `MissingTranslationResource` is emitted whenever a Bad or Awful translation is detected.
- `FormatArgumentCountError` is emitted whenever a translation format expects less or more arguments
  than a CodeString placeholders contains.
- The worst case: `CultureIdentifierClash` is always raised, even if the static gate `Track` is closed.
  This is a serious issue that must be urgently addressed (see [the culture identifiers](ExtendedCultureInfo/README.md#the-cultureinfolcid-obsolescence)).

`MissingTranslationResource` and `FormatArgumentCountError` events are memorized and the corresponding event
is raised only once.

All dynamic issues (except the `TranslationDuplicateResource` and `TranslationFormatError`) can be retrieved thanks
to the `GetIssuesReportAsync` method:
```csharp
/// <summary>
/// Obtains a <see cref="IssuesReport"/> with the detected issues so far and clears the
/// collected information or keeps them.
/// </summary>
/// <param name="reset">True to reset the collected information.</param>
/// <returns>The current issues.</returns>
public static Task<IssuesReport> GetIssuesReportAsync( bool reset )
```

The `GlobalizationAgent.IssuesReport` exposes the dynamic issues plus 3 other computed issues:

- `AutomaticResourceNamesCanUseExistingResName` when automatic "SHA.XXX" resource name can reuse
  an existing explicit ResName.
- `ResourceNamesCanBeMerged` when different resource names are used for the same CodeString format:
  they may be merged.
- `SameResNameWithDifferentFormat` when the same resource name identifies different CodeString formats.
  This is bad and should be corrected.

These computed issues rely on the `CodeString` source locations that are collected only when `Track` is opened.

## Requires.

- `CK.Abstractions` for the DI markers - `ExtendedCultureInfo` is an `IAmbientAutoService` -
  `CK.ActivityMonitor.SimpleSender`, and `CK.PerfectEvent` for the agent's event surface.
