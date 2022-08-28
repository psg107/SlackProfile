using System;
using System.IO;

namespace SlackProfile.Helpers
{
    public static class Logger
    {
        public static string LogDirectory = "logs";
        public static string LogPath => Path.Combine(LogDirectory, $"{DateTime.Now.ToString("yyyyMMdd")}.txt");
        
        public static void WriteLine(string message)
        {
            Directory.CreateDirectory(LogDirectory);

            File.AppendAllText(LogPath, $"[{DateTime.Now.ToString("HH:mm:ss")}] {message}\n");
        }
    }
}
