# Plan — Reset Stack Consolidation

## 1. Objective
Consolidar o stack de reset em um contrato canonicamente estavel, executavel e rastreavel, sem reabrir ownership macro ja estabilizado fora do escopo de reset.

O plano fixa boundaries claras entre:
- `WorldReset` como owner unico do reset macro
- `SceneReset` como owner do pipeline local
- `ResetInterop` como bridge fina entre `SceneFlow` e `WorldReset`
- `SceneFlow` como owner da policy de rota
- `SimulationGate` como owner da trava
- `Gameplay` como consumidor de readiness/gate, sem ownership de reset

## 2. Source of Truth
- Base primaria: auditoria mais recente do Reset Stack.
- Contratos canonicamente preservados:
  - `WorldResetRequest`
  - `WorldResetStartedEvent`
  - `WorldResetCompletedEvent`
  - `ResetDecision`
  - `WorldResetContext`
  - `WorldResetReasons`
  - `ResetKind`
  - `WorldResetOrigin`
- Regras de precedencia:
  - `SceneFlow` define policy de rota e `requiresWorldReset`
  - `WorldReset` define request, validacao, execucao e lifecycle
  - `ResetInterop` nao cria policy propria
  - `SimulationGate` permanece owner da trava
- Contrato unico explicito a preservar:
  - `kind`
  - `targetScene`
  - `contextSignature`
  - `reason`
  - `origin`

## 3. Current Diagnosis
- O fluxo macro de reset ja esta estabilizado, mas ainda existe drift residual dentro do stack de reset.
- A maior ambiguidade atual e a duplicacao de policy entre `SceneFlow`, `ResetInterop` e `WorldReset`.
- O owner real do pipeline macro e o `WorldResetOrchestrator`, mas o `WorldResetService` ainda concentra dedupe, composição e fallback.
- O `SceneReset` esta funcional, mas o host/runner/facade/pipeline ainda carregam mais responsabilidade do que o necessario.
- O completion path ainda tem fallback publish fora do publisher canonico.
- `Gameplay` e `SimulationGate` devem permanecer apenas como boundary/consumo, nao como locus de policy de reset.

## 4. Reset Contracts To Preserve
- `WorldReset` permanece owner do lifecycle canonicamente publicado.
- `SceneReset` permanece owner do pipeline local de cena.
- `ResetInterop` permanece bridge fina.
- `SceneFlow` permanece owner da policy de rota.
- `SimulationGate` permanece owner da trava.
- `Gameplay` consome readiness/gate e nao decide reset.
- `kind`, `targetScene`, `contextSignature`, `reason` e `origin` devem vir de um contrato unico e explicito.
- `WorldResetCompletedEvent` continua sendo o contrato canonico de completion para liberar o gate do `SceneFlow`.

## 5. Ownership Matrix

| Area | Current owner | Target owner | Notes |
|---|---|---|---|
| Reset request | `WorldResetRequestService`, `WorldResetCommands`, `SceneFlowWorldResetDriver` | `WorldReset` entry services + `ResetInterop` bridge fina | Requests diferentes nao podem carregar policy divergente. |
| Reset execution | `WorldResetOrchestrator` | `WorldResetOrchestrator` | Executar, validar e publicar continuam aqui. |
| Reset validation | `WorldResetValidationPipeline`, `WorldResetSignatureValidator`, route policy em `SceneFlow` | validação de reset em `WorldReset`; policy de rota em `SceneFlow` | Separar validacao de request da policy de rota. |
| Reset completion | `WorldResetLifecyclePublisher` + fallback publish no driver + `WorldResetCompletionGate` | publisher canonico unico + gate de correlacao | Gate nao deve ter regra de negocio. |
| Scene-local reset pipeline | `SceneResetController`, `SceneResetRunner`, `SceneResetFacade`, `SceneResetPipeline` | host fino + pipeline local canonico | `SceneReset` nao deve depender de fallback de locator como regra normal. |
| World vs scene boundary | `SceneFlowWorldResetDriver`, `SceneResetControllerLocator` | `SceneFlow` + composition roots | Boundary deve ser resolvido antes do pipeline local. |
| Reset gating | `SimulationGateService`, `SceneResetGateLease`, `SimulationGateWorldResetGuard`, `GameReadinessService`, `GameplayStateGate` | `SimulationGate` owner; demais apenas consumidores/leases | Nao introduzir policy paralela em reset. |
| Reasons/signatures/context | `WorldResetRequest`, `WorldResetReasons`, `ResetDecision`, `WorldResetContext` | contrato unico de request/context | Sem inferencia espalhada. |
| Interop com SceneFlow | `SceneFlowWorldResetDriver`, `WorldResetCompletionGate` | `ResetInterop` | Bridge deve refletir contrato canonico, nao policy propria. |
| Interop com Gameplay/SimulationGate | `GameReadinessService`, `GameplayStateGate`, `GamePauseGateBridge` | boundary de consumo | `Gameplay` nao deve decidir reset. |

