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
Importante: ela não deve reagir a “intenções” que ainda dependem de Scene Flow / reset de mundo.

### GameLoopSceneFlowCoordinator (Coordenação com Scene Flow)
Coordena a intenção de start com o pipeline de Scene Flow (e, por consequência, WorldLifecycle).
É ele quem garante que “start” só vira “definitivo” quando as cenas estiverem prontas e o reset tiver sido disparado.

## Eventos (contratos)

### Eventos de intenção (REQUEST)
Esses eventos representam “o usuário pediu”, mas ainda podem exigir:
- transição de cenas,
- reset determinístico do mundo,
- aquisição/liberação de gates.

- **GameStartRequestedEvent**: intenção de iniciar o jogo (Menu → Playing), pode disparar Scene Flow.

### Eventos definitivos (COMMAND)
Esses eventos representam “agora pode” e devem ser consumidos por bridges que sinalizam serviços.

- **GameStartEvent**: comando definitivo para iniciar o loop (consumido pelo `GameLoopEventInputBridge` → `RequestStart()`).
- **GamePauseEvent**: comando definitivo informando pausa atual (`IsPaused=true/false`).
- **GameResumeRequestedEvent**: pedido de retomar (pode ser tratado como comando no runtime atual).
- **GameResetRequestedEvent**: pedido de reset do GameLoop (FSM volta ao estado inicial).

## Fluxo Opção B (Start sincronizado com Scene Flow)

1) UI/menus/publicador emite:
    - `GameStartRequestedEvent`

2) `GameLoopSceneFlowCoordinator` recebe `GameStartRequestedEvent` e:
    - chama `ISceneTransitionService.TransitionAsync(startPlan)`
    - aguarda `SceneTransitionScenesReadyEvent` do profile/plano esperado
    - (WorldLifecycleRuntimeDriver reage a `SceneTransitionScenesReadyEvent` e dispara reset determinístico)

3) Quando o coordinator considera “liberado para iniciar”, ele emite:
    - `GameStartEvent` (definitivo)

4) `GameLoopEventInputBridge` consome `GameStartEvent` e chama:
    - `_gameLoopService.RequestStart()`

Observação: essa separação evita “start duplo” e garante que o GameLoop não inicia antes do pipeline de cenas/reset ser disparado.

## Notas de integração com ADR (WorldLifecycle/Gates)
- O GameLoop deve ser liberado apenas quando a transição de cenas estiver pronta e o reset determinístico tiver sido disparado.
- Gates (SimulationGate) controlam permissões de execução e evitam input/ação durante transições e resets.
