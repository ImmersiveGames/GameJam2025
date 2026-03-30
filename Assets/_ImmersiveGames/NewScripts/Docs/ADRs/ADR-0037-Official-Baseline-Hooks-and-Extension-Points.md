# ADR-0037: Official Baseline Hooks and Extension Points

## Status
- Aceito
- Data: 2026-03-26

## Evidences canonicas
- Baseline 3.5
- Hooks/extensibility audit concluida para `NewScripts`
- `Docs/Guides/Event-Hooks-Reference.md`
- `Docs/Canon/Canon-Index.md`
- `Docs/Modules/SceneFlow.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/GameLoop.md`
- `Docs/Modules/WorldReset.md`
- `Docs/ADRs/ADR-0030-Fronteiras-Canonicas-do-Stack-SceneFlow-Navigation-LevelFlow.md`
- `Docs/ADRs/ADR-0031-Pipeline-Canonico-da-Transicao-Macro.md`
- `Docs/ADRs/ADR-0032-Semantica-Canonica-de-Route-Level-Reset-e-Dedupe.md`
- `Docs/ADRs/ADR-0033-Resiliencia-Canonica-de-Fade-e-Loading-no-Transito-Macro.md`
- `Docs/ADRs/ADR-0034-Actor-Presentation-Domain-Intent-and-Boundaries.md`
- `Docs/ADRs/ADR-0035-Ownership-Canonicos-dos-Clusters-de-Modulos-em-NewScripts.md`
- `Docs/ADRs/ADR-0036-Extensibilidade-Baseline-3.5.md`

## Context
`NewScripts` ja possui varios eventos e hooks relevantes, mas a baseline ainda precisa de clareza sobre quais pontos sao contratos oficiais para modules externos e quais sao apenas sinais observaveis ou detalhes internos.

Integracoes futuras como save system, trofeus/conquistas, telemetria e APIs externas devem depender de seams pequenas, estaveis e claramente promovidas.

A conclusao desta ADR e que o problema atual e mais de plataforma e classificacao do que de quantidade de eventos.

## Decisao
Os seguintes pontos sao tratados como contracts oficiais para extensao externa ou integrada:
- `GameRunStartedEvent`
- `GameRunEndedEvent`
- `WorldResetStartedEvent`
- `WorldResetCompletedEvent`
- `SceneTransitionCompletedEvent`
- `LevelSelectedEvent`
- `LevelSwapLocalAppliedEvent`
- `LevelEnteredEvent`
- `LevelIntroCompletedEvent`
- `PauseStateChangedEvent`
- `ISceneResetHook`
- `IActorLifecycleHook`

Seams especificos de dominio permanecem no proprio owner e nao sao promovidos a hook oficial global:
- `IActorGroupGameplayResetOrchestrator`
- `ILevelPostRunHookService`

Os seguintes sinais sao apenas observaveis e nao devem ser tratados como contrato externo principal:
- `GameRunEndRequestedEvent`
- `GameResetRequestedEvent`
- `GameExitToMenuRequestedEvent`
- `SceneTransitionStartedEvent`
- `SceneTransitionFadeInCompletedEvent`
- `SceneTransitionScenesReadyEvent`
- `SceneTransitionBeforeFadeOutEvent`
- `SceneFlowRouteLoadingProgressEvent`
- `ReadinessChangedEvent`
- `InputModeRequestEvent`
- `GameLoopActivityChangedEvent`

Os seguintes sinais sao internos ou tecnicos e nao devem ser promovidos como API publica de extensao:
- `GameStartRequestedEvent`
- `GamePauseCommandEvent`
- `GameResumeRequestedEvent`
- `SceneFlowInputModeBridge`
- `LoadingProgressOrchestrator`
- `PostRunOwnershipService`

## Official Hooks
- `GameRunStartedEvent`: use para marcar o inicio real de uma run e iniciar integracoes de sessao.
- `GameRunEndedEvent`: use para salvar, conceder trofeus e fechar telemetria no fim consolidado da run.
- `WorldResetStartedEvent`: use para registrar o inicio do reset macro e preparar checkpoints ou flushes.
- `WorldResetCompletedEvent`: use para reagir ao reset macro concluido e validar estado pronto.
- `SceneTransitionCompletedEvent`: use para integracoes que dependem da rota final ja aplicada.
- `LevelSelectedEvent`: use para capturar selecao de level e contexto atual do fluxo.
- `LevelSwapLocalAppliedEvent`: use para reagir ao fim efetivo do swap local.
- `LevelEnteredEvent`: use como hook oficial pos-aplicacao do level para seams level-owned, incluindo IntroStage.
- `LevelIntroCompletedEvent`: use como handoff oficial para o fluxo level->gameplay apos a intro concluir ou ser pulada.
- `PauseStateChangedEvent`: use para reagir a entrada/saida de pause sem depender de wiring interno do GameLoop ou do overlay.
- `ISceneResetHook`: use para extensao local do lifecycle de reset de cena.
- `IActorLifecycleHook`: use para extensao local do lifecycle de atores durante reset.

## Observable but Not Official
- `GameRunEndRequestedEvent`: intencao de fim de run, util para auditoria e tracking.
- `GameResetRequestedEvent`: intencao de restart macro, util para UI e instrumentation.
- `GameExitToMenuRequestedEvent`: intencao de exit para menu, util para tracking.
- `SceneTransitionStartedEvent`: marker tecnico do inicio da transicao macro.
- `SceneTransitionFadeInCompletedEvent`: marker tecnico da fase de fade.
- `SceneTransitionScenesReadyEvent`: marker tecnico de scenes prontas.
- `SceneTransitionBeforeFadeOutEvent`: marker tecnico antes do fade out final.
- `SceneFlowRouteLoadingProgressEvent`: observabilidade de progresso, nao seam de extensao.
- `ReadinessChangedEvent`: snapshot de readiness para consumers internos e UI.
- `InputModeRequestEvent`: request de input mode, nao contrato de dominio externo.
- `GameLoopActivityChangedEvent`: telemetria de atividade do loop, nao seam de ownership.

## Internal Signals
- `GameStartRequestedEvent`
- `GamePauseCommandEvent`
- `GameResumeRequestedEvent`

## Nota de Pause
`GamePauseCommandEvent` e `GameResumeRequestedEvent` continuam internos e podem permanecer como detalhes de implementacao do trilho atual. O contrato publico de pause passa a ser representado por `PauseStateChangedEvent` e pelas interfaces `IPauseCommands` / `IPauseStateService` descritas no ADR de Pause.

## Internal Components
- `SceneFlowInputModeBridge`
- `LoadingProgressOrchestrator`
- `PostRunOwnershipService`

## Candidate Gaps
Nenhum gap novo foi aprovado nesta ADR.

Se uma lacuna futura provar ser real para save, trofeus, telemetria ou APIs externas, ela deve ser aberta como seam dedicado e pequeno, nao como generalizacao ampla do bus ou da pipeline.

## Consequences
- `Docs/Canon/Official-Baseline-Hooks.md` e o guia canônico de uso deste ADR.
- Modulos externos devem preferir os hooks oficiais acima.
- Eventos observaveis continuam uteis para UI, debug e telemetry, mas nao devem virar dependencias duras.
- Sinais tecnicos de pipeline nao devem ser usados como API publica de integracao.
- A baseline ganha uma politica clara de promocao de hooks sem inflar desnecessariamente o numero de contratos.

## Non-goals
- Nao implementar save
- Nao implementar trofeus
- Nao criar sistema de plugins
- Nao criar mega-bus novo
- Nao promover todo evento existente a API publica

