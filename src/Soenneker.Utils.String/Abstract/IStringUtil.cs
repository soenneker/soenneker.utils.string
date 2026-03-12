using System;
using System.Diagnostics.Contracts;

namespace Soenneker.Utils.String.Abstract;

/// <summary>
/// A utility library for useful String operations
/// </summary>
public interface IStringUtil
{
    /// <summary>
    /// Retrieves the domain from an email address.
    /// </summary>
    /// <param name="address">The email address.</param>
    /// <returns>The domain of the email address, or null if the email address is invalid.</returns>
    [Pure]
    string? GetDomainFromEmail(string address);

    /// <summary>
    /// Parses a query string into an instance of the specified model type.
    /// </summary>
    /// <remarks>Uses caching on the properties of the type for speed.</remarks>
    /// <typeparam name="T">The type of the model to populate with the query string parameters.</typeparam>
    /// <param name="queryString">The query string to parse.</param>
    /// <returns>An instance of the specified model type populated with the query string parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the query string is null.</exception>
    /// <exception cref="InvalidCastException">Thrown when a query string parameter cannot be converted to the corresponding property type.</exception>
    [Pure]
    T ParseQueryString<T>(string queryString) where T : new();
}