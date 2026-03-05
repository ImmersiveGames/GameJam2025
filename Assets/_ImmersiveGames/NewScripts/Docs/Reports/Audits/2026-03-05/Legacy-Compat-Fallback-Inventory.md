# Inventário H0 — LEGADO / COMPAT / FALLBACK no runtime (NewScripts)

Data: 2026-03-05
Escopo analisado: `Assets/_ImmersiveGames/NewScripts/**` (foco em Navigation / SceneFlow / LevelFlow / WorldLifecycle / Composition)
Fonte de verdade: código do repositório atual.

## A) Sumário executivo

1. O maior risco de reintrodução de mistura rota/level está no **restart por caminho de compat** em `GameNavigationService.RestartAsync`, quando cai em `resolveSource='legacy_route_only'` e usa somente `_lastGameplayRouteId` sem `LevelId` canônico.
2. Outro ponto crítico é `GameNavigationService.StartGameplayRouteAsync`, que ainda aplica fallback para `snapshot` ou `_lastStartedGameplayLevelId` quando não recebe seleção explícita suficiente.
3. `RestartNavigationBridge` possui fallback operacional para `IGameNavigationService.RestartAsync` caso `ILevelFlowRuntimeService` esteja ausente; isso mantém fluxo vivo, mas reabre caminho não-canônico.
4. Em SceneFlow, `MacroLevelPrepareCompletionGate` pode **pular LevelPrepare** se DI/serviço não estiver disponível, reduzindo garantias do pipeline canônico antes do fade-out.
5. Em WorldLifecycle, `WorldLifecycleSceneFlowResetDriver` opera em modo **best-effort** e pode publicar completion em SKIP/fallback, evitando deadlock mas permitindo continuidade com reset degradado.
6. Existem APIs marcadas `[Obsolete]` em navegação (string/legacy entrypoints) ainda expostas por interface de runtime.
7. `LevelDefinition` mantém campos legados (`routeId`, `routeRef`) por compat/migração; embora o caminho canônico seja `macroRouteRef`, há superfície de regressão de configuração.
8. `GameplayStartSnapshot.ContextSignature` está obsoleto, mas preservado para compatibilidade.
9. Há várias migrações `UNITY_EDITOR` (catalogs/normalizers) que não executam em runtime, porém podem manter serializações híbridas se processo editorial for inconsistente.
10. O ecossistema inclui componentes “degraded/best-effort” (fade/readiness/world reset) úteis para robustez, mas que devem ser monitorados para não mascarar quebra de contrato canônico.

### Top 5 HIGH risk

1. `GameNavigationService.RestartAsync` (`resolveSource='legacy_route_only'`).
   Evidência: `Modules/Navigation/GameNavigationService.cs` (`GameNavigationService.RestartAsync`).
2. `GameNavigationService.StartGameplayRouteAsync` (fallback para snapshot/last_level_id).
   Evidência: `Modules/Navigation/GameNavigationService.cs` (`GameNavigationService.StartGameplayRouteAsync`).
3. `RestartNavigationBridge.OnResetRequested` (fallback para `IGameNavigationService.RestartAsync`).
   Evidência: `Modules/Navigation/RestartNavigationBridge.cs`.
4. `MacroLevelPrepareCompletionGate.AwaitBeforeFadeOutAsync` (skip de LevelPrepare quando dependência ausente).
   Evidência: `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs`.
5. `WorldLifecycleSceneFlowResetDriver.HandleScenesReadyAsync` / `ExecuteResetWhenRequiredAsync` (best-effort + fallback completion).
   Evidência: `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`.

### Recomendações H1 Hardening (somente sugestão)

- H1.1: Remover fallback `legacy_route_only` de restart em runtime de produção (permitir somente snapshot/level canônico).
- H1.2: Encerrar exposição de entrypoints `[Obsolete]` em interfaces globais (manter apenas adapters internos/editor).
- H1.3: Converter skips críticos (`LevelPrepare skipped`, `Reset service ausente`) em fail-fast configurável por ambiente.
- H1.4: Adicionar telemetria de contagem/alerta para qualquer caminho `Compat`/`FallbackApplied`/`DEGRADED_MODE`.
- H1.5: Consolidar políticas de migração editor-only com validação obrigatória no CI de assets (evitar regressão serializada).

---

## B) Tabela principal (ordenada por risco)

