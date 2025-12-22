using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Driver MonoBehaviour simples para garantir bootstrap e ticking do GameLoopService em cena.
    /// </summary>
    public class GameLoopDriver : MonoBehaviour
    {
        private IGameLoopService _service;
        private bool _ownsService;

        private void Awake()
        {
            GameLoopBootstrap.EnsureRegistered();
            ResolveService();
        }

        private void Start()
        {
            if (_service == null)
            {
                ResolveService();
            }
        }

        private void Update()
        {
            if (_service == null)
            {
                ResolveService();
            }

            _service?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_ownsService && _service != null)
            {
                _service.Dispose();
            }

            _service = null;
            _ownsService = false;
        }

        private void ResolveService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var service))
            {
                _service = service;
                _ownsService = false;
                return;
            }

            DebugUtility.Log<GameLoopDriver>("IGameLoopService não encontrado. Registrando localmente para este driver.");
            var localService = new GameLoopService();
            localService.Initialize();
            _service = localService;
            _ownsService = true;
        }
    }
}
