using UnityEngine;
namespace _ImmersiveGames.Scripts.UISystems
{
    public class UILookAtCamera : MonoBehaviour
    {
        private Camera _mainCamera;

        private void Start()
        {
            // Obt�m a c�mera principal
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera != null)
            {
                // Faz o objeto olhar para a c�mera
                transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}
