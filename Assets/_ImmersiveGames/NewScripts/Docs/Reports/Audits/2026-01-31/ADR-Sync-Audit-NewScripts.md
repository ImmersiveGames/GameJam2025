# ADR Sync Audit — NewScripts (ADR-0009..ADR-0019)

> **Regra operacional:** manter **1 arquivo de evidência por dia** em `Docs/Reports/Evidence/<data>/Baseline-2.2-Evidence-YYYY-MM-DD.md` e mesclar/limpar quaisquer arquivos adicionais.

**Escopo:** Assets/_ImmersiveGames/NewScripts/ (primário) e pastas secundárias quando referenciadas pelos ADRs.
**Contratos de observabilidade:** Docs/Standards/Observability-Contract.md é a fonte canônica de reasons/assinaturas; Reason-Map.md é mencionado como deprecated no contrato, mas o arquivo não foi encontrado no repo (busca por Reason-Map.md).

## 1) Sumário Executivo

### Tabela de Status (ADR x Implementação)

| ADR                          | Status   | Principais Evidências (Paths)                                                                 | Principais Gaps                                                                 | Risco  |
|------------------------------|----------|-----------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------|--------|
| ADR-0009 (Fade + SceneFlow) | OK | SceneTransitionService + FadeService + NewScriptsSceneFlowFadeAdapter + Runtime policy (IRuntimeModeProvider/IDegradedModeReporter) + logs [OBS][Fade] | Sem gaps críticos. Evidência (2026-01-31): `Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md` | Baixo |
| ADR-0010 (Loading HUD + SceneFlow) | OK      | SceneFlowLoadingService + ILoadingHudService + LoadingHudService + GlobalBootstrap                                    | Strict/Release implementado; Release com DEGRADED_MODE feature='loadinghud'; âncoras com signature+phase padronizadas ([OBS][LoadingHUD]) | Baixo  |
| ADR-0011 (WorldDefinition multi-actor) | PARCIAL | WorldDefinition + NewSceneBootstrapper + WorldSpawnServiceFactory + WorldLifecycleOrchestrator | WorldDefinition ausente não falha em gameplay; ausência de verificação de “mínimo de atores” | Médio  |
| ADR-0012 (PostGame)         | PARCIAL | PostGameOverlayController + GameRunOutcomeService + GameLoopRunEndEventBridge + Restart/ExitToMenuNavigationBridge + logs [OBS][PostGame] | Dependências críticas (InputMode/Gate) apenas warning; não fail-fast           | Médio  |
| ADR-0013 (Ciclo de vida)    | PARCIAL | SceneTransitionService + WorldLifecycleSceneFlowResetDriver + WorldLifecycleResetCompletionGate + GameLoopSceneFlowCoordinator + InputModeSceneFlowBridge | GameLoopSceneFlowCoordinator pode RequestStart() antes de IntroStage completar (diverge do contrato esperado em ADR-0010) | Médio  |
| ADR-0014 (Gameplay Reset Targets/Grupos) | PARCIAL | DefaultGameplayResetTargetClassifier + GameplayResetOrchestrator + NewSceneBootstrapper      | Falta fail-fast em targets ausentes; fallback por scan sempre habilitado       | Médio  |
| ADR-0015 (Baseline 2.0 fechamento) | OK      | Evidências e contratos em Docs/Reports/Evidence/LATEST.md e ADR                              | Não aplicável (documental)                                                      | Baixo  |
| ADR-0016 (ContentSwap InPlace-only) | PARCIAL | ContentSwapChangeServiceInPlaceOnly + ContentSwapContextService + GlobalBootstrap            | Respeito a gates (scene_transition/sim.gameplay) não aparece no serviço         | Médio  |
| ADR-0017 (LevelManager + Catalog) | PARCIAL | LevelManager + LevelCatalogResolver + ResourcesLevelCatalogProvider + LevelManagerInstaller + assets em Resources | Fail-fast para ID/config ausente não ocorre; evidência canônica ainda “TODO” no ADR | Médio  |
| ADR-0018 (Gate promoção Baseline 2.2) | PARCIAL | PromotionGateService + gating no bootstrap + contratos de ContentSwap/Level                   | Gate sempre default habilitado (sem config carregado no serviço); critérios de promoção dependem de processo/doc | Médio  |
| ADR-0019 (Promoção Baseline 2.2) | AUSENTE (no runtime) | ADR é processual (Docs/Reports/Evidence); nenhum runtime/DI específico                       | Sem implementação de processo em runtime                                        | Baixo  |

