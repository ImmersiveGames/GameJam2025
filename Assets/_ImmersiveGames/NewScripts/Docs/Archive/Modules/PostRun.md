# PostRun

## Status documental

- Baseline ativo com nomenclatura historica ainda presente no root fisico.
- O canon vivo do fim de run e `RunEndIntent -> RunResultStage` opcional -> `RunDecision -> Overlay`.
- O root fisico atual e `Experience/PostRun`, mas o nome e mantido apenas por compatibilidade historica.
- `PostRun` nao e o owner semantico; ele e apenas a fachada historica do rail terminal.

## Leitura canonica atual

- `RunEndIntent`: intencao de encerrar a run depois do resultado consolidado.
- `RunResultStage`: estagio local opcional do fim da run, simetrico ao `IntroStage`.
- `RunDecision`: etapa distinta de decisao downstream.
- `Overlay`: projecao visual de `RunDecision`.
- `PostRun`: alias historico do rail antigo; nao e mais o conceito central.

## Estrutura historica

- `Handoff`: seam explicito historico com `GameLoop` e transferencia do outcome para o rail local.
- `Ownership`: gate historico de entrada/saida do rail legado; o modelo atual nao usa `PostRun` como conceito central.
- `Result`: snapshot formal do resultado final.
- `Presentation`: presenter historico de `PostRun` e overlay final de `RunDecision`.

## Objetivo historico

- Registrar o modelo legado para compatibilidade.
- Manter o mapeamento historico entre outcome, hook local e overlay.
- Nao deve ser usado como base para reintroduzir `PostRun` como conceito central.

## Ownership historico

- `IPostRunHandoffService`: fronteira historica com `GameLoop`.
- `PostRunHandoffService`: adaptador historico do fim de run para o rail legado.
- `PostRunOwnershipService`: gate e ownership historicos do rail legado; hoje o contrato canonico deve ser lido como `RunResultStage`/`RunDecision`.
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
- `Restart` e `ExitToMenu` continuam saindo por `Navigation` e pelo rail canônico de gameplay.
- O overlay e reativo; nao deve ser lido como owner do fluxo final.
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
- O overlay final so e permitido apos `RunDecisionEntered`.

## Leitura cruzada

- `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Archive/Modules/LevelFlow.md`
- `Docs/Guides/Event-Hooks-Reference.md`
