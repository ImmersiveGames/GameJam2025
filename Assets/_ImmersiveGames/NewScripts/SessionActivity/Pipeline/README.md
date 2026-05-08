# SessionActivityPipeline Sandbox v0

## Objetivo
Validar um ciclo minimo de Session Activity isolado do gameplay legado, em Base 1.1, sem acionar owners do gameplay legado/Base 1.0.

## Estado congelado do pause/resume
O sandbox validado hoje ficou assim:

1. `SessionOperationalPipeline` apenas marca `PauseCapabilityPrepared` antes de `ReadyToOpenCurtain`.
2. `SessionActivityPipeline` decide `PauseRequested` e `ResumeRequested`.
3. `SimulationGate` executa `BlockActivitySimulation` e `ReleaseActivitySimulation`.
4. `SessionActivityPauseOverlayAdapter` e um adapter minimo, observavel e sem ownership de lifecycle.
5. A UI real de pause, o controller visual, `InputMode` real e o input global ficam para etapa futura.
6. `GameLoop` nao e owner do novo fluxo de pause/resume.

## Regras validadas de Pause/Resume
1. `PauseRequested` so e aceito em `GameplayRunning` com `simulationState=Running`.
2. `ResumeRequested` so e aceito em `GameplayRunning` com `simulationState=Paused`.
3. `PauseRequested` emite `PauseResolved` e snapshot `pause_resolved`.
4. `ResumeRequested` emite `ResumeResolved` e snapshot `resume_resolved`.
5. Rejeicoes usam `PauseRejected`/`pause_rejected` ou `ResumeRejected`/`resume_rejected` conforme o comando.
6. `PauseRequested` bloqueia o `SimulationGate` e chama `PauseOverlayAdapter.Show`.
7. `ResumeRequested` libera o `SimulationGate` e chama `PauseOverlayAdapter.Hide`.
8. Nenhuma parte do fluxo novo usa `GameLoopCommands` ou `GameLoopService` como owner de pause.

## Fluxo validado no sandbox
1. `StartDemo`
2. `Activity 01 Activation`
3. `Activity 01 GameplayRunning`
4. `RequestPause`
5. `PauseResolved`
6. `RequestResume`
7. `ResumeResolved`
8. `CompleteCurrentActivity`
9. `PhaseResultPresentation`
10. `ContinueToNextActivity`
11. `PipelineCompleted`

## Limites do sandbox
1. Nao testa gameplay real.
2. Nao testa actors reais.
3. Nao testa `GameLoop` real.
4. Nao testa `InputModes` reais.
5. Nao testa `Gates` reais.
6. Nao substitui `SessionTransition`.
7. Ainda nao testa `IntroStage` real.
8. Ainda nao testa `PostRun` real.
9. Ainda nao testa `Save` real.
10. Ainda nao testa `Loading` real.

## Regra de ownership
1. `SessionActivityPipeline` decide pause/resume.
2. `SimulationGate` executa block/release.
3. `SessionActivityPauseOverlayAdapter` e apenas adapter minimo.
4. `GameLoop` nao e owner do novo fluxo de pause/resume.

## Resumo final
O sandbox v0 prova lifecycle de activity, pause/resume canonicos e `SimulationGate` canonico Base 1.1.
O fluxo atual esta congelado para uso futuro como referencia de Base 1.1.
