# ADR Sync Audit — NewScripts (ADR-0009..ADR-0019)

Data: 2026-01-31
Escopo: `Assets/_ImmersiveGames/NewScripts/`

> **Nota de execução:** auditoria apenas de leitura. Evidências sempre citam arquivo + linha quando o item é baseado em código.

---

## 1) Sumário executivo

### Top 10 divergências por impacto

1) **Fade sem Strict/Release explícito + fallback silencioso**: `NewScriptsFadeService` continua sem fail-fast e segue sem fade quando o controller não existe; `SceneTransitionService` aceita adapter indisponível e segue sem fade, sem âncora `DEGRADED_MODE`. Evidência: `NewScriptsFadeService` faz warning e retorna sem falha; `SceneTransitionService` continua quando adapter não existe; `NewScriptsSceneFlowAdapters` cai para `NullFadeAdapter` quando o DI não tem fade. (Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Fade/NewScriptsFadeService.cs:L43-L137; Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/SceneTransitionService.cs:L301-L332; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/NewScriptsSceneFlowAdapters.cs:L31-L45)
2) **Loading HUD sem Strict/Release explícito + fallback silencioso**: `NewScriptsLoadingHudService` continua sem HUD quando controller/cena não existe; `SceneFlowLoadingService` apenas ignora quando não encontra serviço; ausência de `DEGRADED_MODE`. (Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/NewScriptsLoadingHudService.cs:L41-L117; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/SceneFlowLoadingService.cs:L256-L270)
3) **WorldDefinition não é obrigatória em gameplay**: `NewSceneBootstrapper` permite `worldDefinition == null` e apenas registra 0 spawn services; não há validação de spawn mínimo. (Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs:L158-L226)
4) **PostGame não falha quando Gate/InputMode faltam**: `PostGameOverlayController` apenas loga warning quando `ISimulationGateService` ou `IInputModeService` estão indisponíveis. (Assets/_ImmersiveGames/NewScripts/Gameplay/PostGame/PostGameOverlayController.cs:L320-L419)
5) **Ordem do fluxo: `RequestStart()` pode ocorrer antes de `IntroStage`**: `GameLoopSceneFlowCoordinator` chama `RequestStart()` após `transitionCompleted` + `worldResetCompleted`, mas não valida `IntroStage` concluída (contrato do ADR-0010). (Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs:L234-L279; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L110-L137)
6) **ContentSwap não respeita gates**: `ContentSwapChangeServiceInPlaceOnly` não consulta `flow.scene_transition`/`sim.gameplay`. (Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs:L31-L83; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md:L21-L24)
7) **LevelCatalog sem fail-fast**: `LevelCatalogResolver` só loga warnings quando catálogo/definição faltam; não há política Strict/Release explícita. (Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/LevelCatalogResolver.cs:L32-L193)
8) **PromotionGate default “allow” sem config real**: `PromotionGateService` cria defaults (`defaultEnabled=true`) quando não encontra config; não há decisão explícita nem logs `[OBS][PromotionGate]`. (Assets/_ImmersiveGames/NewScripts/Infrastructure/Promotion/PromotionGateService.cs:L48-L60; Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/LevelCatalogResolver.cs:L252-L262)
9) **GameplayReset com fallback por scan de cena e string-based fallback** sem política Strict/Release: o orchestrator sempre faz fallback por scan quando registry não resolve; `EaterOnly` usa fallback string-based com warning. (Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/GameplayResetOrchestrator.cs:L125-L225; Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/DefaultGameplayResetTargetClassifier.cs:L44-L51)
10) **Ausência de `DEGRADED_MODE` no runtime**: o contrato exige âncora para fallback em Release, mas não há ocorrência em código de produção dentro de `NewScripts`. (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Production-Policy-Strict-Release.md:L26-L45; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L251-L256)

### Top 5 pontos “prontos para promoção”