| Risk | Category | Symbol | FilePath | Callers (top 3) | Notes |
|---|---|---|---|---|---|
| HIGH | RuntimeFallback | `GameNavigationService.RestartAsync` | `Modules/Navigation/GameNavigationService.cs` | `RestartNavigationBridge.OnResetRequested`; `IGameNavigationService.RestartAsync` (interface pública); callers adicionais não encontrados | Pode resolver restart por rota legada (`legacy_route_only`) sem `LevelId` canônico. |
| HIGH | RuntimeFallback | `GameNavigationService.StartGameplayRouteAsync` | `Modules/Navigation/GameNavigationService.cs` | `LevelFlowRuntimeService.StartGameplayAsync`; `LevelFlowRuntimeService.RestartLastGameplayAsync`; `GameNavigationService.RestartAsync` | Fallback para snapshot/último level pode mascarar ausência de seleção explícita. |
| HIGH | RuntimeFallback | `RestartNavigationBridge.OnResetRequested` | `Modules/Navigation/RestartNavigationBridge.cs` | `EventBus<GameResetRequestedEvent>` (binding no construtor); callers diretos adicionais não encontrados | Se `ILevelFlowRuntimeService` faltar, volta para `IGameNavigationService.RestartAsync`. |
| HIGH | BestEffort | `MacroLevelPrepareCompletionGate.AwaitBeforeFadeOutAsync` | `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs` | `SceneTransitionService.AwaitCompletionGateAsync` (via `ISceneTransitionCompletionGate`); gate registrado em `GlobalCompositionRoot.SceneFlowWorldLifecycle.RegisterSceneFlowNative`; callers adicionais não encontrados | Pode pular LevelPrepare em ausência de DI/serviço, mantendo fluxo visual. |
| HIGH | BestEffort | `WorldLifecycleSceneFlowResetDriver.HandleScenesReadyAsync` / `ExecuteResetWhenRequiredAsync` | `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs` | `EventBus<SceneTransitionScenesReadyEvent>`; registro em `GlobalCompositionRoot.Pipeline.InstallWorldLifecycleServices`; callers adicionais não encontrados | Driver libera completion em SKIP/fallback para evitar deadlock, mas reset pode não executar. |
| MED | DeprecatedAPI | `IGameNavigationService.StartGameplayAsync(LevelId)` e `GameNavigationService.StartGameplayAsync(LevelId)` | `Modules/Navigation/IGameNavigationService.cs`, `Modules/Navigation/GameNavigationService.cs` | Tipado na interface global; callers explícitos no escopo principal não encontrados (uso canônico é `ILevelFlowRuntimeService`) | Entry point legado ainda disponível para consumo indevido. |
| MED | DeprecatedAPI | `IGameNavigationService.NavigateAsync(string)` / `RequestMenuAsync` / `RequestGameplayAsync` | `Modules/Navigation/IGameNavigationService.cs`, `Modules/Navigation/GameNavigationService.cs` | callers não encontrados para overload string no escopo alvo | API string-based mantém trilho paralelo ao intent tipado. |
| MED | CompatShim | `LevelDefinition.routeId` / `routeRef` (legado) | `Modules/LevelFlow/Runtime/LevelDefinition.cs` | `LevelCatalogAsset.TryMigrateLegacyRouteRefsInEditor`; `LevelCatalogAsset.TryMigrateMacroRouteRefsInEditor`; uso runtime direto não encontrado | Campos legados podem reintroduzir configuração híbrida route/level. |
| MED | RuntimeFallback | `LevelCatalogAsset.TryResolveLevelId(SceneRouteId, out LevelId)` | `Modules/LevelFlow/Bindings/LevelCatalogAsset.cs` | callers não encontrados | Em macro route ambígua, método permanece best-effort para compat e pode falhar sem sinal forte ao chamador. |
| MED | DeprecatedAPI | `GameplayStartSnapshot.ContextSignature` (`[Obsolete]`) | `Modules/LevelFlow/Runtime/GameplayStartSnapshot.cs` | callers explícitos não encontrados | Mantém alias semântico legado (context x level signature). |
| MED | RuntimeFallback | `DefaultGameplaySceneClassifier.IsGameplayScene` (fallback por nome de cena) | `Modules/SceneFlow/Readiness/Runtime/DefaultGameplaySceneClassifier.cs` | resolução via DI de `IGameplaySceneClassifier`; callers diretos adicionais não encontrados | Sem marker, classifica gameplay por nome fixo (`GameplayScene`). |
| MED | RuntimeFallback | `WorldLifecycleControllerLocator.FindSingleForSceneOrFallback` | `Modules/WorldLifecycle/Runtime/WorldLifecycleControllerLocator.cs` | callers externos não encontrados | Pode selecionar controller único fora do matching estrito por cena. |
| MED | CompatShim | `ProductionWorldResetPolicy.AllowLegacyActorKindFallback` | `Modules/WorldLifecycle/WorldRearm/Policies/ProductionWorldResetPolicy.cs` | `RunRearmOrchestrator.ExecuteAsync` | Permite fallback string-based em descoberta de atores (`EaterFallbackUsed`). |
| MED | BestEffort | `GlobalCompositionRoot.FadeLoading` -> `new DegradedFadeService(...)` | `Infrastructure/Composition/GlobalCompositionRoot.FadeLoading.cs` | `HandleFadeSetupFailure`; `HandleFadeRuntimeFailure`; callers externos não encontrados | Em erro de fade, sistema continua com serviço degradado. |
| MED | RuntimeFallback | `GlobalCompositionRoot.InitializeReadinessGate` (resolver gateService fallback) | `Infrastructure/Composition/GlobalCompositionRoot.PauseReadiness.cs` | instalação de pipeline de composição | Best-effort reduz quebra imediata, mas pode deixar readiness sem proteção. |
| MED | RuntimeFallback | `SceneScopeCompositionRoot.EnsureWorldRoot` (fallback para ActiveScene) | `Infrastructure/Composition/SceneScopeCompositionRoot.cs` | fluxo interno de bootstrap de escopo de cena | Cena inválida usa ActiveScene; risco de root em cena não intencional. |
| LOW | ValidationOnly | `TransitionStyleCatalogAsset.transitionProfileCatalog` (Legacy / Validation-only) | `Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs` | validações editor e composição | Campo legado existe para YAML/editor; runtime usa `profileRef` direto. |
| LOW | ValidationOnly | `GlobalCompositionRoot.RegisterSceneFlowTransitionProfiles` (compat validation) | `Infrastructure/Composition/GlobalCompositionRoot.SceneFlowTransitionProfiles.cs` | pipeline de composição | Registro mantido só para validação de cobertura de profiles. |
| LOW | EditorOnlyMigration | `LevelCatalogAsset.TryMigrateLegacyRouteRefsInEditor` / `TryMigrateMacroRouteRefsInEditor` | `Modules/LevelFlow/Bindings/LevelCatalogAsset.cs` | `LevelCatalogAsset.OnValidate` | Migração restrita a editor; impacto runtime indireto via assets serializados. |
| LOW | ValidationOnly | `GameNavigationCatalogAsset.ResolveCriticalCoreIntentsForValidation` com `LegacyRequiredCoreIntentsFallback` | `Modules/Navigation/GameNavigationCatalogAsset.cs` | `ValidateCriticalCoreSlotsInEditorOrFail` | Uso de nome “Legacy” em validação de slots core obrigatórios (menu/gameplay). |

