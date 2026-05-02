# SessionActivityPipeline Sandbox v0

## Objetivo
Validar um ciclo mínimo de Session Activity isolado do gameplay legado, em Base 1.1, sem acionar owners do gameplay legado/Base 1.0.
O sandbox usa identificação própria `Sandbox`; `Overlay` foi apenas o escudo provisório inicial.

## Fluxo feliz validado
1. `StartDemo`
2. `Activity 01 Activation`
3. `Activity 01 GameplayRunning`
4. `CompleteCurrentActivity`
5. `PhaseResultPresentation`
6. `Pipeline Handoff` para `Activity 02`
7. `ContinueToNextActivity`
8. `Activity 02 Activation`
9. `Activity 02 GameplayRunning`
10. `CompleteCurrentActivity`
11. `PipelineCompleted`

## Navegação canônica entre activities
O sandbox valida navegação canônica entre activities sem alterar `currentActivity` diretamente pelo painel.
Toda navegação passa pela mesma cascata:

1. `Deactivation`
2. `Pipeline Handoff`
3. `Activation`
4. `GameplayRunning`

Os comandos validados para essa navegação são:

1. `GoToNextActivity`
2. `GoToPreviousActivity`
3. `RestartCurrentActivity`
4. `GoToActivity01`
5. `GoToActivity02`

Rejeições de borda validadas:

1. `Previous` na primeira activity -> `no_previous_activity`
2. `Next` na última activity -> `no_next_activity`
3. Destino igual ao atual -> `already_on_activity`
4. Activity inexistente -> `activity_not_found`

## entrySequence
Cada entrada real em uma activity recebe uma nova `entrySequence`.

Casos validados:

1. `StartDemo` -> `entrySequence 1`
2. `RestartCurrentActivity` -> nova `entrySequence`
3. `GoToActivity02` -> nova `entrySequence`
4. `GoToActivity01` -> nova `entrySequence`

`entrySequence` aparece explicitamente em:

1. `Pipeline Identity`
2. `Pipeline Snapshot`
3. `Pipeline Handoff`
4. `DumpState`
5. `Trace` e logs

## Estado de simulação da activity
A activity agora possui um `simulationState` explícito:

1. `Unknown`
2. `Stopped`
3. `Running`
4. `Paused`

Regras validadas:

1. Ao entrar em `GameplayRunning`, `simulationState` vira `Running`.
2. Ao entrar em `Deactivation`, `simulationState` vira `Stopped`.
3. `PipelineCompleted` mantém `simulationState` em `Stopped`.
4. `PauseSimulation` não muda `currentActivity`.
5. `PauseSimulation` não muda `entrySequence`.
6. `PauseSimulation` não cria `handoff`.
7. `ResumeSimulation` não muda `currentActivity`.
8. `ResumeSimulation` não muda `entrySequence`.
9. `ResumeSimulation` não cria `handoff`.

Comandos validados:

1. `PauseSimulation`
2. `ResumeSimulation`

Facts validados:

1. `SimulationPaused`
2. `SimulationResumed`

Snapshots validados:

1. `simulation_paused`
2. `simulation_resumed`

Rejeições validadas:

1. `PauseSimulation` antes de `StartDemo` -> `pipeline_not_started`
2. `ResumeSimulation` antes de `StartDemo` -> `pipeline_not_started`
3. `PauseSimulation` quando já está `Paused` -> `simulation_already_paused`
4. `ResumeSimulation` quando está `Running` -> `simulation_not_paused`
5. `PauseSimulation` depois de `PipelineCompleted` -> `pipeline_completed`
6. `ResumeSimulation` depois de `PipelineCompleted` -> `pipeline_completed`
7. `PauseSimulation` / `ResumeSimulation` fora de `GameplayRunning` e antes de `Completed` -> `unexpected_stage`

Policy terminal:

1. Se `completed=True` ou `stage=Completed`, qualquer `PauseSimulation` / `ResumeSimulation` rejeita com `pipeline_completed` antes de avaliar `simulationState` ou `stage`.

Navegação enquanto pausado:

1. `GoToNextActivity`, `GoToPreviousActivity`, `RestartCurrentActivity`, `GoToActivity01` e `GoToActivity02` continuam sob ownership do pipeline.
2. Ao sair da activity pausada, `Deactivation` limpa `simulationState` para `Stopped`.
3. A próxima entrada em `GameplayRunning` começa como `Running`.
4. `PauseSimulation` não vaza para a próxima activity.

Relação futura com Gate:

1. `PauseSimulation` / `ResumeSimulation` são decisões do `SessionActivityPipeline`.
2. Um gate futuro deve ser `Pipeline Adapter`.
3. O gate executa bloqueio/liberação.
4. O gate não decide lifecycle.
5. `GameLoop`, `InputModes` e `Gates` reais ainda não foram testados neste sandbox v0.

## Regras validadas
1. Comando fora de ordem é rejeitado.
2. Comando depois de `Completed` é rejeitado com `pipeline_completed`.
3. Comando stale/foreign é rejeitado com `stale_or_foreign_command`.
4. A rejeição não altera `state`, `currentActivity`, `identity` ou `handoff`.
5. A rejeição não emite `Pipeline Snapshot`.
6. A ausência válida de phase result vira skip explícito.
7. `RestartCurrentActivity` não reutiliza a identidade operacional da execução anterior da mesma activity.
8. Voltar para uma activity anterior não reutiliza a identidade operacional.
9. `Pipeline Handoff` transporta `from` e `to` com `entrySequence`.
10. `stale/foreign` considera `entrySequence` na validação da identidade.
11. `PauseSimulation` / `ResumeSimulation` respeitam `simulationState`, `stage` e `entrySequence` sem alterar lifecycle.

## Limites do sandbox
1. Não testa gameplay real.
2. Não testa actors.
3. Não testa `GameLoop`.
4. Não testa `InputModes`.
5. Não substitui `SessionTransition`.
6. Não depende mais de `routeKind='Overlay'` como semântica do sandbox.
7. Ainda não testa `IntroStage` real.
8. Ainda não testa `PostRun` real.
9. Ainda não testa `Actors` reais.
10. Ainda não testa `GameLoop` real.
11. Ainda não testa `InputModes` reais.
12. Ainda não testa `Gates` reais.
13. Ainda não testa `Save` ou `Loading` reais.

## Regras de proteção
1. Não chamar `GameplaySessionFlow`, `SessionTransition`, `GameplayPhaseFlow`, `GameLoop`, `InputModes`, `Gates` ou `ActorsExecution`.
2. Não transformar o sandbox em gameplay paralelo.
3. Host/painel só enviam comandos.
4. O pipeline decide lifecycle.
5. Mensagens antigas ou de outro ciclo não podem alterar o ciclo ativo.
6. `SceneFlow/Navigation` continua como `Pipeline Adapter`, não owner do ciclo.
