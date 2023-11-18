using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.Enumerable;
using Soenneker.Extensions.NameValueCollection;
using Soenneker.Extensions.String;
using Soenneker.Utils.String.Abstract;

namespace Soenneker.Utils.String;

///<inheritdoc cref="IStringUtil"/>
public class StringUtil : IStringUtil
{
    private readonly ILogger<StringUtil> _logger;

    public StringUtil(ILogger<StringUtil> logger)
    {
        _logger = logger;
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

    public string? GetDomainFromEmail(string address)
    {
        try
        {
            // Find the position of the last '@' symbol
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
    /// Similar to logging strings:
    /// <code>logger.log("{variable} is a prime number", 2);</code> "2 is a prime number" 
    /// </summary>
    /// <returns>If the number of parameters does not match the number of values coming in, it will return the braced variable in the string</returns>
    /// <param name="str"></param>
    /// <param name="values"></param>
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