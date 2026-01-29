#nullable enable
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Providers
{
    /// <summary>
    /// Provider padrão do LevelCatalog via Resources.
    /// </summary>
    public sealed class ResourcesLevelCatalogProvider : ILevelCatalogProvider
    {
        public const string DefaultResourcesPath = "Levels/LevelCatalog";

        private readonly string _resourcePath;
        private LevelCatalog? _cached;
        private bool _attempted;

        public ResourcesLevelCatalogProvider(string? resourcePath = null)
        {
            _resourcePath = string.IsNullOrWhiteSpace(resourcePath) ? DefaultResourcesPath : resourcePath.Trim();
        }

        public LevelCatalog? GetCatalog()
        {
            if (_cached != null)
            {
                return _cached;
            }

            if (_attempted)
            {
                return null;
            }

            _attempted = true;

            var catalog = Resources.Load<LevelCatalog>(_resourcePath);
            if (catalog != null)
            {
                _cached = catalog;
                DebugUtility.LogVerbose<ResourcesLevelCatalogProvider>(
                    $"[LevelCatalog] Catalog resolvido via Resources. path='{_resourcePath}', name='{catalog.name}'.");
                return _cached;
            }

            var any = Resources.Load(_resourcePath);
            if (any != null)
            {
                DebugUtility.LogWarning<ResourcesLevelCatalogProvider>(
                    $"[LevelCatalog] Asset encontrado em Resources no path '{_resourcePath}', mas com tipo incorreto: '{any.GetType().FullName}'. " +
                    $"Esperado: '{typeof(LevelCatalog).FullName}'.");
                return null;
            }

            DebugUtility.LogWarning<ResourcesLevelCatalogProvider>(
                $"[LevelCatalog] LevelCatalog não encontrado em Resources. path='{_resourcePath}'.");
            return null;
        }
    }
}
