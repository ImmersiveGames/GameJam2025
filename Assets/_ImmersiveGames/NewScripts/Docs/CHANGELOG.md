# Changelog â€” Docs

## 2026-03-11

- **Docs-only:** cadeia canonica sincronizada com o estado pos-H1..H7.
- **Canon-only no eixo principal:** `LevelFlow`, `LevelDefinition`, `Navigation`, `WorldLifecycle V2` e tooling/editor/QA associado passam a ser descritos oficialmente como canon-only.
- **Compat residual removida do eixo principal:** docs deixam de tratar como ativas superficies como `LevelId`, `ContentId`, `NavigateAsync(string)`, `RequestMenuAsync`, `RequestGameplayAsync`, `IGameNavigationLegacyService`, `GameNavigationIntents` e `CreateWithLegacyCompat` no trilho principal.
- **Excecoes registradas com precisao:** permanece excecao localizada em `Gameplay RunRearm` (fallback legado de actor-kind/string) e pequeno residuo editor/serializado em `GameNavigationIntentCatalogAsset`.

- **Docs-only:** correcao cosmetica em `Docs/Reports/Baseline/2026-03-06/Baseline-3.1-Freeze.md`, removendo o ruido de formatacao `` `n `` na secao de evidencia completa sem alterar o conteudo semantico do freeze.
- **Higiene de navegacao:** `Docs/README.md` passa a destacar explicitamente a trilha principal `README -> Canon -> Plan -> Audits/LATEST -> Evidence/LATEST -> ADRs -> CHANGELOG`.
- **Despromocao de redundancias:** `Overview/Overview.md`, `Guides.md`, `Plans/README.md`, arvores auxiliares e snapshots datados permanecem preservados, mas fora da navegacao principal.

- **Docs-only:** sincronizados README, Canon/Canon-Index, Reports/Evidence/LATEST, ADRs/README e auditoria datada para refletir o baseline/evidÃªncia canÃ´nica congelada em Docs/Reports/Baseline/2026-03-06/Baseline-3.1-Freeze.md.
- **Cadeia canÃ´nica clarificada:** Canon/Canon-Index.md -> Plans/Plan-Continuous.md -> Reports/Audits/LATEST.md -> Reports/Evidence/LATEST.md.
- **Drift removido:** Reports/Evidence/LATEST.md deixa de apontar para Baseline 2.2 / 2026-02-03 e passa a promover o freeze 3.1 de 2026-03-06.
## 2026-02-15

- **F3 concluÃ­do (rota como fonte Ãºnica de SceneData) â€” commit X**.
- **Plano Strings â†’ DirectRefs atualizado para polÃ­tica strict fail-fast â€” commit X**.
- **Registrado como concluÃ­do:** `SceneFlow-Navigation-LevelFlow-Refactor-Plan-v2.1.3` (F1â€“F5).
- **F3 (Route como fonte Ãºnica de SceneData):** SceneFlow falha em `RouteId` ausente/invÃ¡lido; Navigation/LevelFlow resolvem rota via `routeRef`/`SceneRouteId` e emitem logs `[OBS][SceneFlow] RouteResolvedVia=...`.


## 2026-02-04

### Changed
- ADR templates separados por tipo (implementaÃ§Ã£o vs completude) e ADRs alinhados ao template correspondente.
- ADRs e Overview atualizados com caminhos atuais e evidÃªncia mais recente (`Docs/Reports/lastlog.log`).
- EvidÃªncia LATEST aponta para o log bruto mais recente (`Reports/lastlog.log`).


## 2026-01-31



### Changed (reduÃ§Ã£o de arquivos)
- Consolidado `Overview/Architecture.md` + `Overview/WorldLifecycle.md` em `Overview/Overview.md`.
- Consolidado guias/checklists em `Guides.md` (remoÃ§Ã£o de `HowTo/` e `Checklists/`).
- Removidos READMEs redundantes de subpastas (Overview/Plans/Reports/Audits/Archive).
- `Reason-Registry.md` incorporado ao `Standards/Standards.md#observability-contract` (Reason-Map removido; ver seÃ§Ã£o `Standards/Standards.md#reason-map-legado`).
- `Standards/Evidence-Methodology.md` removido; a metodologia passa a viver em `Reports/Evidence/README.md`.
- Removido `Reports/README.md` (conteÃºdo redundante/inconsistente).
- Removido `Reports/Evidence/_Archive/` redundante (duplicava snapshots jÃ¡ presentes por data).
- `Archive/Plans/Plano-2.2.md` movido para `Plans/Archive-Plano-2.2.md`.

