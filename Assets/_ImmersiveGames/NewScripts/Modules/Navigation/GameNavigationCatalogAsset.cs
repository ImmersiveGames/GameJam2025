using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Runtime;
using UnityEngine.Serialization;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    public enum GameNavigationIntentKind
    {
        Menu = 0,
        Gameplay = 1,
        GameOver = 2,
        Victory = 3,
        Restart = 4,
        ExitToMenu = 5,
    }

    [Serializable]
    public struct CoreIntentSlot
    {
        [Tooltip("Referência direta obrigatória para a rota do intent core.")]
        public SceneRouteDefinitionAsset routeRef;

        [FormerlySerializedAs("transitionStyleId")]
        [Tooltip("TransitionStyleId que define ProfileId/UseFade (SceneFlow) usado neste intent core.")]
        public TransitionStyleId styleId;
    }

    /// <summary>
    /// Catálogo de intents de navegação (produção).
    ///
    /// F3/Fase 3:
    /// - Catálogo não define Scene Data.
    /// - Intents core usam slots explícitos por enum.
    /// - Intents extras permanecem extensíveis por lista.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameNavigationCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/Navigation/Catalogs/GameNavigationCatalogAsset",
        order = 30)]
    public sealed class GameNavigationCatalogAsset : ScriptableObject, IGameNavigationCatalog, ISerializationCallbackReceiver
    {
        [Serializable]
        public sealed class RouteEntry
        {
            [Tooltip("Identificador canônico do intent extra/custom.")]
            public string routeId;

            [Tooltip("SceneRouteId associado ao intent extra/custom (fallback quando routeRef não está setado).")]
            [SceneFlowAllowEmptyId]
            public SceneRouteId sceneRouteId;

            [Tooltip("Referência direta opcional para a rota canônica do intent extra/custom.")]
            public SceneRouteDefinitionAsset routeRef;

            [FormerlySerializedAs("transitionStyleId")]
            [Tooltip("TransitionStyleId que define ProfileId/UseFade (SceneFlow) usado nesta navegação.")]
            public TransitionStyleId styleId;

            public SceneRouteId ResolveRouteId(string owner)
            {
                if (routeRef != null)
                {
                    var routeRefId = routeRef.RouteId;
                    if (!routeRefId.IsValid)
                    {
                        return SceneRouteId.None;
                    }

                    if (sceneRouteId.IsValid && sceneRouteId != routeRefId)
                    {
                        HandleRouteMismatch(owner, routeId, sceneRouteId, routeRefId);
                    }

                    DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                        $"[OBS][SceneFlow] RouteResolvedVia=AssetRef owner='{owner}', intentId='{routeId}', routeId='{routeRefId}', asset='{routeRef.name}'.",
                        DebugUtility.Colors.Info);

                    return routeRefId;
                }

                if (sceneRouteId.IsValid)
                {
                    DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                        $"[OBS][SceneFlow] RouteResolvedVia=RouteId owner='{owner}', intentId='{routeId}', routeId='{sceneRouteId}'.",
                        DebugUtility.Colors.Info);

                    return sceneRouteId;
                }

                return SceneRouteId.None;
            }

            public void MigrateLegacy()
            {
                routeId = routeId?.Trim();
            }

            public bool TryAutoSyncFromRouteRef(out bool sceneRouteIdSynced)
            {
                sceneRouteIdSynced = false;

                if (routeRef == null)
                {
                    return false;
                }

                SceneRouteId routeRefId = routeRef.RouteId;
                if (!routeRefId.IsValid)
                {
                    return false;
                }

                if (sceneRouteId != routeRefId)
                {
                    sceneRouteId = routeRefId;
                    sceneRouteIdSynced = true;
                }

                return sceneRouteIdSynced;
            }

            private static void HandleRouteMismatch(string owner, string intentId, SceneRouteId sceneRouteId, SceneRouteId routeRefId)
            {
                string message =
                    $"[FATAL][Config] GameNavigationCatalog routeId divergente de routeRef. " +
                    $"owner='{owner}', intentId='{intentId}', sceneRouteId='{sceneRouteId}', routeRef.routeId='{routeRefId}'.";

                FailFastConfig(message);
            }
        }

        [Header("Catalog Reference")]
        [SerializeField]
        [Tooltip("Referência obrigatória para o catálogo canônico de intents (core + custom).")]
        private GameNavigationIntentCatalogAsset assetRef;

        [Header("Core Intents (slots explícitos)")]
        [SerializeField] private CoreIntentSlot menuSlot;
        [SerializeField] private CoreIntentSlot gameplaySlot;
        [SerializeField] private CoreIntentSlot gameOverSlot;
        [SerializeField] private CoreIntentSlot victorySlot;
        [SerializeField] private CoreIntentSlot restartSlot;
        [SerializeField] private CoreIntentSlot exitToMenuSlot;

        [Header("Extra / Custom Intents")]
        [SerializeField]
        [FormerlySerializedAs("_routes")]
        private List<RouteEntry> routes = new();

        private readonly Dictionary<string, GameNavigationEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
        private bool _built;

        public IReadOnlyCollection<string> RouteIds
        {
            get
            {
                EnsureBuilt();
                return _cache.Keys;
            }
        }

        public GameNavigationIntentCatalogAsset IntentCatalogAssetRef => assetRef;

        public bool TryGet(string routeId, out GameNavigationEntry entry)
        {
            entry = default;

            if (string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            string normalizedIntentId = routeId.Trim();
            if (TryMapIntentIdToCoreKind(normalizedIntentId, out GameNavigationIntentKind coreKind))
            {
                entry = ResolveCoreOrFail(coreKind);
                return entry.IsValid;
            }

            EnsureBuilt();
            return _cache.TryGetValue(normalizedIntentId, out entry);
        }

        public GameNavigationEntry ResolveIntentOrFail(string intentId)
        {
            if (string.IsNullOrWhiteSpace(intentId))
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog intentId inválido/vazio. asset='{name}'.");
            }

            string normalizedIntentId = intentId.Trim();
            if (TryMapIntentIdToCoreKind(normalizedIntentId, out GameNavigationIntentKind coreKind))
            {
                return ResolveCoreOrFail(coreKind);
            }

            EnsureBuilt();
            if (_cache.TryGetValue(normalizedIntentId, out GameNavigationEntry entry) && entry.IsValid)
            {
                return entry;
            }

            FailFastConfig(
                $"[FATAL][Config] GameNavigationCatalog sem intent configurado. asset='{name}', intentId='{normalizedIntentId}'.");
            return default;
        }

        public GameNavigationEntry ResolveCoreOrFail(GameNavigationIntentKind kind)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            string intentId = GetIntentId(kind);

            if (slot.routeRef == null)
            {
                FailFastCoreSlot(kind, "routeRef obrigatório e não configurado para intent core.");
            }

            SceneRouteId routeRefId = slot.routeRef.RouteId;
            if (!routeRefId.IsValid)
            {
                FailFastCoreSlot(kind,
                    $"routeRef.RouteId inválido para intent core. intentId='{intentId}', asset='{slot.routeRef.name}'.");
            }

            if (!slot.styleId.IsValid)
            {
                FailFastCoreSlot(kind, $"styleId inválido para intent core. intentId='{intentId}'.");
            }

            DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                $"[OBS][SceneFlow] RouteResolvedVia=AssetRef owner='{name}', intentId='{intentId}', routeId='{routeRefId}', asset='{slot.routeRef.name}'.",
                DebugUtility.Colors.Info);

            return new GameNavigationEntry(routeRefId, slot.styleId, SceneTransitionPayload.Empty);
        }

        public void GetObservabilitySnapshot(out int rawRoutesCount, out int builtRouteIdsCount, out bool hasToGameplay)
        {
            EnsureBuilt();
            rawRoutesCount = routes?.Count ?? 0;
            builtRouteIdsCount = _cache.Count;
            hasToGameplay = _cache.ContainsKey(GetIntentId(GameNavigationIntentKind.Gameplay));
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            ApplyRouteMigration();
            _built = false;
        }

        private void OnValidate()
        {
            ApplyRouteMigration();
            _built = false;

            ValidateIntentCatalogReferenceOrFail();
            ValidateIntentCatalogCoreCoverageOrFail();

            ValidateCoreSlotsInEditorOrFail();
            ValidateMinimumProductionCoreSlotsInEditorOrFail();
            ValidateExtrasInEditorOrFail();

            int syncedEntriesCount = 0;
            int syncedSceneRouteIdCount = 0;

            if (routes == null)
            {
                return;
            }

            for (int i = 0; i < routes.Count; i++)
            {
                RouteEntry route = routes[i];
                if (route == null)
                {
                    continue;
                }

                if (!route.TryAutoSyncFromRouteRef(out bool sceneRouteIdSynced))
                {
                    continue;
                }

                syncedEntriesCount++;
                if (sceneRouteIdSynced)
                {
                    syncedSceneRouteIdCount++;
                }
            }

            if (syncedEntriesCount > 0)
            {
                DebugUtility.Log(typeof(GameNavigationCatalogAsset),
                    "[OBS][Config] GameNavigationCatalog OnValidate auto-fix aplicado: " +
                    $"entries={syncedEntriesCount}, sceneRouteIdSynced={syncedSceneRouteIdCount}, asset='{name}'.",
                    DebugUtility.Colors.Info);
            }
        }

