# LevelFlow Module Audit

## A) Scope
- Entra: `Modules/LevelFlow/**` (`Runtime`, `Bindings`, `Config`, `Dev`).
- EventBus inputs: `SceneTransitionCompletedEvent`, `LevelSwapLocalAppliedEvent` (orchestrator), eventos de restart via comandos.
- Chamadas publicas: `ILevelFlowRuntimeService`, `ILevelSwapLocalService`, `ILevelMacroPrepareService`, `IPostLevelActionsService`, `IRestartContextService`.
- Unity callbacks: assets/config e dev context menu.

## B) Outputs
- Publica: `LevelSelectedEvent`, `LevelSwapLocalAppliedEvent`.
- Aciona reset level por `IWorldResetCommands.ResetLevelAsync` em prepare/swap local.
- Efeito em Gate/InputMode: indireto (gera contexto para IntroStage e reset de nivel).

## C) DI Registration
- `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`:
  - `ILevelSwapLocalService` -> `LevelSwapLocalService`
  - `ILevelFlowRuntimeService` -> `LevelFlowRuntimeService`
  - `IPostLevelActionsService` -> `PostLevelActionsService`
  - `ILevelMacroPrepareService` -> `LevelMacroPrepareService`
  - `LevelStageOrchestrator` (concrete)

## D) Canonical Runtime Rail
- Macro entry: `LevelFlowRuntimeService.StartGameplayDefaultAsync` -> `GameNavigationService.StartGameplayRouteAsync` -> `LevelMacroPrepareService` publica `LevelSelectedEvent` e aplica/reset nivel.
- Intra-macro: `LevelSwapLocalService.SwapLocalAsync` publica `LevelSelectedEvent` + `LevelSwapLocalAppliedEvent`; `LevelStageOrchestrator` inicia IntroStage.

## E) LEGACY/Compat
- Caminhos por `LevelId` em `ILevelSwapLocalService` e `IWorldResetCommands` estao marcados `[Obsolete]` e bloqueados por fail-fast.
- `LevelCatalogAsset` e interfaces antigas foram isolados em `Modules/LevelFlow/Legacy/**` como trilha de compat.

## F) Redundancy Candidates
- `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs` e `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs` publicam `LevelSelectedEvent`; risco medio de semantica divergente.
- `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs` dedupe por `selectionVersion` e `levelSignature`; risco medio junto a outros dedupes de dominio.
- `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs` contem trilha LEGACY (`MacroRouteResolvedViaLevelCatalogLegacy`); risco baixo/medio.

## G) Deletion/Merge Plan (DOC-ONLY)
- Formalizar contrato unico de `LevelSelectedEvent` por origem (`macro_prepare` vs `swap_local`).
- Extrair regra de dedupe de IntroStage para um componente dedicado reutilizavel.
- Planejar remocao de `Modules/LevelFlow/Legacy/**` em LF-1.2 apos validacao de build/log e assets editor/QA.

| FilePath | Type (Runtime/Editor/Shared) | Public Entry Points | EventBus IN | EventBus OUT | DI Provides | Notes (Canon/Legacy/Dead) |
|---|---|---|---|---|---|---|
| Modules/LevelFlow/Runtime/LevelFlowRuntimeService.cs | Runtime | `StartGameplayDefaultAsync`, `RestartLastGameplayAsync` | N/A | N/A | `ILevelFlowRuntimeService` | Canon |
| Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs | Runtime | `PrepareLevelForRouteAsync` (service API) | N/A | `LevelSelectedEvent` | `ILevelMacroPrepareService` | Canon publisher |
| Modules/LevelFlow/Runtime/LevelSwapLocalService.cs | Runtime | `SwapLocalAsync` | N/A | `LevelSelectedEvent`, `LevelSwapLocalAppliedEvent` | `ILevelSwapLocalService` | Canon publisher |
| Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs | Runtime | ctor/Dispose | `SceneTransitionCompletedEvent`, `LevelSwapLocalAppliedEvent` | IntroStage requests (service calls) | concrete orchestrator | Canon consumer + dedupe |
| Modules/LevelFlow/Runtime/PostLevelActionsService.cs | Runtime | `RestartLevelAsync`, `NextLevelAsync` | N/A | `GameResetRequestedEvent` (indireto via `GameCommands`) | `IPostLevelActionsService` | Canon |
| Modules/LevelFlow/Runtime/LevelSelectedEvent.cs | Shared | event contract | N/A | N/A | N/A | Canon |
| Modules/LevelFlow/Runtime/LevelSwapLocalAppliedEvent.cs | Shared | event contract | N/A | N/A | N/A | Canon |
| Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs | Shared | catalog API | N/A | N/A | N/A | Canon + Legacy paths |
