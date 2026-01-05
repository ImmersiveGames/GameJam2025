# Baseline 2.0 — Checklist (Last Run)

**Date:** 2026-01-05  
**Source of truth:** Baseline 2.0 smoke log (manual validation; parser tool considered unreliable)  
**Docs that define the contract:**  
- `Assets/_ImmersiveGames/NewScripts/Docs/WORLD_LIFECYCLE.md`  
- `Assets/_ImmersiveGames/NewScripts/Docs/CHANGELOG-docs.md`  

---

## 1) Evidence signatures (contract)

### 1.1 Scene transition (events + gating)

- `SceneTransitionStartedEvent(SceneTransitionContext)`
  - **Must** acquire gate token: `flow.scene_transition`
- `SceneTransitionScenesReadyEvent(SceneTransitionContext)`
  - **Must** occur before `SceneTransitionCompletedEvent`
- `SceneTransitionCompletedEvent(SceneTransitionContext)`
  - **Must** release gate token: `flow.scene_transition`

### 1.2 Completion gate (FadeOut safety)

- `WorldLifecycleResetCompletionGate`
  - **Waits** for `WorldLifecycleResetCompletedEvent(contextSignature, reason)`
  - **Timeout:** 20.000 ms
  - **Correlation key:** `SceneTransitionContext.ContextSignature` (canonical)

### 1.3 World reset policy (runtime)

- On `ScenesReady`:
  - profile `startup` / `frontend` → **SKIP** (still emits ResetCompleted)
  - profile `gameplay` → **HARD reset** + spawn
- `WorldLifecycleResetCompletedEvent` is emitted **for every transition**, including SKIP.

### 1.4 Fade + Loading HUD ordering (when `UseFade=true`)

- `FadeIn` (await)
- `LoadingHudScene` **Show**
- Load/Unload scenes
- `ScenesReady`
- (runtime) `ResetCompleted` (SKIP or HARD)
- `LoadingHudScene` **Hide**
- `FadeOut`

**When `UseFade=false`:**
- Loading HUD can be shown on `Started` and hidden on `Completed` (fallback path).

---

## 2) Baseline Matrix (A–E)

> Status meanings: **PASS** = evidences present and ordering respected in the smoke log.

| ID | Scenario | Expected behavior | Required evidences | Status |
|---|---|---|---|---|
| A | Boot → Menu (startup) | **SKIP** reset; still completes gate | Started acquires `flow.scene_transition` → (UseFade path) FadeIn + LoadingHUD show → ScenesReady → **Reset SKIPPED** → `WorldLifecycleResetCompletedEvent(signature, reason=Skipped_StartupOrFrontend...)` → Completion gate resolves → LoadingHUD hide → FadeOut → Completed releases gate | PASS |
| B | Menu → Gameplay (profile=gameplay) | **HARD reset** after ScenesReady; baseline spawn (Player+Eater) | ScenesReady → runtime triggers hard reset → `WorldLifecycleResetCompletedEvent(signature, reason=ScenesReady/GameplayScene)` → `ActorRegistry count at 'After Spawn': 2` → Completion gate resolves → Completed | PASS |
| C | Pause → Resume | Gate coherence and deterministic unblock | Acquire `state.pause` token on pause; release on resume; gate snapshots reflect closure/opening; no transition deadlocks | PASS |
| D | Gameplay → PostGame (Victory/Defeat) | End-of-run is idempotent | Only one `GameRunEndedEvent` per run; `state.postgame` token lifecycle coherent; restart/exit actions do not duplicate termination | PASS (manual) |
| E | PostGame → Restart / ExitToMenu | Restart → gameplay reset; ExitToMenu → frontend SKIP | Restart: request gameplay + hard reset cycle completes. ExitToMenu: request menu (frontend) + SKIP reason + ResetCompleted | PASS |

---

## 3) Global invariants

| Invariant | Requirement | Status |
|---|---|---|
| I1 | `Started` acquires `flow.scene_transition` and `Completed` releases it | PASS |
| I2 | `ScenesReady` happens before `Completed` | PASS |
| I3 | `ResetCompleted` exists for every transition (including SKIP) | PASS |
| I4 | `SceneTransitionService` awaits completion gate before `FadeOut` | PASS |
| I5 | Correlation uses canonical `ContextSignature` (no ad-hoc signatures) | PASS |

---

## 4) Accepted debt

- `Baseline2SmokeLastRunTool` remains **non-blocking**; the log is the authoritative evidence until the tool is stabilized.
