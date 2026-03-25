# Plan — Macro Flow Stack Consolidation

## 1. Objective
Consolidar ownership e boundaries do stack macro entre `SceneFlow`, `LevelFlow`, `Navigation`, `ResetInterop`, `GameLoop` e seus bridges/coordinators, preservando os contratos já estabilizados após a refoundation de `SceneFlow` e o cleanup curto de `Navigation`.

O foco deste plano é reduzir policy distribuída, eliminar drift residual e tornar explícito quem decide, quem despacha e quem apenas adapta.

## 2. Source of Truth
- Auditoria macro mais recente do stack de fluxo.
- Código atual dos módulos:
  - `Modules/SceneFlow/**`
  - `Modules/LevelFlow/**`
  - `Modules/Navigation/**`
  - `Modules/ResetInterop/**`
  - `Modules/GameLoop/**`
- Composition roots diretamente ligados ao boot/start, gameplay start, restart, postgame e exit-to-menu.
- Regra de precedência:
  - `SceneFlow` refoundations e `Navigation` cleanup curto prevalecem sobre backlog histórico.
  - Não reabrir refactors locais já resolvidos.

## 3. Current Diagnosis
- `SceneFlow` já está corretamente posicionado como owner da timeline macro, mas ainda concentra observabilidade, dedupe e fallback operacional dentro do service central.
- `LevelFlow` está mais coeso, porém ainda divide a verdade entre snapshot, seleção de level, ações pós-level e gate macro.
- `Navigation` é o owner correto de intent/dispatch, mas ainda participa de decisões de execução e integração com restart/exit.
- `ResetInterop` funciona como ponte, mas ainda carrega policy de integração e fallback publish.
- `GameLoop` é owner da state machine, porém ainda recebe sync macro de forma distribuída por bridges/coordinators.
- Bridges/coordinators do fluxo macro ainda carregam ordering, coalescing, dedupe e fallback além do papel de adaptação.

## 4. Macro Contracts To Preserve
- `SceneFlow` continua owner da timeline macro de transição.
- `LevelFlow` continua owner do level lifecycle, seleção local, prepare/clear e restart semântico.
- `Navigation` continua owner de intent, catalog/route resolution e dispatch macro.
- `ResetInterop` continua como ponte fina entre `SceneFlow` e `WorldReset`.
- `GameLoop` continua como owner da state machine e dos sinais de lifecycle.
- `set-active` permanece no trilho macro do `SceneFlow`.
- `load/unload` técnico continua fora do core de orquestração macro.
- `reason`, `signature` e `context` devem ser propagados sem reinterpretação por bridges.
- Eventos canônicos atuais não devem ser quebrados.

## 5. Ownership Matrix

| Concern | Current owner | Target owner | Notes |
|---|---|---|---|
| Route resolution | `Navigation` + `SceneFlow` | `Navigation` | `SceneFlow` consome route resolvida, não redefine a rota. |
| Dispatch de navegação | `GameNavigationService` | `Navigation` | O dispatch deve ser a única responsabilidade do módulo. |
| Gameplay start | `LevelFlowRuntimeService` + `Navigation` | `LevelFlow` decide, `Navigation` despacha | `LevelFlow` origina a intenção; `Navigation` executa. |
| Level selection/default | `LevelMacroPrepareService` | `LevelFlow` | Owner correto. |
| Level prepare/clear | `LevelMacroPrepareService` + gate composto | `LevelFlow` | `SceneFlow` apenas consome o gate. |
| Reset request/completion | `SceneFlowWorldResetDriver` + `WorldResetService` | `ResetInterop` como ponte | Policy de domínio não deve ficar no driver. |
| Intro stage start | `LevelStageOrchestrator` + `IntroStageCoordinator` | `GameLoop` como executor final, `LevelFlow` como origem de contexto | O orchestration deve ser mínimo. |
| Postgame actions | `PostLevelActionsService` + `GameLoopService` | `LevelFlow` + `GameLoop` | `LevelFlow` decide ações; `GameLoop` mantém estado/outcome. |
| Restart context | `RestartContextService` | `LevelFlow` | Fonte única para snapshot de restart. |
| Input mode sync | `SceneFlowInputModeBridge` | `Infrastructure/InputModes` como aplicador | `SceneFlow` só emite intenção. |
| Game loop sync | `GameLoopSceneFlowSyncCoordinator` | `GameLoop` | Coordinator deve reduzir policy. |
| Readiness / gameplay gate | `SceneFlowWorldResetDriver`, `MacroLevelPrepareCompletionGate` | `SceneFlow` timeline + adapters externos | Gate externo não deve virar owner de policy. |
| reason/signature/context | Vários módulos | Contrato canônico por domínio | Evitar normalização paralela em bridges. |

