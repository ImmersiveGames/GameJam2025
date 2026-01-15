#if NEWSCRIPTS_MODE
using _ImmersiveGames.NewScripts.Gameplay.GameLoop;
using _ImmersiveGames.NewScripts.Gameplay.Phases;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.Debug
{
    /// <summary>
    /// Overlay de debug mínimo para exibir estado do GameLoop e Phase atual.
    /// </summary>
    public sealed class DebugOverlayController : MonoBehaviour
    {
        private const string OverlayObjectName = "DebugOverlay";
        private const int Padding = 8;
        private const int LineHeight = 22;

        public static void EnsureInstalled()
        {
            var existing = GameObject.Find(OverlayObjectName);
            if (existing != null)
            {
                if (!existing.TryGetComponent<DebugOverlayController>(out _))
                {
                    existing.AddComponent<DebugOverlayController>();
                }
                return;
            }

            var go = new GameObject(OverlayObjectName);
            DontDestroyOnLoad(go);
            go.AddComponent<DebugOverlayController>();
        }

        private void OnGUI()
        {
            if (!ShouldRender())
            {
                return;
            }

            var gameLoopState = ResolveGameLoopState();
            var phaseInfo = ResolvePhaseInfo();

            var rect = new Rect(Padding, Padding, 520, LineHeight * 3);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.Box(rect, GUIContent.none);

            GUI.color = Color.white;
            GUI.Label(new Rect(Padding * 2, Padding + 2, 500, LineHeight),
                $"DebugOverlay | GameLoopState: {gameLoopState}");
            GUI.Label(new Rect(Padding * 2, Padding + LineHeight, 500, LineHeight),
                $"Phase: {phaseInfo}");
        }

        private static bool ShouldRender()
        {
#if NEWSCRIPTS_MODE
            return true;
#else
            var state = ResolveGameLoopState();
            return string.Equals(state, nameof(GameLoopStateId.Pregame), System.StringComparison.Ordinal);
#endif
        }

        private static string ResolveGameLoopState()
        {
            if (!DependencyManager.HasInstance)
            {
                return "<no-di>";
            }

            if (!DependencyManager.Provider.TryGetGlobal<IGameLoopService>(out var gameLoop) || gameLoop == null)
            {
                return "<no-gameloop>";
            }

            return string.IsNullOrWhiteSpace(gameLoop.CurrentStateIdName)
                ? "<unknown>"
                : gameLoop.CurrentStateIdName;
        }

        private static string ResolvePhaseInfo()
        {
            if (!DependencyManager.HasInstance)
            {
                return "<no-di>";
            }

            if (!DependencyManager.Provider.TryGetGlobal<IPhaseContextService>(out var phaseContext) || phaseContext == null)
            {
                return "<no-phase>";
            }

            var current = phaseContext.Current;
            if (!current.HasValue)
            {
                return "<none>";
            }

            var phaseId = string.IsNullOrWhiteSpace(current.Value.PhaseId) ? "<none>" : current.Value.PhaseId;
            return $"PhaseId={phaseId}";
        }
    }
}
#endif