### Changed (Docs reorg)
- Reorganizada a estrutura de `Docs/` (Overview/Standards/Reports/Plans/Archive/Checklists).
- `Observability-Contract.md` movido para `Docs/Standards/` e atualizado com Ã¢ncora `DEGRADED_MODE`.
- Consolidado: arquivos em `Docs/Standards/*` em `Docs/Standards/Standards.md`.
- Auditorias movidas para `Docs/Reports/Audits/<data>/`.

### Added
- Evidence snapshot **2026-01-31** (trecho parcial de log) em `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`.

### Changed
- Sincronizado `ADR-0017-LevelManager-Config-Catalog.md` com a versÃ£o mais recente enviada.
- Sincronizados ADRs e Observability-Contract com snapshot canÃ´nico 2026-01-31.
- Sincronizados ADRs e Observability-Contract com snapshot canÃ´nico 2026-01-31.
- Sincronizados ADRs e o contrato de observabilidade com o snapshot canÃ´nico 2026-01-31.
- `Standards/Standards.md#observability-contract` sincronizado para apontar o snapshot 2026-01-31 (sem duplicatas).
- `Standards/Standards.md#observability-contract` sincronizado para apontar para a evidÃªncia 2026-01-31.

### Changed (ADR-0009 â€” completude)

- ADR-0009 atualizado para refletir o contrato operacional completo (Strict vs Release + `DEGRADED_MODE` + Ã¢ncoras `[OBS][Fade]`) e incluir procedimento de verificaÃ§Ã£o QA.
- `ADR-Sync-Audit-NewScripts.md`: ADR-0009 reclassificado de **RISCO** para **OK** (gaps crÃ­ticos removidos; evidÃªncia datada pendente).
- `Invariants-StrictRelease-Audit.md`: Item A atualizado para **PARCIAL** (Fade PASS; LoadingHUD pendente/FAIL).
- ADR-0011: reforÃ§ado contrato de WorldDefinition em gameplay (Strict/Release) e incluÃ­da validaÃ§Ã£o de mÃ­nimos (Player/Eater) na doc.
- `ADR-Sync-Audit-NewScripts.md`: ADR-0011 reclassificado de **PARCIAL** para **OK** (enforce implementado).


## 2026-01-29
- Evidence/Baseline 2.2 (2026-01-29): atualizado com anchors exatos e trechos canÃ´nicos do log (Startupâ†’Menu skip, Menuâ†’Gameplay reset+spawn, IntroStage, ContentSwap, Pause/Resume, PostGame Restart/ExitToMenu).
- ADR-0017 adicionado (LevelManager + ConfigCatalog) com seÃ§Ã£o de evidÃªncia pendente + assinaturas esperadas.
- ADRs/README atualizado para incluir ADR-0017.

## 2026-01-28
- Archived Baseline 2.2 evidence snapshot (Bootâ†’Menu skip, Menuâ†’Gameplay reset+spawn+IntroStage, Level L01 InPlace pipeline).
- ADR-0012: removida referÃªncia obsoleta a `WorldLifecycleRuntimeCoordinator` (substituÃ­do pelo driver canÃ´nico `WorldLifecycleSceneFlowResetDriver`).
- Modules/WorldLifecycle/Runtime (Observability): alinhado contrato mÃ­nimo de observabilidade para WorldLifecycle (ResetRequested/ResetCompleted) e InputMode em `SceneFlow/Completed`.

## 2026-01-27
- Docs: Baseline 2.0 â†’ fontes vigentes (ADR-0015 + Evidence/LATEST + Observability-Contract).
- ADR-0012: PostGame canÃ´nico + idempotÃªncia do overlay (double click + evento duplicado).
- Arquivos alterados: `Docs/Overview/Overview.md`, `Docs/CHANGELOG.md`,
  `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`,
  `Docs/Standards/Standards.md#observability-contract`.

## 2026-01-21
- ARCHITECTURE.md e READMEs ajustados para terminologia consistente (ContentSwap vs LevelManager).

