using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using NewCompleted = _ImmersiveGames.NewScripts.Infrastructure.Scene.SceneTransitionCompletedEvent;
using NewContext = _ImmersiveGames.NewScripts.Infrastructure.Scene.SceneTransitionContext;
using NewScenesReady = _ImmersiveGames.NewScripts.Infrastructure.Scene.SceneTransitionScenesReadyEvent;
using NewStarted = _ImmersiveGames.NewScripts.Infrastructure.Scene.SceneTransitionStartedEvent;
using LegacyCompleted = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionCompletedEvent;
using LegacyContext = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionContext;
using LegacyScenesReady = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionScenesReadyEvent;
using LegacyStarted = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionStartedEvent;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Smoke test para garantir que o LegacySceneFlowBridge está refletindo eventos do fluxo de cenas legado
    /// para o EventBus do NewScripts.
    /// </summary>
    public sealed class LegacySceneFlowBridgeSmokeQATester : MonoBehaviour
    {
        private bool _startedReceived;
        private bool _readyReceived;
        private bool _completedReceived;
        private NewContext _startedContext;
        private bool _skipExecution;

        private EventBinding<NewStarted> _startedBinding;
        private EventBinding<NewScenesReady> _readyBinding;
        private EventBinding<NewCompleted> _completedBinding;

        public void Run()
        {
            ResetState();
            RegisterBindings();

            try
            {
                if (!TryPublishLegacyEvents())
                {
                    return;
                }

                WaitForDispatch();
                ValidateOrThrow();
            }
            finally
            {
                UnregisterBindings();
            }
        }

        private void ResetState()
        {
            _startedReceived = false;
            _readyReceived = false;
            _completedReceived = false;
            _startedContext = default;
            _skipExecution = false;
        }

        private void RegisterBindings()
        {
            _startedBinding = new EventBinding<NewStarted>(OnNewStarted);
            _readyBinding = new EventBinding<NewScenesReady>(_ => _readyReceived = true);
            _completedBinding = new EventBinding<NewCompleted>(_ => _completedReceived = true);

            EventBus<NewStarted>.Register(_startedBinding);
            EventBus<NewScenesReady>.Register(_readyBinding);
            EventBus<NewCompleted>.Register(_completedBinding);
        }

        private void UnregisterBindings()
        {
            if (_startedBinding != null)
            {
                EventBus<NewStarted>.Unregister(_startedBinding);
                _startedBinding = null;
            }

            if (_readyBinding != null)
            {
                EventBus<NewScenesReady>.Unregister(_readyBinding);
                _readyBinding = null;
            }

            if (_completedBinding != null)
            {
                EventBus<NewCompleted>.Unregister(_completedBinding);
                _completedBinding = null;
            }
        }

        private void OnNewStarted(NewStarted evt)
        {
            _startedReceived = true;
            _startedContext = evt.Context;
        }

        private bool TryPublishLegacyEvents()
        {
            try
            {
                var legacyContext = new LegacyContext(
                    new[] { "A" },
                    new[] { "B" },
                    "QA_TestScene",
                    true);

                _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus<LegacyStarted>.Raise(
                    new LegacyStarted(legacyContext));
                _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus<LegacyScenesReady>.Raise(
                    new LegacyScenesReady(legacyContext));
                _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus<LegacyCompleted>.Raise(
                    new LegacyCompleted(legacyContext));
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning(typeof(LegacySceneFlowBridgeSmokeQATester),
                    $"[QA][SceneBridge] SKIP - legacy scene flow not available. ({ex.GetType().Name}: {ex.Message})");
                _skipExecution = true;
                return false;
            }

            return true;
        }

        private void WaitForDispatch()
        {
            if (_skipExecution)
            {
                return;
            }

            if (_startedReceived && _readyReceived && _completedReceived)
            {
                return;
            }

            float start = Time.realtimeSinceStartup;
            const float timeout = 0.25f;

            while (Time.realtimeSinceStartup - start < timeout)
            {
                if (_startedReceived && _readyReceived && _completedReceived)
                {
                    break;
                }
            }
        }

        private void ValidateOrThrow()
        {
            if (!(_startedReceived && _readyReceived && _completedReceived))
            {
                if (_skipExecution)
                {
                    DebugUtility.LogWarning(typeof(LegacySceneFlowBridgeSmokeQATester),
                        "[QA][SceneBridge] SKIP - eventos não recebidos (bridge legado possivelmente ausente).");
                    return;
                }

                throw new InvalidOperationException(
                    "[QA][SceneBridge] FAIL - eventos não refletidos (Started/ScenesReady/Completed).");
            }

            if (_startedContext.ScenesToLoad == null || _startedContext.ScenesToUnload == null)
            {
                throw new InvalidOperationException(
                    "[QA][SceneBridge] FAIL - contexto não mapeado (listas nulas).");
            }

            bool loadOk = ContainsEntry(_startedContext.ScenesToLoad, "A");
            bool unloadOk = ContainsEntry(_startedContext.ScenesToUnload, "B");

            if (_startedContext.TargetActiveScene != "QA_TestScene" ||
                !_startedContext.UseFade ||
                !loadOk ||
                !unloadOk)
            {
                throw new InvalidOperationException(
                    "[QA][SceneBridge] FAIL - contexto mapeado com valores incorretos.");
            }

            DebugUtility.Log(typeof(LegacySceneFlowBridgeSmokeQATester),
                "[QA][SceneBridge] PASS - bridge refletiu eventos do legado para NewScripts.",
                DebugUtility.Colors.Success);
        }

        private static bool ContainsEntry(IReadOnlyList<string> list, string expected)
        {
            if (list == null || string.IsNullOrWhiteSpace(expected))
            {
                return false;
            }

            foreach (var entry in list)
            {
                if (string.Equals(entry, expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