### Top Divergências / Faltas (Impacto Alto)
- (Resolvido) Loading HUD: fluxo agora falha em modo strict quando controller faltar, e o setup final inclui `LoadingHudController` na cena correta.
- IntroStage vs RequestStart: GameLoopSceneFlowCoordinator pode chamar RequestStart() antes de IntroStage completar, divergindo do contrato esperado em ADR-0013 (ordem do fluxo) (obs. no próprio ADR).
- ContentSwap sem gating: ContentSwapChangeServiceInPlaceOnly não consulta gates (scene_transition / sim.gameplay) apesar do contrato exigir respeito a gates.
- WorldDefinition opcional em gameplay: NewSceneBootstrapper aceita worldDefinition nulo; contrato pede spawn determinístico mínimo (Player/Eater).
- Level catalog fail-fast não aplicado: resolver e session logam warnings e retornam false, mas não falham; contrato pede falha explícita para IDs/config ausentes.
- Promotion gate sempre habilitado: PromotionGateService retorna defaults habilitados, sem carregamento de config (contrato de gate processual fica sem enforcement real).

## 2) Matriz Detalhada (por ADR)
Formato: cada ADR contém objetivo/contrato (docs), implementação encontrada, observabilidade, alinhamento e gaps com prioridade.

### ADR-0009 — Fade + SceneFlow
**Objetivo de produção (ideal):** Envelope determinístico fade-out → transição → fade-in, sem flicker.
**Contrato mínimo:** Fade-out antes de mutações; Completed após fade-in; fail-fast quando fade UI não existe.

**Implementação Encontrada:**
- Arquivos: `SceneTransitionService` aplica FadeIn antes das operações de cena e FadeOut após completion gate, emitindo âncoras canônicas `[OBS][Fade]`.
- `FadeService` carrega `FadeScene` (Additive), localiza `FadeController` e executa fades; falha explicitamente quando pré-condições não são atendidas.
- `NewScriptsSceneFlowFadeAdapter` resolve profiles e configura tempos de fade, aplicando policy Strict/Release e `DEGRADED_MODE` quando necessário.
- Policy Runtime (Strict/Release + reporter): `IRuntimeModeProvider` / `IDegradedModeReporter`.
- DI: `GlobalBootstrap` registra `IFadeService` e policy runtime.
- Símbolos-chave: `IFadeService`, `FadeService`, `NewScriptsSceneFlowFadeAdapter`, `SceneTransitionService`, `IRuntimeModeProvider`, `IDegradedModeReporter`.
- Fluxo de produção: `SceneTransitionService` chama `FadeIn` → `ScenesReady` → gate → `BeforeFadeOut` → `FadeOut` → `Completed`.

**Observabilidade:**
- Esperado: logs/âncoras de Fade conforme ADR e contrato de observability.
- Encontrado: Âncoras canônicas no envelope: `[OBS][Fade] FadeInStarted/FadeInCompleted/FadeOutStarted/FadeOutCompleted`. Fallback explícito em Release: `DEGRADED_MODE feature='fade' ...` (quando profile/DI/scene/controller falham).

**Alinhamento:**
- ✅ Aderente: ordem de Fade + ScenesReady + gate + FadeOut está implementada.
- ✅ Completo: fail-fast aplicado em Strict; degraded explícito em Release; observabilidade `[OBS][Fade]` presente.

**Gaps para Completar:**
- Baixa — Evidência:

- **PASS (2026-01-31):** `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md` (contem âncoras `[OBS][Fade]` e ordenação FadeIn→Ops→ScenesReady→Gate→FadeOut→Completed para startup e gameplay).

### ADR-0010 — Loading HUD + SceneFlow

**Status:** OK (fechado)

**Implementação alinhada ao ADR:**
- Separação: Infra (SceneFlow) vs Presentation (LoadingHud UI).
- Política Strict/Release:
  - Strict: validação de cena em Build Settings + erro evidente (log + Break).
  - Release: fallback explícito com `DEGRADED_MODE feature='loadinghud' ...` e HUD desabilitado.
- Observabilidade: logs canônicos com `signature` + `phase` (âncoras `[OBS][LoadingHUD]`).

