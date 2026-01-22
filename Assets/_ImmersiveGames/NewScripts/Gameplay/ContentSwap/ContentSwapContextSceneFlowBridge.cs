#nullable enable
using System;
using System.Linq;
using System.Reflection;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Infrastructure.Gameplay
{
    /// <summary>
    /// Bridge global: SceneFlow -> ContentSwapContext.
    /// Objetivo (Baseline 3B):
    /// - Pending NÃO pode atravessar uma transição de cena.
    /// Estratégia:
    /// - Ao receber SceneTransitionStartedEvent, limpa Pending (sem alterar Current).
    ///
    /// Observação:
    /// - Usa registration "best-effort" via reflection para evitar acoplamento a uma assinatura específica do EventBus.
    /// </summary>
    public sealed class ContentSwapContextSceneFlowBridge
    {
        private readonly Delegate _handlerDelegate;
        private object? _bindingInstance;
        private bool _registered;

        public ContentSwapContextSceneFlowBridge()
        {
            _handlerDelegate = (Action<SceneTransitionStartedEvent>)OnSceneTransitionStarted;

            _registered =
                TryRegisterDirect(typeof(EventBus<SceneTransitionStartedEvent>), _handlerDelegate) ||
                TryRegisterWithEventBinding(typeof(EventBus<SceneTransitionStartedEvent>), _handlerDelegate, out _bindingInstance);

            if (_registered)
            {
                DebugUtility.LogVerbose(
                    typeof(ContentSwapContextSceneFlowBridge),
                    "[ContentSwapContext] ContentSwapContextSceneFlowBridge registrado (SceneTransitionStartedEvent -> ClearPending).",
                    DebugUtility.Colors.Info);
            }
            else
            {
                DebugUtility.LogError(
                    typeof(ContentSwapContextSceneFlowBridge),
                    "[ContentSwapContext] Falha ao registrar ContentSwapContextSceneFlowBridge no EventBus<SceneTransitionStartedEvent>. Pending pode atravessar transições.");
            }
        }

        private static void OnSceneTransitionStarted(SceneTransitionStartedEvent evt)
        {
            if (!DependencyManager.HasInstance)
                return;

            if (!DependencyManager.Provider.TryGetGlobal<IContentSwapContextService>(out var context) || context == null)
                return;

            // Limpa apenas o Pending (Current permanece).
            // Reason curto e estável; o serviço faz sanitize.
            var sig = evt.Context.ContextSignature;
            context.ClearPending($"SceneFlow/TransitionStarted sig={sig}");
        }

        // --------------------------------------------------------------------
        // EventBus registration helpers (reflection)
        // --------------------------------------------------------------------

        private static bool TryRegisterDirect(Type busGenericType, Delegate handler)
        {
            // Tenta padrões comuns:
            // - Register(Action<T>)
            // - Subscribe(Action<T>)
            // - AddListener(Action<T>)
            // - Add(Action<T>)
            var methodNames = new[] { "Register", "Subscribe", "AddListener", "Add" };

            foreach (var name in methodNames)
            {
                var method = FindStaticSingleParamMethod(busGenericType, name, handler.GetType());
                if (method == null)
                    continue;

                try
                {
                    method.Invoke(null, new object[] { handler });
                    return true;
                }
                catch
                {
                    // ignora e tenta próximo
                }
            }

            return false;
        }

        private static bool TryRegisterWithEventBinding(Type busGenericType, Delegate handler, out object? bindingInstance)
        {
            bindingInstance = null;

            // Procura o tipo EventBinding`1 (sem assumir assembly específico).
            var bindingOpenGeneric = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(a =>
                {
                    try { return a.GetType("_ImmersiveGames.NewScripts.Infrastructure.Events.EventBinding`1", false); }
                    catch { return null; }
                })
                .FirstOrDefault(t => t != null);

            if (bindingOpenGeneric == null)
                return false;

            Type? bindingClosed;
            try
            {
                bindingClosed = bindingOpenGeneric.MakeGenericType(typeof(SceneTransitionStartedEvent));
            }
            catch
            {
                return false;
            }

            // Tenta instanciar EventBinding<T> com ctor que aceite o delegate.
            // (padrões comuns: .ctor(Action<T>) ou .ctor(Delegate))
            var ctor = bindingClosed.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(c =>
                {
                    var ps = c.GetParameters();
                    if (ps.Length != 1) return false;
                    return ps[0].ParameterType.IsAssignableFrom(handler.GetType()) ||
                           (ps[0].ParameterType == typeof(Delegate));
                });

            if (ctor == null)
                return false;

            try
            {
                bindingInstance = ctor.GetParameters()[0].ParameterType == typeof(Delegate)
                    ? ctor.Invoke(new object[] { handler })
                    : ctor.Invoke(new object[] { handler });

                if (bindingInstance == null)
                    return false;
            }
            catch
            {
                return false;
            }

            // Agora tenta registrar o binding no EventBus<T>.
            var methodNames = new[] { "Register", "Subscribe", "AddListener", "Add" };

            foreach (var name in methodNames)
            {
                var method = FindStaticSingleParamMethod(busGenericType, name, bindingClosed);
                if (method == null)
                    continue;

                try
                {
                    method.Invoke(null, new[] { bindingInstance });
                    return true;
                }
                catch
                {
                    // ignora e tenta próximo
                }
            }

            bindingInstance = null;
            return false;
        }

        private static MethodInfo? FindStaticSingleParamMethod(Type type, string name, Type paramType)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

            return type.GetMethods(flags)
                .Where(m => m.Name == name)
                .Where(m =>
                {
                    var ps = m.GetParameters();
                    if (ps.Length != 1) return false;

                    // Aceita o tipo exato, ou um supertipo/interface compatível.
                    return ps[0].ParameterType.IsAssignableFrom(paramType);
                })
                .OrderByDescending(m => m.GetParameters()[0].ParameterType == paramType) // prefere match exato
                .FirstOrDefault();
        }
    }
}
