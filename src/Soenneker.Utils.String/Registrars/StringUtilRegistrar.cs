using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Utils.String.Abstract;

namespace Soenneker.Utils.String.Registrars;

/// <summary>
/// A utility library for useful String operations
/// </summary>
public static class StringUtilRegistrar
{
    /// <summary>
    /// Adds <see cref="IStringUtil"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddStringUtilAsScoped(this IServiceCollection services)
    {
        services.TryAddScoped<IStringUtil, StringUtil>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IStringUtil"/> as a singleton service.<para/>
    /// </summary>
    public static IServiceCollection AddStringUtilAsSingleton(this IServiceCollection services)
    {
        services.TryAddSingleton<IStringUtil, StringUtil>();

        return services;
    }
}