# SessionActivityPipeline Sandbox v0

## Objetivo
Validar um ciclo mínimo de Session Activity isolado do gameplay legado, em Base 1.1, sem acionar owners do gameplay legado/Base 1.0.
O sandbox usa identificação própria `Sandbox`. `Overlay` foi apenas o escudo provisório inicial.

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

## SimulationGate canônico Base 1.1
O `SimulationGate` é `Pipeline Adapter`.
Ele executa bloqueio/liberação e não decide lifecycle.
Quem decide `PauseSimulation` / `ResumeSimulation` é o `SessionActivityPipeline`.
Quem decide navegação, restart e deactivation é o `SessionActivityPipeline`.
O `SimulationGate` apenas obedece `Pipeline Command` e emite `fact` e `snapshot`.

O novo `SimulationGate` não usa:

1. `LegacySimulationGate`
2. `LegacySimulationGateTokens`
3. string tokens
4. `ref-count`
5. `IDisposable`
6. `GateChanged`
7. `ReleaseAll`
8. `EventBus`
9. `GamePauseGateBridge`

Comandos do gate:

1. `BlockActivitySimulation`
2. `ReleaseActivitySimulation`
3. `BlockSessionSimulation`
4. `ReleaseSessionSimulation`

Facts do gate:

1. `ActivitySimulationBlocked`
2. `ActivitySimulationReleased`
3. `SessionSimulationBlocked`
4. `SessionSimulationReleased`
5. `SimulationGateCommandRejected`

`gateState` expõe:

1. `sessionBlocked`
2. `sessionIdentity`
3. `activityBlocked`
4. `activityIdentity`
5. `lastFact`
6. `lastSnapshot`

Identidade obrigatória para comandos de activity:

1. `pipelineId`
2. `sessionStateId`
3. `activityId`
4. `activityOrdinal`
5. `entrySequence`
6. `stage`
7. `source`
8. `reason`

Regras validadas:

1. `PauseSimulation` em `GameplayRunning` gera `BlockActivitySimulation`.
2. `ResumeSimulation` em `GameplayRunning` pausado gera `ReleaseActivitySimulation`.
3. `PauseSimulation` não muda `stage`.
4. `PauseSimulation` não muda `activity`.
5. `PauseSimulation` não muda `entrySequence`.
6. `PauseSimulation` não cria `handoff`.
7. `ResumeSimulation` não muda `stage`.
8. `ResumeSimulation` não muda `activity`.
9. `ResumeSimulation` não muda `entrySequence`.
10. `ResumeSimulation` não cria `handoff`.

Navegação enquanto pausado:

1. Se a activity pausada navega para outra activity, o pipeline emite `ReleaseActivitySimulation` para a identity antiga.
2. A activity antiga entra em `Deactivation`.
3. O `handoff` é preparado para a nova activity.
4. A nova activity entra em `GameplayRunning` com `simulationState Running`.
5. `gateState.activityBlocked` termina `False`.
6. Não sobra bloqueio stale da activity anterior.

Restart enquanto pausado:

1. Se `activity_02 entrySequence=2` está pausada, `RestartCurrentActivity` libera o gate da `entrySequence=2`.
2. Depois cria nova entrada com `entrySequence=3`.
3. A nova execução entra em `GameplayRunning`.
4. `gateState.activityBlocked` termina `False`.
5. O gate não confunde a execução antiga com a nova.

Relação com stale/foreign:

1. `entrySequence` participa da identidade operacional.
2. Comando velho ou foreign não pode alterar o gate ativo.
3. Rejeições do gate devem ser explícitas.

`GameLoop`, `InputModes` e `Gates` reais ainda não foram testados neste sandbox v0.

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
12. `SimulationGate` rejeita comandos stale/foreign de forma explícita e sem alterar o estado ativo.

## Limites do sandbox
1. Não testa gameplay real.
2. Não testa actors reais.
3. Não testa `GameLoop` real.
4. Não testa `InputModes` reais.
5. Não testa `Gates` reais.
6. Não substitui `SessionTransition`.
7. Ainda não testa `IntroStage` real.
8. Ainda não testa `PostRun` real.
9. Ainda não testa `Save` real.
10. Ainda não testa `Loading` real.
11. Não depende mais de `routeKind='Overlay'` como semântica do sandbox.
12. O `SimulationGate` atual só executa bloqueio/liberação no sandbox, sem plugar efeitos externos.

## Regras de proteção
1. Não chamar `GameplaySessionFlow`, `SessionTransition`, `GameplayPhaseFlow`, `GameLoop`, `InputModes`, `Gates` ou `ActorsExecution`.
2. Não transformar o sandbox em gameplay paralelo.
3. Host/painel só enviam comandos.
4. O pipeline decide lifecycle.
5. Mensagens antigas ou de outro ciclo não podem alterar o ciclo ativo.
6. `SceneFlow/Navigation` continua como `Pipeline Adapter`, não owner do ciclo.
7. O `SimulationGate` não tem ownership de lifecycle.

## Resumo final
O sandbox v0 agora prova lifecycle de activity, navegação canônica, pause/resume e `SimulationGate` canônico Base 1.1.
`SessionActivityPipeline` decide.
`SimulationGate` executa.
Host/painel só enviam `Pipeline Command`.
Não há ownership de lifecycle dentro do gate.