1) **Ordem do SceneFlow bem definida**: `SceneTransitionService` executa `FadeIn → ScenesReady → BeforeFadeOut → FadeOut → Completed` com gate antes do FadeOut. (Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/SceneTransitionService.cs:L191-L223)
2) **WorldLifecycle com observabilidade canônica**: logs `[OBS][WorldLifecycle] ResetRequested/ResetCompleted` são emitidos com `signature/profile/target/reason`. (Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Bridges/SceneFlow/WorldLifecycleSceneFlowResetDriver.cs:L220-L255)
3) **ContentSwap logs canônicos mínimos**: `ContentSwapChangeServiceInPlaceOnly` emite `[OBS][ContentSwap] ContentSwapRequested` com `mode/contentId/reason`. (Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs:L42-L58)
4) **LevelManager logs canônicos mínimos**: `LevelManager` emite `[OBS][Level] LevelChangeRequested/Started/Completed`. (Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/LevelManager.cs:L25-L61; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L148-L152)
5) **PostGame idempotente no overlay**: ações duplicadas (Restart/ExitToMenu) são ignoradas com guard `_actionRequested`. (Assets/_ImmersiveGames/NewScripts/Gameplay/PostGame/PostGameOverlayController.cs:L104-L148)

---

## 2) Tabela por ADR (0009–0019)

| ADR | Status | Implementação encontrada (arquivos) | Gaps (ideal de produção) | Evidência |
|---|---|---|---|---|
| ADR-0009 (Fade + SceneFlow) | PARCIAL | `NewScriptsFadeService`, `NewScriptsSceneFlowFadeAdapter`, `SceneTransitionService` | Sem fail-fast (Strict) e sem modo degradado explícito/`DEGRADED_MODE`; queda para `NullFadeAdapter`; não há anchors `[OBS][Fade]` (ADR aponta TODO). | Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Fade/NewScriptsFadeService.cs:L43-L137; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/NewScriptsSceneFlowAdapters.cs:L31-L45; Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/SceneTransitionService.cs:L301-L332; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0009-FadeSceneFlow.md:L59-L89 |
| ADR-0010 (Loading HUD + SceneFlow) | PARCIAL | `SceneFlowLoadingService`, `NewScriptsLoadingHudService` | Sem fail-fast e sem degraded explícito; logs não usam `[OBS][LoadingHUD]`; ADR diz que RequestStart deve ocorrer após IntroStage, mas o coordinator pode antecipar. | Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/NewScriptsLoadingHudService.cs:L41-L117; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/SceneFlowLoadingService.cs:L256-L270; Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs:L234-L279; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L110-L137 |
| ADR-0011 (WorldDefinition multi-actor) | PARCIAL | `NewSceneBootstrapper`, `WorldDefinition`, `WorldSpawnServiceFactory` | `worldDefinition` ausente não falha; ausência de verificação de spawn mínimo (Player + Eater) em gameplay. | Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs:L158-L226; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md:L48-L63 |
| ADR-0012 (PostGame) | PARCIAL | `PostGameOverlayController`, `GameLoopService` | Dependências críticas (Gate/InputMode) não fail-fast; sem degraded explícito; overlay é idempotente, mas não há política Strict/Release. | Assets/_ImmersiveGames/NewScripts/Gameplay/PostGame/PostGameOverlayController.cs:L104-L419; Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopService.cs:L171-L201; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md:L86-L151 |
| ADR-0013 (Ciclo de vida) | PARCIAL | `SceneTransitionService`, `WorldLifecycleSceneFlowResetDriver`, `GameLoopSceneFlowCoordinator` | O coordinator chama `RequestStart()` sem garantir IntroStage concluída; ADR espera jogo iniciar após gates liberados. | Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs:L234-L279; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md:L24-L38 |
| ADR-0014 (Gameplay Reset: targets/grupos) | PARCIAL | `DefaultGameplayResetTargetClassifier`, `GameplayResetOrchestrator` | Fallback por scan de cena e fallback string-based (`EaterActor`) sem política Strict/Release; falhas de target não são fail-fast. | Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/DefaultGameplayResetTargetClassifier.cs:L44-L51; Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/GameplayResetOrchestrator.cs:L125-L225; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md:L26-L73 |
| ADR-0015 (Baseline 2.0 fechamento) | ALINHADO | `Docs/Reports/Evidence/LATEST.md` (evidência canônica) | Sem gaps runtime (processual). | Assets/_ImmersiveGames/NewScripts/Docs/Reports/Evidence/LATEST.md:L1-L15 |
| ADR-0016 (ContentSwap InPlace-only) | PARCIAL | `ContentSwapChangeServiceInPlaceOnly`, `ContentSwapContextService`, bootstrap DI | Sem consulta aos gates `flow.scene_transition`/`sim.gameplay`; sem policy de bloqueio/retry/abort. | Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs:L31-L83; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md:L21-L24 |
| ADR-0017 (LevelManager + Catalog) | PARCIAL | `LevelManager`, `LevelCatalogResolver`, `ResourcesLevelCatalogProvider`, `LevelManagerInstaller` | Fail-fast ausente quando catálogo/definição faltam; comportamento Release não definido; evidência canônica LATEST não cobre LevelCatalog. | Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/LevelCatalogResolver.cs:L32-L193; Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/LevelManagerInstaller.cs:L41-L84; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0017-LevelManager-Config-Catalog.md:L10-L43 |
| ADR-0018 (Gate de promoção) | PARCIAL | `PromotionGateService`, `PromotionGateInstaller`, integração via `LevelCatalogResolver` | Gate usa defaults `defaultEnabled=true` quando não há config real; ausência de logs `[OBS][PromotionGate]` e de enforcement explícito no fluxo. | Assets/_ImmersiveGames/NewScripts/Infrastructure/Promotion/PromotionGateService.cs:L48-L60; Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/LevelCatalogResolver.cs:L252-L262; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0018-Gate-de-Promoção-Baseline2.2.md:L24-L45 |
| ADR-0019 (Promoção Baseline 2.2) | PARCIAL | Documentação do processo + LATEST (evidência canônica) | Processo documental existe, mas não há registro explícito de promoção além do LATEST (sem changelog/gates documentados no runtime). | Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0019-Promocao-Baseline2.2.md:L20-L104; Assets/_ImmersiveGames/NewScripts/Docs/Reports/Evidence/LATEST.md:L1-L15 |

