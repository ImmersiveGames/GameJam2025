# Changelog (Docs)

## [2026-01-03]
- Added: `Reports/Baseline-Audit-2026-01-03.md` com matriz de evidência (código + QA/logs) e status de validação.
- Updated: `README.md` com report master, baseline audit e alvo `ByActorKind` na lista de targets.
- Updated: `WORLD_LIFECYCLE.md` com assinatura canônica (`ContextSignature`) e ordem de LoadingHUD por `UseFade`.
- Updated: `WORLDLIFECYCLE_RESET_STATUS.md` com status atual dos targets e correção de assinatura.
- Updated: `ADRs/ADR-0014-GameplayReset-Targets-Grupos.md` com targets completos e evidência de QA.
- Updated: `QA/GameplayReset-QA.md` com passos para `ActorIdSet`/`ByActorKind` e referências de QA.
- Updated: `DECISIONS.md` alinhado ao timing real do Loading HUD (UseFade vs. Started).

## [2026-01-02]
- Added: documentação do **Baseline 2.0** em `Docs/Baseline/`:
  - `Baseline-Matrix-2.0.md` (matriz mínima de cenários)
  - `Baseline-Invariants.md` (invariantes obrigatórias)
  - `Baseline-Evidence-Template.md` (template de evidência)
- Updated: `README.md` para linkar o pacote Baseline 2.0.
- Updated: `Checklist.md` com evidência do log completo (startup/menu → gameplay → pause/resume → defeat/victory → restart → exit-to-menu).

## [2026-01-01]
- Updated: clarificado wiring de fim de run (`IGameRunEndRequestService` → `GameRunEndRequestedEvent` → `GameRunOutcomeEventInputBridge` → `IGameRunOutcomeService` → `GameRunEndedEvent`).
- Updated: `README.md` adicionando seção “Fim de Run (Vitória/Derrota)” com instruções de uso/teste (`IGameRunEndRequestService`, `GameRunEndRequestedEvent`, hotkeys F7/F6).
- Updated: `WORLD_LIFECYCLE.md` detalhando solicitação de fim de run (request → outcome) e “Teste rápido”.
- Updated: `ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md` (seção 5.2) substituindo placeholder por contrato explícito e guia de teste.

## [2025-12-31]
- Updated: `WORLD_LIFECYCLE.md` e `WORLDLIFECYCLE_RESET_STATUS.md` com evidência de validação "sem flash" e ordem final do pipeline (FadeIn → LoadingHUD → Scene load/unload → ScenesReady → Reset/Skip → gate → Hide HUD → FadeOut).
- Updated: `ADR-0009-FadeSceneFlow.md` e `ADR-0010-LoadingHud-SceneFlow.md` alinhados à ordem final (LoadingHUD só aparece após FadeIn; Hide antes de FadeOut; `Completed` como safety).


All notable documentation changes to **NewScripts** are documented in this file.

## [2025-12-30]
### Updated
- `WORLD_LIFECYCLE.md`: evidência de SKIP em startup/frontend e reforço do gate antes do FadeOut.
- `ADR-0010-LoadingHud-SceneFlow.md`: ordem correta do LoadingHUD com gate de reset e FadeOut.
- `GameLoop-StateFlow-QA.md`: passos/evidências de startup → menu → gameplay → pause/resume (bootstrap, navigation, input mode).
- `GameplayReset-QA.md`: reset em `ScenesReady` com gate durante o hard reset e conclusão antes do FadeOut.

### Updated
- `WORLD_LIFECYCLE.md`: alinhado ao fluxo de produção observado em log (startup→menu com SKIP + gameplay hard reset pós `ScenesReady`).
- `GameLoop-StateFlow-QA.md`: atualizado para o fluxo real (startup termina em Ready; gameplay termina em Playing; pause/resume com gate).
- `GameplayReset-QA.md`: QA do reset em gameplay (baseline Player+Eater) e targets parciais (PlayersOnly/EaterOnly).
- `ADR-0010-LoadingHud-SceneFlow.md`: detalhamento do contrato LoadingHUD por fase do Scene Flow.
- `ADR-0011-WorldDefinition-MultiActor-GameplayScene.md`: marcado como implementado no baseline (Player + Eater).
- `ADR-0013-Ciclo-de-Vida-Jogo.md`: marcado como implementado (baseline) e descrito o contrato `WorldLifecycleResetCompletedEvent`.
- `ADR-0014-GameplayReset-Targets-Grupos.md`: listado baseline de targets suportados e integração com WorldLifecycle.

