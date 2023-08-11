# ResultMessage helper

Its goal is to unify "result message" by providing:
- A `[MCString](../MCString.cs) Message` property .
- A `ResultMessageLevel Level` property that is an enum `Info`/`Warn`/`Error`.

Static factory methods can be used to create `ResultMessage` from a plain text, an
interpolated string, with the current culture by default or an explicit CultureInfo and
with or without a Resource name. Examples:

```csharp
/// <summary>
/// Creates a Error result message in the <see cref="NormalizedCultureInfo.Current"/> culture.
/// </summary>
/// <param name="plainText">The plain text.</param>
/// <param name="resName">The optional <see cref="ResName"/> of this result.</param>
/// <param name="filePath">Automatically set by the compiler.</param>
/// <param name="lineNumber">Automatically set by the compiler.</param>
/// <returns>A new Result message.</returns>
public static ResultMessage Error( string plainText,
                                    string? resName = null,
                                    [CallerFilePath] string? filePath = null,
                                    [CallerLineNumber] int lineNumber = 0 )

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

## Usages

The backend developer should return a result message (or a list of result messages) whenever
the message(s) is(are) aimed to be seen by a end user.

The message's format is always written in american english.
Even if the API can use the current culture, this should be avoided and the `culture` should be provided
explicitly:

```csharp
return ResultMessage.Error( culture, $"Upload failed after {tryCount} retries. Retrying in {(int)delay.TotalMinutes} minutes." );
```




