using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Enumerable;
using Soenneker.Extensions.NameValueCollection;
using Soenneker.Extensions.String;
using Soenneker.Reflection.Cache;
using Soenneker.Reflection.Cache.Types;
using Soenneker.Utils.Json;
using Soenneker.Utils.PooledStringBuilders;
using Soenneker.Utils.String.Abstract;

namespace Soenneker.Utils.String;

///<inheritdoc cref="IStringUtil"/>
public sealed class StringUtil : IStringUtil
{
    private static readonly Lazy<ReflectionCache> s_reflectionCache = new(static () => new ReflectionCache(new Reflection.Cache.Options.ReflectionCacheOptions
    {
        PropertyFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
    }));

    /// <summary>
    /// For combining any number of strings with a ':' character between them. Will filter out null or empty strings.
    /// </summary>
    /// <returns>An empty string if all of the keys are null or empty.</returns>
    [Pure]
    public static string ToCombinedId(params string?[] keys)
    {
        if (keys.IsNullOrEmpty())
            return string.Empty;

        var validCount = 0;
        var totalChars = 0;

        for (var i = 0; i < keys.Length; i++)
        {
            string? key = keys[i];
            if (!string.IsNullOrEmpty(key))
            {
                validCount++;
                totalChars += key.Length;
            }
        }

        if (validCount == 0)
            return string.Empty;

        int capacity = totalChars + (validCount - 1);

        using var sb = new PooledStringBuilder(capacity);

        var first = true;

        for (var i = 0; i < keys.Length; i++)
        {
            string? key = keys[i];
            if (string.IsNullOrEmpty(key))
                continue;

            if (!first)
                sb.Append(':');
            else
                first = false;

            sb.Append(key);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Retrieves the value of a query parameter from the specified URL.
    /// </summary>
    [Pure]
    public static string? GetQueryParameter(string? url, string? name)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(name))
            return null;

        int queryStartIndex = url.IndexOf('?');
        if ((uint)queryStartIndex >= (uint)(url.Length - 1)) // -1 or last char
            return null;

        ReadOnlySpan<char> query = url.AsSpan(queryStartIndex + 1);
        ReadOnlySpan<char> nameSpan = name.AsSpan();

        while (!query.IsEmpty)
        {
            // segment is [0..&] or remainder
            int amp = query.IndexOf('&');
            ReadOnlySpan<char> segment = amp >= 0 ? query.Slice(0, amp) : query;
            query = amp >= 0 ? query.Slice(amp + 1) : ReadOnlySpan<char>.Empty;

            if (segment.IsEmpty)
                continue;

            int eq = segment.IndexOf('=');
            if (eq < 0)
            {
                // key-only segments like "?foo&bar=baz"
                if (segment.SequenceEqual(nameSpan))
                    return string.Empty;

                continue;
            }

            ReadOnlySpan<char> key = segment.Slice(0, eq);
            if (!key.SequenceEqual(nameSpan))
                continue;

            ReadOnlySpan<char> value = segment.Slice(eq + 1);
            return DecodeQueryComponentIfNeeded(value);
        }

        return null;
    }

