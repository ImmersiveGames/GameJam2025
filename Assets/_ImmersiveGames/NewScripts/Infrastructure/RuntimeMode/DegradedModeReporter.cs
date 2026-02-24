using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Reporter canônico de DEGRADED_MODE.
    ///
    /// Regras:
    /// - Se RuntimeModeConfig não estiver presente: mantém comportamento legado (dedupe por frame).
    /// - Se RuntimeModeConfig estiver presente: suporta dedupe por sessão ou por cooldown, contagem e resumo periódico.
    ///
    /// Importante:
    /// - Este serviço é "best-effort": nunca deve quebrar o jogo em Release por padrão.
    /// - Em Strict, pode elevar severidade (erro / exceção) apenas se o config habilitar.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class DegradedModeReporter : IDegradedModeReporter
    {
        // Legado: dedupe por frame para evitar spam em loops/acidentais.
        private readonly HashSet<(string key, int frame)> _frameDedupe = new();

        // Novo: tracking por sessão (key -> entry).
        private readonly Dictionary<string, Entry> _entries = new();

        private readonly IRuntimeModeProvider _runtimeModeProvider;
        private readonly RuntimeModeConfig _config;

        private float _lastSummaryTime;
        private bool _droppedKeysWarned;
        private int _droppedKeyReports;

        public DegradedModeReporter()
            : this(new UnityRuntimeModeProvider(), RuntimeModeConfigLoader.LoadOrNull())
        {
        }

        public DegradedModeReporter(IRuntimeModeProvider runtimeModeProvider)
            : this(runtimeModeProvider, RuntimeModeConfigLoader.LoadOrNull())
        {
        }

        public DegradedModeReporter(IRuntimeModeProvider runtimeModeProvider, RuntimeModeConfig config)
        {
            _runtimeModeProvider = runtimeModeProvider ?? new UnityRuntimeModeProvider();
            _config = config;
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

            // Sem config: mantém o comportamento atual (compatibilidade).
            if (_config == null)
            {
                int frame = Time.frameCount;
                var key = (baseMsg, frame);
                if (!_frameDedupe.Add(key))
                {
                    return;
                }

                DebugUtility.LogWarning<DegradedModeReporter>(baseMsg);
                return;
            }

            float now = Time.realtimeSinceStartup;
            var settings = _config.reporter;

            // Proteção contra explosão de keys.
            if (!_entries.TryGetValue(baseMsg, out var entry))
            {
                if (_entries.Count >= settings.maxUniqueKeys)
                {
                    _droppedKeyReports++;
                    WarnDroppedKeysOnce(settings);
                    MaybeEmitSummary(now);
                    return;
                }

                entry = new Entry(feature, reason);
                _entries.Add(baseMsg, entry);
            }

            entry.Count++;

            bool shouldLog = ShouldLogNow(entry, now, settings);
            if (shouldLog)
            {
                string msg = baseMsg;
                if (settings.includeCountInLog)
                {
                    msg += $" count={entry.Count}";
                }

                LogWithSeverity(msg);

                // Strict hard-fail opcional (somente se config habilitar).
                if (ShouldThrowInStrict())
                {
                    throw new InvalidOperationException(msg);
                }

                entry.LastLogTime = now;
                entry.LoggedOnce = true;
            }

            MaybeEmitSummary(now);
        }

        private void WarnDroppedKeysOnce(RuntimeModeConfig.DegradedReporterSettings settings)
        {
            if (_droppedKeysWarned)
            {
                return;
            }

            _droppedKeysWarned = true;

            string msg =
                $"DEGRADED_MODE feature='{DegradedKeys.Feature.Infrastructure}' reason='{DegradedKeys.Reason.Fallback}' " +
                $"detail='MaxUniqueKeys atingido ({settings.maxUniqueKeys}). Novas chaves não serão rastreadas.'";

            DebugUtility.LogWarning<DegradedModeReporter>(msg);
        }

        private bool ShouldLogNow(Entry entry, float now, RuntimeModeConfig.DegradedReporterSettings settings)
        {
            switch (settings.dedupStrategy)
            {
                case DegradedDedupStrategy.PerSession:
                    // Loga apenas a primeira ocorrência (se permitido).
                    return settings.logFirstOccurrence && !entry.LoggedOnce;

                case DegradedDedupStrategy.CooldownSeconds:
                default:
                    // Cooldown=0 -> sempre loga (sem dedupe).
                    if (settings.cooldownSeconds <= 0f)
                    {
                        return settings.logFirstOccurrence || entry.Count > 1;
                    }

                    if (!entry.LoggedOnce)
                    {
                        return settings.logFirstOccurrence;
                    }

                    return now - entry.LastLogTime >= settings.cooldownSeconds;
            }
        }

        private void MaybeEmitSummary(float now)
        {
            float interval = _config.reporter.emitSummaryEverySeconds;
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
            foreach (var kv in _entries)
            {
                total += kv.Value.Count;
            }

            // Top 5 por count (sem LINQ).
            const int topN = 5;
            var topCounts = new int[topN];
            var topLabels = new string[topN];

            foreach (var kv in _entries)
            {
                var e = kv.Value;
                int c = e.Count;
                // Inserção simples em ranking.
                for (int i = 0; i < topN; i++)
                {
                    if (c <= topCounts[i])
                    {
                        continue;
                    }

                    // Shift para baixo.
                    for (int j = topN - 1; j > i; j--)
                    {
                        topCounts[j] = topCounts[j - 1];
                        topLabels[j] = topLabels[j - 1];
                    }

                    topCounts[i] = c;
                    topLabels[i] = $"{e.Feature}/{e.Reason}={c}";
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
                (string.IsNullOrWhiteSpace(topText) ? string.Empty : $" top=[{topText}]" );

            DebugUtility.LogVerbose<DegradedModeReporter>(summary, DebugUtility.Colors.Info);
        }

        private void LogWithSeverity(string msg)
        {
            // Em Strict, pode elevar severidade para erro (config).
            if (_runtimeModeProvider != null && _runtimeModeProvider.IsStrict && _config.strictness.degradedAsError)
            {
                DebugUtility.LogError<DegradedModeReporter>(msg);
                return;
            }

            DebugUtility.LogWarning<DegradedModeReporter>(msg);
        }

        private bool ShouldThrowInStrict()
        {
            if (_runtimeModeProvider == null)
            {
                return false;
            }

            return _runtimeModeProvider.IsStrict && _config.strictness.degradedAsException;
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            // Comentário: evita quebra de formato (aspas simples no payload).
            return value.Replace("'", "’").Trim();
        }

        private sealed class Entry
        {
            public readonly string Feature;
            public readonly string Reason;

            public int Count;
            public float LastLogTime;
            public bool LoggedOnce;

            public Entry(string feature, string reason)
            {
                Feature = string.IsNullOrWhiteSpace(feature) ? DegradedKeys.Feature.Infrastructure : feature;
                Reason = string.IsNullOrWhiteSpace(reason) ? DegradedKeys.Reason.Unknown : reason;
                Count = 0;
                LastLogTime = 0f;
                LoggedOnce = false;
            }
        }
    }
}
