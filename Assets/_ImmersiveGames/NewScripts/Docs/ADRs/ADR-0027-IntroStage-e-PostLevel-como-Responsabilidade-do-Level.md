# ADR-0027 - IntroStage e PostLevel como Responsabilidade do Level

## Status

- Estado: **Aceito (Implementado)**
- Data (decisao): 2026-02-19
- Ultima atualizacao: 2026-03-05
- Tipo: Implementacao
- Escopo: NewScripts/Modules (LevelFlow, IntroStage, PostGame/PostLevel)

## Resumo

A responsabilidade de entrada e acoes pos-level esta no dominio de level:

- IntroStage disparada por orquestracao de level.
- Acoes de pos-level encapsuladas em servico dedicado (`Restart`, `NextLevel`, `ExitToMenu`).

## Decisao

1. `LevelStageOrchestrator` e dono do gatilho de IntroStage:
   - apos `SceneTransitionCompletedEvent` em gameplay;
   - apos `LevelSwapLocalAppliedEvent`.
2. `IPostLevelActionsService` encapsula acoes pos-level:
   - `RestartLevelAsync`
   - `NextLevelAsync`
   - `ExitToMenuAsync`
3. `PostGameOverlayController` invoca `IPostLevelActionsService`, mantendo UI desacoplada da regra de navegacao/swap.

## Implementacao atual (fonte de verdade: codigo)

### IntroStage no dominio Level

- `LevelStageOrchestrator` assina eventos de transicao/swap, aplica dedupe por `SelectionVersion` e inicia IntroStage via `IIntroStageCoordinator`.

### PostLevel actions

- `IPostLevelActionsService` define o contrato das tres acoes.
- `PostLevelActionsService` implementa:
  - restart via `ILevelFlowRuntimeService.RestartLastGameplayAsync(...)`;
  - next level via `ILevelSwapLocalService.SwapLocalAsync(...)` + `TryGetNextLevelInMacro(...)`;
  - exit via `IGameNavigationService.ExitToMenuAsync(...)`.
- `PostGameOverlayController` injeta `IPostLevelActionsService` e aciona Restart/Exit pela interface.

### DI global

- `GlobalCompositionRoot` registra `LevelStageOrchestrator` e `IPostLevelActionsService`.

## Criterios de aceite (DoD)

- [x] IntroStage e iniciada por orquestrador de level (nao por SceneFlow diretamente).
- [x] IntroStage apos swap local esta implementada.
- [x] Servico de acoes pos-level existe com Restart/NextLevel/ExitToMenu.
- [x] UI de pos-jogo usa o servico de dominio (nao chama navegacao/swap diretamente).
- [ ] Hardening: unificar nomenclatura observavel entre PostGame e PostLevel para reduzir ambiguidade semantica.

## Changelog

- 2026-03-05: revisado com base nas auditorias de 2026-03-04/2026-03-05 e no codigo atual.
- 2026-03-04: status atualizado para Implementado; decisao e implementacao alinhadas as classes/metodos atuais.
