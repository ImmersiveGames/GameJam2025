#if UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime;
using _ImmersiveGames.NewScripts.Modules.GameLoop.Runtime.Services;
using _ImmersiveGames.NewScripts.Modules.LevelFlow.Runtime;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.QA.Smoke.Baseline3.Editor
{
    /// <summary>
    /// Comandos canônicos de smoke para níveis (D/E) e atalhos Baseline 3.0.
    /// </summary>
    public static class Baseline3LevelsSmokeCommands
    {
        private const string MenuRootLevels = "QA/Smoke/Baseline 3.0 Levels";
        private const string MenuRootBaseline = "QA/Smoke/Baseline 3.0";

        private const string ReasonStepD = "QA/Smoke/Baseline3/Levels/D/SwapToL2";
        private const string ReasonStepE = "QA/Smoke/Baseline3/Levels/E/LevelReset";
        private const string ReasonStepDE = "QA/Smoke/Baseline3/Levels/DE/SwapAndReset";
        private const string ReasonStepG = "QA/Smoke/Baseline3/G/PostGameRestart";
        private const string ReasonStepH = "QA/Smoke/Baseline3/H/Defeat";

        [MenuItem(MenuRootLevels + "/D. Swap to L2", priority = 2100)]
        private static void RunD_Menu()
        {
            _ = LevelSmokeSteps.RunD_SwapToL2Async();
        }

        [MenuItem(MenuRootLevels + "/E. LevelReset (stay on L2)", priority = 2101)]
        private static void RunE_Menu()
        {
            _ = LevelSmokeSteps.RunE_LevelResetAsync();
        }

        [MenuItem(MenuRootLevels + "/D+E. Swap L2 + LevelReset", priority = 2102)]
        private static void RunDE_Menu()
        {
            _ = LevelSmokeSteps.RunDE_SwapL2ThenResetAsync();
        }

        [MenuItem(MenuRootBaseline + "/Restart (PostGame/Restart)", priority = 2110)]
        private static void RunRestart_Menu()
        {
            _ = RunRestartAsync();
        }

        [MenuItem(MenuRootBaseline + "/Defeat", priority = 2111)]
        private static void RunDefeat_Menu()
        {
            RunDefeat();
        }

        [Shortcut("QA/Smoke/Baseline 3.0 Levels/Run D+E (Swap L2 + LevelReset)", KeyCode.F8)]
        private static void RunDE_Shortcut()
        {
            _ = LevelSmokeSteps.RunDE_SwapL2ThenResetAsync();
        }

        [Shortcut("QA/Smoke/Baseline 3.0/Restart (PostGame/Restart)", KeyCode.F10)]
        private static void RunRestart_Shortcut()
        {
            _ = RunRestartAsync();
        }

        [Shortcut("QA/Smoke/Baseline 3.0/Defeat", KeyCode.F11)]
        private static void RunDefeat_Shortcut()
        {
            RunDefeat();
        }

        private static async Task RunRestartAsync()
        {
            if (!EnsurePlayMode())
            {
                return;
            }

            var postLevelActions = ResolveGlobal<IPostLevelActionsService>("IPostLevelActionsService");
            if (postLevelActions == null)
            {
                return;
            }

            await postLevelActions.RestartLevelAsync(ReasonStepG, CancellationToken.None);
        }

        private static void RunDefeat()
        {
            if (!EnsurePlayMode())
            {
                return;
            }

            var endRequest = ResolveGlobal<IGameRunEndRequestService>("IGameRunEndRequestService");
            if (endRequest == null)
            {
                return;
            }

            endRequest.RequestEnd(GameRunOutcome.Defeat, ReasonStepH);
        }

        private static bool EnsurePlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                return true;
            }

            DebugUtility.LogWarning(typeof(Baseline3LevelsSmokeCommands),
                "[FATAL][Smoke][Levels] Command requires Play Mode.");
            return false;
        }

        private static T ResolveGlobal<T>(string label) where T : class
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogWarning(typeof(Baseline3LevelsSmokeCommands),
                    "[FATAL][Smoke][Levels] DependencyManager.Provider is null.");
                return null;
            }

            if (!DependencyManager.Provider.TryGetGlobal<T>(out var service) || service == null)
            {
                DebugUtility.LogWarning(typeof(Baseline3LevelsSmokeCommands),
                    $"[FATAL][Smoke][Levels] Missing global service='{label}'.");
                return null;
            }

            return service;
        }

        public static class LevelSmokeSteps
        {
            public static async Task RunD_SwapToL2Async()
            {
                if (!EnsurePlayMode())
                {
                    return;
                }

                Baseline3LevelsSmokeConfigAsset config = ResolveConfigOrFail();
                if (config == null)
                {
                    return;
                }

                var swapLocal = ResolveGlobal<ILevelSwapLocalService>("ILevelSwapLocalService");
                var restartContext = ResolveGlobal<IRestartContextService>("IRestartContextService");
                if (swapLocal == null || restartContext == null)
                {
                    return;
                }

                await swapLocal.SwapLocalAsync(config.SwapTargetLevel, ReasonStepD, CancellationToken.None);
                LogStepEvidence("D", config.SwapTargetLevel, restartContext);
            }

            public static async Task RunE_LevelResetAsync()
            {
                if (!EnsurePlayMode())
                {
                    return;
                }

                Baseline3LevelsSmokeConfigAsset config = ResolveConfigOrFail();
                if (config == null)
                {
                    return;
                }

                var postLevelActions = ResolveGlobal<IPostLevelActionsService>("IPostLevelActionsService");
                var restartContext = ResolveGlobal<IRestartContextService>("IRestartContextService");
                if (postLevelActions == null || restartContext == null)
                {
                    return;
                }

                await postLevelActions.RestartLevelAsync(ReasonStepE, CancellationToken.None);
                LogStepEvidence("E", config.SwapTargetLevel, restartContext);
            }

            public static async Task RunDE_SwapL2ThenResetAsync()
            {
                if (!EnsurePlayMode())
                {
                    return;
                }

                Baseline3LevelsSmokeConfigAsset config = ResolveConfigOrFail();
                if (config == null)
                {
                    return;
                }

                var swapLocal = ResolveGlobal<ILevelSwapLocalService>("ILevelSwapLocalService");
                var postLevelActions = ResolveGlobal<IPostLevelActionsService>("IPostLevelActionsService");
                var restartContext = ResolveGlobal<IRestartContextService>("IRestartContextService");
                if (swapLocal == null || postLevelActions == null || restartContext == null)
                {
                    return;
                }

                await swapLocal.SwapLocalAsync(config.SwapTargetLevel, ReasonStepDE + "/D", CancellationToken.None);
                await postLevelActions.RestartLevelAsync(ReasonStepDE + "/E", CancellationToken.None);
                LogStepEvidence("DE", config.SwapTargetLevel, restartContext);
            }

            private static Baseline3LevelsSmokeConfigAsset ResolveConfigOrFail()
            {
                string[] guids = AssetDatabase.FindAssets("t:Baseline3LevelsSmokeConfigAsset");
                if (guids == null || guids.Length == 0)
                {
                    DebugUtility.LogWarning(typeof(Baseline3LevelsSmokeCommands),
                        "[FATAL][Smoke][Levels] Baseline3LevelsSmokeConfigAsset not found.");
                    return null;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var config = AssetDatabase.LoadAssetAtPath<Baseline3LevelsSmokeConfigAsset>(assetPath);

                if (config == null || !config.IsValid)
                {
                    DebugUtility.LogWarning(typeof(Baseline3LevelsSmokeCommands),
                        "[FATAL][Smoke][Levels] Baseline3LevelsSmokeConfigAsset is missing or incomplete (initialLevel/swapTargetLevel).");
                    return null;
                }

                return config;
            }

            private static void LogStepEvidence(string step, LevelId expectedLevel, IRestartContextService restartContext)
            {
                string activeLevel = "<none>";
                string signature = "<none>";

                if (restartContext.TryGetCurrent(out var snapshot) && snapshot.IsValid)
                {
                    activeLevel = snapshot.HasLevelId ? snapshot.LevelId.ToString() : "<none>";
                    signature = string.IsNullOrWhiteSpace(snapshot.LevelSignature)
                        ? "<none>"
                        : snapshot.LevelSignature;
                }

                DebugUtility.Log(typeof(Baseline3LevelsSmokeCommands),
                    $"[OBS][Smoke][Levels] step={step} expected={expectedLevel} active={activeLevel} signature={signature}.");
            }
        }
    }
}
#endif