---

## 3) Auditoria de invariants (Checklist A–F)

**Referência de invariants (A–F):** `Production-Policy-Strict-Release.md` e `ADR-Ideal-Completeness-Checklist.md`. (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Production-Policy-Strict-Release.md:L38-L45; Assets/_ImmersiveGames/NewScripts/Docs/Standards/ADR-Ideal-Completeness-Checklist.md:L15-L34)

| Item | Status | Evidência |
|---|---|---|
| A) Fade/LoadingHUD (Strict + Release + degraded mode) | **FAIL** | Fade/HUD seguem com warning e continuam sem feature; não há `DEGRADED_MODE`. (Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Fade/NewScriptsFadeService.cs:L43-L137; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/NewScriptsLoadingHudService.cs:L41-L117; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L251-L256)
| B) WorldDefinition (Strict + mínimo spawn) | **FAIL** | `worldDefinition == null` é permitido e não há validação de mínimo spawn. (Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs:L158-L226)
| C) LevelCatalog (Strict + Release) | **FAIL** | Resolver apenas warning quando catálogo/definição faltam; sem policy Strict/Release. (Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/LevelCatalogResolver.cs:L32-L193)
| D) PostGame (Strict + Release) | **FAIL** | Gate/InputMode ausentes não falham; apenas warnings. (Assets/_ImmersiveGames/NewScripts/Gameplay/PostGame/PostGameOverlayController.cs:L320-L419)
| E) Ordem do fluxo (RequestStart após IntroStageComplete) | **FAIL** | `GameLoopSceneFlowCoordinator` chama `RequestStart()` sem validar IntroStage; ADR-0010 indica que `RequestStart()` deve ocorrer após IntroStage concluir. (Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs:L234-L279; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L123-L137)
| F) Gates (ContentSwap respeita scene_transition e sim.gameplay) | **FAIL** | `ContentSwapChangeServiceInPlaceOnly` não consulta gates. (Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs:L31-L83)

---

## 4) Observabilidade

### Conformidade com `Observability-Contract.md`

- **OK**: `WorldLifecycleSceneFlowResetDriver` emite `[OBS][WorldLifecycle] ResetRequested/ResetCompleted` com campos canônicos. (Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Bridges/SceneFlow/WorldLifecycleSceneFlowResetDriver.cs:L220-L255)
- **OK**: `LevelManager` emite `[OBS][Level] LevelChangeRequested/Started/Completed` conforme contrato. (Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/LevelManager.cs:L25-L61; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L148-L152)
- **OK**: `ContentSwapChangeServiceInPlaceOnly` emite `[OBS][ContentSwap] ContentSwapRequested` com `mode/contentId/reason`. (Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs:L42-L58)
- **OK**: `GameLoopService` emite `[OBS][PostGame] PostGameEntered/Exited/Skipped` com assinatura e reason. (Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopService.cs:L171-L201)

