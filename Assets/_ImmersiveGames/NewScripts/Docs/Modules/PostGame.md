# PostGame

## Status documental

- Parcial / leitura junto do runtime atual.
- `PostStage` é o owner principal deste módulo, mas o fluxo ainda depende do handoff do `GameLoop` e do bridge de outcome.

## Objetivo

- Interpor o stage pós-outcome entre `GameRunEndedEvent` e a entrada formal em `PostGame`.
- Resolver presenter opcional da cena/conteúdo atual.
- Controlar `PostGameEnteredEvent` e `PostGameExitedEvent` sem assumir o estado terminal da run.

## Estado atual

- `Modules/PostGame` é o owner canônico do `PostStage`.
- O seam de entrada e `GameRunEndedEventBridge.OnGameRunEnded(...)`.
- O handoff final para `GameLoop.RequestRunEnd()` ocorre somente depois de `PostStageCompletedEvent`.
- `PostGameOverlay` não abre direto em `GameRunEndedEvent`; ele abre depois de `PostGameEnteredEvent`.

## Dependências e acoplamentos atuais

- `GameRunEndedEventBridge` é a entrada prática do fluxo.
- `GameLoop.RequestRunEnd()` é o handoff final para o loop terminal.
- `LevelFlow` fornece contexto da cena e presenter opcional quando existe.
- `PostGameOwnershipService` e `PostGameResultService` concentram gate e snapshot do pós-run.
- `PostGameOverlayController` é apresentação local, não owner do stage.

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

## Limites conhecidos

- O módulo ainda depende do `GameLoop` para fechar o fluxo terminal.
- `Restart` e `ExitToMenu` continuam saindo por `LevelFlow` e `Navigation`, não por um owner exclusivo daqui.
- O overlay é reativo; não deve ser lido como owner do pós-run.

## Hooks / contratos públicos

- `PostStageContext`
- `IPostStageCoordinator`
- `IPostStageControlService`
- `IPostStagePresenter`
- `IPostStagePresenterRegistry`
- `IPostStagePresenterScopeResolver`
- `PostStageStartRequestedEvent`
- `PostStageStartedEvent`
- `PostStageCompletedEvent`
- `PostGameEnteredEvent`
- `PostGameExitedEvent`

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
- `PostGame` pode emitir a intenção de restart, mas a semântica concreta do restart e resolvida downstream em `LevelFlow`.
- `PostGame` permanece owner do pós-run e da projeção do resultado, não do significado de `Restart` em si.
- PostGame emite as intenções de saída pós-run; a execução concreta segue para LevelFlow e Navigation.
- `PostPlay` é nomenclatura residual em docs antigos; o runtime atual usa `PostGame`.

## Leitura cruzada

- `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Guides/Event-Hooks-Reference.md`
