using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.ResourceSystems.Bind
{
    public class CanvasPipelineManager : PersistentSingleton<CanvasPipelineManager>, IInjectableComponent
    {
        private readonly Dictionary<string, ICanvasBinder> _canvasRegistry = new();
        
        // CORREÇÃO: Pending binds organizados por canvas e depois por ator+resourceType
        private readonly Dictionary<string, Dictionary<(string actorId, ResourceType resourceType), CanvasBindRequest>> _pendingBindsByCanvas = new();
        
        public DependencyInjectionState InjectionState { get; set; }

        public string GetObjectId() => "CanvasPipelineManager";
        
        public void OnDependenciesInjected()
        {
            InjectionState = DependencyInjectionState.Ready;
            DebugUtility.LogVerbose<CanvasPipelineManager>("✅ Pipeline Manager fully initialized with dependencies");
        }
    
        protected override void InitializeSingleton()
        {
            base.InitializeSingleton();
            ResourceInitializationManager.Instance.RegisterForInjection(this);
            DebugUtility.LogVerbose<CanvasPipelineManager>("✅ Canvas Pipeline Manager Ready");
        }
    
        public void RegisterCanvas(ICanvasBinder canvas)
        {
            if (canvas == null) return;
            
            if (!_canvasRegistry.TryAdd(canvas.CanvasId, canvas)) 
            {
                DebugUtility.LogWarning<CanvasPipelineManager>($"Canvas '{canvas.CanvasId}' already registered in pipeline");
                return;
            }

            ProcessPendingBindsForCanvas(canvas.CanvasId);
            DebugUtility.LogVerbose<CanvasPipelineManager>($"✅ Canvas '{canvas.CanvasId}' registered in pipeline");
        }

        public void UnregisterCanvas(string canvasId)
        {
            if (_canvasRegistry.Remove(canvasId))
            {
                _pendingBindsByCanvas.Remove(canvasId);
                DebugUtility.LogVerbose<CanvasPipelineManager>($"Canvas '{canvasId}' unregistered from pipeline");
            }
        }
    
        public void ScheduleBind(string actorId, ResourceType resourceType, IResourceValue data, string targetCanvasId)
        {
            var request = new CanvasBindRequest(actorId, resourceType, data, targetCanvasId);
        
            // Tentativa imediata
            if (TryExecuteBind(request))
            {
                DebugUtility.LogVerbose<CanvasPipelineManager>($"✅ Immediate bind: {actorId}.{resourceType} -> {targetCanvasId}");
                return;
            }
            
            // Cache para processamento posterior
            CachePendingBind(request);
            DebugUtility.LogVerbose<CanvasPipelineManager>($"📦 Cached bind: {actorId}.{resourceType} -> {targetCanvasId}");

            // Agendar cleanup
            StartCoroutine(CleanupStaleBind(request));
        }

        private bool TryExecuteBind(CanvasBindRequest request)
        {
            if (_canvasRegistry.TryGetValue(request.targetCanvasId, out var canvas) && 
                canvas.CanAcceptBinds())
            {
                canvas.ScheduleBind(request.actorId, request.resourceType, request.data);
                RemovePendingBind(request);
                return true;
            }
            return false;
        }

        private void CachePendingBind(CanvasBindRequest request)
        {
            if (!_pendingBindsByCanvas.ContainsKey(request.targetCanvasId))
            {
                _pendingBindsByCanvas[request.targetCanvasId] = new Dictionary<(string, ResourceType), CanvasBindRequest>();
            }
            
            var key = (request.actorId, request.resourceType);
            _pendingBindsByCanvas[request.targetCanvasId][key] = request;
        }

        private void RemovePendingBind(CanvasBindRequest request)
        {
            var key = (request.actorId, request.resourceType);
            if (_pendingBindsByCanvas.TryGetValue(request.targetCanvasId, out var canvasBinds))
            {
                canvasBinds.Remove(key);
                if (canvasBinds.Count == 0)
                {
                    _pendingBindsByCanvas.Remove(request.targetCanvasId);
                }
            }
        }
    
        private void ProcessPendingBindsForCanvas(string canvasId)
        {
            if (_pendingBindsByCanvas.TryGetValue(canvasId, out var canvasBinds))
            {
                DebugUtility.LogVerbose<CanvasPipelineManager>($"🔄 Processing {canvasBinds.Count} pending binds for canvas '{canvasId}'");
                
                foreach (var request in canvasBinds.Values.ToList())
                {
                    if (TryExecuteBind(request))
                    {
                        DebugUtility.LogVerbose<CanvasPipelineManager>($"✅ Processed cached bind: {request.actorId}.{request.resourceType}");
                    }
                }
            }
        }

        // CORREÇÃO: Novo método para limpar binds de um ator específico
        public void ClearActorBinds(string actorId)
        {
            var canvasesToProcess = _pendingBindsByCanvas.Keys.ToList();
            int totalRemoved = 0;

            foreach (var canvasId in canvasesToProcess)
            {
                var canvasBinds = _pendingBindsByCanvas[canvasId];
                var keysToRemove = canvasBinds.Keys.Where(key => key.actorId == actorId).ToList();
                
                foreach (var key in keysToRemove)
                {
                    canvasBinds.Remove(key);
                    totalRemoved++;
                }

                if (canvasBinds.Count == 0)
                {
                    _pendingBindsByCanvas.Remove(canvasId);
                }
            }

            if (totalRemoved > 0)
            {
                DebugUtility.LogVerbose<CanvasPipelineManager>($"🧹 Cleared {totalRemoved} pending binds for actor '{actorId}'");
            }
        }
    
        private IEnumerator CleanupStaleBind(CanvasBindRequest request, float timeout = 10f)
        {
            yield return new WaitForSeconds(timeout);
        
            // Verificar se o bind ainda está pendente
            if (_pendingBindsByCanvas.TryGetValue(request.targetCanvasId, out var canvasBinds))
            {
                var key = (request.actorId, request.resourceType);
                if (canvasBinds.ContainsKey(key))
                {
                    DebugUtility.LogWarning<CanvasPipelineManager>($"⏰ Stale bind removed: {request.actorId}.{request.resourceType} -> {request.targetCanvasId}");
                    canvasBinds.Remove(key);
                    
                    if (canvasBinds.Count == 0)
                    {
                        _pendingBindsByCanvas.Remove(request.targetCanvasId);
                    }
                }
            }
        }

        [ContextMenu("🔍 DEBUG PIPELINE")]
        public void DebugPipeline()
        {
            Debug.Log($"🎯 PIPELINE MANAGER DEBUG:");
            Debug.Log($"- Registered Canvases: {_canvasRegistry.Count}");
            foreach (var canvasId in _canvasRegistry.Keys)
            {
                var canvas = _canvasRegistry[canvasId];
                Debug.Log($"  - {canvasId} (State: {canvas.State}, AcceptsBinds: {canvas.CanAcceptBinds()})");
            }

            Debug.Log($"- Pending Binds: {_pendingBindsByCanvas.Sum(c => c.Value.Count)} total");
            foreach (var (canvasId, binds) in _pendingBindsByCanvas)
            {
                Debug.Log($"  - Canvas '{canvasId}': {binds.Count} binds");
                foreach (var ((actorId, resourceType), request) in binds)
                {
                    Debug.Log($"    - {actorId}.{resourceType}: {request.data?.GetCurrentValue():F1}");
                }
            }
        }
    }
}