using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace cdb.Common.IntegrationTests
{
    public abstract class TestBase
    {
        private Stopwatch _stopWatch;

        protected void StartTest()
        {
            WriteInfo($@"Start execution at {DateTime.Now.ToLocalTime()}");
            _stopWatch = Stopwatch.StartNew();
            SetWorkingDirectory();
        }

        [SuppressMessage("ReSharper", "UnusedVariable")]
        protected void SetWorkingDirectory()
        {
            var str1 = Directory.GetCurrentDirectory();
            var str2 = AppDomain.CurrentDomain.BaseDirectory;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var currentDirectory = Directory.GetCurrentDirectory();

            WriteInfo($@"Current directory='{currentDirectory}'");
        }

        protected void WriteInfo(string str)
        {
            Console.WriteLine(str);
        }

        protected void WriteElapsedTime()
        {
            WriteInfo($@"Elapsed Time : {_stopWatch.ElapsedMilliseconds / 1000} second(s)");
        }

        protected void PrintList(List<string> list)
        {
            foreach (var item in list)
            {
                WriteInfo(item);
            }
        }
    }
}
