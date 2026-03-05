# ADR-0025 — Pipeline de Loading Macro inclui Etapa de Level antes do FadeOut

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-05
- Tipo: Implementação
- Escopo: NewScripts/Runtime (SceneFlow, WorldLifecycle, LevelFlow)

## Resumo

No macro de gameplay, o pipeline só libera o FadeOut após concluir:
1) gate de reset do WorldLifecycle;
2) preparação de level no domínio LevelFlow.

## Decisão

- O completion gate usado por `SceneTransitionService` é composto:
  - `WorldLifecycleResetCompletionGate` (inner)
  - `MacroLevelPrepareCompletionGate` (wrapper)
- `MacroLevelPrepareCompletionGate` executa `ILevelMacroPrepareService.PrepareAsync(...)` antes de liberar FadeOut em gameplay.

## Implementação atual (fonte de verdade = código)

- `SceneTransitionService` chama `AwaitCompletionGateAsync(context)` antes de `RunFadeOutIfNeeded(...)`.
- `MacroLevelPrepareCompletionGate.AwaitBeforeFadeOutAsync(...)` chama o gate interno e depois roda LevelPrepare quando profile=Gameplay.
- `LevelMacroPrepareService.PrepareAsync(...)` seleciona level válido/default por macro e chama `IWorldResetCommands.ResetLevelAsync(...)`.
- `GlobalCompositionRoot.RegisterSceneFlowNative()` registra o gate composto com `MacroLevelPrepareCompletionGate(innerGate)`.
- `GlobalCompositionRoot` registra `ILevelMacroPrepareService` com `LevelMacroPrepareService`.

## Critérios de aceite (DoD)

- [x] FadeOut depende de completion gate.
- [x] Etapa de LevelPrepare existe e roda antes de FadeOut no gameplay.
- [x] Serviço de LevelPrepare integrado ao DI global.
- [ ] Hardening: timeout/retry específico para falhas de LevelPrepare.

## Changelog

- 2026-03-05: ADR atualizado para refletir a implementação concreta de gate composto + LevelPrepare no código.
