ADR Sync Audit — NewScripts (ADR-0009..ADR-0019)
Escopo: Assets/_ImmersiveGames/NewScripts/ (primário) e pastas secundárias quando referenciadas pelos ADRs.
Contratos de observabilidade: Docs/Reports/Observability-Contract.md é a fonte canônica de reasons/assinaturas; Reason-Map.md é mencionado como deprecated no contrato, mas o arquivo não foi encontrado no repo (busca por Reason-Map.md).

1) Sumário executivo
   Tabela de status (ADR x Implementação)
   ADR	Status	Principais evidências (paths)	Principais gaps	Risco
   ADR-0009 (Fade + SceneFlow)	RISCO	SceneTransitionService + NewScriptsFadeService + NewScriptsSceneFlowFadeAdapter + GlobalBootstrap	Fail-fast não aplicado (continua sem fade); adapter pode virar NullFade; ausência de âncoras de log explícitas para Fade	Alto
   ADR-0010 (Loading HUD + SceneFlow)	RISCO	SceneFlowLoadingService, NewScriptsLoadingHudService, GlobalBootstrap	HUD não fail-fast; continua sem HUD sem modo degradado explícito; logs não são [OBS] no contrato	Alto
   ADR-0011 (WorldDefinition multi-actor)	PARCIAL	WorldDefinition, NewSceneBootstrapper, WorldSpawnServiceFactory, WorldLifecycleOrchestrator	WorldDefinition ausente não falha em gameplay; ausência de verificação de “mínimo de atores”	Médio
   ADR-0012 (PostGame)	PARCIAL	PostGameOverlayController, GameRunOutcomeService, GameLoopRunEndEventBridge, Restart/ExitToMenuNavigationBridge, logs [OBS][PostGame]	Dependências críticas (InputMode/Gate) apenas warning; não fail-fast	Médio
   ADR-0013 (Ciclo de vida)	PARCIAL	SceneTransitionService, WorldLifecycleSceneFlowResetDriver, WorldLifecycleResetCompletionGate, GameLoopSceneFlowCoordinator, InputModeSceneFlowBridge	GameLoopSceneFlowCoordinator pode RequestStart() antes de IntroStage completar (diverge do contrato esperado em ADR-0010)	Médio
   ADR-0014 (Gameplay Reset Targets/Grupos)	PARCIAL	DefaultGameplayResetTargetClassifier, GameplayResetOrchestrator, NewSceneBootstrapper	Falta fail-fast em targets ausentes; fallback por scan sempre habilitado	Médio
   ADR-0015 (Baseline 2.0 fechamento)	OK	Evidências e contratos em Docs/Reports/Evidence/LATEST.md e ADR	Não aplicável (documental)	Baixo
   ADR-0016 (ContentSwap InPlace-only)	PARCIAL	ContentSwapChangeServiceInPlaceOnly, ContentSwapContextService, GlobalBootstrap	Respeito a gates (scene_transition/sim.gameplay) não aparece no serviço	Médio
   ADR-0017 (LevelManager + Catalog)	PARCIAL	LevelManager, LevelCatalogResolver, ResourcesLevelCatalogProvider, LevelManagerInstaller, assets em Resources	Fail-fast para ID/config ausente não ocorre; evidência canônica ainda “TODO” no ADR	Médio
   ADR-0018 (Gate promoção Baseline 2.2)	PARCIAL	PromotionGateService + gating no bootstrap + contratos de ContentSwap/Level	Gate sempre default habilitado (sem config carregado no serviço); critérios de promoção dependem de processo/doc	Médio
   ADR-0019 (Promoção Baseline 2.2)	AUSENTE (no runtime)	ADR é processual (Docs/Reports/Evidence); nenhum runtime/DI específico	Sem implementação de processo em runtime	Baixo
   Top divergências / faltas (impacto alto)
   Fail-fast não cumprido em Fade e Loading HUD: serviços seguem sem UI quando referências faltam (contrato pede falha explícita em dev/QA).

IntroStage vs RequestStart: GameLoopSceneFlowCoordinator pode chamar RequestStart() antes de IntroStage completar, divergindo do contrato esperado em ADR-0010 (obs. no próprio ADR).

ContentSwap sem gating: ContentSwapChangeServiceInPlaceOnly não consulta gates (scene_transition / sim.gameplay) apesar do contrato exigir respeito a gates.

WorldDefinition opcional em gameplay: NewSceneBootstrapper aceita worldDefinition nulo; contrato pede spawn determinístico mínimo (Player/Eater).

Level catalog fail-fast não aplicado: resolver e session logam warnings e retornam false, mas não falham; contrato pede falha explícita para IDs/config ausentes.

Promotion gate sempre habilitado: PromotionGateService retorna defaults habilitados, sem carregamento de config (contrato de gate processual fica sem enforcement real).

