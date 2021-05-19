using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace cdb.Common.Extensions
{
    public static class ConfigurationExtensions
    {
        public static string GetConnectionStringByKey(this IConfiguration config, string strKey)
        {
            if (strKey.IsNullOrEmpty()) return null;

            var list = config
                .GetSection("ConnectionStrings")
                .GetChildren()
                .ToList();

            var item = list
                .FirstOrDefault(x => string.Equals(x.Key.Trim(), strKey.Trim(), StringComparison.CurrentCultureIgnoreCase));

            var ret = item?.Value;

            return ret;
        }

    }
}
