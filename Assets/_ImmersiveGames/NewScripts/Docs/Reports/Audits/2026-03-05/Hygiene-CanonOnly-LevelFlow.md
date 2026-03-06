# Hygiene Canon-Only LevelFlow (2026-03-05)

## Escopo aplicado
- `Assets/_ImmersiveGames/NewScripts/**` apenas.
- Sem alteracao em `Scripts/` legado.

## Arquivos alterados
- `Modules/Navigation/Bindings/MenuPlayButtonBinder.cs`
- `Modules/LevelFlow/Runtime/ILevelFlowRuntimeService.cs`
- `Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs`
- `Modules/Navigation/IGameNavigationService.cs`
- `Modules/Navigation/GameNavigationService.cs`
- `Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
- `Docs/Reports/Audits/2026-03-05/Hygiene-CanonOnly-LevelFlow.md` (este report)

## Arquivos movidos
- Nenhum.

## APIs bloqueadas (legacy/quarentena logica)
- `ILevelFlowRuntimeService.StartGameplayAsync(string levelId, ...)`
  - Runtime: `HardFailFastH1` em `LevelFlowRuntimeService`.
- `IGameNavigationService.StartGameplayAsync(LevelId, ...)`
  - Runtime: `HardFailFastH1` em `GameNavigationService`.
- `IGameNavigationService.RequestGameplayAsync(...)`
  - Runtime: `HardFailFastH1` em `GameNavigationService`.
- `IGameNavigationService.NavigateAsync(string routeId, ...)`
  - Runtime: `HardFailFastH1` em `GameNavigationService`.

## Mudancas canônicas principais
- Entrada de Menu/QA para gameplay padronizada em `StartGameplayDefaultAsync(...)` (route-only, sem `levelId`).
- `GameNavigationService.StartGameplayRouteAsync(...)` sem exigencia de level selecionado.
- Validacao canônica obrigatoria em navigation:
  - resolve `routeAsset` via `SceneRouteCatalogAsset`
  - se `RouteKind=Gameplay`, exige `LevelCollection` nao nula e nao vazia
  - falhas => `HardFailFastH1` com `[FATAL][H1][Navigation]`
- Removido registro do bridge legado de content snapshot no pipeline:
  - `RegisterRestartSnapshotContentSwapBridge()` nao e mais chamado.
- Composicao de navigation/level runtime sem dependencia direta de `LevelCatalogAsset` no trilho de start canônico.

## Grep remanescente em NewScripts

### `LevelCatalogAsset`
```text
Infrastructure\Config\NewScriptsBootstrapConfigAsset.cs:21:        [SerializeField] private LevelCatalogAsset levelCatalog;
Infrastructure\Config\NewScriptsBootstrapConfigAsset.cs:30:        public LevelCatalogAsset LevelCatalog => levelCatalog;
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:18:        fileName = "LevelCatalogAsset",
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:19:        menuName = "ImmersiveGames/NewScripts/Modules/LevelFlow/Catalogs/LevelCatalogAsset",
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:21:    public sealed class LevelCatalogAsset : ScriptableObject, ILevelFlowService, ILevelContentResolver, ILevelMacroRouteCatalog
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:242:                DebugUtility.LogError(typeof(LevelCatalogAsset),
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:243:                    $"[FATAL][Config] LevelCatalogAsset inválido durante OnValidate. detail='{ex.Message}'."
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:327:                DebugUtility.LogVerbose<LevelCatalogAsset>(
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:342:                    DebugUtility.LogVerbose<LevelCatalogAsset>(
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:403:            DebugUtility.LogVerbose<LevelCatalogAsset>(
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:443:            DebugUtility.Log(typeof(LevelCatalogAsset),
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:444:                $"[OBS][Config] LevelCatalogAsset OnValidate migrou routeRef a partir de routeId legado. migrated={migratedCount}, asset='{name}'.",
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:472:                DebugUtility.Log(typeof(LevelCatalogAsset),
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:517:            DebugUtility.Log(typeof(LevelCatalogAsset),
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:518:                $"[OBS][Compat] LevelCatalogAsset OnValidate contentId default aplicado. migrated={migratedCount}, default='{LevelFlowContentDefaults.DefaultContentId}', asset='{name}'.",
Modules\LevelFlow\Bindings\LevelCatalogAsset.cs:550:            DebugUtility.LogError(typeof(LevelCatalogAsset), message);
Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs:77:                LevelCatalog = LoadAssetAtPath<LevelCatalogAsset>(LevelCatalogPath, context),
Modules\SceneFlow\Editor\Validation\SceneFlowConfigValidator.cs:726:            public LevelCatalogAsset LevelCatalog;
```

### `contentId`
```text
Modules/LevelFlow\Runtime\LevelSwapLocalAppliedEvent.cs:35:        [System.Obsolete("Legacy compatibility only. Canonical flow does not use contentId.")]
Modules/LevelFlow\Runtime\LevelSelectedEvent.cs:29:            string contentId,
Modules/LevelFlow\Runtime\LevelSelectedEvent.cs:55:        [System.Obsolete("Legacy compatibility only. Canonical flow does not use contentId.")]
Modules/LevelFlow\Runtime\LevelFlowContentDefaults.cs:7:        public static string Normalize(string contentId)
Modules/LevelFlow\Runtime\LevelFlowContentDefaults.cs:9:            return string.IsNullOrWhiteSpace(contentId) ? DefaultContentId : contentId.Trim();
Modules/LevelFlow\Runtime\LevelDefinition.cs:34:        public string contentId = LevelFlowContentDefaults.DefaultContentId;
Modules/LevelFlow\Runtime\LevelDefinition.cs:58:            => LevelFlowContentDefaults.Normalize(contentId);
Modules/LevelFlow\Runtime\LevelContextSignature.cs:26:            string contentId = null)
Modules/LevelFlow\Runtime\LevelContextSignature.cs:29:            string normalizedContentId = string.IsNullOrWhiteSpace(contentId) ? string.Empty : contentId.Trim();
Modules/Navigation\RestartSnapshotContentSwapBridge.cs:36:            // Comentario: trilho canonico nao sincroniza snapshot por contentId.
Modules/LevelFlow\Runtime\ILevelContentResolver.cs:8:        bool TryResolveContentId(LevelId levelId, out string contentId);
Modules/LevelFlow\Runtime\GameplayStartSnapshot.cs:45:        [System.Obsolete("Legacy compatibility only. Canonical flow does not use contentId.")]
Modules/LevelFlow\Runtime\GameplayStartSnapshot.cs:51:        [System.Obsolete("Legacy compatibility only. Canonical flow does not use contentId.")]
Modules/LevelFlow\Runtime\ILevelFlowService.cs:6:    /// Serviço responsável por resolver LevelId -> MacroRouteId(SceneRouteId) + contentId + payload.
Modules/LevelFlow\Runtime\ILevelFlowService.cs:10:        bool TryResolve(LevelId levelId, out SceneRouteId macroRouteId, out string contentId, out SceneTransitionPayload payload);
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:25:            public LevelResolution(LevelDefinition definition, SceneRouteId macroRouteId, SceneTransitionPayload payload, string contentId)
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:30:                ContentId = contentId;
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:55:        public bool TryResolve(LevelId levelId, out SceneRouteId macroRouteId, out string contentId, out SceneTransitionPayload payload)
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:58:            contentId = string.Empty;
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:74:            contentId = LevelFlowContentDefaults.Normalize(resolution.ContentId);
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:77:            if (string.IsNullOrWhiteSpace(contentId))
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:112:        public bool TryResolveContentId(LevelId levelId, out string contentId)
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:114:            contentId = LevelFlowContentDefaults.DefaultContentId;
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:127:            contentId = LevelFlowContentDefaults.Normalize(resolution.ContentId);
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:404:                $"[OBS][SceneFlow] MacroRouteResolvedVia=LevelCatalog levelId='{levelId}' macroRouteId='{resolution.MacroRouteId}' contentId='{resolution.ContentId}'.",
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:501:                string normalizedContentId = LevelFlowContentDefaults.Normalize(definition.contentId);
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:502:                if (string.Equals(definition.contentId, normalizedContentId, StringComparison.Ordinal))
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:507:                definition.contentId = normalizedContentId;
Modules/LevelFlow\Bindings\LevelCatalogAsset.cs:518:                $"[OBS][Compat] LevelCatalogAsset OnValidate contentId default aplicado. migrated={migratedCount}, default='{LevelFlowContentDefaults.DefaultContentId}', asset='{name}'.",
```

### `StartGameplayAsync(`
```text
Modules\LevelFlow\Runtime\LevelFlowRuntimeService.cs:28:        public Task StartGameplayAsync(string levelId, string reason = null, CancellationToken ct = default)
Modules\LevelFlow\Runtime\LevelFlowRuntimeService.cs:32:                $"[FATAL][H1][LevelFlow] Legacy StartGameplayAsync(levelId) is disabled. levelId='{levelId}' reason='{normalizedReason}'. Use StartGameplayDefaultAsync.");
Modules\LevelFlow\Runtime\ILevelFlowRuntimeService.cs:9:        Task StartGameplayAsync(string levelId, string reason = null, CancellationToken ct = default);
Modules\Navigation\IGameNavigationService.cs:32:        Task StartGameplayAsync(LevelId levelId, string reason = null);
Modules\Navigation\GameNavigationService.cs:128:        public Task StartGameplayAsync(LevelId levelId, string reason = null)
Modules\Navigation\GameNavigationService.cs:130:            WarnLegacyApiUsedOnce("StartGameplayAsync(LevelId)", "ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct)");
Modules\Navigation\GameNavigationService.cs:132:                $"[FATAL][H1][Navigation] Legacy API blocked: StartGameplayAsync(LevelId). levelId='{levelId}' reason='{reason ?? "<null>"}'.");
```

## Checklist final
- [x] `Menu -> Gameplay` nao chama `StartGameplayAsync(levelId, ...)`.
- [x] Nao ha callsite `.StartGameplayAsync(` em `Modules/**` e `Infrastructure/**`.
- [x] Start canônico usa route-only (`to-gameplay`) e delega selecao default para `LevelPrepare`.
- [x] `GameNavigationService.StartGameplayRouteAsync(...)` valida `Gameplay + LevelCollection` e falha com `HardFailFastH1` se invalido.
- [x] Nao ha branch canônico de StartGameplay consultando `LevelCatalogAsset`.

## Observacao de validacao
- Este hardening foi validado por inspeção estatica/grep no escopo `NewScripts`.
- Nao foi executado playmode/build Unity neste passo.
