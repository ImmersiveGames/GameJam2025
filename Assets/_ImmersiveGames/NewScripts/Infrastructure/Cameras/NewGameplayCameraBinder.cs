/*
 * ChangeLog
 * - Mantido registro/desregistro idempotente, evitando logs repetitivos ao resolver indisponível.
 * - Mantido retry em Awake/Start/OnEnable, com log único quando o resolver aparece após falha.
 * - Garantido desregistro também em OnDestroy (cobre casos de teardown/Domain reload).
 */
using _ImmersiveGames.NewScripts.Infrastructure.Debug;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Cameras
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class NewGameplayCameraBinder : MonoBehaviour
    {
        [SerializeField] private int playerId;

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

        private void Update()
        {
            if (!_registered)
            {
                TryRegisterCamera("Update");
            }
        }

        private void OnDisable()
        {
            TryUnregisterCamera();
        }

        private void OnDestroy()
        {
            TryUnregisterCamera();
        }

        private bool TryResolveResolver(string context)
        {
            if (_resolver != null)
            {
                return true;
            }

            if (DependencyManager.Provider.TryGetGlobal(out _resolver) && _resolver != null)
            {
                var message = _warnedMissingResolver
                    ? $"ICameraResolver resolvido após indisponibilidade em {context}."
                    : $"ICameraResolver resolved in {context}.";

                DebugUtility.LogVerbose<NewGameplayCameraBinder>(message, DebugUtility.Colors.Info);

                _warnedMissingResolver = false;
                return true;
            }

            if (!_warnedMissingResolver)
            {
                _warnedMissingResolver = true;

                DebugUtility.LogWarning<NewGameplayCameraBinder>(
                    $"ICameraResolver não encontrado no DI global durante {context}. Vou tentar novamente automaticamente.",
                    this);
            }

            return false;
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

            if (!TryResolveResolver(context))
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
