using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems.Presentation.Bridges
{
    /// <summary>
    /// Mantém o attributeCanvas sempre virado para a câmera (billboard effect)
    /// Para canvases em world space, com suporte a offset de posição/rotação
    /// </summary>
    public class RuntimeAttributeWorldSpaceCanvasBillboard : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [SerializeField] private bool useMainCamera = true;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool billboardX = true;
        [SerializeField] private bool billboardY = true;
        [SerializeField] private bool billboardZ = true;
        [SerializeField] private bool invertForward;

        [Header("Offset Settings")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        [Header("Update Settings")]
        [SerializeField] private UpdateMethod updateMethod = UpdateMethod.LateUpdate;
        [SerializeField] private float updateInterval;

        private Transform _cameraTransform;
        private float _lastUpdateTime;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;

        private enum UpdateMethod
        {
            Update,
            LateUpdate,
            FixedUpdate
        }

        private void Start()
        {
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            FindCamera();
        }

        private void FindCamera()
        {
            if (useMainCamera)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    targetCamera = FindFirstObjectByType<Camera>();
                }
            }

            if (targetCamera != null)
            {
                _cameraTransform = targetCamera.transform;
            }
            else
            {
                DebugUtility.LogWarning<RuntimeAttributeWorldSpaceCanvasBillboard>("RuntimeAttributeWorldSpaceCanvasBillboard: No target camera found.");
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
            if (_cameraTransform == null)
            {
                FindCamera();
                if (_cameraTransform == null) return;
            }

            // Verificar intervalo de update
            if (updateInterval > 0f && Time.time - _lastUpdateTime < updateInterval)
            {
                return;
            }
            _lastUpdateTime = Time.time;

            // Aplicar offset de posição
            var finalPosition = _originalPosition + positionOffset;
            if (positionOffset != Vector3.zero)
            {
                transform.position = finalPosition;
            }

            // Calcular direção para a câmera
            var directionToCamera = _cameraTransform.position - transform.position;

            // Aplicar restrições de eixo
            if (!billboardX) directionToCamera.x = 0f;
            if (!billboardY) directionToCamera.y = 0f;
            if (!billboardZ) directionToCamera.z = 0f;

            // Inverter se necessário
            if (invertForward) directionToCamera = -directionToCamera;

            // Aplicar rotação apenas se a direção for válida
            if (directionToCamera == Vector3.zero) return;
            var targetRotation = Quaternion.LookRotation(directionToCamera);
                
            // Aplicar offset de rotação
            if (rotationOffset != Vector3.zero)
            {
                targetRotation *= Quaternion.Euler(rotationOffset);
            }

            transform.rotation = targetRotation;
        }

        [ContextMenu("Find Camera Now")]
        private void FindCameraNow()
        {
            FindCamera();
            if (_cameraTransform != null)
            {
                DebugUtility.LogVerbose<RuntimeAttributeWorldSpaceCanvasBillboard>($"RuntimeAttributeWorldSpaceCanvasBillboard: Found camera {_cameraTransform.name}");
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
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            DebugUtility.LogVerbose<RuntimeAttributeWorldSpaceCanvasBillboard>("Saved current position and rotation as original");
        }

        [ContextMenu("Reset to Original")]
        private void ResetToOriginal()
        {
            transform.position = _originalPosition;
            transform.rotation = _originalRotation;
            DebugUtility.LogVerbose<RuntimeAttributeWorldSpaceCanvasBillboard>("Reset to original position and rotation");
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
                _cameraTransform = targetCamera.transform;
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