[![](https://img.shields.io/nuget/v/Soenneker.Utils.String.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.String/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.string/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.string/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.String.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.String/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.string/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.string/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.String
String helpers for query strings, lightweight model binding, URL extraction, templates, compound identifiers, and Base64-encoded JSON.

## Installation

```bash
dotnet add package Soenneker.Utils.String
```

Most helpers are static and can be called directly:

```csharp
using Soenneker.Utils.String;

string id = StringUtil.ToCombinedId("customer", "42");
string? returnUrl = StringUtil.GetQueryParameter(
    "https://example.test/callback?returnUrl=%2Faccount%3Ftab%3Dsecurity",
    "returnUrl");
```

`ToCombinedId` removes null and empty parts and joins the rest with `:`. It does not escape delimiters, so do not use it where parts containing `:` must remain distinguishable.

## Query strings

`GetQueryParameter` returns the first matching value from any string containing a `?`. Names and values use form-style decoding, where percent escapes are decoded and `+` becomes a space. A key without `=` has an empty value, matching is case-sensitive, and URL fragments are excluded.

`GetQueryParameters` accepts an absolute URI and returns an ordinal, case-sensitive dictionary. The first occurrence of a duplicate decoded key wins. It returns `null` when the input is not an absolute URI or has no query parameters.

```csharp
Dictionary<string, string>? values = StringUtil.GetQueryParameters(
    "https://example.test/search?q=red+shoes&page=2");

// values["q"] == "red shoes"
```

## Binding query strings

Register `IStringUtil` when you need the reflection-based binder or email-domain extraction through dependency injection:

```csharp
using Soenneker.Utils.String.Abstract;
using Soenneker.Utils.String.Registrars;

services.AddStringUtilAsSingleton();

public sealed class SearchService(IStringUtil strings)
{
    public SearchRequest Parse(string query) => strings.ParseQueryString<SearchRequest>(query);
}
```

`ParseQueryString<T>` creates `T`, matches query keys to public writable properties without regard to case, and converts values with `Convert.ChangeType`. Unknown keys are ignored. It is intended for simple scalar properties; failed conversions throw, and nullable, collection, or complex properties require a different binding strategy.

`ParseQueryStringUsingJson<T>` is the tolerant static alternative. It builds a string-valued dictionary and deserializes it through `Soenneker.Utils.Json`; conversion failures return `null` and are written to the optional logger.

## Other helpers

- `GetDomainFromEmail` returns the text after the last `@` when both sides are non-empty. It does not validate an email address.
- `ExtractUrls` finds URL-shaped substrings. Treat the results as candidates, not validated or trusted destinations.
- `BuildStringFromTemplate` replaces brace-delimited placeholders sequentially with non-null values. Placeholder names are informational; unmatched placeholders remain in the result, and braces are not escaped.
- `ConvertBase64JsonToObject<T>` decodes standard Base64 containing UTF-8 JSON. Invalid Base64 or JSON throws. It does not accept the Base64Url alphabet.

`ConvertBase64JsonToObject<T>` clears the portion of its pooled decode buffer before returning it, but decoded objects and input strings remain managed memory. Do not treat it as a complete secret-erasure mechanism.