---

## C) Seções por módulo (detalhado)

## Navigation

- **`GameNavigationService.RestartAsync`**
  - Categoria: `RuntimeFallback` | RuntimeRisk: `HIGH`
  - Como entra aqui (call sites): `RestartNavigationBridge.OnResetRequested`; consumo de `IGameNavigationService.RestartAsync`; callers extras não encontrados.
  - Impacto: pode reiniciar gameplay por rota legada (`_lastGameplayRouteId`) sem reconciliação por level canônico, abrindo espaço para mistura rota/level em restart.
  - Excerpt:
    - `resolveSource = "legacy_route_only";`
    - `ReverseLookupMissingOrAmbiguous routeId='...'`
    - `RestartResolved macroRouteId='...' source='legacy_route_only'`

- **`GameNavigationService.StartGameplayRouteAsync`**
  - Categoria: `RuntimeFallback` | RuntimeRisk: `HIGH`
  - Como entra aqui: `LevelFlowRuntimeService.StartGameplayAsync`; `LevelFlowRuntimeService.RestartLastGameplayAsync`; `GameNavigationService.RestartAsync`.
  - Impacto: em ausência de seleção explícita, aplica fallback de origem (`snapshot` / `last_level_id`), podendo esconder inconsistência de origem do level selecionado.
  - Excerpt:
    - `FallbackApplied source='snapshot' ...`
    - `FallbackApplied source='last_level_id' ...`
    - `FailFastMissingLevel ... require_explicit_level_or_valid_snapshot`

