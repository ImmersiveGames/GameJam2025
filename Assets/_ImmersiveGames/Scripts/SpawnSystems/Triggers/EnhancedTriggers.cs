using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    // Spawna na inicialização
    [DebugLevel(DebugLevel.Logs)]
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
            if (_delay < 0f)
            {
                DebugUtility.LogError<InitializationTrigger>("Delay não pode ser negativo. Usando 0.");
                _delay = 0f;
            }
            _isActive = true;
            _hasSpawned = false;
            _timer = _delay;
        }

        public void ReArm()
        {
            //nope
        }
        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPointRef)
        {
            _spawnPoint = spawnPointRef ?? throw new System.ArgumentNullException(nameof(spawnPointRef));
            DebugUtility.LogVerbose<InitializationTrigger>($"Inicializado com delay={_delay}s para '{_spawnPoint.name}'.", "blue", _spawnPoint);
        }

        public bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = _spawnPoint.transform.position;
            sourceObject = _spawnPoint.gameObject;
            if (!_isActive || _hasSpawned)
            {
                return false;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _hasSpawned = true;
                DebugUtility.Log<InitializationTrigger>($"Spawn inicial disparado para '{_spawnPoint.name}' na posição {triggerPosition}.", "green", _spawnPoint);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _hasSpawned = false;
            _timer = _delay;
            DebugUtility.LogVerbose<InitializationTrigger>($"Resetado para '{_spawnPoint?.name}' com delay={_delay}s.", "yellow", _spawnPoint);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            DebugUtility.LogVerbose<InitializationTrigger>($"Trigger {(active ? "ativado" : "desativado")} para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }
    }

    // Spawna em intervalos regulares
    [DebugLevel(DebugLevel.Logs)]
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
            if (_interval <= 0f)
            {
                DebugUtility.LogError<IntervalTrigger>("Interval deve ser maior que 0. Usando 2s.");
                _interval = 2f;
            }
            _startImmediately = data.GetProperty("startImmediately", true);
            _timer = _startImmediately ? 0f : _interval;
            _isActive = true;
        }

        public void ReArm()
        {
            //nope
        }
        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPointRef)
        {
            _spawnPoint = spawnPointRef ?? throw new System.ArgumentNullException(nameof(spawnPointRef));
            DebugUtility.LogVerbose<IntervalTrigger>($"Inicializado com interval={_interval}s, startImmediately={_startImmediately} para '{_spawnPoint.name}'.", "blue", _spawnPoint);
        }

        public bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = _spawnPoint.transform.position;
            sourceObject = _spawnPoint.gameObject;
            if (!_isActive)
            {
                return false;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _interval;
                DebugUtility.Log<IntervalTrigger>($"Trigger disparado para '{_spawnPoint.name}' na posição {triggerPosition} a cada {_interval}s.", "green", _spawnPoint);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _timer = _startImmediately ? 0f : _interval;
            DebugUtility.LogVerbose<IntervalTrigger>($"Resetado para '{_spawnPoint?.name}' com timer={_timer}s.", "yellow", _spawnPoint);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            DebugUtility.LogVerbose<IntervalTrigger>($"Trigger {(active ? "ativado" : "desativado")} para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }
    }

    // Spawna com input
    [DebugLevel(DebugLevel.Logs)]
    public class InputSystemTrigger : ISpawnTrigger
    {
        private readonly string _actionName;
        private readonly InputAction _action;
        private bool _isActive;
        private InputSpawnPoint _spawnPoint;
        private bool _wasPressedThisFrame; // Controle de debounce

        public InputSystemTrigger(EnhancedTriggerData data, InputActionAsset inputAsset)
        {
            _actionName = data.GetProperty("actionName", "Fire");
            if (string.IsNullOrEmpty(_actionName))
            {
                DebugUtility.LogError<InputSystemTrigger>("actionName não pode ser vazio.");
                _actionName = "Fire";
            }
            _action = inputAsset?.FindAction(_actionName);
            if (_action == null)
            {
                DebugUtility.LogError<InputSystemTrigger>($"Ação '{_actionName}' não encontrada no InputActionAsset.");
            }
            _isActive = true;
            _wasPressedThisFrame = false;
        }

        public void ReArm()
        {
            // Não necessário
        }

        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPointRef)
        {
            if (spawnPointRef is not InputSpawnPoint inputSpawnPoint)
            {
                DebugUtility.LogError<InputSystemTrigger>($"InputSystemTrigger requer InputSpawnPoint, não {spawnPointRef?.GetType().Name}.", spawnPointRef);
                _isActive = false;
                return;
            }
            _spawnPoint = inputSpawnPoint;
            if (_action != null)
            {
                _action.Enable();
                _action.performed += OnActionPerformed;
                _action.canceled += OnActionCanceled; // Adiciona verificação de liberação
            }
            DebugUtility.LogVerbose<InputSystemTrigger>($"Inicializado com actionName='{_actionName}' para '{_spawnPoint.name}'.", "blue", _spawnPoint);
        }

        public bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = null;
            sourceObject = null;
            return false; // Lógica movida para OnActionPerformed
        }

        private void OnActionPerformed(InputAction.CallbackContext context)
        {
            if (!_isActive || _spawnPoint == null || _wasPressedThisFrame) return;
            _wasPressedThisFrame = true;
            FilteredEventBus.RaiseFiltered(
                new SpawnRequestEvent(_spawnPoint.GetPoolKey(), _spawnPoint.gameObject, _spawnPoint.transform.position)
            );
            DebugUtility.Log<InputSystemTrigger>($"Trigger disparado por input '{_actionName}' em '{_spawnPoint.name}' na posição {_spawnPoint.transform.position}.", "green", _spawnPoint);
        }

        private void OnActionCanceled(InputAction.CallbackContext context)
        {
            _wasPressedThisFrame = false; // Reseta o estado quando o botão é liberado
        }

        public void Reset()
        {
            DebugUtility.LogVerbose<InputSystemTrigger>($"Reset não necessário para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }

        public void SetActive(bool active)
        {
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
            DebugUtility.LogVerbose<InputSystemTrigger>($"Trigger {(active ? "ativado" : "desativado")} para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }
    }

    // Spawna com predicado
    [DebugLevel(DebugLevel.Logs)]
    public class PredicateTrigger : ISpawnTrigger
    {
        private readonly float _checkInterval;
        private float _timer;
        private bool _isActive;
        private SpawnPoint _spawnPoint;
        private System.Func<SpawnPoint, bool> _predicate;

        public PredicateTrigger(EnhancedTriggerData data) // Removido Func<bool> do construtor
        {
            _checkInterval = data.GetProperty("checkInterval", 0.5f);
            if (_checkInterval <= 0f)
            {
                DebugUtility.LogError<PredicateTrigger>("checkInterval deve ser maior que 0. Usando 0.5s.");
                _checkInterval = 0.5f;
            }
            _timer = _checkInterval;
            _isActive = true;
            _predicate = (_) => false; // Padrão: falso até configurado
        }

        public void ReArm()
        {
            //nope
        }
        public bool IsActive => _isActive;

        public void Initialize(SpawnPoint spawnPointRef)
        {
            _spawnPoint = spawnPointRef ?? throw new System.ArgumentNullException(nameof(spawnPointRef));
            if (_predicate == null || _predicate(_spawnPoint) == false)
            {
                DebugUtility.LogWarning<PredicateTrigger>("Predicado não configurado. Trigger não disparará até SetPredicate ser chamado.", _spawnPoint);
            }
            DebugUtility.LogVerbose<PredicateTrigger>($"Inicializado com checkInterval={_checkInterval}s para '{_spawnPoint.name}'.", "blue", _spawnPoint);
        }

        public bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = _spawnPoint.transform.position;
            sourceObject = _spawnPoint.gameObject;
            if (!_isActive)
            {
                return false;
            }

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = _checkInterval;
                if (_predicate(_spawnPoint))
                {
                    DebugUtility.LogVerbose<PredicateTrigger>($"Spawn disparado por predicado em '{_spawnPoint.name}' na posição {triggerPosition}.", "green", _spawnPoint);
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            _timer = _checkInterval;
            DebugUtility.LogVerbose<PredicateTrigger>($"Resetado para '{_spawnPoint?.name}' com checkInterval={_checkInterval}s.", "yellow", _spawnPoint);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            DebugUtility.LogVerbose<PredicateTrigger>($"Trigger {(active ? "ativado" : "desativado")} para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
        }

        public void SetPredicate(System.Func<SpawnPoint, bool> predicate)
        {
            _predicate = predicate ?? (_ => false);
            DebugUtility.LogVerbose<PredicateTrigger>($"Predicado configurado para '{_spawnPoint?.name}'.", "blue", _spawnPoint);
        }
    }
}