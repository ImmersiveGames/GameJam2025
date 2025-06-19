using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    [DefaultExecutionOrder(-10)]
    public abstract class ActorMaster : MonoBehaviour, IActor, IHasSkin, IResettable
    {
        public Transform Transform => transform;
        private ModelRoot _modelRoot;
        public string Name => gameObject.name;
        public bool IsActive { get; set; }
        public ModelRoot ModelRoot => _modelRoot ??= this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
        public Transform ModelTransform => ModelRoot.transform;
        
        protected virtual void Awake()
        {
            Reset();
        }
        
        public void SetSkinActive(bool active)
        {
            if (_modelRoot != null)
            {
                _modelRoot.gameObject.SetActive(active);
            }
        }

        public virtual void Reset()
        {
            IsActive = true;
            _modelRoot = this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
            SetSkinActive(true);
        }
    }
}