- **`RestartNavigationBridge.OnResetRequested`**
  - Categoria: `RuntimeFallback` | RuntimeRisk: `HIGH`
  - Como entra aqui: `EventBus<GameResetRequestedEvent>`.
  - Impacto: se `ILevelFlowRuntimeService` não estiver no DI, bridge volta para restart de navegação, reativando trilho menos canônico.
  - Excerpt:
    - `Restart fallback -> IGameNavigationService.RestartAsync (LevelFlowRuntimeService missing)`

- **`IGameNavigationService` e `GameNavigationService` APIs `[Obsolete]`** (`StartGameplayAsync(LevelId)`, `NavigateAsync(string)`, `RequestMenuAsync`, `RequestGameplayAsync`)
  - Categoria: `DeprecatedAPI` | RuntimeRisk: `MED`
  - Como entra aqui: interface global; callers diretos de overload string não encontrados no escopo principal.
  - Impacto: mantém entrypoints paralelos ao trilho tipado/core, facilitando bypass por consumo legado.
  - Excerpt:
    - `[System.Obsolete("Use ILevelFlowRuntimeService.StartGameplayAsync...")]`
    - `[System.Obsolete("Prefira NavigateAsync(GameNavigationIntentKind...)")]`

- **`GameNavigationCatalogAsset.TryMapIntentIdToCoreKind` (defeat compat -> gameplay)**
  - Categoria: `CompatShim` | RuntimeRisk: `MED`
  - Como entra aqui: resolução de intent em `TryGet` / `ResolveIntentOrFail`.
  - Impacto: `defeat` opcional/compat mapeado para gameplay pode ocultar diferença semântica entre intents históricos e core atual.
  - Excerpt:
    - `defeat ... opcional/compat, resolvida para o mesmo trilho de gameplay.`

## SceneFlow

- **`MacroLevelPrepareCompletionGate.AwaitBeforeFadeOutAsync`**
  - Categoria: `BestEffort` | RuntimeRisk: `HIGH`
  - Como entra aqui: `SceneTransitionService.AwaitCompletionGateAsync` via `ISceneTransitionCompletionGate`; registrado em `GlobalCompositionRoot.SceneFlowWorldLifecycle`.
  - Impacto: com DI ausente ou serviço não registrado, fase `LevelPrepare` é ignorada e transição pode seguir sem preparação de level.
  - Excerpt:
    - `MacroLoadingPhase='LevelPrepare' skipped: DependencyManager.Provider indisponível`
    - `... skipped: ILevelMacroPrepareService ausente ...`

- **`SceneTransitionService.BuildRequestFromRouteDefinition`**
  - Categoria: `DeprecatedAPI` | RuntimeRisk: `MED`
  - Como entra aqui: chamado em `SceneTransitionService.TransitionAsync`.
  - Impacto: ainda detecta “inline scene data” legado; hoje aborta fail-fast, mas indica superfície antiga ainda chegando ao serviço.
  - Excerpt:
    - `[OBS][Deprecated] Inline scene data foi detectado sem RouteId`
    - `Fluxo legado desativado; request será abortada.`

- **`DefaultGameplaySceneClassifier.IsGameplayScene`**
  - Categoria: `RuntimeFallback` | RuntimeRisk: `MED`
  - Como entra aqui: resolução de `IGameplaySceneClassifier` no DI (consumido por orquestradores de estágio/readiness).
  - Impacto: fallback por nome de cena pode classificar gameplay sem marker explícito, acoplando comportamento a convenção de nome.
  - Excerpt:
    - `marker explícito na cena ativa, com fallback por nome.`

- **`TransitionStyleCatalogAsset.transitionProfileCatalog`**
  - Categoria: `ValidationOnly` | RuntimeRisk: `LOW`
  - Como entra aqui: validações de catálogo/editor e bootstrap config.
  - Impacto: campo legado não usado no runtime de resolução de profile, mas mantém superfície de configuração antiga.
  - Excerpt:
    - `Profile Catalog (Legacy / Validation-only)`
    - `Nunca é usado em runtime para resolver profile.`

## LevelFlow

- **`LevelDefinition.routeId` / `routeRef` legados**
  - Categoria: `CompatShim` | RuntimeRisk: `MED`
  - Como entra aqui: migrações editor no `LevelCatalogAsset` (`TryMigrateLegacyRouteRefsInEditor`, `TryMigrateMacroRouteRefsInEditor`).
  - Impacto: coexistência de campos antigos com `macroRouteRef` pode reabrir configuração híbrida rota/level em assets.
  - Excerpt:
    - `[Obsolete("Campo legado apenas para migração/serialização.")]`
    - `routeRef ... (compat/migração). Não usada para navegação.`