## 2026-01-20
- Plano 2.2 reordenado com QA separado para ContentSwap (QA_ContentSwap) e Level (QA_Level).
- Observability-Contract atualizado para ContentSwap + Level (reasons e anchors).

## 2026-01-19
- Ãndice de ADRs atualizado para refletir os novos escopos.

## 2026-01-18
- Reports/Evidence: novo snapshot 2026-01-18 (Baseline 2.1) com logs mesclados (Restart e ExitToMenu).
- ADR-0012: referÃªncia de evidÃªncia atualizada para o snapshot 2026-01-18.
- ADR-0015: referÃªncia de evidÃªncia atualizada para o snapshot 2026-01-18.

## [2026-01-16]

### Alterado

- Consolidado snapshot datado de evidÃªncias em `Docs/Reports/Evidence/2026-01-16/` e atualizado `Docs/Reports/Evidence/LATEST.md`.
- Restaurado `Docs/Standards/Standards.md#observability-contract` como fonte de verdade.
- Atualizados links de evidÃªncia em ADRs e READMEs para apontar para `Docs/Reports/Evidence/`.

## [2026-01-15]
### Changed
- Baseline 2.0 checklist ajustado para refletir a cobertura do log atual (A, B, D, E; **IntroStage pendente**) e a ordem Fade/Loading detalhada.
- ADR-0016 refinado para explicitar contrato operacional da IntroStage (token `sim.gameplay`, InputMode UI/Gameplay, `UIConfirm`/`NoContent`, RuntimeDebugGui/QA).
- ADR-0010 alinhado Ã  ordem real do Fade/Loading HUD e ao posicionamento da IntroStage (post-reveal).
- IntroStage consolidada como termo canÃ´nico (sem compatibilidade legada).

## [2026-01-14]
### Changed
- ADR-0016 atualizado para consolidar **IntroStage (PostReveal)** como nomenclatura canÃ´nica e explicitando que ocorre apÃ³s `FadeOut` e `SceneTransitionCompleted` (fora do Completion Gate).

## [2026-01-13]
### Added
- Registro incremental de evidÃªncias do **Baseline 2.0** (cenÃ¡rios 1 e 2) a partir do log fornecido nesta conversa.

### EvidÃªncia (log) usada como fonte de verdade
- **Teste 1 â€” Startup â†’ Menu (profile=`startup`)**
    - `WorldLifecycleSceneFlowResetDriver` solicitou reset e **SKIPOU** por perfil nÃ£o-gameplay: `Reset SKIPPED (startup/frontend). why='profile'` e emitiu `WorldLifecycleResetCompletedEvent(signature, reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene')`.
    - `WorldLifecycleResetCompletionGate` recebeu o `WorldLifecycleResetCompletedEvent(...)` e liberou o `SceneTransitionService` **antes do FadeOut** (gate cached + â€œCompletion gate concluÃ­do. Prosseguindo para FadeOut.â€).

- **Teste 2 â€” Menu â†’ Gameplay (profile=`gameplay`)**
    - `SceneTransitionScenesReady` observado antes de `SceneTransitionCompleted` (ordem preservada).
    - `WorldLifecycleSceneFlowResetDriver` executou **hard reset apÃ³s ScenesReady**: `Disparando hard reset apÃ³s ScenesReady. reason='ScenesReady/GameplayScene'`.
    - `WorldLifecycleController/Orchestrator` completou o pipeline determinÃ­stico:
        - Hooks: `OnBeforeDespawn` â†’ `Despawn` â†’ `OnAfterDespawn` â†’ `OnBeforeSpawn` â†’ `Spawn` â†’ `OnAfterActorSpawn` â†’ `OnAfterSpawn`.
        - Spawns OK: `Spawn services registered from definition: 2` (Player + Eater) e `ActorRegistry count at 'After Spawn': 2`.
    - `WorldLifecycleSceneFlowResetDriver` emitiu `WorldLifecycleResetCompletedEvent(signature, reason='ScenesReady/GameplayScene')` e o `WorldLifecycleResetCompletionGate` liberou a continuaÃ§Ã£o do SceneFlow.
    - ApÃ³s `SceneTransitionCompleted`: `InputMode` mudou para `Gameplay` e o `GameLoop` entrou em `Playing` (`ENTER: Playing (active=True)`).