## [2025-12-29]
- Added: `Reports/SceneFlow-Assets-Checklist.md`.
- Added: report master `Reports/SceneFlow-Production-EndToEnd-Validation.md` com passo-a-passo do fluxo Menu → Gameplay → Menu.
- Updated: report master end-to-end (`Reports/SceneFlow-Production-EndToEnd-Validation.md`) com padrões de busca e critério de fade/HUD.
- Updated: `README.md` para linkar o report master.
- Fixed: removida data futura no README e alinhado vocabulário de fade ao master.
- Updated: `README.md` com links canônicos (master) e seção de reports históricos de SceneFlow.
- Updated: reports históricos de SceneFlow marcados como **HISTÓRICO** e linkados ao master.
- Updated: `Reports/SceneFlow-Gameplay-Blockers-Report.md` com referência ao master.
- Added: normalização de docs do NewScripts (migrando reports para `Docs/Reports`, consolidando `Docs/ADRs` e atualizando links).
- Added: ADR-0014 (`GameplayReset Targets/Grupos`) com targets canônicos, determinismo e integração com WorldLifecycle.
- Removed: pasta `Docs/ADR/` e o arquivo `Docs/ADR.meta` após consolidação de ADRs.
- Fixed: substituição dos placeholders ADR-00XX em `DECISIONS.md` por referências ao ADR-0013.
- Fixed: datas futuras ajustadas no changelog de documentação.
- Updated: documentação integrada de SceneFlow + WorldLifecycle + GameLoop alinhada ao fluxo de produção (startup → Menu → Gameplay → Menu → Gameplay), incluindo:
    - registro operacional do `WorldLifecycleRuntimeCoordinator` e `WorldLifecycleResetCompletionGate` (skip vs hard reset),
    - revisão do `Reports/GameLoop.md` para alinhar `GameLoopSceneFlowCoordinator` e `InputModeSceneFlowBridge`,
    - atualização do ADR de Fade/Loading (ADR-0009) com orquestração entre `SceneTransitionService`, `INewScriptsFadeService` e `SceneFlowLoadingService`,
    - atualização do QA `GameLoop-StateFlow-QA` com cenário end-to-end (defeat/victory forçados via hotkeys).
- Added: `ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md` com nota operacional sobre skip em frontend e reset completo em gameplay.
- Updated: `Reports/GameLoop.md` para documentar o estado `PostPlay`, os eventos `GameRunStartedEvent` / `GameRunEndedEvent` /
  `GameLoopActivityChangedEvent` e o serviço `IGameRunStatusService` no fluxo de pós-game.
- Updated: `WORLD_LIFECYCLE.md` alinhado ao fluxo de run/resultados via GameLoop.
- Updated: caminhos de QA deprecated consolidados em `QA/Deprecated` e referências de documentação ajustadas.
- Updated: cenas de produção/QA limpas de referências diretas aos tools de QA deprecated.
- Added: bridge `GameResetRequestedEvent` → `RestartNavigationBridge` → reset oficial via SceneFlow/WorldLifecycle.
- Updated: `Reports/GameLoop.md` e `WORLD_LIFECYCLE.md` com o fluxo de pós-game (Restart/Menu).
- Added: `Reports/QA-GameplayResetKind.md` com passos e critérios para validar GameplayReset por ActorKind no Player real.
- Updated: `Reports/QA-Audit-2025-12-27.md` com referência ao `GameplayResetPhaseLogger`.
- Updated: QA report com nota sobre gating de probes/logger em Editor/Dev.
- Updated: `Docs/QA/GameplayReset-QA.md` com notas sobre DI do classifier e `verboseLogs`.
- Added: QA Eater (`GameplayResetKindQaEaterActor`) e spawn opcional no `GameplayResetKindQaSpawner` para validar `EaterOnly`.
- Added: `Reports/QA-GameplayReset-RequestMatrix.md` com evidências da validação da matriz GameplayResetRequest.
- Updated: `WORLDLIFECYCLE_RESET_STATUS.md` com nota sobre classificação Kind-first e fallback string-based em `EaterOnly`.

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
- Updated: `WORLDLIFECYCLE_RESET_STATUS.md` com status do progresso e referência a Addressables (planejado).
- Fixed: remoção de artefatos de truncation/scan (‘...’) em docs (sem mudança de comportamento).

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
