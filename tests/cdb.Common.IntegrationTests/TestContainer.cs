using System;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace cdb.Common.IntegrationTests
{
    public class TestContainer
    {
        private readonly ServiceProvider _diContainer;
        private readonly IConfigurationRoot _configuration;

        public TestContainer()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();

            AddServices(services);

            _diContainer = services.BuildServiceProvider();
        }

        private void AddServices(ServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(_configuration);

            Common.ModuleConfiguration.ConfigureServices(services);
            Module.Console.ModuleConfiguration.ConfigureServices(services);

        }

        public TObject GetService<TObject>()
        {
            return Resolve<TObject>();
        }

        public TObject Resolve<TObject>()
        {
            return _diContainer.GetService<TObject>();
        }
    }

    public class AppLoggerTest : IAppLogger
    {
        private readonly StringBuilder sb = new StringBuilder();

        public void Log(string str)
        {
            sb.AppendLine(str);
            Console.WriteLine(str);
        }

        public string LogText => sb.ToString();

        public void Clear()
        {
            sb.Clear();
        }
    }


}
