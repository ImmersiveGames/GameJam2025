# Scene Flow ↔ GameLoop Coordination (NewScripts)

## Objetivo
Definir como o GameLoop sincroniza seu início com o pipeline de Scene Flow e com o reset determinístico do WorldLifecycle, evitando:
- Start precoce (antes do mundo estar pronto)
- Start duplo (mesmo evento com semântica dupla)

## Visão Geral
O GameLoop é uma FSM em runtime C# (`IGameLoopService`) e recebe sinais via métodos `Request*`.
A transição de cenas e o reset determinístico não são responsabilidade do GameLoop. Eles pertencem ao pipeline de Scene Flow + WorldLifecycle.

A sincronização ocorre via eventos do EventBus.

## Eventos envolvidos

### Eventos REQUEST (intenção)
- `GameStartRequestedEvent`:
    - Emitido por UI/menus/sistemas que desejam iniciar o jogo.
    - Não deve iniciar a FSM diretamente.

### Eventos Scene Flow
- `SceneTransitionStartedEvent`
- `SceneTransitionScenesReadyEvent`
- `SceneTransitionCompletedEvent`

### Eventos COMMAND (definitivos)
- `GameStartEvent`:
    - Emitido apenas quando o início do jogo está liberado.
    - Consumido pelo `GameLoopEventInputBridge` → chama `IGameLoopService.RequestStart()`.

### Readiness (bloqueio de simulação e liberação de gameplay)

O `GameReadinessService` escuta eventos do Scene Flow (`SceneTransitionStarted/ScenesReady/Completed`) e:
- adquire/libera `SimulationGateTokens.SceneTransition` durante a transição
- publica `ReadinessChangedEvent` com `ReadinessSnapshot` para consumidores

O `NewScriptsStateDependentService` usa `ReadinessSnapshot.GameplayReady` e `GateOpen` para decidir quando liberar `ActionType.Move`, reduzindo dependência de timing do tick do GameLoop.

## Componentes

### GameLoopSceneFlowCoordinator
Responsável por converter:
`GameStartRequestedEvent` → `ISceneTransitionService.TransitionAsync(startPlan)` → aguardar `SceneTransitionScenesReadyEvent` (filtrado por profile) → emitir `GameStartEvent`.

Responsabilidades:
- Debounce: ignora múltiplos pedidos enquanto um start está pendente.
- Filtro por profile: só reage ao ScenesReady correspondente ao startPlan esperado.
- Não chama `RequestStart()` diretamente (evita duplicidade e mantém “COMMAND” centralizado via EventBus).

### WorldLifecycleRuntimeDriver
Ao receber `SceneTransitionScenesReadyEvent`, dispara hard reset do WorldLifecycle.
O coordinator não executa o reset; ele apenas garante que a “liberação do start” acontece no timing correto.

### GameLoopEventInputBridge
Consome eventos COMMAND e converte em chamadas no serviço:
- `GameStartEvent` → `RequestStart()`
- `GamePauseEvent` → `RequestPause()` / `RequestResume()`
- `GameResetRequestedEvent` → `RequestReset()`

## Regras de ouro
1) **Nunca** usar `GameStartEvent` como “pedido” e “comando” ao mesmo tempo.
2) UI emite REQUEST, coordinator emite COMMAND.
3) GameLoopEventInputBridge consome apenas COMMAND.
4) O perfil (`TransitionProfileName`) é a chave para correlacionar o ScenesReady ao start pendente.

## Sobre o StartPlan (adiado)
O `SceneTransitionRequest startPlan` define “quais cenas carregar/descarregar e qual cena ficará ativa”.
A definição do conteúdo do plano deve ser tratada separadamente, após estabilizar a semântica REQUEST/COMMAND e o coordinator.
