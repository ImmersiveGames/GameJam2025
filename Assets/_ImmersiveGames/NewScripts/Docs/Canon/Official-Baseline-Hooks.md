# Hooks Oficiais da Baseline

## 1. Propósito
Este documento define o conjunto oficial de hooks para integrações externas e módulos independentes. Use esses seams primeiro para save, troféus, telemetria e APIs externas; não promova sinais técnicos de pipeline a contratos públicos.

## 2. Hooks Oficiais
- `GameRunStartedEvent`: use quando precisar do início real de uma run; não use para fluxos apenas de request ou UI pré-run.
- `GameRunEndedEvent`: use para save, troféus e telemetria terminal no resultado final da run; não use para fluxos apenas de request.
- `WorldResetStartedEvent`: use como limite do reset para bookkeeping pré-reset; não use como marker por actor ou por pipeline.
- `WorldResetCompletedEvent`: use quando o estado do mundo estiver pronto após o reset; não use como sinal de progresso de loading.
- `SceneTransitionCompletedEvent`: use quando a rota alvo já estiver totalmente aplicada; não use para marcos intermediários de loading.
- `LevelSelectedEvent`: use quando um level for selecionado para o fluxo atual; não use como prova de que o swap já foi comprometido.
- `LevelSwapLocalAppliedEvent`: use quando o swap local de level for de fato aplicado; não use para seleção ou roteamento macro.
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

## 5. Pontos de Integração Recomendados
- Save: prefira `GameRunEndedEvent`, `WorldResetCompletedEvent` e `SceneTransitionCompletedEvent`.
- Troféus/conquistas: prefira `GameRunEndedEvent`, `LevelSelectedEvent` e `LevelSwapLocalAppliedEvent`.
- Telemetria: combine hooks oficiais com eventos observáveis quando necessário.
- APIs externas: prefira apenas hooks oficiais.

## 6. Notas
Promova novos hooks apenas por ADR ou plano, nunca por uso casual ou consumo ad hoc.
