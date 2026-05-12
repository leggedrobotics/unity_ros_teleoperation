using System;

namespace LookingGlass.Toolkit {
    public class ConsoleLogger : ILogger {
        public void Log(string message) {
            Console.WriteLine(message);
        }

        public void LogError(string message) {
            Console.WriteLine("[ERROR] " + message);
        }

        public void LogException(Exception e) {
            Console.WriteLine(e.ToString());
        }
    }
}
