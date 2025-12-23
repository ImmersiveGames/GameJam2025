using System;
using System.Collections.Generic;
using System.Reflection;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using LegacySceneTransitionContext = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionContext;
using LegacyStarted = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionStartedEvent;
using LegacyScenesReady = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionScenesReadyEvent;
using LegacyCompleted = _ImmersiveGames.Scripts.SceneManagement.Transition.SceneTransitionCompletedEvent;
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
        private readonly _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBinding<LegacyStarted> _transitionStartedBinding;
        private readonly _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBinding<LegacyScenesReady> _transitionScenesReadyBinding;
        private readonly _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBinding<LegacyCompleted> _transitionCompletedBinding;

        private bool _bindingsRegistered;
        private bool _disposed;

        public LegacySceneFlowBridge()
        {
            _transitionStartedBinding =
                new _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBinding<LegacyStarted>(OnLegacySceneTransitionStarted);
            _transitionScenesReadyBinding =
                new _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBinding<LegacyScenesReady>(OnLegacySceneTransitionScenesReady);
            _transitionCompletedBinding =
                new _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBinding<LegacyCompleted>(OnLegacySceneTransitionCompleted);

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
                _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus<LegacyStarted>.Register(_transitionStartedBinding);
                _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus<LegacyScenesReady>.Register(_transitionScenesReadyBinding);
                _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus<LegacyCompleted>.Register(_transitionCompletedBinding);
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

            _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus<LegacyStarted>.Unregister(_transitionStartedBinding);
            _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus<LegacyScenesReady>.Unregister(_transitionScenesReadyBinding);
            _ImmersiveGames.Scripts.Utils.BusEventSystems.EventBus<LegacyCompleted>.Unregister(_transitionCompletedBinding);
            _bindingsRegistered = false;

            DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                "[SceneBridge] Bridge do Scene Flow legado desregistrada.");
        }

        private void OnLegacySceneTransitionStarted(LegacyStarted evt)
        {
            var context = BuildContext(evt?.Context);
            _ImmersiveGames.NewScripts.Infrastructure.Events.EventBus<NewStarted>.Raise(new NewStarted(context));

            DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                $"[SceneBridge] SceneTransitionStarted (legado) publicado no EventBus do NewScripts. Context={context}");
        }

        private void OnLegacySceneTransitionScenesReady(LegacyScenesReady evt)
        {
            var context = BuildContext(evt?.Context);
            _ImmersiveGames.NewScripts.Infrastructure.Events.EventBus<NewScenesReady>.Raise(new NewScenesReady(context));

            DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                $"[SceneBridge] SceneTransitionScenesReady (legado) publicado no EventBus do NewScripts. Context={context}");
        }

        private void OnLegacySceneTransitionCompleted(LegacyCompleted evt)
        {
            var context = BuildContext(evt?.Context);
            _ImmersiveGames.NewScripts.Infrastructure.Events.EventBus<NewCompleted>.Raise(new NewCompleted(context));

            DebugUtility.LogVerbose<LegacySceneFlowBridge>(
                $"[SceneBridge] SceneTransitionCompleted (legado) publicado no EventBus do NewScripts. Context={context}");
        }

        private static NewContext BuildContext(object legacyContext)
        {
            if (legacyContext is LegacySceneTransitionContext legacyTyped)
            {
                var loadList = ToStringList(legacyTyped.scenesToLoad ?? Array.Empty<string>());
                var unloadList = ToStringList(legacyTyped.scenesToUnload ?? Array.Empty<string>());
                var profileDirect = legacyTyped.transitionProfile != null ? legacyTyped.transitionProfile.name : null;

                return new NewContext(
                    loadList,
                    unloadList,
                    legacyTyped.targetActiveScene,
                    legacyTyped.useFade,
                    profileDirect);
            }

            var scenesToLoad = ToStringList(
                TryGetMember<IEnumerable<string>>(legacyContext, "scenesToLoad", "ScenesToLoad") ??
                TryGetMember<List<string>>(legacyContext, "scenesToLoad", "ScenesToLoad") ??
                Array.Empty<string>());

            var scenesToUnload = ToStringList(
                TryGetMember<IEnumerable<string>>(legacyContext, "scenesToUnload", "ScenesToUnload") ??
                TryGetMember<List<string>>(legacyContext, "scenesToUnload", "ScenesToUnload") ??
                Array.Empty<string>());

            var targetActiveScene = TryGetMember<string>(
                legacyContext,
                "targetActiveScene",
                "TargetActiveScene",
                "activeScene",
                "ActiveScene") ?? null;

            var useFade = TryGetMember<bool?>(
                              legacyContext,
                              "useFade",
                              "UseFade",
                              "fade",
                              "Fade") ?? false;

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
            var profile = TryGetMember<object>(
                legacyContext,
                "transitionProfile",
                "TransitionProfile",
                "profile",
                "Profile");

            if (profile == null)
            {
                return null;
            }

            return TryGetMember<string>(profile, "name", "Name");
        }

        private static T TryGetMember<T>(object obj, params string[] names)
        {
            if (obj == null || names == null || names.Length == 0)
            {
                return default;
            }

            var type = obj.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var name in names)
            {
                var property = type.GetProperty(name, flags);
                if (property != null && property.CanRead)
                {
                    var candidate = property.GetValue(obj);
                    if (candidate is T typedProp)
                    {
                        return typedProp;
                    }
                }

                var field = type.GetField(name, flags);
                if (field != null)
                {
                    var candidate = field.GetValue(obj);
                    if (candidate is T typedField)
                    {
                        return typedField;
                    }
                }
            }

            return default;
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