## 6. Source-of-Truth Gaps
- `requiresWorldReset` aparece como regra em `SceneRouteDefinitionAsset`, `SceneRouteCatalogAsset` e `SceneFlowWorldResetDriver`.
- `targetScene` tem tres semanticas hoje: scene atual, vazio com fallback, e lookup amplo em locator.
- `kind` e inferido no publisher pelo `LevelSignature`, nao explicitado na request.
- `SceneFlowWorldResetDriver` ainda carrega policy residual e fallback publish.
- O pipeline local de `SceneReset` ainda usa strings e sinais operacionais que nao pertencem ao contrato canonico do reset.

## 7. Hotspots
- `Modules/WorldReset/Application/WorldResetService.cs`
- `Modules/WorldReset/Application/WorldResetOrchestrator.cs`
- `Modules/WorldReset/Application/WorldResetLifecyclePublisher.cs`
- `Modules/SceneReset/Bindings/SceneResetController.cs`
- `Modules/SceneReset/Bindings/SceneResetRunner.cs`
- `Modules/SceneReset/Runtime/SceneResetFacade.cs`
- `Modules/SceneReset/Runtime/SceneResetPipeline.cs`
- `Modules/SceneReset/Runtime/SceneResetHookCatalog.cs`
- `Modules/SceneReset/Runtime/SceneResetHookSourceResolver.cs`
- `Modules/SceneReset/Runtime/SceneResetHookScopeFilter.cs`
- `Modules/ResetInterop/Runtime/SceneFlowWorldResetDriver.cs`
- `Modules/ResetInterop/Runtime/WorldResetCompletionGate.cs`
- `Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs`
- `Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs`
- `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs`
- `Modules/Gameplay/State/GameplayStateGate.cs`

## 8. Target Architecture
- `WorldReset`:
  - request canonico
  - dedupe
  - guards
  - validation
  - discovery
  - execution
  - post-validation
  - lifecycle publish
- `SceneReset`:
  - host fino na cena
  - pipeline local sequencial
  - phases explicitas
  - hook discovery e scope filter internos ao modulo
- `ResetInterop`:
  - bridge SceneFlow -> WorldReset
  - bridge completion -> SceneFlow gate
  - sem policy propria
- `SceneFlow`:
  - owner unico de route policy
  - source da decisao `requiresWorldReset`
- `SimulationGate`:
  - trava unica
  - leases e observacao, sem ownership de reset
- `Gameplay`:
  - consumidor de readiness/gate
  - sem decisao de reset

## 9. Phased Execution Plan

### F1
- objective: congelar o contrato canonico de reset e eliminar inferencias implicitas de request/lifecycle.
- main files:
  - `Modules/WorldReset/Domain/WorldResetRequest.cs`
  - `Modules/WorldReset/Domain/WorldResetContext.cs`
  - `Modules/WorldReset/Application/WorldResetLifecyclePublisher.cs`
  - `Modules/WorldReset/Contracts/WorldResetStartedEvent.cs`
  - `Modules/WorldReset/Contracts/WorldResetCompletedEvent.cs`
- expected outcome: `kind`, `targetScene`, `contextSignature`, `reason` e `origin` passam a ser tratados como contrato explicito unico.
- risk: medio; qualquer ajuste aqui afeta observabilidade e correlacao.
- done criteria: nenhum ponto critica inferindo semantica de lifecycle fora do request/context.

### F2
- objective: remover policy residual do `ResetInterop` e reduzir `SceneFlowWorldResetDriver` a bridge fina.
- main files:
  - `Modules/ResetInterop/Runtime/SceneFlowWorldResetDriver.cs`
  - `Modules/ResetInterop/Runtime/WorldResetCompletionGate.cs`
  - `Modules/WorldReset/Policies/SceneRouteResetPolicy.cs`
  - `Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs`
  - `Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs`
- expected outcome: o bridge consome decisao pronta e nao carrega regra de rota paralela.
- risk: alto; e o ponto com maior chance de regressao de fluxo.
- done criteria: `ResetInterop` nao decide policy de reset, apenas executa handoff e correlacao.

### F3
- objective: consolidar `WorldReset` como owner unico do pipeline macro interno.
- main files:
  - `Modules/WorldReset/Application/WorldResetService.cs`
  - `Modules/WorldReset/Application/WorldResetOrchestrator.cs`
  - `Modules/WorldReset/Application/WorldResetExecutor.cs`
  - `Modules/WorldReset/Application/WorldResetPostResetValidator.cs`
  - `Modules/WorldReset/Validation/WorldResetValidationPipeline.cs`
  - `Modules/WorldReset/Guards/SimulationGateWorldResetGuard.cs`
