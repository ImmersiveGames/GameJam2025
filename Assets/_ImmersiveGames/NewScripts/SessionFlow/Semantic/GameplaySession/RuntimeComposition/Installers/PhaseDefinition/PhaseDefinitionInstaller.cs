using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Config;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    public static class PhaseDefinitionInstaller
    {
        private static bool _installed;

        public static void Install(BootstrapConfigAsset bootstrapConfig)
        {
            if (_installed)
            {
                return;
            }

            if (bootstrapConfig == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] BootstrapConfigAsset obrigatorio ausente para instalar PhaseDefinition.");
            }

            bool phaseEnabled = ResolveGameplayPhaseEnablementOrFail(bootstrapConfig);
            if (!phaseEnabled)
            {
                _installed = true;
                DebugUtility.Log(typeof(PhaseDefinitionInstaller),
                    "[OBS][PhaseDefinition][Core] Gameplay route/context phase-disabled; installer no-op.",
                    DebugUtility.Colors.Info);
                return;
            }

            PhaseDefinitionSemanticRegistration.RegisterAll(bootstrapConfig);
            PhaseDefinitionSeamRegistration.RegisterAll();
            PhaseDefinitionOperationalAuxRegistration.RegisterAll();

            _installed = true;

            DebugUtility.Log(typeof(PhaseDefinitionInstaller),
                "[PhaseDefinition][Core] Module installer concluido.",
                DebugUtility.Colors.Info);
        }

        private static bool ResolveGameplayPhaseEnablementOrFail(BootstrapConfigAsset bootstrapConfig)
        {
            if (bootstrapConfig.NavigationCatalog == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] GameNavigationCatalog obrigatorio ausente para resolver phase-enabled/phase-disabled.");
            }

            bool phaseEnabled = bootstrapConfig.NavigationCatalog.IsGameplayPhaseEnabledOrFail();
            DebugUtility.LogVerbose(typeof(PhaseDefinitionInstaller),
                $"[OBS][PhaseDefinition][Core] route-driven phase enablement resolved phaseEnabled={phaseEnabled}.",
                DebugUtility.Colors.Info);
            return phaseEnabled;
        }
    }
}
