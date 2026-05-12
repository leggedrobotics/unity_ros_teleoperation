using System;
using UnityEngine;
using LookingGlass.Toolkit;

namespace LookingGlass {
    public class UnityLogger : LookingGlass.Toolkit.ILogger {
        public void Log(string message) {
            Debug.Log(message);
        }

        public void LogError(string message) {
            Debug.LogError(message);
        }

        public void LogException(Exception e) {
            Debug.LogError(e.ToString());
        }
    }
}
