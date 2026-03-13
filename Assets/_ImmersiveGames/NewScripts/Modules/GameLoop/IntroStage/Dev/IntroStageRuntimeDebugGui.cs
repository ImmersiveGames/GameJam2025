#nullable enable
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.PostGame;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Modules.GameLoop.IntroStage.Dev
{
    /// <summary>
    /// GUI temporario (runtime) para concluir a IntroStageController e o mock de reacao de PostGame.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class IntroStageRuntimeDebugGui : MonoBehaviour
    {
        private static IntroStageRuntimeDebugGui? _instance;
        private const string RuntimeGuiObjectName = "IntroStageRuntimeDebugGui";
        private const string CompleteReason = "IntroStageController/UIConfirm";
        private const string PostGameReactionReason = "PostGame/LevelHook/MockComplete";
        private const float GuiWidth = 320f;
        private const float GuiHeight = 120f;
        private const float GuiMargin = 12f;

        private static TaskCompletionSource<string>? _postGameCompletionSource;
        private static PostGameReactionState _postGameReactionState;

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

            if (_instance != null || FindAnyObjectByType<IntroStageRuntimeDebugGui>() != null || FindExistingRuntimeGuiObject() != null)
            {
                _installed = true;
                return;
            }

            var go = new GameObject(RuntimeGuiObjectName)
            {
                hideFlags = HideFlags.DontSave
            };
            DontDestroyOnLoad(go);
            go.AddComponent<IntroStageRuntimeDebugGui>();
            _installed = true;
        }

        public static async Task RunPostGameReactionAsync(string levelName, PostGameResult result, string reason, CancellationToken cancellationToken)
        {
            EnsureInstalled();

            if (_postGameCompletionSource != null && !_postGameCompletionSource.Task.IsCompleted)
            {
                DebugUtility.LogVerbose<IntroStageRuntimeDebugGui>(
                    $"[PostGame][RuntimeDebugGui] Reacao mock ja ativa; reutilizando painel atual. result='{result}'.",
                    DebugUtility.Colors.Info);
                await AwaitReactionAsync(_postGameCompletionSource.Task, cancellationToken);
                return;
            }

            _postGameReactionState = new PostGameReactionState(levelName, result, reason);
            _postGameCompletionSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            DebugUtility.Log<IntroStageRuntimeDebugGui>(
                $"[PostGame][RuntimeDebugGui] GUI exibido para reacao mock. levelRef='{Normalize(levelName)}' result='{result}' reason='{Normalize(reason)}'.",
                DebugUtility.Colors.Info);

            await AwaitReactionAsync(_postGameCompletionSource.Task, cancellationToken);
        }

        private static async Task AwaitReactionAsync(Task<string> task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                await task;
                return;
            }

            var completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = cancellationToken.Register(() => completionSource.TrySetResult(true));

            if (task == await Task.WhenAny(task, completionSource.Task))
            {
                await task;
                return;
            }

            CompletePostGameReaction("PostGame/LevelHook/MockCancelled");
            await task;
        }

        private static void CompletePostGameReaction(string reason)
        {
            if (_postGameCompletionSource == null)
            {
                return;
            }

            _postGameCompletionSource.TrySetResult(string.IsNullOrWhiteSpace(reason) ? PostGameReactionReason : reason.Trim());
            _postGameCompletionSource = null;
            _postGameReactionState = default;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                if (!_duplicateDestroyedLogged)
                {
                    _duplicateDestroyedLogged = true;
                    DebugUtility.LogVerbose<IntroStageRuntimeDebugGui>(
                        "[IntroStageController][RuntimeDebugGui] Instancia duplicada detectada; destruindo duplicata.",
                        DebugUtility.Colors.Info);
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
            bool shouldShow = IsIntroStageActive() || IsPostGameReactionActive();
            if (shouldShow == _isVisible)
            {
                return;
            }

            _isVisible = shouldShow;
            string state = _isVisible ? "exibido" : "oculto";
            DebugUtility.Log<IntroStageRuntimeDebugGui>($"[IntroStageController][RuntimeDebugGui] GUI {state}.");
        }

        private void OnGUI()
        {
            if (!_isVisible)
            {
                return;
            }

            var rect = new Rect(GuiMargin, GuiMargin, GuiWidth, GuiHeight);
            GUILayout.BeginArea(rect, GUI.skin.box);

            if (IsIntroStageActive())
            {
                GUILayout.Label("IntroStageController (Runtime Debug)");
                if (GUILayout.Button("Concluir IntroStageController"))
                {
                    DebugUtility.Log<IntroStageRuntimeDebugGui>(
                        "[IntroStageController][RuntimeDebugGui] Botao Concluir IntroStageController clicado.");
                    RequestComplete();
                }
            }
            else if (IsPostGameReactionActive())
            {
                GUILayout.Label("PostGame Hook (Runtime Debug)");
                GUILayout.Label($"Level: {Normalize(_postGameReactionState.LevelName)}");
                GUILayout.Label($"Result: {_postGameReactionState.Result}");

                if (GUILayout.Button("Concluir Reacao Mock de PostGame"))
                {
                    DebugUtility.Log<IntroStageRuntimeDebugGui>(
                        $"[PostGame][RuntimeDebugGui] Botao concluir reacao mock clicado. result='{_postGameReactionState.Result}'.");
                    CompletePostGameReaction(PostGameReactionReason);
                }
            }

            GUILayout.EndArea();
        }

        private void RequestComplete()
        {
            var controlService = ResolveControlService();
            if (controlService == null)
            {
                DebugUtility.LogWarning<IntroStageRuntimeDebugGui>(
                    "[IntroStageController][RuntimeDebugGui] IIntroStageControlService indisponivel; Complete ignorado.");
                return;
            }

            DebugUtility.Log<IntroStageRuntimeDebugGui>(
                $"[IntroStageController][RuntimeDebugGui] Solicitando CompleteIntroStage reason='{CompleteReason}'.");
            controlService.CompleteIntroStage(CompleteReason);
        }

        private bool IsIntroStageActive()
        {
            var controlService = ResolveControlService();
            return controlService != null && controlService.IsIntroStageActive;
        }

        private static bool IsPostGameReactionActive()
        {
            return _postGameCompletionSource != null && !_postGameCompletionSource.Task.IsCompleted;
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
            GameObject[]? objects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
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

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();

        private readonly struct PostGameReactionState
        {
            public PostGameReactionState(string levelName, PostGameResult result, string reason)
            {
                LevelName = levelName ?? string.Empty;
                Result = result;
                Reason = reason ?? string.Empty;
            }

            public string LevelName { get; }
            public PostGameResult Result { get; }
            public string Reason { get; }
        }
    }
}
#endif
