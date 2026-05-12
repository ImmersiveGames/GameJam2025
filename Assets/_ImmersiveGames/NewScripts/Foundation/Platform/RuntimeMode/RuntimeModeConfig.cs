using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.SaveRuntime.Authoring;
using _ImmersiveGames.NewScripts.SessionOperational.Contracts;
using _ImmersiveGames.NewScripts.SessionOperational.Pipeline;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode
{
    /// <summary>
    /// Configuração global (asset) para controlar o modo de execução e a política do reporter de degradação.
    ///
    /// Uso esperado:
    /// - Criar um asset em Resources com o nome "RuntimeModeConfig".
    /// - O bootstrap canônico faz a resolução explícita e falha cedo se o asset obrigatório estiver ausente.
    /// </summary>
    [CreateAssetMenu(
        fileName = "RuntimeModeConfig",
        menuName = "ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/RuntimeModeConfig",
        order = 20)]
    public sealed class RuntimeModeConfig : ScriptableObject
    {
        /// <summary>
        /// Caminho canônico para carregamento via Resources.
        /// </summary>
        public const string DefaultResourcesPath = "RuntimeModeConfig";

        /// <summary>
        /// Modo de execução: Auto (automático), ForceStrict (strict mode) ou ForceRelease (release mode).
        /// </summary>
        [Header("Modo")]
        [Tooltip("Auto: decide sozinho. ForceStrict/ForceRelease: força o modo, útil para testes.")]
        public RuntimeModeOverride modeOverride = RuntimeModeOverride.Auto;

        /// <summary>
        /// Política canônica de cenas persistentes do modo de runtime atual.
        /// </summary>
        [Header("Runtime Persistent Scenes")]
        [Tooltip("Policy asset with runtime-support scenes that must stay loaded while the runtime mode is active.")]
        [SerializeField] private RuntimePersistentScenesPolicyAsset runtimePersistentScenesPolicy;

        public RuntimePersistentScenesPolicyAsset RuntimePersistentScenesPolicy => runtimePersistentScenesPolicy;

        /// <summary>
        /// Defaults canÃ´nicos de Ã¡udio do modo de runtime atual.
        /// </summary>
        [Header("Audio")]
        [Tooltip("Audio defaults asset canonical for the active runtime mode.")]
        [InspectorName("AudioDefaults")]
        [SerializeField] private AudioDefaultsAsset audioDefaults;

        public AudioDefaultsAsset AudioDefaults => audioDefaults;

        /// <summary>
        /// Default loading policy for routes that opt into RuntimeDefault loading mode.
        /// </summary>
        [Header("Loading")]
        [Tooltip("Default loading mode used when a route chooses RuntimeDefault.")]
        [SerializeField] private SessionOperationalRouteLoadingMode defaultLoadingMode = SessionOperationalRouteLoadingMode.None;

        /// <summary>
        /// Default loading profile for routes that opt into RuntimeDefault loading mode.
        /// </summary>
        [Tooltip("Default loading profile used when DefaultLoadingMode=Profile.")]
        [SerializeField] private RuntimeLoadingProfileAsset defaultLoadingProfile;

        public SessionOperationalRouteLoadingMode DefaultLoadingMode => defaultLoadingMode;
        public RuntimeLoadingProfileAsset DefaultLoadingProfile => defaultLoadingProfile;

        /// <summary>
        /// ConfiguraÃ§Ãµes do reporter de degradaÃ§Ã£o (dedupe, resumo, etc).
        /// </summary>
        [Header("Degraded Mode Reporter")]
        public DegradedReporterSettings reporter = new();

        /// <summary>
        /// ConfiguraÃ§Ãµes de strictness aplicadas quando em modo Strict.
        /// </summary>
        [Header("Strictness (somente em Strict)")]
        public StrictnessSettings strictness = new();

        /// <summary>
        /// ConfiguraÃ§Ãµes do mÃ³dulo InputModes.
        /// </summary>
        [Header("Input Modes")]
        public InputModesSettings inputModes = new();

        /// <summary>
        /// Profile de composiÃ§Ã£o global usado pelo bootstrap.
        /// O profile canônico remove rails legados do caminho.
        /// </summary>
        [Header("Composition Profile")]
        [Tooltip("Seleciona o profile de composição global. O profile canônico remove rails legados do caminho.")]
        public CompositionProfileKind compositionProfile = CompositionProfileKind.Base11Sandbox;

        /// <summary>
        /// Rota inicial explÃ­cita do profile canônico.
        /// NÃ£o Ã© um default implÃ­cito: o bootstrap falha se estiver ausente ou invÃ¡lido.
        /// </summary>
        [Header("Canonical Runtime")]
        [Tooltip("Referência direta para a rota inicial do profile canônico.")]
        [SerializeField] private OperationalRouteAsset startupRouteDefinition;

        [Header("Save")]
        [Tooltip("Referência direta para a configuração canônica do Save.")]
        [SerializeField] private SaveConfigAsset saveConfig;

        /// <summary>
        /// Referência direta para a rota inicial do profile canônico.
        /// </summary>
        [Header("Canonical Runtime")]
        [Tooltip("Referência direta para a rota inicial do profile canônico.")]
        public OperationalRouteAsset StartupRouteDefinition => startupRouteDefinition;
        public SaveConfigAsset SaveConfig => saveConfig;

        public bool TryValidateLoadingConfiguration(out string errorMessage)
        {
            if (defaultLoadingMode == SessionOperationalRouteLoadingMode.RuntimeDefault)
            {
                errorMessage = "defaultLoadingMode cannot be RuntimeDefault.";
                return false;
            }

            if (defaultLoadingMode == SessionOperationalRouteLoadingMode.Profile)
            {
                if (defaultLoadingProfile == null)
                {
                    errorMessage = "defaultLoadingProfile is required when DefaultLoadingMode=Profile.";
                    return false;
                }

                if (!defaultLoadingProfile.TryValidate(out string profileValidationError))
                {
                    errorMessage = $"defaultLoadingProfile is invalid. detail='{profileValidationError}'.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        public bool TryValidateAudioConfiguration(out string errorMessage)
        {
            if (compositionProfile == CompositionProfileKind.Base11Sandbox)
            {
                if (audioDefaults == null)
                {
                    errorMessage = "audioDefaults is required when CompositionProfile=canonical profile.";
                    return false;
                }
            }
            else if (audioDefaults == null)
            {
                errorMessage = "audioDefaults is required when set for non-canonical profiles.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnValidate()
        {
            bool loadingValid = TryValidateLoadingConfiguration(out string loadingError) || string.IsNullOrWhiteSpace(loadingError);
            bool audioValid = TryValidateAudioConfiguration(out string audioError) || string.IsNullOrWhiteSpace(audioError);

            if (loadingValid && audioValid)
            {
                return;
            }

            string errorMessage = !loadingValid ? loadingError : audioError;
            DebugUtility.LogWarning(typeof(RuntimeModeConfig),
                $"[Config][Editor] RuntimeModeConfig invalid. detail='{errorMessage}'");
        }
#endif

        /// <summary>
        /// ConfiguraÃ§Ãµes do reporter de degradaÃ§Ã£o: dedupe, resumos periÃ³dicos e limite de chaves.
        /// </summary>
        [Serializable]
        public sealed class DegradedReporterSettings
        {
            /// <summary>
            /// EstratÃ©gia para evitar repetiÃ§Ã£o de logs de degradaÃ§Ã£o.
            /// </summary>
            [Tooltip("Como evitar repetiÃ§Ã£o de logs de degradaÃ§Ã£o.")]
            public DegradedDedupStrategy dedupStrategy = DegradedDedupStrategy.CooldownSeconds;

            /// <summary>
            /// Intervalo mÃ­nimo (em segundos) entre logs iguais. Aplicado se DedupStrategy=CooldownSeconds.
            /// </summary>
            [Tooltip("Se DedupStrategy=CooldownSeconds, define o intervalo mÃ­nimo entre logs iguais (segundos).")]
            [Range(0f, 60f)]
            public float cooldownSeconds = 5f;

            /// <summary>
            /// Intervalo (em segundos) para emissÃ£o periÃ³dica de resumo. 0 desliga o resumo.
            /// </summary>
            [Tooltip("Emite um resumo periÃ³dico com contagens (0 desliga).")]
            [Range(0f, 300f)]
            public float emitSummaryEverySeconds = 30f;

            /// <summary>
            /// Limite mÃ¡ximo de chaves Ãºnicas rastreadas por sessÃ£o (proteÃ§Ã£o contra explosÃ£o de memory).
            /// </summary>
            [Tooltip("Limite de chaves Ãºnicas rastreadas por sessÃ£o (proteÃ§Ã£o contra explosÃ£o de keys).")]
            [Range(16, 4096)]
            public int maxUniqueKeys = 256;

            /// <summary>
            /// Se verdadeiro, imprime a primeira ocorrÃªncia imediatamente, mesmo com dedupe ligado.
            /// </summary>
            [Tooltip("Imprime a primeira ocorrÃªncia imediatamente, mesmo com dedupe ligado.")]
            public bool logFirstOccurrence = true;

            /// <summary>
            /// Se verdadeiro, inclui a contagem acumulada no log (ex.: count=7).
            /// </summary>
            [Tooltip("Inclui a contagem acumulada no log (ex.: count=7).")]
            public bool includeCountInLog = true;
        }

        /// <summary>
        /// ConfiguraÃ§Ãµes de comportamento em modo Strict.
        /// </summary>
        [Serializable]
        public sealed class StrictnessSettings
        {
            /// <summary>
            /// Se verdadeiro, logs de degradaÃ§Ã£o sobem para erro (sem exceÃ§Ã£o).
            /// </summary>
            [Tooltip("Em Strict, logs de degradaÃ§Ã£o sobem para erro (sem exceÃ§Ã£o).")]
            public bool degradedAsError = true;

            /// <summary>
            /// Se verdadeiro, permite falhar hard (exceÃ§Ã£o) em casos de degradaÃ§Ã£o.
            /// Recomendado manter falso nesta fase.
            /// </summary>
            [Tooltip("Em Strict, permite falhar hard (exceÃ§Ã£o) em casos de degradaÃ§Ã£o. Recomendado manter falso nesta fase.")]
            public bool degradedAsException;
        }

        /// <summary>
        /// ConfiguraÃ§Ãµes do mÃ³dulo InputModes.
        /// </summary>
        [Serializable]
        public sealed class InputModesSettings
        {
            /// <summary>
            /// Deve permanecer verdadeiro para manter o trilho canonico do InputModes.
            /// </summary>
            [Tooltip("Deve permanecer habilitado. Quando falso, o boot falha por quebrar o trilho canonico de InputModes.")]
            public bool enableInputModes = true;

            /// <summary>
            /// Nome do action map de gameplay (Player).
            /// </summary>
            [Tooltip("Nome do action map de gameplay (Player).")]
            public string playerActionMapName = InputModesDefaults.PlayerActionMapName;

            /// <summary>
            /// Nome do action map de menu/UI.
            /// </summary>
            [Tooltip("Nome do action map de menu/UI.")]
            public string menuActionMapName = InputModesDefaults.MenuActionMapName;

            /// <summary>
            /// Se verdadeiro, emite logs verbosos de configuraÃ§Ã£o/registro.
            /// </summary>
            [Tooltip("Emite logs verbosos de configuraÃ§Ã£o/registro.")]
            public bool logVerbose = true;
        }
    }

    /// <summary>
    /// Define como o sistema deve se comportar em termos de modo de execuÃ§Ã£o.
    /// </summary>
    public enum RuntimeModeOverride
    {
        /// <summary>
        /// Modo automÃ¡tico: o sistema decide entre Strict ou Release baseado no build.
        /// </summary>
        Auto = 0,
        /// <summary>
        /// ForÃ§a modo Strict: validaÃ§Ãµes rÃ­gidas, erros em degradaÃ§Ã£o.
        /// </summary>
        ForceStrict = 1,
        /// <summary>
        /// ForÃ§a modo Release: lenient, tenta se recuperar de degradaÃ§Ã£o.
        /// </summary>
        ForceRelease = 2
    }

    /// <summary>
    /// EstratÃ©gia de dedupe para evitar repetiÃ§Ã£o excessiva de logs de degradaÃ§Ã£o.
    /// </summary>
    public enum DegradedDedupStrategy
    {
        /// <summary>
        /// Uma Ãºnica vez por sessÃ£o: cada chave Ã© logada apenas uma vez.
        /// </summary>
        PerSession = 0,
        /// <summary>
        /// Com cooldown em segundos: mesma chave sÃ³ Ã© logada se passou o intervalo.
        /// </summary>
        CooldownSeconds = 1
    }

    /// <summary>
    /// Profile explÃ­cito de composiÃ§Ã£o global.
    /// </summary>
    public enum CompositionProfileKind
    {
        LegacyCompatible = 0,
        Base11Sandbox = 1
    }
}