- **Teste 2 â€” PostGame â†’ ExitToMenu (profile=`frontend`)**
    - `GameRunEndedEvent` publicado (Outcome=Defeat, Reason='Gameplay/Timeout'), overlay exibido e gate adquirido com `token='state.postgame'` (bloqueio esperado).
    - Em `ExitToMenu`: navegaÃ§Ã£o para Menu com `Profile='frontend'` e reset novamente **SKIPADO**: `reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'`.

### Notes
- ObservaÃ§Ã£o de ordenaÃ§Ã£o: hÃ¡ um log pontual de `GameplayNotReady` imediatamente antes do snapshot final marcar `gameplayReady=True` no `SceneTransitionCompleted`. Isso Ã© consistente com a janela entre â€œCompleted:Gameplayâ€ e a publicaÃ§Ã£o do snapshot â€œscene_transition_completedâ€.

## [2026-01-05]
### Added
- `IWorldResetRequestService` como gatilho de produÃ§Ã£o para `ResetWorld` fora de transiÃ§Ã£o, com reason padronizado e dedupe por `contextSignature` durante o fluxo de SceneFlow.
- Registro de validaÃ§Ã£o checklist-driven em `Docs/Reports/Baseline-2.0-ChecklistVerification-LastRun.md` (gerado pelo menu Verify Last Run).
- ADR de fechamento do Baseline 2.0: `Docs/ADRs/ADR-0016-Baseline-2.0-Fechamento.md`.

### Changed
- Consolidado o conjunto canÃ´nico de documentaÃ§Ã£o (README/ARCHITECTURE/WORLD_LIFECYCLE/ADRs/Baseline 2.0/CHANGELOG) e ajustado o mapa de navegaÃ§Ã£o.
- Baseline 2.0 Spec atualizado como fonte Ãºnica de matriz/invariantes e template mÃ­nimo de evidÃªncia.
- Checklist operacional do Baseline 2.0 consolidado em `Docs/Reports` (referÃªncia Ãºnica para o Ãºltimo smoke).
- Checklist do Baseline 2.0 atualizado com **assinaturas/strings exatas** do log para cada cenÃ¡rio (Aâ€“E) e invariantes globais.
- Adicionada evidÃªncia explÃ­cita de **ExitToMenu** (profile `frontend`, reset SKIPPED) e **Restart** pÃ³s-PostGame (profile `gameplay`).
- Documentado o mÃ³dulo de Loading HUD (`LoadingHudOrchestrator` + `ILoadingHudService`) no `WORLD_LIFECYCLE.md` com ordem de fases e assinaturas de log estÃ¡veis.

### Removed
- DuplicaÃ§Ãµes de checklist de baseline espalhadas em ADRs/QA/Baseline.
- Documentos redundantes de visÃ£o geral/planejamento que repetiam arquitetura e baseline.

### Validated
- Baseline 2.0 (Smoke, Aâ€“E) **aprovado por evidÃªncia manual** usando o log `Reports/Baseline-2.0-Smoke-LastRun.log` como fonte de verdade nesta iteraÃ§Ã£o.
- Baseline 2.0 (Aâ€“E + invariantes) reforÃ§ado com evidÃªncia hard do `Baseline-2.0-Smoke-LastRun.log` sem ambiguidades de assinatura/reason.
- Checklist-driven **Pass** confirmado em `Reports/Baseline-2.0-ChecklistVerification-LastRun.md`.

### Notes
- A validaÃ§Ã£o automÃ¡tica do relatÃ³rio (parser do `Baseline2SmokeLastRunTool`) permaneceu instÃ¡vel apÃ³s mÃºltiplas tentativas; para evitar bloqueio de progresso, o status do Baseline foi concluÃ­do **com base no log bruto** (assinaturas e invariantes verificadas via busca manual).
- A correÃ§Ã£o/robustez do tool fica registrada como dÃ©bito tÃ©cnico para retomada posterior (sem impacto no avanÃ§o do fluxo de produÃ§Ã£o).
- Updated: contrato de `reason` (ResetCompleted) e evidÃªncias/docs para explicitar o formato completo do SKIP (`Skipped_StartupOrFrontend:profile=...;scene=...`) e consolidar prefixos canÃ´nicos no runtime.
- Updated: `WORLD_LIFECYCLE.md` com seÃ§Ã£o/tabela de ownership de limpeza (Global vs Scene vs Object) e regras de descarte (Dispose) para evitar vazamentos entre transiÃ§Ãµes/resets.

