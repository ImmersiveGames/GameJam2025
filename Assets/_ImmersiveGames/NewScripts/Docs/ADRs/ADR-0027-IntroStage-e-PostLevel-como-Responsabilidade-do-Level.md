# ADR-0027 — IntroStage e PostLevel como Responsabilidade do Level

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-03-05
- Tipo: Implementação
- Escopo: NewScripts/Modules (LevelFlow, GameLoop IntroStage, PostLevel)

## Resumo

A orquestração de IntroStage e ações de pós-level está no domínio de LevelFlow.

## Decisão

- IntroStage é disparado pelo `LevelStageOrchestrator`:
  - após `SceneTransitionCompleted` em gameplay;
  - após `LevelSwapLocalAppliedEvent`.
- Dedupe por versão de seleção evita disparos repetidos de IntroStage.
- Pós-level é exposto por `IPostLevelActionsService` com ações:
  - RestartLevel
  - NextLevel
  - ExitToMenu

## Implementação atual (fonte de verdade = código)

- `LevelStageOrchestrator` escuta `SceneTransitionCompletedEvent` e `LevelSwapLocalAppliedEvent`, monta `IntroStageContext` e chama `IIntroStageCoordinator.RunIntroStageAsync(...)`.
- `LevelStageOrchestrator` usa `_lastProcessedSelectionVersion` para evitar replay.
- `IPostLevelActionsService` define `RestartLevelAsync`, `NextLevelAsync`, `ExitToMenuAsync`.
- `PostLevelActionsService` implementa:
  - Restart via `GameResetRequestedEvent`;
  - NextLevel via `ILevelSwapLocalService.SwapLocalAsync(...)`;
  - Exit via `IGameNavigationService.ExitToMenuAsync(...)`.

## Critérios de aceite (DoD)

- [x] IntroStage orquestrado no domínio LevelFlow.
- [x] IntroStage possui dedupe por `SelectionVersion`.
- [x] PostLevel expõe Restart/NextLevel/ExitToMenu em serviço dedicado.
- [x] NextLevel usa swap local (não macro transition) por contrato de implementação.
- [ ] Hardening: testes end-to-end cobrindo loop completo Intro -> Playing -> PostLevel.

## Changelog

- 2026-03-05: status atualizado para **Implementado** e conteúdo alinhado ao código atual.
