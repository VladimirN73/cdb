using System.Drawing;

namespace MsSqlCloneDb
{
    public interface ILogSink
    {
        void AddLogEntry(string log);
        void AddLogEntry(string log, Color color);
        void AddBoldLogEntry(string log);
        void AddBoldLogEntry(string log, Color color);
    }
}