### EvidÃªncia (log) usada como fonte de verdade
- Captura de log end-to-end cobre: **Startup â†’ Menu â†’ Gameplay â†’ Pause/Resume â†’ Victory â†’ Restart â†’ Defeat â†’ ExitToMenu â†’ Quit**.
- Assinaturas-chave observadas:
    - `SceneTransitionStarted` adquire gate com token **`flow.scene_transition`** e publica snapshot `gateOpen=False`.
    - `SceneTransitionScenesReady` ocorre **antes** de `SceneTransitionCompleted` (ordem preservada).
    - `WorldLifecycleSceneFlowResetDriver`:
        - **profile=startup/frontend:** `Reset SKIPPED (...)` e emite `WorldLifecycleResetCompletedEvent(signature, reason)`.
        - **profile=gameplay:** `Disparando hard reset apÃ³s ScenesReady` e emite `WorldLifecycleResetCompletedEvent(signature, reason='ScenesReady/GameplayScene')`.
    - `WorldLifecycleResetCompletionGate` recebe o evento e libera a continuaÃ§Ã£o do SceneFlow **antes do FadeOut**.
    - Spawn em Gameplay registra 2 serviÃ§os (`PlayerSpawnService`, `EaterSpawnService`) e resulta em `ActorRegistry count at 'After Spawn': 2`.
    - `GameLoopService` sincroniza **Ready â†’ Playing** apÃ³s `SceneTransitionCompleted` (profile gameplay), liberando StateDependent (`Action 'Move' liberada`).
- Checklist gerada para o Baseline 2.0 (removida posteriormente conforme ADR-0015;
  referÃªncia vigente Ã© Evidence/LATEST + Observability-Contract).

## [2026-01-03]
- Added: `Reports/Baseline-Audit-2026-01-03.md` com matriz de evidÃªncia (cÃ³digo + QA/logs) e status de validaÃ§Ã£o.
- Updated: `README.md` com report master, baseline audit e alvo `ByActorKind` na lista de targets.
- Updated: `WORLD_LIFECYCLE.md` com assinatura canÃ´nica (`ContextSignature`) e ordem de LoadingHUD por `UseFade`.
- Updated: `WORLDLIFECYCLE_RESET_STATUS.md` com status atual dos targets e correÃ§Ã£o de assinatura.
- Updated: `ADRs/ADR-0014-GameplayReset-Targets-Grupos.md` com targets completos e evidÃªncia de QA.
- Updated: `QA/GameplayReset-QA.md` com passos para `ActorIdSet`/`ByActorKind` e referÃªncias de QA.
- TODO: `QA/GameplayReset-QA.md` nÃ£o encontrado em Docs/Modules; confirmar doc equivalente.
- Updated: `DECISIONS.md` alinhado ao timing real do Loading HUD (UseFade vs. Started).

## [2026-01-02]
- Added: documentaÃ§Ã£o do **Baseline 2.0** em `Docs/Baseline/`:
    - `Baseline-Matrix-2.0.md` (matriz mÃ­nima de cenÃ¡rios)
    - `Baseline-Invariants.md` (invariantes obrigatÃ³rias)
    - `Baseline-Evidence-Template.md` (template de evidÃªncia)
- Updated: `README.md` para linkar o pacote Baseline 2.0.
- Updated: `Checklist.md` com evidÃªncia do log completo (startup/menu â†’ gameplay â†’ pause/resume â†’ defeat/victory â†’ restart â†’ exit-to-menu).

## [2026-01-01]
- Updated: clarificado wiring de fim de run (`IGameRunEndRequestService` â†’ `GameRunEndRequestedEvent` â†’ `GameRunOutcomeCommandBridge` â†’ `IGameRunOutcomeService` â†’ `GameRunEndedEvent`).
- Updated: `README.md` adicionando seÃ§Ã£o â€œFim de Run (VitÃ³ria/Derrota)â€ com instruÃ§Ãµes de uso/teste (`IGameRunEndRequestService`, `GameRunEndRequestedEvent`, hotkeys F7/F6).
- Updated: `WORLD_LIFECYCLE.md` detalhando solicitaÃ§Ã£o de fim de run (request â†’ outcome) e â€œTeste rÃ¡pidoâ€.
- Updated: `ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md` (seÃ§Ã£o 5.2) substituindo placeholder por contrato explÃ­cito e guia de teste.

