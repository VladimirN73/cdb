using System.Diagnostics;

namespace cdb.Common;

public static class StringExtensions
{
    [DebuggerStepThrough]
    public static bool IsNullOrEmpty(this string value)
    {
        return string.IsNullOrEmpty(value);
    }
}