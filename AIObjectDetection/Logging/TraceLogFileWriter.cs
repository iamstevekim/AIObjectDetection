using System;
using System.IO;

namespace AICore.Logging
{
    class TraceLogFileWriter : LogFileWriter
    {
        public TraceLogFileWriter(string logDirectory, string logFileName, int maxLines) : base(logDirectory, logFileName, maxLines) { }

        public override string GenerateLogFilePath()
        {
            DateTime today = DateTime.Now;
            DirectoryInfo dirTest = new DirectoryInfo($"{LogDirectory}\\Logs\\{today.Year}\\{today.Month.ToString().PadLeft(2, Convert.ToChar("0"))}\\{today.Day.ToString().PadLeft(2, Convert.ToChar("0"))}");
            if (!dirTest.Exists)
                dirTest.Create();

            string logDateConvention = $"{dirTest.FullName}\\{LogFileName}{CreateLogFormatDateString()}";

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

        private string CreateLogFormatDateString()
        {
            DateTime today = DateTime.Now;
            return $"{today.Year.ToString()}-{ today.Month.ToString().PadLeft(2, Convert.ToChar("0"))}-{ today.Day.ToString().PadLeft(2, Convert.ToChar("0"))}";
        }
    }
}
