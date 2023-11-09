using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web;
using Soenneker.Extensions.Enumerable;
using Soenneker.Extensions.NameValueCollection;
using Soenneker.Extensions.String;

namespace Soenneker.Utils.String;

/// <summary>
/// A utility library for useful String operations
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class StringUtil
{
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
}