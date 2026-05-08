using System;
using _ImmersiveGames.NewScripts.AudioRuntime.Authoring.Config;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
using _ImmersiveGames.NewScripts.InputModes.Runtime;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.SessionOperationalPipeline;
using UnityEngine;
using UnityEngine.Serialization;

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
        /// Configuração raiz obrigatória do NewScripts (resolvida pelo GlobalCompositionRoot).
        /// </summary>
        [Header("Bootstrap")]
        [Tooltip("Config raiz obrigatória do NewScripts (resolvido pelo GlobalCompositionRoot).")]
        [FormerlySerializedAs("BootstrapConfig")]
        [SerializeField] private BootstrapConfigAsset bootstrapConfig;

        [Obsolete("BootstrapConfig is legacy and must not be used by the canonical Base11Sandbox rail. Use RuntimeModeConfig.RuntimePersistentScenesPolicy and RuntimeModeConfig.StartupRouteDefinition instead.")]
        public BootstrapConfigAsset BootstrapConfig => bootstrapConfig;

        /// <summary>
        /// Política canônica de cenas persistentes do modo de runtime atual.
        /// </summary>
        [Header("Runtime Persistent Scenes")]
        [Tooltip("Policy asset with runtime-support scenes that must stay loaded while the runtime mode is active.")]
        [SerializeField] private RuntimePersistentScenesPolicyAsset runtimePersistentScenesPolicy;

        public RuntimePersistentScenesPolicyAsset RuntimePersistentScenesPolicy => runtimePersistentScenesPolicy;

        /// <summary>
        /// Defaults canônicos de áudio do modo de runtime atual.
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
        /// Configurações do reporter de degradação (dedupe, resumo, etc).
        /// </summary>
        [Header("Degraded Mode Reporter")]
        public DegradedReporterSettings reporter = new();

        /// <summary>
        /// Configurações de strictness aplicadas quando em modo Strict.
        /// </summary>
        [Header("Strictness (somente em Strict)")]
        public StrictnessSettings strictness = new();

        /// <summary>
        /// Configurações do módulo InputModes.
        /// </summary>
        [Header("Input Modes")]
        public InputModesSettings inputModes = new();

        /// <summary>
        /// Profile de composição global usado pelo bootstrap.
        /// Base11Sandbox remove rails legados do caminho.
        /// </summary>
        [Header("Composition Profile")]
        [Tooltip("Seleciona o profile de composição global. Base11Sandbox remove rails legados do caminho.")]
        public CompositionProfileKind compositionProfile = CompositionProfileKind.Base11Sandbox;

        /// <summary>
        /// Rota inicial explícita do profile Base11Sandbox.
        /// Não é um default implícito: o bootstrap falha se estiver ausente ou inválido.
        /// </summary>
        [Header("Base11 Sandbox")]
        [Tooltip("Referência direta para a rota inicial do Base11Sandbox.")]
        [SerializeField] private SessionOperationalRouteAsset startupRouteDefinition;

        /// <summary>
        /// Referência direta para a rota inicial do Base11Sandbox.
        /// </summary>
        [Header("Base11 Sandbox")]
        [Tooltip("Referência direta para a rota inicial do Base11Sandbox.")]
        public SessionOperationalRouteAsset StartupRouteDefinition => startupRouteDefinition;

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
                    errorMessage = "audioDefaults is required when CompositionProfile=Base11Sandbox.";
                    return false;
                }
            }
            else if (audioDefaults == null)
            {
                errorMessage = "audioDefaults is required when set for non-Base11Sandbox profiles.";
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
        /// Configurações do reporter de degradação: dedupe, resumos periódicos e limite de chaves.
        /// </summary>
        [Serializable]
        public sealed class DegradedReporterSettings
        {
            /// <summary>
            /// Estratégia para evitar repetição de logs de degradação.
            /// </summary>
            [Tooltip("Como evitar repetição de logs de degradação.")]
            public DegradedDedupStrategy dedupStrategy = DegradedDedupStrategy.CooldownSeconds;

            /// <summary>
            /// Intervalo mínimo (em segundos) entre logs iguais. Aplicado se DedupStrategy=CooldownSeconds.
            /// </summary>
            [Tooltip("Se DedupStrategy=CooldownSeconds, define o intervalo mínimo entre logs iguais (segundos).")]
            [Range(0f, 60f)]
            public float cooldownSeconds = 5f;

            /// <summary>
            /// Intervalo (em segundos) para emissão periódica de resumo. 0 desliga o resumo.
            /// </summary>
            [Tooltip("Emite um resumo periódico com contagens (0 desliga).")]
            [Range(0f, 300f)]
            public float emitSummaryEverySeconds = 30f;

            /// <summary>
            /// Limite máximo de chaves únicas rastreadas por sessão (proteção contra explosão de memory).
            /// </summary>
            [Tooltip("Limite de chaves únicas rastreadas por sessão (proteção contra explosão de keys).")]
            [Range(16, 4096)]
            public int maxUniqueKeys = 256;

            /// <summary>
            /// Se verdadeiro, imprime a primeira ocorrência imediatamente, mesmo com dedupe ligado.
            /// </summary>
            [Tooltip("Imprime a primeira ocorrência imediatamente, mesmo com dedupe ligado.")]
            public bool logFirstOccurrence = true;

            /// <summary>
            /// Se verdadeiro, inclui a contagem acumulada no log (ex.: count=7).
            /// </summary>
            [Tooltip("Inclui a contagem acumulada no log (ex.: count=7).")]
            public bool includeCountInLog = true;
        }

        /// <summary>
        /// Configurações de comportamento em modo Strict.
        /// </summary>
        [Serializable]
        public sealed class StrictnessSettings
        {
            /// <summary>
            /// Se verdadeiro, logs de degradação sobem para erro (sem exceção).
            /// </summary>
            [Tooltip("Em Strict, logs de degradação sobem para erro (sem exceção).")]
            public bool degradedAsError = true;

            /// <summary>
            /// Se verdadeiro, permite falhar hard (exceção) em casos de degradação.
            /// Recomendado manter falso nesta fase.
            /// </summary>
            [Tooltip("Em Strict, permite falhar hard (exceção) em casos de degradação. Recomendado manter falso nesta fase.")]
            public bool degradedAsException;
        }

        /// <summary>
        /// Configurações do módulo InputModes.
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
            /// Se verdadeiro, emite logs verbosos de configuração/registro.
            /// </summary>
            [Tooltip("Emite logs verbosos de configuração/registro.")]
            public bool logVerbose = true;
        }
    }

    /// <summary>
    /// Define como o sistema deve se comportar em termos de modo de execução.
    /// </summary>
    public enum RuntimeModeOverride
    {
        /// <summary>
        /// Modo automático: o sistema decide entre Strict ou Release baseado no build.
        /// </summary>
        Auto = 0,
        /// <summary>
        /// Força modo Strict: validações rígidas, erros em degradação.
        /// </summary>
        ForceStrict = 1,
        /// <summary>
        /// Força modo Release: lenient, tenta se recuperar de degradação.
        /// </summary>
        ForceRelease = 2
    }

    /// <summary>
    /// Estratégia de dedupe para evitar repetição excessiva de logs de degradação.
    /// </summary>
    public enum DegradedDedupStrategy
    {
        /// <summary>
        /// Uma única vez por sessão: cada chave é logada apenas uma vez.
        /// </summary>
        PerSession = 0,
        /// <summary>
        /// Com cooldown em segundos: mesma chave só é logada se passou o intervalo.
        /// </summary>
        CooldownSeconds = 1
    }

    /// <summary>
    /// Profile explícito de composição global.
    /// </summary>
    public enum CompositionProfileKind
    {
        LegacyCompatible = 0,
        Base11Sandbox = 1
    }
}
