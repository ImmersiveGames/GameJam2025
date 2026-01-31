# ADR Sync Audit — NewScripts (ADR-0009..ADR-0019)

Data: 2026-01-31
Escopo: `Assets/_ImmersiveGames/NewScripts/`

> **Nota de execução:** auditoria SOMENTE-LEITURA. Não foram sugeridos patches nem alterações de código.

---

## 1) Sumário executivo

### Principais divergências (por impacto)

1) **Strict/Release/DEGRADED_MODE ausentes no runtime**: Fade/LoadingHUD/PostGame caem em fallback por warnings, sem branch Strict/Release explícito e sem âncora `DEGRADED_MODE`. (Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Fade/NewScriptsFadeService.cs:L43-L137; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/NewScriptsLoadingHudService.cs:L41-L117; Assets/_ImmersiveGames/NewScripts/Gameplay/PostGame/PostGameOverlayController.cs:L320-L419; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L251-L256; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Production-Policy-Strict-Release.md:L14-L45)
2) **Ordem do fluxo**: `GameLoopSceneFlowCoordinator` chama `RequestStart()` sem evidência de que a IntroStage foi concluída; o ADR-0010 documenta que o start deve ocorrer após IntroStage completar. (Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs:L234-L279; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L110-L137)
3) **Gates de ContentSwap não são respeitados**: o serviço InPlace não consulta `flow.scene_transition` nem `sim.gameplay`, apesar do contrato do ADR-0016. (Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs:L31-L83; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md:L21-L24)
4) **WorldDefinition em gameplay sem fail-fast**: `NewSceneBootstrapper` aceita `worldDefinition == null` e não valida mínimo de spawn (Player+Eater). (Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs:L158-L226; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md:L48-L63)
5) **LevelCatalog sem policy Strict/Release**: `LevelCatalogResolver` apenas emite warnings e retorna `false` quando catálogo/definição não existem. (Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/LevelCatalogResolver.cs:L32-L193; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0017-LevelManager-Config-Catalog.md:L10-L43)

### Pontos positivos (prontos para promoção)

1) **Ordem do SceneFlow**: `SceneTransitionService` executa `FadeIn → ScenesReady → BeforeFadeOut → FadeOut → Completed`. (Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/SceneTransitionService.cs:L191-L223)
2) **WorldLifecycle observável**: logs `[OBS][WorldLifecycle] ResetRequested/ResetCompleted` com campos canônicos. (Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Bridges/SceneFlow/WorldLifecycleSceneFlowResetDriver.cs:L220-L255)
3) **ContentSwap com logs canônicos mínimos**: `[OBS][ContentSwap] ContentSwapRequested`. (Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs:L42-L58)
4) **LevelManager com logs canônicos mínimos**: `[OBS][Level] LevelChangeRequested/Started/Completed`. (Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/LevelManager.cs:L25-L61; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L148-L152)
5) **PostGame idempotente**: dupla ação de Restart/ExitToMenu é bloqueada por `_actionRequested`. (Assets/_ImmersiveGames/NewScripts/Gameplay/PostGame/PostGameOverlayController.cs:L104-L148)

---

## 2) Tabela por ADR (0009–0019)

> Status permitido: **ALINHADO | PARCIAL | DIVERGENTE**.

