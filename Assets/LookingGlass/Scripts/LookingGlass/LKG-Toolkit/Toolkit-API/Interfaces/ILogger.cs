using System;

namespace LookingGlass.Toolkit {
    public interface ILogger {
        public void Log(object obj) => Log(obj == null ? "null" : obj.ToString());
        public void Log(string message);
        public void LogError(string message);
        public void LogException(Exception e);
    }
}
