using System.Diagnostics.Contracts;

namespace Soenneker.Utils.String.Abstract;

/// <summary>
/// A utility library for useful String operations
/// </summary>
public interface IStringUtil
{
    [Pure]
    string? GetDomainFromEmail(string address);
}