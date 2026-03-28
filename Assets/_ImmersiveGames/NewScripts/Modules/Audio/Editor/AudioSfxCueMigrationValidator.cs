#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using _ImmersiveGames.NewScripts.Modules.Audio.Config;
using UnityEditor;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Editor
{
    internal static class AudioSfxCueMigrationValidator
    {
        private const string CueFolder = "Assets/_ImmersiveGames/NewScripts/Modules/Audio/Content/Cue";

        [MenuItem("Tools/NewScripts/Audio/Validate SFX Cue Migration")]
        public static void ValidateAll()
        {
            var guids = AssetDatabase.FindAssets("t:AudioSfxCueAsset", new[] { CueFolder });
            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning($"[Audio][Migration][SFXCue] No AudioSfxCueAsset found under '{CueFolder}'.");
                return;
            }

            var issues = new List<string>();
            int readyCount = 0;
            int warningCount = 0;
            int errorCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var cue = AssetDatabase.LoadAssetAtPath<AudioSfxCueAsset>(path);
                if (cue == null)
                {
                    continue;
                }

                var result = ValidateCue(cue);
                if (result.ErrorCount == 0 && result.WarningCount == 0)
                {
                    readyCount++;
                }

                warningCount += result.WarningCount;
                errorCount += result.ErrorCount;

                if (!string.IsNullOrWhiteSpace(result.Message))
                {
                    issues.Add(result.Message);
                }
            }

            foreach (var issue in issues)
            {
                Debug.Log(issue);
            }

            Debug.Log(
                $"[Audio][Migration][SFXCue] Summary ready={readyCount} warnings={warningCount} errors={errorCount} folder='{CueFolder}'.");
        }

        private static ValidationResult ValidateCue(AudioSfxCueAsset cue)
        {
            var sb = new StringBuilder();
            int warningCount = 0;
            int errorCount = 0;

            if (cue == null)
            {
                return new ValidationResult(0, 1, "[Audio][Migration][SFXCue] ERROR cue is null.");
            }

            bool emissionReady = cue.EmissionProfile != null;
            bool executionReady = cue.ExecutionProfile != null;

            if (!emissionReady)
            {
                errorCount++;
                AppendLine(sb, $"[Audio][Migration][SFXCue] ERROR cue='{cue.name}' missing EmissionProfile.");
            }

            if (!executionReady)
            {
                errorCount++;
                AppendLine(sb, $"[Audio][Migration][SFXCue] ERROR cue='{cue.name}' missing ExecutionProfile.");
            }
            else
            {
                if (cue.ExecutionProfile.ExecutionMode == AudioSfxExecutionMode.PooledOneShot &&
                    cue.ExecutionProfile.PooledVoiceProfile == null)
                {
                    errorCount++;
                    AppendLine(sb, $"[Audio][Migration][SFXCue] ERROR cue='{cue.name}' pooled ExecutionProfile missing PooledVoiceProfile.");
                }
            }

            if (errorCount == 0 && warningCount == 0)
            {
                AppendLine(sb, $"[Audio][Migration][SFXCue] READY cue='{cue.name}' canonical profiles present.");
            }

            return new ValidationResult(warningCount, errorCount, sb.ToString().TrimEnd());
        }

        private static void AppendLine(StringBuilder sb, string line)
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
            }

            sb.Append(line);
        }

        private readonly struct ValidationResult
        {
            public ValidationResult(int warningCount, int errorCount, string message)
            {
                WarningCount = warningCount;
                ErrorCount = errorCount;
                Message = message;
            }

            public int WarningCount { get; }
            public int ErrorCount { get; }
            public string Message { get; }
        }
    }
}
#endif
