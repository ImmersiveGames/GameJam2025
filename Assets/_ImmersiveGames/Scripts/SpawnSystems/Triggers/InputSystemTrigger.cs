using _ImmersiveGames.Scripts.SpawnSystems.Data;
using _ImmersiveGames.Scripts.SpawnSystems.Events;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    [DebugLevel(DebugLevel.Logs)]
    public class InputSystemTriggerOld : ISpawnTriggerOld
    {
        private readonly string _actionName;
        private readonly InputAction _action;
        private bool _isActive;
        private InputSpawnPoint _spawnPoint;
        private bool _wasPressedThisFrame;

        public InputSystemTriggerOld(EnhancedTriggerData data, InputActionAsset inputAsset)
        {
            _actionName = data.GetProperty("actionName", "Fire");
            if (string.IsNullOrEmpty(_actionName))
            {
                DebugUtility.LogError<InputSystemTriggerOld>("actionName não pode ser vazio. Usando 'Fire'.", null);
                _actionName = "Fire";
            }
            _action = inputAsset?.FindAction(_actionName);
            if (_action == null)
            {
                DebugUtility.LogError<InputSystemTriggerOld>($"Ação '{_actionName}' não encontrada no InputActionAsset.", null);
            }
            _isActive = true;
            _wasPressedThisFrame = false;
        }

        public void Initialize(SpawnPoint spawnPointRef)
        {
            if (spawnPointRef is not InputSpawnPoint inputSpawnPoint)
            {
                DebugUtility.LogError<InputSystemTriggerOld>($"InputSystemTriggerOld requer InputSpawnPoint, não {spawnPointRef?.GetType().Name}.", spawnPointRef);
                _isActive = false;
                return;
            }
            _spawnPoint = inputSpawnPoint;
            if (_action != null)
            {
                _action.Enable();
                _action.performed += OnActionPerformed;
                _action.canceled += OnActionCanceled;
                DebugUtility.LogVerbose<InputSystemTriggerOld>($"Ação '{_actionName}' habilitada para '{_spawnPoint.name}'.", "blue", _spawnPoint);
            }
            else
            {
                DebugUtility.LogError<InputSystemTriggerOld>($"Ação '{_actionName}' é nula. InputSystemTriggerOld não funcionará.", _spawnPoint);
                _isActive = false;
            }
            DebugUtility.LogVerbose<InputSystemTriggerOld>($"Inicializado com actionName='{_actionName}' para '{_spawnPoint.name}'.", "blue", _spawnPoint);
        }

        public bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = _spawnPoint?.transform.position;
            sourceObject = _spawnPoint?.gameObject;

            if (!_isActive || _spawnPoint == null || !_wasPressedThisFrame)
            {
                return false;
            }

            DebugUtility.LogVerbose<InputSystemTriggerOld>($"CheckTrigger retornou true para '{_spawnPoint.name}' na posição {triggerPosition}.", "cyan", _spawnPoint);
            return true;
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            if (!_isActive || _spawnPoint == null || _wasPressedThisFrame) return;
            _wasPressedThisFrame = true;

            FilteredEventBus<SpawnRequestEvent>.RaiseFiltered(
                new SpawnRequestEvent(_spawnPoint.GetPoolKey(), _spawnPoint.gameObject, _spawnPoint.transform.position),
                _spawnPoint
            );

            DebugUtility.Log<InputSystemTriggerOld>($"Trigger disparado por input '{_actionName}' em '{_spawnPoint.name}' na posição {_spawnPoint.transform.position}.", "green", _spawnPoint);
        }

        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            _wasPressedThisFrame = false;
            DebugUtility.LogVerbose<InputSystemTriggerOld>($"Input '{_actionName}' cancelado em '{_spawnPoint.name}'.", "yellow", _spawnPoint);
        }

        public void Reset()
        {
            SetActive(true);
            _wasPressedThisFrame = false;
            DebugUtility.LogVerbose<InputSystemTriggerOld>($"Resetado para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }

        public void SetActive(bool active)
        {
            if (_isActive == active) return;
            _isActive = active;
            if (_action != null)
            {
                if (active)
                {
                    _action.Enable();
                    _action.performed += OnActionPerformed;
                    _action.canceled += OnActionCanceled;
                }
                else
                {
                    _action.Disable();
                    _action.performed -= OnActionPerformed;
                    _action.canceled -= OnActionCanceled;
                }
            }
            DebugUtility.LogVerbose<InputSystemTriggerOld>($"Trigger {(active ? "ativado" : "desativado")} para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }

        public void OnDisable()
        {
            if (_action != null)
            {
                _action.Disable();
                _action.performed -= OnActionPerformed;
                _action.canceled -= OnActionCanceled;
            }
            _isActive = false;
            DebugUtility.LogVerbose<InputSystemTriggerOld>($"OnDisable chamado para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }

        public bool IsActive => _isActive;
    }
}