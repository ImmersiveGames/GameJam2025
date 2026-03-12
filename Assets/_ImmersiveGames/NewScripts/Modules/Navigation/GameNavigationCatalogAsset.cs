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
        [Tooltip("ReferÃªncia direta obrigatÃ³ria para a rota do intent core.")]
        public SceneRouteDefinitionAsset routeRef;

        [FormerlySerializedAs("transitionStyleId")]
        [Tooltip("TransitionStyleId que define ProfileId/UseFade (SceneFlow) usado neste intent core.")]
        public TransitionStyleId styleId;
    }

    /// <summary>
    /// CatÃ¡logo de intents de navegaÃ§Ã£o (produÃ§Ã£o).
    ///
    /// F3/Fase 3:
    /// - CatÃ¡logo nÃ£o define Scene Data.
    /// - Intents core usam slots explÃ­citos por enum.
    /// - Intents extras permanecem extensÃ­veis por lista.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameNavigationCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Modules/Navigation/Catalogs/GameNavigationCatalogAsset",
        order = 30)]
    public sealed partial class GameNavigationCatalogAsset : ScriptableObject, IGameNavigationCatalog, ISerializationCallbackReceiver
    {
        private static readonly GameNavigationIntentKind[] RequiredCoreIntents =
        {
            GameNavigationIntentKind.Menu,
            GameNavigationIntentKind.Gameplay,
        };

        private static readonly GameNavigationIntentKind[] OptionalCoreIntents =
        {
            GameNavigationIntentKind.GameOver,
            GameNavigationIntentKind.Victory,
            GameNavigationIntentKind.Restart,
            GameNavigationIntentKind.ExitToMenu,
        };
        [Serializable]
        public sealed class RouteEntry
        {
            [Tooltip("Identificador tipado do intent extra/custom.")]
            public NavigationIntentId intentId;

            [Tooltip("Referencia direta obrigatoria para a rota canonica do intent extra/custom.")]
            public SceneRouteDefinitionAsset routeRef;

            [FormerlySerializedAs("transitionStyleId")]
            [Tooltip("TransitionStyleId que define ProfileId/UseFade (SceneFlow) usado nesta navegacao.")]
            public TransitionStyleId styleId;

            public SceneRouteId ResolveRouteRefIdOrFail(string owner)
            {
                string resolvedIntentId = ResolveIntentId().Value;

                if (routeRef == null)
                {
                    FailFastConfig(
                        $"[FATAL][Config] GameNavigationCatalog extra sem routeRef obrigatorio. owner='{owner}', intentId='{resolvedIntentId}'.");
                }

                SceneRouteId routeRefId = routeRef.RouteId;
                if (!routeRefId.IsValid)
                {
                    FailFastConfig(
                        $"[FATAL][Config] GameNavigationCatalog extra com routeRef invalido. owner='{owner}', intentId='{resolvedIntentId}', asset='{routeRef.name}'.");
                }

                DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                    $"[OBS][SceneFlow] RouteResolvedVia=AssetRef owner='{owner}', intentId='{resolvedIntentId}', routeId='{routeRefId}', asset='{routeRef.name}'.",
                    DebugUtility.Colors.Info);

                return routeRefId;
            }

            public NavigationIntentId ResolveIntentId()
            {
                if (!intentId.IsValid)
                {
                    return NavigationIntentId.None;
                }

                return NavigationIntentId.FromName(intentId.Value);
            }
        }
        [Header("Catalog Reference")]
        [SerializeField]
        [FormerlySerializedAs("assetRef")]
        [Tooltip("ReferÃªncia opcional para o catÃ¡logo canÃ´nico de intents (core + custom).")]
        private GameNavigationIntentCatalogAsset intentCatalog;

        [Header("Core Intents (slots explÃ­citos)")]
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

        public GameNavigationIntentCatalogAsset IntentCatalogAssetRef => intentCatalog;

        public bool TryGet(string routeId, out GameNavigationEntry entry)
        {
            entry = default;

            if (string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            string normalizedIntentId = NavigationIntentId.Normalize(routeId);
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
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog intentId invÃ¡lido/vazio. asset='{name}'.");
            }

            string normalizedIntentId = NavigationIntentId.Normalize(intentId);
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
                FailFastCoreSlot(kind, "routeRef obrigatÃ³rio e nÃ£o configurado para intent core.");
            }

            SceneRouteId routeRefId = slot.routeRef.RouteId;
            if (!routeRefId.IsValid)
            {
                FailFastCoreSlot(kind,
                    $"routeRef.RouteId invÃ¡lido para intent core. intentId='{intentId}', asset='{slot.routeRef.name}'.");
            }

            if (!slot.styleId.IsValid)
            {
                FailFastCoreSlot(kind, $"styleId invÃ¡lido para intent core. intentId='{intentId}'.");
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
            _built = false;
        }

        private void OnValidate()
        {
            _built = false;

            ValidateCriticalCoreSlotsInEditorOrFail();
            ValidateOptionalCoreSlotsInEditor();
            ValidateCoreIntentRouteInvariantsOrFail();
            LogMissingOptionalIntentsObservability();
            ValidateExtrasInEditorOrFail();


        }

