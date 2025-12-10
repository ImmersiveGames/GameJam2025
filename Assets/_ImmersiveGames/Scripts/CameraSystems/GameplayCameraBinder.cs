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
    public class GameplayCameraBinder : MonoBehaviour
    {
        [SerializeField] private int playerId = 0;

        private ICameraResolver _resolver;
        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (!DependencyManager.Provider.TryGetGlobal(out _resolver))
            {
                DebugUtility.LogError<GameplayCameraBinder>("CameraResolverService não encontrado.");
                return;
            }

            Register();
        }

        private void OnEnable() => Register();
        private void OnDisable() => Unregister();

        private void Register()
        {
            if (_resolver == null || _camera == null) return;

            _resolver.RegisterCamera(playerId, _camera);

            DebugUtility.Log<GameplayCameraBinder>(
                $"Câmera registrada como Player {playerId}: {_camera.name}",
                DebugUtility.Colors.Info);
        }

        private void Unregister()
        {
            if (_resolver == null || _camera == null) return;

            _resolver.UnregisterCamera(playerId, _camera);
        }
    }
}