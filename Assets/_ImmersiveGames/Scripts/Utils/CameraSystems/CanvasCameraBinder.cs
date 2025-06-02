using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.CameraSystems
{
    [RequireComponent(typeof(Canvas))]
    public class CanvasCameraBinder : MonoBehaviour
    {
        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void OnEnable()
        {
            BindCamera();
        }

        private void BindCamera()
        {
            if (_canvas.renderMode != RenderMode.WorldSpace) return;

            if (Camera.main == null)
            {
                Debug.LogWarning($"[{name}] Nenhuma câmera com tag 'MainCamera' foi encontrada.");
                return;
            }

            _canvas.worldCamera = Camera.main;
        }
    }
}