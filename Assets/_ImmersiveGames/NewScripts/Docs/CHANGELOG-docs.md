# Changelog (Docs)

All notable documentation changes to **NewScripts** are documented in this file.

## [2025-12-27]
- Added: `Docs/Reports/Legacy-Cleanup-Report.md` com inventário de referências residuais ao legado e plano de remoção.
- Added: `Docs/Reports/SceneFlow-Smoke-Result.md` com resultado do smoke test do SceneFlow (startup/menu → gameplay) incluindo logs essenciais.
- Added: `Docs/Reports/QA-Audit-2025-12-27.md` com auditoria dos QAs ativos/removidos e recomendações de baseline.

- Updated: `Docs/ADRs/ADR-0009-FadeSceneFlow.md` (Opção A) — Fade via cena aditiva (`FadeScene`) integrada ao SceneFlow.
- Updated: `Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md` (Opção A) — HUD de loading via cena aditiva (`LoadingHudScene`) integrada ao SceneFlow.
- Updated: `Docs/ARCHITECTURE_TECHNICAL.md` — consolidado o pipeline operacional (Fade + LoadingHUD) e os pontos de integração com SceneFlow/WorldLifecycle.
- Updated: `Docs/DECISIONS.md` — corrigida a referência de ADR-0010 (agora Loading HUD) e promovida a decisão de GameLoop events como “decisão” (não ADR).

## [2025-12-26]
- Updated: `Docs/WORLD_LIFECYCLE.md` com semântica de Reset-in-Place clarificada.
- Updated: `Docs/WORLDLIFECYCLE_RESET_STATUS.md` com status do progresso e pendências.
- Updated: `Docs/ARCHITECTURE.md` reorganizado para diferenciar "Visão" e "Operacional".
- Updated: `Docs/GLOSSARY.md` com termos do pipeline (SceneFlow, Gate, Reset, etc.).
- Added: `Docs/EXAMPLES_BEST_PRACTICES.md` com exemplos mínimos (hooks, spawn, gating) e padrões recomendados.
