#nullable enable
using System;
using _ImmersiveGames.NewScripts.Gameplay.Levels.Catalogs;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels.Providers
{
    /// <summary>
    /// Provider padrão do LevelCatalog via Resources.
    /// </summary>
    public sealed class ResourcesLevelCatalogProvider : ILevelCatalogProvider
    {
        public const string DefaultResourcesPath = "NewScripts/Config/LevelCatalog";
        public const string LegacyResourcesPath = "Levels/LevelCatalog";

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

            if (TryLoadCatalog(_resourcePath, out var catalog))
            {
                _cached = catalog;
                return _cached;
            }

            if (!string.Equals(_resourcePath, LegacyResourcesPath, StringComparison.Ordinal)
                && TryLoadCatalog(LegacyResourcesPath, out catalog))
            {
                _cached = catalog;
                return _cached;
            }

            DebugUtility.LogWarning<ResourcesLevelCatalogProvider>(
                $"[LevelCatalog] LevelCatalog não encontrado em Resources. path='{_resourcePath}', fallback='{LegacyResourcesPath}'.");
            return null;
        }

        private static bool TryLoadCatalog(string path, out LevelCatalog catalog)
        {
            catalog = Resources.Load<LevelCatalog>(path);
            if (catalog != null)
            {
                DebugUtility.LogVerbose<ResourcesLevelCatalogProvider>(
                    $"[LevelCatalog] Catalog resolvido via Resources. path='{path}', name='{catalog.name}'.");
                return true;
            }

            var any = Resources.Load(path);
            if (any != null)
            {
                DebugUtility.LogWarning<ResourcesLevelCatalogProvider>(
                    $"[LevelCatalog] Asset encontrado em Resources no path '{path}', mas com tipo incorreto: '{any.GetType().FullName}'. " +
                    $"Esperado: '{typeof(LevelCatalog).FullName}'.");
            }

            return false;
        }
    }
}
