# Baseline 2.1 - Contract Verification (2026-01-18)

## Method
Manual verification against console mirror (parser/tool considered non-canonical for this snapshot).
Source log: ../Logs/Baseline-2.1-ConsoleLog-2026-01-18.log

## Result
PASS (manual).

## Checks
- SceneFlow Started acquires gate token 'flow.scene_transition' (readiness NOT READY).
- For gameplay profile: ScenesReady triggers WorldLifecycle reset and publishes completion gate before FadeOut.
- Restart: PostGame intent -> Boot cycle -> NavigateAsync(to-gameplay) -> ScenesReady -> ResetWorld -> TransitionCompleted.
- ExitToMenu: PostGame intent -> NavigateAsync(to-menu) with profile=frontend; WorldLifecycle reset driver ignores ScenesReady (profile != gameplay).
- No dependency on tool-generated log extraction; evidence is the console mirror above.

## Notes
- The SceneFlow signature changes between profiles (gameplay vs frontend); this is expected and used for gating.
