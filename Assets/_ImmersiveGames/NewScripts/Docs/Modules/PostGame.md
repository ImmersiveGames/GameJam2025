# PostGame

## Estado atual

- `Modules/PostGame` e o owner canonico do `PostStage`.
- O seam de entrada e `GameRunEndedEventBridge.OnGameRunEnded(...)`.
- O handoff final para `GameLoop.RequestRunEnd()` ocorre somente depois de `PostStageCompletedEvent`.
- `PostGameOverlay` nao abre direto em `GameRunEndedEvent`; ele abre depois de `PostGameEnteredEvent`.

## Fluxo validado

1. `GameRunEndedEvent`
2. `PostStageStartRequestedEvent`
3. `PostStageStartedEvent`
4. presenter opcional da cena/conteudo atual
5. `Complete` ou `Skip`
6. `PostStageCompletedEvent`
7. `PostStageRunEndHandoff`
8. `IGameLoopService.RequestRunEnd()`
9. `GameLoop -> RunEnded/PostGame`
10. `PostGameEnteredEvent`
11. abertura do overlay

## Politica padrao

- Default = sem PostStage.
- Ausencia de presenter implica `PostStageSkipped reason='PostStage/NoPresenter'`.
- Se a cena/conteudo registrar um presenter valido, o stage real roda com GUI minima.
- `Complete` e `Skip` sao one-shot.
- Ambiguidade de presenter falha fast.

## Contratos principais

- `PostStageContext`
- `IPostStageCoordinator`
- `IPostStageControlService`
- `IPostStagePresenter`
- `IPostStagePresenterRegistry`
- `IPostStagePresenterScopeResolver`
- `PostStageStartRequestedEvent`
- `PostStageStartedEvent`
- `PostStageCompletedEvent`

## Ownership

- `PostGameOwnershipService`: gate/input do pos-game e publicacao de `PostGameEnteredEvent` / `PostGameExitedEvent`.
- `PostGameResultService`: snapshot formal do resultado.
- `PostStageCoordinator`: coordenacao da fase de validacao pos-outcome.
- `PostStagePresenterRegistry`: adocao do presenter scene-local.
- `PostGameOverlayController`: contexto visual local que delega `Restart` e `ExitToMenu` ao owner de `LevelFlow`/`Navigation`.
- `PostGame` pode emitir a intenção de restart, mas a semântica concreta do restart é resolvida downstream em `LevelFlow`.
- `PostGame` permanece owner do pós-run e da projeção do resultado, nao do significado de `Restart` em si.

## Leitura cruzada

- `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Guides/Event-Hooks-Reference.md`