- **`LevelCatalogAsset.TryResolveLevelId(SceneRouteId, out LevelId)`**
  - Categoria: `RuntimeFallback` | RuntimeRisk: `MED`
  - Como entra aqui: callers não encontrados no escopo analisado.
  - Impacto: método de reverse lookup por rota macro permanece exposto; em ambiguidades, opera best-effort e retorna false.
  - Excerpt:
    - `TryResolveLevelId permanece best-effort para compat.`

- **`GameplayStartSnapshot.ContextSignature`**
  - Categoria: `DeprecatedAPI` | RuntimeRisk: `MED`
  - Como entra aqui: callers explícitos não encontrados.
  - Impacto: alias legado pode confundir assinatura de level com assinatura de contexto histórico.
  - Excerpt:
    - `[System.Obsolete("Use LevelSignature...")]`
    - `public string ContextSignature => LevelSignature;`

- **`LevelCatalogAsset` migrações editor-only** (`TryMigrateLegacyRouteRefsInEditor`, `TryMigrateMacroRouteRefsInEditor`)
  - Categoria: `EditorOnlyMigration` | RuntimeRisk: `LOW`
  - Como entra aqui: `OnValidate` com `#if UNITY_EDITOR`.
  - Impacto: não roda em player, mas altera serialização de assets e pode manter vestígios legados.
  - Excerpt:
    - `#if UNITY_EDITOR`
    - `OnValidate ... TryMigrateLegacyRouteRefsInEditor();`

## WorldLifecycle

- **`WorldLifecycleSceneFlowResetDriver` (best-effort + fallback completion)**
  - Categoria: `BestEffort` | RuntimeRisk: `HIGH`
  - Como entra aqui: binding em `SceneTransitionScenesReadyEvent`; registrado por `GlobalCompositionRoot.Pipeline.InstallWorldLifecycleServices`.
  - Impacto: prioriza não bloquear transição; em erro/ausência de reset service, publica completion e segue, potencialmente com mundo não rearmado.
  - Excerpt:
    - `Best-effort: ... NAO impede liberacao do gate.`
    - `Fallback/SKIP: driver libera o gate ...`
    - `WorldResetService ausente no DI. Reset nao executado.`

- **`ProductionWorldResetPolicy.AllowLegacyActorKindFallback => true`**
  - Categoria: `CompatShim` | RuntimeRisk: `MED`
  - Como entra aqui: `RunRearmOrchestrator.ExecuteAsync` valida `_worldResetPolicy.AllowLegacyActorKindFallback`.
  - Impacto: permite fallback por string para descoberta de atores, útil em recuperação, mas pode perpetuar contrato antigo de classificação.
  - Excerpt:
    - `public bool AllowLegacyActorKindFallback => true;`

- **`WorldLifecycleControllerLocator.FindSingleForSceneOrFallback`**
  - Categoria: `RuntimeFallback` | RuntimeRisk: `MED`
  - Como entra aqui: callers externos não encontrados (método disponível no utilitário).
  - Impacto: quando usado, pode aceitar controller único fora do match exato da cena alvo.
  - Excerpt:
    - `Padroniza o comportamento de fallback ...`
    - `fallback: se houver exatamente um controller válido...`

## Composition

- **`GlobalCompositionRoot.FadeLoading` -> `DegradedFadeService`**
  - Categoria: `BestEffort` | RuntimeRisk: `MED`
  - Como entra aqui: `HandleFadeSetupFailure`; `HandleFadeRuntimeFailure`.
  - Impacto: mantém app operacional quando fade falha, porém pode ocultar problema estrutural de UX/transição.
  - Excerpt:
    - `new DegradedFadeService(reason)`
    - `[ERROR][DEGRADED][Fade] ...`

- **`GlobalCompositionRoot.InitializeReadinessGate`**
  - Categoria: `RuntimeFallback` | RuntimeRisk: `MED`
  - Como entra aqui: pipeline de composição de Pause/Readiness.
  - Impacto: resolve `ISimulationGateService` por fallback best-effort; em ausência, readiness fica sem proteção.
  - Excerpt:
    - `fallback: tenta resolver aqui (best-effort)`

- **`SceneScopeCompositionRoot.EnsureWorldRoot`**
  - Categoria: `RuntimeFallback` | RuntimeRisk: `MED`
  - Como entra aqui: bootstrap de escopo de cena.
  - Impacto: cena inválida usa ActiveScene, podendo prender WorldRoot em cena não esperada.
  - Excerpt:
    - `Usando ActiveScene como fallback.`

