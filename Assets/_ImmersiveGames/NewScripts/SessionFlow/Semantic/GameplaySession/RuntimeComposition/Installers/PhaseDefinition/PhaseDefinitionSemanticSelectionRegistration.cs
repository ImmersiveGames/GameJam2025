using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Authoring;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.Contracts;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.OrdinalNavigation;
using _ImmersiveGames.NewScripts.SessionFlow.Semantic.PhaseCatalog.RuntimeState;
namespace _ImmersiveGames.NewScripts.SessionFlow.Semantic.GameplaySession.RuntimeComposition.Installers.PhaseDefinition
{
    internal static class PhaseDefinitionSemanticSelectionRegistration
    {
        public static void RegisterAll()
        {
            RegisterPhaseDefinitionSelectionService();
        }

        private static void RegisterPhaseDefinitionSelectionService()
        {
            if (!DependencyManager.Provider.TryGetGlobal<IPhaseDefinitionSelectionService>(out var existingSelectionService) || existingSelectionService == null)
            {
                if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var runtimeStateService) || runtimeStateService == null)
                {
                    throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseCatalogRuntimeStateService missing from global DI before selection service registration.");
                }

                var selectionService = new PhaseDefinitionSelectionService(runtimeStateService);
                DependencyManager.Provider.RegisterGlobal<IPhaseDefinitionSelectionService>(selectionService);

                DebugUtility.LogVerbose(typeof(PhaseDefinitionSemanticSelectionRegistration),
                    $"[OBS][PhaseDefinition][Core] Selection service registered initialPhaseId='{selectionService.SelectedPhaseDefinitionId}' asset='{selectionService.Current.name}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseCatalogRuntimeStateService>(out var runtimeStateServiceRef) || runtimeStateServiceRef == null)
            {
                throw new InvalidOperationException("[FATAL][Config][PhaseDefinition] IPhaseCatalogRuntimeStateService missing from global DI while validating selection service registration.");
            }

            PhaseDefinitionAsset committedPhaseDefinitionRef = runtimeStateServiceRef.CurrentCommitted;
            if (!ReferenceEquals(existingSelectionService.Current, committedPhaseDefinitionRef))
            {
                throw new InvalidOperationException(
                    $"[FATAL][Config][PhaseDefinition] Selection service mismatch: DI has phaseAsset='{existingSelectionService.Current?.name ?? "<none>"}' but runtime committed phase is '{committedPhaseDefinitionRef.name}'.");
            }
        }
    }
}
