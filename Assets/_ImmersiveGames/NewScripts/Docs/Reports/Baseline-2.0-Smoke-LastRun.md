# Baseline 2.0 — Smoke Run (Editor PlayMode)

- Result: **FAIL**
- Duration: `22,78s`
- Last signature seen: `p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap`
- Last profile seen: `startup`

## Failure reason

- Timeout aguardando estado estável de Menu (SceneTransitionCompleted + GameLoop Ready).

## Evidências de navegação (Menu → Gameplay)

- Menu SceneTransitionCompleted observado: `False`
- GameLoop Ready observado: `True`
- IGameNavigationService resolvido: `<null>`
- NavigateAsync(to-gameplay) logado: `False`
- SceneTransition Started (gameplay) observado: `False`
- SceneTransition Completed (gameplay) observado: `False`

## Token balance (Acquire vs Release)

- flow.scene_transition: `1` vs `1`
- WorldLifecycle.WorldReset: `0` vs `0`
- state.pause: `0` vs `0`
- state.postgame: `0` vs `0`

## Artifacts

- Raw log: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.log`
- Report: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.md`