### Âncoras ausentes ou inconsistentes

- **Ausente `DEGRADED_MODE` em fallback** (Fade/LoadingHUD/PostGame/InputMode/WorldDefinition). O contrato exige âncora explícita. (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L251-L256)
- **Fade/LoadingHUD sem anchors `[OBS][Fade]` / `[OBS][LoadingHUD]`** e ADRs citam TODO de evidência. (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0009-FadeSceneFlow.md:L86-L89; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L165-L168)
- **PromotionGate sem anchors `[OBS][PromotionGate]`** (apenas log de defaults). (Assets/_ImmersiveGames/NewScripts/Infrastructure/Promotion/PromotionGateService.cs:L48-L60)

---

## 5) Lista de ações sugeridas (sem código)

> Prioridade por impacto/risco. Cada ação indica ADR e checklist A–F que desbloqueia.

1) **Implementar Strict/Release explícito + `DEGRADED_MODE` para Fade e LoadingHUD** (ADR-0009/ADR-0010; Checklist A). Garantir fail-fast em Dev/QA e fallback explícito em Release. (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Production-Policy-Strict-Release.md:L14-L45; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L251-L256)
2) **Validar `WorldDefinition` + spawn mínimo (Player + Eater) em gameplay** (ADR-0011; Checklist B). Deve falhar em Strict e abortar gameplay em Release. (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md:L48-L63)
3) **PostGame: exigir Gate/InputMode em Strict + degraded explícito em Release** (ADR-0012; Checklist D). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md:L86-L151)
4) **Ordem do fluxo: postergar `RequestStart()` até IntroStage concluir** (ADR-0010/ADR-0013; Checklist E). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L123-L137)
5) **ContentSwap: respeitar `flow.scene_transition` e `sim.gameplay` com política de bloqueio/adiamento** (ADR-0016; Checklist F). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md:L21-L24)
6) **LevelCatalog: falhar cedo em Strict quando catálogo/definição faltarem** (ADR-0017; Checklist C). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0017-LevelManager-Config-Catalog.md:L10-L43)
7) **PromotionGate: carregar config real ou declarar policy explícita e observável** (ADR-0018; Checklist C/E/Promoção). (Assets/_ImmersiveGames/NewScripts/Infrastructure/Promotion/PromotionGateService.cs:L48-L60; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0018-Gate-de-Promoção-Baseline2.2.md:L24-L45)
8) **GameplayReset: definir política de fallback e validação de targets** (ADR-0014; Checklist B/C). Evitar scan silencioso em produção. (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md:L26-L73)
9) **Atualizar evidências canônicas com anchors de Fade/LoadingHUD/LevelCatalog** (ADR-0009/ADR-0010/ADR-0017; Observabilidade). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0009-FadeSceneFlow.md:L86-L89; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L165-L168)
10) **Registrar claramente processo de promoção Baseline 2.2** (ADR-0019). Garantir changelog + snapshot datado + `LATEST.md` atualizado. (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0019-Promocao-Baseline2.2.md:L20-L104; Assets/_ImmersiveGames/NewScripts/Docs/Reports/Evidence/LATEST.md:L1-L15)

---

## Referências internas consultadas

- `Docs/Standards/Production-Policy-Strict-Release.md` (Strict/Release + A–F). (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Production-Policy-Strict-Release.md:L1-L61)
- `Docs/Standards/ADR-Ideal-Completeness-Checklist.md` (completude ideal por ADR). (Assets/_ImmersiveGames/NewScripts/Docs/Standards/ADR-Ideal-Completeness-Checklist.md:L1-L34)
- `Docs/Standards/Observability-Contract.md` (anchors + DEGRADED_MODE). (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L140-L256)
- `Docs/Reports/Evidence/LATEST.md` (evidência canônica). (Assets/_ImmersiveGames/NewScripts/Docs/Reports/Evidence/LATEST.md:L1-L15)