## [2025-12-31]
- Updated: `WORLD_LIFECYCLE.md` e `WORLDLIFECYCLE_RESET_STATUS.md` com evidÃªncia de validaÃ§Ã£o "sem flash" e ordem final do pipeline (FadeIn â†’ LoadingHUD â†’ Scene load/unload â†’ ScenesReady â†’ Reset/Skip â†’ gate â†’ Hide HUD â†’ FadeOut).
- Updated: `ADR-0009-FadeSceneFlow.md` e `ADR-0010-LoadingHud-SceneFlow.md` alinhados Ã  ordem final (LoadingHUD sÃ³ aparece apÃ³s FadeIn; Hide antes de FadeOut; `Completed` como safety).

All notable documentation changes to **NewScripts** are documented in this file.

## [2026-02-25]
### Docs cleanup / retenÃ§Ã£o
- Removed: planos concluÃ­dos/arquivados em `Docs/Plans/` (DONE/ARCHIVED, SUPERSEDED e `Archive-*`) para manter somente trilho ativo (`Plan-Continuous` + WIP).
- Removed: versÃµes antigas do plano de refactor em `Docs/Overview/` (`v2` e `v2.1.1`) e promoÃ§Ã£o da versÃ£o mais recente para nome canÃ´nico sem sufixo de versÃ£o.
- Removed: auditorias antigas em `Docs/Reports/Audits/`, mantendo apenas as 3 pastas datadas mais recentes (`2026-02-17`, `2026-02-18`, `2026-02-19`).
- Moved: `Reports/Audits/2026-02-19/ADR-Sync-Audit-Prompt.md` para `Reports/Audits/2026-02-19/` por aderÃªncia ao padrÃ£o de retenÃ§Ã£o por data.
- Updated: Ã­ndices e ponteiros (`Docs/README.md`, `Overview/Overview.md`, `Plans/README.md`, `Reports/Evidence/README.md`, `Reports/Evidence/LATEST.md`) para evitar referÃªncias quebradas.
- Nota: histÃ³rico completo permanece disponÃ­vel via Git.

## [2025-12-30]
### Updated
- `WORLD_LIFECYCLE.md`: evidÃªncia de SKIP em startup/frontend e reforÃ§o do gate antes do FadeOut.
- `ADR-0010-LoadingHud-SceneFlow.md`: ordem correta do LoadingHUD com gate de reset e FadeOut.
- `GameLoop-StateFlow-QA.md`: passos/evidÃªncias de startup â†’ menu â†’ gameplay â†’ pause/resume (bootstrap, navigation, input mode).
- `GameplayReset-QA.md`: reset em `ScenesReady` com gate durante o hard reset e conclusÃ£o antes do FadeOut.
- `WORLD_LIFECYCLE.md`: alinhado ao fluxo de produÃ§Ã£o observado em log (startupâ†’menu com SKIP + gameplay hard reset pÃ³s `ScenesReady`).
- `GameLoop-StateFlow-QA.md`: atualizado para o fluxo real (startup termina em Ready; gameplay termina em Playing; pause/resume com gate).
- `GameplayReset-QA.md`: QA do reset em gameplay (baseline Player+Eater) e targets parciais (PlayersOnly/EaterOnly).
- `ADR-0010-LoadingHud-SceneFlow.md`: detalhamento do contrato LoadingHUD por fase do Scene Flow.
- `ADR-0011-WorldDefinition-MultiActor-GameplayScene.md`: marcado como implementado no baseline (Player + Eater).
- `ADR-0013-Ciclo-de-Vida-Jogo.md`: marcado como implementado (baseline) e descrito o contrato `WorldLifecycleResetCompletedEvent`.
- `ADR-0014-GameplayReset-Targets-Grupos.md`: listado baseline de targets suportados e integraÃ§Ã£o com WorldLifecycle.

