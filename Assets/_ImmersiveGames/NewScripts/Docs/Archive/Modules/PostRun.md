# PostRun

## Status documental

- Referencia historica e de compatibilidade.
- O centro semantico atual nao e `PostRun`; e a separacao `RunResultStage` / `RunDecision`.
- O root fisico atual continua sendo `Experience/PostRun`, mantido por compatibilidade historica.
- `PostRun` nao e o owner semantico; e apenas a fachada historica do rail terminal.

## Leitura canonica atual

- `RunEndIntent`: intencao de encerrar a run depois do resultado consolidado.
- `RunResultStage`: saida local da phase.
- `RunDecision`: continuidade macro/gameplay.
- `Overlay`: projecao visual de `RunDecision`.
- `PostRun`: alias historico do rail antigo; nao e mais o conceito central.

## Estrutura historica

- `Handoff`: seam historico com `GameLoop`.
- `Ownership`: gate historico do rail legado.
- `Result`: snapshot formal do resultado final.
- `Presentation`: presenter historico de `PostRun` e overlay final de `RunDecision`.

## Objetivo historico

- Registrar o modelo legado para compatibilidade.
- Manter o mapeamento historico entre outcome, hook local e overlay.
- Nao deve ser usado como base para reintroduzir `PostRun` como conceito central.
- `RunContinuation` aqui deve ser lido como parte do fluxo macro de continuidade, nao como conceito local da phase.

## Ownership historico

- `IPostRunHandoffService`: fronteira historica com `GameLoop`.
- `PostRunHandoffService`: adaptador historico do fim de run para o rail legado.
- `PostRunOwnershipService`: gate e ownership historicos do rail legado; hoje o contrato canonico deve ser lido como `RunResultStage` / `RunDecision`.
- `PostRunResultService`: snapshot formal do resultado.
- `PostRunOverlayController`: projecao visual de `RunDecision`.
- `ILevelPostRunHookPresenterRegistry` e `ILevelPostRunHookPresenterScopeResolver`: descoberta tecnica historica do presenter scene-local.
- `LevelPostRunHookPresenter`: visual historico de compatibilidade do rail legado.

## Fluxo historico validado

1. `GameRunEndedEvent`
2. `RunOutcomeAccepted`
3. `PostRunHandoffStarted`
4. bloqueio imediato da gameplay para o rail local historico de `PostRun`
5. `PostRunStarted`
6. `LevelPostRunHookPresenterRegistered` / `Adopted` / `Bound`
7. GUI local historica de `PostRun`
8. `LevelPostRunHookPresenterCompleted`
9. `LevelPostRunHookPresenterDismissed`
10. `LevelPostRunHookCompleted`
11. `PostRunCompleted`
12. `RunDecisionEntered`
13. abertura do overlay final
14. `Restart` / `ExitToMenu`

## Limites conhecidos

- O modulo ainda depende do `GameLoop` para iniciar o fluxo terminal via outcome.
- `Restart` e `ExitToMenu` continuam saindo por `Navigation` e pelo rail canonico de gameplay.
- O overlay e reativo; nao deve ser lido como owner do fluxo final.
- O overlay nao e gatilho de entrada em `RunDecision`; ele e consequencia dessa entrada.
- `PostGame`, `GameOver` e `PostPlay` sao historicos fora do canon ativo.
- `PostRun` nao deve ser usado para reintroduzir um owner semantico novo; ele e apenas a camada historica de compatibilidade.

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
- O overlay final so e permitido apos `RunDecisionEntered`.

## Leitura cruzada

- `Docs/ADRs/ADR-0049-Fluxo-Canonico-de-Fim-de-Run-e-PostRun.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Archive/Modules/LevelFlow.md`
- `Docs/Guides/Event-Hooks-Reference.md`
