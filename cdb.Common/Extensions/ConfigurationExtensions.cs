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

        public static T ToEnum<T>(this string value, T defaultValue) //where T : struct
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            var valueX = value.Replace(" ", ""); // remove all empty spaces

            Enum.TryParse(typeof(T), valueX, true, out object retTemp);
            
            var ret = defaultValue;

            if (retTemp != null)
            {
                ret = (T)retTemp;
            }

            return ret;
        }

    }
}
