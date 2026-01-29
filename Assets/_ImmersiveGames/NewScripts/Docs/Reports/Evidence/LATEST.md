# Evidence — LATEST

Último snapshot arquivado: **2026-01-29**

- Baseline 2.2 Evidence Snapshot:
    - `Evidence/2026-01-29/Baseline-2.2-Evidence-2026-01-29.md`

Notas:
- Snapshot 2026-01-29 confirma o pipeline completo para:
    - Boot→Menu (startup, reset skip)
    - Menu→Gameplay (reset + spawn + IntroStage → Playing)
    - ContentSwap in-place (G01) com reason canônico (`QA/ContentSwap/InPlace/NoVisuals`)
    - Pause/Resume e PostGame (Victory/Defeat) com gates/tokens e InputMode coerentes
    - Restart e ExitToMenu (reset determinístico / frontend reset skip)
