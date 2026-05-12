// Thanks to https://github.com/needle-mirror/com.unity.recorder/blob/master/Editor/Sources/FileNameGenerator.cs
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LookingGlass {
    public class PathUtil {
        private static readonly Lazy<Regex> InvalidFileNamePattern = new Lazy<Regex>(() => {
            string invalidCharacters = new string(Path.GetInvalidFileNameChars());
            return new Regex(string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidCharacters));
        });

        private static readonly Lazy<Regex> InvalidFilePathPattern = new Lazy<Regex>(() => {
            char[] f = Path.GetInvalidFileNameChars();
            char[] p = Path.GetInvalidPathChars();
            char[] all = f.Union(p).Where(
                c => c != '/' &&
                c != '\\' &&
                c != ':') //TODO: Properly handle path santization, ex: "K:/file.txt" is valid, but "K:/file:name.txt" is NOT VALID.
                .ToArray();

            string invalidCharacters = new string(all);
            return new Regex(string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidCharacters));
        });

        private static readonly Lazy<Regex> MultiSlashPattern = new Lazy<Regex>(() =>
             new Regex("/{2,}")
        );

        /// <summary>
        /// Gets rid of invalid character in file or folder name.
        /// </summary>
        /// <param name="fileName">The file/folder name to sanitize.</param>
        /// <returns>The file/folder name with occurrences of invalid characters replaced with <c>"_"</c> characters.</returns>
        public static string SanitizeFileName(string fileName) {
            return InvalidFileNamePattern.Value.Replace(fileName, "_");
        }

        /// <summary>
        /// Makes the file path compliant with any OS (replacing any "\" by "/"), and replacing multiple consecutive "/" characters with a single "/" character.
        /// </summary>
        /// <param name="path">The path to sanitize.</param>
        /// <returns>The full path with single slashes "/" as folder separators.</returns>
        public static string SanitizeFilePath(string path) {
            string result = MultiSlashPattern.Value.Replace(path.Replace('\\', '/'), "/");
            return InvalidFilePathPattern.Value.Replace(result, "_");
        }
    }
}
