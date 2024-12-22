using System;
using System.IO;

namespace NetworkLatencyOptimizer.Core
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public static class Logger
    {
        private static readonly string LogFile = "Logs/optimizer.log";
        private static readonly object LockObj = new object();

        static Logger()
        {
            string directory = Path.GetDirectoryName(LogFile);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {level} - {message}";
            
            lock (LockObj)
            {
                try
                {
                    File.AppendAllText(LogFile, logMessage + Environment.NewLine);
                    Console.WriteLine(logMessage);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"日志记录失败: {ex.Message}");
                }
            }
        }
    }
} 