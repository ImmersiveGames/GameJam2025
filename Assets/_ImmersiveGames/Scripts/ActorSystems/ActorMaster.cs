using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ActorSystems
{
    [DefaultExecutionOrder(-10), DebugLevel(DebugLevel.Verbose)]
    public class ActorMaster : MonoBehaviour, IActor, IHasSkin, IResettable
    {
        [Header("Actor Identity")]
        [SerializeField] private string customActorId;

        private ModelRoot _modelRoot;
        private string _actorId;

        public string ActorId => _actorId ?? customActorId ?? gameObject.name;
        public string ActorName => gameObject.name;
        public Transform Transform => transform;
        public bool IsActive { get; set; }
        public ModelRoot ModelRoot => _modelRoot ??= this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
        public Transform ModelTransform => ModelRoot.transform;

        private EventBinding<DeathEvent> _deathBinding;
        private EventBinding<DamageEvent> _damageBinding;
        private EventBinding<ReviveEvent> _reviveBinding;
        private EventBinding<ResetEvent> _resetBinding;

        protected virtual void Awake()
        {
            GenerateActorId();
            Reset();
            InitializeBaseComponents();

            // Registrar com FilteredEventBus usando o ActorId como scope
            _deathBinding = new EventBinding<DeathEvent>(OnDeath);
            _damageBinding = new EventBinding<DamageEvent>(OnDamage);
            _reviveBinding = new EventBinding<ReviveEvent>(OnRevive);
            _resetBinding = new EventBinding<ResetEvent>(OnReset);

            FilteredEventBus<DeathEvent>.Register(_deathBinding, ActorId);
            FilteredEventBus<DamageEvent>.Register(_damageBinding, ActorId);
            FilteredEventBus<ReviveEvent>.Register(_reviveBinding, ActorId);
            FilteredEventBus<ResetEvent>.Register(_resetBinding, ActorId);
        }

        private void OnDeath(DeathEvent e)
        {
            DebugUtility.LogVerbose<ActorMaster>($"💀 {ActorId} morreu por {e.ResourceType}!");
            SetSkinActive(false);
            IsActive = false;

            // Evento global (UI, pontuação, etc.)
            EventBus<DeathEvent>.Raise(e);
        }

        private void OnDamage(DamageEvent e)
        {
            DebugUtility.LogVerbose<ActorMaster>($"⚔️ {ActorId} recebeu {e.FinalDamage} de {e.AttackerId}");
            // Local para feedbacks visuais/áudio (commit futuro)
        }

        private void OnRevive(ReviveEvent e)
        {
            DebugUtility.LogVerbose<ActorMaster>($"❤️ {ActorId} reviveu!");
            SetSkinActive(true);
            IsActive = true;
        }

        private void OnReset(ResetEvent e)
        {
            DebugUtility.LogVerbose<ActorMaster>($"🔄 {ActorId} foi resetado!");
            Reset();
        }

        private void GenerateActorId()
        {
            if (!string.IsNullOrEmpty(customActorId))
            {
                _actorId = customActorId;
                return;
            }

            if (!DependencyManager.Instance.TryGetGlobal(out IUniqueIdFactory factory))
            {
                factory = new UniqueIdFactory();
                DependencyManager.Instance.RegisterGlobal(factory);
            }

            _actorId = factory.GenerateId(gameObject, "");
            DebugUtility.LogVerbose<ActorMaster>($"Generated ActorId: {_actorId} for {ActorName}");
        }

        private void InitializeBaseComponents()
        {
            _modelRoot = this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
        }

        public virtual void Reset()
        {
            IsActive = true;
            _modelRoot = this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
            SetSkinActive(true);
        }

        public void SetSkinActive(bool active)
        {
            if (_modelRoot != null)
                _modelRoot.gameObject.SetActive(active);
        }

        protected virtual void OnDestroy()
        {
            // Desregistrar bindings individuais para evitar leaks
            FilteredEventBus<DeathEvent>.Unregister(_deathBinding, ActorId);
            FilteredEventBus<DamageEvent>.Unregister(_damageBinding, ActorId);
            FilteredEventBus<ReviveEvent>.Unregister(_reviveBinding, ActorId);
            FilteredEventBus<ResetEvent>.Unregister(_resetBinding, ActorId);
        }
    }
}