2) Matriz detalhada (por ADR)
   Formato: cada ADR contém objetivo/contrato (docs), implementação encontrada, observabilidade, alinhamento e gaps com prioridade.

ADR-0009 — Fade + SceneFlow
Objetivo de produção (ideal): envelope determinístico fade-out → transição → fade-in, sem flicker.
Contrato mínimo: fade-out antes de mutações; Completed após fade-in; fail-fast quando fade UI não existe.

Implementação encontrada

Arquivos:

SceneTransitionService aplica FadeIn antes das operações de cena e FadeOut após completion gate.

NewScriptsFadeService carrega FadeScene, localiza NewScriptsFadeController e executa fades.

NewScriptsSceneFlowFadeAdapter resolve profiles e configura tempos de fade, com fallback para NullFadeAdapter.

DI: GlobalBootstrap registra INewScriptsFadeService e SceneTransitionService com gate de WorldLifecycle.

Símbolos-chave: INewScriptsFadeService, NewScriptsFadeService, NewScriptsSceneFlowFadeAdapter, SceneTransitionService.

Registro DI: RegisterSceneFlowFadeModule() + RegisterSceneFlowNative() no GlobalBootstrap.

Fluxo de produção: SceneTransitionService chama FadeIn → ScenesReady → gate → BeforeFadeOut → FadeOut → Completed.

Observabilidade

Esperado: logs/âncoras de Fade conforme ADR e contrato de observability (ainda “TODO” no ADR).

Encontrado: logs [Fade] em NewScriptsFadeService e logs de profile em NewScriptsSceneFlowFadeAdapter (não [OBS]).

Alinhamento

✅ Aderente: ordem de Fade + ScenesReady + gate + FadeOut está implementada.

⚠️ Parcial: observabilidade do fade não segue anchors [OBS] e ADR diz “TODO”.

❌ Divergente: fail-fast não aplicado; continua sem fade se controller não existe ou se não houver serviço no DI. Isso é fallback sem modo degradado explícito (RISCO).

Gaps para completar

Alta — Fail-fast configurável: ausência de FadeScene/NewScriptsFadeController deve falhar (ou exigir modo degradado explícito com log âncora). Onde: NewScriptsFadeService, NewScriptsSceneFlowAdapters. Critério de pronto: erro explícito em dev/QA; modo degradado apenas quando configurado.

Média — Âncoras de observabilidade para Fade ([OBS]) alinhadas ao contrato. Onde: NewScriptsFadeService/SceneTransitionService. Critério de pronto: logs canônicos presentes e documentados.

ADR-0010 — Loading HUD + SceneFlow
Objetivo de produção: feedback de loading sem acoplar ao fluxo, determinístico e observável.
Contrato mínimo: HUD opcional, ordem correta (fade → loading → ready → fade-out), fail-fast em dev/QA; fallback só com configuração explícita.

Implementação encontrada

Arquivos:

SceneFlowLoadingService orquestra etapas Started/FadeInCompleted/ScenesReady/BeforeFadeOut/Completed.

NewScriptsLoadingHudService carrega LoadingHudScene e controla NewScriptsLoadingHudController.

Registro no DI via GlobalBootstrap (INewScriptsLoadingHudService + SceneFlowLoadingService).

Fluxo de produção: HUD “Ensure” no Started; Show após FadeIn; Hide antes do FadeOut; safety hide no Completed.

Observabilidade

Esperado: logs de estado e etapas (ADR).

Encontrado: logs [Loading] e [LoadingHUD] (sem prefixo [OBS]).

Alinhamento

✅ Aderente: ordem de Show/Hide está conforme ADR.

⚠️ Parcial: logs não estão no formato canônico [OBS].

❌ Divergente: fallback silencioso quando HUD/controller faltam, sem modo degradado explícito (RISCO).

Gaps

Alta — Fail-fast em dev/QA quando LoadingHudScene/controller não existe; fallback apenas via config explícita.

Média — Padronizar logs com assinatura [OBS] se exigido pelo contrato.

ADR-0011 — WorldDefinition multi-actor
Objetivo: WorldDefinition declara Player + Eater determinístico via reset pipeline.
Contrato mínimo: spawn via WorldLifecycle, ActorRegistry atualizado, fail-fast em ausência de refs.

Implementação encontrada

Arquivos: WorldDefinition (ScriptableObject), NewSceneBootstrapper registra spawn services via definition, WorldSpawnServiceFactory cria Player/Eater, WorldLifecycleOrchestrator loga ActorRegistry counts.

Fluxo: NewSceneBootstrapper registra spawn services por WorldDefinition; WorldLifecycleOrchestrator despawn/spawn e loga contagens.

Observabilidade

Esperado: ActorRegistry mínimo (ex.: 2) em logs.

