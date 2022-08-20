using System.Linq;

namespace cdb.Module.Console;

public interface ICmdParameterParser
{
    bool TryGetParameterValue(string[] parameterArray, string key, out string parameterValue);
    bool TryParseParameter(string parameter, string key, out string parameterValue);
    bool HasOption(string[] parameterArray, string key);
}

//TODO ADD UNIT TESTS FOR ALL FUNC
public class CmdParameterParser : ICmdParameterParser
{
    public bool TryGetParameterValue(string[] parameterArray, string key, out string parameterValue)
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

    public bool TryParseParameter(string parameter, string key, out string parameterValue)
    {
        var parameterTmp = parameter.ToLower();
        key = key.ToLower() + "=";
        parameterValue = null;
        if (!parameterTmp.StartsWith(key))
        {
            return false;
        }
        var idx = key.Length;
        parameterValue = parameter.Substring(idx, parameter.Length - idx);
        return true;
    }

    public bool HasOption(string[] parameterArray, string key)
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