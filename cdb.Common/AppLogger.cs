using System;

namespace cdb.Common;

public interface IAppLogger
{
    void Log(string str);
}

public class AppLogger : IAppLogger
{
    public void Log(string str)
    {
        Console.WriteLine(str);
    }
}