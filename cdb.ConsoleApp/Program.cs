using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace cdb.ConsoleApp
{
    // Info:
    // https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection


    public static class Program
    {
        public static void Main(string[] args)
        {
            using var host = CreateHostBuilder(System.Array.Empty<string>()).Build();
            var app = host.Services.GetRequiredService<ConsoleApp>();
            app.Run(args);
        }

        static IHostBuilder CreateHostBuilder(string[] args) => Host
            .CreateDefaultBuilder(args)
            .ConfigureLogging(AppConfigureLogging)
            .ConfigureServices(AppConfigureServices)
        ;

        private static void AppConfigureLogging(ILoggingBuilder logging)
        {
            logging.ClearProviders();
            logging.AddConsole();
        }

        private static void AppConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ConsoleApp>();
            services.AddSingleton<AppConfigService>();
            services.AddSingleton<ILogger, Logger<ConsoleApp>>();
            
            Common.ModuleConfiguration.ConfigureServices(services);
            Module.Console.ModuleConfiguration.ConfigureServices(services);
        }
    }
}
