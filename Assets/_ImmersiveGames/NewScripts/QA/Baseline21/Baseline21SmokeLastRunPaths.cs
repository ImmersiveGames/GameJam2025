using System;
using System.IO;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Baseline21
{
    internal static class Baseline21SmokeLastRunPaths
    {
        internal const string RelativeReportsDir = "_ImmersiveGames/NewScripts/Docs/Reports";
        internal const string LastRunLogFile = "Baseline-2.1-Smoke-LastRun.log";
        internal const string StateFileName = "Baseline-2.1-Smoke-LastRun.state";

        internal static string ReportsDirAbs => Path.Combine(Application.dataPath, RelativeReportsDir);
        internal static string LastRunLogAbs => Path.Combine(ReportsDirAbs, LastRunLogFile);

        internal static string StateFilePath
        {
            get
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                return Path.Combine(projectRoot, "Library", "Temp", StateFileName);
            }
        }

        internal static string GetTimestampedLogAbs(DateTime utcNow)
        {
            var timestamp = utcNow.ToString("yyyyMMdd-HHmmss");
            var fileName = $"Baseline-2.1-Smoke-{timestamp}.log";
            return Path.Combine(ReportsDirAbs, fileName);
        }
    }
}
