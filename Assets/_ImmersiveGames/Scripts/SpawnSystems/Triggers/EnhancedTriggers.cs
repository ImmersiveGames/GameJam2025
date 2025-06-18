using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.SpawnSystems.DynamicPropertiesSystem;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    // Spawna na inicialização
    public class InitializationTrigger : ISpawnTrigger
    {
        private readonly float _delay;
        private bool _isActive;
        private bool _hasSpawned;
        private float _timer;
        private SpawnPoint _spawnPoint;

        public InitializationTrigger(EnhancedTriggerData data) // Corrigido para aceitar data
        {
            _delay = data.GetProperty("delay", 0f);
            _isActive = true;
            _hasSpawned = false;
            _timer = _delay;
        }

        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint;
        }

        public bool CheckTrigger(Vector3 origin)
        {
            if (!_isActive || _hasSpawned) return false;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _hasSpawned = true;
                EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(_spawnPoint.GetPoolKey(), _spawnPoint.gameObject));
                DebugUtility.Log<InitializationTrigger>($"Spawn inicial disparado para '{_spawnPoint.name}'.", "green", _spawnPoint);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _hasSpawned = false;
            _timer = _delay;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }
    }

    // Spawna em intervalos regulares
    public class IntervalTrigger : ISpawnTrigger
    {
        private readonly float _interval;
        private readonly bool _startImmediately;
        private float _timer;
        private bool _isActive;
        private SpawnPoint _spawnPoint;

        public IntervalTrigger(EnhancedTriggerData data)
        {
            _interval = data.GetProperty("interval", 2f);
            _startImmediately = data.GetProperty("startImmediately", true);
            _timer = _startImmediately ? 0f : _interval;
            _isActive = true;
        }

        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint;
        }

        public bool CheckTrigger(Vector3 origin)
        {
            if (!_isActive) return false;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _interval;
                EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(_spawnPoint.GetPoolKey(), _spawnPoint.gameObject));
                DebugUtility.Log<IntervalTrigger>($"Trigger disparado para '{_spawnPoint.name}' a cada {_interval}s.", "green", _spawnPoint);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _timer = _startImmediately ? 0f : _interval;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }
    }

    // Spawna com input
    public class InputSystemTrigger : ISpawnTrigger
    {
        private readonly string _actionName;
        private readonly InputAction _action;
        private bool _isActive;
        private InputSpawnPoint _spawnPoint; // Alterado para InputSpawnPoint

        public InputSystemTrigger(EnhancedTriggerData data, InputActionAsset inputAsset)
        {
            _actionName = data.GetProperty("actionName", "Fire");
            _action = inputAsset?.FindAction(_actionName);
            if (_action == null)
            {
                DebugUtility.LogError<InputSystemTrigger>($"Ação '{_actionName}' não encontrada no InputActionAsset.", null);
            }
            _isActive = true;
        }

        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPoint)
        {
            if (!(spawnPoint is InputSpawnPoint inputSpawnPoint))
            {
                DebugUtility.LogError<InputSystemTrigger>($"InputSystemTrigger só pode ser usado com InputSpawnPoint, não com {spawnPoint.GetType().Name}.", spawnPoint);
                _isActive = false;
                return;
            }
            _spawnPoint = inputSpawnPoint;
            _action?.Enable();
            _action!.performed += OnActionPerformed;
        }

        public bool CheckTrigger(Vector3 origin)
        {
            return false; // Lógica movida para OnActionPerformed
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            if (!_isActive || _spawnPoint == null) return;
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(_spawnPoint.GetPoolKey(), _spawnPoint.gameObject));
            DebugUtility.Log<InputSystemTrigger>($"Trigger disparado por input '{_actionName}' em '{_spawnPoint.name}'.", "green", _spawnPoint);
        }

        public void Reset()
        {
            // Não precisa de reset
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
    }

    // Spawna com evento global
    public class GlobalEventTrigger : ISpawnTrigger
    {
        private readonly string _eventName;
        private bool _isActive;
        private SpawnPoint _spawnPoint;
        private EventBinding<GlobalSpawnEvent> _eventBinding;

        public GlobalEventTrigger(EnhancedTriggerData data) // Corrigido para aceitar data
        {
            _eventName = data.GetProperty("eventName", "GlobalSpawnEvent");
            _isActive = true;
        }

        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint;
            _eventBinding = new EventBinding<GlobalSpawnEvent>(OnGlobalEvent);
            EventBus<GlobalSpawnEvent>.Register(_eventBinding);
        }

        public bool CheckTrigger(Vector3 origin)
        {
            return false; // Lógica movida para evento
        }

        private void OnGlobalEvent(GlobalSpawnEvent evt)
        {
            if (!_isActive || evt.EventName != _eventName) return;
            EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(_spawnPoint.GetPoolKey(), _spawnPoint.gameObject));
            DebugUtility.Log<GlobalEventTrigger>($"Spawn disparado por evento '{_eventName}' em '{_spawnPoint.name}'.", "green", _spawnPoint);
        }

        public void Reset()
        {
            // Não precisa de reset
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (_eventBinding != null)
            {
                if (active) EventBus<GlobalSpawnEvent>.Register(_eventBinding);
                else EventBus<GlobalSpawnEvent>.Unregister(_eventBinding);
            }
        }
    }

    // Spawna com predicado
    public class PredicateTrigger : ISpawnTrigger
    {
        private readonly float _checkInterval;
        private float _timer;
        private bool _isActive;
        private SpawnPoint _spawnPoint;
        private System.Func<bool> _predicate;

        public PredicateTrigger(EnhancedTriggerData data) // Removido Func<bool> do construtor
        {
            _checkInterval = data.GetProperty("checkInterval", 0.5f);
            _timer = _checkInterval;
            _isActive = true;
            _predicate = () => false; // Padrão: falso até configurado
        }

        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPoint)
        {
            _spawnPoint = spawnPoint;
        }

        public bool CheckTrigger(Vector3 origin)
        {
            if (!_isActive) return false;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _checkInterval;
                if (_predicate())
                {
                    EventBus<SpawnRequestEvent>.Raise(new SpawnRequestEvent(_spawnPoint.GetPoolKey(), _spawnPoint.gameObject));
                    DebugUtility.Log<PredicateTrigger>($"Spawn disparado por predicado em '{_spawnPoint.name}'.", "green", _spawnPoint);
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            _timer = _checkInterval;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        public void SetPredicate(System.Func<bool> predicate)
        {
            _predicate = predicate ?? (() => false);
        }
    }
}