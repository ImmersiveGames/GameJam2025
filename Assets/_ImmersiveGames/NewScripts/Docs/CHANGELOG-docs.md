# Changelog (Docs)

All notable documentation changes to **NewScripts** are documented in this file.

## [2025-12-27]
- Added: `Reports/Legacy-Cleanup-Report.md` com inventário de referências residuais ao legado e plano de remoção.
- Added: `Reports/SceneFlow-Smoke-Result.md` com resultado do smoke test do SceneFlow (startup/menu → gameplay) incluindo logs essenciais.
- Added: `Reports/QA-Audit-2025-12-27.md` com auditoria dos QAs ativos/removidos e recomendações de baseline.

- Updated: `ADRs/ADR-0009-FadeSceneFlow.md` (Opção A) — Fade via cena aditiva (`FadeScene`) integrada ao SceneFlow.
- Updated: `ADRs/ADR-0010-LoadingHud-SceneFlow.md` (Opção A) — HUD de loading via cena aditiva (`LoadingHudScene`) integrada ao SceneFlow.
- Updated: `ARCHITECTURE_TECHNICAL.md` — consolidado o pipeline operacional (Fade + LoadingHUD) e os pontos de integração com SceneFlow/WorldLifecycle.
- Updated: `DECISIONS.md` — corrigida a referência de ADR-0010 (agora Loading HUD) e promovida a decisão de GameLoop events como “decisão” (não ADR).

## [2025-12-26]
- Updated: `WORLD_LIFECYCLE.md` com semântica de Reset-in-Place clarificada.
- Updated: `WORLDLIFECYCLE_RESET_STATUS.md` com status do progresso e pendências.
- Updated: `ARCHITECTURE.md` reorganizado para diferenciar "Visão" e "Operacional".
- Updated: `GLOSSARY.md` com termos do pipeline (SceneFlow, Gate, Reset, etc.).
- Added: `EXAMPLES_BEST_PRACTICES.md` com exemplos mínimos (hooks, spawn, gating) e padrões recomendados.
