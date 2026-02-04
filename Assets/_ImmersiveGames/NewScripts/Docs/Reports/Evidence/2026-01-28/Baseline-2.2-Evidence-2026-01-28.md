# Baseline 2.2 — Evidence Snapshot (2026-01-28)

Este snapshot consolida evidências do run capturado em 2026-01-28 para validação do Baseline 2.2.
Foco: SceneFlow + WorldLifecycle + InputMode + Level/ContentSwap/IntroStage.

## Ambiente
- NEWSCRIPTS_MODE: ativo
- Run: Boot → Menu (startup) → Gameplay (gameplay) → Level L01 (InPlace)

---

## A) Boot → Menu (startup) — Reset SKIP esperado (Frontend/Startup)

### Anchors
- SceneFlow TransitionStarted (startup)
- WorldLifecycle ResetRequested/ResetCompleted com reason=Skipped_StartupOrFrontend
- InputMode Applied FrontendMenu em SceneFlow/Completed:Frontend

### Evidência (linhas âncora)
- [SceneFlow] TransitionStarted ... profile='startup'
- [OBS][WorldLifecycle] ResetRequested ... reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'
- [OBS][WorldLifecycle] ResetCompleted ... reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'
- [OBS][InputMode] Applied ... reason='SceneFlow/Completed:Frontend'

**Resultado:** PASS

---

## B) Menu → Gameplay (gameplay) — Reset + Spawn + IntroStage → Playing

### Anchors
- SceneFlow TransitionStarted/ScenesReady/TransitionCompleted (gameplay)
- WorldLifecycle ResetRequested/ResetCompleted reason=SceneFlow/ScenesReady
- Spawn Player + Eater
- InputMode Applied Gameplay em SceneFlow/Completed:Gameplay
- IntroStageStarted (SceneFlow/Completed) → Complete (UIConfirm) → IntroStageCompleted → GameLoop Playing

### Evidência (linhas âncora)
- [SceneFlow] TransitionStarted ... profile='gameplay'
- [OBS][WorldLifecycle] ResetRequested ... reason='SceneFlow/ScenesReady'
- WorldLifecycleController Reset ... Spawn services ... Player/Eater spawned
- [OBS][WorldLifecycle] ResetCompleted ... reason='SceneFlow/ScenesReady'
- [OBS][InputMode] Applied mode='Gameplay' ... reason='SceneFlow/Completed:Gameplay'
- [OBS][IntroStage] IntroStageStarted ... reason='SceneFlow/Completed'
- [OBS][IntroStage] CompleteIntroStage received reason='IntroStage/UIConfirm'
- [OBS][IntroStage] IntroStageCompleted result='completed'
- [GameLoop] ENTER: Playing

**Resultado:** PASS

---

## C) Level L01 — InPlace ContentSwap → Reset direto → Commit → IntroStage

### Anchors
- LevelChangeRequested/Started (InPlace)
- ContentSwapRequested event=content_swap_inplace
- WorldLifecycle ResetRequested source=contentswap.inplace
- ContentSwapCommitted
- LevelStartPipeline -> IntroStage
- IntroStageStarted reason=LevelStart/Committed...
- IntroStageCompleted (AutoComplete QA)

### Evidência (linhas âncora)
- [QA][Level] L01 start ... mode=InPlace reason='QA/Levels/InPlace/DefaultIntroStage'
- [OBS][Level] LevelChangeRequested ... mode='InPlace'
- [OBS][ContentSwap] ContentSwapRequested ... event=content_swap_inplace mode=InPlace
- [OBS][WorldLifecycle] ResetRequested ... source='contentswap.inplace:content.2' reason='ProductionTrigger/contentswap.inplace:content.2'
- ContentSwapCommitted ... current='content.2 | content:level.1'
- [OBS][ContentSwap] LevelStartPipeline -> IntroStage ... reason='LevelStart/Committed|contentId=content.2|reason=QA/Levels/InPlace/DefaultIntroStage'
- [OBS][IntroStage] IntroStageStarted ... reason='LevelStart/Committed|contentId=content.2|reason=QA/Levels/InPlace/DefaultIntroStage'
- [OBS][IntroStage] CompleteIntroStage received reason='QA/Levels/AutoComplete/InPlace'
- [OBS][IntroStage] IntroStageCompleted result='completed'

**Resultado:** PASS (InPlace)

---

## Conclusão do Snapshot
- PASS: Boot→Menu (skip), Menu→Gameplay (reset+spawn+intro→playing), Level L01 InPlace pipeline completo.
