# Baseline 2.1 - Evidence Snapshot (2026-01-18)

## Status
- Date: 2026-01-18
- Source of truth: Unity Console (manual copy)
- Scope: PostGame (Victory/Defeat), Restart (Boot cycle), ExitToMenu, SceneFlow + WorldLifecycle completion gate

## Artifacts
- Console mirror: Logs/Baseline-2.1-ConsoleLog-2026-01-18.log
- SHA-256: 2a3c1228c2344308a22542067c9836e65122bad68ef39eb37e8eab31dee04f36

## What this snapshot covers
- Victory -> PostGame overlay -> Restart -> SceneFlow profile=gameplay -> WorldLifecycle reset -> IntroStage -> Playing
- Defeat (Timeout) -> PostGame overlay -> ExitToMenu -> SceneFlow profile=frontend (no reset)

## Key evidence strings (anchors)
- RequestEnd(Victory, reason='Gameplay/DevManualVictory')
- Publicando GameRunEndedEvent. Outcome=Victory
- RestartRequested -> RequestReset (expect Boot cycle). reason='PostGame/Restart'
- NavigateAsync -> routeId='to-gameplay' ... Profile='gameplay'
- [WorldLifecycle] Disparando ResetWorld ... reason='SceneFlow/ScenesReady'
- Reset concluido. reason='SceneFlow/ScenesReady'
- IntroStageStarted ... reason='SceneFlow/Completed'
- RequestEnd(Defeat, reason='Gameplay/Timeout')
- ExitToMenu recebido -> RequestMenuAsync ... Profile='frontend'
- [WorldLifecycle] ScenesReady ignorado (profile != gameplay)