#if UNITY_EDITOR
        public void ValidateCriticalIntentsInEditor(GameNavigationIntentCatalogAsset intents)
        {
            if (intents == null)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog validação crítica sem GameNavigationIntentCatalogAsset. asset='{name}'.");
            }

            foreach (NavigationIntentId criticalIntent in intents.EnumerateCriticalIntents())
            {
                ValidateCriticalIntentIdInEditorOrFail(criticalIntent);
            }
        }
#endif

        private void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            ApplyRouteMigration();

            _built = true;
            _cache.Clear();

            BuildCoreCacheEntries();
            BuildExtrasCacheEntries();
        }

        private void BuildCoreCacheEntries()
        {
            AddCoreToCacheOrFail(GameNavigationIntentKind.Menu, required: true);
            AddCoreToCacheOrFail(GameNavigationIntentKind.Gameplay, required: true);
            AddCoreToCacheOrFail(GameNavigationIntentKind.GameOver, required: false);
            AddCoreToCacheOrFail(GameNavigationIntentKind.Victory, required: false);
            AddCoreToCacheOrFail(GameNavigationIntentKind.Restart, required: true);
            AddCoreToCacheOrFail(GameNavigationIntentKind.ExitToMenu, required: true);
        }

        private void AddCoreToCacheOrFail(GameNavigationIntentKind kind, bool required)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            string intentId = GetIntentId(kind);

            if (!slot.routeRef)
            {
                if (required)
                {
                    FailFastCoreSlot(kind, "routeRef obrigatório e não configurado para intent core obrigatório.");
                }

                if (slot.styleId.IsValid)
                {
                    FailFastCoreSlot(kind,
                        "slot core parcialmente configurado (styleId setado sem routeRef). Remova styleId ou configure routeRef.");
                }

                return;
            }

            GameNavigationEntry entry = ResolveCoreOrFail(kind);
            if (_cache.ContainsKey(intentId))
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog duplicado para intent core. asset='{name}', intentId='{intentId}'.");
            }

            _cache.Add(intentId, entry);
        }

        private void BuildExtrasCacheEntries()
        {
            if (routes == null)
            {
                return;
            }

            foreach (RouteEntry route in routes)
            {
                if (!TryBuildExtraEntry(route, out string intentId, out GameNavigationEntry entry))
                {
                    continue;
                }

                _cache[intentId] = entry;
            }
        }

        private void ApplyRouteMigration()
        {
            if (routes == null)
            {
                return;
            }

            foreach (RouteEntry route in routes)
            {
                route?.MigrateLegacy();
            }
        }

        private bool TryBuildExtraEntry(RouteEntry route, out string intentId, out GameNavigationEntry entry)
        {
            intentId = string.Empty;
            entry = default;

            if (route == null || string.IsNullOrWhiteSpace(route.routeId))
            {
                return false;
            }

            intentId = route.routeId.Trim();
            if (TryMapIntentIdToCoreKind(intentId, out _))
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog extras não pode usar intent reservado. asset='{name}', intentId='{intentId}'.");
            }

            SceneRouteId resolvedRouteId = route.ResolveRouteId(name);
            if (!resolvedRouteId.IsValid || !route.styleId.IsValid)
            {
                return false;
            }

            entry = new GameNavigationEntry(resolvedRouteId, route.styleId, SceneTransitionPayload.Empty);
            return true;
        }

        private void ValidateCoreSlotsInEditorOrFail()
        {
            ValidateCoreSlotOrFail(GameNavigationIntentKind.Menu, required: false);
            ValidateCoreSlotOrFail(GameNavigationIntentKind.Gameplay, required: false);
            ValidateCoreSlotOrFail(GameNavigationIntentKind.GameOver, required: false);
            ValidateCoreSlotOrFail(GameNavigationIntentKind.Victory, required: false);
            ValidateCoreSlotOrFail(GameNavigationIntentKind.Restart, required: false);
            ValidateCoreSlotOrFail(GameNavigationIntentKind.ExitToMenu, required: false);
        }

        private void ValidateMinimumProductionCoreSlotsInEditorOrFail()
        {
            ValidateCoreSlotOrFail(GameNavigationIntentKind.Menu, required: true);
            ValidateCoreSlotOrFail(GameNavigationIntentKind.Gameplay, required: true);
            ValidateCoreSlotOrFail(GameNavigationIntentKind.Restart, required: true);
            ValidateCoreSlotOrFail(GameNavigationIntentKind.ExitToMenu, required: true);
        }

        private void ValidateCoreSlotOrFail(GameNavigationIntentKind kind, bool required)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            if (!slot.routeRef)
            {
                if (required)
                {
                    FailFastCoreSlot(kind, "slot core obrigatório sem routeRef.");
                }

                if (slot.styleId.IsValid)
                {
                    FailFastCoreSlot(kind,
                        "slot core parcialmente configurado (styleId setado sem routeRef). Remova styleId ou configure routeRef.");
                }

                return;
            }

            ResolveCoreOrFail(kind);
        }

        private void ValidateCriticalIntentIdInEditorOrFail(NavigationIntentId criticalIntent)
        {
            if (!criticalIntent.IsValid)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationIntentCatalog contém intent crítico inválido/vazio. catalogAsset='{name}'.");
            }

            string criticalIntentId = criticalIntent.Value;
            if (TryMapIntentIdToCoreKind(criticalIntentId, out GameNavigationIntentKind coreKind))
            {
                ValidateCoreSlotOrFail(coreKind, required: true);
                return;
            }

            RouteEntry route = FindExtraRouteByIntentId(criticalIntentId);
            if (route == null)
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog sem intent crítico configurado nos extras. asset='{name}', intentId='{criticalIntentId}'.");
            }

            ValidateExtraCriticalRouteOrFail(route, criticalIntentId);
        }

        private RouteEntry FindExtraRouteByIntentId(string intentId)
        {
            if (routes == null)
            {
                return null;
            }

            for (int i = 0; i < routes.Count; i++)
            {
                RouteEntry route = routes[i];
                if (route == null || string.IsNullOrWhiteSpace(route.routeId))
                {
                    continue;
                }

                if (string.Equals(route.routeId.Trim(), intentId, StringComparison.OrdinalIgnoreCase))
                {
                    return route;
                }
            }

            return null;
        }

        private void ValidateExtraCriticalRouteOrFail(RouteEntry route, string intentId)
        {
            if (route.routeRef == null)
            {
                FailFastConfig(
                    $"[FATAL][Config] Intent crítico exige routeRef obrigatório. asset='{name}', intentId='{intentId}'.");
            }

            SceneRouteId routeRefId = route.routeRef.RouteId;
            if (!routeRefId.IsValid)
            {
                FailFastConfig(
                    $"[FATAL][Config] Intent crítico com routeRef inválido. asset='{name}', intentId='{intentId}', routeRef='{route.routeRef.name}'.");
            }

            if (!string.Equals(routeRefId.Value, intentId, StringComparison.OrdinalIgnoreCase))
            {
                FailFastConfig(
                    $"[FATAL][Config] Intent crítico divergente de routeRef.routeId. asset='{name}', intentId='{intentId}', routeRef.routeId='{routeRefId}'.");
            }
        }

        private void ValidateExtrasInEditorOrFail()
        {
            if (routes == null)
            {
                return;
            }

            for (int i = 0; i < routes.Count; i++)
            {
                RouteEntry route = routes[i];
                if (route == null || string.IsNullOrWhiteSpace(route.routeId))
                {
                    continue;
                }

                string intentId = route.routeId.Trim();
                if (TryMapIntentIdToCoreKind(intentId, out _))
                {
                    FailFastConfig(
                        $"[FATAL][Config] GameNavigationCatalog extras não pode usar intent reservado. asset='{name}', index={i}, intentId='{intentId}'.");
                }
            }
        }

        private bool TryMapIntentIdToCoreKind(string intentId, out GameNavigationIntentKind kind)
        {
            string normalized = intentId?.Trim();
            if (string.Equals(normalized, GetIntentId(GameNavigationIntentKind.Menu), StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Menu;
                return true;
            }

            if (string.Equals(normalized, GetIntentId(GameNavigationIntentKind.Gameplay), StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Gameplay;
                return true;
            }

            if (string.Equals(normalized, GetIntentId(GameNavigationIntentKind.GameOver), StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.GameOver;
                return true;
            }

            if (string.Equals(normalized, GetIntentId(GameNavigationIntentKind.Victory), StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Victory;
                return true;
            }

            if (string.Equals(normalized, GetIntentId(GameNavigationIntentKind.Restart), StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Restart;
                return true;
            }

            if (string.Equals(normalized, GetIntentId(GameNavigationIntentKind.ExitToMenu), StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.ExitToMenu;
                return true;
            }

            kind = default;
            return false;
        }

        private string GetIntentId(GameNavigationIntentKind kind)
        {
            ValidateIntentCatalogReferenceOrFail();

            NavigationIntentId intentId;
            switch (kind)
            {
                case GameNavigationIntentKind.Menu:
                    intentId = assetRef.Menu;
                    break;
                case GameNavigationIntentKind.Gameplay:
                    intentId = assetRef.Gameplay;
                    break;
                case GameNavigationIntentKind.GameOver:
                    intentId = assetRef.GameOver;
                    break;
                case GameNavigationIntentKind.Victory:
                    intentId = assetRef.Victory;
                    break;
                case GameNavigationIntentKind.Restart:
                    intentId = assetRef.Restart;
                    break;
                case GameNavigationIntentKind.ExitToMenu:
                    intentId = assetRef.ExitToMenu;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            if (!intentId.IsValid)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog sem intent core canônica no GameNavigationIntentCatalog. asset='{name}', kind='{kind}'.");
            }

            return intentId.Value;
        }

        private void ValidateIntentCatalogReferenceOrFail()
        {
            if (assetRef == null)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog exige assetRef (GameNavigationIntentCatalogAsset). asset='{name}'.");
            }
        }

        private void ValidateIntentCatalogCoreCoverageOrFail()
        {
            ValidateIntentCatalogReferenceOrFail();
            assetRef.EnsureCoreIntentsForProductionOrFail();
        }

        private CoreIntentSlot GetCoreSlot(GameNavigationIntentKind kind)
        {
            switch (kind)
            {
                case GameNavigationIntentKind.Menu:
                    return menuSlot;
                case GameNavigationIntentKind.Gameplay:
                    return gameplaySlot;
                case GameNavigationIntentKind.GameOver:
                    return gameOverSlot;
                case GameNavigationIntentKind.Victory:
                    return victorySlot;
                case GameNavigationIntentKind.Restart:
                    return restartSlot;
                case GameNavigationIntentKind.ExitToMenu:
                    return exitToMenuSlot;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private void FailFastCoreSlot(GameNavigationIntentKind kind, string detail)
        {
            string message =
                $"[FATAL][Config] GameNavigationCatalog inválido para intent core. asset='{name}', kind='{kind}', intentId='{GetIntentId(kind)}', detail='{detail}'";

            FailFastConfig(message);
        }

        private static void FailFastConfig(string message)
        {
            DebugUtility.LogError(typeof(GameNavigationCatalogAsset), message);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }
    }
}
