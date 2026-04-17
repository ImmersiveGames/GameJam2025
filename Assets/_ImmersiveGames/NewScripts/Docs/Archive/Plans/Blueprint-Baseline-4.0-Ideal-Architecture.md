# Blueprint-Baseline-4.0-Ideal-Architecture

## Status

- Blueprint historico/superseded.
- Este arquivo registra uma visao anterior do Baseline 4.0 e nao e a fonte atual de ownership.

## Canon atual

- `Gameplay Runtime Composition` e o centro semantico do gameplay.
- `GameplaySessionFlow` e o primeiro bloco interno desse centro.
- `PhaseDefinition` e a fonte autoral da phase.
- `IntroStage` e scene-local depois de `SceneTransitionCompletedEvent`.
- `RunResultStage` e `RunDecision` sao o rail terminal atual.

## Leitura historica

- As secoes antigas sobre `PostRun`, `LevelFlow` e compat historica servem apenas como rastreio.
- `ADR-0044` consolidou uma arquitetura anterior; o baseline vivo deve ser lido a partir dos ADRs 0045, 0046, 0047, 0049 e 0050.

## Reuso historico

| Componente antigo | Papel historico | Leitura atual |
| --- | --- | --- |
| `PostRunOwnershipService` | gate e ownership do rail historico | ler atraves de `RunResultStage` / `RunDecision` |
| `PostRunResultService` | snapshot do resultado | projeao historica para o rail terminal |
| `PostRunOverlayController` | contexto visual local | apresentacao downstream de `RunDecision` |
| `LevelFlowRuntimeService` | runtime de lifecycle historico | seam legado, nao owner final |
| `LevelPostRunHookService` | reacao opcional historica | compat/residual apenas |

## Regra

- Nunca use este blueprint como justificativa para manter `LevelLifecycle`, `LevelFlow` ou `PostRun` como forma final do baseline.