#if UNITY_EDITOR
        public void ValidateCriticalIntentsInEditor(GameNavigationIntentCatalogAsset intents)
        {
            intentCatalog = intents;
            ValidateCriticalCoreSlotsInEditorOrFail();
        }
#endif

        private void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            _cache.Clear();

            BuildCoreCacheEntries();
            ValidateCoreIntentRouteInvariantsOrFail();
            BuildExtrasCacheEntries();

            _built = true;
        }

        private void ValidateCoreIntentRouteInvariantsOrFail()
        {
            SceneRouteId menuRouteId = ResolveRequiredCoreRouteIdOrFail(GameNavigationIntentKind.Menu);
            SceneRouteId gameplayRouteId = ResolveRequiredCoreRouteIdOrFail(GameNavigationIntentKind.Gameplay);

            ValidateExitToMenuPointsToMenuOrFail(menuRouteId);
            ValidateRestartPointsToGameplayOrFail(menuRouteId, gameplayRouteId);
        }

        private SceneRouteId ResolveRequiredCoreRouteIdOrFail(GameNavigationIntentKind kind)
        {
            if (!TryGetIntentId(kind, out NavigationIntentId intentId))
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog sem intent core canÃ´nica obrigatÃ³ria. owner='{name}', kind='{kind}', intentId='<missing>', routeId='<none>', asset='{name}'.");
            }

            CoreIntentSlot slot = GetCoreSlot(kind);
            if (slot.routeRef == null)
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog core intent obrigatÃ³rio sem routeRef. owner='{name}', kind='{kind}', intentId='{intentId.Value}', routeId='<none>', asset='{name}'.");
            }

            SceneRouteId routeId = slot.routeRef.RouteId;
            if (!routeId.IsValid)
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog core intent obrigatÃ³rio com routeId invÃ¡lido. owner='{name}', kind='{kind}', intentId='{intentId.Value}', routeId='{routeId}', asset='{slot.routeRef.name}'.");
            }

            return routeId;
        }

        private void ValidateExitToMenuPointsToMenuOrFail(SceneRouteId menuRouteId)
        {
            if (!TryGetIntentId(GameNavigationIntentKind.ExitToMenu, out NavigationIntentId intentId))
            {
                return;
            }

            CoreIntentSlot slot = GetCoreSlot(GameNavigationIntentKind.ExitToMenu);
            if (slot.routeRef == null)
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog core intent invÃ¡lido: ExitToMenu sem routeRef. owner='{name}', kind='{GameNavigationIntentKind.ExitToMenu}', intentId='{intentId.Value}', routeId='<none>', asset='{name}'.");
            }

            SceneRouteId routeId = slot.routeRef.RouteId;
            bool matchesMenuRoute = routeId.IsValid && routeId == menuRouteId;
            bool matchesMenuRouteRef = slot.routeRef == GetCoreSlot(GameNavigationIntentKind.Menu).routeRef;
            if (matchesMenuRoute || matchesMenuRouteRef)
            {
                return;
            }

            FailFastConfig(
                $"[FATAL][Config] GameNavigationCatalog core intent invÃ¡lido: ExitToMenu deve resolver para menu. owner='{name}', kind='{GameNavigationIntentKind.ExitToMenu}', intentId='{intentId.Value}', routeId='{routeId}', asset='{slot.routeRef.name}'.");
        }

        private void ValidateRestartPointsToGameplayOrFail(SceneRouteId menuRouteId, SceneRouteId gameplayRouteId)
        {
            if (!TryGetIntentId(GameNavigationIntentKind.Restart, out NavigationIntentId intentId))
            {
                return;
            }

            CoreIntentSlot slot = GetCoreSlot(GameNavigationIntentKind.Restart);
            if (slot.routeRef == null)
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog core intent invÃ¡lido: Restart sem routeRef. owner='{name}', kind='{GameNavigationIntentKind.Restart}', intentId='{intentId.Value}', routeId='<none>', asset='{name}'.");
            }

            SceneRouteId routeId = slot.routeRef.RouteId;
            if (routeId == menuRouteId)
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog core intent invÃ¡lido: Restart nÃ£o pode resolver para menu. owner='{name}', kind='{GameNavigationIntentKind.Restart}', intentId='{intentId.Value}', routeId='{routeId}', asset='{slot.routeRef.name}'.");
            }

            if (routeId != gameplayRouteId)
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog core intent invÃ¡lido: Restart deve resolver para gameplay. owner='{name}', kind='{GameNavigationIntentKind.Restart}', intentId='{intentId.Value}', routeId='{routeId}', asset='{slot.routeRef.name}'.");
            }
        }

        private void BuildCoreCacheEntries()
        {
            AddCoreToCacheOrFail(GameNavigationIntentKind.Menu, required: true);
            AddCoreToCacheOrFail(GameNavigationIntentKind.Gameplay, required: true);
            AddCoreToCacheOrFail(GameNavigationIntentKind.GameOver, required: false);
            AddCoreToCacheOrFail(GameNavigationIntentKind.Victory, required: false);
            AddCoreToCacheOrFail(GameNavigationIntentKind.Restart, required: false);
            AddCoreToCacheOrFail(GameNavigationIntentKind.ExitToMenu, required: false);
        }

        private void AddCoreToCacheOrFail(GameNavigationIntentKind kind, bool required)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            bool hasCanonicalIntentId = TryGetIntentId(kind, out NavigationIntentId intentIdValue);

            if (!hasCanonicalIntentId)
            {
                if (required)
                {
                    FailFastConfig(
                        $"[FATAL][Config] GameNavigationCatalog sem intent core obrigatÃ³ria no GameNavigationIntentCatalog. asset='{name}', kind='{kind}'.");
                }

                DebugUtility.Log(typeof(GameNavigationCatalogAsset),
                    $"[OBS][SceneFlow][Config] Optional core intent ausente no GameNavigationIntentCatalog durante build de cache (permitido). owner='{name}', kind='{kind}'.",
                    DebugUtility.Colors.Info);
                return;
            }

            string intentId = intentIdValue.Value;
            if (!required)
            {
                if (!TryBuildOptionalCoreEntry(kind, intentId, slot, out GameNavigationEntry optionalEntry))
                {
                    return;
                }

                if (_cache.ContainsKey(intentId))
                {
                    FailFastConfig(
                        $"[FATAL][Config] GameNavigationCatalog duplicado para intent core. asset='{name}', intentId='{intentId}'.");
                }

                _cache.Add(intentId, optionalEntry);
                return;
            }

            if (!slot.routeRef)
            {
                FailFastCoreSlot(kind, "routeRef obrigatÃ³rio e nÃ£o configurado para intent core obrigatÃ³rio.");
            }

            GameNavigationEntry entry = ResolveCoreOrFail(kind);
            if (_cache.ContainsKey(intentId))
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog duplicado para intent core. asset='{name}', intentId='{intentId}'.");
            }

            _cache.Add(intentId, entry);
        }


        private bool TryBuildOptionalCoreEntry(
            GameNavigationIntentKind kind,
            string intentId,
            CoreIntentSlot slot,
            out GameNavigationEntry entry)
        {
            entry = default;

            if (slot.routeRef == null)
            {
                if (slot.styleId.IsValid)
                {
                    DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                        $"[WARN][SceneFlow][Config] Optional core intent ignorado no cache por configuraÃ§Ã£o parcial (styleId sem routeRef). owner='{name}', kind='{kind}', intentId='{intentId}'.");
                }

                return false;
            }

            SceneRouteId routeRefId = slot.routeRef.RouteId;
            if (!routeRefId.IsValid)
            {
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    $"[WARN][SceneFlow][Config] Optional core intent ignorado no cache por routeRef invÃ¡lido. owner='{name}', kind='{kind}', intentId='{intentId}', routeAsset='{slot.routeRef.name}'.");
                return false;
            }

            if (!slot.styleId.IsValid)
            {
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    $"[WARN][SceneFlow][Config] Optional core intent ignorado no cache por styleId invÃ¡lido. owner='{name}', kind='{kind}', intentId='{intentId}', routeId='{routeRefId}'.");
                return false;
            }

            DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                $"[OBS][SceneFlow] RouteResolvedVia=AssetRef owner='{name}', intentId='{intentId}', routeId='{routeRefId}', asset='{slot.routeRef.name}'.",
                DebugUtility.Colors.Info);

            entry = new GameNavigationEntry(routeRefId, slot.styleId, SceneTransitionPayload.Empty);
            return true;
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


        private bool TryBuildExtraEntry(RouteEntry route, out string intentId, out GameNavigationEntry entry)
        {
            intentId = string.Empty;
            entry = default;

            if (route == null)
            {
                return false;
            }

            NavigationIntentId typedIntentId = route.ResolveIntentId();
            if (!typedIntentId.IsValid)
            {
                return false;
            }

            intentId = typedIntentId.Value;
            if (TryMapIntentIdToCoreKind(intentId, out _))
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog extras nÃ£o pode usar intent reservado. asset='{name}', intentId='{intentId}'.");
            }

            SceneRouteId resolvedRouteId = route.ResolveRouteRefIdOrFail(name);
            if (!resolvedRouteId.IsValid)
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog extra com routeRef invÃ¡lido apÃ³s resoluÃ§Ã£o. asset='{name}', intentId='{intentId}'.");
            }

            if (!route.styleId.IsValid)
            {
                FailFastConfig(
                    $"[FATAL][Config] GameNavigationCatalog extra com styleId invÃ¡lido. asset='{name}', intentId='{intentId}'.");
            }

            entry = new GameNavigationEntry(resolvedRouteId, route.styleId, SceneTransitionPayload.Empty);
            return true;
        }

        private void ValidateCriticalCoreSlotsInEditorOrFail()
        {
            List<GameNavigationIntentKind> criticalIntents = ResolveCriticalCoreIntentsForValidation();
            for (int i = 0; i < criticalIntents.Count; i++)
            {
                ValidateCoreSlotOrFail(criticalIntents[i], required: true);
            }
        }

        private List<GameNavigationIntentKind> ResolveCriticalCoreIntentsForValidation()
        {
            // Regra canÃ´nica (ADR-0019): fail-fast somente para os slots core obrigatÃ³rios.
            return new List<GameNavigationIntentKind>(RequiredCoreIntents);
        }

        private void ValidateOptionalCoreSlotsInEditor()
        {
            for (int i = 0; i < OptionalCoreIntents.Length; i++)
            {
                ValidateOptionalCoreSlotInEditor(OptionalCoreIntents[i]);
            }
        }

        private void ValidateCoreSlotOrFail(GameNavigationIntentKind kind, bool required)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            if (!slot.routeRef)
            {
                if (required)
                {
                    FailFastCoreSlot(kind, "slot core obrigatÃ³rio sem routeRef.");
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

        private void ValidateOptionalCoreSlotInEditor(GameNavigationIntentKind kind)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            bool hasCanonicalIntentId = TryGetIntentId(kind, out NavigationIntentId intentIdValue);
            string intentId = hasCanonicalIntentId ? intentIdValue.Value : "<missing-in-intent-catalog>";

            if (!hasCanonicalIntentId)
            {
                DebugUtility.Log(typeof(GameNavigationCatalogAsset),
                    $"[OBS][SceneFlow][Config] Optional core intent ausente no GameNavigationIntentCatalog (permitido). owner='{name}', kind='{kind}'.",
                    DebugUtility.Colors.Info);
            }

            if (slot.routeRef == null)
            {
                if (slot.styleId.IsValid)
                {
                    DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                        $"[WARN][SceneFlow][Config] Optional core intent parcialmente configurado (styleId sem routeRef). owner='{name}', kind='{kind}', intentId='{intentId}'.");
                }
                else
                {
                    DebugUtility.Log(typeof(GameNavigationCatalogAsset),
                        $"[OBS][SceneFlow][Config] Optional core intent ausente (permitido). owner='{name}', kind='{kind}', intentId='{intentId}'.",
                        DebugUtility.Colors.Info);
                }

                return;
            }

            SceneRouteId routeRefId = slot.routeRef.RouteId;
            if (!routeRefId.IsValid)
            {
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    $"[WARN][SceneFlow][Config] Optional core intent com routeRef invÃ¡lido. owner='{name}', kind='{kind}', intentId='{intentId}', routeAsset='{slot.routeRef.name}'.");
                return;
            }

            if (!slot.styleId.IsValid)
            {
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    $"[WARN][SceneFlow][Config] Optional core intent com styleId invÃ¡lido. owner='{name}', kind='{kind}', intentId='{intentId}', routeId='{routeRefId}'.");
                return;
            }

            DebugUtility.Log(typeof(GameNavigationCatalogAsset),
                $"[OBS][SceneFlow][Config] Optional core intent configurado. owner='{name}', kind='{kind}', intentId='{intentId}', routeId='{routeRefId}'.",
                DebugUtility.Colors.Info);
        }

        private void LogMissingOptionalIntentsObservability()
        {
            List<string> optionalCoreIntentIds = CollectOptionalCoreIntentIdsFromCatalog();
            if (optionalCoreIntentIds.Count == 0)
            {
                return;
            }

            List<string> missingOptionalIntentIds = new List<string>();
            for (int i = 0; i < optionalCoreIntentIds.Count; i++)
            {
                string intentId = optionalCoreIntentIds[i];
                if (IsIntentMapped(intentId))
                {
                    continue;
                }

                missingOptionalIntentIds.Add(intentId);
            }

            if (missingOptionalIntentIds.Count == 0)
            {
                return;
            }

            DebugUtility.Log(typeof(GameNavigationCatalogAsset),
                $"[OBS][Config] MissingOptionalIntents=[{string.Join(",", missingOptionalIntentIds)}] asset='{name}'.",
                DebugUtility.Colors.Info);
        }

        private List<string> CollectOptionalCoreIntentIdsFromCatalog()
        {
            List<string> optional = new List<string>();
            if (intentCatalog == null)
            {
                AddOptionalIntentIdIfValid(optional, NavigationIntentId.FromName("victory"));
                AddOptionalIntentIdIfValid(optional, NavigationIntentId.FromName("defeat"));
                AddOptionalIntentIdIfValid(optional, NavigationIntentId.FromName("restart"));
                AddOptionalIntentIdIfValid(optional, NavigationIntentId.FromName("exit-to-menu"));
                AddOptionalIntentIdIfValid(optional, NavigationIntentId.FromName("gameover"));
                return optional;
            }

            AddOptionalIntentIdIfValid(optional, intentCatalog.Victory);
            AddOptionalIntentIdIfValid(optional, intentCatalog.Defeat);
            AddOptionalIntentIdIfValid(optional, intentCatalog.Restart);
            AddOptionalIntentIdIfValid(optional, intentCatalog.ExitToMenu);
            AddOptionalIntentIdIfValid(optional, intentCatalog.GameOver);

            return optional;
        }

        private static void AddOptionalIntentIdIfValid(List<string> optional, NavigationIntentId intentId)
        {
            if (!intentId.IsValid)
            {
                return;
            }

            string value = intentId.Value;
            for (int i = 0; i < optional.Count; i++)
            {
                if (string.Equals(optional[i], value, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            optional.Add(value);
        }

        private bool IsIntentMapped(string intentId)
        {
            string normalizedIntentId = NavigationIntentId.Normalize(intentId);
            if (string.IsNullOrWhiteSpace(normalizedIntentId))
            {
                return false;
            }

            if (TryMapIntentIdToCoreKind(normalizedIntentId, out GameNavigationIntentKind kind))
            {
                CoreIntentSlot slot = GetCoreSlot(kind);
                if (slot.routeRef == null || !slot.styleId.IsValid)
                {
                    return false;
                }

                return slot.routeRef.RouteId.IsValid;
            }

            if (routes == null)
            {
                return false;
            }

            for (int i = 0; i < routes.Count; i++)
            {
                RouteEntry route = routes[i];
                if (route == null)
                {
                    continue;
                }

                NavigationIntentId routeIntentId = route.ResolveIntentId();
                if (!routeIntentId.IsValid)
                {
                    continue;
                }

                if (!string.Equals(routeIntentId.Value, normalizedIntentId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                SceneRouteId resolvedRouteId = route.ResolveRouteRefIdOrFail(name);
                return resolvedRouteId.IsValid && route.styleId.IsValid;
            }

            return false;
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
                if (route == null)
                {
                    continue;
                }

                NavigationIntentId routeIntentId = route.ResolveIntentId();
                if (!routeIntentId.IsValid)
                {
                    continue;
                }

                string intentId = routeIntentId.Value;
                if (TryMapIntentIdToCoreKind(intentId, out _))
                {
                    FailFastConfig(
                        $"[FATAL][Config] GameNavigationCatalog extras nÃ£o pode usar intent reservado. asset='{name}', index={i}, intentId='{intentId}'.");
                }

                if (route.routeRef == null)
                {
                    FailFastConfig(
                        $"[FATAL][Config] GameNavigationCatalog extra sem routeRef obrigatÃ³rio. asset='{name}', index={i}, intentId='{intentId}'.");
                }

                SceneRouteId resolvedRouteId = route.routeRef.RouteId;
                if (!resolvedRouteId.IsValid)
                {
                    FailFastConfig(
                        $"[FATAL][Config] GameNavigationCatalog extra com routeRef invÃ¡lido. asset='{name}', index={i}, intentId='{intentId}', routeAsset='{route.routeRef.name}'.");
                }
            }
        }

        private bool TryMapIntentIdToCoreKind(string intentId, out GameNavigationIntentKind kind)
        {
            string normalized = NavigationIntentId.Normalize(intentId);
            if (TryGetIntentId(GameNavigationIntentKind.Menu, out NavigationIntentId menuIntentId) &&
                string.Equals(normalized, menuIntentId.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Menu;
                return true;
            }

            if (TryGetIntentId(GameNavigationIntentKind.Gameplay, out NavigationIntentId gameplayIntentId) &&
                string.Equals(normalized, gameplayIntentId.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Gameplay;
                return true;
            }

            NavigationIntentId defeatIntentId = intentCatalog != null
                ? intentCatalog.Defeat
                : NavigationIntentId.FromName("defeat");

            // ComentÃ¡rio: defeat Ã© intent opcional/compat, resolvida para o mesmo trilho de gameplay.
            if (defeatIntentId.IsValid && string.Equals(normalized, defeatIntentId.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Gameplay;
                return true;
            }

            if (TryGetIntentId(GameNavigationIntentKind.GameOver, out NavigationIntentId gameOverIntentId) &&
                string.Equals(normalized, gameOverIntentId.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.GameOver;
                return true;
            }

            if (TryGetIntentId(GameNavigationIntentKind.Victory, out NavigationIntentId victoryIntentId) &&
                string.Equals(normalized, victoryIntentId.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Victory;
                return true;
            }

            if (TryGetIntentId(GameNavigationIntentKind.Restart, out NavigationIntentId restartIntentId) &&
                string.Equals(normalized, restartIntentId.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.Restart;
                return true;
            }

            if (TryGetIntentId(GameNavigationIntentKind.ExitToMenu, out NavigationIntentId exitToMenuIntentId) &&
                string.Equals(normalized, exitToMenuIntentId.Value, StringComparison.OrdinalIgnoreCase))
            {
                kind = GameNavigationIntentKind.ExitToMenu;
                return true;
            }

            kind = default;
            return false;
        }

        private string GetIntentId(GameNavigationIntentKind kind)
        {
            if (!TryGetIntentId(kind, out NavigationIntentId intentId))
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog sem intent core canÃ´nica no GameNavigationIntentCatalog. asset='{name}', kind='{kind}'.");
            }

            return intentId.Value;
        }

        private bool TryGetIntentId(GameNavigationIntentKind kind, out NavigationIntentId intentId)
        {
            switch (kind)
            {
                case GameNavigationIntentKind.Menu:
                    if (intentCatalog == null)
                    {
                        intentId = NavigationIntentId.FromName("to-menu");
                        return true;
                    }

                    intentId = intentCatalog.Menu;
                    break;
                case GameNavigationIntentKind.Gameplay:
                    if (intentCatalog == null)
                    {
                        intentId = NavigationIntentId.FromName("to-gameplay");
                        return true;
                    }

                    intentId = intentCatalog.Gameplay;
                    break;
                case GameNavigationIntentKind.GameOver:
                    intentId = intentCatalog != null ? intentCatalog.GameOver : NavigationIntentId.FromName("gameover");
                    break;
                case GameNavigationIntentKind.Victory:
                    intentId = intentCatalog != null ? intentCatalog.Victory : NavigationIntentId.FromName("victory");
                    break;
                case GameNavigationIntentKind.Restart:
                    intentId = intentCatalog != null ? intentCatalog.Restart : NavigationIntentId.FromName("restart");
                    break;
                case GameNavigationIntentKind.ExitToMenu:
                    intentId = intentCatalog != null ? intentCatalog.ExitToMenu : NavigationIntentId.FromName("exit-to-menu");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            return intentId.IsValid;
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
                $"[FATAL][Config] GameNavigationCatalog invÃ¡lido para intent core. asset='{name}', kind='{kind}', intentId='{GetIntentId(kind)}', detail='{detail}'";

            FailFastConfig(message);
        }

        private static void FailFastConfig(string message)
        {
            DebugUtility.LogError(typeof(GameNavigationCatalogAsset), message);
#if UNITY_EDITOR
            StopPlayModeInEditor();
#else
            Application.Quit();
#endif
            throw new InvalidOperationException(message);
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static partial void StopPlayModeInEditor();
#endif

    }
}




