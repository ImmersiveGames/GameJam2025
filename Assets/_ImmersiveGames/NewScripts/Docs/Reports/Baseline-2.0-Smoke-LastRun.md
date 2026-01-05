# Baseline 2.0 — Last Run Report

- GeneratedAt: `2026-01-05 01:55:21Z`
- Source: `C:/Projetos/GameJam2025/Assets\_ImmersiveGames/NewScripts/Docs/Reports\Baseline-2.0-Smoke-LastRun.log`
- Result: **PASS**

## Scenario Results

| Scenario | Title | Result | Missing (Hard) | Order Violations |
|---|---|---:|---:|---:|
| A | Boot → Menu (profile=startup, SKIP reset) | **PASS** | 0 | 0 |
| B | Menu → Gameplay (profile=gameplay, reset + spawn) | **PASS** | 0 | 0 |
| C | Pause → Resume (Gameplay, token state.pause) | **PASS** | 0 | 0 |
| D | PostGame (Defeat) → Restart → Gameplay novamente | **PASS** | 0 | 0 |
| E | PostGame (Victory) → ExitToMenu (profile=frontend, SKIP reset) | **PASS** | 0 | 0 |

## Token Summary (Acquire/Release)

| Token | Acquire | Release |
|---|---:|---:|
| `flow.scene_transition` | 4 | 4 |
| `state.pause` | 1 | 1 |
| `state.postgame` | 2 | 2 |
| `WorldLifecycle.WorldReset` | 2 | 2 |

## Details

### Scenario A — PASS

Boot → Menu (profile=startup, SKIP reset)

No issues detected.

### Scenario B — PASS

Menu → Gameplay (profile=gameplay, reset + spawn)

No issues detected.

### Scenario C — PASS

Pause → Resume (Gameplay, token state.pause)

No issues detected.

### Scenario D — PASS

PostGame (Defeat) → Restart → Gameplay novamente

No issues detected.

### Scenario E — PASS

PostGame (Victory) → ExitToMenu (profile=frontend, SKIP reset)

No issues detected.

