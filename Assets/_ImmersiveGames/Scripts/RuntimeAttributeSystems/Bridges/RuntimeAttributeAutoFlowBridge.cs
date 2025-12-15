using System.Collections;
using _ImmersiveGames.Scripts.RuntimeAttributeSystems.Services;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.RuntimeAttributeSystems
{
    
    public class RuntimeAttributeAutoFlowBridge : RuntimeAttributeBridgeBase
    {
        [SerializeField] private bool startPaused = true;

        private RuntimeAttributeAutoFlowService _autoFlow;
        private Coroutine _pendingResumeRoutine;
        private int _manualPauseCount;
        private int _automaticPauseCount;
        private bool _autoResumeAllowed;

        public bool HasAutoFlowService => _autoFlow != null;
        public bool IsAutoFlowActive => _autoFlow is { IsPaused: false };
        public bool AutoResumeAllowed => _autoResumeAllowed;
        public bool StartPaused => startPaused;

        protected override void OnServiceInitialized()
        {
            if (runtimeAttributeContext == null)
            {
                DebugUtility.LogWarning<RuntimeAttributeAutoFlowBridge>($"RuntimeAttributeContext null em {actor.ActorId}");
                enabled = false;
                return;
            }

            if (!HasAutoFlowResources())
            {
                DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>($"Nenhum recurso AutoFlow em {actor.ActorId} — Component desativado.");
                enabled = false;
                return;
            }

            _autoFlow = new RuntimeAttributeAutoFlowService(runtimeAttributeContext, startPaused);
            _autoResumeAllowed = !startPaused;
            runtimeAttributeContext.ResourceChanging += HandleResourceChanging;
            runtimeAttributeContext.ResourceChanged += HandleResourceChanged;

            DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>($"🚀 AutoFlow inicializado para {actor.ActorId}", null, this);

            if (!startPaused)
            {
                _autoFlow.Resume();
                DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>($"▶️ AutoFlow iniciado imediatamente para {actor.ActorId}", null, this);
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

            // Assim que o jogador solicitar a retomada manual, liberamos o autorresume.
            if (!_autoResumeAllowed)
            {
                _autoResumeAllowed = true;
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

            if (_autoFlow.IsPaused && _automaticPauseCount == 0)
            {
                _autoFlow.Resume();
                string actorId = actor?.ActorId ?? name;
                DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>($"▶️ AutoFlow retomado para {actorId}.", null, this);
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
            DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>($"⏸️ AutoFlow pausado para {actorId}.", null, this);
            return true;
        }

        protected override void OnServiceDispose()
        {
            if (runtimeAttributeContext != null)
            {
                runtimeAttributeContext.ResourceChanging -= HandleResourceChanging;
                runtimeAttributeContext.ResourceChanged -= HandleResourceChanged;
            }

            CancelPendingResume();
            _manualPauseCount = 0;
            _automaticPauseCount = 0;
            _autoResumeAllowed = false;

            _autoFlow?.Dispose();
            _autoFlow = null;
        }

        private bool HasAutoFlowResources()
        {
            foreach (var (type, _) in runtimeAttributeContext.GetAll())
            {
                var cfg = runtimeAttributeContext.GetInstanceConfig(type);
                if (cfg is { hasAutoFlow: true } && cfg.autoFlowConfig != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleResourceChanging(RuntimeAttributeChangeContext context)
        {
            if (!ShouldReactToContext(context))
            {
                return;
            }

            _automaticPauseCount = Mathf.Max(0, _automaticPauseCount) + 1;
            CancelPendingResume();

            if (_autoFlow is { IsPaused: false })
            {
                _autoFlow.Pause();
                string actorId = actor?.ActorId ?? name;
                DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>($"⏸️ AutoFlow pausado automaticamente ({context.RuntimeAttributeType}, Δ={context.Delta:F2}) para {actorId}.", null, this);
            }
        }

        private void HandleResourceChanged(RuntimeAttributeChangeContext context)
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
                DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>($"⏸️ AutoFlow segue pausado manualmente após alteração de {context.RuntimeAttributeType}.", null, this, deduplicate: true);
                return;
            }

            if (context is { IsIncrease: true, ReachedMax: true })
            {
                DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>($"🔒 AutoFlow mantido em pausa porque {context.RuntimeAttributeType} atingiu o valor máximo.", null, this);
                return;
            }

            if (!_autoResumeAllowed)
            {
                DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>(
                    $"⏸️ AutoFlow permanece pausado (StartPaused ativo) após alteração de {context.RuntimeAttributeType}.",
                    null,
                    this,
                    deduplicate: true);
                return;
            }

            ScheduleAutomaticResume();
        }

        private bool ShouldReactToContext(RuntimeAttributeChangeContext context)
        {
            if (_autoFlow == null || !IsInitialized)
            {
                return false;
            }

            if (context.Source == RuntimeAttributeChangeSource.AutoFlow)
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

            if (!_autoResumeAllowed)
            {
                DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>(
                    "⏸️ AutoFlow permaneceu pausado por configuração startPaused.",
                    null,
                    this,
                    deduplicate: true);
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

            if (!_autoResumeAllowed)
            {
                yield break;
            }

            _autoFlow.Resume();
            string actorId = actor?.ActorId ?? name;
            DebugUtility.LogVerbose<RuntimeAttributeAutoFlowBridge>($"▶️ AutoFlow retomado automaticamente para {actorId}.", null, this);
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
