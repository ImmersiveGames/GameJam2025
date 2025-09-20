using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.ResourceSystems.Events;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.Events;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [Serializable]
    public class ResourceThresholdUnityEvent : UnityEvent<float> { }

    [RequireComponent(typeof(IActor))]
    public abstract class ResourceSystem : MonoBehaviour, IResource
    {
        // ==============================
        // 🔹 Configuração
        // ==============================
        [SerializeField] protected ResourceConfigSo config;

        private string _uniqueId;
        private string _actorId;
        private bool _isGameActive;

        // ==============================
        // 🔹 Serviços internos
        // ==============================
        private ValueCore _valueCore;
        private ThresholdMonitor _thresholdMonitor;
        private ModifierManager _modifierManager;

        private ResourceAutoChangeService _autoFillService;
        private ResourceAutoChangeService _autoDrainService;

        // ==============================
        // 🔹 Eventos
        // ==============================
        public event Action EventDepleted;
        public event Action<float> EventValueChanged;
        public event Action<float> OnThresholdReached;

        [SerializeField] public ResourceThresholdUnityEvent onThresholdReached;

        private EventBinding<StateChangedEvent> _stateChangedBinding;

        // ==============================
        // 🔹 Ciclo de Vida Unity
        // ==============================
        protected virtual void Awake()
        {
            if (!config)
            {
                DebugUtility.LogError<ResourceSystem>("ResourceConfigSO não atribuído!", this);
                return;
            }

            _valueCore = new ValueCore(config);
            _thresholdMonitor = new ThresholdMonitor(config, _valueCore.GetState());
            _modifierManager = new ModifierManager(_uniqueId, gameObject, GetActorId());

            _uniqueId = UniqueIdFactory.Instance.GenerateId(gameObject, config.UniqueId);
            _actorId = GetActorId();

            DebugUtility.LogVerbose<ResourceSystem>(
                $"Awake: Inicializado ResourceSystem UniqueId={_uniqueId}, " +
                $"InitialValue={_valueCore.GetCurrentValue()}, MaxValue={_valueCore.GetMaxValue()}, Source={gameObject.name}");
        }

        protected virtual void OnEnable()
        {
            // Estratégias de auto-mudança
            if (config.AutoFillEnabled)
            {
                _autoFillService = new ResourceAutoChangeService(
                    new AutoFillStrategy(),
                    _valueCore,
                    _thresholdMonitor,
                    _modifierManager,
                    config,
                    _uniqueId,
                    gameObject,
                    _actorId);
            }

            if (config.AutoDrainEnabled)
            {
                _autoDrainService = new ResourceAutoChangeService(
                    new AutoDrainStrategy(),
                    _valueCore,
                    _thresholdMonitor,
                    _modifierManager,
                    config,
                    _uniqueId,
                    gameObject,
                    _actorId);
            }

            // Bind com o ciclo de jogo
            _stateChangedBinding = new EventBinding<StateChangedEvent>(OnStateChanged);
            EventBus<StateChangedEvent>.Register(_stateChangedBinding);

            DebugUtility.LogVerbose<ResourceSystem>(
                $"OnEnable: AutoFill={config.AutoFillEnabled} Rate={config.AutoFillRate}, " +
                $"AutoDrain={config.AutoDrainEnabled} Rate={config.AutoDrainRate}, Source={gameObject.name}");
        }

        private void Start()
        {
            EventBus<ResourceBindEvent>.Raise(new ResourceBindEvent(
                gameObject, config.ResourceType, _uniqueId, this, _actorId));

            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(
                _uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), true, _actorId));
        }

        protected virtual void Update()
        {
            if (!_isGameActive) return;

            _autoFillService?.Tick(Time.deltaTime);
            _autoDrainService?.Tick(Time.deltaTime);
            ApplyModifiers();
        }

        // ==============================
        // 🔹 Eventos Internos
        // ==============================
        private void OnStateChanged(StateChangedEvent evt)
        {
            _isGameActive = evt.IsGameActive;
        }

        private void ApplyModifiers()
        {
            float delta = _modifierManager.UpdateAndGetDelta(Time.deltaTime);

            if (delta == 0) return;

            if (delta > 0) _valueCore.Increase(delta);
            else _valueCore.Decrease(-delta);

            _thresholdMonitor.CheckThresholds();

            EventValueChanged?.Invoke(_valueCore.GetPercentage());
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(
                _uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), delta > 0, _actorId));
        }

        // ==============================
        // 🔹 API Pública
        // ==============================
        public void Increase(float amount)
        {
            _valueCore.Increase(amount);
            NotifyChange(true);

            if (_valueCore.GetCurrentValue() <= 0)
                OnResourceDepleted();
        }

        public void Decrease(float amount)
        {
            _valueCore.Decrease(amount);
            NotifyChange(false);

            if (_valueCore.GetCurrentValue() <= 0)
                OnResourceDepleted();
        }

        private void NotifyChange(bool isAscending)
        {
            EventValueChanged?.Invoke(_valueCore.GetPercentage());
            EventBus<ResourceValueChangedEvent>.Raise(new ResourceValueChangedEvent(
                _uniqueId, gameObject, config.ResourceType, _valueCore.GetPercentage(), isAscending, _actorId));
            _thresholdMonitor.CheckThresholds();
        }

        protected virtual void OnResourceDepleted() =>
            EventDepleted?.Invoke();

        public void CheckThresholds() =>
            _thresholdMonitor.CheckThresholds();

        public virtual void Reset(bool resetSkin = true)
        {
            _valueCore.SetCurrentValue(resetSkin ? config.InitialValue : _valueCore.GetCurrentValue());
            _thresholdMonitor = new ThresholdMonitor(config, _valueCore.GetState());
            _modifierManager.RemoveAllModifiers();

            _autoFillService?.Reset();
            _autoDrainService?.Reset();

            NotifyChange(true);
        }

        public void AddModifier(float amountPerSecond, float duration, bool isPermanent = false) =>
            _modifierManager.AddModifier(amountPerSecond, duration, isPermanent);

        public void RemoveAllModifiers() =>
            _modifierManager.RemoveAllModifiers();

        // ==============================
        // 🔹 Getters
        // ==============================
        public float GetCurrentValue() => _valueCore.GetCurrentValue();
        public float GetMaxValue() => _valueCore.GetMaxValue();
        public float GetPercentage() => _valueCore.GetPercentage();

        public ResourceConfigSo Config => config;
        public ResourceType Type => config.ResourceType;
        public string UniqueId => _uniqueId;
        public string ActorId => _actorId;
        public GameObject Source => gameObject;

        public string GetActorId()
        {
            var actor = GetComponentInParent<IActor>();
            return actor?.Name ?? string.Empty;
        }
    }
}
