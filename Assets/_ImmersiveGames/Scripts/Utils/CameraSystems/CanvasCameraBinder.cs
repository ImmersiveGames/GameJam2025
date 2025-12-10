using _ImmersiveGames.Scripts.CameraSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.CameraSystems
{
    /// <summary>
    /// Vincula automaticamente a câmera de gameplay (vinda do ICameraResolver)
    /// ao Canvas em modo WorldSpace. Suporta troca de câmera em runtime.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraBinder : MonoBehaviour
    {
        #region Private Fields

        private Canvas _canvas;
        private ICameraResolver _resolver;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();

            if (!DependencyManager.Provider.TryGetGlobal(out _resolver))
            {
                DebugUtility.LogError<CanvasCameraBinder>(
                    $"[{name}] CameraResolverService não encontrado. CanvasCameraBinder desativado.");
                enabled = false;
                return;
            }

            _resolver.OnDefaultCameraChanged += OnCameraChanged;
        }

        private void OnEnable()
        {
            // Ao habilitar, já tenta vincular a câmera atual
            BindCamera();
        }

        private void OnDisable()
        {
            // Se o componente for desabilitado, não queremos mais receber eventos
            if (_resolver != null)
            {
                _resolver.OnDefaultCameraChanged -= OnCameraChanged;
            }
        }

        private void OnDestroy()
        {
            // Segurança extra para casos de destruição direta do GameObject
            if (_resolver != null)
            {
                _resolver.OnDefaultCameraChanged -= OnCameraChanged;
            }

            _canvas = null;
        }

        #endregion

        #region Camera Binding Logic

        private void BindCamera()
        {
            // Se o Canvas já foi destruído ou não existe, não faz nada
            if (_canvas == null)
                return;

            // Só faz sentido em WorldSpace
            if (_canvas.renderMode != RenderMode.WorldSpace)
                return;

            if (_resolver == null)
                return;

            var canvasWorldCamera = _resolver.GetDefaultCamera();

            if (canvasWorldCamera == null)
            {
                DebugUtility.LogWarning<CanvasCameraBinder>(
                    $"[{name}] Nenhuma câmera registrada no CameraResolverService.");
                return;
            }

            _canvas.worldCamera = canvasWorldCamera;
        }

        private void OnCameraChanged(Camera newCamera)
        {
            // Este callback pode ser chamado depois que o Canvas for destruído,
            // então precisamos garantir que ainda existe antes de acessar.
            if (_canvas == null)
                return;

            if (_canvas.renderMode != RenderMode.WorldSpace)
                return;

            _canvas.worldCamera = newCamera;
        }

        #endregion
    }
}
