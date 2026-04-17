using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImmersiveGames.GameJam2025.Core.Logging.Config;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ImmersiveGames.GameJam2025.Core.Logging
{
    public enum DebugLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Logs = 3,
        Verbose = 4
    }

    public static class DebugUtility
    {
        private readonly struct NamespaceRuleEntry
        {
            public NamespaceRuleEntry(string ruleId, string namespacePrefix, DebugLevel level)
            {
                RuleId = ruleId;
                NamespacePrefix = namespacePrefix;
                Level = level;
            }

            public string RuleId { get; }
            public string NamespacePrefix { get; }
            public DebugLevel Level { get; }
        }

        private readonly struct NamespaceRuleMatch
        {
            public NamespaceRuleMatch(bool hasMatch, string ruleId, string namespacePrefix, DebugLevel level)
            {
                HasMatch = hasMatch;
                RuleId = ruleId;
                NamespacePrefix = namespacePrefix;
                Level = level;
            }

            public bool HasMatch { get; }
            public string RuleId { get; }
            public string NamespacePrefix { get; }
            public DebugLevel Level { get; }
        }

        private static bool _globalDebugEnabled = true;
        private static bool _verboseLoggingEnabled = true;
        private static bool _logFallbacks = true;
        private static bool _repeatedCallVerboseEnabled = true;
        private static DebugLevel _defaultDebugLevel = DebugLevel.Logs;
        private static string _lastPolicyKey;
        private static int _lastPolicyFrame = -1;
        private static bool _hasAppliedPolicy;
        private static bool _lastAppliedGlobalDebugEnabled;
        private static bool _lastAppliedVerboseEnabled;
        private static bool _lastAppliedFallbacksEnabled;
        private static bool _lastAppliedRepeatedVerboseEnabled;
        private static DebugLevel _lastAppliedDefaultLevel = DebugLevel.Logs;
        private static string _lastAppliedSource;
        private static bool _lastAppliedEarlyDefault;
        private static int _mainThreadId = -1;

        private static readonly Dictionary<Type, DebugLevel> _scriptDebugLevels = new();
        private static readonly Dictionary<object, DebugLevel> _localLevels = new();
        private static readonly Dictionary<Type, DebugLevel> _attributeLevels = new();
        private static readonly Dictionary<Type, DebugLevel> _effectiveLevels = new();
        private static readonly Dictionary<Type, NamespaceRuleMatch> _matchedNamespaceRules = new();
        private static readonly List<NamespaceRuleEntry> _activeNamespaceRules = new();
        private static readonly HashSet<Type> _disabledVerboseTypes = new();

        // Tracking por frame (limpamos quando o frame muda).
        private static readonly HashSet<(string key, int frame)> _callTracker = new();
        private static readonly HashSet<(string key, int frame)> _repeatedCallTracker = new();
        private static int _lastTrackedFrame = -1;
        private static readonly object _verboseLogLock = new();

        private static readonly StringBuilder _stringBuilder = new(256);
        private static readonly Dictionary<string, string> _messagePool = new();

        private const string RepeatedCallColor = "#FFD54F";
        private const string AlertIcon = "[!]";

        public static class Colors
        {
            public const string CrucialInfo = "#00BCD4";
            public const string Info = "#A8DEED";
            public const string Success = "#4CAF50";
            public const string Warning = "yellow";
            public const string Error = "red";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
#if NEWSCRIPTS_MODE
            Debug.Log("NEWSCRIPTS_MODE ativo: DebugUtility.Initialize executando reset de estado.");
#endif
            _scriptDebugLevels.Clear();
            _localLevels.Clear();
            _attributeLevels.Clear();
            _effectiveLevels.Clear();
            _matchedNamespaceRules.Clear();
            _activeNamespaceRules.Clear();
            _disabledVerboseTypes.Clear();

            _callTracker.Clear();
            _repeatedCallTracker.Clear();
            _lastTrackedFrame = -1;
            _lastPolicyFrame = -1;
            _lastPolicyKey = null;
            _hasAppliedPolicy = false;
            _lastAppliedGlobalDebugEnabled = true;
            _lastAppliedVerboseEnabled = false;
            _lastAppliedFallbacksEnabled = false;
            _lastAppliedRepeatedVerboseEnabled = false;
            _lastAppliedDefaultLevel = DebugLevel.Logs;
            _lastAppliedSource = null;
            _lastAppliedEarlyDefault = true;
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;

            _messagePool.Clear();

            ApplyEarlyDefaultPolicy();
            LogInternal("[BOOT] DebugUtility initialized with EarlyDefault policy.");
        }

        #region Configuracoes
        public static bool IsGlobalDebugEnabled => _globalDebugEnabled;
        public static bool IsVerboseLoggingEnabled => _verboseLoggingEnabled;
        public static bool IsFallbacksEnabled => _logFallbacks;
        public static bool IsRepeatedCallVerboseEnabled => _repeatedCallVerboseEnabled;
        public static DebugLevel DefaultDebugLevel => _defaultDebugLevel;

        public static void SetGlobalDebugState(bool enabled) => _globalDebugEnabled = enabled;
        public static void SetVerboseLogging(bool enabled) => _verboseLoggingEnabled = enabled;
        public static void SetLogFallbacks(bool enabled) => _logFallbacks = enabled;
        public static void SetRepeatedCallVerbose(bool enabled) => _repeatedCallVerboseEnabled = enabled;
        public static bool GetRepeatedCallVerbose() => _repeatedCallVerboseEnabled;

        public static void DisableVerboseForType(Type type) => _disabledVerboseTypes.Add(type);
        public static void EnableVerboseForType(Type type) => _disabledVerboseTypes.Remove(type);

        public static void SetDefaultDebugLevel(DebugLevel level) => _defaultDebugLevel = level;
        public static void RegisterScriptDebugLevel(Type type, DebugLevel level)
        {
            _scriptDebugLevels[type] = level;
            InvalidateResolvedCaches("type_runtime_override_updated");
        }

        public static void SetLocalDebugLevel(object instance, DebugLevel level) => _localLevels[instance] = level;

        public static void ApplyEarlyDefaultPolicy()
        {
            ApplyLoggingPolicyInternal(
                globalDebugEnabled: true,
                verboseEnabled: false,
                fallbacksEnabled: false,
                repeatedVerboseEnabled: false,
                defaultLevel: DebugLevel.Logs,
                source: "EarlyDefault",
                namespaceRules: null,
                isEarlyDefault: true);
        }

        public static void ApplyLoggingPolicyFromAsset(LoggingConfigAsset config, string source)
        {
            if (config == null)
            {
                LogRuntimeModeObs("[OBS][BOOT] LoggingPolicyApplySkipped reason='null_logging_config_asset'");
                return;
            }

            List<NamespaceRuleEntry> entries = BuildNamespaceRules(config.Rules);
            ApplyLoggingPolicyInternal(
                globalDebugEnabled: config.GlobalEnabled,
                verboseEnabled: config.VerboseEnabled,
                fallbacksEnabled: config.FallbacksEnabled,
                repeatedVerboseEnabled: config.RepeatedVerboseEnabled,
                defaultLevel: config.DefaultLevel,
                source: source,
                namespaceRules: entries,
                isEarlyDefault: false);
        }

        public static void ApplyLoggingPolicyFromBootstrap(
            DebugLevel defaultLevel,
            bool verboseEnabled,
            bool fallbacksEnabled,
            bool globalDebugEnabled = true,
            bool repeatedVerboseEnabled = true,
            string source = "BootstrapFallbackHardcoded")
        {
            ApplyLoggingPolicyInternal(
                globalDebugEnabled,
                verboseEnabled,
                fallbacksEnabled,
                repeatedVerboseEnabled,
                defaultLevel,
                source,
                namespaceRules: null,
                isEarlyDefault: false);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static async void Dev_ForceReapplyLastLoggingPolicyForEvidence()
        {
            if (!_hasAppliedPolicy)
            {
                LogRuntimeModeObs("[OBS][RuntimeMode] LoggingPolicyEvidenceSkipped reason='no_last_policy'");
                return;
            }

            ApplyLoggingPolicyInternal(
                _lastAppliedGlobalDebugEnabled,
                _lastAppliedVerboseEnabled,
                _lastAppliedFallbacksEnabled,
                _lastAppliedRepeatedVerboseEnabled,
                _lastAppliedDefaultLevel,
                _lastAppliedSource,
                new List<NamespaceRuleEntry>(_activeNamespaceRules),
                _lastAppliedEarlyDefault);

            await Task.Yield();

            ApplyLoggingPolicyInternal(
                _lastAppliedGlobalDebugEnabled,
                _lastAppliedVerboseEnabled,
                _lastAppliedFallbacksEnabled,
                _lastAppliedRepeatedVerboseEnabled,
                _lastAppliedDefaultLevel,
                _lastAppliedSource,
                new List<NamespaceRuleEntry>(_activeNamespaceRules),
                _lastAppliedEarlyDefault);
        }
#endif
        #endregion

        #region Log estatico por Type
        public static void Log(Type type, string message, string color = null, Object context = null)
        {
            if (!ShouldLog(type, null, DebugLevel.Logs))
            {
                return;
            }

            Debug.Log(ApplyColor(BuildLogMessage("INFO", type, message), color), context);
        }

        public static void LogWarning(Type type, string message, Object context = null)
        {
            if (!ShouldLog(type, null, DebugLevel.Warning))
            {
                return;
            }

            Debug.LogWarning(BuildLogMessage("WARNING", type, message), context);
        }

        public static void LogError(Type type, string message, Object context = null)
        {
            if (!ShouldLog(type, null, DebugLevel.Error))
            {
                return;
            }

            Debug.LogError(BuildLogMessage("ERROR", type, message), context);
        }

        public static void LogVerbose(Type type, string message, string color = null, Object context = null, bool isFallback = false, bool deduplicate = false)
        {
            lock (_verboseLogLock)
            {
                if (!_verboseLoggingEnabled || _disabledVerboseTypes.Contains(type) || (isFallback && !_logFallbacks) || !ShouldLog(type, null, DebugLevel.Verbose))
                {
                    return;
                }

                if (!TrackCall(type, message, context, deduplicate))
                {
                    return;
                }

                Debug.Log(ApplyColor(GetPooledMessage(type, message, isFallback), color), context);
            }
        }
        #endregion

        #region Log generico por tipo (T)
        public static void Log<T>(string message, string color = null, Object context = null, T instance = null) where T : class
        {
            var type = typeof(T);
            if (!ShouldLog(type, instance, DebugLevel.Logs))
            {
                return;
            }

            Debug.Log(ApplyColor(BuildLogMessage("INFO", type, message), color), context);
        }

        public static void LogWarning<T>(string message, Object context = null, T instance = null) where T : class
        {
            var type = typeof(T);
            if (!ShouldLog(type, instance, DebugLevel.Warning))
            {
                return;
            }

            Debug.LogWarning(BuildLogMessage("WARNING", type, message), context);
        }

        public static void LogError<T>(string message, Object context = null, T instance = null) where T : class
        {
            var type = typeof(T);
            if (!ShouldLog(type, instance, DebugLevel.Error))
            {
                return;
            }

            Debug.LogError(BuildLogMessage("ERROR", type, message), context);
        }

        public static void LogVerbose<T>(string message, string color = null, Object context = null, T instance = null, bool isFallback = false, bool deduplicate = false) where T : class
        {
            var type = typeof(T);
            lock (_verboseLogLock)
            {
                if (!_verboseLoggingEnabled || _disabledVerboseTypes.Contains(type) || (isFallback && !_logFallbacks) || !ShouldLog(type, instance, DebugLevel.Verbose))
                {
                    return;
                }

                if (!TrackCall(type, message, context, deduplicate))
                {
                    return;
                }

                Debug.Log(ApplyColor(GetPooledMessage(type, message, isFallback), color), context);
            }
        }
        #endregion

        #region Helpers internos
        private static bool ShouldLog(Type type, object instance, DebugLevel messageLevel)
        {
            if (!_globalDebugEnabled)
            {
                return false;
            }

            // Precedencia 1: override local por instancia.
            if (instance != null && _localLevels.TryGetValue(instance, out var localLevel))
            {
                return (int)localLevel >= (int)messageLevel;
            }

            if (type == null)
            {
                return (int)_defaultDebugLevel >= (int)messageLevel;
            }

            // Precedencia 2: override runtime por tipo.
            if (_scriptDebugLevels.TryGetValue(type, out var scriptLevel))
            {
                return (int)scriptLevel >= (int)messageLevel;
            }

            if (_effectiveLevels.TryGetValue(type, out var cachedEffectiveLevel))
            {
                return (int)cachedEffectiveLevel >= (int)messageLevel;
            }

            DebugLevel effectiveLevel = ResolveEffectiveLevel(type);
            _effectiveLevels[type] = effectiveLevel;
            return (int)effectiveLevel >= (int)messageLevel;
        }

        private static DebugLevel ResolveEffectiveLevel(Type type)
        {
            // Precedencia 3: regra de namespace (StartsWith + longest-prefix).
            if (TryGetNamespaceRuleMatch(type, out var ruleMatch))
            {
                return ruleMatch.Level;
            }

            // Precedencia 4 e 5: atributo e default.
            if (_attributeLevels.TryGetValue(type, out var attributeLevel))
            {
                return attributeLevel;
            }

            attributeLevel = Attribute.GetCustomAttribute(type, typeof(DebugLevelAttribute)) is DebugLevelAttribute attr
                ? attr.Level
                : _defaultDebugLevel;

            _attributeLevels[type] = attributeLevel;
            return attributeLevel;
        }

        private static bool TryGetNamespaceRuleMatch(Type type, out NamespaceRuleMatch match)
        {
            if (_matchedNamespaceRules.TryGetValue(type, out match))
            {
                return match.HasMatch;
            }

            string typeNamespace = type?.Namespace;
            if (string.IsNullOrWhiteSpace(typeNamespace))
            {
                match = new NamespaceRuleMatch(false, string.Empty, string.Empty, _defaultDebugLevel);
                _matchedNamespaceRules[type] = match;
                return false;
            }

            for (int i = 0; i < _activeNamespaceRules.Count; i++)
            {
                NamespaceRuleEntry entry = _activeNamespaceRules[i];
                if (typeNamespace.StartsWith(entry.NamespacePrefix, StringComparison.Ordinal))
                {
                    match = new NamespaceRuleMatch(true, entry.RuleId, entry.NamespacePrefix, entry.Level);
                    _matchedNamespaceRules[type] = match;
                    return true;
                }
            }

            match = new NamespaceRuleMatch(false, string.Empty, string.Empty, _defaultDebugLevel);
            _matchedNamespaceRules[type] = match;
            return false;
        }

        private static List<NamespaceRuleEntry> BuildNamespaceRules(IReadOnlyList<LoggingConfigAsset.NamespaceRule> rules)
        {
            var entries = new List<NamespaceRuleEntry>();
            if (rules == null)
            {
                return entries;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                LoggingConfigAsset.NamespaceRule rule = rules[i];
                if (rule == null || !rule.enabled)
                {
                    continue;
                }

                string prefix = (rule.namespacePrefix ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }

                string ruleId = string.IsNullOrWhiteSpace(rule.ruleId) ? $"rule_{i}" : rule.ruleId.Trim();
                entries.Add(new NamespaceRuleEntry(ruleId, prefix, rule.level));
            }

            entries.Sort(static (a, b) =>
            {
                int prefixLengthCompare = b.NamespacePrefix.Length.CompareTo(a.NamespacePrefix.Length);
                if (prefixLengthCompare != 0)
                {
                    return prefixLengthCompare;
                }

                return string.Compare(a.RuleId, b.RuleId, StringComparison.Ordinal);
            });

            return entries;
        }

        private static string BuildRuleSignature(IReadOnlyList<NamespaceRuleEntry> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                return "no_rules";
            }

            var builder = new StringBuilder(128);
            for (int i = 0; i < rules.Count; i++)
            {
                NamespaceRuleEntry rule = rules[i];
                if (i > 0)
                {
                    builder.Append(',');
                }

                builder.Append(rule.RuleId)
                    .Append('@')
                    .Append(rule.NamespacePrefix)
                    .Append('=')
                    .Append(rule.Level);
            }

            return builder.ToString();
        }

        private static void ApplyLoggingPolicyInternal(
            bool globalDebugEnabled,
            bool verboseEnabled,
            bool fallbacksEnabled,
            bool repeatedVerboseEnabled,
            DebugLevel defaultLevel,
            string source,
            IReadOnlyList<NamespaceRuleEntry> namespaceRules,
            bool isEarlyDefault)
        {
            string rulesSignature = BuildRuleSignature(namespaceRules);
            string policyKey = BuildLoggingPolicyKey(
                GetBuildVariant(),
                GetNewscriptsModeState(),
                globalDebugEnabled,
                verboseEnabled,
                defaultLevel,
                repeatedVerboseEnabled,
                fallbacksEnabled,
                source,
                isEarlyDefault,
                rulesSignature);
            int policyFrame = GetPolicyFrame();

            if (policyFrame == _lastPolicyFrame && string.Equals(policyKey, _lastPolicyKey, StringComparison.Ordinal))
            {
                LogRuntimeModeObs($"[OBS][BOOT] LoggingPolicyApplySkipped reason='dedupe_same_frame' key='{policyKey}'");
                return;
            }

            if (string.Equals(policyKey, _lastPolicyKey, StringComparison.Ordinal))
            {
                LogRuntimeModeObs($"[OBS][BOOT] LoggingPolicyApplySkipped reason='dedupe_same_key' key='{policyKey}'");
                return;
            }

            _globalDebugEnabled = globalDebugEnabled;
            _verboseLoggingEnabled = verboseEnabled;
            _logFallbacks = fallbacksEnabled;
            _repeatedCallVerboseEnabled = repeatedVerboseEnabled;
            _defaultDebugLevel = defaultLevel;

            _activeNamespaceRules.Clear();
            if (namespaceRules != null)
            {
                for (int i = 0; i < namespaceRules.Count; i++)
                {
                    _activeNamespaceRules.Add(namespaceRules[i]);
                }
            }

            InvalidateResolvedCaches("policy_reapplied");

            _lastPolicyKey = policyKey;
            _lastPolicyFrame = policyFrame;
            _hasAppliedPolicy = true;
            _lastAppliedGlobalDebugEnabled = globalDebugEnabled;
            _lastAppliedVerboseEnabled = verboseEnabled;
            _lastAppliedFallbacksEnabled = fallbacksEnabled;
            _lastAppliedRepeatedVerboseEnabled = repeatedVerboseEnabled;
            _lastAppliedDefaultLevel = defaultLevel;
            _lastAppliedSource = source;
            _lastAppliedEarlyDefault = isEarlyDefault;

            string phase = isEarlyDefault ? "BOOT" : "STARTUP";
            string policyFlavor = isEarlyDefault ? "EarlyDefault" : "BootstrapConfigAsset";
            LogRuntimeModeObs(
                $"[OBS][{phase}] LoggingPolicyApplied source='{source}' policy='{policyFlavor}' " +
                $"defaultLevel='{defaultLevel}' activeRuleCount={_activeNamespaceRules.Count} " +
                $"global={globalDebugEnabled} verbose={verboseEnabled} fallbacks={fallbacksEnabled} repeatedVerbose={repeatedVerboseEnabled}");
        }

        private static void InvalidateResolvedCaches(string reason)
        {
            int effectiveCount = _effectiveLevels.Count;
            int ruleMatchCount = _matchedNamespaceRules.Count;
            int attributeCount = _attributeLevels.Count;

            _effectiveLevels.Clear();
            _matchedNamespaceRules.Clear();
            _attributeLevels.Clear();

            LogRuntimeModeObs(
                $"[OBS][BOOT] LoggingPolicyCacheInvalidated reason='{reason}' " +
                $"effectiveTypeCount={effectiveCount} matchedRuleCount={ruleMatchCount} attributeCount={attributeCount}");
        }

        // policyKey e um contrato estavel/deterministico usado para dedupe e evidencia.
        private static string BuildLoggingPolicyKey(
            string buildVariant,
            string newscriptsMode,
            bool globalDebugEnabled,
            bool verboseEnabled,
            DebugLevel defaultLevel,
            bool repeatedVerboseEnabled,
            bool fallbacksEnabled,
            string source,
            bool isEarlyDefault,
            string rulesSignature)
        {
            var builder = new StringBuilder(160);
            builder.Append(buildVariant)
                .Append('|').Append(newscriptsMode)
                .Append("|global=").Append(globalDebugEnabled)
                .Append(";verbose=").Append(verboseEnabled)
                .Append(";default=").Append(defaultLevel)
                .Append(";repeated=").Append(repeatedVerboseEnabled)
                .Append(";fallbacks=").Append(fallbacksEnabled)
                .Append(";early=").Append(isEarlyDefault)
                .Append(";source=").Append(source)
                .Append("|rules=").Append(rulesSignature);
            return builder.ToString();
        }

        private static string BuildLogMessage(string level, Type type, string message)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append('[').Append(level).Append("] [").Append(type?.Name ?? nameof(DebugUtility)).Append("] ").Append(message);
            return _stringBuilder.ToString();
        }

        private static string GetPooledMessage(Type type, string message, bool isFallback)
        {
            string typeName = type?.Name ?? nameof(DebugUtility);
            string key = $"{typeName}:{message}:{isFallback}";
            if (!_messagePool.TryGetValue(key, out string baseMessage))
            {
                _stringBuilder.Clear();
                _stringBuilder.Append("[VERBOSE] [").Append(typeName).Append("] ").Append(message);
                if (isFallback)
                {
                    _stringBuilder.Append(" (fallback)");
                }

                baseMessage = _stringBuilder.ToString();
                _messagePool[key] = baseMessage;
            }

            _stringBuilder.Clear();
            _stringBuilder.Append(baseMessage)
                .Append(" (@ ").Append(GetTimestampSuffix()).Append("s)");
            return _stringBuilder.ToString();
        }

        private static string ApplyColor(string message, string color)
        {
            return string.IsNullOrEmpty(color) ? message : $"<color={color}>{message}</color>";
        }

        private static int GetPolicyFrame()
        {
            if (IsMainThread())
            {
                int frame = Time.frameCount;
                _lastTrackedFrame = frame;
                return frame;
            }

            return _lastTrackedFrame >= 0 ? _lastTrackedFrame : -1;
        }

        private static bool IsMainThread()
        {
            return _mainThreadId > 0 && Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }

        private static string GetTimestampSuffix()
        {
            if (IsMainThread())
            {
                return Time.realtimeSinceStartup.ToString("F2", CultureInfo.InvariantCulture);
            }

            return DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        private static string GetBuildVariant()
        {
#if UNITY_EDITOR
#if DEVELOPMENT_BUILD
            return "DevBuild";
#else
            return "Editor";
#endif
#else
            return "Release";
#endif
        }

        private static string GetNewscriptsModeState()
        {
#if NEWSCRIPTS_MODE
            return "Enabled";
#else
            return "Disabled";
#endif
        }

        private static void LogRuntimeModeObs(string message)
        {
            Debug.Log(ApplyColor(BuildLogMessage("INFO", typeof(DebugUtility), message), Colors.Info));
        }

        private static void LogInternal(string message, Object context = null)
        {
            if (!ShouldLog(null, null, DebugLevel.Logs))
            {
                return;
            }

            Debug.Log(ApplyColor(BuildLogMessage("INFO", typeof(DebugUtility), message), null), context);
        }

        private static bool TrackCall(Type type, string message, Object context, bool deduplicate)
        {
            int frame = GetPolicyFrame();

            // Limpamos 1x por frame (mais barato do que RemoveWhere em toda chamada).
            if (_lastTrackedFrame != frame)
            {
                _callTracker.Clear();
                _repeatedCallTracker.Clear();
                _lastTrackedFrame = frame;
            }

            string typeName = type?.Name ?? nameof(DebugUtility);
            int contextId = context != null ? context.GetInstanceID() : 0;
            string key = $"{typeName}:{message}:ctx={contextId}";
            var trackerKey = (key, frame);

            bool isRepeat = _callTracker.Contains(trackerKey);
            if (isRepeat)
            {
                if (!deduplicate && _repeatedCallVerboseEnabled)
                {
                    if (_repeatedCallTracker.Add(trackerKey) && !ShouldSuppressRepeatedCallWarning(type, message))
                    {
                        LogRepeatedCallVerbose(type, message, frame);
                    }
                }

                return !deduplicate;
            }

            _callTracker.Add(trackerKey);
            return true;
        }

        private static void LogRepeatedCallVerbose(Type type, string message, int frame)
        {
            if (!_verboseLoggingEnabled || !_repeatedCallVerboseEnabled)
            {
                return;
            }

            if (type != null && _disabledVerboseTypes.Contains(type))
            {
                return;
            }

            if (!ShouldLog(type, null, DebugLevel.Verbose))
            {
                return;
            }

            _stringBuilder.Clear();
            _stringBuilder.Append("[DebugUtility] ")
                .Append(AlertIcon)
                .Append(" Chamada repetida no frame ")
                .Append(frame)
                .Append(": [")
                .Append(type?.Name ?? nameof(DebugUtility))
                .Append("] ")
                .Append(message);

            Debug.Log(ApplyColor(_stringBuilder.ToString(), RepeatedCallColor));
        }

        private static bool ShouldSuppressRepeatedCallWarning(Type type, string message)
        {
            string typeName = type?.Name ?? string.Empty;
            if (!string.Equals(typeName, "GameNavigationCatalogAsset", StringComparison.Ordinal))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            // Suprime apenas spam de observabilidade idempotente do catalogo de navegacao.
            return message.Contains("[OBS][SceneFlow] RouteResolvedVia=AssetRef", StringComparison.Ordinal) ||
                message.Contains("[OBS][Config] RouteResolvedVia=AssetRef", StringComparison.Ordinal);
        }
        #endregion
    }
}

