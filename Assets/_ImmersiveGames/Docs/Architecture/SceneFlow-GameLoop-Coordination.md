# Scene Flow ↔ GameLoop Coordination (NewScripts)

## Objetivo
Definir como o GameLoop sincroniza seu início com o pipeline de Scene Flow e com o reset determinístico do WorldLifecycle, evitando:

- Start precoce (antes do mundo estar pronto)
- Start duplo (mesmo evento com semântica dupla)

## Visão Geral
O GameLoop é uma FSM em runtime C# (`IGameLoopService`) e recebe sinais via métodos `Request*`.

A transição de cenas e o reset determinístico não são responsabilidade do GameLoop. Eles pertencem ao pipeline de **Scene Flow + WorldLifecycle**.

A sincronização ocorre via eventos do EventBus.

> Pré-requisito operacional: o `WorldLifecycleBaselineRunner` deve passar (Hard Reset + Soft Reset Players como MVP) antes de validar o fluxo de start.

## Eventos envolvidos

### Eventos REQUEST (intenção)
- `GameStartRequestedEvent`:
    - emitido por UI/menus/sistemas que desejam iniciar o jogo;
    - **não** inicia a FSM diretamente.

### Eventos Scene Flow
- `SceneTransitionStartedEvent`
- `SceneTransitionScenesReadyEvent`
- `SceneTransitionCompletedEvent`

### Eventos COMMAND (definitivos)
- `GameStartEvent`:
    - emitido apenas quando o início do jogo está liberado;
    - consumido pelo `GameLoopEventInputBridge` → chama `IGameLoopService.RequestStart()`.

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
Consome eventos COMMAND e converte em chamadas no serviço, por exemplo:

- `GameStartEvent` → `RequestStart()`

> Nota: outros comandos (pause/resume/reset) seguem a mesma diretriz: REQUEST ≠ COMMAND.

## Regras de ouro
1) Nunca usar `GameStartEvent` como “pedido” e “comando” ao mesmo tempo.
2) UI emite REQUEST, coordinator emite COMMAND.
3) GameLoopEventInputBridge consome apenas COMMAND.
4) O perfil (`TransitionProfileName`) é a chave para correlacionar o ScenesReady ao start pendente.

## Sobre o StartPlan (adiado)
O `SceneTransitionRequest startPlan` define quais cenas carregar/descarregar e qual cena ficará ativa.

A definição do conteúdo do plano deve ser tratada separadamente, após estabilizar a semântica REQUEST/COMMAND e o coordinator.