- expected outcome: `WorldResetService` fica fino e o orchestrator vira owner real da sequencia macro.
- risk: medio.
- done criteria: dedupe, guard, validate, execute e publish ficam claramente separados por responsabilidade.

### F4
- objective: reduzir drift interno do `SceneReset` e estabilizar o pipeline local.
- main files:
  - `Modules/SceneReset/Bindings/SceneResetController.cs`
  - `Modules/SceneReset/Bindings/SceneResetRunner.cs`
  - `Modules/SceneReset/Bindings/SceneResetRuntimeFactory.cs`
  - `Modules/SceneReset/Runtime/SceneResetFacade.cs`
  - `Modules/SceneReset/Runtime/SceneResetPipeline.cs`
  - `Modules/SceneReset/Runtime/Phases/*`
- expected outcome: host, factory e pipeline ficam mais previsiveis e menos acoplados a fallback.
- risk: medio.
- done criteria: o pipeline local opera com ownership claro e sem ambiguidade de start/cleanup.

### F5
- objective: eliminar duplicacao de source-of-truth entre SceneFlow, WorldReset e the local locator path.
- main files:
  - `Modules/SceneReset/Runtime/SceneResetControllerLocator.cs`
  - `Modules/SceneFlow/Navigation/Bindings/SceneRouteDefinitionAsset.cs`
  - `Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.WorldReset.cs`
- expected outcome: rota, target scene e decisao de reset passam a convergir em um unico contrato de resolucao.
- risk: medio/alto.
- done criteria: nao existem mais semanticas concorrentes para `targetScene` e `requiresWorldReset`.

### F6
- objective: limpar boundaries com `Gameplay` e `SimulationGate` sem reabrir ownership deles.
- main files:
  - `Infrastructure/SimulationGate/SimulationGateService.cs`
  - `Infrastructure/SimulationGate/Interop/GamePauseGateBridge.cs`
  - `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs`
  - `Modules/Gameplay/State/GameplayStateGate.cs`
  - `Modules/Gameplay/State/GameplayStateGateBindings.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.GameLoop.cs`
- expected outcome: `Gameplay` continua consumidor e `SimulationGate` continua owner da trava.
- risk: baixo/medio.
- done criteria: nenhum comportamento de reset depende de policy ou fallback do lado de gameplay.

## 10. Risks and Guardrails
- Risco de regressao de correlacao entre request, completion e gate.
- Risco de duplicar ou atrasar publication de lifecycle.
- Risco de introduzir fallback silencioso em vez de fail-fast.
- Risco de espalhar a policy de rota para fora de `SceneFlow`.
- Guardrails:
  - mudancas pequenas por fase
  - sem workarounds em modulo nao dono
  - sem fallback silencioso novo
  - preservar contratos e eventos canonicamente publicados
  - manter `SimulationGate` como owner unico da trava

## 11. Out of Scope
- Nao reabrir o stack macro estabilizado fora do reset.
- Nao alterar `Scripts/**` legado.
- Nao criar novo ADR.
- Nao reorganizar pastas agora.
- Nao expandir o plano para audit amplo fora de `WorldReset`, `SceneReset`, `ResetInterop` e boundaries minimos com `Gameplay`/`SimulationGate`.
- Nao fazer refatoracao ampla de `GameLoop`, `Navigation` ou `LevelFlow`.

## 12. Execution Checklist
- [ ] F1: contrato canonico explicito fechado
- [ ] F2: policy residual removida do bridge
- [ ] F3: `WorldReset` consolidado como owner do pipeline macro
- [ ] F4: pipeline local de `SceneReset` estabilizado
- [ ] F5: source-of-truths duplicadas removidas
- [ ] F6: boundaries com `Gameplay` e `SimulationGate` limpos
- [ ] `kind`, `targetScene`, `contextSignature`, `reason` e `origin` vindos de contrato unico
- [ ] `ResetInterop` sem policy propria
- [ ] `SceneFlow` como owner unico da policy de rota
- [ ] `SimulationGate` sem ownership de reset

## 13. Exit Condition
O plano pode ser considerado concluido quando:
- `WorldReset` for claramente o owner unico do reset macro
- `SceneReset` for claramente o owner do pipeline local
- `ResetInterop` for apenas bridge fina
- `SceneFlow` continuar sendo o owner da policy de rota
- `SimulationGate` continuar sendo o owner da trava
- `Gameplay` continuar apenas como consumidor de readiness/gate
- nao existir inferencia implicita de `kind`, `targetScene`, `contextSignature`, `reason` ou `origin` fora do contrato explicito
- nao houver mais policy residual em bridges/interops
- nao houver mais duplicacao relevante de source-of-truth no stack de reset