## [2025-12-29]
- Added: `Reports/SceneFlow-Assets-Checklist.md`.
- Added: report master `Reports/SceneFlow-Production-EndToEnd-Validation.md` com passo-a-passo do fluxo Menu â†’ Gameplay â†’ Menu.
- Updated: report master end-to-end (`Reports/SceneFlow-Production-EndToEnd-Validation.md`) com padrÃµes de busca e critÃ©rio de fade/HUD.
- Updated: `README.md` para linkar o report master.
- Fixed: removida data futura no README e alinhado vocabulÃ¡rio de fade ao master.
- Updated: `README.md` com links canÃ´nicos (master) e seÃ§Ã£o de reports histÃ³ricos de SceneFlow.
- Updated: reports histÃ³ricos de SceneFlow marcados como **HISTÃ“RICO** e linkados ao master.
- Updated: `Reports/Archive/2025/SceneFlow-Gameplay-Blockers-Report.md` com referÃªncia ao master.
- Added: normalizaÃ§Ã£o de docs do NewScripts (migrando reports para `Docs/Reports`, consolidando `Docs/ADRs` e atualizando links).
- Added: ADR-0014 (`GameplayReset Targets/Grupos`) com targets canÃ´nicos, determinismo e integraÃ§Ã£o com WorldLifecycle.
- Removed: pasta `Docs/ADR/` e o arquivo `Docs/ADR.meta` apÃ³s consolidaÃ§Ã£o de ADRs.
- Fixed: substituiÃ§Ã£o dos placeholders ADR-00XX em `DECISIONS.md` por referÃªncias ao ADR-0013.
- Fixed: datas futuras ajustadas no changelog de documentaÃ§Ã£o.
- Updated: documentaÃ§Ã£o integrada de SceneFlow + WorldLifecycle + GameLoop alinhada ao fluxo de produÃ§Ã£o (startup â†’ Menu â†’ Gameplay â†’ Menu â†’ Gameplay), incluindo:
    - registro operacional do `WorldLifecycleSceneFlowResetDriver` e `WorldLifecycleResetCompletionGate` (skip vs hard reset),
    - revisÃ£o do `Reports/GameLoop.md` para alinhar `GameLoopSceneFlowCoordinator` e `InputModeSceneFlowBridge`,
    - atualizaÃ§Ã£o do ADR de Fade/Loading (ADR-0009) com orquestraÃ§Ã£o entre `SceneTransitionService`, `IFadeService` e `LoadingHudOrchestrator`,
    - atualizaÃ§Ã£o do QA `GameLoop-StateFlow-QA` com cenÃ¡rio end-to-end (defeat/victory forÃ§ados via hotkeys).
- Added: `ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md` com nota operacional sobre skip em frontend e reset completo em gameplay.
- Updated: `Reports/GameLoop.md` para documentar o estado interno `PostPlay` (nome canÃ´nico: **PostGame**),
  os eventos `GameRunStartedEvent` / `GameRunEndedEvent` / `GameLoopActivityChangedEvent` e o serviÃ§o
  `IGameRunStateService` no fluxo de pÃ³s-game.
- Updated: `WORLD_LIFECYCLE.md` alinhado ao fluxo de run/resultados via GameLoop.
- Updated: caminhos de QA deprecated consolidados em `QA/Deprecated` e referÃªncias de documentaÃ§Ã£o ajustadas.
- TODO: `QA/Deprecated` nÃ£o encontrado em Modules/; confirmar destino para QA legado.
- Updated: cenas de produÃ§Ã£o/QA limpas de referÃªncias diretas aos tools de QA deprecated.
- Added: bridge `GameResetRequestedEvent` â†’ `RestartNavigationBridge` â†’ reset oficial via SceneFlow/WorldLifecycle.
- Updated: `Reports/GameLoop.md` e `WORLD_LIFECYCLE.md` com o fluxo de pÃ³s-game (Restart/Menu).
- Added: `Reports/QA-GameplayResetKind.md` com passos e critÃ©rios para validar GameplayReset por ActorKind no Player real.
- Updated: `Reports/Archive/2025/QA-Audit-2025-12-27.md` com referÃªncia ao `GameplayResetStepLogger`.
- Updated: QA report com nota sobre gating de probes/logger em Editor/Dev.
- Updated: documentaÃ§Ã£o de QA de GameplayReset com notas sobre DI do classifier e `verboseLogs`.
- Added: QA Eater (`GameplayResetKindQaEaterActor`) e spawn opcional no `GameplayResetKindQaSpawner` para validar `EaterOnly`.
- Added: `Reports/QA-GameplayReset-RequestMatrix.md` com evidÃªncias da validaÃ§Ã£o da matriz GameplayResetRequest.
- Updated: `WORLDLIFECYCLE_RESET_STATUS.md` com nota sobre classificaÃ§Ã£o Kind-first e fallback string-based em `EaterOnly`.

