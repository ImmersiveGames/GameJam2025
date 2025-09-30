using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.StateMachineSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.Utils.DependencySystems
{
    [DebugLevel(DebugLevel.Logs)]
    public class DependencyBootstrapper : PersistentSingleton<DependencyBootstrapper>
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            DebugUtility.SetDefaultDebugLevel(DebugLevel.Verbose);
            
            // Garantir que DependencyManager existe
            if (!DependencyManager.HasInstance)
            {
                DependencyManager.Instance.ToString(); // Força criação
            }

            // Registrar serviços ESSENCIAIS (apenas os que precisam existir antes de tudo)
            Instance.RegisterEssentialServices();
        }

        private void RegisterEssentialServices()
        {
            // APENAS serviços que PRECISAM existir antes de qualquer cena
            DependencyManager.Instance.RegisterGlobal<IUniqueIdFactory>(new UniqueIdFactory());
            DependencyManager.Instance.RegisterGlobal<IStateDependentService>(new StateDependentService(GameManagerStateMachine.Instance));
     
            DependencyManager.Instance.RegisterGlobal<IActorResourceOrchestrator>(new ActorResourceOrchestratorService());

            
            DebugUtility.LogVerbose<DependencyBootstrapper>("Serviços essenciais registrados.");
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _initialized = false;
#endif
    }
}