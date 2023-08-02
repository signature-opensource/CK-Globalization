# ResultMessage helper

Its goal is to unify "result message" by providing:
- A `[FormattedString](../FormattedString/README.md) Message` property that captures
  a regular sting text, the culture and can compute the message format (with positional placeholders)
  when string interpolation has been used to format the message.
- A `ResultMessageType Type` property that is an enum `Info`/`Warn`/`Error`.
- An optional `string? MessageCode` property: an efficient and maintainable resource-like
  name ("UserManagent.BadEmail", "Security.AccessDenied", etc.) that can be used as a key
  to find a translation of the message's format.

Static factory methods can be used to create `ResultMessage` from a plain text, an
interpolated string, with the current culture by default or an explicit CultureInfo and
with or without a MessageCode. Examples:

```csharp
/// <summary>
/// Creates a Error result message.
/// </summary>
/// <param name="plainText">The plain text.</param>
/// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
/// <returns>A new Result message.</returns>
public static ResultMessage Error( string plainText, string? messageCode = null );

/// <summary>
/// Creates a Info result message.
/// </summary>
/// <param name="culture">The culture used to format placeholders' content.</param>
/// <param name="text">The interpolated text.</param>
/// <param name="messageCode">The optional <see cref="MessageCode"/>.</param>
/// <returns>A new Result message.</returns>
public static ResultMessage Info( CultureInfo culture, [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text, string? messageCode = null ) => new ResultMessage( ResultMessageType.Info, FormattedString.Create( ref text, culture ), messageCode );
```

Like `FormattedString`, a result message supports both simple and versioned serialization.

## Usages

The backend developer should return a result message (or a list of result messages) whenever
the message(s) is(are) aimed to be seen by a end user.

The message's format is always written in english:
```csharp
return ResultMessage.Error( $"Upload failed after {tryCount} retries. Retrying in {delay.TotalMiniutes} minutes." );
```
The user doesn't speak english. He is French.

In a "Front translation" scenario:
- The front receives the message, the interpolated string (unfortunately in english) but
  also the placeholders positions.





