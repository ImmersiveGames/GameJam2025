using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.CameraSystems
{
    /// <summary>
    /// Responsável por registrar a câmera de gameplay no CameraResolver.
    /// Deve ser colocado na câmera principal da GameplayScene.
    /// </summary>
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public class GameplayCameraBinder : MonoBehaviour
    {
        [SerializeField] private int playerId = 0;

        private ICameraResolver _resolver;
        private Camera _camera;

        private bool _registered;
        private bool _triedResolve;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (_camera == null)
            {
                DebugUtility.LogError<GameplayCameraBinder>("GameplayCameraBinder exige um componente Camera no mesmo GameObject.");
            }

            // Não registra aqui. Apenas tenta resolver (pode falhar dependendo da ordem de bootstrap).
            TryResolve();
        }

        private void Start()
        {
            // Caso o resolver global ainda não existisse no Awake, tenta novamente no Start.
            if (_resolver == null)
            {
                TryResolve();
            }

            // Se o objeto já está enabled (normal), OnEnable já terá tentado registrar.
            // Mas se a resolução só ficou pronta no Start, garantimos o registro aqui.
            TryRegister();
        }

        private void OnEnable()
        {
            TryRegister();
        }

        private void OnDisable()
        {
            TryUnregister();
        }

        private void TryResolve()
        {
            if (_triedResolve && _resolver != null)
            {
                return;
            }

            _triedResolve = true;

            if (!DependencyManager.Provider.TryGetGlobal(out _resolver) || _resolver == null)
            {
                // Não é erro fatal; depende da ordem de bootstrap global.
                DebugUtility.LogWarning<GameplayCameraBinder>(
                    "CameraResolverService não encontrado no DI global (ainda). Vou tentar novamente no Start.",
                    this);

                _resolver = null;
            }
        }

        private void TryRegister()
        {
            if (_registered)
            {
                return;
            }

            if (_camera == null)
            {
                return;
            }

            if (_resolver == null)
            {
                // Tenta resolver caso ainda não tenha sido possível.
                TryResolve();
                if (_resolver == null)
                {
                    return;
                }
            }

            _resolver.RegisterCamera(playerId, _camera);
            _registered = true;

            DebugUtility.Log<GameplayCameraBinder>(
                $"Câmera registrada como Player {playerId}: {_camera.name}",
                DebugUtility.Colors.Info);
        }

        private void TryUnregister()
        {
            if (!_registered)
            {
                return;
            }

            if (_resolver == null || _camera == null)
            {
                _registered = false;
                return;
            }

            _resolver.UnregisterCamera(playerId, _camera);
            _registered = false;
        }
    }
}
