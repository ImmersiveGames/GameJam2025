/*
 * ChangeLog
 * - Mantido registro/desregistro idempotente, evitando logs repetitivos ao resolver indisponível.
 * - Mantido retry em Awake/Start/OnEnable, com log único quando o resolver aparece após falha.
 * - Garantido desregistro também em OnDestroy (cobre casos de teardown/Domain reload).
 */

using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.Gameplay.Runtime.View;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Infrastructure.View.Bindings
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class GameplayCameraBinder : MonoBehaviour
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
                string message = _warnedMissingResolver
                    ? $"ICameraResolver resolvido após indisponibilidade em {context}."
                    : $"ICameraResolver resolved in {context}.";

                DebugUtility.LogVerbose<GameplayCameraBinder>(message, DebugUtility.Colors.Info);

                _warnedMissingResolver = false;
                return true;
            }

            if (!_warnedMissingResolver)
            {
                _warnedMissingResolver = true;

                DebugUtility.LogWarning<GameplayCameraBinder>(
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
                DebugUtility.LogError<GameplayCameraBinder>(
                    "GameplayCameraBinder requer um componente Camera no mesmo GameObject.",
                    this);
                return;
            }

            if (!TryResolveResolver(context))
            {
                return;
            }

            _resolver.RegisterCamera(playerId, _camera);
            _registered = true;

            DebugUtility.Log<GameplayCameraBinder>(
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

