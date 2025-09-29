using _ImmersiveGames.Scripts.GameManagerSystems.Events;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ActorSystems
{
    [DefaultExecutionOrder(-10)]
    public class ActorMaster : MonoBehaviour, IActor, IHasSkin, IResettable
    {
        [SerializeField] private string customActorId; // Optional manual override for ActorId

        private string _actorId;
        public string ActorId => _actorId ?? customActorId ?? gameObject.name; // Fallback to name if not generated
        
        public Transform Transform => transform;
        private ModelRoot _modelRoot;
        public string ActorName => gameObject.name;
        public bool IsActive { get; set; }
        public ModelRoot ModelRoot => _modelRoot ??= this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
        public Transform ModelTransform => ModelRoot.transform;

        private EventBinding<ActorStateChangedEvent> _actorStateChangedEvent;

        protected virtual void Awake()
        {
            GenerateActorId();
            Reset();
            RegisterEventListeners();
        }

        private void OnDestroy()
        {
            UnregisterEventListeners();
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
                DependencyManager.Instance.RegisterGlobal<IUniqueIdFactory>(factory);
            }

            _actorId = factory.GenerateId(gameObject, "");
            DebugUtility.LogVerbose<ActorMaster>($"Generated ActorId: {_actorId} for {ActorName}");
        }

        private void RegisterEventListeners()
        {
            _actorStateChangedEvent = new EventBinding<ActorStateChangedEvent>(OnActorStateChanged);
            EventBus<ActorStateChangedEvent>.Register(_actorStateChangedEvent);
        }

        private void UnregisterEventListeners()
        {
            EventBus<ActorStateChangedEvent>.Unregister(_actorStateChangedEvent);
        }

        private void OnActorStateChanged(ActorStateChangedEvent evt)
        {
            IsActive = evt.IsActive;
            DebugUtility.LogVerbose<ActorMaster>($"Ator {ActorName} atualizado: IsActive = {IsActive}");
        }

        public void SetSkinActive(bool active)
        {
            if (_modelRoot != null)
            {
                _modelRoot.gameObject.SetActive(active);
            }
        }

        public virtual void Reset(bool resetSkin = false)
        {
            IsActive = true;
            _modelRoot = this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
            SetSkinActive(resetSkin);
        }
    }
}