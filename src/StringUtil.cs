﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
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
using Soenneker.Utils.String.Abstract;

namespace Soenneker.Utils.String;

///<inheritdoc cref="IStringUtil"/>
public class StringUtil : IStringUtil
{
    private readonly ILogger<StringUtil> _logger;
    private readonly Lazy<ReflectionCache> _reflectionCache;

    public StringUtil(ILogger<StringUtil> logger)
    {
        _logger = logger;
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

        List<string?> filtered = keys.Where(c => !c.IsNullOrEmpty()).ToList();

        if (filtered.Empty())
            return "";

        if (filtered.Count == 1)
            return filtered[0]!;

        return string.Join(':', filtered);
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

        NameValueCollection queryParams;

        try
        {
            var uri = new Uri(url);

            if (uri.Query == "")
                return null;

            queryParams = HttpUtility.ParseQueryString(uri.Query);
        }
        catch
        {
            return null;
        }

        if (queryParams.Count == 0)
            return null;

        string? result = queryParams.Get(name);
        return result;
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

        NameValueCollection queryParams;

        try
        {
            var uri = new Uri(url);

            if (uri.Query == "")
                return null;

            queryParams = HttpUtility.ParseQueryString(uri.Query);
        }
        catch
        {
            return null;
        }

        if (queryParams.Count == 0)
            return null;

        Dictionary<string, string> result = queryParams.ToDictionary();
        return result;
    }

    public static T? ParseQueryStringUsingJson<T>(string queryString, ILogger? logger = null) where T : new()
    {
        try
        {
            NameValueCollection queryParameters = HttpUtility.ParseQueryString(queryString);
            Dictionary<string, string> dict = queryParameters.ToDictionary();

            // Convert the dictionary to a JSON string
            string? json = JsonUtil.Serialize(dict);

            if (json == null)
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
        try
        {
            int lastIndex = address.LastIndexOf('@');

            // Check if the '@' symbol is present and not the first or last character
            if (lastIndex != -1 && lastIndex > 0 && lastIndex < address.Length - 1)
            {
                // Split the string into two parts based on the last '@' symbol
                string domain = address[(lastIndex + 1)..];
                return domain;
            }

            return null;
        }
        catch
        {
            _logger.LogError("Unable to get domain from email ({address})", address);
            return null;
        }
    }

    /// <summary>
    /// Extracts URLs from the given value.
    /// </summary>
    /// <param name="value">The value to extract URLs from.</param>
    /// <returns>A list of URLs extracted from the value, an empty list if there are no matches or if the value is null or empty</returns>
    [Pure]
    public static List<string>? ExtractUrls(string? value)
    {
        var urls = new List<string>();

        if (value.IsNullOrWhiteSpace())
            return urls;

        MatchCollection matches = RegexCollection.RegexCollection.Url().Matches(value);

        foreach (Match match in matches)
        {
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
}