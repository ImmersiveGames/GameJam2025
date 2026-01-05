# Baseline 2.0 — Checklist (Operacional)

**Data da última validação:** 2026-01-05  
**Fonte de verdade:** `Reports/Baseline-2.0-Smoke-LastRun.log` (validação manual)  
**Spec canônica:** [Baseline 2.0 — Spec](Baseline-2.0-Spec.md)

## Resumo
- Validação manual devido à instabilidade do parser (`Baseline2SmokeLastRunTool`).
- O log cobre: **Startup → Menu → Gameplay → Pause/Resume → Victory → Restart → Defeat → ExitToMenu → Menu**.

## Checklist por cenário (A–E)

| ID | Cenário | Evidências principais (resumo) | Status |
|---|---|---|---|
| A | Boot → Menu (startup) | Gate `flow.scene_transition` fecha/abre; `Reset SKIPPED` + `WorldLifecycleResetCompletedEvent`; completion gate antes do FadeOut | PASS |
| B | Menu → Gameplay (gameplay) | `ScenesReady` → hard reset → `WorldLifecycleResetCompletedEvent`; spawn 2 atores (`ActorRegistry count at 'After Spawn': 2`) | PASS |
| C | Pause → Resume | `Acquire`/`Release` de `state.pause` com gate coerente | PASS |
| D | PostGame (Defeat) → Restart | `Outcome=Defeat` → `NavigateAsync ... Profile='gameplay'` → reset completo | PASS |
| E | PostGame (Victory) → ExitToMenu | `Outcome=Victory` → `NavigateAsync ... Profile='frontend'` → `Reset SKIPPED` + `ResetCompleted` | PASS |

## Invariantes globais

| Invariante | Requisito | Status |
|---|---|---|
| I1 | `SceneTransitionStarted` adquire `flow.scene_transition` e `SceneTransitionCompleted` libera | PASS |
| I2 | `ScenesReady` ocorre antes de `Completed` | PASS |
| I3 | `ResetCompleted` existe para toda transição (inclusive SKIP) | PASS |
| I4 | Completion gate é aguardado antes do `FadeOut` | PASS |
| I5 | Correlação usa `ContextSignature` canônica | PASS |

## Ordem Fade/Loading (UseFade=true)
- `FadeIn` → `LoadingHUD.Show` → Load/Unload → `ScenesReady` → `ResetCompleted` → `LoadingHUD.Hide` → `FadeOut` → `Completed`.

## Dívida aceita
- `Baseline2SmokeLastRunTool` permanece **não-bloqueante**; o log é a evidência oficial até o tool estabilizar.
