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
    public sealed class DependencyDISmokeQATester : MonoBehaviour
    {
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

            string sceneName = SceneManager.GetActiveScene().name;
            DebugUtility.Log(typeof(DependencyDISmokeQATester), $"[QA][DI][A] Scene: {sceneName}");
            passes++;

            // B) Registrar serviços dummy
            provider.RegisterGlobal(new DummyGlobalService { Id = "G1" }, allowOverride: true);
            provider.RegisterForScene(sceneName, new DummySceneService { Id = "S1" }, allowOverride: true);

            bool hasGlobal = provider.TryGetGlobal(out DummyGlobalService globalService);
            bool hasScene = provider.TryGetForScene(sceneName, out DummySceneService sceneService);

            if (hasGlobal && globalService != null)
            {
                LogPass("B", $"Global resolved: {globalService.Id}");
                passes++;
            }
            else
            {
                LogFail("B", "Global service not resolved.");
                fails++;
            }

            if (hasScene && sceneService != null)
            {
                LogPass("B", $"Scene resolved: {sceneService.Id}");
                passes++;
            }
            else
            {
                LogFail("B", "Scene service not resolved.");
                fails++;
            }

            // C) Validar InjectDependencies
            var consumer = new DummyConsumer();
            provider.InjectDependencies(consumer);
            DebugUtility.Log(typeof(DependencyDISmokeQATester), $"[QA][DI][C] {consumer.Report()}");
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
            List<Type> scenes = provider.ListServicesForScene(sceneName);
            DebugUtility.Log(typeof(DependencyDISmokeQATester),
                $"[QA][DI][D] Global services: {globals.Count} [{string.Join(", ", globals.Select(t => t.Name))}]");
            DebugUtility.Log(typeof(DependencyDISmokeQATester),
                $"[QA][DI][D] Scene services ({sceneName}): {scenes.Count} [{string.Join(", ", scenes.Select(t => t.Name))}]");
            passes += 2;

            // E) Limpeza de cena
            provider.ClearSceneServices(sceneName);
            if (!provider.TryGetForScene<DummySceneService>(sceneName, out _))
            {
                LogPass("E", "Scene services cleared successfully.");
                passes++;
            }
            else
            {
                LogFail("E", "Scene services still present after ClearSceneServices.");
                fails++;
            }

            DebugUtility.Log(typeof(DependencyDISmokeQATester),
                $"[QA][DI] QA complete. Passes={passes} Fails={fails}",
                fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);
        }

        private static void LogPass(string step, string message)
        {
            DebugUtility.Log(typeof(DependencyDISmokeQATester), $"[QA][DI][{step}] PASS - {message}",
                DebugUtility.Colors.Success);
        }

        private static void LogFail(string step, string message)
        {
            DebugUtility.LogError(typeof(DependencyDISmokeQATester), $"[QA][DI][{step}] FAIL - {message}");
        }

        private sealed class DummyGlobalService
        {
            public string Id;
        }

        private sealed class DummySceneService
        {
            public string Id;
        }

        private sealed class DummyConsumer
        {
            [Inject] private DummyGlobalService _global;
            [Inject] private DummySceneService _scene;

            public bool HasGlobal => _global != null;
            public bool HasScene => _scene != null;
            public string Report() => $"HasGlobal={HasGlobal} HasScene={HasScene} G={_global?.Id} S={_scene?.Id}";
        }
    }
}
