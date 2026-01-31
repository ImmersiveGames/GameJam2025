# Baseline 2.2 — Evidence (LATEST)

**Canonical evidence snapshot:** `2026-01-29`  
See: `Docs/Reports/Evidence/2026-01-29/Baseline-2.2-Evidence-2026-01-29.md`

This snapshot covers:

- Boot → Menu (startup) with frontend **SKIP** reset
- Menu → Gameplay with **ResetWorld + ResetCompleted + spawn** (Player + Eater)
- IntroStage blocks `sim.gameplay` and completes via `IntroStage/UIConfirm` → Playing
- QA ContentSwap in-place (`QA/ContentSwap/InPlace/NoVisuals`)
- Pause/Resume (`state.pause` token + InputMode `PauseOverlay`)
- PostGame flows: Victory/Defeat, Restart (`PostGame/Restart`), ExitToMenu (`PostGame/ExitToMenu`) with frontend **SKIP** reset

If you update log signatures / reasons, create a new dated snapshot folder under `Docs/Reports/Evidence/` and update this file.
