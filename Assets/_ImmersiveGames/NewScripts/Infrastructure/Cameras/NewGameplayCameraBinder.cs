using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Cameras
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class NewGameplayCameraBinder : MonoBehaviour
    {
        [SerializeField] private int playerId = 0;

        private Camera _camera;
        private ICameraResolver _resolver;

        private bool _registered;
        private bool _warnedMissingResolver;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            TryResolveResolver("Awake");
        }

        private void Start()
        {
            TryResolveResolver("Start");
            TryRegisterCamera("Start");
        }

        private void OnEnable()
        {
            TryRegisterCamera("OnEnable");
        }

        private void OnDisable()
        {
            TryUnregisterCamera();
        }

        private void TryResolveResolver(string context)
        {
            if (_resolver != null)
            {
                return;
            }

            if (DependencyManager.Provider.TryGetGlobal(out _resolver) && _resolver != null)
            {
                DebugUtility.LogVerbose<NewGameplayCameraBinder>(
                    $"ICameraResolver resolved in {context}.",
                    DebugUtility.Colors.Info);
                return;
            }

            if (_warnedMissingResolver)
            {
                DebugUtility.LogVerbose<NewGameplayCameraBinder>(
                    $"ICameraResolver ainda indisponível em {context}. Tentarei novamente automaticamente.",
                    DebugUtility.Colors.Warning);
                return;
            }

            _warnedMissingResolver = true;

            DebugUtility.LogWarning<NewGameplayCameraBinder>(
                $"ICameraResolver não encontrado no DI global durante {context}. Vou tentar novamente automaticamente.",
                this);
        }

        private void TryRegisterCamera(string context)
        {
            if (_registered)
            {
                return;
            }

            if (_camera == null)
            {
                DebugUtility.LogError<NewGameplayCameraBinder>(
                    "NewGameplayCameraBinder requer um componente Camera no mesmo GameObject.",
                    this);
                return;
            }

            TryResolveResolver(context);

            if (_resolver == null)
            {
                return;
            }

            _resolver.RegisterCamera(playerId, _camera);
            _registered = true;

            DebugUtility.Log<NewGameplayCameraBinder>(
                $"Gameplay camera registrada (playerId={playerId}): {_camera.name}.",
                DebugUtility.Colors.Info);
        }

        private void TryUnregisterCamera()
        {
            if (!_registered)
            {
                return;
            }

            if (_resolver != null && _camera != null)
            {
                _resolver.UnregisterCamera(playerId, _camera);
            }

            _registered = false;
        }
    }
}
