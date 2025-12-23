# GameLoop (NewScripts)

## Objetivo
O GameLoop define o estado “macro” do jogo (ex.: Menu, Playing, Paused) de forma determinística e desacoplada de MonoBehaviours.
Ele não executa loading de cenas nem reset do mundo diretamente: isso é responsabilidade do pipeline de Scene Flow + WorldLifecycle.

## Componentes

### IGameLoopService / GameLoopService
- FSM em C# puro (sem MonoBehaviour).
- Recebe sinais via `RequestStart / RequestPause / RequestResume / RequestReset`.
- É tickado por um driver (ex.: `GameLoopDriver`), tipicamente atrelado ao update do Unity.

### GameLoopBootstrap
Registra o `IGameLoopService` no DI global e registra bridges de entrada que convertem eventos globais em sinais para o serviço.

### GameLoopEventInputBridge (Entrada)
Bridge de entrada que **consome eventos definitivos** e sinaliza o `IGameLoopService`.
Importante: ele ignora o `GameStartEvent` quando o coordinator de Scene Flow está instalado, evitando start duplo.

### GameLoopSceneFlowCoordinator (Coordenação com Scene Flow)
Coordena a intenção de start com o pipeline de Scene Flow (e, por consequência, WorldLifecycle).
É ele quem garante que “start” só vira “definitivo” quando as cenas estiverem prontas e o reset tiver sido disparado.

## Eventos (contratos)

### Eventos de intenção/entrada
- **GameStartEvent**: intenção de iniciar o jogo (Menu → Playing). Quando o coordinator está instalado, ele consome este evento e libera o start após `ScenesReady`. Sem coordinator, o bridge trata este evento como comando direto.

### Eventos definitivos (COMMAND)
Esses eventos representam “agora pode” e devem ser consumidos por bridges que sinalizam serviços.

- **GamePauseEvent**: comando definitivo informando pausa atual (`IsPaused=true/false`).
- **GameResumeRequestedEvent**: pedido de retomar (tratado como comando no runtime atual).
- **GameResetRequestedEvent**: pedido de reset do GameLoop (FSM volta ao estado inicial).

## Fluxo Opção B (Start sincronizado com Scene Flow)

1) UI/menus/publicador emite:
    - `GameStartEvent`

2) `GameLoopSceneFlowCoordinator` recebe `GameStartEvent` e:
    - chama `ISceneTransitionService.TransitionAsync(startPlan)`
    - aguarda `SceneTransitionScenesReadyEvent` do profile/plano esperado
    - (WorldLifecycleRuntimeDriver reage a `SceneTransitionScenesReadyEvent` e dispara reset determinístico)

3) Quando o coordinator considera “liberado para iniciar”, ele chama:
    - `IGameLoopService.RequestStart()`

4) `GameLoopEventInputBridge` segue consumindo pausa/retomada/reset e ignora `GameStartEvent` enquanto o coordinator estiver instalado.

Observação: essa separação evita “start duplo” e garante que o GameLoop não inicia antes do pipeline de cenas/reset ser disparado.

## QA
- **GameLoopStateFlowQATester** (`Infrastructure/QA/GameLoopStateFlowQATester.cs`): valida Boot → Menu, start via Scene Flow, pausa/retomada/reset e mapeamento do `IStateDependentService`.
- **PlayerMovementLeakSmokeBootstrap** (`Infrastructure/QA/PlayerMovementLeakSmokeBootstrap.cs`): valida gate de pausa, reset e ausência de input fantasma no player.

## Notas de integração com ADR (WorldLifecycle/Gates)
- O GameLoop deve ser liberado apenas quando a transição de cenas estiver pronta e o reset determinístico tiver sido disparado.
- Gates (SimulationGate) controlam permissões de execução e evitam input/ação durante transições e resets.
