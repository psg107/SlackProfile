using System;
using System.IO;

namespace SlackProfile.Helpers
{
    public static class Logger
    {
        private readonly static string logDirectory = "logs";
        private readonly static string logPath = Path.Combine(logDirectory, $"{DateTime.Now.ToString("yyyyMMdd")}.txt");
        
        public static void WriteLine(string message)
        {
            Directory.CreateDirectory(logDirectory);

            File.AppendAllText(logPath, $"[{DateTime.Now.ToString("HH:mm:ss")}] {message}\n");
        }
    }
}
