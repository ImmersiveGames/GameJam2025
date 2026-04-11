using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Experience.Audio.Config;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Bindings;
using _ImmersiveGames.NewScripts.Orchestration.SceneFlow.Navigation.Runtime;
using _ImmersiveGames.NewScripts.Orchestration.PhaseDefinition;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Orchestration.Navigation
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
        [Tooltip("Referencia direta obrigatoria para a rota do intent core.")]
        public SceneRouteDefinitionAsset routeRef;

        [Tooltip("Referencia direta obrigatoria para o TransitionStyleAsset canonico.")]
        public TransitionStyleAsset transitionStyleRef;

        [Tooltip("BGM opcional para este intent core.")]
        public AudioBgmCueAsset bgmCueRef;
    }

    [CreateAssetMenu(
        fileName = "GameNavigationCatalogAsset",
        menuName = "ImmersiveGames/NewScripts/Orchestration/Navigation/Catalogs/GameNavigationCatalogAsset",
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
        };

        private static readonly GameNavigationIntentKind[] CompatibilityIntents =
        {
            GameNavigationIntentKind.Restart,
            GameNavigationIntentKind.ExitToMenu,
        };

        [Serializable]
        public sealed class RouteEntry
        {
            public NavigationIntentId intentId;
            public SceneRouteDefinitionAsset routeRef;
            public TransitionStyleAsset transitionStyleRef;
            public AudioBgmCueAsset bgmCueRef;

            public SceneRouteId ResolveRouteRefIdOrFail(string owner)
            {
                string resolvedIntentId = ResolveIntentId().Value;
                if (routeRef == null)
                {
                    FailFastConfig($"[FATAL][Config] GameNavigationCatalog extra sem routeRef obrigatorio. owner='{owner}', intentId='{resolvedIntentId}'.");
                }

                SceneRouteId routeRefId = routeRef.RouteId;
                if (!routeRefId.IsValid)
                {
                    FailFastConfig($"[FATAL][Config] GameNavigationCatalog extra com routeRef invalido. owner='{owner}', intentId='{resolvedIntentId}', asset='{routeRef.name}'.");
                }

                DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                    $"[OBS][NavigationCore] RouteResolvedVia=AssetRef owner='{owner}', intentId='{resolvedIntentId}', routeId='{routeRefId}', asset='{routeRef.name}'.",
                    DebugUtility.Colors.Info);

                return routeRefId;
            }

            public NavigationIntentId ResolveIntentId()
            {
                return intentId.IsValid ? NavigationIntentId.FromName(intentId.Value) : NavigationIntentId.None;
            }
        }

        [Header("Core Intents (slots explicitos)")]
        [SerializeField] private CoreIntentSlot menuSlot;
        [SerializeField] private CoreIntentSlot gameplaySlot;
        [SerializeField] private CoreIntentSlot gameOverSlot;
        [SerializeField] private CoreIntentSlot victorySlot;
        [SerializeField] private CoreIntentSlot restartSlot;
        [SerializeField] private CoreIntentSlot exitToMenuSlot;

        [Header("Extra / Custom Intents")]
        [SerializeField] private List<RouteEntry> routes = new();

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

        public bool TryGet(string routeId, out GameNavigationEntry entry)
        {
            entry = default;
            if (string.IsNullOrWhiteSpace(routeId))
            {
                return false;
            }

            string normalizedIntentId = NavigationIntentId.Normalize(routeId);
            if (TryMapToKnownKind(routeId, out GameNavigationIntentKind coreKind))
            {
                entry = ResolveKindOrFail(coreKind);
                return entry.IsValid;
            }

            EnsureBuilt();
            return _cache.TryGetValue(normalizedIntentId, out entry);
        }

        public SceneRouteDefinitionAsset ResolveGameplayRouteRefOrFail()
        {
            GameNavigationEntry gameplayEntry = ResolveCoreOrFail(GameNavigationIntentKind.Gameplay);
            if (gameplayEntry.RouteRef == null)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog gameplay core intent sem routeRef obrigatorio. asset='{name}', intentId='{GetIntentId(GameNavigationIntentKind.Gameplay)}'.");
            }

            if (gameplayEntry.RouteRef.RouteKind != SceneRouteKind.Gameplay)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog gameplay core intent exige RouteKind.Gameplay. asset='{name}', intentId='{GetIntentId(GameNavigationIntentKind.Gameplay)}', routeId='{gameplayEntry.RouteRef.RouteId}', routeKind='{gameplayEntry.RouteRef.RouteKind}'.");
            }

            return gameplayEntry.RouteRef;
        }

        public bool IsGameplayPhaseEnabledOrFail()
        {
            SceneRouteDefinitionAsset gameplayRouteRef = ResolveGameplayRouteRefOrFail();
            bool phaseEnabled = gameplayRouteRef.PhaseDefinitionCatalog != null;

            DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                $"[OBS][NavigationCore] route-driven phase enablement resolved routeKind='{gameplayRouteRef.RouteKind}' phaseCatalogPresent={phaseEnabled} phaseEnabled={phaseEnabled}.",
                DebugUtility.Colors.Info);

            return phaseEnabled;
        }

        public PhaseDefinitionCatalogAsset ResolveGameplayPhaseCatalogOrFail()
        {
            SceneRouteDefinitionAsset gameplayRouteRef = ResolveGameplayRouteRefOrFail();
            PhaseDefinitionCatalogAsset phaseDefinitionCatalog = gameplayRouteRef.PhaseDefinitionCatalog;
            if (phaseDefinitionCatalog == null)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog gameplay core intent requires PhaseDefinitionCatalog when phase-enabled. asset='{name}', intentId='{GetIntentId(GameNavigationIntentKind.Gameplay)}', routeId='{gameplayRouteRef.RouteId}'.");
            }

            return phaseDefinitionCatalog;
        }

        public GameNavigationEntry ResolveIntentOrFail(string intentId)
        {
            if (string.IsNullOrWhiteSpace(intentId))
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog intentId invalido/vazio. asset='{name}'.");
            }

            string normalizedIntentId = NavigationIntentId.Normalize(intentId);
            if (TryMapToKnownKind(intentId, out GameNavigationIntentKind coreKind))
            {
                return ResolveKindOrFail(coreKind);
            }

            EnsureBuilt();
            if (_cache.TryGetValue(normalizedIntentId, out GameNavigationEntry entry) && entry.IsValid)
            {
                return entry;
            }

            FailFastConfig($"[FATAL][Config] GameNavigationCatalog sem intent configurado. asset='{name}', intentId='{normalizedIntentId}'.");
            return default;
        }

        public GameNavigationEntry ResolveCoreOrFail(GameNavigationIntentKind kind)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            string intentId = GetIntentId(kind);
            if (slot.routeRef == null)
            {
                FailFastCoreSlot(kind, "routeRef obrigatorio e nao configurado para intent core.");
            }

            if (slot.transitionStyleRef == null)
            {
                FailFastCoreSlot(kind, $"transitionStyleRef obrigatorio ausente para intent core. intentId='{intentId}'.");
            }

            SceneRouteId routeRefId = slot.routeRef.RouteId;
            if (!routeRefId.IsValid)
            {
                FailFastCoreSlot(kind, $"routeRef.RouteId invalido para intent core. intentId='{intentId}', asset='{slot.routeRef.name}'.");
            }

            DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                $"[OBS][NavigationCore] RouteResolvedVia=AssetRef owner='{name}', intentId='{intentId}', routeId='{routeRefId}', asset='{slot.routeRef.name}'.",
                DebugUtility.Colors.Info);

            return new GameNavigationEntry(routeRefId, slot.transitionStyleRef, SceneTransitionPayload.Empty, slot.routeRef);
        }

        private GameNavigationEntry ResolveCompatibilityOrFail(GameNavigationIntentKind kind)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            string intentId = GetCompatibilityIntentId(kind).Value;
            if (slot.routeRef == null)
            {
                FailFastCompatibilitySlot(kind, "routeRef obrigatorio e nao configurado para intent de compatibilidade.");
            }

            if (slot.transitionStyleRef == null)
            {
                FailFastCompatibilitySlot(kind, $"transitionStyleRef obrigatorio ausente para intent de compatibilidade. intentId='{intentId}'.");
            }

            SceneRouteId routeRefId = slot.routeRef.RouteId;
            if (!routeRefId.IsValid)
            {
                FailFastCompatibilitySlot(kind, $"routeRef.RouteId invalido para intent de compatibilidade. intentId='{intentId}', asset='{slot.routeRef.name}'.");
            }

            DebugUtility.LogVerbose(typeof(GameNavigationCatalogAsset),
                $"[OBS][NavigationCore] RouteResolvedVia=AssetRef owner='{name}', intentId='{intentId}', routeId='{routeRefId}', asset='{slot.routeRef.name}'.",
                DebugUtility.Colors.Info);

            return new GameNavigationEntry(routeRefId, slot.transitionStyleRef, SceneTransitionPayload.Empty, slot.routeRef);
        }

        private GameNavigationEntry ResolveKindOrFail(GameNavigationIntentKind kind)
        {
            if (IsCoreIntentKind(kind))
            {
                return ResolveCoreOrFail(kind);
            }

            if (IsCompatibilityIntentKind(kind))
            {
                return ResolveCompatibilityOrFail(kind);
            }

            throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        public bool TryResolveBgmCueByRoute(SceneRouteId routeId, out AudioBgmCueAsset cue, out string owner)
        {
            cue = null;
            owner = string.Empty;

            if (!routeId.IsValid)
            {
                return false;
            }

            if (TryResolveCoreBgmCueByRoute(routeId, out cue, out owner))
            {
                return cue != null;
            }

            if (TryResolveCompatibilityBgmCueByRoute(routeId, out cue, out owner))
            {
                return cue != null;
            }

            if (TryResolveExtraBgmCueByRoute(routeId, out cue, out owner))
            {
                return cue != null;
            }

            return false;
        }

        public void GetObservabilitySnapshot(out int rawRoutesCount, out int builtRouteIdsCount, out bool hasToGameplay)
        {
            EnsureBuilt();
            rawRoutesCount = routes?.Count ?? 0;
            builtRouteIdsCount = _cache.Count;
            hasToGameplay = _cache.ContainsKey(GetIntentId(GameNavigationIntentKind.Gameplay));
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { _built = false; }

        private void OnValidate()
        {
            _built = false;
            ValidateCriticalCoreSlotsInEditorOrFail();
            ValidateOptionalCoreSlotsInEditor();
            ValidateCanonicalCoreIntentRouteInvariantsOrFail();
            ValidateCompatibilityIntentRouteInvariantsOrFail();
            LogMissingOptionalIntentsObservability();
            LogMissingCompatibilityIntentsObservability();
            ValidateExtrasInEditorOrFail();
        }

#if UNITY_EDITOR
        public void ValidateCriticalIntentsInEditor()
        {
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
            ValidateCanonicalCoreIntentRouteInvariantsOrFail();
            ValidateCompatibilityIntentRouteInvariantsOrFail();
            BuildExtrasCacheEntries();
            _built = true;
        }

        private void ValidateCanonicalCoreIntentRouteInvariantsOrFail()
        {
            SceneRouteId menuRouteId = ResolveRequiredCoreRouteIdOrFail(GameNavigationIntentKind.Menu);
            SceneRouteId gameplayRouteId = ResolveRequiredCoreRouteIdOrFail(GameNavigationIntentKind.Gameplay);
            ValidateCanonicalCoreIntentRouteCoverageOrFail(menuRouteId, gameplayRouteId);
        }

        private void ValidateCompatibilityIntentRouteInvariantsOrFail()
        {
            SceneRouteId menuRouteId = ResolveRequiredCoreRouteIdOrFail(GameNavigationIntentKind.Menu);
            SceneRouteId gameplayRouteId = ResolveRequiredCoreRouteIdOrFail(GameNavigationIntentKind.Gameplay);
            ValidateExitToMenuPointsToMenuOrFail(menuRouteId);
            ValidateRestartPointsToGameplayOrFail(menuRouteId, gameplayRouteId);
        }

        private void ValidateCanonicalCoreIntentRouteCoverageOrFail(SceneRouteId menuRouteId, SceneRouteId gameplayRouteId)
        {
            if (!menuRouteId.IsValid)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog core canonico sem menu valido. owner='{name}', routeId='{menuRouteId}', asset='{name}'.");
            }

            if (!gameplayRouteId.IsValid)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog core canonico sem gameplay valido. owner='{name}', routeId='{gameplayRouteId}', asset='{name}'.");
            }
        }

        private SceneRouteId ResolveRequiredCoreRouteIdOrFail(GameNavigationIntentKind kind)
        {
            if (!TryGetIntentId(kind, out NavigationIntentId intentId))
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog sem intent core canonica obrigatoria. owner='{name}', kind='{kind}', intentId='<missing>', routeId='<none>', asset='{name}'.");
            }

            CoreIntentSlot slot = GetCoreSlot(kind);
            if (slot.routeRef == null)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog core intent obrigatorio sem routeRef. owner='{name}', kind='{kind}', intentId='{intentId.Value}', routeId='<none>', asset='{name}'.");
            }

            SceneRouteId routeId = slot.routeRef.RouteId;
            if (!routeId.IsValid)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog core intent obrigatorio com routeId invalido. owner='{name}', kind='{kind}', intentId='{intentId.Value}', routeId='{routeId}', asset='{slot.routeRef.name}'.");
            }

            return routeId;
        }

        private void ValidateExitToMenuPointsToMenuOrFail(SceneRouteId menuRouteId)
        {
            NavigationIntentId intentId = GetCompatibilityIntentId(GameNavigationIntentKind.ExitToMenu);
            CoreIntentSlot slot = GetCoreSlot(GameNavigationIntentKind.ExitToMenu);
            if (slot.routeRef == null)
            {
                return;
            }

            SceneRouteId routeId = slot.routeRef.RouteId;
            bool matchesMenuRoute = routeId.IsValid && routeId == menuRouteId;
            bool matchesMenuRouteRef = slot.routeRef == GetCoreSlot(GameNavigationIntentKind.Menu).routeRef;
            if (matchesMenuRoute || matchesMenuRouteRef)
            {
                return;
            }

            FailFastConfig($"[FATAL][Config] GameNavigationCatalog core intent invalido: ExitToMenu deve resolver para menu. owner='{name}', kind='{GameNavigationIntentKind.ExitToMenu}', intentId='{intentId.Value}', routeId='{routeId}', asset='{slot.routeRef.name}'.");
        }

        private void ValidateRestartPointsToGameplayOrFail(SceneRouteId menuRouteId, SceneRouteId gameplayRouteId)
        {
            NavigationIntentId intentId = GetCompatibilityIntentId(GameNavigationIntentKind.Restart);
            CoreIntentSlot slot = GetCoreSlot(GameNavigationIntentKind.Restart);
            if (slot.routeRef == null)
            {
                return;
            }

            SceneRouteId routeId = slot.routeRef.RouteId;
            if (routeId == menuRouteId)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog core intent invalido: Restart nao pode resolver para menu. owner='{name}', kind='{GameNavigationIntentKind.Restart}', intentId='{intentId.Value}', routeId='{routeId}', asset='{slot.routeRef.name}'.");
            }

            if (routeId != gameplayRouteId)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog core intent invalido: Restart deve resolver para gameplay. owner='{name}', kind='{GameNavigationIntentKind.Restart}', intentId='{intentId.Value}', routeId='{routeId}', asset='{slot.routeRef.name}'.");
            }
        }

        private void BuildCoreCacheEntries()
        {
            AddCoreToCacheOrFail(GameNavigationIntentKind.Menu, true);
            AddCoreToCacheOrFail(GameNavigationIntentKind.Gameplay, true);
            AddCoreToCacheOrFail(GameNavigationIntentKind.GameOver, false);
            AddCoreToCacheOrFail(GameNavigationIntentKind.Victory, false);
            BuildCompatibilityCacheEntries();
        }

        private void BuildCompatibilityCacheEntries()
        {
            AddCompatibilityToCacheOrFail(GameNavigationIntentKind.Restart);
            AddCompatibilityToCacheOrFail(GameNavigationIntentKind.ExitToMenu);
        }

        private void AddCoreToCacheOrFail(GameNavigationIntentKind kind, bool required)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            bool hasCanonicalIntentId = TryGetIntentId(kind, out NavigationIntentId intentIdValue);
            if (!hasCanonicalIntentId)
            {
                if (required)
                {
                    FailFastConfig($"[FATAL][Config] GameNavigationCatalog sem intent core obrigatoria canonica em codigo. asset='{name}', kind='{kind}'.");
                }

                DebugUtility.Log(typeof(GameNavigationCatalogAsset),
                    $"[OBS][NavigationCore][Config] Optional core intent ausente na fonte canonica em codigo durante build de cache (permitido). owner='{name}', kind='{kind}'.",
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
                    FailFastConfig($"[FATAL][Config] GameNavigationCatalog duplicado para intent core. asset='{name}', intentId='{intentId}'.");
                }

                _cache.Add(intentId, optionalEntry);
                return;
            }

            if (slot.routeRef == null)
            {
                FailFastCoreSlot(kind, "routeRef obrigatorio e nao configurado para intent core obrigatorio.");
            }

            GameNavigationEntry entry = ResolveCoreOrFail(kind);
            if (_cache.ContainsKey(intentId))
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog duplicado para intent core. asset='{name}', intentId='{intentId}'.");
            }

            _cache.Add(intentId, entry);
        }

        private void AddCompatibilityToCacheOrFail(GameNavigationIntentKind kind)
        {
            NavigationIntentId intentId = GetCompatibilityIntentId(kind);
            GameNavigationEntry entry = ResolveCompatibilityOrFail(kind);
            if (_cache.ContainsKey(intentId.Value))
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog duplicado para intent compatibilidade. asset='{name}', intentId='{intentId.Value}'.");
            }

            _cache.Add(intentId.Value, entry);
        }

        private bool TryBuildOptionalCoreEntry(GameNavigationIntentKind kind, string intentId, CoreIntentSlot slot, out GameNavigationEntry entry)
        {
            entry = default;
            if (slot.routeRef == null)
            {
                if (slot.transitionStyleRef != null)
                {
                    DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                        $"[WARN][NavigationCore][Config] Optional core intent ignorado no cache por configuracao parcial (transitionStyleRef sem routeRef). owner='{name}', kind='{kind}', intentId='{intentId}'.");
                }

                return false;
            }

            if (slot.transitionStyleRef == null)
            {
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    $"[WARN][NavigationCore][Config] Optional core intent ignorado no cache por transitionStyleRef ausente. owner='{name}', kind='{kind}', intentId='{intentId}'.");
                return false;
            }

            SceneRouteId routeRefId = slot.routeRef.RouteId;
            if (!routeRefId.IsValid)
            {
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    $"[WARN][NavigationCore][Config] Optional core intent ignorado no cache por routeRef invalido. owner='{name}', kind='{kind}', intentId='{intentId}', routeAsset='{slot.routeRef.name}'.");
                return false;
            }

            entry = new GameNavigationEntry(routeRefId, slot.transitionStyleRef, SceneTransitionPayload.Empty, slot.routeRef);
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
                if (TryBuildExtraEntry(route, out string intentId, out GameNavigationEntry entry))
                {
                    _cache[intentId] = entry;
                }
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
            if (GameNavigationIntents.TryMapToCoreKind(intentId, out _))
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog extras nao pode usar intent reservado. asset='{name}', intentId='{intentId}'.");
            }

            SceneRouteId resolvedRouteId = route.ResolveRouteRefIdOrFail(name);
            if (!resolvedRouteId.IsValid)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog extra com routeRef invalido apos resolucao. asset='{name}', intentId='{intentId}'.");
            }

            if (route.transitionStyleRef == null)
            {
                FailFastConfig($"[FATAL][Config] GameNavigationCatalog extra sem transitionStyleRef obrigatorio. asset='{name}', intentId='{intentId}'.");
            }

            entry = new GameNavigationEntry(resolvedRouteId, route.transitionStyleRef, SceneTransitionPayload.Empty, route.routeRef);
            return true;
        }

        private bool TryResolveCoreBgmCueByRoute(SceneRouteId routeId, out AudioBgmCueAsset cue, out string owner)
        {
            cue = null;
            owner = string.Empty;

            foreach (GameNavigationIntentKind kind in RequiredCoreIntents)
            {
                if (TryResolveCoreSlotBgmCue(kind, routeId, out cue, out owner))
                {
                    return true;
                }
            }

            foreach (GameNavigationIntentKind kind in OptionalCoreIntents)
            {
                if (TryResolveCoreSlotBgmCue(kind, routeId, out cue, out owner))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryResolveCompatibilityBgmCueByRoute(SceneRouteId routeId, out AudioBgmCueAsset cue, out string owner)
        {
            cue = null;
            owner = string.Empty;

            foreach (GameNavigationIntentKind kind in CompatibilityIntents)
            {
                if (TryResolveCoreSlotBgmCue(kind, routeId, out cue, out owner))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryResolveCoreSlotBgmCue(
            GameNavigationIntentKind kind,
            SceneRouteId routeId,
            out AudioBgmCueAsset cue,
            out string owner)
        {
            cue = null;
            owner = string.Empty;

            CoreIntentSlot slot = GetCoreSlot(kind);
            if (slot.routeRef == null || !slot.routeRef.RouteId.IsValid || slot.routeRef.RouteId != routeId)
            {
                return false;
            }

            cue = slot.bgmCueRef;
            owner = $"core:{kind}:{GetIntentId(kind)}";
            return cue != null;
        }

        private bool TryResolveExtraBgmCueByRoute(SceneRouteId routeId, out AudioBgmCueAsset cue, out string owner)
        {
            cue = null;
            owner = string.Empty;

            if (routes == null)
            {
                return false;
            }

            for (int i = 0; i < routes.Count; i++)
            {
                RouteEntry route = routes[i];
                if (route == null || route.routeRef == null || !route.routeRef.RouteId.IsValid || route.routeRef.RouteId != routeId)
                {
                    continue;
                }

                if (route.bgmCueRef == null)
                {
                    continue;
                }

                NavigationIntentId intentId = route.ResolveIntentId();
                owner = intentId.IsValid ? $"extra:{intentId.Value}" : $"extra:index:{i}";
                cue = route.bgmCueRef;
                return true;
            }

            return false;
        }

        private void ValidateCriticalCoreSlotsInEditorOrFail()
        {
            foreach (GameNavigationIntentKind kind in RequiredCoreIntents)
            {
                ValidateCoreSlotOrFail(kind, true);
            }
        }

        private void ValidateOptionalCoreSlotsInEditor()
        {
            foreach (GameNavigationIntentKind kind in OptionalCoreIntents)
            {
                ValidateOptionalCoreSlotInEditor(kind);
            }
        }

        private void ValidateCoreSlotOrFail(GameNavigationIntentKind kind, bool required)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            if (!slot.routeRef)
            {
                if (required)
                {
                    FailFastCoreSlot(kind, "slot core obrigatorio sem routeRef.");
                }

                if (slot.transitionStyleRef != null)
                {
                    FailFastCoreSlot(kind, "slot core parcialmente configurado (transitionStyleRef sem routeRef).");
                }

                return;
            }

            if (slot.transitionStyleRef == null)
            {
                FailFastCoreSlot(kind, "slot core sem transitionStyleRef obrigatorio.");
            }

            ResolveCoreOrFail(kind);
        }

        private void ValidateOptionalCoreSlotInEditor(GameNavigationIntentKind kind)
        {
            CoreIntentSlot slot = GetCoreSlot(kind);
            bool hasCanonicalIntentId = TryGetIntentId(kind, out NavigationIntentId intentIdValue);
            string intentId = hasCanonicalIntentId ? intentIdValue.Value : "<missing-in-code>";

            if (slot.routeRef == null)
            {
                if (slot.transitionStyleRef != null)
                {
                    DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                        $"[WARN][NavigationCore][Config] Optional core intent parcialmente configurado (transitionStyleRef sem routeRef). owner='{name}', kind='{kind}', intentId='{intentId}'.");
                }

                return;
            }

            if (slot.transitionStyleRef == null)
            {
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    $"[WARN][NavigationCore][Config] Optional core intent sem transitionStyleRef. owner='{name}', kind='{kind}', intentId='{intentId}'.");
                return;
            }

            if (!slot.routeRef.RouteId.IsValid)
            {
                DebugUtility.LogWarning(typeof(GameNavigationCatalogAsset),
                    $"[WARN][NavigationCore][Config] Optional core intent com routeRef invalido. owner='{name}', kind='{kind}', intentId='{intentId}', routeAsset='{slot.routeRef.name}'.");
            }
        }

        private void LogMissingOptionalIntentsObservability()
        {
            List<string> missingOptionalIntentIds = new List<string>();
            foreach (GameNavigationIntentKind optional in OptionalCoreIntents)
            {
                string optionalIntentId = GetIntentId(optional);
                if (!IsIntentMapped(optionalIntentId))
                {
                    missingOptionalIntentIds.Add(optionalIntentId);
                }
            }

            if (missingOptionalIntentIds.Count > 0)
            {
                DebugUtility.Log(typeof(GameNavigationCatalogAsset),
                    $"[OBS][NavigationCore][Config] MissingOptionalIntents=[{string.Join(",", missingOptionalIntentIds)}] asset='{name}'.",
                    DebugUtility.Colors.Info);
            }
        }

        private void LogMissingCompatibilityIntentsObservability()
        {
            List<string> missingCompatibilityIntentIds = new List<string>();
            foreach (GameNavigationIntentKind compatibility in CompatibilityIntents)
            {
                string compatibilityIntentId = GetCompatibilityIntentId(compatibility);
                if (!IsIntentMapped(compatibilityIntentId))
                {
                    missingCompatibilityIntentIds.Add(compatibilityIntentId);
                }
            }

            if (missingCompatibilityIntentIds.Count > 0)
            {
                DebugUtility.Log(typeof(GameNavigationCatalogAsset),
                    $"[OBS][NavigationCompatibility][Config] MissingCompatibilityIntents=[{string.Join(",", missingCompatibilityIntentIds)}] asset='{name}'.",
                    DebugUtility.Colors.Info);
            }
        }

        private bool IsIntentMapped(string intentId)
        {
            string normalizedIntentId = NavigationIntentId.Normalize(intentId);
            if (TryMapToKnownKind(intentId, out GameNavigationIntentKind kind))
            {
                CoreIntentSlot slot = GetCoreSlot(kind);
                return slot.routeRef != null && slot.transitionStyleRef != null && slot.routeRef.RouteId.IsValid;
            }

            if (routes == null)
            {
                return false;
            }

            foreach (RouteEntry route in routes)
            {
                if (route == null)
                {
                    continue;
                }

                NavigationIntentId routeIntentId = route.ResolveIntentId();
                if (routeIntentId.IsValid && string.Equals(routeIntentId.Value, normalizedIntentId, StringComparison.OrdinalIgnoreCase))
                {
                    return route.routeRef != null && route.routeRef.RouteId.IsValid && route.transitionStyleRef != null;
                }
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
                if (TryMapToKnownKind(intentId, out _))
                {
                    FailFastConfig($"[FATAL][Config] GameNavigationCatalog extras nao pode usar intent reservado. asset='{name}', index={i}, intentId='{intentId}'.");
                }

                if (route.routeRef == null || !route.routeRef.RouteId.IsValid)
                {
                    FailFastConfig($"[FATAL][Config] GameNavigationCatalog extra com routeRef invalido. asset='{name}', index={i}, intentId='{intentId}'.");
                }

                if (route.transitionStyleRef == null)
                {
                    FailFastConfig($"[FATAL][Config] GameNavigationCatalog extra sem transitionStyleRef obrigatorio. asset='{name}', index={i}, intentId='{intentId}'.");
                }
            }
        }

        private static string GetIntentId(GameNavigationIntentKind kind)
        {
            if (!TryGetIntentId(kind, out NavigationIntentId intentId))
            {
                throw new InvalidOperationException($"[FATAL][Config] GameNavigationIntents sem intent core canonica em codigo. kind='{kind}'.");
            }

            return intentId.Value;
        }

        private static bool TryGetIntentId(GameNavigationIntentKind kind, out NavigationIntentId intentId)
        {
            intentId = GameNavigationIntents.GetCoreId(kind);
            return intentId.IsValid;
        }

        private static NavigationIntentId GetCompatibilityIntentId(GameNavigationIntentKind kind)
        {
            return GameNavigationCompatibility.GetCompatibilityId(kind);
        }

        private static bool TryMapToKnownKind(string intentId, out GameNavigationIntentKind kind)
        {
            if (GameNavigationIntents.TryMapToCoreKind(intentId, out kind))
            {
                return true;
            }

            return GameNavigationCompatibility.TryMapToCompatibilityKind(intentId, out kind);
        }

        private static bool IsCoreIntentKind(GameNavigationIntentKind kind)
        {
            return kind == GameNavigationIntentKind.Menu ||
                   kind == GameNavigationIntentKind.Gameplay ||
                   kind == GameNavigationIntentKind.GameOver ||
                   kind == GameNavigationIntentKind.Victory;
        }

        private static bool IsCompatibilityIntentKind(GameNavigationIntentKind kind)
        {
            return kind == GameNavigationIntentKind.Restart ||
                   kind == GameNavigationIntentKind.ExitToMenu;
        }

        private CoreIntentSlot GetCoreSlot(GameNavigationIntentKind kind)
        {
            switch (kind)
            {
                case GameNavigationIntentKind.Menu: return menuSlot;
                case GameNavigationIntentKind.Gameplay: return gameplaySlot;
                case GameNavigationIntentKind.GameOver: return gameOverSlot;
                case GameNavigationIntentKind.Victory: return victorySlot;
                case GameNavigationIntentKind.Restart: return restartSlot;
                case GameNavigationIntentKind.ExitToMenu: return exitToMenuSlot;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private void FailFastCoreSlot(GameNavigationIntentKind kind, string detail)
        {
            string message = $"[FATAL][Config] GameNavigationCatalog invalido para intent core. asset='{name}', kind='{kind}', intentId='{GetIntentId(kind)}', detail='{detail}'";
            FailFastConfig(message);
        }

        private void FailFastCompatibilitySlot(GameNavigationIntentKind kind, string detail)
        {
            string message = $"[FATAL][Config] GameNavigationCatalog invalido para intent de compatibilidade. asset='{name}', kind='{kind}', intentId='{GetCompatibilityIntentId(kind)}', detail='{detail}'";
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

