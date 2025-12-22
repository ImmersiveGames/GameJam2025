## [2025-12-20]
- Added: Seção de pause em `WorldLifecycle/WorldLifecycle.md` descrevendo bloqueio de ações via gate sem congelar física/timeScale, agora explícito que o fluxo é GamePauseGateBridge → SimulationGateTokens.Pause → NewScriptsStateDependentService (serviço oficial) bloqueando Move.

## [2025-12-19]
- Added: Baseline Audit for `ResetScope.Players` documenting As-Is state, identified subsystems, and gaps prior to gameplay integration.

## Documentação — Changelog de Normalização

- Moved: referência de pipeline/ordenção de hooks de `ARCHITECTURE.md` (resumo) → `WorldLifecycle/WorldLifecycle.md` (owner operacional já existente; arquitetura mantém o link de resumo)
- Moved: semântica detalhada de fases/passos/reset de `ADR/ADR-ciclo-de-vida-jogo.md` → `WorldLifecycle/WorldLifecycle.md` (referenciado com links explícitos)
- Moved: explicações do pipeline na checklist de QA `QA/WorldLifecycle-Baseline-Checklist.md` → referências diretas para `WorldLifecycle/WorldLifecycle.md`
- Removed (duplicate): detalhes operacionais em ADR-0001 substituídos por referência ao contrato em `WorldLifecycle/WorldLifecycle.md`
- Updated links: `DECISIONS.md`, `ARCHITECTURE.md`, `Guides/UTILS-SYSTEMS-GUIDE.md`, `ADR/ADR.md`, `README.md`, `ADR/ADR-0001-NewScripts-Migracao-Legado.md`, `QA/WorldLifecycle-Baseline-Checklist.md`, `ADR/ADR-ciclo-de-vida-jogo.md`
- No functional change (documentação apenas)
