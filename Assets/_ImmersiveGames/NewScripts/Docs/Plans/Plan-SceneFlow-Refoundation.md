# Plan — SceneFlow Refoundation

Status: **Concluído** (2026-03-25)

## 1. Objective
Congelar um plano executável, incremental e rastreável para rebasear o módulo SceneFlow sem quebrar contratos externos, reduzindo drift arquitetural, clarificando ownership e estabilizando boundaries entre SceneFlow, Navigation, LevelFlow, ResetInterop, Loading/Fade e infraestrutura de composição.

## 2. Source of Truth
- Auditoria base: **SceneFlow Architecture Audit** (mais recente).
- ADRs canônicos: `Docs/ADRs/ADR-0030-Fronteiras-Canonicas-do-Stack-SceneFlow-Navigation-LevelFlow.md`, `Docs/ADRs/ADR-0031-Pipeline-Canonico-da-Transicao-Macro.md`, `Docs/ADRs/ADR-0032-Semantica-Canonica-de-Route-Level-Reset-e-Dedupe.md`, `Docs/ADRs/ADR-0033-Resiliencia-Canonica-de-Fade-e-Loading-no-Transito-Macro.md`.
- Docs modulares ativos: `Docs/Modules/SceneFlow.md`, `Docs/Modules/Navigation.md`, `Docs/Modules/LevelFlow.md`, `Docs/Modules/ResetInterop.md`, `Docs/Modules/WorldReset.md`.
- Regra de precedência: ADR-0030..0033 + docs modulares atuais **prevalecem** sobre histórico conflitante (`Docs/ADRs/Obsolete/**` e baseline antiga).

## 3. Current Diagnosis
- **Drift de contrato**: inconsistência entre política strict/degraded esperada e comportamento real (principalmente Loading/Gate).
- **Ownership parcialmente errado**: SceneFlow ainda concentra parte da execução técnica de composição de cenas.
- **Boundary ruim**: acoplamento cruzado em interop de InputModes/GameLoop e gate local dentro do trilho macro.
- **Complexidade interna**: classes grandes, múltiplos pontos de dedupe/concurrency e resolução tardia via service locator.

## 4. Contract To Preserve
- SceneFlow continua owner da **timeline macro** de transição.
- `set-active` permanece no trilho macro do SceneFlow.
- Loading e Fade permanecem **apresentação**, não orchestration.
- `load/unload` técnico deve convergir para executor técnico dedicado (`SceneComposition`).
- Eventos canônicos de transição permanecem estáveis:
  - `SceneTransitionStartedEvent`
  - `SceneTransitionFadeInCompletedEvent`
  - `SceneTransitionScenesReadyEvent`
  - `SceneTransitionBeforeFadeOutEvent`
  - `SceneTransitionCompletedEvent`

## 5. Responsibilities Leaving SceneFlow

| responsibility | current owner/class | target module | rationale |
|---|---|---|---|
| Aplicação técnica detalhada de `load/unload/reload` macro | `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | `Infrastructure/SceneComposition` | Separar semântica macro da execução técnica e reduzir complexidade do core SceneFlow. |
| Gate de preparo local (`LevelPrepare/Clear`) acoplado ao pipeline SceneFlow | `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs` | `Modules/LevelFlow` (interop/gate adapter) | Preparo/clear é responsabilidade local de LevelFlow, não do core macro. |
| Sincronização de estado do GameLoop junto com input bridge | `Modules/SceneFlow/Interop/SceneFlowInputModeBridge.cs` | `Modules/GameLoop/Flow` + `Infrastructure/InputModes` | Separar request de input de controle de estado do loop. |

## 6. Responsibilities Staying In SceneFlow
- Sequenciamento macro da transição (start -> fade -> route apply -> ready -> gate -> fade out -> completed).
- Hidratação e validação macro de route/style/profile.
- Dedupe e controle de concorrência da transição macro.
- Emissão de eventos canônicos e assinatura de correlação macro.
- Ponto de decisão do `set-active` no pipeline macro.

## 7. Target Modular Architecture
- **Core SceneFlow**: owner da semântica e timeline macro; sem loops técnicos pesados de composição.
- **Presentation**: Fade/Loading como serviços visuais consumidos pelo pipeline; sem ownership de rota/reset/prepare.
- **Composition Executor**: `Infrastructure/SceneComposition` como executor técnico comum (`Macro` e `Local`).
- **Reset Interop**: `ResetInterop` mantém trigger/gate/correlação entre SceneFlow e WorldReset.
- **Input Bridge**: bridge de SceneFlow publica intenção de input; aplicação em `InputModes`; sync de loop fora do bridge.
- **Navigation/LevelFlow Interop**: Navigation mantém intent/route+style; LevelFlow mantém identidade local, prepare/clear e swap local.

## 8. Phased Execution Plan

### F0
- objective: congelar contrato operacional e matriz de ownership.
- main files:
  - `Docs/Modules/SceneFlow.md`
  - `Docs/Modules/ResetInterop.md`
  - `Docs/ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md` (alinhamento de referências)
- expected outcome: leitura única e sem ambiguidade de ownership/policy.
- risk: baixo.
- done criteria: conflitos documentais resolvidos com precedência explícita para ADR-0030..0033.

### F1
- objective: retirar de SceneFlow a aplicação técnica detalhada de composição macro.
- main files:
  - `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
  - `Modules/SceneFlow/Transition/Runtime/RouteSceneCompositionRequestFactory.cs`
  - `Infrastructure/SceneComposition/SceneCompositionExecutor.cs`
