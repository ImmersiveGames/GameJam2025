# Plan - SceneFlow Refoundation

Status: **Concluído** (2026-03-26)

## 1. Objective
Congelar um plano executável, incremental e rastreável para rebasear o módulo `SceneFlow` sem quebrar contratos externos, reduzindo drift arquitetural e estabilizando boundaries entre `SceneFlow`, `Navigation`, `LevelFlow`, `ResetInterop`, `Loading/Fade` e a infraestrutura de composição.

## 2. Source of Truth
- Auditoria base: `SceneFlow Architecture Audit`.
- ADRs canônicos: `Docs/ADRs/ADR-0030-Fronteiras-Canonicas-do-Stack-SceneFlow-Navigation-LevelFlow.md`, `Docs/ADRs/ADR-0031-Pipeline-Canonico-da-Transicao-Macro.md`, `Docs/ADRs/ADR-0032-Semantica-Canonica-de-Route-Level-Reset-e-Dedupe.md`, `Docs/ADRs/ADR-0033-Resiliencia-Canonica-de-Fade-e-Loading-no-Transito-Macro.md`.
- Docs modulares ativos: `Docs/Modules/SceneFlow.md`, `Docs/Modules/Navigation.md`, `Docs/Modules/LevelFlow.md`, `Docs/Modules/ResetInterop.md`, `Docs/Modules/WorldReset.md`.
- Regra de precedência: ADR-0030..0033 + docs modulares atuais prevalecem sobre histórico conflitante.

## 3. Current Diagnosis
- Drift de contrato entre política strict/degraded esperada e comportamento real.
- Ownership parcialmente errado concentrando composição técnica no core `SceneFlow`.
- Boundary ruim em interop de input e sync de `GameLoop`.
- Complexidade interna alta, com dedupe/concurrency demais no núcleo.

## 4. Contract To Preserve
- `SceneFlow` continua owner da timeline macro de transição.
- `set-active` permanece no trilho macro do `SceneFlow`.
- `Loading` e `Fade` permanecem apresentação, não orchestration.
- `load/unload` técnico converge para executor técnico dedicado.
- Eventos canônicos de transição permanecem estáveis.

## 5. Responsibilities Leaving SceneFlow

| responsibility | current owner/class | target module | rationale |
|---|---|---|---|
| Aplicação técnica detalhada de `load/unload/reload` macro | `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs` | `Infrastructure/SceneComposition` | Separar semântica macro da execução técnica. |
| Gate de preparo local acoplado ao pipeline `SceneFlow` | `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs` | `Modules/LevelFlow` | Preparo/clear é responsabilidade local de `LevelFlow`. |
| Sincronização de estado do `GameLoop` junto com input bridge | `Modules/SceneFlow/Interop/SceneFlowInputModeBridge.cs` | `Modules/GameLoop/Flow` + `Infrastructure/InputModes` | Separar request de input de controle de estado do loop. |

## 6. Responsibilities Staying In SceneFlow
- Sequenciamento macro da transição.
- Hidratação e validação macro de route/style/profile.
- Dedupe e controle de concorrência da transição macro.
- Emissão de eventos canônicos e correlação macro.
- Ponto de decisão do `set-active` no pipeline macro.

## 7. Target Modular Architecture
- Core `SceneFlow`: owner da semântica e timeline macro.
- Presentation: `Fade`/`Loading` como serviços visuais.
- Composition Executor: `Infrastructure/SceneComposition` como executor técnico.
- Reset Interop: ponte entre `SceneFlow` e `WorldReset`.
- Input Bridge: publicação de intenção, aplicação fora do bridge.
- Navigation/LevelFlow Interop: intent/route+style em `Navigation`, identidade local em `LevelFlow`.

## 8. Execution Checklist
- [x] F0 concluída
- [x] F1 concluída
- [x] F2 concluída
- [x] F3 concluída
- [x] F4 concluída
- [x] F5 concluída

## 9. Exit Condition
Este plano foi concluído quando F0..F5 estavam completas com contratos preservados, sem quebra da timeline macro, de `set-active`, dos eventos canônicos e das boundaries de apresentação.

## 10. Final Status (Outcome)
- `SceneFlow` permaneceu como owner da timeline macro.
- A composição técnica saiu do core macro.
- `Loading/Fade`, `GameLoop` sync e `LevelPrepare/Clear` ficaram no boundary correto.
- O lifecycle dos orchestrators de loading ficou explícito e previsível.
