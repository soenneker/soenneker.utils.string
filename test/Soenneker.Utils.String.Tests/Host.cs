using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Soenneker.TestHosts.Unit;
using Soenneker.Utils.String.Registrars;
using Soenneker.Utils.Test;

namespace Soenneker.Utils.String.Tests;

public class Host : UnitTestHost
{
    public override Task InitializeAsync()
    {
        SetupIoC(Services);

        return base.InitializeAsync();
    }

    private static void SetupIoC(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: false);
        });

        IConfiguration config = TestUtil.BuildConfig();
        services.AddSingleton(config);

        services.AddStringUtilAsScoped();
    }
}