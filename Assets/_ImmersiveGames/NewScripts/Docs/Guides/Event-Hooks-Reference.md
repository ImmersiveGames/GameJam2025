# Event-Hooks-Reference

## Escopo

- Referencia de hooks publicos e observaveis do baseline.
- Nao use como lista historica de compat residual.

## Canon de leitura

- O contrato canonico vigente do gameplay esta em `Docs/ADRs/ADR-0045-Gameplay-Runtime-Composition-Centro-Semantico-do-Gameplay.md`, `Docs/ADRs/ADR-0046-GameplaySessionFlow-como-primeiro-bloco-interno-do-Gameplay-Runtime-Composition.md`, `Docs/ADRs/ADR-0047-Gameplay-Phase-Construction-Pipeline-dentro-do-GameplaySessionFlow.md`, `Docs/ADRs/ADR-0049-Fluxo-Canonico-de-Fim-de-Run-e-PostRun.md` e `Docs/ADRs/ADR-0050-IntroStage-Canonical-Content-Presenter-Hook.md`.
- O fim de run canonico e `RunEndIntent -> RunResultStage` opcional -> `RunDecision -> Overlay`.
- `IntroStage` e scene-local e so pode ser resolvida depois de `SceneTransitionCompletedEvent`.

## Hooks de gameplay / phase

| Objetivo | Hook / sinal | Camada |
| --- | --- | --- |
| saber que o gameplay iniciou | `GameplaySessionStartedEvent` | `GameplaySessionFlow` |
| saber que a phase foi selecionada | `PhaseDefinitionSelectedEvent` | `GameplaySessionFlow` |
| saber que a composicao foi aplicada | `PhaseContentAppliedEvent` | `GameplaySessionFlow` |
| saber que a derivacao foi concluida | `PhaseDerivationCompletedEvent` | `GameplaySessionFlow` |
| saber que a transicao macro concluiu | `SceneTransitionCompletedEvent` | `SceneFlow` |
| saber que a IntroStage foi liberada | `IntroStageReleasedOnSceneTransitionCompleted` | `GameLoop` / host scene-local |
| saber que a IntroStage foi pulada | `IntroStageSkipped` | `GameLoop` / host scene-local |
| saber que o gameplay entrou em Playing | `GameplaySimulationUnblocked` | `GameLoop` |

## Hooks de fim de run

| Objetivo | Hook / sinal | Camada |
| --- | --- | --- |
| saber que a run terminou | `GameRunEndedEvent` | `GameLoop` |
| saber que `RunResultStage` foi despachado | `RunResultStageDispatchRequested` | `Experience/PostRun` |
| saber que o presenter do resultado foi adotado | `RunResultStagePresenterAdopted` | `Experience/PostRun` |
| saber que `RunResultStage` entrou | `RunResultStageEntered` | `Experience/PostRun` |
| saber que `RunDecision` entrou | `RunDecisionEnteredEvent` | `Experience/PostRun` |
| saber que o overlay final foi aberto | `RunDecisionOverlayOpenedEvent` | `Experience/PostRun` |

## Hooks historicos / alias

- `LevelSelectedEvent`
- `LevelSwapLocalAppliedEvent`
- `LevelEnteredEvent`
- `LevelIntroCompletedEvent`
- `PostRunEnteredEvent`
- `PostRunCompletedEvent`
- `LevelPostRunHookStartedEvent`
- `LevelPostRunHookCompletedEvent`

## Regras de leitura

- O baseline atual nao deve usar hooks historicos como fonte primária de ownership.
- IntroStage e scene-local, depois de `SceneTransitionCompletedEvent`.
- RunResultStage e RunDecision sao rails distintos.
- Se um hook historico ainda existir no runtime, ele deve ser lido como compatibilidade ou observabilidade, nao como owner final.
