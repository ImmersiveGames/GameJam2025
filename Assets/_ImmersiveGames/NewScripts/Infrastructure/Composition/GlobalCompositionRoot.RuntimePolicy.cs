using System;
using _ImmersiveGames.NewScripts.Infrastructure.Composition;
using _ImmersiveGames.NewScripts.Infrastructure.Config;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Orchestration.WorldReset.Policies;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterRuntimePolicyServices()
        {
            var bootstrapConfig = GetRequiredBootstrapConfig(out _);
            var config = ResolveRuntimeModeConfigOrFailFast(bootstrapConfig);

            var provider = DependencyManager.Provider;

            // (Opcional) expõe a config no DI global para inspeção/QA.
            // Importante: não registrar nulo.
            if (config != null)
            {
                if (!provider.TryGetGlobal<RuntimeModeConfig>(out var existingConfig) || existingConfig == null)
                {
                    provider.RegisterGlobal(config, allowOverride: false);

                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        $"[RuntimePolicy] RuntimeModeConfig carregado (asset='{config.name}').",
                        DebugUtility.Colors.Info);
                }
            }

            // Provider configurável: o config agora é obrigatório no boot; o fallback do provider fica só para override explícito no asset.
            RegisterIfMissing<IRuntimeModeProvider>(() =>
                new ConfigurableRuntimeModeProvider(new UnityRuntimeModeProvider(), config));

            provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
            if (runtimeModeProvider == null)
            {
                runtimeModeProvider = new UnityRuntimeModeProvider();
            }

            // Reporter configurável com settings vindos do asset obrigatório.
            RegisterIfMissing<IDegradedModeReporter>(() =>
                new DegradedModeReporter(runtimeModeProvider, config));

            provider.TryGetGlobal<IDegradedModeReporter>(out var degradedReporter);

            RegisterIfMissing<IWorldResetPolicy>(() =>
                new ProductionWorldResetPolicy(runtimeModeProvider, degradedReporter));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[RuntimePolicy] IRuntimeModeProvider + IDegradedModeReporter + IWorldResetPolicy registrados no DI global.",
                DebugUtility.Colors.Info);
        }

        private static RuntimeModeConfig ResolveRuntimeModeConfigOrFailFast(BootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig == null)
            {
                string message = "[FATAL][Config][RuntimePolicy] BootstrapConfigAsset obrigatorio ausente antes de resolver RuntimeModeConfig.";
                DebugUtility.LogError(typeof(GlobalCompositionRoot), message);
                throw new InvalidOperationException(message);
            }

            RuntimeModeConfig config = bootstrapConfig.RuntimeModeConfig;
            if (config == null)
            {
                string message =
                    $"[FATAL][Config][RuntimePolicy] RuntimeModeConfig obrigatorio ausente no BootstrapConfigAsset. bootstrap='{bootstrapConfig.name}'.";

                DebugUtility.LogError(typeof(GlobalCompositionRoot), message);
                throw new InvalidOperationException(message);
            }

            if (DependencyManager.HasInstance)
            {
                var provider = DependencyManager.Provider;
                if (provider != null && (!provider.TryGetGlobal<RuntimeModeConfig>(out var existingConfig) || existingConfig == null))
                {
                    provider.RegisterGlobal(config, allowOverride: false);
                }
            }

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                $"[RuntimePolicy] RuntimeModeConfig resolvido via BootstrapConfigAsset (asset='{config.name}').",
                DebugUtility.Colors.Info);
            return config;
        }

    }
}