| ADR | Implementação encontrada (paths) | Divergências vs ideal de produção | Evidências (linhas) | Status |
|---|---|---|---|---|
| ADR-0009 (Fade + SceneFlow) | `NewScriptsFadeService`, `NewScriptsSceneFlowFadeAdapter`, `SceneTransitionService` | Sem fail-fast em Strict e sem `DEGRADED_MODE` em Release; fallback para `NullFadeAdapter`; não há logs `[OBS][Fade]` no runtime. | Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Fade/NewScriptsFadeService.cs:L43-L137; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/NewScriptsSceneFlowAdapters.cs:L31-L45; Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/SceneTransitionService.cs:L301-L332; Assets/_ImmersiveGames/NewScripts/Docs/Standards/ADR-Ideal-Completeness-Checklist.md:L15-L16 | **PARCIAL** |
| ADR-0010 (Loading HUD + SceneFlow) | `SceneFlowLoadingService`, `NewScriptsLoadingHudService` | Sem fail-fast em Strict e sem `DEGRADED_MODE` em Release; ausência de `[OBS][LoadingHUD]`; ADR-0010 registra risco de `RequestStart()` antecipado. | Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/NewScriptsLoadingHudService.cs:L41-L117; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/SceneFlowLoadingService.cs:L256-L270; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L110-L137 | **PARCIAL** |
| ADR-0011 (WorldDefinition multi-actor) | `NewSceneBootstrapper`, `WorldDefinition`, `WorldSpawnServiceFactory` | `worldDefinition` nulo é permitido mesmo em gameplay; não há validação explícita de spawn mínimo (Player+Eater). | Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs:L158-L226; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md:L48-L63 | **PARCIAL** |
| ADR-0012 (PostGame) | `PostGameOverlayController`, `GameLoopService` | Dependências críticas (Gate/InputMode) não falham em Strict; fallback sem `DEGRADED_MODE` em Release. | Assets/_ImmersiveGames/NewScripts/Gameplay/PostGame/PostGameOverlayController.cs:L320-L419; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md:L86-L151 | **PARCIAL** |
| ADR-0013 (Ciclo de vida) | `SceneTransitionService`, `WorldLifecycleSceneFlowResetDriver`, `GameLoopSceneFlowCoordinator` | `RequestStart()` não é condicionado à conclusão da IntroStage; contrato pede start pós-IntroStage. | Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs:L234-L279; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md:L24-L38 | **PARCIAL** |
| ADR-0014 (Gameplay Reset: targets/grupos) | `DefaultGameplayResetTargetClassifier`, `GameplayResetOrchestrator` | Fallback por scan de cena e fallback string-based para `EaterOnly` sem política Strict/Release explícita; falhas de target não são fail-fast. | Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/DefaultGameplayResetTargetClassifier.cs:L44-L51; Assets/_ImmersiveGames/NewScripts/Gameplay/Reset/GameplayResetOrchestrator.cs:L125-L225; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md:L26-L73 | **PARCIAL** |
| ADR-0015 (Baseline 2.0 fechamento) | `Docs/Reports/Evidence/LATEST.md` | Processo documental presente e referenciado como canônico. | Assets/_ImmersiveGames/NewScripts/Docs/Reports/Evidence/LATEST.md:L1-L15 | **ALINHADO** |
| ADR-0016 (ContentSwap InPlace-only) | `ContentSwapChangeServiceInPlaceOnly`, `ContentSwapContextService` | Não consulta gates `flow.scene_transition`/`sim.gameplay`; sem policy de bloqueio/retry/abort. | Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs:L31-L83; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md:L21-L24 | **PARCIAL** |
| ADR-0017 (LevelManager + Catalog) | `LevelManager`, `LevelCatalogResolver`, `ResourcesLevelCatalogProvider`, `LevelManagerInstaller` | Falha de catálogo/definição não é Strict fail-fast; Release não tem comportamento definido; evidência canônica não cobre LevelCatalog. | Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/LevelCatalogResolver.cs:L32-L193; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0017-LevelManager-Config-Catalog.md:L10-L43 | **PARCIAL** |
| ADR-0018 (Gate de promoção) | `PromotionGateService`, `PromotionGateInstaller` | Gate default `defaultEnabled=true` sem config real; ausência de logs `[OBS][PromotionGate]`. | Assets/_ImmersiveGames/NewScripts/Infrastructure/Promotion/PromotionGateService.cs:L48-L60; Assets/_ImmersiveGames/NewScripts/Docs/Standards/ADR-Ideal-Completeness-Checklist.md:L24-L25 | **PARCIAL** |
| ADR-0019 (Promoção Baseline 2.2) | Processo documental + `LATEST.md` | Processo documentado, mas sem evidência explícita de gate fechado além do LATEST; sem registro de promoção no runtime. | Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0019-Promocao-Baseline2.2.md:L20-L104; Assets/_ImmersiveGames/NewScripts/Docs/Reports/Evidence/LATEST.md:L1-L15 | **PARCIAL** |

---

## 3) Auditoria de invariants (Checklist A–F)

Checklist A–F conforme `Production-Policy-Strict-Release.md` e `ADR-Ideal-Completeness-Checklist.md`. (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Production-Policy-Strict-Release.md:L38-L45; Assets/_ImmersiveGames/NewScripts/Docs/Standards/ADR-Ideal-Completeness-Checklist.md:L15-L34)

| Item | PASS/FAIL | Evidências |
|---|---|---|
| A) Fade/LoadingHUD | **FAIL** | Não há branch Strict/Release nem `DEGRADED_MODE`; fallback por warning no runtime. (Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Fade/NewScriptsFadeService.cs:L43-L137; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/NewScriptsLoadingHudService.cs:L41-L117; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L251-L256)
| B) WorldDefinition | **FAIL** | `worldDefinition` pode ser nulo em gameplay e não há validação de spawn mínimo. (Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs:L158-L226)
| C) LevelCatalog | **FAIL** | Ausência de catálogo/definição apenas gera warning; não há Strict fail-fast nem política Release explícita. (Assets/_ImmersiveGames/NewScripts/Gameplay/Levels/Resolvers/LevelCatalogResolver.cs:L32-L193)
| D) PostGame | **FAIL** | Gate/InputMode indisponíveis só geram warning; não há `DEGRADED_MODE`. (Assets/_ImmersiveGames/NewScripts/Gameplay/PostGame/PostGameOverlayController.cs:L320-L419; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L251-L256)
| E) Ordem do fluxo | **FAIL** | `GameLoopSceneFlowCoordinator` chama `RequestStart()` sem evidência de IntroStage concluída; ADR-0010 explicita que start deve ocorrer após IntroStage completar. (Assets/_ImmersiveGames/NewScripts/Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs:L234-L279; Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L123-L137)
| F) ContentSwap (gates) | **FAIL** | `ContentSwapChangeServiceInPlaceOnly` não consulta `flow.scene_transition`/`sim.gameplay`. (Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs:L31-L83)

