using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Text;
using System.Text.Json;
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
    private readonly Lazy<ReflectionCache> _reflectionCache;

    public StringUtil()
    {
        _reflectionCache = new Lazy<ReflectionCache>(() =>
            new ReflectionCache(new Reflection.Cache.Options.ReflectionCacheOptions {PropertyFlags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance}));
    }

    /// <summary>
    /// For combining any number of strings with a ':' character between them.  Will filter out null or empty strings.
    /// </summary>
    /// <returns>An empty string if all of the keys are null or empty.</returns>
    [Pure]
    public static string ToCombinedId(params string?[] keys)
    {
        if (keys.IsNullOrEmpty())
            return "";

        // First pass: exact sizing
        var validCount = 0;
        var totalChars = 0;

        for (var i = 0; i < keys.Length; i++)
        {
            string? key = keys[i];

            if (key.HasContent())
            {
                validCount++;
                totalChars += key.Length;
            }
        }

        if (validCount == 0)
            return "";

        int capacity = totalChars + (validCount - 1); // ':' separators

        using var sb = new PooledStringBuilder(capacity);

        for (var i = 0; i < keys.Length; i++)
        {
            string? key = keys[i];

            if (key.HasContent())
            {
                sb.AppendSeparatorIfNotEmpty(':');
                sb.Append(key);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Retrieves the value of a query parameter from the specified URL.
    /// </summary>
    /// <param name="url">The URL to extract the query parameter from.</param>
    /// <param name="name">The name of the query parameter.</param>
    /// <returns>The value of the query parameter, or null if the URL is null or empty, or if the query parameter does not exist.</returns>
    [Pure]
    public static string? GetQueryParameter(string? url, string? name)
    {
        if (url.IsNullOrEmpty() || name.IsNullOrEmpty())
            return null;

        int queryStartIndex = url.IndexOf('?');
        if (queryStartIndex == -1 || queryStartIndex == url.Length - 1)
            return null;

        ReadOnlySpan<char> querySpan = url.AsSpan(queryStartIndex + 1);
        ReadOnlySpan<char> nameSpan = name.AsSpan();

        while (!querySpan.IsEmpty)
        {
            int equalsIndex = querySpan.IndexOf('=');
            if (equalsIndex is -1)
                break;

            ReadOnlySpan<char> key = querySpan.Slice(0, equalsIndex);
            int ampersandIndex = querySpan.Slice(equalsIndex + 1).IndexOf('&');

            ReadOnlySpan<char> value;
            if (ampersandIndex is -1)
            {
                value = querySpan.Slice(equalsIndex + 1);
                querySpan = ReadOnlySpan<char>.Empty;
            }
            else
            {
                value = querySpan.Slice(equalsIndex + 1, ampersandIndex);
                querySpan = querySpan.Slice(equalsIndex + 1 + ampersandIndex + 1);
            }

            if (key.SequenceEqual(nameSpan))
            {
                return Uri.UnescapeDataString(value.ToString());
            }
        }

        return null;
    }

    /// <summary>
    /// Retrieves the query parameters from the specified URL.
    /// </summary>
    /// <param name="url">The URL to extract the query parameters from.</param>
    /// <returns>A dictionary containing the query parameters as key-value pairs, or null if the URL is null or empty, or if the URL does not contain any query parameters.</returns>
    [Pure]
    public static Dictionary<string, string>? GetQueryParameters(string? url)
    {
        if (url.IsNullOrEmpty())
            return null;

        // Attempt to parse the URL only once
        Uri? uri;
        try
        {
            uri = new Uri(url);
        }
        catch (UriFormatException)
        {
            return null;
        }

        // Early exit if there's no query string
        string query = uri.Query;
        if (query.IsNullOrEmpty() || query.Length <= 1) // Query is either empty or only "?"
            return null;

        // Remove the leading '?' from the query string
        ReadOnlySpan<char> querySpan = query.AsSpan(1);

        // Dictionary allocation with an estimated initial capacity for minimal resizing
        var result = new Dictionary<string, string>(4, StringComparer.Ordinal);

        // Parse the query string manually for optimal performance
        var start = 0;

        while (start < querySpan.Length)
        {
            int equalsIndex = querySpan.Slice(start).IndexOf('=');
            if (equalsIndex is -1)
                break;

            int ampIndex = querySpan.Slice(start + equalsIndex + 1).IndexOf('&');

            if (ampIndex is -1)
                ampIndex = querySpan.Length - start - equalsIndex - 1;

            ReadOnlySpan<char> key = querySpan.Slice(start, equalsIndex);
            ReadOnlySpan<char> value = querySpan.Slice(start + equalsIndex + 1, ampIndex);

            string decodedKey = Uri.UnescapeDataString(key.ToString());
            string decodedValue = Uri.UnescapeDataString(value.ToString());

            result.TryAdd(decodedKey, decodedValue);

            start += equalsIndex + ampIndex + 2; // Advance past "key=value&"
        }

        return result.Count > 0 ? result : null;
    }

    public static T? ParseQueryStringUsingJson<T>(string queryString, ILogger? logger = null) where T : new()
    {
        try
        {
            NameValueCollection queryParameters = HttpUtility.ParseQueryString(queryString);
            Dictionary<string, string> dict = queryParameters.ToDictionary();

            // Convert the dictionary to a JSON string
            string? json = JsonUtil.Serialize(dict);

            if (json is null)
            {
                logger?.LogError("Error serializing query parameters");
                return default;
            }

            // Deserialize the JSON string to the specified type
            var obj = JsonUtil.Deserialize<T>(json);

            return obj;
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

        Type type = typeof(T);

        var model = new T();

        CachedType cachedTyped = _reflectionCache.Value.GetCachedType(type);

        foreach (string key in queryParameters.Keys)
        {
            PropertyInfo? property = cachedTyped.GetProperty(key);

            if (property == null || !property.CanWrite)
                continue;

            Type propertyType = property.PropertyType;
            string? value = queryParameters[key];

            if (value == null)
                continue;

            object convertedValue = Convert.ChangeType(value, propertyType);
            property.SetValue(model, convertedValue);
        }

        return model;
    }

    public string? GetDomainFromEmail(string address)
    {
        if (address.IsNullOrEmpty())
            return null;

        int lastIndex = address.LastIndexOf('@');

        // Ensure '@' exists and is in a valid position
        if (lastIndex > 0 && lastIndex < address.Length - 1)
        {
            // Extract domain without allocation using string slicing
            return address.AsSpan(lastIndex + 1).ToString();
        }

        // Return null for invalid email formats
        return null;
    }

    /// <summary>
    /// Extracts URLs from the given value.
    /// </summary>
    /// <param name="value">The value to extract URLs from.</param>
    /// <returns>A list of URLs extracted from the value, an empty list if there are no matches or if the value is null or empty</returns>
    [Pure]
    public static List<string>? ExtractUrls(string? value)
    {
        if (value.IsNullOrWhiteSpace())
            return null;

        // Cache the regex instance for reuse, as RegexOptions.Compiled is expensive to recreate.
        Regex regex = RegexCollection.RegexCollection.Url();

        List<string>? urls = null;

        foreach (Match match in regex.Matches(value))
        {
            // Lazy initialization of the list to reduce allocations for inputs with no matches.
            urls ??= new List<string>(4);
            urls.Add(match.Value);
        }

        return urls;
    }

    /// <summary>
    /// Similar to logging strings:
    /// <code>logger.log("{variable} is a prime number", 2);</code> "2 is a prime number" 
    /// </summary>
    /// <param name="str">The string to build from the template.</param>
    /// <param name="values">The values to inject into the template.</param>
    /// <returns>If the number of parameters does not match the number of values coming in, it will return the braced variable in the string</returns>
    [Pure]
    [return: NotNullIfNotNull("str")]
    public static string? BuildStringFromTemplate(string? str, params object?[]? values)
    {
        if (str == null)
            return null;

        if (values.IsNullOrEmpty())
            return str;

        try
        {
            var i = 0;
            while (i < values.Length)
            {
                var injectionValue = values[i++]?.ToString();

                if (injectionValue == null)
                    continue;

                int start = str.IndexOf('{');
                int end = str.IndexOf('}');
                if (start >= 0 && end > start)
                {
                    str = str.Remove(start, end - start + 1).Insert(start, injectionValue);
                }
                else
                {
                    break;
                }
            }

            return str;
        }
        catch
        {
            return str;
        }
    }

    /// <summary>
    /// Decodes a Base64-encoded JSON string into a strongly typed object.
    /// </summary>
    /// <typeparam name="T">
    /// The type of object to deserialize into.
    /// </typeparam>
    /// <param name="base64">
    /// The Base64-encoded string containing JSON data.
    /// </param>
    /// <returns>
    /// The deserialized object of type <typeparamref name="T"/>.
    /// Returns <c>null</c> if the JSON represents a null value.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="base64"/> is null, empty, or consists only of white-space characters.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown if the Base64 string is not in a valid format.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown if the JSON is invalid or cannot be deserialized to the target type.
    /// </exception>
    public static T? ConvertBase64JsonToObject<T>(string base64)
    {
        if (base64.IsNullOrWhiteSpace())
            throw new ArgumentException("Base64 string is null or empty.", nameof(base64));

        byte[] bytes = base64.ToBytesFromBase64();
        return JsonUtil.Deserialize<T>(bytes);
    }
}