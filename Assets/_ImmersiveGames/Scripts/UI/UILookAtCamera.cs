using UnityEngine;

public class UILookAtCamera : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        // Obtém a câmera principal
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Faz o objeto olhar para a câmera
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }
}
