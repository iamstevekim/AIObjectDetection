using System.Threading.Tasks;

namespace AICore
{
    public delegate void LogErrorHandler(string msg);
    public delegate void LogTraceHandler(string msg);
    interface ILogging
    {
        event LogErrorHandler LogErrorDelegate;
        event LogTraceHandler LogTraceDelegate;
    }
}
