# Production-How-To-Use-Core-Modules

## Objetivo

- Indicar o owner certo dentro da leitura `Base 1.0`, sem depender de linguagem historica.
- Evitar que `LevelLifecycle`, `LevelFlow`, `ContentSwap` ou `PostRun` sejam lidos como baseline vivo.

## Canon de leitura

- `Docs/ADRs/ADR-0057-Base-1.0-Leitura-Sistemica-Composta-entre-Baseline-4.0-Session-Integration-e-Camadas-Semanticas.md`
- `Docs/ADRs/ADR-0056-Baseline-40-como-executor-tecnico-fino-e-fronteira-com-GameplaySessionFlow.md`
- `Docs/ADRs/ADR-0055-Seam-de-Integracao-Semantica-de-Sessao-como-area-propria-da-arquitetura.md`
- `Docs/ADRs/ADR-0052-Session-Transition-Composicao-de-Eixos-Acima-do-Baseline.md`
- `Docs/ADRs/ADR-0045-Gameplay-Runtime-Composition-Centro-Semantico-do-Gameplay.md`
- `Docs/ADRs/ADR-0046-GameplaySessionFlow-como-primeiro-bloco-interno-do-Gameplay-Runtime-Composition.md`
- `Docs/ADRs/ADR-0047-Gameplay-Phase-Construction-Pipeline-dentro-do-GameplaySessionFlow.md`
- `Docs/ADRs/ADR-0049-Fluxo-Canonico-de-Fim-de-Run-e-PostRun.md`
- `Docs/ADRs/ADR-0050-IntroStage-Canonical-Content-Presenter-Hook.md`

## Base 1.0 operacional

| Camada | ADR | Papel |
| --- | --- | --- |
| Leitura composta do sistema | `ADR-0057` | referencia operacional primaria |
| Baseline tecnico fino | `ADR-0056` | executor tecnico fino |
| Session Integration | `ADR-0055` | seam explicito acima do baseline |
| Antecedentes semanticos e de composicao | `ADR-0045`, `ADR-0046`, `ADR-0047`, `ADR-0052` | leitura de gameplay composition e session transition |

## Escolha rapida

| Use para | Leia / chame |
| --- | --- |
| Composicao semantica do gameplay | `GameplayPhaseFlowService`, `GameplaySessionContextService`, `PhaseDefinitionInstaller`, `PhaseNextPhaseService` |
| Transicao macro de cena | `SceneFlow`, `SceneTransitionService`, `SceneRouteDefinitionAsset` |
| Navegacao macro | `GameNavigationService`, `GameNavigationCatalogAsset` |
| IntroStage scene-local | `IntroStagePresenterHost`, `IntroStagePresenterScopeResolver`, `IntroStageControlService`, `IntroStageCoordinator` |
| RunResultStage / RunDecision | `RunResultStageOwnershipService`, `RunDecisionOwnershipService`, `RunResultStagePresenterHost`, `RunDecisionStagePresenterHost` |
| Reset e continuidade | `RestartContextService`, `GameplayStartSnapshot`, `WorldResetService`, `SceneFlowWorldResetDriver` |

## Regras de uso

- A leitura operacional do sistema sempre passa por `Base 1.0` antes de qualquer escolha local de owner.
- Nao use `LevelLifecycle` como owner semantico.
- Nao use `LevelFlow` como ponto principal de composicao.
- Nao use `PostRun` como nome central do fim de run.
- Nao use `ContentSwap` como linguagem operacional atual.
- IntroStage e scene-local; resolver presenter acontece depois de `SceneTransitionCompletedEvent`.
- `RunResultStage` e `RunDecision` continuam em rails separados.

## Historico

- Se voce encontrar nomes historicos em runtime ou evidencias antigas, leia-os como compatibilidade ou archive.
- Os guias ativos sempre devem ser interpretados pela cadeia `Gameplay Runtime Composition -> GameplaySessionFlow -> PhaseDefinition -> IntroStage / RunResultStage / RunDecision`.
