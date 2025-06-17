using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [DebugLevel(DebugLevel.Warning)]
    public class InputSystemHoldTrigger : ISpawnTrigger
    {
        private readonly string _actionName;
        private InputAction _action;
        private readonly float _interval;
        private bool _isActive = true;
        private SpawnPoint _spawnPoint;
        private float _lastTriggerTime;

        public InputSystemHoldTrigger(string actionName, InputActionAsset inputAsset, float interval = 0.5f)
        {
            _actionName = actionName;
            _interval = interval;
            if (string.IsNullOrEmpty(actionName))
            {
                DebugUtility.LogError<InputSystemHoldTrigger>("actionName está vazio.");
                return;
            }
            if (inputAsset == null)
            {
                DebugUtility.LogError<InputSystemHoldTrigger>("InputActionAsset é nulo.");
                return;
            }
            _action = inputAsset.FindAction(actionName);
            if (_action == null)
            {
                DebugUtility.LogError<InputSystemHoldTrigger>($"Ação '{actionName}' não encontrada no InputActionAsset.");
                return;
            }
            _action.Enable();
            DebugUtility.Log<InputSystemHoldTrigger>($"Ação '{actionName}' inicializada com intervalo {_interval}.", "green");
        }

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint ?? throw new System.ArgumentNullException(nameof(spawnPoint));
            _lastTriggerTime = Time.time - _interval; // Permite disparo imediato
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive || _action == null || !_action.IsPressed() || Time.time < _lastTriggerTime + _interval)
                return false;

            _lastTriggerTime = Time.time;
            DebugUtility.Log<InputSystemHoldTrigger>($"Disparando SpawnRequestEvent com input segurado '{_actionName}' em {origin}", "green");
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(data.PoolableData.ObjectName, origin, data));
            return true;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (_action != null)
            {
                if (active) _action.Enable();
                else _action.Disable();
            }
        }

        public void Reset()
        {
            _isActive = true;
            _lastTriggerTime = Time.time - _interval; // Reseta para permitir disparo imediato
        }
    }
}