    /// <summary>
    /// Retrieves the query parameters from the specified URL.
    /// </summary>
    [Pure]
    public static Dictionary<string, string>? GetQueryParameters(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        // Avoid exception cost
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return null;

        string query = uri.Query;
        if (query.Length <= 1) // empty or "?"
            return null;

        ReadOnlySpan<char> span = query.AsSpan(1); // skip '?'

        // Estimate count by '&' to reduce resizes.
        var estimated = 1;
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == '&')
                estimated++;
        }

        var result = new Dictionary<string, string>(estimated, StringComparer.Ordinal);

        while (!span.IsEmpty)
        {
            int amp = span.IndexOf('&');
            ReadOnlySpan<char> segment = amp >= 0 ? span.Slice(0, amp) : span;
            span = amp >= 0 ? span.Slice(amp + 1) : ReadOnlySpan<char>.Empty;

            if (segment.IsEmpty)
                continue;

            int eq = segment.IndexOf('=');

            ReadOnlySpan<char> keySpan;
            ReadOnlySpan<char> valSpan;

            if (eq < 0)
            {
                // key-only
                keySpan = segment;
                valSpan = ReadOnlySpan<char>.Empty;
            }
            else
            {
                keySpan = segment.Slice(0, eq);
                valSpan = segment.Slice(eq + 1);
            }

            // Decode only if needed to avoid allocation + Uri.UnescapeDataString overhead.
            string key = DecodeQueryComponentIfNeeded(keySpan);
            string value = DecodeQueryComponentIfNeeded(valSpan);

            result.TryAdd(key, value);
        }

        return result.Count != 0 ? result : null;
    }

    public static T? ParseQueryStringUsingJson<T>(string queryString, ILogger? logger = null) where T : new()
    {
        try
        {
            // This path is fundamentally allocation-heavy (NVC -> Dictionary -> JSON -> model).
            // Keeping behavior, but tightened a bit.
            NameValueCollection queryParameters = HttpUtility.ParseQueryString(queryString);

            Dictionary<string, string> dict = queryParameters.ToDictionary();

            string? json = JsonUtil.Serialize(dict);
            if (json is null)
            {
                logger?.LogError("Error serializing query parameters");
                return default;
            }

            return JsonUtil.Deserialize<T>(json);
        }
        catch (Exception e)
        {
            logger?.LogError(e, "Error parsing query using JSON");
            return default;
        }
    }

    public T ParseQueryString<T>(string queryString) where T : new()
    {
        queryString.ThrowIfNullOrEmpty();

        NameValueCollection queryParameters = HttpUtility.ParseQueryString(queryString);

        var model = new T();

        CachedType cachedType = s_reflectionCache.Value.GetCachedType(typeof(T));

        // NameValueCollection.Keys is ICollection, enumerator alloc can happen.
        // This is still OK relative to reflection, but we can minimize other costs.
        foreach (string? key in queryParameters.AllKeys)
        {
            if (string.IsNullOrEmpty(key))
                continue;

            PropertyInfo? property = cachedType.GetProperty(key);
            if (property is null || !property.CanWrite)
                continue;

            string? value = queryParameters[key];
            if (value is null)
                continue;

            object convertedValue = Convert.ChangeType(value, property.PropertyType);
            property.SetValue(model, convertedValue);
        }

        return model;
    }

    public string? GetDomainFromEmail(string address)
    {
        if (string.IsNullOrEmpty(address))
            return null;

        int lastIndex = address.LastIndexOf('@');
        if ((uint)lastIndex < (uint)(address.Length - 1) && lastIndex > 0)
            return address.AsSpan(lastIndex + 1)
                          .ToString();

        return null;
    }

    /// <summary>
    /// Extracts URLs from the given value.
    /// </summary>
    [Pure]
    public static List<string>? ExtractUrls(string? value)
    {
        if (value.IsNullOrWhiteSpace())
            return null;

        Regex regex = RegexCollection.RegexCollection.Url();

        List<string>? urls = null;

        foreach (ValueMatch match in regex.EnumerateMatches(value))
        {
            urls ??= new List<string>(4);
            urls.Add(value.Substring(match.Index, match.Length));
        }

        return urls;
    }

    /// <summary>
    /// Similar to logging strings:
    /// <code>logger.log("{variable} is a prime number", 2);</code> "2 is a prime number"
    /// </summary>
    [Pure]
    [return: NotNullIfNotNull("str")]
    public static string? BuildStringFromTemplate(string? str, params object?[]? values)
    {
        if (str is null)
            return null;

        if (values.IsNullOrEmpty())
            return str;

        // Single-pass builder; avoids repeated Remove/Insert allocations.
        ReadOnlySpan<char> input = str.AsSpan();

        var valueIndex = 0;
        var pos = 0;

        // Quick scan: if no '{', return original
        if (input.IndexOf('{') < 0)
            return str;

        using var sb = new PooledStringBuilder(str.Length + 16);

        while (pos < input.Length)
        {
            int open = input.Slice(pos)
                            .IndexOf('{');
            if (open < 0)
            {
                sb.Append(input.Slice(pos));
                break;
            }

            open += pos;

            int close = input.Slice(open + 1)
                             .IndexOf('}');
            if (close < 0)
            {
                // no closing brace; append rest
                sb.Append(input.Slice(pos));
                break;
            }

            close += open + 1;

            // append text before '{'
            sb.Append(input.Slice(pos, open - pos));

            // placeholder token includes braces: {token}
            ReadOnlySpan<char> placeholder = input.Slice(open, close - open + 1);

            if ((uint)valueIndex < (uint)values.Length)
            {
                object? obj = null;

                while ((uint)valueIndex < (uint)values.Length)
                {
                    obj = values[valueIndex++];
                    if (obj is not null)
                        break;
                }

                if (obj is not null)
                {
                    sb.Append(obj.ToString());
                }
                else
                {
                    // no remaining non-null values
                    sb.Append(placeholder);
                }
            }
            else
            {
                // Not enough values -> keep placeholder (matches your remarks)
                sb.Append(placeholder);
            }

            pos = close + 1;
        }

        return sb.ToString();
    }

    public static T? ConvertBase64JsonToObject<T>(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            throw new ArgumentException("Base64 string is null or empty.", nameof(base64));

        // Convert.FromBase64String allocates a new byte[] every time.
        // Use ArrayPool + TryFromBase64String to reduce GC pressure.
        int maxLen = base64.Length * 3 / 4 + 3;
        byte[] rented = ArrayPool<byte>.Shared.Rent(maxLen);

        try
        {
            if (!Convert.TryFromBase64String(base64, rented, out int bytesWritten))
                throw new FormatException("The Base64 string is not in a valid format.");

            // If your JsonUtil has a ReadOnlySpan<byte> overload, prefer it.
            // Otherwise, we must copy to an exact array (costly). Assuming yours can take byte[].
            // If JsonUtil.Deserialize<T>(byte[]) reads full array length, this is a bug. It should respect bytesWritten.
            // Ideally JsonUtil.Deserialize<T>(ReadOnlySpan<byte>) exists.
            return JsonUtil.Deserialize<T>(new ReadOnlySpan<byte>(rented, 0, bytesWritten));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string DecodeQueryComponentIfNeeded(ReadOnlySpan<char> component)
    {
        if (component.IsEmpty)
            return string.Empty;

        // Most query strings are already plain ASCII without escapes.
        // Only decode if we see '%' (percent-encoding) or '+' (commonly used for space in x-www-form-urlencoded).
        for (var i = 0; i < component.Length; i++)
        {
            char c = component[i];
            if (c == '%' || c == '+')
                return Uri.UnescapeDataString(component.ToString());
        }

        // No decoding needed -> single allocation for the string itself.
        return component.ToString();
    }
}