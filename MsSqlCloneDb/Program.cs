using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MsSqlCloneDb.Lib;

namespace MsSqlCloneDb
{
    internal static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Logger.AddLogEntry($@"Clone Tool v.{Application.ProductVersion}");

            if (args.Length < 1)
            {
                StartAsWinform();
            }
            else
            {
                StartAsConsole(args);
            }
        }

        private static void StartAsWinform()
        {           
            Logger.AddLogEntry("Started in Window-Mode");
            Logger.AddLogEntry("To start in Console-Mode please provide the command-lines parameters");
            Logger.AddLogEntry("Example:");
            Logger.AddLogEntry("MsSqlCloneDb -help");
   
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void StartAsConsole(string[] args)
        {
            var isStreamingConsoleOutput = false;

            try
            {
                const string optionStreamConsoleOutput = "streamConsoleOutput";
                isStreamingConsoleOutput = CmdParameterParser.HasOption(args, optionStreamConsoleOutput);

                if (isStreamingConsoleOutput)
                {
                    SetConsoleOutputStreaming();
                }

                var showHelp = CmdParameterParser.HasOption(args, "help");

                if (showHelp)
                {
                    ShowHelp();
                    return;
                }

                Logger.AddLogEntry(@"Started in Console Mode");
                Logger.AddLogEntry(@"Detected (but not loaded yet) parameters in commandline");
                var str = string.Join("\n", args);
                Logger.AddLogEntry(str);

                Logger.AddLogEntry("");

                var myParams = CloneParametersExt.GetParameters(args);

                Logger.AddLogEntry("\nLoaded (accepted) parameters from commandline");
                CloneParametersExt.PrintParameters(myParams, Logger);

                // Merge with App.Config
                myParams = CloneParametersExt.AdaptParameters(myParams);
                Logger.AddLogEntry("\nWorking (combined with App.Config) parameters");
                CloneParametersExt.PrintParameters(myParams, Logger);

                CloneParametersExt.DecryptParameters(myParams, Logger);

                CloneParametersExt.ReplaceVariablesInFinalScripts(myParams, Logger);

                //do not print decrypted parameters, due to the plain-text Connection-string shall be hidden 
                //CloneParametersExt.PrintParameters(myParams, Logger);
                
                var retSuccess = Doit(myParams);
                if (!retSuccess)
                {
                    throw new Exception("Failed. See Log for more Info.");
                }

            }
            catch (Exception ex)
            {
                var errorLogBuilder = new StringBuilder();
                errorLogBuilder.AppendLine("=========================================================");
                errorLogBuilder.AppendLine($"Fehler/Error:{ex}");
                errorLogBuilder.AppendLine("=========================================================");

                var errorLog = errorLogBuilder.ToString();

                Logger.AddLogEntry(errorLog);

                if (isStreamingConsoleOutput)
                {
                    Console.Error.WriteLine(errorLog);
                }

                Environment.Exit(1);
            }
            finally
            {
                Logger.AddLogEntry("Finish");
                Console.Out.Close();
            }
        }

        private static void SetConsoleOutputStreaming()
        {
            var writer = new StreamWriter("ConsoleOutput.txt") {AutoFlush = true};
            var errorWriter = new StreamWriter("ConsoleError.txt") { AutoFlush = true };

            Console.SetOut(writer);
            Console.SetError(errorWriter);
        }

        private static bool Doit(CloneParametersExt config)
        {
            ICloneProcessor cloneProcessor = new CloneProcessor();

            cloneProcessor.Execute(config);

            return true;
        }


        public static void ShowHelp()
        {
            var helpText = File.ReadAllLines("_help.txt");
            foreach (var textLine in helpText)
            {
                Console.Out.WriteLine(textLine);
            }
        }

        private static ILogSink Logger => MyLogger.Instance;

    }
    
    public class MyLogger : ILogSink
    {
        public void AddLogEntry(string log)
        {
            AddLogEntry(log, Color.White);
        }

        public void AddLogEntry(string log, Color color)
        {
            //var currentColor = Console.ForegroundColor;
            //var consoleColor = Enum.Parse(typeof(ConsoleColor), color.Name);
            //if (consoleColor == null)
            //{
            //    consoleColor = ConsoleColor.White;
            //}
            //Console.ForegroundColor = (ConsoleColor)consoleColor;
            
            //Console.WriteLine(log); // this does not work for win app. 
           
            //Console.ForegroundColor = currentColor;

            Trace.TraceInformation(log);

            Console.Out.WriteLine(log);

        }

        public void AddBoldLogEntry(string log)
        {
            AddLogEntry(log, Color.White);
        }

        public void AddBoldLogEntry(string log, Color color)
        {
            AddLogEntry(log, color);
        }

        private static MyLogger _instance;
        public static MyLogger Instance => _instance ?? (_instance = new MyLogger());
    }
}
