using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.InputSystem;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    // Spawna na inicialização
    [DebugLevel(DebugLevel.Logs)]
    public class InitializationTrigger : BaseTrigger
    {
        private readonly float _delay;
        private bool _hasSpawned;
        private float _timer;

        public InitializationTrigger(EnhancedTriggerData data) : base(data)
        {
            _delay = data.GetProperty("delay", 0f);
            if (_delay < 0f)
            {
                DebugUtility.LogError<InitializationTrigger>("Delay não pode ser negativo. Usando 0.");
                _delay = 0f;
            }
            _hasSpawned = false;
            _timer = _delay;
        }

        public override void Initialize(SpawnPoint spawnPointRef)
        {
            base.Initialize(spawnPointRef);
            DebugUtility.LogVerbose<InitializationTrigger>($"Inicializado com delay={_delay}s para '{spawnPoint.name}'.", "blue", spawnPoint);
        }

        public override bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = spawnPoint.transform.position;
            sourceObject = spawnPoint.gameObject;
            if (!isActive || _hasSpawned)
                return false;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _hasSpawned = true;
                DebugUtility.Log<InitializationTrigger>($"Spawn inicial disparado para '{spawnPoint.name}' na posição {triggerPosition}.", "green", spawnPoint);
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            base.Reset();
            _hasSpawned = false;
            _timer = _delay;
            DebugUtility.LogVerbose<InitializationTrigger>($"Resetado para '{spawnPoint?.name}' com delay={_delay}s.", "yellow", spawnPoint);
        }
    }

    // Spawna em intervalos regulares
    [DebugLevel(DebugLevel.Logs)]
    public class IntervalTrigger : TimedTrigger
    {
        private readonly float _interval;
        private readonly bool _startImmediately;

        public IntervalTrigger(EnhancedTriggerData data) : base(data)
        {
            _interval = data.GetProperty("interval", 2f);
            if (_interval <= 0f)
            {
                DebugUtility.LogError<IntervalTrigger>("Interval deve ser maior que 0. Usando 2s.");
                _interval = 2f;
            }
            _startImmediately = data.GetProperty("startImmediately", true);
            timer = _startImmediately ? 0f : _interval;
        }

        public override void Initialize(SpawnPoint spawnPointRef)
        {
            base.Initialize(spawnPointRef);
            DebugUtility.LogVerbose<IntervalTrigger>($"Inicializado com interval={_interval}s, startImmediately={_startImmediately} para '{spawnPoint.name}'.", "blue", spawnPoint);
        }

        protected override bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = spawnPoint.transform.position;
            sourceObject = spawnPoint.gameObject;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = _interval;
                DebugUtility.Log<IntervalTrigger>($"Trigger disparado para '{spawnPoint.name}' na posição {triggerPosition} a cada {_interval}s.", "green", spawnPoint);
                return true;
            }
            return false;
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
    private bool _wasPressedThisFrame;

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
            _action.canceled += OnActionCanceled;
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
        _wasPressedThisFrame = false;
    }

    public void Reset()
    {
        SetActive(true);
        DebugUtility.LogVerbose<InputSystemTrigger>($"Resetado para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
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
        DebugUtility.LogVerbose<InputSystemTrigger>($"Trigger {(active ? "ativado" : "desativado")} para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
    }

    public void OnDisable()
    {
        if (_action != null)
        {
            _action.Disable();
            _action.performed -= OnActionPerformed;
            _action.canceled -= OnActionCanceled;
        }
        DebugUtility.LogVerbose<InputSystemTrigger>($"OnDisable chamado para '{_spawnPoint?.name}'.", "yellow", _spawnPoint);
    }

    public bool IsActive => _isActive;
}

    // Spawna com predicado
    [DebugLevel(DebugLevel.Logs)]
    public class PredicateTrigger : TimedTrigger
    {
        private readonly float _checkInterval;
        private System.Func<SpawnPoint, bool> _predicate;

        public PredicateTrigger(EnhancedTriggerData data) : base(data)
        {
            _checkInterval = data.GetProperty("checkInterval", 0.5f);
            if (_checkInterval <= 0f)
            {
                DebugUtility.LogError<PredicateTrigger>("checkInterval deve ser maior que 0. Usando 0.5s.");
                _checkInterval = 0.5f;
            }
            timer = _checkInterval;
            _predicate = (_) => false;
        }

        public override void Initialize(SpawnPoint spawnPointRef)
        {
            base.Initialize(spawnPointRef);
            if (_predicate == null || !_predicate(spawnPoint))
            {
                DebugUtility.LogWarning<PredicateTrigger>("Predicado não configurado. Trigger não disparará até SetPredicate ser chamado.", spawnPoint);
            }
            DebugUtility.LogVerbose<PredicateTrigger>($"Inicializado com checkInterval={_checkInterval}s para '{spawnPoint.name}'.", "blue", spawnPoint);
        }

        protected override bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = spawnPoint.transform.position;
            sourceObject = spawnPoint.gameObject;

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = _checkInterval;
                if (_predicate(spawnPoint))
                {
                    DebugUtility.LogVerbose<PredicateTrigger>($"Spawn disparado por predicado em '{spawnPoint.name}' na posição {triggerPosition}.", "green", spawnPoint);
                    return true;
                }
            }
            return false;
        }

        public void SetPredicate(System.Func<SpawnPoint, bool> predicate)
        {
            _predicate = predicate ?? (_ => false);
            DebugUtility.LogVerbose<PredicateTrigger>($"Predicado configurado para '{spawnPoint?.name}'.", "blue", spawnPoint);
        }
    }
}