---

## 4) Observabilidade

### Ocorrência de `DEGRADED_MODE`

- **Definição existe apenas nos standards**; não há uso explícito no runtime observado nas features auditadas. (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L251-L256; Assets/_ImmersiveGames/NewScripts/Docs/Standards/Production-Policy-Strict-Release.md:L26-L45)
- **Fallbacks atuais** usam warnings sem âncora `DEGRADED_MODE` (Fade/LoadingHUD/PostGame). (Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Fade/NewScriptsFadeService.cs:L43-L137; Assets/_ImmersiveGames/NewScripts/Infrastructure/SceneFlow/Loading/NewScriptsLoadingHudService.cs:L41-L117; Assets/_ImmersiveGames/NewScripts/Gameplay/PostGame/PostGameOverlayController.cs:L320-L419)

### Logs `[OBS][Fade]`, `[OBS][LoadingHUD]`, `[OBS][PromotionGate]`

- **Não há logs `[OBS][Fade]`/`[OBS][LoadingHUD]` no runtime**; o checklist ideal os menciona como requisito. (Assets/_ImmersiveGames/NewScripts/Docs/Standards/ADR-Ideal-Completeness-Checklist.md:L15-L16)
- **`PromotionGateService`** não emite logs `[OBS][PromotionGate]` (apenas log genérico de defaults). (Assets/_ImmersiveGames/NewScripts/Infrastructure/Promotion/PromotionGateService.cs:L48-L60)

---

## 5) Ações sugeridas (sem código)

> Priorização por risco/impacto. Cada item indica ADR(s) e o checklist A–F que desbloqueia.

1) **Definir política Strict/Release e âncora `DEGRADED_MODE`** para Fade e LoadingHUD (ADR-0009/ADR-0010; Checklist A). (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Production-Policy-Strict-Release.md:L14-L45)
2) **WorldDefinition em gameplay deve ser obrigatória + validação de spawn mínimo** (ADR-0011; Checklist B). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0011-WorldDefinition-MultiActor-GameplayScene.md:L48-L63)
3) **PostGame: Gate/InputMode como pré-condições em Strict + degraded explícito em Release** (ADR-0012; Checklist D). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md:L86-L151)
4) **Ordem de fluxo: RequestStart após IntroStageComplete** (ADR-0010/ADR-0013; Checklist E). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0010-LoadingHud-SceneFlow.md:L123-L137)
5) **ContentSwap: checagem de gates + policy de bloqueio/adiamento/abort** (ADR-0016; Checklist F). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0016-ContentSwap-WorldLifecycle.md:L21-L24)
6) **LevelCatalog: fail-fast em Strict e comportamento Release definido** (ADR-0017; Checklist C). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0017-LevelManager-Config-Catalog.md:L10-L43)
7) **PromotionGate: config real ou policy “always on” observável** com logs canônicos (ADR-0018). (Assets/_ImmersiveGames/NewScripts/Docs/Standards/ADR-Ideal-Completeness-Checklist.md:L24-L25)
8) **GameplayReset: política explícita para fallback por scan/strings** (ADR-0014; Checklist B/C). (Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md:L26-L73)

---

## Referências internas consultadas

- `Docs/Standards/Production-Policy-Strict-Release.md` (Strict/Release + A–F). (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Production-Policy-Strict-Release.md:L1-L61)
- `Docs/Standards/ADR-Ideal-Completeness-Checklist.md` (completude ideal por ADR). (Assets/_ImmersiveGames/NewScripts/Docs/Standards/ADR-Ideal-Completeness-Checklist.md:L1-L34)
- `Docs/Standards/Observability-Contract.md` (anchors + DEGRADED_MODE). (Assets/_ImmersiveGames/NewScripts/Docs/Standards/Observability-Contract.md:L140-L256)
- `Docs/Reports/Evidence/LATEST.md` (evidência canônica). (Assets/_ImmersiveGames/NewScripts/Docs/Reports/Evidence/LATEST.md:L1-L15)
