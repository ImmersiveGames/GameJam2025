using System;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    [CreateAssetMenu(
        fileName = "RuntimeModeConfig",
        menuName = "ImmersiveGames/Infrastructure/RuntimeMode/RuntimeModeConfig",
        order = 20)]
    public sealed class RuntimeModeConfig : ScriptableObject
    {

        [Header("Modo")]
        [Tooltip("Auto: decide sozinho. ForceStrict/ForceRelease: for�a o modo, �til para testes.")]
        public RuntimeModeOverride modeOverride = RuntimeModeOverride.Auto;

        [Header("Composition Profile")]
        [Tooltip("Seleciona o profile de composi��o global. O profile can�nico remove rails n�o can�nicos do caminho.")]
        public CompositionProfileKind compositionProfile = CompositionProfileKind.Base11Sandbox;

        [Header("Runtime Config Set")]
        [Tooltip("Refer�ncia expl�cita para o RuntimeConfigSetAsset can�nico do modo atual.")]
        [SerializeField] private RuntimeConfigSetAsset runtimeConfigSet;


        public RuntimeConfigSetAsset RuntimeConfigSet => runtimeConfigSet;

        [Serializable]
        public sealed class DegradedReporterSettings
        {
            [Tooltip("Como evitar repeti��o de logs de degrada��o.")]
            public DegradedDedupStrategy dedupStrategy = DegradedDedupStrategy.CooldownSeconds;

            [Tooltip("Se DedupStrategy=CooldownSeconds, define o intervalo m�nimo entre logs iguais (segundos).")]
            [Range(0f, 60f)]
            public float cooldownSeconds = 5f;

            [Tooltip("Emite um resumo peri�dico com contagens (0 desliga).")]
            [Range(0f, 300f)]
            public float emitSummaryEverySeconds = 30f;

            [Tooltip("Limite de chaves �nicas rastreadas por sess�o (prote��o contra explos�o de keys).")]
            [Range(16, 4096)]
            public int maxUniqueKeys = 256;

            [Tooltip("Imprime a primeira ocorr�ncia imediatamente, mesmo com dedupe ligado.")]
            public bool logFirstOccurrence = true;

            [Tooltip("Inclui a contagem acumulada no log (ex.: count=7).")]
            public bool includeCountInLog = true;
        }

        [Serializable]
        public sealed class StrictnessSettings
        {
            [Tooltip("Em Strict, logs de degrada��o sobem para erro (sem exce��o).")]
            public bool degradedAsError = true;

            [Tooltip("Em Strict, permite falhar hard (exce��o) em casos de degrada��o. Recomendado manter falso nesta fase.")]
            public bool degradedAsException;
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

    public enum CompositionProfileKind
    {
        NonCanonical = 0,
        Base11Sandbox = 1
    }
}
