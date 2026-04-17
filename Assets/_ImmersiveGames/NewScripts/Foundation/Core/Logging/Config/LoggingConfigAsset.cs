using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImmersiveGames.GameJam2025.Core.Logging.Config
{
    [CreateAssetMenu(
        fileName = "LoggingConfig",
        menuName = "ImmersiveGames/NewScripts/Core/Logging/LoggingConfigAsset",
        order = 30)]
    public sealed class LoggingConfigAsset : ScriptableObject
    {
        [Serializable]
        public sealed class NamespaceRule
        {
            public string ruleId = string.Empty;
            public bool enabled = true;
            public string namespacePrefix = string.Empty;
            public DebugLevel level = DebugLevel.Logs;
        }

        [Header("Global")]
        [SerializeField] private bool globalEnabled = true;
        [SerializeField] private bool verboseEnabled = true;
        [SerializeField] private bool fallbacksEnabled = true;
        [SerializeField] private bool repeatedVerboseEnabled = true;
        [SerializeField] private DebugLevel defaultLevel = DebugLevel.Logs;

        [Header("Namespace Rules")]
        [SerializeField] private List<NamespaceRule> rules = new();

        public bool GlobalEnabled => globalEnabled;
        public bool VerboseEnabled => verboseEnabled;
        public bool FallbacksEnabled => fallbacksEnabled;
        public bool RepeatedVerboseEnabled => repeatedVerboseEnabled;
        public DebugLevel DefaultLevel => defaultLevel;
        public IReadOnlyList<NamespaceRule> Rules => rules;

#if UNITY_EDITOR
        private void OnValidate()
        {
            TrimRules();
            WarnInvalidRules();
        }
#endif

        private void TrimRules()
        {
            if (rules == null)
            {
                return;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null)
                {
                    continue;
                }

                rule.ruleId = (rule.ruleId ?? string.Empty).Trim();
                rule.namespacePrefix = (rule.namespacePrefix ?? string.Empty).Trim();
            }
        }

        private void WarnInvalidRules()
        {
            if (rules == null || rules.Count == 0)
            {
                return;
            }

            var ruleIds = new HashSet<string>(StringComparer.Ordinal);
            var prefixes = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null)
                {
                    Debug.LogWarning($"[WARNING] [LoggingConfigAsset] Rule at index {i} is null in asset '{name}'.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(rule.namespacePrefix))
                {
                    Debug.LogWarning($"[WARNING] [LoggingConfigAsset] Rule '{rule.ruleId}' has empty namespacePrefix in asset '{name}'.");
                }
                else if (LooksLikeProjectNamespace(rule.namespacePrefix) && !rule.namespacePrefix.StartsWith("_", StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[WARNING] [LoggingConfigAsset] Rule '{rule.ruleId}' uses suspicious namespacePrefix '{rule.namespacePrefix}' (missing '_' prefix).");
                }

                if (!string.IsNullOrWhiteSpace(rule.ruleId) && !ruleIds.Add(rule.ruleId))
                {
                    Debug.LogWarning($"[WARNING] [LoggingConfigAsset] Duplicate ruleId '{rule.ruleId}' in asset '{name}'.");
                }

                if (!string.IsNullOrWhiteSpace(rule.namespacePrefix) && !prefixes.Add(rule.namespacePrefix))
                {
                    Debug.LogWarning($"[WARNING] [LoggingConfigAsset] Duplicate namespacePrefix '{rule.namespacePrefix}' in asset '{name}'.");
                }
            }
        }

        private static bool LooksLikeProjectNamespace(string prefix)
        {
            return prefix.IndexOf("ImmersiveGames.NewScripts", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

