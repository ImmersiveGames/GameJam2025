using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    [DefaultExecutionOrder(-10)]
    public abstract class ActorMaster : MonoBehaviour, IResettable
    {
        [SerializeField] private ModelRoot modelRoot;
        [SerializeField] private CanvasRoot canvasRoot;
        [SerializeField] private FxRoot fxRoot;
        public bool IsActive { get; set; } // Estado do Actor (ativo/inativo)
        protected virtual void Awake()
        {
            modelRoot = this.GetOrCreateComponentInChild<ModelRoot>("ModelRoot");
            canvasRoot = this.GetOrCreateComponentInChild<CanvasRoot>("CanvasRoot");
            fxRoot = this.GetOrCreateComponentInChild<FxRoot>("FxRoot");
            IsActive = true;
        }
        public ModelRoot GetModelRoot() => modelRoot;


        public CanvasRoot GetCanvasRoot() => canvasRoot;

        public FxRoot GetFxRoot() => fxRoot;
        
        public abstract void Reset();
        
    }
}