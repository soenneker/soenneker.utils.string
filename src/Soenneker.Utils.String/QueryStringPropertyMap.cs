using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Soenneker.Utils.String;

internal static class QueryStringPropertyMap<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    internal static readonly Dictionary<string, PropertyInfo> Value = Create();

    private static Dictionary<string, PropertyInfo> Create()
    {
        var result = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
        foreach (PropertyInfo property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            if (property.GetIndexParameters().Length == 0)
                result[property.Name] = property;
        return result;
    }
}
