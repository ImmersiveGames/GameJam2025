# Changelog — Documentação (NewScripts)

Este changelog cobre **apenas** arquivos de documentação.

- Clarified: invariantes de concorrência (1 transição em voo), eventos do GameLoop context-free e CanPerform como helper não gate-aware; enforcement via IStateDependentService.
- Clarified: correlação por contexto é responsabilidade do Scene Flow/World Lifecycle + Coordinator; eventos do GameLoop permanecem context-free por design.
- Clarified: `CanPerform` não é gate-aware e não deve ser usado como autorização final; usar `IStateDependentService` (gate-aware).
- Updated: `ADR-0009-FadeSceneFlow.md` para refletir implementação validada do Fade + resolução de profile `startup` via `NewScriptsSceneTransitionProfile`.
- Updated: `WORLD_LIFECYCLE.md` com integração real via `WorldLifecycleRuntimeDriver` e regra de SKIP em `startup/menu`.
- Updated: `README.md`, `ARCHITECTURE.md`, `ARCHITECTURE_TECHNICAL.md`, `DECISIONS.md`, `EXAMPLES_BEST_PRACTICES.md`, `GLOSSARY.md` para:
    - remover referências obsoletas/truncadas,
    - alinhar nomenclatura (NewScriptsSceneTransitionProfile vs SceneTransitionProfile legado),
    - corrigir exemplos para respeitar `SceneTransitionContext` como `readonly struct`.

## [2025-12-24]
- Added: `ADR-0009-FadeSceneFlow.md` (primeira versão).
- Added: documentos consolidados em um pacote reduzido (README/ARCHITECTURE/TECHNICAL/WORLD_LIFECYCLE/DECISIONS/EXAMPLES/GLOSSARY).

## [2025-12-23]
- Added: resumo do pipeline de Scene Flow: `SceneTransitionService` → eventos Started/ScenesReady/Completed.
- Added: nota de integração com `GameReadinessService` e `WorldLifecycleRuntimeDriver` (SKIP no startup/menu).
