using System.Collections;
using _ImmersiveGames.Scripts.ResourceSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    
    public class ResourceAutoFlowBridge : ResourceBridgeBase
    {
        [SerializeField] private bool startPaused = true;

        private ResourceAutoFlowService _autoFlow;
        private Coroutine _pendingResumeRoutine;
        private int _manualPauseCount;
        private int _automaticPauseCount;

        public bool HasAutoFlowService => _autoFlow != null;
        public bool IsAutoFlowActive => _autoFlow != null && !_autoFlow.IsPaused;

        protected override void OnServiceInitialized()
        {
            if (resourceSystem == null)
            {
                DebugUtility.LogWarning<ResourceAutoFlowBridge>($"ResourceSystem null em {actor.ActorId}");
                enabled = false;
                return;
            }

            if (!HasAutoFlowResources())
            {
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"Nenhum recurso AutoFlow em {actor.ActorId} — Bridge desativado.");
                enabled = false;
                return;
            }

            _autoFlow = new ResourceAutoFlowService(resourceSystem, startPaused);
            resourceSystem.ResourceChanging += HandleResourceChanging;
            resourceSystem.ResourceChanged += HandleResourceChanged;

            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"🚀 AutoFlow inicializado para {actor.ActorId}", null, this);

            if (!startPaused)
            {
                _autoFlow.Resume();
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"▶️ AutoFlow iniciado imediatamente para {actor.ActorId}", null, this);
            }
        }

        private void Update()
        {
            if (_autoFlow != null && IsInitialized && !_autoFlow.IsPaused)
            {
                _autoFlow.Process(Time.deltaTime);
            }
        }

        public bool ResumeAutoFlow()
        {
            if (_autoFlow == null)
            {
                return false;
            }

            if (_manualPauseCount > 0)
            {
                _manualPauseCount = Mathf.Max(0, _manualPauseCount - 1);
            }

            if (_manualPauseCount > 0)
            {
                return !_autoFlow.IsPaused;
            }

            if (_automaticPauseCount > 0)
            {
                return false;
            }

            if (_autoFlow.IsPaused)
            {
                _autoFlow.Resume();
                string actorId = actor?.ActorId ?? name;
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"▶️ AutoFlow retomado para {actorId}.", null, this);
            }

            return !_autoFlow.IsPaused;
        }

        public bool PauseAutoFlow()
        {
            if (_autoFlow == null)
            {
                return false;
            }

            _manualPauseCount = Mathf.Max(0, _manualPauseCount) + 1;
            CancelPendingResume();

            if (_autoFlow.IsPaused)
            {
                return true;
            }

            _autoFlow.Pause();
            string actorId = actor?.ActorId ?? name;
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"⏸️ AutoFlow pausado para {actorId}.", null, this);
            return true;
        }

        protected override void OnServiceDispose()
        {
            if (resourceSystem != null)
            {
                resourceSystem.ResourceChanging -= HandleResourceChanging;
                resourceSystem.ResourceChanged -= HandleResourceChanged;
            }

            CancelPendingResume();
            _manualPauseCount = 0;
            _automaticPauseCount = 0;

            _autoFlow?.Dispose();
            _autoFlow = null;
        }

        private bool HasAutoFlowResources()
        {
            foreach (var (type, _) in resourceSystem.GetAll())
            {
                var cfg = resourceSystem.GetInstanceConfig(type);
                if (cfg is { hasAutoFlow: true } && cfg.autoFlowConfig != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleResourceChanging(ResourceChangeContext context)
        {
            if (!ShouldReactToContext(context))
            {
                return;
            }

            _automaticPauseCount = Mathf.Max(0, _automaticPauseCount) + 1;
            CancelPendingResume();

            if (_autoFlow != null && !_autoFlow.IsPaused)
            {
                _autoFlow.Pause();
                string actorId = actor?.ActorId ?? name;
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"⏸️ AutoFlow pausado automaticamente ({context.ResourceType}, Δ={context.Delta:F2}) para {actorId}.", null, this);
            }
        }

        private void HandleResourceChanged(ResourceChangeContext context)
        {
            if (!ShouldReactToContext(context))
            {
                return;
            }

            _automaticPauseCount = Mathf.Max(0, _automaticPauseCount - 1);

            if (_automaticPauseCount > 0)
            {
                return;
            }

            if (_manualPauseCount > 0)
            {
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"⏸️ AutoFlow segue pausado manualmente após alteração de {context.ResourceType}.", null, this, deduplicate: true);
                return;
            }

            if (context.IsIncrease && context.ReachedMax)
            {
                DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"🔒 AutoFlow mantido em pausa porque {context.ResourceType} atingiu o valor máximo.", null, this);
                return;
            }

            ScheduleAutomaticResume();
        }

        private bool ShouldReactToContext(ResourceChangeContext context)
        {
            if (_autoFlow == null || !IsInitialized)
            {
                return false;
            }

            if (context.Source == ResourceChangeSource.AutoFlow)
            {
                return false;
            }

            return true;
        }

        private void ScheduleAutomaticResume()
        {
            if (_autoFlow == null || !_autoFlow.IsPaused)
            {
                return;
            }

            CancelPendingResume();
            _pendingResumeRoutine = StartCoroutine(ResumeAutoFlowNextFrame());
        }

        private IEnumerator ResumeAutoFlowNextFrame()
        {
            yield return null;

            _pendingResumeRoutine = null;

            if (_autoFlow == null || !_autoFlow.IsPaused)
            {
                yield break;
            }

            if (_manualPauseCount > 0 || _automaticPauseCount > 0)
            {
                yield break;
            }

            _autoFlow.Resume();
            string actorId = actor?.ActorId ?? name;
            DebugUtility.LogVerbose<ResourceAutoFlowBridge>($"▶️ AutoFlow retomado automaticamente para {actorId}.", null, this);
        }

        private void CancelPendingResume()
        {
            if (_pendingResumeRoutine != null)
            {
                StopCoroutine(_pendingResumeRoutine);
                _pendingResumeRoutine = null;
            }
        }

        // ContextMenu removidos — debug deve ser via DebugUtility/Inspector Customizado.
    }
}
