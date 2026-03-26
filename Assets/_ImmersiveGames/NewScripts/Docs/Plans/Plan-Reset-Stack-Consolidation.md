# Plan - Reset Stack Consolidation

Status: **Concluído** (2026-03-26)

## 1. Objective
Consolidar o stack de reset em um contrato canonicamente estável, executável e rastreável, sem reabrir ownership macro já estabilizado fora do escopo de reset.

## 2. Source of Truth
- Base primaria: auditoria mais recente do Reset Stack.
- Contratos preservados:
  - `WorldResetRequest`
  - `WorldResetStartedEvent`
  - `WorldResetCompletedEvent`
  - `ResetDecision`
  - `WorldResetContext`
  - `WorldResetReasons`
  - `ResetKind`
  - `WorldResetOrigin`
- Regras de precedência:
  - `SceneFlow` define policy de rota e `requiresWorldReset`
  - `WorldReset` define request, validação, execução e lifecycle
  - `ResetInterop` não cria policy própria
  - `SimulationGate` permanece owner da trava

## 3. Current Diagnosis
- O fluxo macro de reset já está estabilizado.
- Ainda existe drift residual dentro do stack de reset.
- A maior ambiguidade é a duplicação de policy entre `SceneFlow`, `ResetInterop` e `WorldReset`.
- O owner real do pipeline macro é o `WorldResetOrchestrator`.
- O completion path ainda tem fallback publish fora do publisher canônico.
- `Gameplay` e `SimulationGate` devem permanecer apenas como boundary/consumo.

## 4. Reset Contracts To Preserve
- `WorldReset` permanece owner do lifecycle canonicamente publicado.
- `SceneReset` permanece owner do pipeline local de cena.
- `ResetInterop` permanece bridge fina.
- `SceneFlow` permanece owner da policy de rota.
- `SimulationGate` permanece owner da trava.
- `Gameplay` consome readiness/gate e não decide reset.
- `WorldResetCompletedEvent` continua sendo o contrato canônico de completion para liberar o gate do `SceneFlow`.

## 5. Ownership Matrix

| Area | Current owner | Target owner | Notes |
|---|---|---|---|
| Reset request | `WorldResetRequestService`, `WorldResetCommands`, `SceneFlowWorldResetDriver` | `WorldReset` entry services + `ResetInterop` bridge fina | Requests diferentes não podem carregar policy divergente. |
| Reset execution | `WorldResetOrchestrator` | `WorldResetOrchestrator` | Executar, validar e publicar continuam aqui. |
| Reset validation | `WorldResetValidationPipeline`, `WorldResetSignatureValidator`, route policy em `SceneFlow` | validação de reset em `WorldReset`; policy de rota em `SceneFlow` | Separar validação de request da policy de rota. |
| Reset completion | `WorldResetLifecyclePublisher` + fallback publish no driver + `WorldResetCompletionGate` | publisher canônico único + gate de correlação | Gate não deve ter regra de negócio. |
| Scene-local reset pipeline | `SceneResetController`, `SceneResetRunner`, `SceneResetFacade`, `SceneResetPipeline` | host fino + pipeline local canônico | `SceneReset` não deve depender de fallback de locator como regra normal. |
| Reset gating | `SimulationGateService`, `SceneResetGateLease`, `SimulationGateWorldResetGuard`, `GameReadinessService`, `GameplayStateGate` | `SimulationGate` owner; demais apenas consumidores/leases | Não introduzir policy paralela em reset. |

## 6. Target Architecture
- `WorldReset`: request canônico, dedupe, guards, validation, discovery, execution, post-validation e lifecycle publish.
- `SceneReset`: host fino na cena, pipeline local sequencial, phases explícitas.
- `ResetInterop`: bridge `SceneFlow -> WorldReset` e bridge de completion -> `SceneFlow` gate, sem policy própria.
- `SceneFlow`: owner único de route policy e da decisão `requiresWorldReset`.
- `SimulationGate`: trava única, sem ownership de reset.
- `Gameplay`: consumidor de readiness/gate, sem decisão de reset.

## 7. Execution Checklist
- [ ] F1: contrato canônico explícito fechado
- [ ] F2: policy residual removida do bridge
- [ ] F3: `WorldReset` consolidado como owner do pipeline macro
- [ ] F4: pipeline local de `SceneReset` estabilizado
- [ ] F5: source-of-truths duplicadas removidas
- [ ] F6: boundaries com `Gameplay` e `SimulationGate` limpos
- [ ] `ResetInterop` sem policy própria
- [ ] `SceneFlow` como owner único da policy de rota
- [ ] `SimulationGate` sem ownership de reset

## 8. Exit Condition
O plano pode ser considerado concluído quando `WorldReset`, `SceneReset`, `ResetInterop`, `SceneFlow`, `SimulationGate` e `Gameplay` estiverem com ownership claro e sem policy residual duplicada no stack de reset.

## 9. Final Status
Concluído. O stack foi consolidado em seis fases e ficou como base estabilizada:

- F1: contrato canônico explícito fechado em `WorldReset` e lifecycle.
- F2: `ResetInterop` reduzido a bridge fina.
- F3: `WorldReset` concentrado no orchestrator.
- F4: `SceneReset` estabilizado no pipeline local.
- F5: `requiresWorldReset` consolidado no lado de `SceneFlow`.
- F6: `Gameplay` e `SimulationGate` mantidos como consumidores e owner da trava.
