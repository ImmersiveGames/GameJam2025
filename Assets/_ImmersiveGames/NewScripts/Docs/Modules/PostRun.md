# PostRun

## Status documental

- Canonico ativo para o fluxo local de pos-run.
- O root fisico atual e `Experience/PostRun`.
- `PostStage` pode permanecer apenas como seam tecnico interno quando necessario.

## Estrutura atual

- `Handoff`: seam explicito com `GameLoop` e transferencia do outcome para o rail local.
- `Ownership`: gate de entrada/saida do `PostRun` local e transferencia para `RunDecision`.
- `Result`: snapshot formal do resultado final.
- `Presentation`: presenter local de `PostRun` e overlay final de `RunDecision`.

## Objetivo

- Interpor o rail local entre `RunOutcome` e a entrada formal em `RunDecision`.
- Resolver presenter opcional da cena/conteudo atual.
- Controlar a conclusao do `PostRun` local sem assumir o estado terminal da run.

## Ownership atual

- `IPostRunHandoffService`: fronteira principal com `GameLoop`.
- `PostRunHandoffService`: adaptador do fim de run para `PostRun`.
- `PostRunOwnershipService`: gate e ownership do `PostRun` e da entrada em `RunDecision`.
- `PostRunResultService`: snapshot formal do resultado.
- `PostRunOverlayController`: owner exclusivo da apresentacao final de `RunDecision`.
- `ILevelPostRunHookPresenterRegistry` e `ILevelPostRunHookPresenterScopeResolver`: adopcao do presenter scene-local.
- `LevelPostRunHookPresenter`: visual local canonico do `PostRun`.

## Fluxo validado

1. `GameRunEndedEvent`
2. `RunOutcomeAccepted`
3. `PostRunStarted`
4. `LevelPostRunHookPresenterRegistered` / `Adopted` / `Bound`
5. GUI local de `PostRun`
6. `LevelPostRunHookPresenterCompleted`
7. `LevelPostRunHookPresenterDismissed`
8. `LevelPostRunHookCompleted`
9. `PostRunCompleted`
10. `TransferOwnershipFromPostRun`
11. `RunDecisionEntered`
12. abertura do overlay final

## Limites conhecidos

- O modulo ainda depende do `GameLoop` para iniciar o fluxo terminal via outcome.
- `Restart` e `ExitToMenu` continuam saindo por `LevelLifecycle` e `Navigation`.
- O overlay e reativo; nao deve ser lido como owner do `PostRun`.
- `PostGame`, `GameOver` e `PostPlay` sao historicos fora do canon ativo.

## Hooks / contratos publicos

- `PostRunContext`
- `IPostRunCoordinator`
- `IPostRunControlService`
- `ILevelPostRunHookPresenter`
- `ILevelPostRunHookPresenterRegistry`
- `ILevelPostRunHookPresenterScopeResolver`
- `PostRunStartedEvent`
- `LevelPostRunHookStartedEvent`
- `LevelPostRunHookCompletedEvent`
- `PostRunCompletedEvent`
- `RunDecisionEnteredEvent`

## Politica padrao

- Default = sem presenter local.
- Ausencia de presenter implica fallback observavel explicito no rail local.
- Se a cena/conteudo registrar um presenter valido, o rail real roda com GUI minima.
- `Complete` e `Skip` sao one-shot.
- Ambiguidade de presenter falha fast.

## Leitura cruzada

- `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Guides/Event-Hooks-Reference.md`
