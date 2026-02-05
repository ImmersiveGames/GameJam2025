using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// Configuração global (asset) para controlar o modo de execução e a política do reporter de degradação.
    ///
    /// Uso esperado:
    /// - Criar um asset em Resources com o nome "RuntimeModeConfig" para carregamento automático.
    /// - Se o asset não existir, o sistema opera em modo Auto (comportamento atual).
    /// </summary>
    [CreateAssetMenu(
        fileName = "RuntimeModeConfig",
        menuName = "ImmersiveGames/Runtime Mode Config",
        order = 0)]
    public sealed class RuntimeModeConfig : ScriptableObject
    {
        [Header("Modo")]
        [Tooltip("Auto: decide sozinho. ForceStrict/ForceRelease: força o modo, útil para testes.")]
        public RuntimeModeOverride modeOverride = RuntimeModeOverride.Auto;

        [Header("Degraded Mode Reporter")]
        public DegradedReporterSettings reporter = new DegradedReporterSettings();

        [Header("Strictness (somente em Strict)")]
        public StrictnessSettings strictness = new StrictnessSettings();

        [Serializable]
        public sealed class DegradedReporterSettings
        {
            [Tooltip("Como evitar repetição de logs de degradação.")]
            public DegradedDedupStrategy dedupStrategy = DegradedDedupStrategy.CooldownSeconds;

            [Tooltip("Se DedupStrategy=CooldownSeconds, define o intervalo mínimo entre logs iguais (segundos).")]
            [Range(0f, 60f)]
            public float cooldownSeconds = 5f;

            [Tooltip("Emite um resumo periódico com contagens (0 desliga).")]
            [Range(0f, 300f)]
            public float emitSummaryEverySeconds = 30f;

            [Tooltip("Limite de chaves únicas rastreadas por sessão (proteção contra explosão de keys).")]
            [Range(16, 4096)]
            public int maxUniqueKeys = 256;

            [Tooltip("Imprime a primeira ocorrência imediatamente, mesmo com dedupe ligado.")]
            public bool logFirstOccurrence = true;

            [Tooltip("Inclui a contagem acumulada no log (ex.: count=7).")]
            public bool includeCountInLog = true;
        }

        [Serializable]
        public sealed class StrictnessSettings
        {
            [Tooltip("Em Strict, logs de degradação sobem para erro (sem exceção).")]
            public bool degradedAsError = true;

            [Tooltip("Em Strict, permite falhar hard (exceção) em casos de degradação. Recomendado manter falso nesta fase.")]
            public bool degradedAsException = false;
        }
    }

    public enum RuntimeModeOverride
    {
        Auto = 0,
        ForceStrict = 1,
        ForceRelease = 2
    }

    public enum DegradedDedupStrategy
    {
        PerSession = 0,
        CooldownSeconds = 1
    }
}
