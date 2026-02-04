using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.GameLoop;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Runtime.GameLoop.Drivers
{
    /// <summary>
    /// Runner global (DontDestroyOnLoad) responsável por ticker o IGameLoopService.
    /// Criado automaticamente pelo GameLoopBootstrap.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameLoopRuntimeDriver : MonoBehaviour
    {
        private IGameLoopService _service;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ResolveService(logIfMissing: true);
        }

        private void Update()
        {
            if (_service == null)
            {
                ResolveService(logIfMissing: false);
                if (_service == null)
                {
                    return;
                }
            }

            _service.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            // Não dispose aqui: o serviço global pertence ao DI global.
            _service = null;
        }

        private void ResolveService(bool logIfMissing)
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var service) && service != null)
            {
                _service = service;
                return;
            }

            if (logIfMissing)
            {
                DebugUtility.LogWarning(typeof(GameLoopRuntimeDriver),
                    "[GameLoop] IGameLoopService não encontrado no DI global no Awake. " +
                    "O GameLoopBootstrap deve registrar o serviço antes do primeiro tick.");
            }
        }
    }
}