## [2025-12-28]
- Added: suporte a `ActorKind.Eater` na GameplayScene (EaterSpawnService + WorldDefinition) documentado como parte do reset hard de produÃ§Ã£o.
- Added: `EaterRandomMovementController` documentado como integrado ao `IStateDependentService` para `GameplayAction.Move` (respeita GameLoop/SimulationGate/Pause).
- Added: `WorldLifecycleMultiActorSpawnQa` documentado para validar Player + Eater no `IActorRegistry` apÃ³s reset da GameplayScene.
- Updated: `WORLD_LIFECYCLE.md` e `ADR-0011-WorldDefinition-MultiActor-GameplayScene.md` com extensÃ£o de multi-actor spawn na GameplayScene.
- Updated: docs para reforÃ§ar `GameLoopSceneFlowCoordinator` como fonte Ãºnica de `RequestStart()` e que navigation nÃ£o emite start.
- Updated: debug tools/QA triggers marcados como dev-only na documentaÃ§Ã£o e relatÃ³rio de validaÃ§Ã£o do SceneFlow.
- Added: `Reports/SceneFlow-Production-Validation-2025-12-28.md` com checklist do fluxo de produÃ§Ã£o e evidÃªncia mÃ­nima de logs.
- Added: `Reports/Archive/2025/SceneFlow-Gameplay-To-Menu-Report.md` com checklist e logs esperados do retorno Gameplay â†’ Menu.
- Updated: `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/ExitToMenuNavigationBridge.cs`, `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.cs` e `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Commands/GameCommands.cs` para suportar ExitToMenu em produÃ§Ã£o/dev.
- Added: `Reports/Archive/2025/SceneFlow-Gameplay-Blockers-Report.md` com os 3 blockers do fluxo Menu â†’ Gameplay (erros, causa raiz, correÃ§Ãµes e evidÃªncias).
- Updated: `Assets/_ImmersiveGames/NewScripts/Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs` e `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Spawning/PlayerSpawnService.cs` (fixes de blockers do fluxo).
- Updated: `README.md` e `WORLD_LIFECYCLE.md` com explicaÃ§Ã£o simples do pipeline, definiÃ§Ã£o de â€œloading realâ€
  e critÃ©rio para remover o SKIP (decisÃ£o registrada).
- Updated: `ADRs/ADR-0010-LoadingHud-SceneFlow.md` e `ARCHITECTURE_TECHNICAL.md` com formalizaÃ§Ã£o de
  reset/spawn como parte do loading e diretrizes futuras para Addressables (tarefas agregadas).
- Updated: `WORLDLIFECYCLE_RESET_STATUS.md` com status do progresso e referÃªncia a Addressables (planejado).
- Fixed: remoÃ§Ã£o de artefatos de truncation/scan (â€˜...â€™) em docs (sem mudanÃ§a de comportamento).

## [2025-12-27]
- Added: `Reports/Archive/2025/Legacy-Cleanup-Report.md` com inventÃ¡rio de referÃªncias residuais ao legado e plano de remoÃ§Ã£o.
- Added: `Reports/Archive/2025/SceneFlow-Smoke-Result.md` com resultado do smoke test do SceneFlow (startup/menu â†’ gameplay) incluindo logs essenciais.
- Added: `Reports/Archive/2025/QA-Audit-2025-12-27.md` com auditoria dos QAs ativos/removidos e recomendaÃ§Ãµes de baseline.

- Updated: docs: convert navigation references to Markdown links + cleanup placeholders.
- Updated: `ADRs/ADR-0009-FadeSceneFlow.md` (OpÃ§Ã£o A) â€” Fade via cena aditiva (`FadeScene`) integrada ao SceneFlow.
- Updated: `ADRs/ADR-0010-LoadingHud-SceneFlow.md` (OpÃ§Ã£o A) â€” HUD de loading via cena aditiva (`LoadingHudScene`) integrada ao SceneFlow.


