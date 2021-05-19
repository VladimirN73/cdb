using System;
using System.IO;
using System.Text;
using cdb.Common;
using cdb.Module.Console;
using Microsoft.Extensions.Configuration;

namespace cdb.ConsoleApp
{
    public class ConsoleApp
    {
        private readonly AppConfigService _appConfigService;
        private readonly ICmdParameterParser _cmdParser;
        private readonly IAppLogger _logger;
        private readonly ICloneProcessor _cloneProcessor;
        private readonly IConfiguration _config;

        public ConsoleApp(AppConfigService appConfigService, ICmdParameterParser cmdParser, IAppLogger logger, ICloneProcessor cloneProcessor, IConfiguration config)
        {
            _appConfigService = appConfigService;
            _cmdParser = cmdParser;
            _logger = logger;
            _cloneProcessor = cloneProcessor;
            _config = config;
        }

        public void Run(string[] args)
        {
            try
            {
                RunInternal(args);
            }
            catch (Exception ex)
            {
                var errorLogBuilder = new StringBuilder();
                errorLogBuilder.AppendLine("=========================================================");
                errorLogBuilder.AppendLine($"Error: '{ex}'");
                errorLogBuilder.AppendLine("=========================================================");

                var errorLog = errorLogBuilder.ToString();

                Log(errorLog);

                Environment.Exit(1);
            }
            finally
            {
                Log("Finish");
                Console.Out.Close();
            }
        }

        private void RunInternal(string[] args)
        {
            Console.WriteLine($"Clone Tool v. {_appConfigService.AppVersion}");

            var showHelp = _cmdParser.HasOption(args, "help");

            if (showHelp)
            {
                ShowHelp();
                return;
            }

            Log(@"Detected (but not loaded yet) parameters in commandline");
            var str = string.Join("\n", args);
            Log(str);

            var myParams = CloneParametersExt.GetParameters(args, _cmdParser);

            Log("\nLoaded (accepted) parameters from commandline");
            CloneParametersExt.PrintParameters(myParams, _logger);

            // Merge with App.Config
            myParams = CloneParametersExt.AdaptParameters(myParams, _config);
            Log("\nWorking (adapted) parameters");
            CloneParametersExt.PrintParameters(myParams, _logger);



            var retSuccess = Doit(myParams);
            if (!retSuccess)
            {
                throw new Exception("Failed. See Log for more Info.");
            }
        }

        private bool Doit(CloneParametersExt config)
        {
            _cloneProcessor.Execute(config);

            return true;
        }

        private void ShowHelp()
        {
            var helpText = File.ReadAllLines("_help.txt");
            foreach (var textLine in helpText)
            {
                Console.Out.WriteLine(textLine);
            }
        }

        private void Log(string str)
        {
            _logger?.Log(str);
        }

    }
}
