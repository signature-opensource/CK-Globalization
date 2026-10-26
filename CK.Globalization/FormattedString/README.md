# FormattedString

A [`FormattedString`](FormattedString.cs) is a capture of an interpolated string:
- `Text` is the usual final interpolated string.
- `Placeholders` are all the occurences (Start,Length) of the formatted placeholders.
- `GetPlaceholderContents()` returns a `ReadOnlyMemory<char>[]` of the formatted contents.
- `Culture` is the `ExtendedCultureInfo` that has been used to format placeholders' content (and the target culture
  into which this string should ultimately be translated).

The `GetFormatString()` method computes the corresponding
[composite format string](https://learn.microsoft.com/en-us/dotnet/standard/base-types/composite-formatting):
positional placeholders {0}, {1} etc. for each placeholder.

The purpose of this format string is not to rewrite the message with other contents, it is to ease globalization
process by providing the message's format in order to translate it into different languages.

```csharp
var enUS = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "en-US" );
var frFR = NormalizedCultureInfo.EnsureNormalizedCultureInfo( "fr-FR" );

var d = new DateTime( 2023, 07, 27, 23, 59, 59, 999, DateTimeKind.Utc );
var value = 37.12;

var inAmerica = new FormattedString( enUS, $"Date: {d:F}, V: {value:C}" );
var inFrance = new FormattedString( frFR, $"Date: {d:F}, V: {value:C}" );

inAmerica.Text.ShouldBe( "Date: Thursday, July 27, 2023 11:59:59 PM, V: $37.12" );
inFrance.Text.ShouldBe( "Date: jeudi 27 juillet 2023 23:59:59, V: 37,12 €" );
inAmerica.GetFormatString().ShouldBe( "Date: {0}, V: {1}" );
inAmerica.GetFormatString().ShouldBe( inFrance.GetFormatString() );

inAmerica.GetPlaceholderContents().Select( a => a.ToString() )
        .ShouldBe( new[] { "Thursday, July 27, 2023 11:59:59 PM", "$37.12" } );

inFrance.GetPlaceholderContents().Select( a => a.ToString() )
        .ShouldBe( new[] { "jeudi 27 juillet 2023 23:59:59", "37,12 €" } );

inFrance.Culture.ShouldBeSameAs( frFR );
inAmerica.Culture.ShouldBeSameAs( enUS );
```

`FormattedString` supports both simple and versioned serialization. Its primary usage is
to support the [`CodeString`](../CodeString.cs) (and therefore the [`MCString`](../MCString.cs)) implementation.
