[![](https://img.shields.io/nuget/v/Soenneker.Utils.String.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.String/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.string/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.string/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.String.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.String/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.string/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.string/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.String
A utility library for useful String operations.

## Installation

```bash
dotnet add package Soenneker.Utils.String
```

## Quick start

```csharp
using Soenneker.Utils.String.Registrars;

services.AddStringUtilAsSingleton();
```

Then inject `IStringUtil` wherever you need it.

## Common operations

- `ToCombinedId()` - For combining any number of strings with a ':' character between them. Will filter out null or empty strings.
- `GetQueryParameter()` - Retrieves the value of a query parameter from the specified URL.
- `GetQueryParameters()` - Retrieves the query parameters from the specified URL.
- `ParseQueryStringUsingJson()` - Maps query-string keys and values to `T` through JSON; returns `null` and optionally logs when conversion fails.
- `ParseQueryString()` - Parses a query string into an instance of the specified model type. Returns an instance of the specified model type populated with the query string parameters.
- `GetDomainFromEmail()` - Retrieves the domain from an email address. Returns the domain of the email address, or null if the email address is invalid.
- `ExtractUrls()` - Extracts URLs from the given value.
- `BuildStringFromTemplate()` - Similar to logging strings: `logger.log("{variable} is a prime number", 2);` "2 is a prime number".
- `ConvertBase64JsonToObject()` - Decodes Base64 JSON into `T`; malformed Base64 or JSON throws, and pooled temporary storage is returned after use.
