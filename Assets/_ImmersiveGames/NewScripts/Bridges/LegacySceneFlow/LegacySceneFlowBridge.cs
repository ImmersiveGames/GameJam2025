using System;
using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using LegacyEventBus = _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus;
using LegacyEventBinding = _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBinding;
using LegacySceneTransitionContext = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionContext;
using LegacyStarted = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionStartedEvent;
using LegacyScenesReady = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionScenesReadyEvent;
using LegacyCompleted = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionCompletedEvent;
using NewEventBus = _ImmersiveGames.NewScripts.Infrastructure.Events.EventBus;
using NewContext = _ImmersiveGames.NewScripts.Infrastructure.Scene.SceneTransitionContext;
using NewStarted = _ImmersiveGames.NewScripts.Infrastructure.Scene.SceneTransitionStartedEvent;
using NewScenesReady = _ImmersiveGames.NewScripts.Infrastructure.Scene.SceneTransitionScenesReadyEvent;
using NewCompleted = _ImmersiveGames.NewScripts.Infrastructure.Scene.SceneTransitionCompletedEvent;

namespace _ImmersiveGames.NewScripts.Bridges.LegacySceneFlow
{
    /// <summary>
    /// Bridge temporária para refletir eventos do Scene Flow legado no pipeline NewScripts.
    /// Não altera o comportamento existente; apenas observa e republica os marcos de transição.
        /// </summary>
        [DebugLevel(DebugLevel.Verbose)]
        public sealed class LegacySceneFlowBridge : IDisposable
        {
            private readonly LegacyEventBinding<LegacyStarted> _transitionStartedBinding;
            private readonly LegacyEventBinding<LegacyScenesReady> _transitionScenesReadyBinding;
            private readonly LegacyEventBinding<LegacyCompleted> _transitionCompletedBinding;

            private bool _bindingsRegistered;
            private bool _disposed;

            public LegacySceneFlowBridge()
        {
            _transitionStartedBinding =
                new LegacyEventBinding<LegacyStarted>(OnLegacySceneTransitionStarted);
            _transitionScenesReadyBinding =
                new LegacyEventBinding<LegacyScenesReady>(OnLegacySceneTransitionScenesReady);
            _transitionCompletedBinding =
                new LegacyEventBinding<LegacyCompleted>(OnLegacySceneTransitionCompleted);

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
                LegacyEventBus<LegacyStarted>.Register(_transitionStartedBinding);
                LegacyEventBus<LegacyScenesReady>.Register(_transitionScenesReadyBinding);
                LegacyEventBus<LegacyCompleted>.Register(_transitionCompletedBinding);
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

            LegacyEventBus<LegacyStarted>.Unregister(_transitionStartedBinding);
            LegacyEventBus<LegacyScenesReady>.Unregister(_transitionScenesReadyBinding);
            LegacyEventBus<LegacyCompleted>.Unregister(_transitionCompletedBinding);
            _bindingsRegistered = false;

            DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                "[SceneBridge] Bridge do Scene Flow legado desregistrada.");
        }

            private void OnLegacySceneTransitionStarted(LegacyStarted evt)
            {
                var context = BuildContext(evt.Context);
                NewEventBus<NewStarted>.Raise(new NewStarted(context));

                DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                    $"[SceneBridge] SceneTransitionStarted (legado) publicado no EventBus do NewScripts. Context={context}");
            }

            private void OnLegacySceneTransitionScenesReady(LegacyScenesReady evt)
            {
                var context = BuildContext(evt.Context);
                NewEventBus<NewScenesReady>.Raise(new NewScenesReady(context));

                DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                    $"[SceneBridge] SceneTransitionScenesReady (legado) publicado no EventBus do NewScripts. Context={context}");
            }

            private void OnLegacySceneTransitionCompleted(LegacyCompleted evt)
            {
                var context = BuildContext(evt.Context);
                NewEventBus<NewCompleted>.Raise(new NewCompleted(context));

                DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                    $"[SceneBridge] SceneTransitionCompleted (legado) publicado no EventBus do NewScripts. Context={context}");
            }

            private static NewContext BuildContext(object legacyContext)
            {
                var scenesToLoad = ToStringList(GetMemberOrDefault<IEnumerable<string>>(
                    legacyContext,
                    Array.Empty<string>(),
                    "scenesToLoad",
                    "ScenesToLoad"));

                var scenesToUnload = ToStringList(GetMemberOrDefault<IEnumerable<string>>(
                    legacyContext,
                    Array.Empty<string>(),
                    "scenesToUnload",
                    "ScenesToUnload"));

                var targetActiveScene = GetMemberOrDefault(
                    legacyContext,
                    defaultValue: (string)null,
                    "targetActiveScene",
                    "TargetActiveScene",
                    "activeScene",
                    "ActiveScene");

                var useFade = GetMemberOrDefault(
                    legacyContext,
                    defaultValue: false,
                    "useFade",
                    "UseFade",
                    "fade",
                    "Fade");

                var profileName = TryResolveProfileName(legacyContext);

                return new NewContext(
                    scenesToLoad,
                    scenesToUnload,
                    targetActiveScene,
                    useFade,
                    profileName);
            }

            private static string TryResolveProfileName(object legacyContext)
            {
                if (!TryGetMember<object>(
                        legacyContext,
                        out var profile,
                        "transitionProfile",
                    "TransitionProfile",
                    "profile",
                    "Profile"))
            {
                return null;
            }

            return GetMemberOrDefault(
                profile,
                defaultValue: (string)null,
                "name",
                "Name");
        }

        private static bool TryGetMember<T>(object obj, out T value, params string[] names)
        {
            value = default;

            if (obj == null || names == null || names.Length == 0)
            {
                return false;
            }

            var type = obj.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var name in names)
            {
                var property = type.GetProperty(name, flags);
                if (property != null && property.CanRead)
                {
                    var candidate = property.GetValue(obj);
                    if (TryConvert(candidate, out value))
                    {
                        return true;
                    }
                }

                var field = type.GetField(name, flags);
                if (field != null)
                {
                    var candidate = field.GetValue(obj);
                    if (TryConvert(candidate, out value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static T GetMemberOrDefault<T>(object obj, T defaultValue, params string[] names)
        {
            return TryGetMember(obj, out T value, names) ? value : defaultValue;
        }

        private static bool TryConvert<T>(object candidate, out T value)
        {
            value = default;

            if (candidate is T typed)
            {
                value = typed;
                return true;
            }

            if (candidate == null)
            {
                return false;
            }

            // Conversão específica para coleções de string → IReadOnlyList<string>/IEnumerable<string>/List<string>
            if (typeof(T) == typeof(IEnumerable<string>) ||
                typeof(T) == typeof(IReadOnlyList<string>))
            {
                if (candidate is IEnumerable<string> enumerable)
                {
                    value = (T)enumerable;
                    return true;
                }
            }

            return false;
        }

        private static List<string> ToStringList(IEnumerable<string> source)
        {
            var list = new List<string>();
            if (source == null)
            {
                return list;
            }

            foreach (var entry in source)
            {
                if (string.IsNullOrWhiteSpace(entry))
                {
                    continue;
                }
                list.Add(entry);
            }

            return list;
        }
    }
}
