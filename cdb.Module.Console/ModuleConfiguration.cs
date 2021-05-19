using Microsoft.Extensions.DependencyInjection;

namespace cdb.Module.Console
{
    public static class ModuleConfiguration
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ICmdParameterParser, CmdParameterParser>();
        }
    }
}
