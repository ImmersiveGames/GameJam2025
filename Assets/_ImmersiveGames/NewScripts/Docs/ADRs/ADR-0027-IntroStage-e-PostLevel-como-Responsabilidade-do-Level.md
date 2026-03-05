# ADR-0027 — IntroStage e PostLevel como Responsabilidade do Level

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-04
- Tipo: Implementação
- Escopo: NewScripts/Modules (LevelFlow, IntroStage, PostGame/PostLevel)

## Resumo

A responsabilidade de entrada e ações pós-level está no domínio de level:

- IntroStage disparada por orquestração de level.
- Ações de pós-level encapsuladas em serviço dedicado (`Restart`, `NextLevel`, `ExitToMenu`).

## Decisão

1. `LevelStageOrchestrator` é dono do gatilho de IntroStage:
   - após `SceneTransitionCompletedEvent` em gameplay;
   - após `LevelSwapLocalAppliedEvent`.
2. `IPostLevelActionsService` encapsula ações pós-level:
   - `RestartLevelAsync`
   - `NextLevelAsync`
   - `ExitToMenuAsync`
3. `PostGameOverlayController` invoca `IPostLevelActionsService`, mantendo UI desacoplada da regra de navegação/swap.

## Implementação atual (fonte de verdade: código)

### IntroStage no domínio Level

- `LevelStageOrchestrator` assina eventos de transição/swap, aplica dedupe por `SelectionVersion` e inicia IntroStage via `IIntroStageCoordinator`.

### PostLevel actions

- `IPostLevelActionsService` define o contrato das três ações.
- `PostLevelActionsService` implementa:
  - restart via `ILevelFlowRuntimeService.RestartLastGameplayAsync(...)`;
  - next level via `ILevelSwapLocalService.SwapLocalAsync(...)` + `TryGetNextLevelInMacro(...)`;
  - exit via `IGameNavigationService.ExitToMenuAsync(...)`.
- `PostGameOverlayController` injeta `IPostLevelActionsService` e aciona Restart/Exit pela interface.

### DI global

- `GlobalCompositionRoot` registra `LevelStageOrchestrator` e `IPostLevelActionsService`.

## Critérios de aceite (DoD)

- [x] IntroStage é iniciada por orquestrador de level (não por SceneFlow diretamente).
- [x] IntroStage após swap local está implementada.
- [x] Serviço de ações pós-level existe com Restart/NextLevel/ExitToMenu.
- [x] UI de pós-jogo usa o serviço de domínio (não chama navegação/swap diretamente).
- [ ] Hardening: unificar nomenclatura observável entre “PostGame” e “PostLevel” para reduzir ambiguidade semântica.

## Changelog

- 2026-03-04: status atualizado para Implementado; decisão e implementação alinhadas às classes/métodos atuais.
