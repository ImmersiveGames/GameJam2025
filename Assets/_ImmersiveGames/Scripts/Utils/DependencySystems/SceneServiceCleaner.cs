using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.Utils.DependencySystems
{
    [DebugLevel(DebugLevel.Error)]
    public class SceneServiceCleaner
    {
        private readonly SceneServiceRegistry _sceneRegistry;

        public SceneServiceCleaner(SceneServiceRegistry sceneRegistry)
        {
            _sceneRegistry = sceneRegistry;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            DebugUtility.LogVerbose(typeof(SceneServiceCleaner), "SceneServiceCleaner inicializado.", "yellow");
        }

        private void OnSceneUnloaded(Scene scene)
        {
            _sceneRegistry.Clear(scene.name);
            DebugUtility.LogVerbose(typeof(SceneServiceCleaner), $"Cena {scene.name} descarregada, serviços limpos.", "yellow");
        }

        public void Dispose()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            DebugUtility.LogVerbose(typeof(SceneServiceCleaner), "SceneServiceCleaner finalizado.", "yellow");
        }
    }
}