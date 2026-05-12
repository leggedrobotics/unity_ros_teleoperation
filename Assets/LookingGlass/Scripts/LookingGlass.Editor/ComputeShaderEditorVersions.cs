using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

namespace LookingGlass.Editor {
    public class ComputeShaderEditorVersion : AssetPostprocessor {
        private struct Regexes {
            public Regex unityVersion;
            public Regex ourCustomSyntax;
            public Regex isCommented;
            public Regex isPragma;
        }

        private static readonly Lazy<Regexes> CachedRegexes = new Lazy<Regexes>(
            () => new Regexes() {
                unityVersion = new Regex("(?<major>[0-9]{4})\\.(?<minor>[0-9]{1})"),
                ourCustomSyntax = new Regex("\\/\\/#(?<negated>!{0,1})UNITY_(?<major>[0-9]{4})_(?<minor>[0-9]{1})_OR_NEWER"),
                isCommented = new Regex("\\s*(?<commentStart>\\/{2,})"),
                isPragma = new Regex("\\s*#pragma kernel")
            }
        );

        //NOTE: We want to make sure we DO NOT alter users' assets!
        //This is just for our ViewInterpolation shader.. for now.
        private bool ShouldModifyAsset(string assetPath) {
            return assetPath.EndsWith("ViewInterpolation.compute");
        }

        private void OnPreprocessAsset() {
            if (!ShouldModifyAsset(assetPath))
                return;

            Regexes cachedRegexes = CachedRegexes.Value;

            Match version = cachedRegexes.unityVersion.Match(Application.unityVersion);
            int unityMajorVersion = int.Parse(version.Groups["major"].Value);
            int unityMinorVersion = int.Parse(version.Groups["minor"].Value);

            string[] lines = File.ReadAllLines(assetPath);

            if (CheckForFileEdits(unityMajorVersion, unityMinorVersion, lines, cachedRegexes))
                File.WriteAllLines(assetPath, lines);
        }

        private bool CheckForFileEdits(int unityMajorVersion, int unityMinorVersion, string[] lines, Regexes cachedRegexes) {
            bool dirty = false;
            bool isSpecialRegion = false;
            bool regionShouldBeCommented = true;
            for (int i = 0; i < lines.Length; i++) {
                string line = lines[i];
                if (isSpecialRegion) {
                    if (!cachedRegexes.isPragma.IsMatch(line)) {
                        isSpecialRegion = false;
                        continue;
                    }

                    Match lineCommentedMatch = cachedRegexes.isCommented.Match(line);
                    int commentStartIndex = lineCommentedMatch.Groups["commentStart"].Index;
                    bool isCommented = lineCommentedMatch.Groups["commentStart"].Success;

                    if (regionShouldBeCommented != isCommented) {
                        if (regionShouldBeCommented)
                            lines[i] = line = line.Insert(commentStartIndex, "//");
                        else
                            lines[i] = line = line.Remove(commentStartIndex, lineCommentedMatch.Groups["commentStart"].Length);
                        dirty = true;
                    }
                } else {
                    Match lineMatch = cachedRegexes.ourCustomSyntax.Match(line);
                    if (lineMatch.Success) {
                        isSpecialRegion = true;

                        bool negated = lineMatch.Groups["negated"].Length > 0;
                        int commentMajorVersion = int.Parse(lineMatch.Groups["major"].Value);
                        int commentMinorVersion = int.Parse(lineMatch.Groups["minor"].Value);

                        regionShouldBeCommented = unityMajorVersion < commentMajorVersion
                            || (unityMajorVersion == commentMajorVersion && unityMinorVersion < commentMinorVersion);
                        if (negated)
                            regionShouldBeCommented = !regionShouldBeCommented;
                    }
                }
            }
            return dirty;
        }
    }
}