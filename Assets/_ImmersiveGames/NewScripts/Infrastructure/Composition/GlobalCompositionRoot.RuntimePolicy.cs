using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.WorldLifecycle.WorldRearm.Policies;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterRuntimePolicyServices()
        {
            // RuntimeModeConfig (opcional) via Resources.
            // Contrato: ausência de config não deve quebrar o jogo.
            var config = RuntimeModeConfigLoader.LoadOrNull();

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

            // Provider configurável: usa o config se existir, senão cai no comportamento atual (UnityRuntimeModeProvider).
            RegisterIfMissing<IRuntimeModeProvider>(() =>
                new ConfigurableRuntimeModeProvider(new UnityRuntimeModeProvider(), config));

            provider.TryGetGlobal<IRuntimeModeProvider>(out var runtimeModeProvider);
            if (runtimeModeProvider == null)
            {
                runtimeModeProvider = new UnityRuntimeModeProvider();
            }

            // Reporter configurável (dedupe/summary/limites via config, se existir).
            RegisterIfMissing<IDegradedModeReporter>(() =>
                new DegradedModeReporter(runtimeModeProvider, config));

            provider.TryGetGlobal<IDegradedModeReporter>(out var degradedReporter);

            RegisterIfMissing<IWorldResetPolicy>(() =>
                new ProductionWorldResetPolicy(runtimeModeProvider, degradedReporter));

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[RuntimePolicy] IRuntimeModeProvider + IDegradedModeReporter + IWorldResetPolicy registrados no DI global.",
                DebugUtility.Colors.Info);
        }

    }
}
