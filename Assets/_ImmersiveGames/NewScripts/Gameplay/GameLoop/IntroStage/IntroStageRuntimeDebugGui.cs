#nullable enable
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.GameLoop
{
    /// <summary>
    /// GUI temporário (runtime) para concluir a IntroStage sem depender de QA.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageRuntimeDebugGui : MonoBehaviour
    {
        private static IntroStageRuntimeDebugGui _instance;
        private const string RuntimeGuiObjectName = "IntroStageRuntimeDebugGui";
        private const string CompleteReason = "IntroStage/UIConfirm";
        private const float GuiWidth = 280f;
        private const float GuiHeight = 90f;
        private const float GuiMargin = 12f;

        private bool _isVisible;
        private IIntroStageControlService? _controlService;
        private static bool _installed;
        private static bool _duplicateDestroyedLogged;

        public static void EnsureInstalled()
        {
            if (_installed)
            {
                return;
            }

            if (_instance != null)
            {
                _installed = true;
                return;
            }

            if (FindObjectOfType<IntroStageRuntimeDebugGui>(true) != null || FindExistingRuntimeGuiObject() != null)
            {
                _installed = true;
                return;
            }

            var go = new GameObject(RuntimeGuiObjectName);
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            go.AddComponent<IntroStageRuntimeDebugGui>();
            _installed = true;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (!_duplicateDestroyedLogged)
                {
                    _duplicateDestroyedLogged = true;
                    DebugUtility.LogWarning<IntroStageRuntimeDebugGui>(
                        "[IntroStage][RuntimeDebugGui] Instância duplicada detectada; destruindo duplicata.");
                }

                Destroy(gameObject);
                return;
            }
            var existing = FindExistingRuntimeGuiObject();
            if (existing != null && existing != gameObject)
            {
                if (!_duplicateDestroyedLogged)
                {
                    _duplicateDestroyedLogged = true;
                    DebugUtility.LogWarning<IntroStageRuntimeDebugGui>(
                        "[IntroStage][RuntimeDebugGui] Instância duplicada detectada; destruindo duplicata.");
                }

                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            var shouldShow = IsIntroStageActive();
            if (shouldShow == _isVisible)
            {
                return;
            }

            _isVisible = shouldShow;
            var state = _isVisible ? "exibido" : "oculto";
            DebugUtility.Log<IntroStageRuntimeDebugGui>($"[IntroStage][RuntimeDebugGui] GUI {state}.");
        }

        private void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            var rect = new Rect(GuiMargin, GuiMargin, GuiWidth, GuiHeight);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("IntroStage (Runtime Debug)");

            if (GUILayout.Button("Concluir IntroStage"))
            {
                DebugUtility.Log<IntroStageRuntimeDebugGui>(
                    "[IntroStage][RuntimeDebugGui] Botão Concluir IntroStage clicado.");
                RequestComplete();
            }

            GUILayout.EndArea();
        }

        private void RequestComplete()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                DebugUtility.LogWarning<IntroStageRuntimeDebugGui>(
                    "[IntroStage][RuntimeDebugGui] IIntroStageControlService indisponível; Complete ignorado.");
                return;
            }

            DebugUtility.Log<IntroStageRuntimeDebugGui>(
                $"[IntroStage][RuntimeDebugGui] Solicitando CompleteIntroStage reason='{CompleteReason}'.");
            controlService.CompleteIntroStage(CompleteReason);
        }

        private bool IsIntroStageActive()
        {
            var controlService = ResolveControlService();
            return controlService != null && controlService.IsIntroStageActive;
        }

        private IIntroStageControlService? ResolveControlService()
        {
            if (_controlService != null)
            {
                return _controlService;
            }

            if (!DependencyManager.HasInstance)
            {
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<IIntroStageControlService>(out var service) || service == null)
            {
                return null;
            }

            _controlService = service;
            return _controlService;
        }

        private static GameObject? FindExistingRuntimeGuiObject()
        {
            var objects = FindObjectsOfType<GameObject>(true);
            foreach (var obj in objects)
            {
                if (!string.Equals(obj.name, RuntimeGuiObjectName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (obj.GetComponent<IntroStageRuntimeDebugGui>() != null)
                {
                    return obj;
                }
            }

            return null;
        }
    }
}
#endif
