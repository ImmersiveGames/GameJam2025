using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class DegradedModeReporter : IDegradedModeReporter
    {
        private readonly Dictionary<string, Entry> _entries = new();

        private readonly IRuntimeModeProvider _runtimeModeProvider;

        private float _lastSummaryTime;
        private bool _droppedKeysWarned;
        private int _droppedKeyReports;
        private bool _configSourceLogged;

        public DegradedModeReporter()
            : this(new UnityRuntimeModeProvider(), null)
        {
        }

        public DegradedModeReporter(IRuntimeModeProvider runtimeModeProvider)
            : this(runtimeModeProvider, null)
        {
        }

        public DegradedModeReporter(IRuntimeModeProvider runtimeModeProvider, RuntimeModeConfig config)
        {
            _runtimeModeProvider = runtimeModeProvider ?? new UnityRuntimeModeProvider();
            _ = config;
            _lastSummaryTime = Time.realtimeSinceStartup;
        }

        public void Report(string feature, string reason, string detail = null, string signature = null, string profile = null)
        {
            feature = Sanitize(feature);
            reason = Sanitize(reason);
            detail = Sanitize(detail);
            signature = Sanitize(signature);
            profile = Sanitize(profile);

            string baseMsg =
                $"DEGRADED_MODE feature='{feature}' reason='{reason}'" +
                (string.IsNullOrWhiteSpace(detail) ? string.Empty : $" detail='{detail}'") +
                (string.IsNullOrWhiteSpace(signature) ? string.Empty : $" signature='{signature}'") +
                (string.IsNullOrWhiteSpace(profile) ? string.Empty : $" profile='{profile}'");

            var settings = ResolveRuntimePolicySettingsOrFail();

            float now = Time.realtimeSinceStartup;

            if (!_entries.TryGetValue(baseMsg, out var entry))
            {
                if (_entries.Count >= settings.ReporterMaxUniqueKeys)
                {
                    _droppedKeyReports++;
                    WarnDroppedKeysOnce(settings);
                    MaybeEmitSummary(now, settings);
                    return;
                }

                entry = new Entry(feature, reason);
                _entries.Add(baseMsg, entry);
            }

            entry.count++;

            bool shouldLog = ShouldLogNow(entry, now, settings);
            if (shouldLog)
            {
                string msg = baseMsg;
                if (settings.ReporterIncludeCountInLog)
                {
                    msg += $" count={entry.count}";
                }

                LogWithSeverity(msg, settings);

                if (ShouldThrowInStrict(settings))
                {
                    throw new InvalidOperationException(msg);
                }

                entry.lastLogTime = now;
                entry.loggedOnce = true;
            }

            MaybeEmitSummary(now, settings);
        }

        private void WarnDroppedKeysOnce(EffectiveRuntimePolicySettings settings)
        {
            if (_droppedKeysWarned)
            {
                return;
            }

            _droppedKeysWarned = true;

            string msg =
                $"DEGRADED_MODE feature='{DegradedKeys.Feature.Infrastructure}' reason='{DegradedKeys.Reason.CapacityLimit}' " +
                $"detail='MaxUniqueKeys atingido ({settings.ReporterMaxUniqueKeys}). Novas chaves nao serao rastreadas.'";

            DebugUtility.LogWarning<DegradedModeReporter>(msg);
        }

        private bool ShouldLogNow(Entry entry, float now, EffectiveRuntimePolicySettings settings)
        {
            switch (settings.ReporterDedupStrategy)
            {
                case DegradedDedupStrategy.PerSession:
                    return settings.ReporterLogFirstOccurrence && !entry.loggedOnce;

                case DegradedDedupStrategy.CooldownSeconds:
                default:
                    if (settings.ReporterCooldownSeconds <= 0f)
                    {
                        return settings.ReporterLogFirstOccurrence || entry.count > 1;
                    }

                    if (!entry.loggedOnce)
                    {
                        return settings.ReporterLogFirstOccurrence;
                    }

                    return now - entry.lastLogTime >= settings.ReporterCooldownSeconds;
            }
        }

        private void MaybeEmitSummary(float now, EffectiveRuntimePolicySettings settings)
        {
            float interval = settings.ReporterEmitSummaryEverySeconds;
            if (interval <= 0f)
            {
                return;
            }

            if (now - _lastSummaryTime < interval)
            {
                return;
            }

            _lastSummaryTime = now;

            int total = _droppedKeyReports;
            foreach (KeyValuePair<string, Entry> kv in _entries)
            {
                total += kv.Value.count;
            }

            const int topN = 5;
            int[] topCounts = new int[topN];
            string[] topLabels = new string[topN];

            foreach (KeyValuePair<string, Entry> kv in _entries)
            {
                var e = kv.Value;
                int c = e.count;
                for (int i = 0; i < topN; i++)
                {
                    if (c <= topCounts[i])
                    {
                        continue;
                    }

                    for (int j = topN - 1; j > i; j--)
                    {
                        topCounts[j] = topCounts[j - 1];
                        topLabels[j] = topLabels[j - 1];
                    }

                    topCounts[i] = c;
                    topLabels[i] = $"{e.feature}/{e.reason}={c}";
                    break;
                }
            }

            string topText = string.Empty;
            for (int i = 0; i < topN; i++)
            {
                if (topCounts[i] <= 0 || string.IsNullOrWhiteSpace(topLabels[i]))
                {
                    continue;
                }

                topText += (topText.Length == 0 ? string.Empty : "; ") + topLabels[i];
            }

            string summary =
                $"DEGRADED_SUMMARY uniqueKeys={_entries.Count} totalReports={total}" +
                (_droppedKeyReports > 0 ? $" droppedReports={_droppedKeyReports}" : string.Empty) +
                (string.IsNullOrWhiteSpace(topText) ? string.Empty : $" top=[{topText}]");

            DebugUtility.LogVerbose<DegradedModeReporter>(summary, DebugUtility.Colors.Info);
        }

        private void LogWithSeverity(string msg, EffectiveRuntimePolicySettings settings)
        {
            if (_runtimeModeProvider is { IsStrict: true } && settings.StrictnessDegradedAsError)
            {
                DebugUtility.LogError<DegradedModeReporter>(msg);
                return;
            }

            DebugUtility.LogWarning<DegradedModeReporter>(msg);
        }

        private bool ShouldThrowInStrict(EffectiveRuntimePolicySettings settings)
        {
            if (_runtimeModeProvider == null)
            {
                return false;
            }

            return _runtimeModeProvider.IsStrict && settings.StrictnessDegradedAsException;
        }

        private EffectiveRuntimePolicySettings ResolveRuntimePolicySettingsOrFail()
        {
            if (RuntimeConfigRegistry.TryGetSnapshot(out var snapshot) && snapshot != null)
            {
                var runtimePolicy = snapshot.RuntimePolicy;
                if (runtimePolicy == null)
                {
                    throw new InvalidOperationException("[FATAL][RuntimeMode][Degraded] RuntimeConfigRegistry invariant breach: snapshot.RuntimePolicy obrigatorio ausente.");
                }

                var settings = EffectiveRuntimePolicySettings.FromRegistry(runtimePolicy);
                if (!settings.IsValid)
                {
                    throw new InvalidOperationException("[FATAL][RuntimeMode][Degraded] RuntimeConfigRegistry invariant breach: RuntimePolicy reporter/strictness invalidos no snapshot.");
                }

                LogConfigSourceOnce("registry");
                return settings;
            }

            throw new InvalidOperationException("[FATAL][RuntimeMode][Degraded] RuntimeConfigRegistry snapshot obrigatorio ausente para RuntimePolicy (reporter/strictness) migrado.");
        }

        private void LogConfigSourceOnce(string source)
        {
            if (_configSourceLogged)
            {
                return;
            }

            _configSourceLogged = true;

            if (string.Equals(source, "registry", StringComparison.Ordinal))
            {
                DebugUtility.Log(typeof(DegradedModeReporter),
                    "[OBS][RuntimePolicy][Config] DegradedModeReporter using RuntimeConfigRegistry snapshot for reporter/strictness.",
                    DebugUtility.Colors.Info);
            }
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Replace("'", "''").Trim();
        }

        private sealed class Entry
        {
            public readonly string feature;
            public readonly string reason;

            public int count;
            public float lastLogTime;
            public bool loggedOnce;

            public Entry(string feature, string reason)
            {
                this.feature = string.IsNullOrWhiteSpace(feature) ? DegradedKeys.Feature.Infrastructure : feature;
                this.reason = string.IsNullOrWhiteSpace(reason) ? DegradedKeys.Reason.Unknown : reason;
                count = 0;
                lastLogTime = 0f;
                loggedOnce = false;
            }
        }

        private readonly struct EffectiveRuntimePolicySettings
        {
            private EffectiveRuntimePolicySettings(
                bool hasRuntimePolicy,
                DegradedDedupStrategy reporterDedupStrategy,
                float reporterCooldownSeconds,
                float reporterEmitSummaryEverySeconds,
                int reporterMaxUniqueKeys,
                bool reporterLogFirstOccurrence,
                bool reporterIncludeCountInLog,
                bool strictnessDegradedAsError,
                bool strictnessDegradedAsException)
            {
                HasRuntimePolicy = hasRuntimePolicy;
                ReporterDedupStrategy = reporterDedupStrategy;
                ReporterCooldownSeconds = reporterCooldownSeconds;
                ReporterEmitSummaryEverySeconds = reporterEmitSummaryEverySeconds;
                ReporterMaxUniqueKeys = reporterMaxUniqueKeys;
                ReporterLogFirstOccurrence = reporterLogFirstOccurrence;
                ReporterIncludeCountInLog = reporterIncludeCountInLog;
                StrictnessDegradedAsError = strictnessDegradedAsError;
                StrictnessDegradedAsException = strictnessDegradedAsException;
            }

            public bool HasRuntimePolicy { get; }
            public DegradedDedupStrategy ReporterDedupStrategy { get; }
            public float ReporterCooldownSeconds { get; }
            public float ReporterEmitSummaryEverySeconds { get; }
            public int ReporterMaxUniqueKeys { get; }
            public bool ReporterLogFirstOccurrence { get; }
            public bool ReporterIncludeCountInLog { get; }
            public bool StrictnessDegradedAsError { get; }
            public bool StrictnessDegradedAsException { get; }

            public bool IsValid => ReporterMaxUniqueKeys > 0;

            public static EffectiveRuntimePolicySettings FromRegistry(IRuntimePolicyConfigGroupReadOnly source)
            {
                return new EffectiveRuntimePolicySettings(
                    hasRuntimePolicy: true,
                    reporterDedupStrategy: source.ReporterDedupStrategy,
                    reporterCooldownSeconds: source.ReporterCooldownSeconds,
                    reporterEmitSummaryEverySeconds: source.ReporterEmitSummaryEverySeconds,
                    reporterMaxUniqueKeys: source.ReporterMaxUniqueKeys,
                    reporterLogFirstOccurrence: source.ReporterLogFirstOccurrence,
                    reporterIncludeCountInLog: source.ReporterIncludeCountInLog,
                    strictnessDegradedAsError: source.StrictnessDegradedAsError,
                    strictnessDegradedAsException: source.StrictnessDegradedAsException);
            }

        }
    }
}
