# ADR-0013 - Ciclo de Vida do Jogo (NewScripts)

## Status
- Estado: Implementado
- Data (decisao): 2025-12-24
- Ultima atualizacao: 2026-03-25
- Tipo: Implementacao
- Escopo: WorldReset + SceneReset + ResetInterop + SceneFlow + GameLoop (NewScripts)

## Nota de precedencia canonica (F0)
- Este ADR continua valido como contrato de ciclo de vida implementado.
- Para ownership e policy do stack SceneFlow, a leitura operacional primaria e:
  - `ADR-0030` (fronteiras modulares)
  - `ADR-0031` (pipeline macro)
  - `ADR-0032` (semantica route/level/reset/dedupe)
  - `ADR-0033` (resiliencia fade/loading)
- Referencias da baseline antiga (`ADR-0009`, `ADR-0010` e demais pre-0030) passam a ser historicas quando houver conflito.

## Objetivo de producao
Definir ciclo unico e auditavel com ordem estavel:
1. Boot (startup)
2. Frontend/menu
3. Entrada em gameplay
4. Playing
5. PostGame

## Sequencia canonica consolidada
- `SceneFlow` executa a transicao macro.
- Em `ScenesReady`, o handoff de reset ocorre via `ResetInterop` para `WorldReset` quando aplicavel.
- O completion gate macro (reset + prepare/clear) conclui antes da revelacao final.
- `SceneTransitionCompleted` fecha o ciclo macro.
- `GameLoop` segue para IntroStage/Playing conforme contrato de estado.

## Invariantes (contrato)

### SceneFlow
- `SceneTransitionStartedEvent` fecha `flow.scene_transition`.
- `ScenesReady` ocorre antes de `SceneTransitionCompletedEvent` na mesma `signature`.
- `set-active` permanece no trilho macro de SceneFlow.
- Fade e Loading permanecem apresentacao, nao ownership de fluxo.

### WorldReset / SceneReset / ResetInterop
- `ResetWorld` e deterministico para mesmo `reason/contextSignature`.
- `WorldResetCompletedEvent` e correlacionado por `ContextSignature` para liberar o gate macro.
- `ResetInterop` faz ponte e correlacao; nao absorve policy de reset.

### GameLoop
- IntroStage bloqueia `sim.gameplay` ate confirmacao.
- Entrada em Playing ocorre apos desbloqueio da simulacao.
- PostGame permanece idempotente.

## Nao-objetivos
- Nao redefine arquitetura fora da linha canonica 0030..0033.
- Nao move ownership de SceneFlow para Loading/Fade.
- Nao colapsa reset macro e reset local em um unico conceito.

## Mapeamento principal (implementacao)
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- `Modules/ResetInterop/Runtime/SceneFlowWorldResetDriver.cs`
- `Modules/ResetInterop/Runtime/WorldResetCompletionGate.cs`
- `Modules/WorldReset/Application/WorldResetService.cs`
- `Modules/GameLoop/Flow/GameLoopSceneFlowSyncCoordinator.cs`
- `Modules/SceneFlow/Interop/SceneFlowInputModeBridge.cs`

## Observabilidade minima
- `SceneTransitionStartedEvent`
- `SceneTransitionScenesReadyEvent`
- `SceneTransitionBeforeFadeOutEvent`
- `SceneTransitionCompletedEvent`
- `WorldResetStartedEvent` / `WorldResetCompletedEvent`
- Sinais de gate de gameplay (`blocked/unblocked`) e entrada em Playing

## Evidencia
- Fonte canonica atual: `Docs/Reports/Evidence/LATEST.md`
