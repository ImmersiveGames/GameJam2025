using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// Driver MonoBehaviour para garantir bootstrap e ticking do GameLoopService em cena.
    /// Importante: não cria instância local do GameLoop (evita dois loops em paralelo).
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class GameLoopDriver : MonoBehaviour
    {
        [Header("Lifetime")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private IGameLoopService _service;
        private bool _loggedMissingOnce;

        private void Awake()
        {
            GameLoopBootstrap.EnsureRegistered();

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            ResolveService();
        }

        private void Update()
        {
            if (_service == null)
            {
                ResolveService();
                return;
            }

            _service.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            _service = null;
        }

        private void ResolveService()
        {
            if (DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var service) && service != null)
            {
                _service = service;
                _loggedMissingOnce = false;
                return;
            }

            _service = null;

            if (!_loggedMissingOnce)
            {
                DebugUtility.LogWarning<GameLoopDriver>(
                    "[GameLoopDriver] IGameLoopService ainda não disponível no DI global. Driver aguardará até resolver.");
                _loggedMissingOnce = true;
            }
        }
    }
}