Encontrado: logs de contagem no orchestrator (ActorRegistry count at 'After Spawn').

Alinhamento

✅ Aderente: assets e pipeline de spawn existem, incluindo Player/Eater.

⚠️ Parcial: worldDefinition pode ser nula sem erro (em gameplay isso mascara problema).

⚠️ Parcial: não há validação explícita de “mínimo de atores”.

Gaps

Alta — Validar presença de WorldDefinition em gameplay (fail-fast ou gate).

Média — Check explícito de mínimos (Player + Eater) com log âncora.

ADR-0012 — Fluxo pós-gameplay (PostGame)
Objetivo: PostGame idempotente via eventos, overlay + gating/input, Restart/ExitToMenu.
Contrato mínimo: entrada via evento, gate sim.gameplay, Restart via SceneFlow/WorldLifecycle, ExitToMenu via frontend.

Implementação encontrada

Overlay e intents: PostGameOverlayController publica GameResetRequestedEvent/GameExitToMenuRequestedEvent e usa gate state.postgame + InputMode.

Eventos de fim de run: GameRunOutcomeService publica GameRunEndedEvent de forma idempotente e só em Playing.

Bridge para GameLoop: GameLoopRunEndEventBridge converte GameRunEndedEvent → RequestEnd().

Navegação: RestartNavigationBridge e ExitToMenuNavigationBridge acionam IGameNavigationService.

Observabilidade

Esperado: [OBS][PostGame] + logs do overlay (contrato).

Encontrado: [OBS][PostGame] em GameLoopService e logs [PostGame] no overlay.

Alinhamento

✅ Aderente: eventos e navegação canonizados (Restart/ExitToMenu) com logs.

⚠️ Parcial: dependências críticas (gate/input) apenas warning (não fail-fast).

Gaps

Média — Fail-fast controlado em dev/QA para dependências de UI/gate (conforme ADR).

ADR-0013 — Ciclo de vida do jogo
Objetivo: Boot → Menu → Gameplay → PostGame → Restart/Exit com SceneFlow + WorldLifecycle + GameLoop.
Contrato mínimo: reset só em gameplay; ResetCompleted sempre; GameLoop só joga após gates/IntroStage.

Implementação encontrada

SceneFlow + Gate: SceneTransitionService aguarda completion gate antes do FadeOut.

Reset determinístico: WorldLifecycleSceneFlowResetDriver publica [OBS][WorldLifecycle] ResetRequested/ResetCompleted e lida com SKIP/Failed.

GameLoop sync: GameLoopSceneFlowCoordinator aguarda ScenesReady + ResetCompleted + Completed e em gameplay faz RequestStart().

InputMode + IntroStage: InputModeSceneFlowBridge aplica InputMode e dispara IntroStage em SceneFlow/Completed:Gameplay.

Observabilidade

Esperado: logs de SceneFlow + ResetCompleted + IntroStage/Playing.

Encontrado: [OBS][WorldLifecycle] em driver; [OBS][InputMode] em bridge; logs de SceneFlow em SceneTransitionService.

Alinhamento

✅ Aderente: completion gate e reset driver respeitam ordem.

⚠️ Parcial: GameLoopSceneFlowCoordinator pode RequestStart() antes da IntroStage (divergência já citada no ADR-0010).

Gaps

Alta — Garantir que RequestStart() ocorra após IntroStage (contrato esperado).

ADR-0014 — Gameplay Reset Targets/Grupos
Objetivo: resets determinísticos por grupo/target.
Contrato mínimo: targets idempotentes e ordenados; fail-fast se target ausente/config inconsistente.

Implementação encontrada

Classifier: DefaultGameplayResetTargetClassifier usa ActorRegistry e fallback string-based para Eater.

Orchestrator: GameplayResetOrchestrator tenta ActorRegistry e faz fallback por scan de cena.

Registro de cena: NewSceneBootstrapper registra classifier e orchestrator por cena.

Observabilidade

Esperado: logs de ResetRequested/ResetCompleted com reason canônico.

Encontrado: logs [OBS][WorldLifecycle] em driver; GameplayReset logs informativos (não [OBS]).

Alinhamento

✅ Aderente: classificação por ActorRegistry + fallback conforme ADR.

⚠️ Parcial: fail-fast para targets ausentes não ocorre (fallback sempre habilitado).

Gaps

Média — Fail-fast controlado quando target inválido/config inconsistente.

ADR-0015 — Baseline 2.0 fechamento
Objetivo/Contrato: fechamento documental e evidências datadas (LATEST).
Implementação encontrada: evidências e LATEST existem no docs; natureza processual (sem runtime).

Status: ✅ OK (documental; sem gaps de runtime).

