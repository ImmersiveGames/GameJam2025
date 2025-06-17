using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.SpawnSystems.Interfaces
{
    [DebugLevel(DebugLevel.Warning)]
    public class InputSystemTrigger : ISpawnTrigger
    {
        private readonly string _actionName;
        private InputAction _action;
        private bool _isActive = true;
        private SpawnPoint _spawnPoint;

        public InputSystemTrigger(string actionName, InputActionAsset inputAsset)
        {
            _actionName = actionName;
            if (string.IsNullOrEmpty(actionName))
            {
                DebugUtility.LogError<InputSystemTrigger>("actionName está vazio.");
                return;
            }
            if (inputAsset == null)
            {
                DebugUtility.LogError<InputSystemTrigger>("InputActionAsset é nulo.");
                return;
            }
            _action = inputAsset.FindAction(actionName);
            if (_action == null)
            {
                DebugUtility.LogError<InputSystemTrigger>($"Ação '{actionName}' não encontrada no InputActionAsset.");
                return;
            }
            _action.Enable();
            DebugUtility.Log<InputSystemTrigger>($"Ação '{actionName}' inicializada.", "green");
        }

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint ?? throw new System.ArgumentNullException(nameof(spawnPoint));
        }

        public bool CheckTrigger(Vector3 origin, SpawnData data)
        {
            if (!_isActive || _action == null || !_action.triggered) return false;
            DebugUtility.Log<InputSystemTrigger>($"Disparando SpawnRequestEvent com input '{_actionName}' em {origin}", "green");
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
        }
    }
}