using System;
using System.IO;

namespace AICore.Logging
{
    class ErrorLogFileWriter : LogFileWriter
    {
        public ErrorLogFileWriter(string logDirectory, string logFileName, int maxLines) : base(logDirectory, logFileName, maxLines) { }

        public override string GenerateLogFilePath()
        {
            DateTime today = DateTime.Now;
            DirectoryInfo dirTest = new DirectoryInfo($"{LogDirectory}\\Logs");
            if (!dirTest.Exists)
                dirTest.Create();

            string logFileConvention = $"{dirTest.FullName}\\{LogFileName}";

            FileInfo logFileTest = new FileInfo($"{logFileConvention}.log");
            int counter = 0;
            if (logFileTest.Exists)
            {
                while (logFileTest.Length > MaxLines)
                {
                    counter++;
                    logFileTest = new FileInfo($"{logFileConvention}-{counter.ToString()}.log");
                    if (!logFileTest.Exists)
                        break;
                }
            }

            return logFileTest.FullName;
        }
    }
}
