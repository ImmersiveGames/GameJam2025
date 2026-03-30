# PostRun

## Status documental

- Parcial / leitura junto do runtime atual.
- O root fisico atual e `Experience/PostRun`.
- O owner atual esta dividido em `Handoff`, `Ownership`, `Result` e `Presentation`.

## Estrutura atual

- `Handoff`: seam explicito com `GameLoop` e coordenacao do `PostStage`.
- `Ownership`: gate de entrada/saida do pos-run.
- `Result`: snapshot formal do resultado final.
- `Presentation`: overlay e presenter local.

## Objetivo

- Interpor o stage pos-outcome entre `GameRunEndedEvent` e a entrada formal em `PostRun`.
- Resolver presenter opcional da cena/conteudo atual.
- Controlar `PostRunEnteredEvent` e `PostRunExitedEvent` sem assumir o estado terminal da run.

## Ownership atual

- `IPostRunHandoffService`: fronteira principal com `GameLoop`.
- `PostRunHandoffService`: adaptador do fim de run para `PostRun`.
- `PostStageCoordinator`: coordenacao da fase de validacao pos-outcome.
- `PostRunOwnershipService`: gate e ownership do pos-run.
- `PostRunResultService`: snapshot formal do resultado.
- `PostRunOverlayController`: apresentacao local, nao owner do stage.
- `PostStagePresenterRegistry` e `PostStagePresenterScopeResolver`: adopcao do presenter scene-local.

## Fluxo validado

1. `GameRunEndedEvent`
2. `PostStageStartRequestedEvent`
3. `PostStageStartedEvent`
4. presenter opcional da cena/conteudo atual
5. `Complete` ou `Skip`
6. `PostStageCompletedEvent`
7. `PostStageRunEndHandoff`
8. `IPostRunHandoffService`
9. `GameLoop -> RunEnded/PostRun`
10. `PostRunEnteredEvent`
11. abertura do overlay

## Limites conhecidos

- O modulo ainda depende do `GameLoop` para fechar o fluxo terminal.
- `Restart` e `ExitToMenu` continuam saindo por `LevelLifecycle` e `Navigation`.
- O overlay e reativo; nao deve ser lido como owner do pos-run.
- `PostPlay` e nomenclatura residual em docs antigos; o runtime atual usa `PostRun`.

## Hooks / contratos publicos

- `PostStageContext`
- `IPostStageCoordinator`
- `IPostStageControlService`
- `IPostStagePresenter`
- `IPostStagePresenterRegistry`
- `IPostStagePresenterScopeResolver`
- `PostStageStartRequestedEvent`
- `PostStageStartedEvent`
- `PostStageCompletedEvent`
- `PostRunEnteredEvent`
- `PostRunExitedEvent`

## Politica padrao

- Default = sem `PostStage`.
- Ausencia de presenter implica `PostStageSkipped reason='PostStage/NoPresenter'`.
- Se a cena/conteudo registrar um presenter valido, o stage real roda com GUI minima.
- `Complete` e `Skip` sao one-shot.
- Ambiguidade de presenter falha fast.

## Leitura cruzada

- `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Guides/Event-Hooks-Reference.md`
