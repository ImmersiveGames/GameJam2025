# Plan - Macro Flow Stack Consolidation

Status: **Concluído** (2026-03-26)

## 1. Objective
Consolidar ownership e boundaries do stack macro entre `SceneFlow`, `LevelFlow`, `Navigation`, `ResetInterop`, `GameLoop` e seus bridges/coordinators, preservando os contratos já estabilizados após a refoundation de `SceneFlow`.

## 2. Source of Truth
- Auditoria macro mais recente do stack de fluxo.
- Código atual dos módulos:
  - `Modules/SceneFlow/**`
  - `Modules/LevelFlow/**`
  - `Modules/Navigation/**`
  - `Modules/ResetInterop/**`
  - `Modules/GameLoop/**`
- Composition roots ligados ao boot/start, gameplay start, restart, postgame e exit-to-menu.
- Regra de precedência: refoundation de `SceneFlow` e cleanup de `Navigation` prevalecem sobre backlog histórico.

## 3. Current Diagnosis
- `SceneFlow` já está corretamente posicionado como owner da timeline macro.
- `LevelFlow` ainda dividia a verdade entre snapshot, seleção de level e gate macro.
- `Navigation` ainda participava de decisões de execução.
- `ResetInterop` ainda carregava policy de integração.
- `GameLoop` ainda recebia sync macro de forma distribuída.
- Bridges/coordinators ainda carregavam ordering e fallback além da adaptação.

## 4. Macro Contracts To Preserve
- `SceneFlow` continua owner da timeline macro de transição.
- `LevelFlow` continua owner do level lifecycle, seleção local, prepare/clear e restart semântico.
- `Navigation` continua owner de intent, catalog/route resolution e dispatch macro.
- `ResetInterop` continua como ponte fina entre `SceneFlow` e `WorldReset`.
- `GameLoop` continua como owner da state machine e dos sinais de lifecycle.
- `set-active` permanece no trilho macro do `SceneFlow`.
- `load/unload` técnico continua fora do core de orquestração macro.
- Eventos canônicos atuais não devem ser quebrados.

## 5. Ownership Matrix

| Concern | Current owner | Target owner | Notes |
|---|---|---|---|
| Route resolution | `Navigation` + `SceneFlow` | `Navigation` | `SceneFlow` consome rota resolvida. |
| Dispatch de navegação | `GameNavigationService` | `Navigation` | O dispatch deve ser responsabilidade do módulo. |
| Gameplay start | `LevelFlowRuntimeService` + `Navigation` | `LevelFlow` decide, `Navigation` despacha | `LevelFlow` origina a intenção. |
| Level selection/default | `LevelMacroPrepareService` | `LevelFlow` | Owner correto. |
| Level prepare/clear | `LevelMacroPrepareService` + gate composto | `LevelFlow` | `SceneFlow` apenas consome o gate. |
| Reset request/completion | `SceneFlowWorldResetDriver` + `WorldResetService` | `ResetInterop` como ponte | Policy de domínio não deve ficar no driver. |
| Intro stage start | `LevelStageOrchestrator` + `IntroStageCoordinator` | `GameLoop` como executor final, `LevelFlow` como origem de contexto | O orchestration deve ser mínimo. |
| Postgame actions | `PostLevelActionsService` + `GameLoopService` | `LevelFlow` + `GameLoop` | `LevelFlow` decide ações. |
| Restart context | `RestartContextService` | `LevelFlow` | Fonte única para snapshot de restart. |
| Input mode sync | `SceneFlowInputModeBridge` | `Infrastructure/InputModes` como aplicador | `SceneFlow` só emite intenção. |
| Game loop sync | `GameLoopSceneFlowSyncCoordinator` | `GameLoop` | Coordinator deve reduzir policy. |

## 6. Target Architecture
- `SceneFlow`: owner da timeline macro, eventos canônicos e sequencing.
- `LevelFlow`: owner do lifecycle local de level, snapshot, prepare/clear e ações pós-level.
- `Navigation`: owner da semântica de intent, resolução de rota e dispatch macro.
- `ResetInterop`: ponte fina entre `SceneFlow` e `WorldReset`.
- `GameLoop`: owner da state machine, dos sinais de readiness e das transições de estado.
- Bridges/coordinators: adaptadores finos, com policy mínima.

## 7. Execution Checklist
- [x] Auditoria macro base tomada como referência.
- [x] Ownership atual consolidado por módulo.
- [x] Gaps de source-of-truth mapeados.
- [x] Bridges/coordinators com policy excessiva identificados.
- [x] Fases P0..P5 executadas com critérios de pronto explícitos.
- [x] Contratos de macro flow estabilizados sem drift residual relevante.

## 8. Exit Condition
O plano foi concluído: `SceneFlow`, `LevelFlow`, `Navigation`, `ResetInterop` e `GameLoop` ficaram com ownership mais claramente separado, e o fluxo macro preserva os contratos canônicos sem reabrir o escopo.

## 9. Final Status (Outcome)
- `SceneFlow` ficou como owner da timeline macro.
- `Navigation`, `LevelFlow`, `ResetInterop` e `GameLoop` ficaram mais claramente separados por boundary.
- Bridges e coordinators foram reduzidos ao papel de adaptação.
- O stack macro foi congelado como base estabilizada para a próxima camada.
