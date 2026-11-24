An opinionated i18n workflow: normalized cultures with their fallbacks, the active culture as an ambient service and
deferred translation of code-emitted interpolated strings.

The cultures are `NormalizedCultureInfo` and `ExtendedCultureInfo`, the active culture is `CurrentCultureInfo`, and
`FormattedString`, `CodeString`, `MCString` and `TranslationService` translate the strings. `UserMessage`,
`UserMessageCollector` and `MCException` build on them; `GlobalizationAgent` tracks the globalization issues.
