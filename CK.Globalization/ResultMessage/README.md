# ResultMessage helper

Its goal is to unify "result message" by providing:
- A `[MCString](../MCString.cs) Message` property .
- A `ResultMessageLevel Level` property that is an enum `Info`/`Warn`/`Error`.

Static factory methods can be used to create `ResultMessage` from a plain text, an
interpolated string, with an explicit `ExtendedCultureInfo` (ar a `NormalizedCultureInfo` that specializes
the `ExtendedCultureInfo`) and with or without a Resource name.
Example:

```csharp
  /// <summary>
  /// Creates a Info result message.
  /// </summary>
  /// <param name="culture">The culture used to format placeholders' content.</param>
  /// <param name="text">The interpolated text.</param>
  /// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
  /// <param name="filePath">Automatically set by the compiler.</param>
  /// <param name="lineNumber">Automatically set by the compiler.</param>
  /// <returns>A new Result message.</returns>
  public static ResultMessage Info( ExtendedCultureInfo culture,
                                    [InterpolatedStringHandlerArgument( nameof( culture ) )] FormattedStringHandler text,
                                    string? resName = null,
                                    [CallerFilePath] string? filePath = null,
                                    [CallerLineNumber] int lineNumber = 0 )
```

The backend developer should return a result message (or a list of result messages) whenever
the message(s) is(are) aimed to be seen by a end user.

The message's text is always written in american english.
Even if the API can use the current culture, this should be avoided and the `culture` should be provided
explicitly:

```csharp
return ResultMessage.Error( culture, $"Upload failed after {tryCount} retries. Retrying in {(int)delay.TotalMinutes} minutes." );
```
When the `culture` is the scoped `CurrentCultureInfo`, there is nothing more to do: the message has been
translated automatically (as long as there is a translation available for it).

When the `culture` is not specified, the `NormalizedCultureInfo.Current` is used (this should be avoided),
or when `culture` is a `ExtendedCultureInfo`, the message is not (yet) translated. It can be translated
later, typically when the result is sent back to the caller, and even on the caller side if the `ResultMessage`
has been marshalled with all its information.


