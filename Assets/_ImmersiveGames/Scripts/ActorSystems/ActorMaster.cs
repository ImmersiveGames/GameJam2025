﻿using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Tags;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    [DefaultExecutionOrder(-10)]
    public abstract class ActorMaster : MonoBehaviour, IResettable
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
            if (!modelRoot)
            {
                modelRoot = GetComponentInChildren<ModelRoot>();
            }
            return modelRoot;
        }
        
        public CanvasRoot GetCanvasRoot()
        {
            if (!canvasRoot)
            {
                canvasRoot = GetComponentInChildren<CanvasRoot>();
            }
            return canvasRoot;
        }
        
        public FxRoot GetFxRoot()
        {
            if (!fxRoot)
            {
                fxRoot = GetComponentInChildren<FxRoot>();
            }
            return fxRoot;
        }
        public abstract void Reset();
    }
}