- **`GlobalCompositionRoot.RegisterSceneFlowTransitionProfiles`**
  - Categoria: `ValidationOnly` | RuntimeRisk: `LOW`
  - Como entra aqui: instalação de SceneFlow no bootstrap global.
  - Impacto: mantém catálogo legado apenas para validação de cobertura, não para resolução runtime de profile.
  - Excerpt:
    - `registrado apenas para validação/compatibilidade (runtime usa referência direta de profile).`

---

## D) Entrypoints canônicos vs legados

### Entrypoints canônicos (runtime atual)

- `ILevelFlowRuntimeService.StartGameplayAsync(string levelId, ...)`
- `ILevelFlowRuntimeService.SwapLevelLocalAsync(LevelId, ...)`
- `ILevelFlowRuntimeService.RestartLastGameplayAsync(...)`
- `IGameNavigationService.StartGameplayRouteAsync(SceneRouteId, payload, ...)` (rota já resolvida pelo LevelFlow)
- `IGameNavigationService.NavigateAsync(GameNavigationIntentKind, ...)` (core tipado)
- `IWorldResetCommands.ResetMacroAsync(...)` e `ResetLevelAsync(...)`
- `ILevelMacroPrepareService.PrepareAsync(...)` (via completion gate do SceneFlow)

### Entrypoints paralelos/antigos ainda presentes

- `IGameNavigationService.StartGameplayAsync(LevelId, ...)` `[Obsolete]`
  - Como é chamado hoje: callers diretos não encontrados no escopo principal.
- `IGameNavigationService.NavigateAsync(string routeId, ...)` `[Obsolete]`
  - Como é chamado hoje: callers diretos não encontrados no escopo principal.
- `IGameNavigationService.RequestMenuAsync(...)` / `RequestGameplayAsync(...)` `[Obsolete]`
  - Como é chamado hoje: callers diretos não encontrados no escopo principal.
- `GameNavigationService.RestartAsync(...)` com resolução `legacy_route_only`
  - Como é chamado hoje: fallback de `RestartNavigationBridge` quando `ILevelFlowRuntimeService` está ausente.
- `GameplayStartSnapshot.ContextSignature` `[Obsolete]`
  - Como é chamado hoje: callers não encontrados.

---

## H1 status (2026-03-05)

- **HIGH-01 — `GameNavigationService.RestartAsync` (`legacy_route_only`)**
  - H1 status: **FIXED (Strict/Production)** / **DEV_ONLY (escape hatch)**
  - Referência: `Modules/Navigation/GameNavigationService.cs` (`RestartAsync`, `FailFastH1`).

- **HIGH-02 — `GameNavigationService.StartGameplayRouteAsync` fallback (`snapshot`/`last_level_id`)**
  - H1 status: **FIXED (Strict/Production para `last_level_id`)** / **DEV_ONLY (fallback com warn)**
  - Referência: `Modules/Navigation/GameNavigationService.cs` (`StartGameplayRouteAsync`, logs `[WARN][COMPAT][NAV]`).

- **HIGH-03 — `RestartNavigationBridge.OnResetRequested` fallback para `IGameNavigationService`**
  - H1 status: **FIXED (Strict/Production)** / **DEV_ONLY (escape hatch)**
  - Referência: `Modules/Navigation/RestartNavigationBridge.cs` (`OnResetRequested`, guard `[FATAL][H1][NAV][DI]`).

- **HIGH-04 — `MacroLevelPrepareCompletionGate.AwaitBeforeFadeOutAsync` skip silencioso**
  - H1 status: **FIXED (Strict/Production)** / **DEV_ONLY (degraded warn)**
  - Referência: `Modules/SceneFlow/Transition/Runtime/MacroLevelPrepareCompletionGate.cs` (`FailFastH1`, logs `[WARN][DEGRADED][SceneFlow]`).

- **HIGH-05 — `WorldLifecycleSceneFlowResetDriver` fallback completion com serviço crítico ausente**
  - H1 status: **FIXED (Strict/Production)** / **DEV_ONLY (best-effort com contador)**
  - Referência: `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs` (`HandleScenesReadyAsync`, `ExecuteResetWhenRequiredAsync`).

## Observações de método

- Itens com “callers não encontrados” foram mantidos explicitamente assim para evitar inferência indevida.
- Este inventário não altera runtime code e não propõe refatoração nesta entrega H0.