**Arquivos-chave:**
- `NewScripts/Infrastructure/SceneFlow/Loading/ILoadingHudService.cs`
- `NewScripts/Infrastructure/SceneFlow/Loading/SceneFlowLoadingService.cs`
- `NewScripts/Presentation/LoadingHud/LoadingHudService.cs`
- `NewScripts/Presentation/LoadingHud/LoadingHudController.cs`
- `NewScripts/Infrastructure/GlobalBootstrap.cs`

**Evidência:**
- `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md` (seção ADR-0010)


### ADR-0013 — Ciclo de vida do jogo
**Objetivo:** Boot → Menu → Gameplay → PostGame → Restart/Exit com SceneFlow + WorldLifecycle + GameLoop.
**Contrato mínimo:** Reset só em gameplay; ResetCompleted sempre; GameLoop só joga após gates/IntroStage.

**Implementação Encontrada:**
- SceneFlow + Gate: SceneTransitionService aguarda completion gate antes do FadeOut.
- Reset determinístico: WorldLifecycleSceneFlowResetDriver publica [OBS][WorldLifecycle] ResetRequested/ResetCompleted e lida com SKIP/Failed.
- GameLoop sync: GameLoopSceneFlowCoordinator aguarda ScenesReady + ResetCompleted + Completed e em gameplay faz RequestStart().
- InputMode + IntroStage: InputModeSceneFlowBridge aplica InputMode e dispara IntroStage em SceneFlow/Completed:Gameplay.

**Observabilidade:**
- Esperado: logs de SceneFlow + ResetCompleted + IntroStage/Playing.
- Encontrado: [OBS][WorldLifecycle] em driver; [OBS][InputMode] em bridge; logs de SceneFlow em SceneTransitionService.

**Alinhamento:**
- ✅ Aderente: completion gate e reset driver respeitam ordem.
- ⚠️ Parcial: GameLoopSceneFlowCoordinator pode RequestStart() antes da IntroStage (divergência já citada no ADR-0013).

**Gaps:**
- Alta — Garantir que RequestStart() ocorra após IntroStage (contrato esperado).

### ADR-0014 — Gameplay Reset Targets/Grupos
**Objetivo:** Resets determinísticos por grupo/target.
**Contrato mínimo:** Targets idempotentes e ordenados; fail-fast se target ausente/config inconsistente.

**Implementação Encontrada:**
- Classifier: DefaultGameplayResetTargetClassifier usa ActorRegistry e fallback string-based para Eater.
- Orchestrator: GameplayResetOrchestrator tenta ActorRegistry e faz fallback por scan de cena.
- Registro de cena: NewSceneBootstrapper registra classifier e orchestrator por cena.

**Observabilidade:**
- Esperado: logs de ResetRequested/ResetCompleted com reason canônico.
- Encontrado: logs [OBS][WorldLifecycle] em driver; GameplayReset logs informativos (não [OBS]).

**Alinhamento:**
- ✅ Aderente: classificação por ActorRegistry + fallback conforme ADR.
- ⚠️ Parcial: fail-fast para targets ausentes não ocorre (fallback sempre habilitado).

**Gaps:**
- Média — Fail-fast controlado quando target inválido/config inconsistente.

### ADR-0015 — Baseline 2.0 fechamento
**Objetivo/Contrato:** Fechamento documental e evidências datadas (LATEST).
**Implementação Encontrada:** Evidências e LATEST existem no docs; natureza processual (sem runtime).

**Status:** ✅ OK (documental; sem gaps de runtime).

### ADR-0016 — ContentSwap InPlace-only
**Objetivo:** ContentSwap in-place com observabilidade; respeitar gates.
**Contrato mínimo:** Logs ContentSwapRequested/Pending/Committed/Cleared.

**Implementação Encontrada:**
- Serviço: ContentSwapChangeServiceInPlaceOnly emite [OBS][ContentSwap] ContentSwapRequested e ignora Fade/LoadingHUD.
- Contexto: ContentSwapContextService emite ContentSwapPendingSet, Committed, PendingCleared.
- Registro DI: GlobalBootstrap registra IContentSwapContextService + IContentSwapChangeService.

**Observabilidade:**
- Esperado: [OBS][ContentSwap] + context logs conforme contrato.
- Encontrado: logs em serviço + contexto conforme contrato mínimo.

