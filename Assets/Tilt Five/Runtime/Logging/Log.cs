/*
 * Copyright (C) 2020-2023 Tilt Five, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TiltFive.Logging
{

    [System.Serializable]
    public class LogSettings
    {
        [Range(0, 5)]
        public int level = Log.VERBOSE_LEVEL;
        public string TAG = "TFI";

#if UNITY_EDITOR
        internal LogSettings Copy()
        {
            return (LogSettings)MemberwiseClone();
        }
#endif
    }

    /// <summary>
    /// The Logger.
    /// </summary>
	public class Log : Singleton<Log>
    {

        /// <summary> The logger instance. </summary>
		private ILogger logger = UnityEngine.Debug.unityLogger;
        
        /// <summary> The logging tag. </summary>
		private string tag = "Tilt Five, Inc.";
        
        /// <summary> The logging level. </summary>
		private int level = VERBOSE_LEVEL;

        /// <summary>
        /// Gets or sets the logging tag.
        /// </summary>
        /// <value>The tag.</value>
		public static string TAG
        {
            get => Instance.tag;
            set => Instance.tag = value;
        }

        /// <summary>
        /// Gets or sets the logging level.
        /// </summary>
        /// <value>The log level.</value>
		public static int LogLevel
        {
            get => Instance.level;
            set
            {
                Instance.level = value;

                if (INFO_LEVEL <= value)
                    Instance.logger.filterLogType = LogType.Log;
                else if (WARN_LEVEL == value)
                    Instance.logger.filterLogType = LogType.Warning;
                else if (ERROR_LEVEL == value)
                    Instance.logger.filterLogType = LogType.Error;
                else
                    Instance.logger.filterLogType = LogType.Log;
            }
        }
        /// <summary> DEBUG logging level as a string. </summary>
		private const string DEBUG = "DEBUG";
        /// <summary> ERROR logging level as a string. </summary>
		private const string ERROR = "ERROR";
        /// <summary> INFO logging level as a string. </summary>
		private const string INFO = "INFO";
        /// <summary> VERBOSE logging level as a string. </summary>
		private const string VERBOSE = "VERBOSE";
        /// <summary> WARN logging level as a string. </summary>
		private const string WARN = "WARN";

        /// <summary> DEBUG logging level as an int. </summary>
        public const int DEBUG_LEVEL = 1;
        /// <summary> ERROR logging level as an int. </summary>
        public const int ERROR_LEVEL = 4;
        /// <summary> INFO logging level as an int. </summary>
        public const int INFO_LEVEL = 2;
        /// <summary> VERBOSE logging level as an int. </summary>
        public const int VERBOSE_LEVEL = 0;
        /// <summary> WARN logging level as an int. </summary>
        public const int WARN_LEVEL = 3;
        /// <summary> DISABLED logging level as an int. </summary>
        public const int DISABLED = 5;

        /// <summary>
        /// DEBUG logging function call.
        /// </summary>
        /// <param name="m">The string message to log.</param>
        /// <param name="list">Optional ist of input parameters to <paramref name="m"/>, when following string.Format rules.</param>
		public static void Debug(string m, params object[] list)
        {            
            if (DEBUG_LEVEL >= LogLevel)
            {
                log(LogType.Log, DEBUG, m, list);
            }
        }

        /// <summary>
        /// ERROR logging function call.
        /// </summary>
        /// <param name="m">The string message to log.</param>
        /// <param name="list">Optional ist of input parameters to <paramref name="m"/>, when following string.Format rules.</param>

        public static void Error(string m, params object[] list)
        {
            if (ERROR_LEVEL >= LogLevel)
            {
                log(LogType.Error, ERROR, m, list);
            }
        }

        /// <summary>
        /// INFO logging function call.
        /// </summary>
        /// <param name="m">The string message to log.</param>
        /// <param name="list">Optional ist of input parameters to <paramref name="m"/>, when following string.Format rules.</param>
        public static void Info(string m, params object[] list)
        {
            if (INFO_LEVEL >= LogLevel)
            {
                log(LogType.Log, INFO, m, list);
            }
        }

        /// <summary>
        /// VERBOSE logging function call.
        /// </summary>
        /// <param name="m">The string message to log.</param>
        /// <param name="list">Optional ist of input parameters to <paramref name="m"/>, when following string.Format rules.</param>
        public static void Verbose(string m, params object[] list)
        {
            if (VERBOSE_LEVEL >= LogLevel)
            {
                log(LogType.Log, VERBOSE, m, list);
            }
        }

        /// <summary>
        /// WARN logging function call.
        /// </summary>
        /// <param name="m">The string message to log.</param>
        /// <param name="list">Optional ist of input parameters to <paramref name="m"/>, when following string.Format rules.</param>
        public static void Warn(string m, params object[] list)
        {
            if (WARN_LEVEL >= LogLevel)
            {
                log(LogType.Warning, WARN, m, list);
            }
        }

        /// <summary>
        /// Universal log write function.
        /// </summary>
        /// <param name="logType">The logging type.</param>
        /// <param name="tag">The logging tag.</param>
        /// <param name="m">The logging message.</param>
        /// <param name="list">Optional ist of input parameters to <paramref name="m"/>, when following string.Format rules.</param>
        private static void log(LogType logType, string tag, string m, params object[] list)
        {
            Instance.logger.Log(logType, tag, string.Format("[{0}]\n{1}", TAG, string.Format(m, list)));
        }
    }
}