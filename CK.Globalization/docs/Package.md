An opinionated i18n workflow that aims to minimize the developer's burden.

Cultures are normalized: an ExtendedCultureInfo is a culture with its fallbacks, a NormalizedCultureInfo
is a single one, and both are cached and comparable. The active culture flows through the DI container as
an ambient service rather than being passed around.

Messages carry their own translation state. A CodeString captures an interpolated string with the
positions of its placeholders, so it can be re-translated later without losing what was inserted; an
MCString is the result, and knows whether the translation happened. User messages, exceptions and
validation collectors build on those.
