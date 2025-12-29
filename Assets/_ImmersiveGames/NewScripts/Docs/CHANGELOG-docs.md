# Changelog (Docs)

All notable documentation changes to **NewScripts** are documented in this file.

## [2025-12-31]
- Updated: `GameLoop.md` para documentar o estado `PostPlay`, os eventos `GameRunStartedEvent` / `GameRunEndedEvent` /
  `GameLoopActivityChangedEvent` e o serviço `IGameRunStatusService` no fluxo de pós-game.
- Updated: `WORLD_LIFECYCLE.md` alinhado ao fluxo de run/resultados via GameLoop.

## [2025-12-28]
- Added: suporte a `ActorKind.Eater` na GameplayScene (EaterSpawnService + WorldDefinition) documentado como parte do reset hard de produção.
- Added: `NewEaterRandomMovementController` documentado como integrado ao `IStateDependentService` para `ActionType.Move` (respeita GameLoop/SimulationGate/Pause).
- Added: `WorldLifecycleMultiActorSpawnQa` documentado para validar Player + Eater no `IActorRegistry` após reset da GameplayScene.
- Updated: `WORLD_LIFECYCLE.md` e `ADR-0011-WorldDefinition-MultiActor-GameplayScene.md` com extensão de multi-actor spawn na GameplayScene.
- Updated: docs para reforçar `GameLoopSceneFlowCoordinator` como fonte única de `RequestStart()` e que navigation não emite start.
- Updated: debug tools/QA triggers marcados como dev-only na documentação e relatório de validação do SceneFlow.
- Added: `Reports/SceneFlow-Production-Validation-2025-12-28.md` com checklist do fluxo de produção e evidência mínima de logs.
- Added: `Reports/SceneFlow-Gameplay-To-Menu-Report.md` com checklist e logs esperados do retorno Gameplay → Menu.
- Updated: `Infrastructure/Navigation/ExitToMenuNavigationBridge.cs`, `Infrastructure/GlobalBootstrap.cs` e `Gameplay/Navigation/GameplayExitToMenuDebugTrigger.cs` para suportar ExitToMenu em produção/dev.
- Added: `Reports/SceneFlow-Gameplay-Blockers-Report.md` com os 3 blockers do fluxo Menu → Gameplay (erros, causa raiz, correções e evidências).
- Updated: `Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs` e `Infrastructure/WorldLifecycle/Spawn/PlayerSpawnService.cs` (fixes de blockers do fluxo).
- Updated: `README.md` e `WORLD_LIFECYCLE.md` com explicação simples do pipeline, definição de “loading real”
  e critério para remover o SKIP (decisão registrada).
- Updated: `ADRs/ADR-0010-LoadingHud-SceneFlow.md` e `ARCHITECTURE_TECHNICAL.md` com formalização de
  reset/spawn como parte do loading e diretrizes futuras para Addressables (tarefas agregadas).
- Updated: `WORLDLIFECYCLE_RESET_STATUS.md` com status macro em escala 0–100 e referência a Addressables (planejado).
- Fixed: remoção de artefatos de truncation/scan (‘...’) em docs (sem mudança de comportamento).

## [2025-12-30]
- Updated: caminhos de QA deprecated consolidados em `QA/Deprecated` e referências de documentação ajustadas.
- Updated: cenas de produção/QA limpas de referências diretas aos tools de QA deprecated.

## [2025-12-29]
- Added: bridge `GameResetRequestedEvent` → `RestartNavigationBridge` → reset oficial via SceneFlow/WorldLifecycle.
- Updated: `GameLoop.md` e `WORLD_LIFECYCLE.md` com o fluxo de pós-game (Restart/Menu).
- Added: `Reports/QA-GameplayResetKind.md` com passos e critérios para validar GameplayReset por ActorKind no Player real.
- Updated: `Reports/QA-Audit-2025-12-27.md` com referência ao `GameplayResetPhaseLogger`.
- Updated: QA report com nota sobre gating de probes/logger em Editor/Dev.
- Updated: `Docs/QA/GameplayReset-QA.md` com notas sobre DI do classifier e `verboseLogs`.
- Added: QA Eater (`GameplayResetKindQaEaterActor`) e spawn opcional no `GameplayResetKindQaSpawner` para validar `EaterOnly`.
- Added: `Reports/QA-GameplayReset-RequestMatrix.md` com evidências da validação da matriz GameplayResetRequest.

## [2025-12-27]
- Added: `Reports/Legacy-Cleanup-Report.md` com inventário de referências residuais ao legado e plano de remoção.
- Added: `Reports/SceneFlow-Smoke-Result.md` com resultado do smoke test do SceneFlow (startup/menu → gameplay) incluindo logs essenciais.
- Added: `Reports/QA-Audit-2025-12-27.md` com auditoria dos QAs ativos/removidos e recomendações de baseline.

- Updated: docs: convert navigation references to Markdown links + cleanup placeholders.
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
