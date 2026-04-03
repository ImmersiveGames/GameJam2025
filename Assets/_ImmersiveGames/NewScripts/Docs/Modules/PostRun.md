# PostRun

## Status documental

- Canonico ativo para o fluxo local de pos-run.
- O root fisico atual e `Experience/PostRun`.
- `PostStage` pode permanecer apenas como seam tecnico interno quando necessario.

## Estrutura atual

- `Handoff`: seam explicito com `GameLoop` e transferencia do outcome para o rail local.
- `Ownership`: gate de entrada/saida do `PostRun` local; a transferencia para `RunDecision` ocorre depois da conclusao do rail local.
- `Result`: snapshot formal do resultado final.
- `Presentation`: presenter local de `PostRun` e overlay final de `RunDecision`.

## Objetivo

- Interpor o rail local entre `RunOutcome` e a entrada formal em `RunDecision`.
- Resolver presenter opcional da cena/conteudo atual.
- Controlar a conclusao do `PostRun` local sem confundir esse rail com a abertura do overlay final.

## Ownership atual

- `IPostRunHandoffService`: fronteira principal com `GameLoop`.
- `PostRunHandoffService`: adaptador do fim de run para `PostRun`.
- `PostRunOwnershipService`: gate e ownership do `PostRun` local; `RunDecision` e a camada visual/decisao que vem depois.
- `PostRunResultService`: snapshot formal do resultado.
- `PostRunOverlayController`: owner exclusivo da apresentacao final de `RunDecision`.
- `ILevelPostRunHookPresenterRegistry` e `ILevelPostRunHookPresenterScopeResolver`: adopcao do presenter scene-local.
- `LevelPostRunHookPresenter`: visual local canonico do `PostRun`.

## Fluxo validado

1. `GameRunEndedEvent`
2. `RunOutcomeAccepted`
3. `PostRunHandoffStarted`
4. bloqueio imediato da gameplay para o rail local de `PostRun`
5. `PostRunStarted`
6. `LevelPostRunHookPresenterRegistered` / `Adopted` / `Bound`
7. GUI local de `PostRun`
8. `LevelPostRunHookPresenterCompleted`
9. `LevelPostRunHookPresenterDismissed`
10. `LevelPostRunHookCompleted`
11. `PostRunCompleted`
12. `RunDecisionEntered`
13. abertura do overlay final
14. `Restart` / `ExitToMenu`

## Limites conhecidos

- O modulo ainda depende do `GameLoop` para iniciar o fluxo terminal via outcome.
- `Restart` e `ExitToMenu` continuam saindo por `LevelLifecycle` e `Navigation`.
- O overlay e reativo; nao deve ser lido como owner do `PostRun`.
- O overlay nao e gatilho de entrada em `RunDecision`; ele e consequencia dessa entrada.
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
- A abertura do overlay final so e permitida apos `PostRunCompleted` e `RunDecisionEntered`.

## Leitura cruzada

- `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Guides/Event-Hooks-Reference.md`
