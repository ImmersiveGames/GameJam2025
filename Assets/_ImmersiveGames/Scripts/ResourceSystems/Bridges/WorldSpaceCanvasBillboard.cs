using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    /// <summary>
    /// Mantém o canvas sempre virado para a câmera (billboard effect)
    /// Para canvases em world space, com suporte a offset de posição/rotação
    /// </summary>
    public class WorldSpaceCanvasBillboard : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [SerializeField] private bool useMainCamera = true;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool billboardX = true;
        [SerializeField] private bool billboardY = true;
        [SerializeField] private bool billboardZ = true;
        [SerializeField] private bool invertForward = false;

        [Header("Offset Settings")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        [Header("Update Settings")]
        [SerializeField] private UpdateMethod updateMethod = UpdateMethod.LateUpdate;
        [SerializeField] private float updateInterval = 0f;

        private Transform cameraTransform;
        private float lastUpdateTime;
        private Vector3 originalPosition;
        private Quaternion originalRotation;

        public enum UpdateMethod
        {
            Update,
            LateUpdate,
            FixedUpdate
        }

        private void Start()
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            FindCamera();
        }

        private void FindCamera()
        {
            if (useMainCamera)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = FindObjectOfType<Camera>();
                }
            }

            if (targetCamera != null)
            {
                cameraTransform = targetCamera.transform;
            }
            else
            {
                Debug.LogWarning("WorldSpaceCanvasBillboard: No target camera found.");
            }
        }

        private void Update()
        {
            if (updateMethod == UpdateMethod.Update)
            {
                UpdateBillboard();
            }
        }

        private void LateUpdate()
        {
            if (updateMethod == UpdateMethod.LateUpdate)
            {
                UpdateBillboard();
            }
        }

        private void FixedUpdate()
        {
            if (updateMethod == UpdateMethod.FixedUpdate)
            {
                UpdateBillboard();
            }
        }

        private void UpdateBillboard()
        {
            if (cameraTransform == null)
            {
                FindCamera();
                if (cameraTransform == null) return;
            }

            // Verificar intervalo de update
            if (updateInterval > 0f && Time.time - lastUpdateTime < updateInterval)
            {
                return;
            }
            lastUpdateTime = Time.time;

            // Aplicar offset de posição
            Vector3 finalPosition = originalPosition + positionOffset;
            if (positionOffset != Vector3.zero)
            {
                transform.position = finalPosition;
            }

            // Calcular direção para a câmera
            Vector3 directionToCamera = cameraTransform.position - transform.position;

            // Aplicar restrições de eixo
            if (!billboardX) directionToCamera.x = 0f;
            if (!billboardY) directionToCamera.y = 0f;
            if (!billboardZ) directionToCamera.z = 0f;

            // Inverter se necessário
            if (invertForward) directionToCamera = -directionToCamera;

            // Aplicar rotação apenas se a direção for válida
            if (directionToCamera != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                
                // Aplicar offset de rotação
                if (rotationOffset != Vector3.zero)
                {
                    targetRotation *= Quaternion.Euler(rotationOffset);
                }

                transform.rotation = targetRotation;
            }
        }

        [ContextMenu("Find Camera Now")]
        private void FindCameraNow()
        {
            FindCamera();
            if (cameraTransform != null)
            {
                Debug.Log($"WorldSpaceCanvasBillboard: Found camera {cameraTransform.name}");
            }
        }

        [ContextMenu("Force Update")]
        private void ForceUpdate()
        {
            UpdateBillboard();
        }

        [ContextMenu("Save Current as Original")]
        private void SaveCurrentAsOriginal()
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            Debug.Log("Saved current position and rotation as original");
        }

        [ContextMenu("Reset to Original")]
        private void ResetToOriginal()
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            Debug.Log("Reset to original position and rotation");
        }

        public void SetPositionOffset(Vector3 newOffset)
        {
            positionOffset = newOffset;
            UpdateBillboard();
        }

        public void SetRotationOffset(Vector3 newOffset)
        {
            rotationOffset = newOffset;
            UpdateBillboard();
        }

        public void SetTargetCamera(Camera newCamera)
        {
            targetCamera = newCamera;
            if (targetCamera != null)
            {
                cameraTransform = targetCamera.transform;
            }
        }

        public void SetUseMainCamera(bool useMain)
        {
            useMainCamera = useMain;
            if (useMainCamera)
            {
                FindCamera();
            }
        }
    }
}