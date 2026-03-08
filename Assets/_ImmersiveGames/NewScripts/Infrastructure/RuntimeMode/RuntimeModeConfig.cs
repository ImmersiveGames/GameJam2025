using System;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Modules.InputModes;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode
{
    /// <summary>
    /// ConfiguraÃ§Ã£o global (asset) para controlar o modo de execuÃ§Ã£o e a polÃ­tica do reporter de degradaÃ§Ã£o.
    ///
    /// Uso esperado:
    /// - Criar um asset em Resources com o nome "RuntimeModeConfig" para carregamento automÃ¡tico.
    /// - Se o asset nÃ£o existir, o sistema opera em modo Auto (comportamento atual).
    /// </summary>
    [CreateAssetMenu(
        fileName = "RuntimeModeConfig",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/Configs/RuntimeModeConfig",
        order = 20)]
    public sealed class RuntimeModeConfig : ScriptableObject
    {
        // Caminho canÃ´nico para carregamento via Resources.
        public const string DefaultResourcesPath = "RuntimeModeConfig";

        [Header("Modo")]
        [Tooltip("Auto: decide sozinho. ForceStrict/ForceRelease: forÃ§a o modo, Ãºtil para testes.")]
        public RuntimeModeOverride modeOverride = RuntimeModeOverride.Auto;

        [Header("Bootstrap")]
        [Tooltip("Config raiz obrigatÃ³rio do NewScripts (resolvido pelo GlobalCompositionRoot).")]
        [SerializeField] private NewScriptsBootstrapConfigAsset newScriptsBootstrapConfig;

        public NewScriptsBootstrapConfigAsset NewScriptsBootstrapConfig => newScriptsBootstrapConfig;

        [Header("Degraded Mode Reporter")]
        public DegradedReporterSettings reporter = new DegradedReporterSettings();

        [Header("Strictness (somente em Strict)")]
        public StrictnessSettings strictness = new StrictnessSettings();

        [Header("Input Modes")]
        public InputModesSettings inputModes = new InputModesSettings();

        [Serializable]
        public sealed class DegradedReporterSettings
        {
            [Tooltip("Como evitar repetiÃ§Ã£o de logs de degradaÃ§Ã£o.")]
            public DegradedDedupStrategy dedupStrategy = DegradedDedupStrategy.CooldownSeconds;

            [Tooltip("Se DedupStrategy=CooldownSeconds, define o intervalo mÃ­nimo entre logs iguais (segundos).")]
            [Range(0f, 60f)]
            public float cooldownSeconds = 5f;

            [Tooltip("Emite um resumo periÃ³dico com contagens (0 desliga).")]
            [Range(0f, 300f)]
            public float emitSummaryEverySeconds = 30f;

            [Tooltip("Limite de chaves Ãºnicas rastreadas por sessÃ£o (proteÃ§Ã£o contra explosÃ£o de keys).")]
            [Range(16, 4096)]
            public int maxUniqueKeys = 256;

            [Tooltip("Imprime a primeira ocorrÃªncia imediatamente, mesmo com dedupe ligado.")]
            public bool logFirstOccurrence = true;

            [Tooltip("Inclui a contagem acumulada no log (ex.: count=7).")]
            public bool includeCountInLog = true;
        }

        [Serializable]
        public sealed class StrictnessSettings
        {
            [Tooltip("Em Strict, logs de degradaÃ§Ã£o sobem para erro (sem exceÃ§Ã£o).")]
            public bool degradedAsError = true;

            [Tooltip("Em Strict, permite falhar hard (exceÃ§Ã£o) em casos de degradaÃ§Ã£o. Recomendado manter falso nesta fase.")]
            public bool degradedAsException = false;
        }

        [Serializable]
        public sealed class InputModesSettings
        {
            [Tooltip("Habilita o mÃ³dulo InputModes (registro do IInputModeService no DI global).")]
            public bool enableInputModes = true;

            [Tooltip("Nome do action map de gameplay (Player).")]
            public string playerActionMapName = InputModesDefaults.PlayerActionMapName;

            [Tooltip("Nome do action map de menu/UI.")]
            public string menuActionMapName = InputModesDefaults.MenuActionMapName;

            [Tooltip("Emite logs verbosos de configuraÃ§Ã£o/registro.")]
            public bool logVerbose = true;
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

