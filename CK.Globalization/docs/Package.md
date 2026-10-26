An opinionated i18n workflow: normalized cultures with their fallbacks (`NormalizedCultureInfo`, `ExtendedCultureInfo`),
the active culture as an ambient service (`CurrentCultureInfo`) and deferred translation of code-emitted interpolated
strings (`FormattedString`, `CodeString`, `MCString`, `TranslationService`). `UserMessage`, `UserMessageCollector` and
`MCException` build on them; `GlobalizationAgent` tracks the globalization issues.
