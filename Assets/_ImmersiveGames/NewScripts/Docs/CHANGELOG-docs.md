# Changelog — Documentação (NewScripts)

Este changelog cobre **apenas** arquivos de documentação.

## [2025-12-25]
- Added: `ADR-0010-LoadingHud-SceneFlow.md` (HUD de loading separado do Fade).
- Added: módulo de Loading HUD (scripts em `Infrastructure/SceneFlow/Loading/`).
- Updated: Scene Flow com evento `SceneTransitionBeforeFadeOutEvent` e emissão antes do FadeOut.
- Updated: Loading HUD com ordenação acima do Fade, pendências por assinatura e eventos de registro/desregistro.
- Added: registro de evolução do **Gameplay Reset** (`Gameplay/Reset/`) nos docs (targets + fases + contracts + DI por cena).
- Added: registro do **QA isolado** para validar reset por grupos (`GameplayResetQaSpawner` + `GameplayResetQaProbe`).
- Updated: `WORLD_LIFECYCLE.md`, `ARCHITECTURE.md`, `ARCHITECTURE_TECHNICAL.md`, `DECISIONS.md`, `EXAMPLES_BEST_PRACTICES.md`, `GLOSSARY.md`, `README.md` para refletir a integração **WorldLifecycle → Gameplay Reset** via `PlayersResetParticipant`.

- Clarified: invariantes de concorrência (1 transição em voo), eventos do GameLoop context-free e CanPerform como helper não gate-aware; enforcement via IStateDependentService.
- Clarified: correlação por contexto é responsabilidade do Scene Flow/World Lifecycle + Coordinator; eventos do GameLoop permanecem context-free por design.
- Clarified: `CanPerform` não é gate-aware e não deve ser usado como autorização final; usar `IStateDependentService` (gate-aware).
- Updated: `ADR-0009-FadeSceneFlow.md` para refletir implementação validada do Fade + resolução de profile `startup` via `NewScriptsSceneTransitionProfile`.
- Updated: `WORLD_LIFECYCLE.md` com integração real via `WorldLifecycleRuntimeCoordinator` e regra de SKIP em `startup/menu`.
- Updated: `README.md`, `ARCHITECTURE.md`, `ARCHITECTURE_TECHNICAL.md`, `DECISIONS.md`, `EXAMPLES_BEST_PRACTICES.md`, `GLOSSARY.md` para:
    - remover referências obsoletas/truncadas,
    - alinhar nomenclatura (NewScriptsSceneTransitionProfile vs SceneTransitionProfile legado),
    - corrigir exemplos para respeitar `SceneTransitionContext` como `readonly struct`.

## [2025-12-24]
- Added: `ADR-0009-FadeSceneFlow.md` (primeira versão).
- Added: documentos consolidados em um pacote reduzido (README/ARCHITECTURE/TECHNICAL/WORLD_LIFECYCLE/DECISIONS/EXAMPLES/GLOSSARY).

## [2025-12-23]
- Added: resumo do pipeline de Scene Flow: `SceneTransitionService` → eventos Started/ScenesReady/Completed.
- Added: nota de integração com `GameReadinessService` e `WorldLifecycleRuntimeCoordinator` (SKIP no startup/menu).