## 6. Source-of-Truth Gaps
- `RestartContextService` e seus consumidores ainda dividem a verdade do snapshot.
- `Navigation` e `SceneFlow` ainda compartilham entendimento de rota e estado de execução.
- `LevelFlow` usa múltiplos serviços para compor o mesmo contexto de gameplay.
- `ResetInterop` decide fallback/skip em vez de apenas transportar e reportar.
- `GameLoop` e `SceneFlow` ainda compartilham a verdade do start via `GameStartRequestedEvent`, `RequestStart()` e sync coordinator.

## 7. Bridges/Coordinators Carrying Too Much Policy
- `GameLoopSceneFlowSyncCoordinator`
  - Faz matching de assinatura, fallback de sync e decisão de `Ready/Resume`.
- `SceneFlowWorldResetDriver`
  - Resolve policy de reset, dedupe e fallback publish.
- `ExitToMenuCoordinator`
  - Faz gating, coalescing e sequência operacional de saída.
- `MacroRestartCoordinator`
  - Define ordem canônica do restart, debounce e orquestração multi-serviço.
- `LevelStageOrchestrator`
  - Decide intro-stage, dedupe por versão/signature e fallback de `RequestStart`.
- `SceneFlowInputModeBridge`
  - Ainda contém dedupe e decisão de sync, além de adaptação.

## 8. Target Architecture
- `SceneFlow`
  - Owner da timeline macro, eventos canônicos e sequencing de transição.
  - Sem policy de domínio externo e sem execução técnica pesada de composição.
- `LevelFlow`
  - Owner do lifecycle local de level, snapshot, prepare/clear e ações pós-level.
- `Navigation`
  - Owner da semântica de intent, resolução de rota e dispatch macro.
- `ResetInterop`
  - Ponte fina entre `SceneFlow` e `WorldReset`, com fallback mínimo e observabilidade.
- `GameLoop`
  - Owner da state machine, dos sinais de readiness e das transições de estado.
- Bridges/coordinators
  - Devem tender a adaptadores finos, com policy mínima e sem fonte própria de verdade.

## 9. Phased Execution Plan

