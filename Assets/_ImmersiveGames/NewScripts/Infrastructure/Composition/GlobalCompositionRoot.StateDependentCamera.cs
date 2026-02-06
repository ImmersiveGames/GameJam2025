using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Actions.States;
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public static partial class GlobalCompositionRoot
    {
        // StateDependent / Camera
        // --------------------------------------------------------------------

        private static void RegisterStateDependentService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IStateDependentService>(out var existing) && existing != null)
            {
                if (existing is StateDependentService)
                {
                    DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                        "[StateDependent] StateDependentService já registrado no DI global.",
                        DebugUtility.Colors.Info);
                    return;
                }

                DebugUtility.LogWarning(typeof(GlobalCompositionRoot),
                    $"[StateDependent] Serviço registrado ({existing.GetType().Name}) não usa gate; substituindo por StateDependentService.");

                DependencyManager.Provider.RegisterGlobal<IStateDependentService>(
                    new StateDependentService());

                DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                    "[StateDependent] Registrado StateDependentService (gate-aware) como IStateDependentService.",
                    DebugUtility.Colors.Info);
                return;
            }

            RegisterIfMissing<IStateDependentService>(() => new StateDependentService());

            DebugUtility.LogVerbose(typeof(GlobalCompositionRoot),
                "[StateDependent] Registrado StateDependentService (gate-aware) como IStateDependentService.",
                DebugUtility.Colors.Info);
        }

    }
}
