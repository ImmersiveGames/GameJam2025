using System;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.SceneComposition;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Foundation.Platform.Composition
{
    /// <summary>
    /// Inicializa servicos minimos de escopo de cena para o NewScripts e garante limpeza deterministica.
    /// </summary>
    public sealed class SceneScopeCompositionRoot : MonoBehaviour
    {
        private string _sceneName = string.Empty;
        private bool _registered;

        private void Awake()
        {
            var scene = gameObject.scene;
            _sceneName = scene.name;

            if (_registered)
            {
                DebugUtility.LogVerbose(typeof(SceneScopeCompositionRoot),
                    $"Scene scope already created (ignored): {_sceneName}");
                return;
            }

            var provider = DependencyManager.Provider;
            provider.RegisterForScene<ISceneScopeMarker>(
                _sceneName,
                new SceneScopeMarker(),
                allowOverride: false);

            var worldRoot = EnsureWorldRoot(scene);
            provider.RegisterForScene(
                _sceneName,
                new SceneWorldRootContext(_sceneName, worldRoot),
                allowOverride: false);

            _registered = true;
            DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                $"scene scope created scene='{_sceneName}' worldRoot='{BuildTransformPath(worldRoot)}'.",
                DebugUtility.Colors.Info);
        }

        private void OnDestroy()
        {
            if (!_registered)
            {
                return;
            }

            if (string.IsNullOrEmpty(_sceneName))
            {
                _sceneName = gameObject.scene.name;
            }

            DependencyManager.Provider.ClearSceneServices(_sceneName);
            DebugUtility.Log(typeof(SceneScopeCompositionRoot),
                $"scene scope cleared scene='{_sceneName}'.",
                DebugUtility.Colors.Info);

            _registered = false;
        }

        private Transform EnsureWorldRoot(Scene scene)
        {
            var targetScene = scene.IsValid() ? scene : SceneManager.GetActiveScene();
            if (!targetScene.IsValid())
            {
                throw new InvalidOperationException("[FATAL][Config][SceneScope] Target scene is invalid while ensuring WorldRoot.");
            }

            GameObject[] rootObjects = targetScene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i] != null && rootObjects[i].name == "WorldRoot")
                {
                    return rootObjects[i].transform;
                }
            }

            var worldRootGo = new GameObject("WorldRoot");
            SceneManager.MoveGameObjectToScene(worldRootGo, targetScene);
            return worldRootGo.transform;
        }

        private static string BuildTransformPath(Transform transform)
        {
            if (transform == null)
            {
                return "<null>";
            }

            return $"{transform.gameObject.scene.name}/{transform.name}";
        }

        private sealed class SceneWorldRootContext
        {
            public SceneWorldRootContext(string sceneName, Transform worldRoot)
            {
                SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
                WorldRoot = worldRoot;
            }

            public string SceneName { get; }
            public Transform WorldRoot { get; }
        }
    }
}
