# SA-19C1 — Host Thin Delegator / First Batch

Status: Applied / Pending compile + smoke
Date: 2026-06-15

## Scope

This batch keeps the work on the original SA-19 plan. It follows SA-19C0 and does not reopen B2/B3.

Included:

- Move `OnDisable` lifecycle validation from `SessionActivityHost` to `SessionActivityPipeline`.
- Remove the public `SessionActivityHost.Pipeline` surface.
- Add narrow read-only Host accessors for debug/QA observation:
  - `HasPipeline`
  - `GetCurrentActivityContentLoadedSet()`
  - `GetCurrentActivitySetupInventory()`
- Update `SessionActivityDebugPanel` to stop reading `host.Pipeline` / `host.Pipeline.EntryPipeline`.
- Extract QA command forwarding into `SessionActivityHostQaCommandSurface` while keeping Unity-facing Host wrapper methods for buttons/context menus.

Out of scope:

- No teardown changes.
- No SA-19B2 reopening.
- No SA-19B3 reopening.
- No reset policy changes.
- No Actor/Command/Projectile changes.
- No movement/camera/content release changes.
- No manager/coordinator/processor.

## Ownership matrix

| Concern | Before | After | Owner |
|---|---|---|---|
| Host disable lifecycle guard | `SessionActivityHost.OnDisable` decided | Host delegates validation to `SessionActivityPipeline.ValidateHostDisableOrFail` | `SessionActivityPipeline` |
| Public pipeline access | `SessionActivityHost.Pipeline` exposed full pipeline | Removed; debug panel uses narrow accessors | Host as debug boundary |
| Debug loaded set/inventory observation | DebugPanel used `host.Pipeline` / `EntryPipeline` | DebugPanel calls Host read-only accessors | Host debug surface |
| QA command forwarding | Host contained command plumbing/logging | `SessionActivityHostQaCommandSurface` handles QA forwarding | QA surface; pipeline still executes |
| Operational route-exit/readiness boundaries | Host delegates to pipeline | unchanged | Host boundary + Pipeline lifecycle |

## Anti-deslocamento answers

- Pipeline owner: `SessionActivityPipeline` remains macro lifecycle owner.
- Type: Host boundary + QA/debug surface narrowing; not new policy/lifecycle.
- Final or bridge: Host remains Unity boundary; QA surface is tooling boundary.
- Compatibility: Unity button wrappers remain only as UI compatibility; not lifecycle ownership.
- Symptom or boundary: boundary issue, because Host exposed full pipeline and decided disable guard locally.
- Duplicate owner: reduced; Host no longer decides disable lifecycle.
- Extraction effect: moves decision to canonical macro pipeline and narrows debug/QA access.
- Naming: `SessionActivityHostQaCommandSurface` is concrete and tooling-specific.
- No generic layer: no manager/coordinator/processor introduced.
- Evidence: requires canonical smoke after compile.

## Required smoke

- `RestartCurrentActivity`
- `Activity01ToActivity02`
- `RouteExitBackToMenu`

Minimum criteria:

- no `error CS`
- no `FATAL`
- no `Exception`
- no `route_transition_failed`
- no `checkpointStatus='Failed'`
- `ActivityParticipationExitStarted`
- `ActivityParticipationExited`
- `ActivityParticipationExitCompleted`
- `ActorLifetimeDecisionResolved`
- `ActivityRetainedParticipantLookupResolved`
- `ActivityParticipantActorMaterializationRetained`
- `ActivityEntryParticipantBindingCompleted`
- `ActivityEntryParticipantResetCompleted`
- `ActivityParticipantResetAppliedFromInventory`
- `ActivityContentReleaseCompleted`
- `ActorPresentationReleased`
- `ActorAttributeReleased`
- no improper `RejectedForeign`
- no improper `RejectedStale`

## Next if PASS

Proceed to `SA-19C2 — Lock Route-Exit Teardown Owner` audit or a short C1 follow-up only if compile/smoke exposes a direct regression.
