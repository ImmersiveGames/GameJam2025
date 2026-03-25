using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.State;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // StateDependent / Camera
        // --------------------------------------------------------------------

        private static void RegisterStateDependentService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameplayStateGate>(out var existing) && existing != null)
            {
                if (existing is GameplayStateGate)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        "[StateDependent] GameplayStateGate já registrado no DI global.",
                        DebugUtility.Colors.Info);
                    return;
                }

                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[StateDependent] Serviço registrado ({existing.GetType().Name}) não usa gate; substituindo por GameplayStateGate.");

                DependencyManager.Provider.RegisterGlobal<IGameplayStateGate>(
                    new GameplayStateGate());

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[StateDependent] Registrado GameplayStateGate (gate-aware) como IGameplayStateGate.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing<IGameplayStateGate>(() => new GameplayStateGate());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[StateDependent] Registrado GameplayStateGate (gate-aware) como IGameplayStateGate.",
                DebugUtility.Colors.Info);
        }

    }
}
