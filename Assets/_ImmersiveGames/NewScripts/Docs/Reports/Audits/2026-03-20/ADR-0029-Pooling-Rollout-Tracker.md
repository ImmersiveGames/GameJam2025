# ADR-0029 Pooling Rollout Tracker (Packages A+B+C+D)

- Date: 2026-03-20
- Scope: `Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/**`
- Reference ADR: `Docs/ADRs/ADR-0029-Canonical-Pooling-In-NewScripts.md`
- Tracker status: CLOSED (Rollout COMPLETE)

## 1) Baseline real (repo state at rollout start)

- ADR-0029 remained **Accepted / Complete / PASS** as architectural contract.
- At rollout start, the repository snapshot did not have a usable canonical pooling implementation under:
  - `Infrastructure/Pooling/Contracts/**`
  - `Infrastructure/Pooling/Config/**`
  - `Infrastructure/Pooling/Runtime/**`
- Conclusion:
  - contract validity stayed intact;
  - implementation had to be reconstructed incrementally in this rollout.

## 2) Final status by package

| Package | Goal | Final Status | Evidence |
|---|---|---|---|
| A (F0/F1/F2) | Baseline + structure + bootstrap/DI | DONE | `Infrastructure/Pooling/**`, `GlobalCompositionRoot.Pipeline.cs` |
| B | Runtime core (ensure/prewarm/rent/return/expand/max-limit/cleanup) | DONE | `PoolService.cs`, `GameObjectPool.cs`, `PoolRuntimeHost.cs`, `PoolRuntimeInstance.cs` |
| C | Standalone QA harness (ContextMenu / Play Mode) | DONE | `Infrastructure/Pooling/QA/**` |
| D | `autoReturnSeconds` runtime + QA validation flow reuse | DONE | `PoolAutoReturnTracker.cs`, `GameObjectPool.cs`, `PoolingQaContextMenuDriver.cs` |

## 3) Rollout close-out validation (manual Play Mode)

Validated as PASS for:

- ensure/register by `PoolDefinitionAsset`
- prewarm
- rent
- manual return
- controlled expansion + explicit max-limit failure
- cleanup/shutdown
- auto-return (`autoReturnSeconds`)
- auto-return cancellation on manual return
- auto-return cancellation on cleanup
- multiple simultaneous rented instances with auto-return
- QA harness operation via ContextMenu

## 4) Final QA harness observability alignment

- Residual observability gap was closed in QA driver:
  - local rented cache reconciliation now updates return counters when instances auto-return.
  - driver logs/snapshots now stay coherent with runtime state after timer-based returns.
- Scope remained harness-only (no structural runtime refactor).

## 5) Guardrails final check

- Identity by `PoolDefinitionAsset` reference (no string structural key).
- Ownership remains in `GlobalCompositionRoot`.
- No `PersistentSingleton`.
- No `RuntimeInitializeOnLoadMethod`.
- No `Resources.Load`.
- No coupling with Audio/Gameplay/UI/VFX/Actor/spawner domains.

## 6) Final capability snapshot

Canonical pooling in `Infrastructure/Pooling/**` is operational for:

- ensure/register
- prewarm
- rent
- return (manual and auto-return)
- controlled expansion with max limit and explicit failure at ceiling
- cleanup/shutdown
- standalone QA harness validation via ContextMenu

## 7) Rollout closure and handoff

- ADR-0029 rollout is formally closed as COMPLETE.
- Module is ready for consumption by other modules through canonical global bootstrap/DI (no alternate bootstrap required).
- Next project step after this closure: resume Audio roadmap at ADR-0028 / F3 (`IAudioBgmService` runtime).

## 8) Post-close operational improvement (2026-03-20)

- Final canonical strategy for prewarm is consumer-driven with explicit asset intent:
  - `PoolDefinitionAsset.prewarm` declares whether the pool should be prewarmed.
  - `PoolService.EnsureRegistered(...)` is ensure-only (register/create/idempotent), with no hidden prewarm.
  - `PoolConsumerBehaviourBase` (reusable consumer base in namespace `Infrastructure.Pooling.Interop`) resolves `IPoolService` and runs:
    - `EnsureRegistered(definition)` for every explicit dependency
    - `Prewarm(definition)` only when `definition.prewarm == true`
  - `PoolingQaContextMenuDriver` now inherits from that base directly (no complementary `MonoBehaviour` required in the same GameObject); base + QA implementation are colocated in `Infrastructure/Pooling/QA/PoolingQaContextMenuDriver.cs`.
- Global boot prewarm list strategy remains removed:
  - no `BootstrapConfigAsset.bootPoolDefinitions`
  - no pool prewarm loop in `GlobalCompositionRoot` boot stage
- Operational guardrails remain unchanged:
  - no global asset scan
  - no `Resources.Load`
  - no domain coupling

## 9) Documentary close-out (2026-03-20)

- Final module documentation is now available in `Docs/Guides/**`:
  - `Docs/Guides/Pooling-How-To.md` (technical view + operational how-to)
  - `Docs/Guides/Pooling-Quick-Access.html` (fast practical reference)
- Both docs reflect the accepted final shape:
  - `PoolDefinitionAsset.prewarm` declares intent
  - consumer-side reusable base applies ensure + conditional prewarm
  - no global pool boot list
  - no hidden prewarm inside `EnsureRegistered(...)`
- Rollout status remains `CLOSED / COMPLETE` for ADR-0029 scope.
- Next natural project step remains Audio roadmap: ADR-0028 / F3 (`IAudioBgmService` runtime).
