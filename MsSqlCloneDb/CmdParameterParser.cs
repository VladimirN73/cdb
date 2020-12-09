using System.Linq;

namespace MsSqlCloneDb
{
    public static class CmdParameterParser
    {
        public static bool TryGetParameterValue(string[] parameterArray, string key, out string parameterValue)
        {
            parameterValue = null;

            if (!key.StartsWith("-"))
            {
                key = "-" + key;
            }

            foreach (var p in parameterArray)
            {
                if (TryParseParameter(p, key, out parameterValue))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool TryParseParameter(string parameter, string key, out string parameterValue)
        {
            var parameterTmp = parameter.ToLower();
            key = key.ToLower() + "=";
            parameterValue = null;
            if (!parameterTmp.StartsWith(key))
            {
                return false;
            }
            //var token = parameter.Split('=');  // we cannot use Split('='), because ConnectionString may contain '='
            //if (token.Length > 1)
            //{
            //    parameterValue = parameter.Substring(token[0].Length + 1);
            //    return true;
            //}

            var idx = key.Length;
            parameterValue = parameter.Substring(idx, parameter.Length - idx);
            return true;
        }

        public static bool HasOption(string[] parameterArray, string key)
        {
            if (!key.StartsWith("-"))
            {
                key = "-" + key;
            }
            key = key.ToLower().Trim();

            var ret = parameterArray.Any(x => x.ToLower().Trim().Equals(key));

            return ret;
        }
    }
}