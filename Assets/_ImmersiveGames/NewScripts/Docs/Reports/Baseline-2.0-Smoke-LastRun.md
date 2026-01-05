# Baseline 2.0 — Last Run Report

- GeneratedAt: `2026-01-05 13:52:33Z`
- Source: `C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Baseline-2.0-Smoke-LastRun.log`
- Spec: `C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Baseline-2.0-Spec.md`
- Result: **FAIL**

## Scenario Results

| Scenario | Title | Result | Missing (Hard) | Order Violations |
|---|---|---:|---:|---:|
| A | Boot → Menu (profile=startup, SKIP reset) | **FAIL** | 0 | 1 |
| B | Menu → Gameplay (profile=gameplay, reset + spawn) | **FAIL** | 0 | 1 |
| C | Pause → Resume (token state.pause) | **PASS** | 0 | 0 |
| D | PostGame (Defeat) → Restart → Gameplay | **PASS** | 0 | 0 |
| E | PostGame (Victory) → ExitToMenu (profile=frontend, SKIP reset) | **PASS** | 0 | 0 |

## Missing Soft (Summary)

| Scenario | Missing (Soft) |
|---|---:|
| A | 0 |
| B | 0 |
| C | 0 |
| D | 0 |
| E | 0 |

## Global Invariants

| Result | Missing (Hard) | Order Violations |
|---:|---:|---:|
| **FAIL** | 0 | 2 |

## Fail Reasons

### Global invariants
- missingHard=0, orderViolations=2

### Scenario failures
- `A`: missingHard=0, orderViolations=1
- `B`: missingHard=0, orderViolations=1

## Token Summary (Acquire/Release)

| Token | Acquire | Release |
|---|---:|---:|
| `flow.scene_transition` | 4 | 4 |
| `state.pause` | 1 | 1 |
| `state.postgame` | 2 | 2 |
| `WorldLifecycle.WorldReset` | 2 | 2 |

## Details

### Scenario A — FAIL

Boot → Menu (profile=startup, SKIP reset)

**Order violations:**
- Order violation: A.Order.ResetCompletedBeforeFadeOut (completed without started). before=`Emitting WorldLifecycleResetCompletedEvent.*profile='startup'`, after=`Completion gate conclu[ií]do\. Prosseguindo para FadeOut`

### Scenario B — FAIL

Menu → Gameplay (profile=gameplay, reset + spawn)

**Order violations:**
- Order violation: B.Order.ScenesReadyBeforeResetCompleted (started without completed). before=`SceneTransitionScenesReady`, after=`WorldLifecycleResetCompletedEvent`

### Scenario C — PASS

Pause → Resume (token state.pause)

No issues detected.

### Scenario D — PASS

PostGame (Defeat) → Restart → Gameplay

No issues detected.

### Scenario E — PASS

PostGame (Victory) → ExitToMenu (profile=frontend, SKIP reset)

No issues detected.

### Global Invariants

**Order violations:**
- Order violation: I.ScenesReadyBeforeCompleted (started without completed). before=`SceneTransitionScenesReady`, after=`\[Readiness\].*SceneTransitionCompleted`
- Order violation: I.ResetCompletedBeforeFadeOut (started without completed). before=`WorldLifecycleResetCompletedEvent|Reset SKIPPED \(startup/frontend\)`, after=`Completion gate conclu[ií]do\. Prosseguindo para FadeOut`

