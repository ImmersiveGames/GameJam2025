# Hooks Oficiais da Baseline

## 1. Propósito
Este documento define o conjunto oficial de hooks para integrações externas e módulos independentes. Use esses seams primeiro para save, troféus, telemetria e APIs externas; não promova sinais técnicos de pipeline a contratos públicos.

Rodada estrutural atual:
- `PauseStateChangedEvent`, `PauseWillEnterEvent` e `PauseWillExitEvent` são os hooks oficiais do contrato mínimo de `Pause`.
- `PostStageStartRequestedEvent`, `PostStageStartedEvent` e `PostStageCompletedEvent` continuam como hooks oficiais do `PostStage`.

## 2. Hooks Oficiais
- `GameRunStartedEvent`: use quando precisar do início real de uma run; não use para fluxos apenas de request ou UI pré-run.
- `GameRunEndedEvent`: use para save, troféus e telemetria terminal no resultado final da run; não use para fluxos apenas de request.
- `WorldResetStartedEvent`: use como limite do reset para bookkeeping pré-reset; não use como marker por actor ou por pipeline.
- `WorldResetCompletedEvent`: use quando o estado do mundo estiver pronto após o reset; não use como sinal de progresso de loading.
- `SceneTransitionCompletedEvent`: use quando a rota alvo já estiver totalmente aplicada; não use para marcos intermediários de loading.
- `PauseWillEnterEvent`: use como hook precoce quando a pausa vai entrar; não use como verdade final do estado.
- `PauseWillExitEvent`: use como hook precoce quando a pausa vai sair; não use como verdade final do estado.
- `PauseStateChangedEvent`: use como verdade final do estado de pause; não dependa do wiring interno do GameLoop ou do overlay.
- `LevelSelectedEvent`: use quando um level for selecionado para o fluxo atual; não use como prova de que o swap já foi comprometido.
- `LevelSwapLocalAppliedEvent`: use quando o swap local de level for de fato aplicado; não use para seleção ou roteamento macro.
- `LevelEnteredEvent`: use como hook canônico pós-aplicação do level para seams level-owned, incluindo IntroStage; não use antes do level estar ativo.
- `LevelIntroCompletedEvent`: use como handoff canônico de fim da intro para liberar o fluxo level->gameplay; não use como substituto de `LevelEnteredEvent`.
- `PostStageStartRequestedEvent`: use para observar o pedido oficial de entrada no stage pos-outcome.
- `PostStageStartedEvent`: use para observar que o stage foi assumido.
- `PostStageCompletedEvent`: use para observar o encerramento oficial do stage e o handoff final ao `GameLoop`.
- `ISceneResetHook`: use para extensões locais do lifecycle de reset de cena; não use como hook global de módulo.
- `IActorLifecycleHook`: use para lifecycle local de actor durante reset; não use para lógica de gameplay fora de reset.

## 3. Observáveis, mas Não Oficiais
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

Esses sinais são úteis para UI, debug e telemetria, mas não são o seam principal para integração externa.

## 4. Internos / Não Contratuais
- `GameStartRequestedEvent`
- `GamePauseCommandEvent`
- `GameResumeRequestedEvent`
- `SceneFlowInputModeBridge`
- `GameReadinessService`
- `LoadingProgressOrchestrator`
- `PostGameOwnershipService`

Esses sinais e componentes são detalhes internos de coordenação e não devem ser usados como API externa.

## 5. Nota de Pause
`GamePauseCommandEvent` e `GameResumeRequestedEvent` continuam internos. O contrato publico de pause passa a ser representado por `PauseWillEnterEvent`, `PauseWillExitEvent`, `PauseStateChangedEvent` e pelas interfaces `IPauseCommands` / `IPauseStateService` definidas no ADR de Pause.
O overlay permanece reativo, e o ducking de áudio reage aos hooks oficiais do pause sem virar owner do estado.

## 6. Pontos de Integração Recomendados
- Save: prefira `GameRunEndedEvent`, `WorldResetCompletedEvent` e `SceneTransitionCompletedEvent`.
- Troféus/conquistas: prefira `GameRunEndedEvent`, `LevelSelectedEvent` e `LevelSwapLocalAppliedEvent`.
- Telemetria: combine hooks oficiais com eventos observáveis quando necessário.
- APIs externas: prefira apenas hooks oficiais.

## 7. Notas
Promova novos hooks apenas por ADR ou plano, nunca por uso casual ou consumo ad hoc.
