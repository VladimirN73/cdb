using Microsoft.Extensions.Configuration;

namespace cdb.ConsoleApp
{
    public class AppConfigService
    {
        private readonly IConfiguration _config;

        public AppConfigService(IConfiguration config)
        {
            _config = config;
        }

        public string AppVersion =>  _config["version"] ?? "n/a";

    }
}
