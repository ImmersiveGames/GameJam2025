using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.RuntimeMode;
namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    public static partial class GlobalCompositionRoot
    {
        private static void RegisterRuntimePolicyServices()
        {
            var config = GetRequiredRuntimeModeConfig(out _);

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
            RuntimeConfigRegistry.InitializeOrFail(config);
            DebugUtility.Log(typeof(GlobalCompositionRoot),
                $"[RuntimePolicy] RuntimeConfigRegistry initialized from RuntimeModeConfig.RuntimeConfigSet (runtimeModeConfig='{config.name}' configSet='{config.RuntimeConfigSet.name}').",
                DebugUtility.Colors.Info);
            ApplyRuntimePolicyLoggingConfigOrFail();

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

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[RuntimePolicy] IRuntimeModeProvider + IDegradedModeReporter registrados no DI global.",
                DebugUtility.Colors.Info);
        }

        private static void ApplyRuntimePolicyLoggingConfigOrFail()
        {
            if (!RuntimeConfigRegistry.TryGetSnapshot(out var snapshot) || snapshot == null)
            {
                string message = "[FATAL][Config][RuntimePolicy] RuntimeConfigRegistry snapshot obrigatorio ausente para LoggingConfig.";
                DebugUtility.LogError(typeof(GlobalCompositionRoot), message);
                throw new InvalidOperationException(message);
            }

            var loggingConfig = snapshot.RuntimePolicy?.LoggingConfig;
            if (loggingConfig == null)
            {
                string message =
                    "[FATAL][Config][RuntimePolicy] RuntimeConfigRegistry invariant breach: RuntimePolicy.loggingConfig obrigatorio ausente.";
                DebugUtility.LogError(typeof(GlobalCompositionRoot), message);
                throw new InvalidOperationException(message);
            }

            const string source = "RuntimeConfigRegistry/RuntimePolicy.loggingConfig";
            DebugUtility.ApplyLoggingPolicyFromAsset(loggingConfig, source);
            DebugUtility.Log(typeof(GlobalCompositionRoot),
                $"[STARTUP][Logging] Final policy applied from LoggingConfigAsset. source='{source}' asset='{loggingConfig.name}'.",
                DebugUtility.Colors.Info);
        }

    }
}