- expected outcome: SceneFlow orquestra; executor técnico aplica plano.
- risk: médio.
- done criteria: `SceneTransitionService` sem loops técnicos de load/unload/reload por cena.

### F2
- objective: alinhar strict/degraded de Loading/Fade com ADR-0033.
- main files:
  - `Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs`
  - `Modules/SceneFlow/Transition/Adapters/SceneFlowFadeAdapter.cs`
- expected outcome: falha estrutural obrigatória fail-fast; degradação só para falha operacional.
- risk: médio.
- done criteria: caminhos estruturais inválidos não seguem em modo strict.

### F3
- objective: separar input bridge de sync de GameLoop.
- main files:
  - `Modules/SceneFlow/Interop/SceneFlowInputModeBridge.cs`
  - `Modules/GameLoop/Flow/GameLoopSceneFlowSyncCoordinator.cs`
  - `Infrastructure/InputModes/Runtime/InputModeCoordinator.cs`
- expected outcome: bridge de SceneFlow fica focado em input intent.
- risk: médio.
- done criteria: `SceneFlowInputModeBridge` não altera estado de GameLoop diretamente.

### F4
- objective: mover gate de LevelPrepare para boundary LevelFlow.
- main files:
  - `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.LevelFlow.cs`
- expected outcome: SceneFlow consome contrato de gate composto sem policy local interna.
- risk: médio.
- done criteria: responsabilidade de prepare/clear encapsulada no lado LevelFlow.

### F5
- objective: reduzir hotspots e ajustar lifecycle dos orquestradores/event bindings.
- main files:
  - `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs`
  - `Modules/SceneFlow/Loading/Runtime/LoadingProgressOrchestrator.cs`
  - `Infrastructure/Composition/GlobalCompositionRoot.Events.cs`
- expected outcome: menor risco de duplicação/retenção de bindings e maior previsibilidade de runtime.
- risk: baixo/médio.
- done criteria: lifecycle explícito e verificável para componentes event-driven críticos.

## 9. Risks and Guardrails
- Risco de churn alto em classe central (`SceneTransitionService`) durante extração de composição técnica.
- Risco de regressão de ordem temporal do pipeline macro (fade/loading/gates/events).
- Risco de regressão de correlação (`ContextSignature`) entre SceneFlow, ResetInterop e WorldReset.
- Guardrails:
  - sem refatoração ampla fora do ownership definido neste plano;
  - mudanças pequenas por fase, com critérios de pronto explícitos;
  - preservar contratos externos e eventos canônicos;
  - evitar workaround em módulo não-dono.

## 10. Out of Scope
- Refatoração ampla de `Modules/SceneReset/**`.
- Reescrita da state machine central de `GameLoop/Core/**`.
- Expansão de escopo para módulos não auditados como relevantes.
- Criação de novo ADR durante execução deste plano.
- Reorganização de pastas fora do necessário para boundaries definidos.

## 11. Execution Checklist
- [x] F0 concluída (contrato documental e ownership congelados)
- [x] F1 concluída (execução técnica macro fora do core SceneFlow)
- [x] F2 concluída (strict/degraded alinhado a ADR-0033)
- [x] F3 concluída (input bridge separado de GameLoop sync)
- [x] F4 concluída (gate local movido para boundary LevelFlow)
- [x] F5 concluída (hotspots/lifecycle estabilizados)

## 12. Exit Condition
Este plano é considerado concluído quando F0..F5 estiverem completas com critérios de pronto atendidos, sem quebra dos contratos preservados (timeline macro, `set-active`, eventos canônicos e boundaries de apresentação), e com ownership final aderente ao baseline canônico ADR-0030..0033.

## 13. Final Status (Outcome)
- SceneFlow mantido como owner da timeline macro.
- Composição técnica removida do core macro e consolidada no executor técnico.
- Policy strict/degraded de Loading/Fade alinhada ao baseline canônico.
- Sync de GameLoop removido do input bridge e concentrado no coordinator de GameLoop.
- LevelPrepare/Clear movido para o boundary de LevelFlow (SceneFlow consome gate composto).
- Lifecycle dos orchestrators de Loading tornado explícito (registro/desregistro/rebind previsíveis).
