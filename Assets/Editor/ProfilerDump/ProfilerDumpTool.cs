using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;

namespace UvgRos.EditorTools
{
    /// <summary>
    /// Batch-mode tool to dump real per-frame marker timing out of a saved
    /// Profiler .data capture, using Unity's own ProfilerDriver/
    /// HierarchyFrameDataView APIs -- run via:
    ///   Unity -batchmode -nographics -projectPath &lt;proj&gt; -quit
    ///     -executeMethod UvgRos.EditorTools.ProfilerDumpTool.DumpFromCommandLine
    ///     -dumpProfilerData /abs/path/to/capture.data
    ///     -dumpOutputPrefix /abs/path/to/output_prefix
    ///     -dumpStallMs 15
    /// Writes "<prefix>_markers.csv" (aggregated self-time per marker name,
    /// main thread only, across all frames) and "<prefix>_stalls.txt" (every
    /// individual marker instance whose self time exceeded the stall
    /// threshold, in frame order) so a real per-frame cost can actually be
    /// inspected instead of just knowing a marker name exists in the file.
    /// </summary>
    public static class ProfilerDumpTool
    {
        private class MarkerAgg
        {
            public int count;
            public double totalSelfMs;
            public double maxSelfMs;
        }

        public static void DumpFromCommandLine()
        {
            try
            {
                string dataPath = GetArgValue("-dumpProfilerData");
                string outputPrefix = GetArgValue("-dumpOutputPrefix");
                float stallMs = 15f;
                string stallArg = GetArgValue("-dumpStallMs");
                if (!string.IsNullOrEmpty(stallArg)) float.TryParse(stallArg, out stallMs);

                if (string.IsNullOrEmpty(dataPath) || string.IsNullOrEmpty(outputPrefix))
                {
                    Console.WriteLine("[ProfilerDumpTool] missing -dumpProfilerData or -dumpOutputPrefix");
                    EditorApplication.Exit(1);
                    return;
                }

                dataPath = Path.GetFullPath(dataPath);
                Console.WriteLine($"[ProfilerDumpTool] loading profile: {dataPath}");

                bool loaded = ProfilerDriver.LoadProfile(dataPath, false);
                if (!loaded)
                {
                    Console.WriteLine("[ProfilerDumpTool] ProfilerDriver.LoadProfile returned false");
                    EditorApplication.Exit(1);
                    return;
                }

                int first = ProfilerDriver.firstFrameIndex;
                int last = ProfilerDriver.lastFrameIndex;
                Console.WriteLine($"[ProfilerDumpTool] loaded frames {first}..{last} ({last - first + 1} frames)");

                var agg = new Dictionary<string, MarkerAgg>();
                var stallLines = new List<string>();
                var frameSummaryLines = new List<string> { "frame,mainThreadMs" };

                for (int frame = first; frame <= last; frame++)
                {
                    using (var view = ProfilerDriver.GetHierarchyFrameDataView(
                        frame, 0, HierarchyFrameDataView.ViewModes.Default,
                        HierarchyFrameDataView.columnTotalTime, false))
                    {
                        if (view == null || !view.valid) continue;

                        int rootId = view.GetRootItemID();
                        double frameTotalMs = view.GetItemColumnDataAsSingle(rootId, HierarchyFrameDataView.columnTotalTime);
                        frameSummaryLines.Add($"{frame},{frameTotalMs:F3}");

                        WalkItem(view, rootId, frame, agg, stallLines, stallMs);
                    }
                }

                var markerCsv = new List<string> { "marker,count,totalSelfMs,avgSelfMs,maxSelfMs" };
                foreach (var kv in agg.OrderByDescending(kv => kv.Value.totalSelfMs))
                {
                    var m = kv.Value;
                    double avg = m.totalSelfMs / Math.Max(1, m.count);
                    markerCsv.Add($"\"{kv.Key}\",{m.count},{m.totalSelfMs:F3},{avg:F3},{m.maxSelfMs:F3}");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPrefix)) ?? ".");
                File.WriteAllLines(outputPrefix + "_markers.csv", markerCsv);
                File.WriteAllLines(outputPrefix + "_stalls.txt", stallLines);
                File.WriteAllLines(outputPrefix + "_frames.csv", frameSummaryLines);

                Console.WriteLine($"[ProfilerDumpTool] wrote {outputPrefix}_markers.csv ({markerCsv.Count - 1} markers)");
                Console.WriteLine($"[ProfilerDumpTool] wrote {outputPrefix}_stalls.txt ({stallLines.Count} stalls >= {stallMs}ms)");
                Console.WriteLine($"[ProfilerDumpTool] wrote {outputPrefix}_frames.csv ({frameSummaryLines.Count - 1} frames)");
                Console.WriteLine("[ProfilerDumpTool] DONE");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Console.WriteLine("[ProfilerDumpTool] EXCEPTION: " + e);
                EditorApplication.Exit(1);
            }
        }

        private static void WalkItem(HierarchyFrameDataView view, int itemId, int frame,
            Dictionary<string, MarkerAgg> agg, List<string> stallLines, float stallMs)
        {
            string name = view.GetItemName(itemId);
            double selfMs = view.GetItemColumnDataAsSingle(itemId, HierarchyFrameDataView.columnSelfTime);

            if (!string.IsNullOrEmpty(name))
            {
                if (!agg.TryGetValue(name, out var m))
                {
                    m = new MarkerAgg();
                    agg[name] = m;
                }
                m.count++;
                m.totalSelfMs += selfMs;
                if (selfMs > m.maxSelfMs) m.maxSelfMs = selfMs;

                if (selfMs >= stallMs)
                    stallLines.Add($"frame={frame} selfMs={selfMs:F2} marker=\"{name}\"");
            }

            var children = new List<int>();
            view.GetItemChildren(itemId, children);
            foreach (var childId in children)
                WalkItem(view, childId, frame, agg, stallLines, stallMs);
        }

        private static string GetArgValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
