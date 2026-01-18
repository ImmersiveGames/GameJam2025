# Baseline 2.1 — Evidência — 2026-01-18

## Status

**PASS (evidência consolidada via log do Console informado no chat)**

## Artefatos

- Log (recorte): `../Logs/Baseline-2.1-Smoke-2026-01-18.log`
- Hash (SHA-256) do log: `b95c31490a048eceb5d630f2fb484b56e7887aacf204b4c46d83383e9bbf6be9`

## Contexto do run

- Boot → Menu com `Profile='startup'`.
- Menu → Gameplay com `Profile='gameplay'` (reset + spawn).
- IntroStage bloqueia (`sim.gameplay`) e depois libera.
- Pause/Resume usa token `state.pause`.
- Reset in-place via `Gameplay/HotkeyR`.
- ExitToMenu com `Profile='frontend'` (reset skip).

## Invariantes observadas (âncoras)

### INV-00 — Evidência de Profile (fonte: SceneFlow)

```log
[SceneFlow] TransitionStarted ... signature='p:startup|...' profile='startup'
[SceneFlow] TransitionStarted ... signature='p:gameplay|...' profile='gameplay'
[SceneFlow] TransitionStarted ... signature='p:frontend|...' profile='frontend'
```

### INV-01 — Startup → Menu: Reset deve ser SKIP (profile != gameplay)

```log
[WorldLifecycleSceneFlowResetDriver] ... ScenesReady ignorado (profile != gameplay) ... profile='startup'.
```

### INV-02 — Gameplay: ScenesReady dispara ResetWorld e completa

```log
[WorldLifecycleSceneFlowResetDriver] ... Disparando ResetWorld ... targetScene='GameplayScene'.
[WorldLifecycleController] Reset iniciado. reason='SceneFlow/ScenesReady', scene='GameplayScene'.
[WorldLifecycleController] Reset concluído. reason='SceneFlow/ScenesReady', scene='GameplayScene'.
[WorldLifecycleResetCompletionGate] ... WorldLifecycleResetCompletedEvent recebido ... reason='SceneFlow/ScenesReady'.
```

### INV-03 — Spawn determinístico: Player + Eater

```log
[PlayerSpawnService] Actor spawned: ... Player_NewScripts ... scene=GameplayScene
[EaterSpawnService] Actor spawned: ... Eater_NewScripts ... scene=GameplayScene
```

### INV-04 — IntroStage: bloqueio e liberação do gameplay (token sim.gameplay)

```log
[OBS][IntroStage] GameplaySimulationBlocked token='sim.gameplay' ... reason='SceneFlow/Completed'.
[OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm' ...
[OBS][IntroStage] GameplaySimulationUnblocked token='sim.gameplay' ...
```

### INV-05 — Pause/Resume: token state.pause

```log
[Gate] Acquire token='state.pause'. Active=1. IsOpen=False
[Gate] Release token='state.pause'. Active=0. IsOpen=True
```

### INV-06 — Reset in-place: reason Gameplay/HotkeyR

```log
[WorldResetRequestService] ... RequestResetAsync → ResetWorldAsync. source='Gameplay/HotkeyR', scene='GameplayScene'.
[WorldLifecycleController] Reset iniciado. reason='Gameplay/HotkeyR', scene='GameplayScene'.
[WorldLifecycleController] Reset concluído. reason='Gameplay/HotkeyR', scene='GameplayScene'.
```

### INV-07 — ExitToMenu: profile frontend e reset SKIP

```log
[GameNavigationService] ... NavigateAsync ... Profile='frontend'.
[WorldLifecycleSceneFlowResetDriver] ... ScenesReady ignorado (profile != gameplay) ... profile='frontend'.
```

## Nota

- Fonte de verdade: **Console**. O arquivo de log é um recorte manual, contendo as linhas usadas nas âncoras.
