using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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