# ADR-0029 Pooling Rollout Tracker (Package A)

- Date: 2026-03-20
- Scope: `Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/**`
- Reference ADR: `Docs/ADRs/ADR-0029-Canonical-Pooling-In-NewScripts.md`
- Tracker status: OPEN (Package A complete; Package B next)

## 1) Baseline real (repo state at rollout start)

- ADR-0029 remains **Accepted / Complete / PASS** as architectural contract.
- Current repository state before this rollout had **no usable canonical pooling module** in:
  - `Infrastructure/Pooling/Contracts/**`
  - `Infrastructure/Pooling/Config/**`
  - `Infrastructure/Pooling/Runtime/**`
- Conclusion:
  - Contract is valid.
  - Implementation availability in this repo snapshot needed reconstruction from zero.

## 2) Phase status (Package A only)

| Phase | Name | Goal | Status | Evidence |
|---|---|---|---|---|
| F0 | Baseline + tracker | Freeze real baseline, guardrails, stop/go, residual risks | DONE | This tracker |
| F1 | Structure + contracts | Create canonical shape and freeze base contracts/config | DONE | `Infrastructure/Pooling/**` created |
| F2 | Bootstrap + DI base | Wire pooling stage in global pipeline and register `IPoolService` | DONE | `GlobalCompositionRoot` updated |

## 3) Guardrails (mandatory)

- Pooling stays in `Infrastructure/Pooling/**` as shared infrastructure.
- Ownership remains in `GlobalCompositionRoot`.
- Structural identity is always `PoolDefinitionAsset` reference (never string).
- No `PersistentSingleton`.
- No `RuntimeInitializeOnLoadMethod` as pooling owner.
- No `Resources.Load`.
- No domain coupling (`Gameplay`, `Audio`, `UI`, `VFX`, `Actor`, `spawner`).
- No hardcoded world positioning rule (`y = 0` or equivalent).
- Package A must not claim full runtime delivery.

## 4) Stop/Go criteria

### GO criteria for Package A completion

- Tracker exists and documents baseline discrepancy explicitly.
- Canonical folders exist: `Contracts`, `Config`, `Runtime`.
- Base artifacts exist:
  - `IPoolService`
  - `IPoolableObject`
  - `PooledBehaviour`
  - `PoolDefinitionAsset` with required fields:
    - `prefab`
    - `initialSize`
    - `canExpand`
    - `maxSize`
    - `autoReturnSeconds`
    - `poolLabel`
- Global pipeline includes `Pooling` stage.
- `IPoolService` is registered in global DI.
- Canonical boot logs for pooling are present in composition/service code.

### STOP criteria

- Any identity keyed by string for canonical pool registration.
- Any structural dependency on domain modules.
- Any hidden bootstrap outside `GlobalCompositionRoot`.
- Any implementation that implies runtime completeness not delivered in Package A.
- Compilation blockers introduced by Package A changes.

## 5) Residual risks after Package A

- Runtime behavior is intentionally incomplete (`prewarm`, `rent`, `return`, expansion and auto-return execution still Package B).
- Consumers calling runtime operations now receive explicit "Package B" failure by design.
- No standalone scene/mock validation executed in this package (kept for Package B validation phase).
- Additional runtime internals (`GameObjectPool`, host/instance lifecycle, auto-return timing) are placeholders only.

## 6) Explicit handoff to Package B

- Handoff status: Package A concluido; Package B e o proximo passo do rollout.
- Implement operational runtime for:
  - pool creation and internal storage
  - prewarm
  - rent
  - return
  - controlled expansion with max ceiling
  - optional auto-return timer behavior
  - shutdown cleanup
- Add standalone validation flow (mock object + controlled scenario) as required by ADR-0029.
- Keep contract and ownership frozen as delivered in Package A.
