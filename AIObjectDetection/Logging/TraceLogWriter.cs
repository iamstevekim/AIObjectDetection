using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using System.Transactions;
using System.Threading;

namespace AICore.Logging
{
    class TraceLogWriter : ILogWriter
    {
        private string LogDirectory;
        private string LogName;
        private int MaxLines;
        private Queue Buffer;
        private object FlushSynchronizer = new object();

        CancellationTokenSource Source = new CancellationTokenSource();
        Task LogDelay;
        public TraceLogWriter(string logDirectory, string logName, int maxLines)
        {
            LogDirectory = logDirectory;
            LogName = logName;
            MaxLines = maxLines;
            Buffer = Queue.Synchronized(new Queue());
        }
        public async Task Log(string msg)
        {
            lock (this)
            {
                string logMsg = $"{DateTime.Now.ToString()}: {msg}";
                Buffer.Enqueue(logMsg);
            }
            await Task.Run(() => FlushLogs());
        }

        private void FlushLogs()
        {
            lock (FlushSynchronizer)
            {
                if (Buffer.Count == 0)
                    return;

                using (FileStream fs = new FileStream(GenerateLogFilePath(), FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        while (Buffer.Count > 0)
                        {
                            sw.WriteLine(Buffer.Dequeue());
                        }
                    }
                }
            }
        }

        private string CreateLogFormatDateString()
        {
            DateTime today = DateTime.Now;
            return $"{today.Year.ToString()}-{ today.Month.ToString().PadLeft(2, Convert.ToChar("0"))}-{ today.Day.ToString().PadLeft(2, Convert.ToChar("0"))}";
        }
        private string GenerateLogFilePath()
        {
            DateTime today = DateTime.Now;
            DirectoryInfo dirTest = new DirectoryInfo($"{LogDirectory}\\Logs\\{today.Year}\\{today.Month.ToString().PadLeft(2, Convert.ToChar("0"))}\\{today.Day.ToString().PadLeft(2, Convert.ToChar("0"))}");
            if (!dirTest.Exists)
                dirTest.Create();

            string logDateConvention = $"{dirTest.FullName}\\{LogName}{CreateLogFormatDateString()}";

            FileInfo logFileTest = new FileInfo($"{logDateConvention}.log");
            int counter = 0;
            if (logFileTest.Exists)
            {
                while (logFileTest.Length > MaxLines)
                {
                    counter++;
                    logFileTest = new FileInfo($"{logDateConvention}-{counter.ToString()}.log");
                    if (!logFileTest.Exists)
                        break;
                }
            }

            return logFileTest.FullName;

        }
    }
}
