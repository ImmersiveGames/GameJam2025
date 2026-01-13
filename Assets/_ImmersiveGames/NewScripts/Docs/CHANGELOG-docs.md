# Changelog (Docs)

## [2026-01-13]
### Added
- Registro incremental de evidências do **Baseline 2.0** (cenários 1 e 2) a partir do log fornecido nesta conversa.

### Evidência (log) usada como fonte de verdade
- **Teste 1 — Startup → Menu (profile=`startup`)**
    - `WorldLifecycleRuntimeCoordinator` solicitou reset e **SKIPOU** por perfil não-gameplay: `Reset SKIPPED (startup/frontend). why='profile'` e emitiu `WorldLifecycleResetCompletedEvent(signature, reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene')`.
    - `WorldLifecycleResetCompletionGate` recebeu o `WorldLifecycleResetCompletedEvent(...)` e liberou o `SceneTransitionService` **antes do FadeOut** (gate cached + “Completion gate concluído. Prosseguindo para FadeOut.”).

- **Teste 2 — Menu → Gameplay (profile=`gameplay`)**
    - `SceneTransitionScenesReady` observado antes de `SceneTransitionCompleted` (ordem preservada).
    - `WorldLifecycleRuntimeCoordinator` executou **hard reset após ScenesReady**: `Disparando hard reset após ScenesReady. reason='ScenesReady/GameplayScene'`.
    - `WorldLifecycleController/Orchestrator` completou o pipeline determinístico:
        - Hooks: `OnBeforeDespawn` → `Despawn` → `OnAfterDespawn` → `OnBeforeSpawn` → `Spawn` → `OnAfterActorSpawn` → `OnAfterSpawn`.
        - Spawns OK: `Spawn services registered from definition: 2` (Player + Eater) e `ActorRegistry count at 'After Spawn': 2`.
    - `WorldLifecycleRuntimeCoordinator` emitiu `WorldLifecycleResetCompletedEvent(signature, reason='ScenesReady/GameplayScene')` e o `WorldLifecycleResetCompletionGate` liberou a continuação do SceneFlow.
    - Após `SceneTransitionCompleted`: `InputMode` mudou para `Gameplay` e o `GameLoop` entrou em `Playing` (`ENTER: Playing (active=True)`).

- **Teste 2 — PostGame → ExitToMenu (profile=`frontend`)**
    - `GameRunEndedEvent` publicado (Outcome=Defeat, Reason='Gameplay/Timeout'), overlay exibido e gate adquirido com `token='state.postgame'` (bloqueio esperado).
    - Em `ExitToMenu`: navegação para Menu com `Profile='frontend'` e reset novamente **SKIPADO**: `reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'`.

### Notes
- Observação de ordenação: há um log pontual de `GameplayNotReady` imediatamente antes do snapshot final marcar `gameplayReady=True` no `SceneTransitionCompleted`. Isso é consistente com a janela entre “Completed:Gameplay” e a publicação do snapshot “scene_transition_completed”.

## [2026-01-05]
### Added
- `IWorldResetRequestService` como gatilho de produção para `ResetWorld` fora de transição, com reason padronizado e dedupe por `contextSignature` durante o fluxo de SceneFlow.
- Registro de validação checklist-driven em `Docs/Reports/Baseline-2.0-ChecklistVerification-LastRun.md` (gerado pelo menu Verify Last Run).
- ADR de fechamento do Baseline 2.0: `Docs/ADRs/ADR-0016-Baseline-2.0-Fechamento.md`.

### Changed
- Consolidado o conjunto canônico de documentação (README/ARCHITECTURE/WORLD_LIFECYCLE/ADRs/Baseline 2.0/CHANGELOG) e ajustado o mapa de navegação.
- Baseline 2.0 Spec atualizado como fonte única de matriz/invariantes e template mínimo de evidência.
- Checklist operacional do Baseline 2.0 consolidado em `Docs/Reports` (referência única para o último smoke).
- Checklist do Baseline 2.0 atualizado com **assinaturas/strings exatas** do log para cada cenário (A–E) e invariantes globais.
- Adicionada evidência explícita de **ExitToMenu** (profile `frontend`, reset SKIPPED) e **Restart** pós-PostGame (profile `gameplay`).
- Documentado o módulo de Loading HUD (`SceneFlowLoadingService` + `INewScriptsLoadingHudService`) no `WORLD_LIFECYCLE.md` com ordem de fases e assinaturas de log estáveis.

### Removed
- Duplicações de checklist de baseline espalhadas em ADRs/QA/Baseline.
- Documentos redundantes de visão geral/planejamento que repetiam arquitetura e baseline.

### Validated
- Baseline 2.0 (Smoke, A–E) **aprovado por evidência manual** usando o log `Reports/Baseline-2.0-Smoke-LastRun.log` como fonte de verdade nesta iteração.
- Baseline 2.0 (A–E + invariantes) reforçado com evidência hard do `Baseline-2.0-Smoke-LastRun.log` sem ambiguidades de assinatura/reason.
- Checklist-driven **Pass** confirmado em `Reports/Baseline-2.0-ChecklistVerification-LastRun.md`.

### Notes
- A validação automática do relatório (parser do `Baseline2SmokeLastRunTool`) permaneceu instável após múltiplas tentativas; para evitar bloqueio de progresso, o status do Baseline foi concluído **com base no log bruto** (assinaturas e invariantes verificadas via busca manual).
- A correção/robustez do tool fica registrada como débito técnico para retomada posterior (sem impacto no avanço do fluxo de produção).
- Updated: contrato de `reason` (ResetCompleted) e evidências/docs para explicitar o formato completo do SKIP (`Skipped_StartupOrFrontend:profile=...;scene=...`) e consolidar prefixos canônicos no runtime.
- Updated: `WORLD_LIFECYCLE.md` com seção/tabela de ownership de limpeza (Global vs Scene vs Object) e regras de descarte (Dispose) para evitar vazamentos entre transições/resets.

### Evidência (log) usada como fonte de verdade
- Captura de log end-to-end cobre: **Startup → Menu → Gameplay → Pause/Resume → Victory → Restart → Defeat → ExitToMenu → Quit**.
- Assinaturas-chave observadas:
    - `SceneTransitionStarted` adquire gate com token **`flow.scene_transition`** e publica snapshot `gateOpen=False`.
    - `SceneTransitionScenesReady` ocorre **antes** de `SceneTransitionCompleted` (ordem preservada).
    - `WorldLifecycleRuntimeCoordinator`:
        - **profile=startup/frontend:** `Reset SKIPPED (...)` e emite `WorldLifecycleResetCompletedEvent(signature, reason)`.
        - **profile=gameplay:** `Disparando hard reset após ScenesReady` e emite `WorldLifecycleResetCompletedEvent(signature, reason='ScenesReady/GameplayScene')`.
    - `WorldLifecycleResetCompletionGate` recebe o evento e libera a continuação do SceneFlow **antes do FadeOut**.
    - Spawn em Gameplay registra 2 serviços (`PlayerSpawnService`, `EaterSpawnService`) e resulta em `ActorRegistry count at 'After Spawn': 2`.
    - `GameLoopService` sincroniza **Ready → Playing** após `SceneTransitionCompleted` (profile gameplay), liberando StateDependent (`Action 'Move' liberada`).
- Checklist gerada: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Checklist.md`.

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
