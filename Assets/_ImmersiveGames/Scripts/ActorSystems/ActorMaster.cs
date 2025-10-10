using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ActorSystems
{
    [DefaultExecutionOrder(-10), DebugLevel(DebugLevel.Warning)]
    
    public class ActorMaster : MonoBehaviour, IActor, IHasSkin, IResettable
    {
        [Header("Actor Identity")]
        [SerializeField] private string customActorId;
        
        // Componentes base - sem referência a SkinController
        private ModelRoot _modelRoot;
        private string _actorId;

        // IActor Implementation - apenas identidade
        public string ActorId => _actorId ?? customActorId ?? gameObject.name;
        public string ActorName => gameObject.name;
        public Transform Transform => transform;
        public bool IsActive { get; set; }
        
        // IHasSkin Implementation - apenas contrato
        public ModelRoot ModelRoot => _modelRoot ??= this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
        public Transform ModelTransform => ModelRoot.transform;
        public void SetSkinActive(bool active)
        {
            if (_modelRoot != null)
            {
                _modelRoot.gameObject.SetActive(active);
            }
        }
        

        protected virtual void Awake()
        {
            GenerateActorId();
            Reset();
            InitializeBaseComponents();
        }
        
        // gerador de identidade
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
            // Garantir apenas os componentes base necessários
            _modelRoot = this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
        }

        public virtual void Reset()
        {
            IsActive = true;
            _modelRoot = this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
            SetSkinActive(true);
        }
    }
}