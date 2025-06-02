using _ImmersiveGames.Scripts.Tags;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    [DefaultExecutionOrder(-10)]
    public abstract class ActorMaster : MonoBehaviour
    {
        [SerializeField] private ModelRoot modelRoot;
        [SerializeField] private CanvasRoot canvasRoot;
        [SerializeField] private FxRoot fxRoot;
        
        private void Awake()
        {
            modelRoot = GetComponentInChildren<ModelRoot>();
            canvasRoot = GetComponentInChildren<CanvasRoot>();
            fxRoot = GetComponentInChildren<FxRoot>();
        }
        
        public ModelRoot GetModelRoot()
        {
            if (modelRoot == null)
            {
                modelRoot = GetComponentInChildren<ModelRoot>();
            }
            return modelRoot;
        }
        
        public CanvasRoot GetCanvasRoot()
        {
            if (canvasRoot == null)
            {
                canvasRoot = GetComponentInChildren<CanvasRoot>();
            }
            return canvasRoot;
        }
        
        public FxRoot GetFxRoot()
        {
            if (fxRoot == null)
            {
                fxRoot = GetComponentInChildren<FxRoot>();
            }
            return fxRoot;
        }
    }
}