ADR-0016 — ContentSwap InPlace-only
Objetivo: ContentSwap in-place com observabilidade; respeitar gates.
Contrato mínimo: logs ContentSwapRequested/Pending/Committed/Cleared.

Implementação encontrada

Serviço: ContentSwapChangeServiceInPlaceOnly emite [OBS][ContentSwap] ContentSwapRequested e ignora Fade/LoadingHUD.

Contexto: ContentSwapContextService emite ContentSwapPendingSet, Committed, PendingCleared.

Registro DI: GlobalBootstrap registra IContentSwapContextService + IContentSwapChangeService.

Observabilidade

Esperado: [OBS][ContentSwap] + context logs conforme contrato.

Encontrado: logs em serviço + contexto conforme contrato mínimo.

Alinhamento

✅ Aderente: modo InPlace e logs canônicos.

⚠️ Parcial: ausência de checagem de gates scene_transition/sim.gameplay no serviço.

Gaps

Média — Respeito explícito a gates (ou justificativa documentada).

ADR-0017 — LevelManager + Catalog
Objetivo: catalog como fonte única (DIP/SRP), sem hardcode.
Contrato mínimo: fail-fast para ID/config ausente; resolução via catálogo; logs de resolução.

Implementação encontrada

Código: LevelManager emite [OBS][Level] + usa ContentSwap.

Resolver + Providers: LevelCatalogResolver, ResourcesLevelCatalogProvider.

DI: LevelManagerInstaller registra providers/resolver/manager/session.

Assets: LevelCatalog.asset e LevelDefinition_level.1/level.2 em Resources.

Observabilidade

Esperado: [OBS][Level] e [OBS][LevelCatalog] (contrato).

Encontrado: logs [OBS][Level] no manager e [OBS][LevelCatalog] no resolver/provider.

Alinhamento

✅ Aderente: catálogo + resolver + assets existem.

⚠️ Parcial: ausência de fail-fast (apenas warnings).

⚠️ Parcial: ADR indica evidência canônica “TODO”.

Gaps

Alta — Fail-fast para IDs/config ausentes.

Média — Evidência canônica do catálogo em LATEST.md (ADR cita TODO).

ADR-0018 — Gate de promoção (Baseline 2.2)
Objetivo: gate processual e semântica ContentSwap vs LevelManager.
Contrato mínimo: gate define critérios + LATEST.md como referência canônica.

Implementação encontrada

PromotionGate: PromotionGateService sempre default-enabled (sem carregamento de config).

Registro: PromotionGateInstaller é chamado no bootstrap.

Uso: LevelCatalogResolver checa gate para plans e GlobalBootstrap usa gate para registrar ContentSwap/LevelManager.

Observabilidade

Esperado: observability alinhada ao contrato (ContentSwap/Level).

Encontrado: logs [OBS][LevelCatalog] sobre gate missing/allow.

Alinhamento

✅ Aderente: separação de semântica ContentSwap vs LevelManager está refletida no código.

⚠️ Parcial: gate sempre allow (defaults), sem config real.

Gaps

Média — Implementar leitura de config real para gates (ou explicitar modo sempre habilitado).

ADR-0019 — Promoção Baseline 2.2
Objetivo/Contrato: processo de promoção e evidência canônica.
Implementação encontrada: nenhuma em runtime (é processo de docs/evidências).

Status: AUSENTE (no runtime)
Gaps:

Baixa — Processo permanece documental; sem automation runtime (esperado, mas explícito).

3) Inventário de “componentes órfãos” e “documentação órfã”
   Componentes órfãos (código sem ADR explícito 0009–0019)
   WorldSpawnSmokeRunner (tool de smoke test) não aparece nos ADRs de 0009–0019; é utilitário isolado para testes locais.

WorldResetRequestHotkeyBridge (dev hotkey) também não aparece nos ADRs do ciclo 0009–0019; é ferramenta DEV/QA.

Documentação órfã
Reason-Map.md não encontrado: o contrato de observabilidade afirma que o Reason-Map é deprecated e deveria conter apenas redirect, mas o arquivo não existe no repo (busca realizada).

4) Checklist para o próximo passo (planejamento)
   Perguntas respondíveis pelo próximo log/evidência

Logs mostram FadeOut/FadeIn com âncoras [OBS]? (atualmente não existem).

LevelCatalog e LevelChange aparecem no snapshot canônico LATEST?

IntroStage sempre antecede GameLoop em Playing nos fluxos de gameplay?

Alvos para normalização (áreas)

Fail-fast controlado para Fade/LoadingHUD/WorldDefinition/LevelCatalog (evitar fallback silencioso em produção).

Gates explícitos para ContentSwap e LevelChange (scene_transition / sim.gameplay).

Promoção (Baseline 2.2): decidir se gate fica sempre enabled ou se haverá config real + evidências.
