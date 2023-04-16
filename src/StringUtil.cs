using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Soenneker.Extensions.Enumerable;
using Soenneker.Extensions.String;

namespace Soenneker.Utils.String;

/// <summary>
/// A utility library for useful String operations
/// </summary>
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
}