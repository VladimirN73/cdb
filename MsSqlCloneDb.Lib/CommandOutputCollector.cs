using System.Collections.Generic;
using System.Data.SqlClient;

namespace MsSqlCloneDb.Lib
{
    internal class CommandOutputCollector
    {
        #region Fields

        public static CommandOutputCollector instance = new CommandOutputCollector();

        private readonly List<string> _outputList = new List<string>();
        private bool _captureOutput = true;

        #endregion Fields

        #region Methods

        public void AddOutputMessage(string message)
        {
            if (_captureOutput)
            {
                _outputList.Add(message);
            }
        }

        public IReadOnlyList<string> GetOutput()
        {
            return new List<string>(_outputList);
        }

        public void Reset()
        {
            _outputList.Clear();
        }

        public void Disable()
        {
            _captureOutput = false;
            Reset();
        }

        public void Enable()
        {
            Reset();
            _captureOutput = true;
        }

        public static void CollectCommandOutput(object obj, SqlInfoMessageEventArgs e)
        {
            instance.Enable();
            instance.AddOutputMessage(e.Message);
        }

        public static void AddOutputCollectorHandler(SqlConnection con)
        {
            RemoveOutputCollectorHandler(con);
            con.InfoMessage += CollectCommandOutput;
        }

        public static void RemoveOutputCollectorHandler(SqlConnection con)
        {
            con.InfoMessage -= CollectCommandOutput;
        }

        #endregion Methods
    }
}