### P0
- objective: congelar o contrato macro canônico e o mapa de ownership.
- main files:
  - `Docs/Plans/Plan-MacroFlow-Stack-Consolidation.md`
  - `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
- expected outcome: leitura única de ordem de bootstrap e ownership macro.
- risk: baixo.
- done criteria: contrato macro documentado sem ambiguidade sobre quem inicia, despacha e sincroniza.

### P1
- objective: reduzir policy no trilho de start e gameplay boot.
- main files:
  - `Modules/GameLoop/Flow/GameLoopSceneFlowSyncCoordinator.cs`
  - `Modules/GameLoop/Input/GameLoopStartRequestEmitter.cs`
  - `Modules/GameLoop/IntroStage/Runtime/IntroStageCoordinator.cs`
- expected outcome: start do run e intro-stage ficam mais próximos da state machine e menos do bridge.
- risk: médio.
- done criteria: decisão de start/ready fica concentrada no owner do `GameLoop`, com bridges apenas traduzindo eventos.

### P2
- objective: enxugar o handoff SceneFlow ↔ ResetInterop ↔ WorldReset.
- main files:
  - `Modules/ResetInterop/Runtime/SceneFlowWorldResetDriver.cs`
  - `Modules/ResetInterop/Runtime/WorldResetCompletionGate.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.WorldReset.cs`
- expected outcome: driver vira ponte fina e policy de reset fica explícita.
- risk: médio.
- done criteria: fallback/skip e dedupe ficam minimizados e previsíveis, sem decisão de domínio espalhada.

### P3
- objective: concentrar o lifecycle de level e o contexto de restart em `LevelFlow`.
- main files:
  - `Modules/LevelFlow/Runtime/RestartContextService.cs`
  - `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs`
  - `Modules/LevelFlow/Runtime/PostLevelActionsService.cs`
  - `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs`
- expected outcome: snapshot, seleção, prepare/clear e postgame ficam alinhados ao mesmo boundary.
- risk: médio.
- done criteria: o contexto canônico de restart não é reconstituído fora de `LevelFlow`.

### P4
- objective: estreitar `Navigation` para intent/dispatch e retirar policy residual de execução.
- main files:
  - `Modules/Navigation/GameNavigationService.cs`
  - `Modules/Navigation/Runtime/ExitToMenuCoordinator.cs`
  - `Modules/Navigation/Runtime/MacroRestartCoordinator.cs`
- expected outcome: `Navigation` permanece owner do dispatch, sem virar orquestrador macro de mais.
- risk: médio.
- done criteria: restart e exit-to-menu continuam funcionando, mas com menos lógica distribuída em coordinators.

### P5
- objective: revisar bridges restantes e remover policy duplicada de sync/observabilidade.
- main files:
  - `Modules/SceneFlow/Interop/SceneFlowInputModeBridge.cs`
  - `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs`
  - `Modules/SceneFlow/Loading/Runtime/LoadingProgressOrchestrator.cs`
  - `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
- expected outcome: bridges ficam finos, com dedupe e observabilidade reduzidos ao mínimo necessário.
- risk: baixo/médio.
- done criteria: adapters só adaptam; policy e source-of-truth não ficam duplicadas.

## 10. Risks and Guardrails
- Risco de regressão temporal no pipeline macro ao mexer em ordering de bootstrap.
- Risco de quebrar `reason`, `signature` e `context` ao reduzir policy distribuída.
- Risco de reintroduzir acoplamento entre `SceneFlow`, `Navigation` e `GameLoop` via bridges.
- Guardrails:
  - mudanças pequenas por fase;
  - preservar contratos externos e eventos canônicos;
  - evitar workaround em módulo não-dono;
  - fail-fast para dependências obrigatórias;
  - não reabrir refactors locais já estabilizados.

## 11. Out of Scope
- Reescrita ampla de `SceneTransitionService`.
- Split estrutural de `GameNavigationCatalogAsset`.
- Refatoração do core da state machine de `GameLoop`.
- Mudanças em `Scripts` legado.
- Novos ADRs.
- Reorganização de pastas.
- Cleanup de binders de frontend fora do fluxo macro.

## 12. Execution Checklist
- [x] Auditoria macro base tomada como referência.
- [x] Ownership atual consolidado por módulo.
- [x] Gaps de source-of-truth mapeados.
- [x] Bridges/coordinators com policy excessiva identificados.
- [x] Fases P0..P5 executadas com critérios de pronto explícitos.
- [x] Contratos de macro flow estabilizados sem drift residual relevante.

## 13. Exit Condition
O plano foi concluído: `SceneFlow`, `LevelFlow`, `Navigation`, `ResetInterop` e `GameLoop` ficaram com ownership mais claramente separado, bridges/coordinators ficaram mais finos e o fluxo macro preserva os contratos canônicos sem reabrir o escopo macro.
