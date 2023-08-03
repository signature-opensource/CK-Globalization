# CK-Globalization

Managing cultures is never easy and is complicated by the legacy issues. This is an opinionated library that
aims to define a simple, good-enough, i18n workflow that minimizes the developer's burden. 

It is based on a Code-first approach: the developer writes and emits en-US texts directly in its code, using
interpolated strings. Placeholders are rendered immediately in the current culture. This current culture
being if possible an ubiquitous injected scoped service or fallbacks to the thread static [CultureInfo.CurrentCulture](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo.currentculture).

We currently don't exploit the .NET 8 [CompositeFormat](https://learn.microsoft.com/en-us/dotnet/api/system.text.compositeformat).
Using it requires more work from the developper: to emit a text, a CompositeFormat in the appropriate culture (the "current" one)
must be located first and then written/formatted with an array of objects. These objects must be compatible in terms of types
and cardinality with the CompositeFormat. Even if solutions that involve code generation exist like [TypealizR](https://github.com/earloc/TypealizR)
that secures this process by enforcing type safety, this is always more work for the developper.

Our approach is different. Instead of trying to obtain a format (the "enveloppe" of the text) before formatting,
we always format a text with a en-US format but with placeholders rendered in the "current" culture and captures
the resulting `Text`, the "current" `ContentCulture` and the placeholders text ranges. Armed with this, we can
*later* applies another format/enveloppe to this text and obtains the "translated" text.

An interesting side-effect of this deferred translation is that it is not required to be done on the original
system: the dictionary of translations can reside on a different system in a distributed system, freeing the
"edge agent" of this work.

## CultureInfo, NormalizedCultureInfo and ExtendedCultureInfo.
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
of the `NormalizedCultureInfo`. The latter normalizes the culture name (as a lower invariant string) and
carries a basic memory cache of available translations.

Note that for us, the 3 cultures on the path "en-US" - "en" - "" (Invariant) are *de facto* the same and cannot
have any cached translation dictionary.




