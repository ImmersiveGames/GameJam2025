using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
namespace _ImmersiveGames.Scripts.Utils.DebugSystems
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
        private static bool _globalDebugEnabled = true;
        private static bool _verboseLoggingEnabled = true;
        private static bool _logFallbacks = true;
        private static bool _repeatedCallVerboseEnabled = true;
        private static DebugLevel _defaultDebugLevel = DebugLevel.Logs;
        private static readonly Dictionary<Type, DebugLevel> _scriptDebugLevels = new();
        private static readonly Dictionary<object, DebugLevel> _localLevels = new();
        private static readonly Dictionary<Type, DebugLevel> _attributeLevels = new();
        private static readonly HashSet<Type> _disabledVerboseTypes = new();
        private static readonly HashSet<(string key, int frame)> _callTracker = new();
        private static readonly StringBuilder _stringBuilder = new(256);
        private static readonly Dictionary<string, string> _messagePool = new();
        private const string RepeatedCallColor = "#FFD54F";
        private const string AlertIcon = "⚠️";

        public static class Colors
        {
            public const string CrucialInfo = "#00BCD4";
            public const string Info = "#A8DEED";
            public const string Success = "#4CAF50";
            public const string Warning ="yellow";
            public const string Error = "red";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Initialize()
        {
#if NEWSCRIPTS_MODE
            Debug.Log("NEWSCRIPTS_MODE ativo: DebugUtility.Initialize ignorado.");
            return;
#endif
            _globalDebugEnabled = true;
            _verboseLoggingEnabled = Application.isEditor;
            _logFallbacks = Application.isEditor;
            _repeatedCallVerboseEnabled = true;
            _defaultDebugLevel = DebugLevel.Logs;
            _scriptDebugLevels.Clear();
            _localLevels.Clear();
            _attributeLevels.Clear();
            _disabledVerboseTypes.Clear();
            _callTracker.Clear();
            _messagePool.Clear();

            LogInternal("DebugUtility inicializado antes de todos os sistemas.");
        }

        #region Configurações
        public static void SetGlobalDebugState(bool enabled) => _globalDebugEnabled = enabled;
        public static void SetVerboseLogging(bool enabled) => _verboseLoggingEnabled = enabled;
        public static void SetLogFallbacks(bool enabled) => _logFallbacks = enabled;
        public static void SetRepeatedCallVerbose(bool enabled) => _repeatedCallVerboseEnabled = enabled;
        public static bool GetRepeatedCallVerbose() => _repeatedCallVerboseEnabled;
        public static void DisableVerboseForType(Type type) => _disabledVerboseTypes.Add(type);
        public static void EnableVerboseForType(Type type) => _disabledVerboseTypes.Remove(type);
        public static void SetDefaultDebugLevel(DebugLevel level) => _defaultDebugLevel = level;
        public static void RegisterScriptDebugLevel(Type type, DebugLevel level) => _scriptDebugLevels[type] = level;
        public static void SetLocalDebugLevel(object instance, DebugLevel level) => _localLevels[instance] = level;
        #endregion

        #region Log estático por Type
        public static void Log(Type type, string message, string color = null, Object context = null)
        {
            if (!ShouldLog(type, null, DebugLevel.Logs)) return;
            Debug.Log(ApplyColor(BuildLogMessage("INFO", type, message), color), context);
        }

        public static void LogWarning(Type type, string message, Object context = null)
        {
            if (!ShouldLog(type, null, DebugLevel.Warning)) return;
            Debug.LogWarning(BuildLogMessage("WARNING", type, message), context);
        }

        public static void LogError(Type type, string message, Object context = null)
        {
            if (!ShouldLog(type, null, DebugLevel.Error)) return;
            Debug.LogError(BuildLogMessage("ERROR", type, message), context);
        }

        public static void LogVerbose(Type type, string message, string color = null, Object context = null, bool isFallback = false, bool deduplicate = false)
        {
            if (!_verboseLoggingEnabled || _disabledVerboseTypes.Contains(type) || (isFallback && !_logFallbacks) || !ShouldLog(type, null, DebugLevel.Verbose)) return;

            if (!TrackCall(type, message, deduplicate)) return;
            Debug.Log(ApplyColor(GetPooledMessage(type, message, isFallback), color), context);
        }
        #endregion

        #region Log genérico por tipo (T)
        public static void Log<T>(string message, string color = null, Object context = null, T instance = null) where T : class
        {
            var type = typeof(T);
            if (!ShouldLog(type, instance, DebugLevel.Logs)) return;
            Debug.Log(ApplyColor(BuildLogMessage("INFO", type, message), color), context);
        }

        public static void LogWarning<T>(string message, Object context = null, T instance = null) where T : class
        {
            var type = typeof(T);
            if (!ShouldLog(type, instance, DebugLevel.Warning)) return;
            Debug.LogWarning(BuildLogMessage("WARNING", type, message), context);
        }

        public static void LogError<T>(string message, Object context = null, T instance = null) where T : class
        {
            var type = typeof(T);
            if (!ShouldLog(type, instance, DebugLevel.Error)) return;
            Debug.LogError(BuildLogMessage("ERROR", type, message), context);
        }

        public static void LogVerbose<T>(string message, string color = null, Object context = null, T instance = null, bool isFallback = false, bool deduplicate = false) where T : class
        {
            var type = typeof(T);
            if (!_verboseLoggingEnabled || _disabledVerboseTypes.Contains(type) || (isFallback && !_logFallbacks) || !ShouldLog(type, instance, DebugLevel.Verbose)) return;

            if (!TrackCall(type, message, deduplicate)) return;
            Debug.Log(ApplyColor(GetPooledMessage(type, message, isFallback), color), context);
        }
        #endregion

        #region Helpers Internos
        private static bool ShouldLog(Type type, object instance, DebugLevel messageLevel)
        {
            if (!_globalDebugEnabled) return false;

            if (instance != null && _localLevels.TryGetValue(instance, out var localLevel))
                return (int)localLevel >= (int)messageLevel;

            if (type == null) return (int)_defaultDebugLevel >= (int)messageLevel;

            if (_scriptDebugLevels.TryGetValue(type, out var scriptLevel))
                return (int)scriptLevel >= (int)messageLevel;

            if (_attributeLevels.TryGetValue(type, out var attributeLevel))
                return (int)attributeLevel >= (int)messageLevel;

            attributeLevel = Attribute.GetCustomAttribute(type, typeof(DebugLevelAttribute)) is DebugLevelAttribute attr
                ? attr.Level
                : _defaultDebugLevel;

            _attributeLevels[type] = attributeLevel;
            return (int)attributeLevel >= (int)messageLevel;
        }

        private static string BuildLogMessage(string level, Type type, string message)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append('[').Append(level).Append("] [").Append(type.Name).Append("] ").Append(message);
            return _stringBuilder.ToString();
        }

        private static string GetPooledMessage(Type type, string message, bool isFallback)
        {
            string key = $"{type.Name}:{message}:{isFallback}";
            if (!_messagePool.TryGetValue(key, out string baseMessage))
            {
                _stringBuilder.Clear();
                _stringBuilder.Append("[VERBOSE] [").Append(type.Name).Append("] ").Append(message);
                if (isFallback)
                    _stringBuilder.Append(" (fallback)");
                baseMessage = _stringBuilder.ToString();
                _messagePool[key] = baseMessage;
            }

            _stringBuilder.Clear();
            _stringBuilder.Append(baseMessage)
                          .Append(" (@ ").Append(Time.realtimeSinceStartup.ToString("F2")).Append("s)");
            return _stringBuilder.ToString();
        }

        private static string ApplyColor(string message, string color)
        {
            return string.IsNullOrEmpty(color) ? message : $"<color={color}>{message}</color>";
        }

        private static void LogInternal(string message, Object context = null)
        {
            if (!ShouldLog(null, null, DebugLevel.Logs)) return;
            Debug.Log(ApplyColor(BuildLogMessage("INFO", typeof(DebugUtility), message), null), context);
        }

        private static bool TrackCall(Type type, string message, bool deduplicate)
        {
            string key = $"{type.Name}:{message}";
            int frame = Time.frameCount;
            var trackerKey = (key, frame);

            bool isRepeat = _callTracker.Contains(trackerKey);
    
            if (isRepeat)
            {
                // ✅ REPETIÇÃO: registra como verbose colorido quando não houver deduplicação
                if (!deduplicate && _repeatedCallVerboseEnabled)
                    LogRepeatedCallVerbose(type, message, frame);
                return !deduplicate; // Se deduplicate=true, bloqueia; se false, permite novo log após registrar verbose
            }

            // ✅ PRIMEIRA VEZ: adiciona ao tracker
            _callTracker.RemoveWhere(k => k.frame < frame); // Limpa antigos
            _callTracker.Add(trackerKey);
            return true;
        }

        private static void LogRepeatedCallVerbose(Type type, string message, int frame)
        {
            // Verbose respeita as configurações globais e por tipo
            if (!_verboseLoggingEnabled || !_repeatedCallVerboseEnabled) return;
            if (type != null && _disabledVerboseTypes.Contains(type)) return;
            if (!ShouldLog(type, null, DebugLevel.Verbose)) return;

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
        #endregion
    }
}
