# Baseline 2.2 — Evidence — 2026-01-31

**Fonte:** trecho do log de runtime enviado em chat (31 Jan 2026).

## Escopo coberto neste arquivo

- **Startup / Frontend**: `NewBootstrap -> MenuScene + UIGlobalScene` (profile=`startup`) com **ResetWorld SKIP** (conforme contrato) e envelope de **Fade + LoadingHUD**.
- **Menu -> Gameplay**: `MenuScene -> GameplayScene + UIGlobalScene` (profile=`gameplay`) com **ResetWorld obrigatório**, spawn determinístico (Player + Eater), IntroStage e transição para Playing.

## Assinaturas-chave observadas (âncoras canônicas)

### SceneFlow
- `SceneTransitionService`:
  - `TransitionStarted`
  - `ScenesReady`
  - `TransitionCompleted`

### Fade (ADR-0009)
- `[OBS][Fade] FadeInStarted`
- `[OBS][Fade] FadeInCompleted`
- `[OBS][Fade] FadeOutStarted`
- `[OBS][Fade] FadeOutCompleted`

### Loading HUD (ADR-0010)
- `[LoadingHudController] Controller inicializado (CanvasGroup pronto)`
- `[OBS][LoadingHUD] EnsureReady`
- `[OBS][LoadingHUD] ShowApplied` (AfterFadeIn / ScenesReady)
- `[OBS][LoadingHUD] HideApplied` (BeforeFadeOut)

### WorldLifecycle (gating de SceneFlow)
- `[OBS][WorldLifecycle] ResetRequested` (gameplay)
- `[OBS][WorldLifecycle] ResetCompleted` (gameplay)
- `WorldLifecycleResetCompletionGate` recebendo/caching `WorldLifecycleResetCompletedEvent`

---

## Evidência — Startup (profile=startup)

```log
[SceneTransitionService] [SceneFlow] TransitionStarted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'
[INFO] [SceneTransitionService] [OBS][Fade] FadeInStarted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.
[VERBOSE] [LoadingHudController] [LoadingHUD] Controller inicializado (CanvasGroup pronto).
[VERBOSE] [LoadingHudService] [OBS][LoadingHUD] EnsureReady signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' controller='LoadingHudController'.
[INFO] [SceneTransitionService] [OBS][Fade] FadeInCompleted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.
[VERBOSE] [LoadingHudService] [OBS][LoadingHUD] ShowApplied signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' phase='AfterFadeIn'.
...
[INFO] [SceneTransitionService] [SceneFlow] ScenesReady id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.
[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [WorldLifecycle] ResetWorld SKIP (profile != gameplay). signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', profile='startup', targetScene='MenuScene'.
[VERBOSE] [LoadingHudService] [OBS][LoadingHUD] HideApplied signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' phase='BeforeFadeOut'.
[INFO] [SceneTransitionService] [OBS][Fade] FadeOutStarted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.
[INFO] [SceneTransitionService] [OBS][Fade] FadeOutCompleted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.
[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted id=1 signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap' profile='startup'.
```

**Conclusão (startup):** LoadingHUD apareceu **após FadeIn**, manteve-se durante a fase de load/ScenesReady e foi ocultado **antes do FadeOut**, sem erros Strict. ResetWorld foi corretamente **SKIP** por profile não-gameplay.

---

## Evidência — Menu → Gameplay (profile=gameplay)

```log
[GameNavigationService] [Navigation] NavigateAsync -> routeId='to-gameplay', reason='Menu/PlayButton', Load=[GameplayScene, UIGlobalScene], Unload=[MenuScene], Active='GameplayScene', UseFade=True, Profile='gameplay'.
[SceneTransitionService] [SceneFlow] TransitionStarted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'
[INFO] [SceneTransitionService] [OBS][Fade] FadeInStarted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.
[INFO] [SceneTransitionService] [OBS][Fade] FadeInCompleted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.
[VERBOSE] [LoadingHudService] [OBS][LoadingHUD] ShowApplied signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' phase='AfterFadeIn'.
...
[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetRequested signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'.
[INFO] [WorldLifecycleController] Reset iniciado. reason='SceneFlow/ScenesReady', scene='GameplayScene'.
[INFO] [SceneBootstrapper] Spawn services registered from definition: 2
[INFO] [PlayerSpawnService] Actor spawned: ... (prefab=Player_NewScripts, scene=GameplayScene)
[INFO] [EaterSpawnService] Actor spawned: ... (prefab=Eater_NewScripts, scene=GameplayScene)
[VERBOSE] [WorldLifecycleSceneFlowResetDriver] [OBS][WorldLifecycle] ResetCompleted signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay' target='GameplayScene' reason='SceneFlow/ScenesReady'.
[INFO] [SceneTransitionService] [SceneFlow] ScenesReady id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.
[VERBOSE] [LoadingHudService] [OBS][LoadingHUD] HideApplied signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' phase='BeforeFadeOut'.
[INFO] [SceneTransitionService] [OBS][Fade] FadeOutStarted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.
[INFO] [SceneTransitionService] [OBS][Fade] FadeOutCompleted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.
[INFO] [SceneTransitionService] [SceneFlow] TransitionCompleted id=2 signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene' profile='gameplay'.
...
[INFO] [IntroStageCoordinator] [OBS][IntroStage] IntroStageStarted ... reason='SceneFlow/Completed'.
[INFO] [IntroStageControlService] [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' ...
[INFO] [IntroStageCoordinator] [OBS][IntroStage] GameplaySimulationUnblocked token='sim.gameplay' ...
[VERBOSE] [GameLoopService] [GameLoop] ENTER: Playing (active=True)
```

**Conclusão (gameplay):** o envelope SceneFlow + Fade + LoadingHUD funciona no caminho de gameplay; `ResetWorld` dispara em `ScenesReady` e completa com spawn determinístico (Player/Eater). LoadingHUD é ocultado antes do FadeOut, e o fluxo segue para IntroStage e Playing.
