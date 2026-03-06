# Module Audit Summary (LevelFlow excerpt)

## LevelFlow
**Canonical Rail:** `LevelFlowRuntimeService` inicia/reinicia gameplay default; `LevelMacroPrepareService` executa LevelPrepare na fase macro do SceneFlow; `LevelSwapLocalService` cobre swap local intra-macro; `LevelStageOrchestrator` coordena IntroStage por `SceneTransitionCompletedEvent` + `LevelSwapLocalAppliedEvent`.

**Atualizacao LF-1.1 (higiene sem mudanca comportamental):**
- Legacy/Compat isolado para `Modules/LevelFlow/Legacy/**`:
  - `Legacy/Bindings/LevelCatalogAsset.cs`
  - `Legacy/Runtime/ILevelFlowService.cs`
  - `Legacy/Runtime/ILevelMacroRouteCatalog.cs`
  - `Legacy/Runtime/ILevelContentResolver.cs`
- Pipeline canonico de DI/runtime permaneceu inalterado.
