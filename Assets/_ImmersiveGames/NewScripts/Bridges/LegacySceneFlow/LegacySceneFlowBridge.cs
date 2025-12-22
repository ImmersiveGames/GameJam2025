using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

using LegacyRoot = global::_ImmersiveGames;
using LegacyScripts = LegacyRoot.Scripts;
using LegacyEventBus = LegacyScripts.Utils.BusEventSystems.EventBus;
using LegacyEventBinding = LegacyScripts.Utils.BusEventSystems.EventBinding;
using LegacySceneTransitionContext = LegacyScripts.SceneManagement.Transition.SceneTransitionContext;
using LegacySceneTransitionStartedEvent = LegacyScripts.SceneManagement.Transition.SceneTransitionStartedEvent;
using LegacySceneTransitionScenesReadyEvent = LegacyScripts.SceneManagement.Transition.SceneTransitionScenesReadyEvent;
using LegacySceneTransitionCompletedEvent = LegacyScripts.SceneManagement.Transition.SceneTransitionCompletedEvent;

namespace _ImmersiveGames.NewScripts.Bridges.LegacySceneFlow
{
    /// <summary>
    /// Bridge temporária para refletir eventos do Scene Flow legado no pipeline NewScripts.
    /// Não altera o comportamento existente; apenas observa e republica os marcos de transição.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LegacySceneFlowBridge : IDisposable
    {
        private readonly LegacyEventBinding<LegacySceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly LegacyEventBinding<LegacySceneTransitionScenesReadyEvent> _transitionScenesReadyBinding;
        private readonly LegacyEventBinding<LegacySceneTransitionCompletedEvent> _transitionCompletedBinding;

        private bool _bindingsRegistered;
        private bool _disposed;

        public LegacySceneFlowBridge()
        {
            _transitionStartedBinding =
                new LegacyEventBinding<LegacySceneTransitionStartedEvent>(OnLegacySceneTransitionStarted);
            _transitionScenesReadyBinding =
                new LegacyEventBinding<LegacySceneTransitionScenesReadyEvent>(OnLegacySceneTransitionScenesReady);
            _transitionCompletedBinding =
                new LegacyEventBinding<LegacySceneTransitionCompletedEvent>(OnLegacySceneTransitionCompleted);

            TryRegisterBindings();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            UnregisterBindings();
        }

        private void TryRegisterBindings()
        {
            if (_bindingsRegistered)
            {
                return;
            }

            try
            {
                LegacyEventBus<LegacySceneTransitionStartedEvent>.Register(_transitionStartedBinding);
                LegacyEventBus<LegacySceneTransitionScenesReadyEvent>.Register(_transitionScenesReadyBinding);
                LegacyEventBus<LegacySceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);
                _bindingsRegistered = true;

                DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                    "[SceneBridge] Registrado para refletir transições de cena (legado → NewScripts).");
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<LegacySceneFlowBridge>(
                    $"[SceneBridge] Falha ao registrar bridge do Scene Flow legado: {ex.GetType().Name}.");
                _bindingsRegistered = false;
            }
        }

        private void UnregisterBindings()
        {
            if (!_bindingsRegistered)
            {
                return;
            }

            LegacyEventBus<LegacySceneTransitionStartedEvent>.Unregister(_transitionStartedBinding);
            LegacyEventBus<LegacySceneTransitionScenesReadyEvent>.Unregister(_transitionScenesReadyBinding);
            LegacyEventBus<LegacySceneTransitionCompletedEvent>.Unregister(_transitionCompletedBinding);
            _bindingsRegistered = false;

            DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                "[SceneBridge] Bridge do Scene Flow legado desregistrada.");
        }

        private void OnLegacySceneTransitionStarted(LegacySceneTransitionStartedEvent evt)
        {
            var context = BuildContext(evt.Context);
            EventBus<SceneTransitionStartedEvent>.Raise(new SceneTransitionStartedEvent(context));

            DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                $"[SceneBridge] SceneTransitionStarted (legado) publicado no EventBus do NewScripts. Context={context}");
        }

        private void OnLegacySceneTransitionScenesReady(LegacySceneTransitionScenesReadyEvent evt)
        {
            var context = BuildContext(evt.Context);
            EventBus<SceneTransitionScenesReadyEvent>.Raise(new SceneTransitionScenesReadyEvent(context));

            DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                $"[SceneBridge] SceneTransitionScenesReady (legado) publicado no EventBus do NewScripts. Context={context}");
        }

        private void OnLegacySceneTransitionCompleted(LegacySceneTransitionCompletedEvent evt)
        {
            var context = BuildContext(evt.Context);
            EventBus<SceneTransitionCompletedEvent>.Raise(new SceneTransitionCompletedEvent(context));

            DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                $"[SceneBridge] SceneTransitionCompleted (legado) publicado no EventBus do NewScripts. Context={context}");
        }

        private static SceneTransitionContext BuildContext(LegacySceneTransitionContext legacyContext)
        {
            var scenesToLoad = legacyContext.scenesToLoad ?? Array.Empty<string>();
            var scenesToUnload = legacyContext.scenesToUnload ?? Array.Empty<string>();
            var profileName = TryResolveProfileName(legacyContext);

            return new SceneTransitionContext(
                scenesToLoad,
                scenesToUnload,
                legacyContext.targetActiveScene,
                legacyContext.useFade,
                profileName);
        }

        private static string TryResolveProfileName(LegacySceneTransitionContext legacyContext)
        {
            try
            {
                return legacyContext.transitionProfile != null
                    ? legacyContext.transitionProfile.name
                    : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
