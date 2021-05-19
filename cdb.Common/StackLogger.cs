using System;
using System.Diagnostics;

namespace cdb.Common
{
    public class StackLogger : IDisposable
    {
        private readonly string _strMethod;
        private readonly bool _writeLog;

        public StackLogger(string strMethod, bool writeLog = true)
        {
            _strMethod = strMethod;
            _writeLog = writeLog;

            if (_writeLog) AddLog($@"--> {_strMethod}");
        }

        public void Dispose()
        {
            if (_writeLog) AddLog($@"<-- {_strMethod}");
        }

        private void AddLog(string log)
        {
            Console.Out.WriteLine(log);
            Trace.TraceInformation(log);
        }
    }
}
