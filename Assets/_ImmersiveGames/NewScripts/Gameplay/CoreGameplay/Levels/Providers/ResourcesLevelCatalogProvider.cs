#nullable enable
using System;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Levels.Catalogs;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Levels.Providers
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
        private string _cachedSource = string.Empty;
        private bool _cacheHitLogged;

        public ResourcesLevelCatalogProvider(string? resourcePath = null)
        {
            _resourcePath = string.IsNullOrWhiteSpace(resourcePath) ? DefaultResourcesPath : resourcePath.Trim();
        }

        public LevelCatalog? GetCatalog()
        {
            if (_cached != null)
            {
                // Observabilidade: quando o catalog já está em cache, ainda queremos um sinal único no log.
                // Isso evita “buracos” de evidência quando o primeiro load ocorreu muito antes do QA/Apply.
                if (!_cacheHitLogged)
                {
                    _cacheHitLogged = true;
                    DebugUtility.LogVerbose<ResourcesLevelCatalogProvider>(
                        $"[OBS][LevelCatalog] CatalogCacheHit name='{_cached.name}' source='{_cachedSource}'.");
                }

                return _cached;
            }

            if (_attempted)
            {
                return null;
            }

            _attempted = true;

            if (TryLoadCatalog(_resourcePath, LegacyResourcesPath, out var catalog, out string source))
            {
                _cached = catalog;
                _cachedSource = source;
                LogCatalogLoaded(_cached, source);
                return _cached;
            }

            if (!string.Equals(_resourcePath, LegacyResourcesPath, StringComparison.Ordinal)
                && TryLoadCatalog(LegacyResourcesPath, "none", out catalog, out source))
            {
                _cached = catalog;
                _cachedSource = source;
                LogCatalogLoaded(_cached, source);
                return _cached;
            }

            DebugUtility.LogWarning<ResourcesLevelCatalogProvider>(
                $"[LevelCatalog] LevelCatalog não encontrado em Resources. path='{_resourcePath}', fallback='{LegacyResourcesPath}'.");
            return null;
        }

        private static bool TryLoadCatalog(string path, string fallback, out LevelCatalog catalog, out string source)
        {
            catalog = Resources.Load<LevelCatalog>(path);
            source = string.Equals(path, LegacyResourcesPath, StringComparison.Ordinal) ? "legacy" : "new";

            if (catalog != null)
            {
                DebugUtility.Log<ResourcesLevelCatalogProvider>(
                    $"[OBS][LevelCatalog] CatalogLoadAttempt path='{path}' result='hit' fallback='{fallback}'.");
                DebugUtility.LogVerbose<ResourcesLevelCatalogProvider>(
                    $"[LevelCatalog] Catalog resolvido via Resources. path='{path}', name='{catalog.name}'.");
                return true;
            }

            DebugUtility.Log<ResourcesLevelCatalogProvider>(
                $"[OBS][LevelCatalog] CatalogLoadAttempt path='{path}' result='miss' fallback='{fallback}'.");

            var any = Resources.Load(path);
            if (any != null)
            {
                DebugUtility.LogWarning<ResourcesLevelCatalogProvider>(
                    $"[LevelCatalog] Asset encontrado em Resources no path '{path}', mas com tipo incorreto: '{any.GetType().FullName}'. " +
                    $"Esperado: '{typeof(LevelCatalog).FullName}'.");
            }

            return false;
        }

        private static void LogCatalogLoaded(LevelCatalog? catalog, string source)
        {
            string initial = catalog?.InitialLevelId ?? string.Empty;
            int orderedCount = catalog?.OrderedLevels?.Count ?? 0;
            int definitionsCount = catalog?.Definitions?.Count ?? 0;

            DebugUtility.Log<ResourcesLevelCatalogProvider>(
                $"[OBS][LevelCatalog] CatalogLoaded name='{catalog?.name}' initial='{initial}' orderedCount='{orderedCount}' definitionsCount='{definitionsCount}' source='{source}'.");
        }
    }
}
