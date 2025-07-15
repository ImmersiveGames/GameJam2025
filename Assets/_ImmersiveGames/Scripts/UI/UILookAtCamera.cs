using UnityEngine;
namespace _ImmersiveGames.Scripts.UI
{
    public class UILookAtCamera : MonoBehaviour
    {
        private Camera mainCamera;

        void Start()
        {
            // Obt�m a c�mera principal
            mainCamera = Camera.main;
        }

        void LateUpdate()
        {
            if (mainCamera != null)
            {
                // Faz o objeto olhar para a c�mera
                transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                    mainCamera.transform.rotation * Vector3.up);
            }
        }
    }
}
