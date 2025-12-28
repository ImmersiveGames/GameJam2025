#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Smoke test manual para validar registro, resolução e injeção do DI do NewScripts.
    /// </summary>
    public sealed class DependencyDiSmokeQaTester : MonoBehaviour
    {
        private const string QaScene = "__QA_DI__";

        [ContextMenu("QA/DI/Run Smoke")]
        public void RunSmoke()
        {
            int passes = 0;
            int fails = 0;

            // A) Boot/Provider
            var provider = DependencyManager.Provider;
            if (provider == null)
            {
                LogFail("A", "DependencyManager.Provider is null.");
                fails++;
                return;
            }

            string activeSceneName = SceneManager.GetActiveScene().name;
            DebugUtility.Log(typeof(DependencyDiSmokeQaTester), $"[QA][DI][A] Scene: {activeSceneName}");
            passes++;

            // B) Registrar serviços dummy
            provider.RegisterGlobal(new DummyGlobalService { id = "G1" }, allowOverride: true);
            provider.RegisterForScene(QaScene, new DummySceneService { id = "S_QA" }, allowOverride: true);
            provider.RegisterForScene(activeSceneName, new DummySceneService { id = "S_ACTIVE" }, allowOverride: true);

            bool hasGlobal = provider.TryGetGlobal(out DummyGlobalService globalService);
            bool hasScene = provider.TryGetForScene(QaScene, out DummySceneService sceneService);

            if (hasGlobal && globalService != null)
            {
                LogPass("B", $"Global resolved: {globalService.id}");
                passes++;
            }
            else
            {
                LogFail("B", "Global service not resolved.");
                fails++;
            }

            if (hasScene && sceneService != null)
            {
                LogPass("B", $"Scene resolved ({QaScene}): {sceneService.id}");
                passes++;
            }
            else
            {
                LogFail("B", $"Scene service not resolved for {QaScene}.");
                fails++;
            }

            // C) Validar InjectDependencies
            var consumer = new DummyConsumer();
            provider.InjectDependencies(consumer);
            DebugUtility.Log(typeof(DependencyDiSmokeQaTester), $"[QA][DI][C] {consumer.Report()}");
            if (consumer.HasGlobal && consumer.HasScene)
            {
                LogPass("C", "InjectDependencies filled all fields.");
                passes++;
            }
            else
            {
                LogFail("C", "InjectDependencies missing services.");
                fails++;
            }

            // D) Listagem (opcional)
            List<Type> globals = provider.ListGlobalServices();
            List<Type> scenes = provider.ListServicesForScene(QaScene);
            DebugUtility.Log(typeof(DependencyDiSmokeQaTester),
                $"[QA][DI][D] Global services: {globals.Count} [{string.Join(", ", globals.Select(t => t.Name))}]");
            DebugUtility.Log(typeof(DependencyDiSmokeQaTester),
                $"[QA][DI][D] Scene services ({QaScene}): {scenes.Count} [{string.Join(", ", scenes.Select(t => t.Name))}]");
            passes += 2;

            // E) Limpeza de cena
            provider.ClearSceneServices(QaScene);
            if (provider.ListServicesForScene(QaScene).Count == 0)
            {
                LogPass("E", $"Scene services cleared successfully for {QaScene}.");
                passes++;
            }
            else
            {
                LogFail("E", $"Scene services still present for {QaScene} after ClearSceneServices.");
                fails++;
            }

            DebugUtility.Log(typeof(DependencyDiSmokeQaTester),
                $"[QA][DI] QA complete. Passes={passes} Fails={fails}",
                fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
        }

        private static void LogPass(string step, string message)
        {
            DebugUtility.Log(typeof(DependencyDiSmokeQaTester), $"[QA][DI][{step}] PASS - {message}",
                DebugUtility.Colors.Success);
        }

        private static void LogFail(string step, string message)
        {
            DebugUtility.LogError(typeof(DependencyDiSmokeQaTester), $"[QA][DI][{step}] FAIL - {message}");
        }

        private sealed class DummyGlobalService
        {
            public string id;
        }

        private sealed class DummySceneService
        {
            public string id;
        }

        private sealed class DummyConsumer
        {
            [Inject] private DummyGlobalService _global;
            [Inject] private DummySceneService _scene;

            public bool HasGlobal => _global != null;
            public bool HasScene => _scene != null;
            public string Report() => $"HasGlobal={HasGlobal} HasScene={HasScene} G={_global?.id} S={_scene?.id}";
        }

        /// <summary>
        /// Wrapper para execução pelo runner agregado.
        /// </summary>
        public void Run()
        {
            RunSmoke();
        }
    }
}

#endif