**Alinhamento:**
- ✅ Aderente: modo InPlace e logs canônicos.
- ⚠️ Parcial: ausência de checagem de gates scene_transition/sim.gameplay no serviço.

**Gaps:**
- Média — Respeito explícito a gates (ou justificativa documentada).

### ADR-0017 — LevelManager + Catalog
**Objetivo:** Catalog como fonte única (DIP/SRP), sem hardcode.
**Contrato mínimo:** Fail-fast para ID/config ausente; resolução via catálogo; logs de resolução.

**Implementação Encontrada:**
- Código: LevelManager emite [OBS][Level] + usa ContentSwap.
- Resolver + Providers: LevelCatalogResolver, ResourcesLevelCatalogProvider.
- DI: LevelManagerInstaller registra providers/resolver/manager/session.
- Assets: LevelCatalog.asset e LevelDefinition_level.1/level.2 em Resources.

**Observabilidade:**
- Esperado: [OBS][Level] e [OBS][LevelCatalog] (contrato).
- Encontrado: logs [OBS][Level] no manager e [OBS][LevelCatalog] no resolver/provider.

**Alinhamento:**
- ✅ Aderente: catálogo + resolver + assets existem.
- ⚠️ Parcial: ausência de fail-fast (apenas warnings).
- ⚠️ Parcial: ADR indica evidência canônica “TODO”.

**Gaps:**
- Alta — Fail-fast para IDs/config ausentes.
- Média — Evidência canônica do catálogo em LATEST.md (ADR cita TODO).

### ADR-0018 — Gate de promoção (Baseline 2.2)
**Objetivo:** Gate processual e semântica ContentSwap vs LevelManager.
**Contrato mínimo:** Gate define critérios + LATEST.md como referência canônica.

**Implementação Encontrada:**
- PromotionGate: PromotionGateService sempre default-enabled (sem carregamento de config).
- Registro: PromotionGateInstaller é chamado no bootstrap.
- Uso: LevelCatalogResolver checa gate para plans e GlobalBootstrap usa gate para registrar ContentSwap/LevelManager.

**Observabilidade:**
- Esperado: observability alinhada ao contrato (ContentSwap/Level).
- Encontrado: logs [OBS][LevelCatalog] sobre gate missing/allow.

**Alinhamento:**
- ✅ Aderente: separação de semântica ContentSwap vs LevelManager está refletida no código.
- ⚠️ Parcial: gate sempre allow (defaults), sem config real.

**Gaps:**
- Média — Implementar leitura de config real para gates (ou explicitar modo sempre habilitado).

### ADR-0019 — Promoção Baseline 2.2
**Objetivo/Contrato:** Processo de promoção e evidência canônica.
**Implementação Encontrada:** Nenhuma em runtime (é processo de docs/evidências).

**Status:** AUSENTE (no runtime)
**Gaps:**
- Baixa — Processo permanece documental; sem automation runtime (esperado, mas explícito).

## 3) Inventário de “Componentes Órfãos” e “Documentação Órfã”
### Componentes Órfãos (Código sem ADR Explícito 0009–0019)
- WorldSpawnSmokeRunner (tool de smoke test) não aparece nos ADRs de 0009–0019; é utilitário isolado para testes locais.
- WorldResetRequestHotkeyBridge (dev hotkey) também não aparece nos ADRs do ciclo 0009–0019; é ferramenta DEV/QA.

### Documentação Órfã
- Reason-Map.md não encontrado: o contrato de observabilidade afirma que o Reason-Map é deprecated e deveria conter apenas redirect, mas o arquivo não existe no repo (busca realizada).

## 4) Checklist para o Próximo Passo (Planejamento)
### Perguntas Respondíveis pelo Próximo Log/Evidência
- Logs mostram FadeOut/FadeIn com âncoras [OBS]? (atualmente não existem).
- LevelCatalog e LevelChange aparecem no snapshot canônico LATEST?
- IntroStage sempre antecede GameLoop em Playing nos fluxos de gameplay?

### Alvos para Normalização (Áreas)
- Fail-fast controlado para Fade/LoadingHUD/WorldDefinition/LevelCatalog (evitar fallback silencioso em produção).
- Gates explícitos para ContentSwap e LevelChange (scene_transition / sim.gameplay).
- Promoção (Baseline 2.2): decidir se gate fica sempre enabled ou se haverá config real + evidências.
