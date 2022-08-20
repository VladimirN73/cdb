using Microsoft.Extensions.DependencyInjection;


namespace cdb.Common;

public static class ModuleConfiguration
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<ICloneProcessor, CloneProcessor>();
        services.AddSingleton<IAppLogger, AppLogger>();
    }
}