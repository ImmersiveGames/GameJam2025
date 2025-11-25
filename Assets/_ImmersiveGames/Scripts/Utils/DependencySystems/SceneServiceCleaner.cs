using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine.SceneManagement;
namespace _ImmersiveGames.Scripts.Utils.DependencySystems
{
    
    public class SceneServiceCleaner
    {
        private readonly SceneServiceRegistry _sceneRegistry;

        public SceneServiceCleaner(SceneServiceRegistry sceneRegistry)
        {
            _sceneRegistry = sceneRegistry;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            DebugUtility.LogVerbose(
                typeof(SceneServiceCleaner),
                "SceneServiceCleaner inicializado.",
                DebugUtility.Colors.CrucialInfo);
        }

        private void OnSceneUnloaded(Scene scene)
        {
            _sceneRegistry.Clear(scene.name);
            DebugUtility.LogVerbose(
                typeof(SceneServiceCleaner),
                $"Cena {scene.name} descarregada, serviços limpos.",
                DebugUtility.Colors.Success);
        }

        public void Dispose()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            DebugUtility.LogVerbose(
                typeof(SceneServiceCleaner),
                "SceneServiceCleaner finalizado.",
                DebugUtility.Colors.Success);
        }
    }
}