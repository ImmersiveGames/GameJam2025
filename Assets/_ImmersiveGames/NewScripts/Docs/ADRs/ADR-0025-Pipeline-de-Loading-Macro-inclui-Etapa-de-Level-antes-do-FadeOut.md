# ADR-0025 — Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-04
- Tipo: Implementação
- Escopo: NewScripts/Runtime (SceneFlow, WorldLifecycle, LevelFlow, Loading)

## Resumo

Garantir que, para macro gameplay, o FadeOut só aconteça após:

1. Gate de reset macro/world lifecycle.
2. Preparação de level no domínio de level.

## Decisão

1. `SceneTransitionService` chama `AwaitCompletionGateAsync(...)` antes de `RunFadeOutIfNeeded(...)`.
2. O gate usado no DI é `MacroLevelPrepareCompletionGate(inner=WorldLifecycleResetCompletionGate)`.
3. `MacroLevelPrepareCompletionGate` chama `ILevelMacroPrepareService.PrepareAsync(...)` para profile gameplay.
4. `LevelMacroPrepareService` garante seleção/aplicação do level (com `ResetLevelAsync`) antes de liberar o pipeline.

## Implementação atual (fonte de verdade: código)

- `SceneTransitionService.TransitionAsync(...)`: ordem `ScenesReady -> AwaitCompletionGateAsync -> BeforeFadeOut -> FadeOut`.
- `GlobalCompositionRoot.SceneFlowWorldLifecycle.RegisterSceneFlowNative()`: registra gate composto com `MacroLevelPrepareCompletionGate` envolvendo `WorldLifecycleResetCompletionGate`.
- `MacroLevelPrepareCompletionGate.AwaitBeforeFadeOutAsync(...)`: executa inner gate, filtra profile gameplay e aciona `ILevelMacroPrepareService`.
- `LevelMacroPrepareService.PrepareAsync(...)`: resolve level alvo por snapshot/catalog, publica seleção quando necessário e executa `IWorldResetCommands.ResetLevelAsync(...)`.

## Critérios de aceite (DoD)

- [x] FadeOut ocorre após completion gate no pipeline.
- [x] Etapa `LevelPrepare` está implementada no gate macro (não apenas por efeito indireto).
- [x] `LevelPrepare` usa serviço de domínio (`ILevelMacroPrepareService`) e chama reset local (`ResetLevelAsync`).
- [x] DI global registra o gate composto e o serviço de prepare.

## Changelog

- 2026-03-04: ADR auditado contra o código; seção de decisão alinhada ao shape real (`MacroLevelPrepareCompletionGate` + `LevelMacroPrepareService`).
