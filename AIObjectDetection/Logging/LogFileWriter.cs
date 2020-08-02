using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;

namespace AICore.Logging
{
    abstract class LogFileWriter : ILogWriter
    {
        protected string LogDirectory;
        protected string LogFileName;
        protected int MaxLines;
        private Queue Buffer;
        private object FlushSynchronizer = new object();

        public LogFileWriter(string logDirectory, string logName, int maxLines)
        {
            LogDirectory = logDirectory;
            LogFileName = logName;
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

        public abstract string GenerateLogFilePath();
    }
}
