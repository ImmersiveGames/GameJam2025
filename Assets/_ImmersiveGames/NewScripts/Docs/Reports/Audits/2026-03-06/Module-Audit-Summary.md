# Module Audit Summary (Baseline 3.1) - 2026-03-06

## Core
**Canonical Rail:** O trilho canonico de base e `Core/Events/EventBus.cs` + `Core/Composition/DependencyManager.cs` + `Core/Logging/DebugUtility.cs`. Os modulos de dominio publicam/consomem eventos pelo `EventBus<T>` e resolvem servicos pelo `DependencyManager.Provider`, com o `GlobalCompositionRoot` apenas compondo dependencias.

**Top 5 redundancias**
- `Core/Events/FilteredEventBus.Legacy.cs` existe como wrapper de compat para API antiga.
- `Core/Events/FilteredEventBus.cs` e `Core/Events/EventBus.cs` coexistem em cenarios similares de roteamento de eventos.
- `Core/Composition/GlobalServiceRegistry.cs` e `Core/Composition/ServiceRegistry.cs` podem gerar sobreposicao de responsabilidade.
- `Core/Composition/ObjectServiceRegistry.cs` e `Core/Composition/SceneServiceRegistry.cs` repetem padroes de lookup/clean.
- `Core/Logging/DebugManagerConfig.cs` e `Core/Logging/DebugLogSettings.cs` compartilham controle de fallback/log-policy.

**Risco de mudanca:** Med  
**Ordem recomendada de limpeza:** 9/10 (depois dos modulos de fluxo/runtime).

**CORE-1.1 status:** inventory done (DOC-only).
- live doc: `Docs/Modules/Core.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v1.md`
- candidatos `B MOVE`: `FilteredEventBus.Legacy`, `DebugManagerConfig`, `DebugLogSettings`
- candidatos `C RISK`: `EventBusUtil`

**CORE-1.2a status:** DONE (behavior-preserving).
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v2.md`
- `FilteredEventBus.Legacy.cs` movido para `Core/Events/Legacy/FilteredEventBus.Legacy.cs`
- namespace e tipo publico preservados; zero callsites em `Infrastructure/**` e `Modules/**`; sem refs em assets
**CORE-1.2b status:** DONE (behavior-preserving).
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v3.md`
- `DebugManagerConfig.cs` movido para `Dev/Core/Logging/DebugManagerConfig.cs`
- namespace e tipo publico preservados; `DebugManagerConfig` isolado em `Dev/Core/Logging`; sem refs em assets para `DebugManagerConfig`
- `DebugLogSettings` classified `B dev-only`: no runtime loader, no refs in scenes/prefabs, asset self-referenced only.
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v4.md`
- moved to `Dev/Core/Logging/DebugLogSettings.cs` + `Dev/Core/Logging/DebugLogSettings.asset`
- `InjectableEventBus<T>` classified `A canonical`: `EventBus<T>` instantiates it directly as `InternalBus` / default `GlobalBus` backend.
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v5.md`
- `Core/Fsm` stack classified `C reserve`: `StateMachine`, `Transition`, `IState`, `ITransition` e `IPredicate` have no real runtime callsites outside Core and no asset refs.
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v7.md`
- `Preconditions.cs` classified `C reserve`: only internal Core usage (`Core/Fsm/StateMachine.cs`), no asset refs and no Editor leak.
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v8.md`
- `SceneServiceCleaner.cs` classified `C reserve`: only internal Core usage via `SceneServiceRegistry`, no asset refs and no Editor leak.
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v9.md`
- `EventBusUtil` classified `C reserve`: no callsites in `Modules/**`/`Infrastructure/**`, no asset refs, and the Editor hook is already isolated in `Editor/Core/Events/EventBusUtil.Editor.cs`.
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Cleanup-Audit-v10.md`
- `CORE-1.2i` documented `Reserve <> Legacy`, promotion path and PR/CI gates for the Core rail.
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Core-Gates-v1.md`
## Infrastructure-Composition
**Canonical Rail:** `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` chama `RegisterEssentialServicesOnly` em `GlobalCompositionRoot.Pipeline.cs`, que faz dispatch direto por stage em `InstallCompositionModules()` e registra servicos canonicos de runtime por metodos especializados (`GameLoop`, `SceneFlow`, `WorldLifecycle`, `Navigation`, `Levels`, `ContentSwap`, `DevQA`).

**Top 5 redundancias**
- `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs` teve o registro legacy `RegisterRestartSnapshotContentSwapBridge()` removido no cleanup canonico.
- `Infrastructure/Composition/Modules/*.cs` (boilerplate de gate por stage) foi removido no IC-1.3; dispatch agora e direto no pipeline.
- Registro de bridges cross-modulo concentrado em `NavigationInputModes.cs` com mistura de Navigation/LevelFlow/InputMode.
- Duplicidade de pontos de configuracao SceneFlow entre `GlobalCompositionRoot.SceneFlowWorldLifecycle.cs` e `GlobalCompositionRoot.FadeLoading.cs`.
- `SceneScopeCompositionRoot.cs` e `GlobalCompositionRoot.*` compartilham responsabilidade de boot DI em escopos diferentes.

**Risco de mudanca:** High  
**Ordem recomendada de limpeza:** 1/10 (primeiro, para reduzir acoplamento transversal).

## Gates-Readiness-StateDependent
**Canonical Rail:** `SimulationGateService` (`Modules/Gates`) recebe intents de pausa por `GamePauseGateBridge`, `GameReadinessService` (`Modules/SceneFlow/Readiness/Runtime`) aplica bloqueio/desbloqueio por transicoes SceneFlow, e `StateDependentService` (`Modules/Gameplay/Runtime/Actions/States`) integra estado runtime com reset/pause.

**Top 5 redundancias**
- `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs` e `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs` ambos reagem a `SceneTransitionStarted/Completed`.
- `GamePauseGateBridge` + `StateDependentService` podem tocar o mesmo dominio de bloqueio/estado por caminhos diferentes.
- Dedupe local por assinatura em `SceneFlowInputModeBridge` sem coordenacao central com readiness.
- `DefaultGameplaySceneClassifier` e fallback por nome de cena em consumidores distintos.
- Registro em DI cruzando arquivos `GlobalCompositionRoot.PauseReadiness.cs` e `GlobalCompositionRoot.StateDependentCamera.cs`.

**Risco de mudanca:** Med  
**Ordem recomendada de limpeza:** 4/10.

## GameLoop
**Canonical Rail:** `GameLoopService` (estado da run), comandos via `GameCommands`, e coordenacao SceneFlow por `GameLoopSceneFlowCoordinator`. Restart canonico e externo ao bridge legado: `GameCommands -> GameResetRequestedEvent -> MacroRestartCoordinator`.

**Top 5 redundancias**
- `Modules/GameLoop/Runtime/Bridges/GameLoopCommandEventBridge.cs` mantem trilho de eventos com listener de reset explicitamente desativado (LEGACY marker).
- Responsabilidades de transicao de estado repartidas entre `GameLoopService`, `GameLoopSceneFlowCoordinator` e `SceneFlowInputModeBridge`.
- `GameRunStateService` e `GameRunOutcomeService` consomem eventos de run em paralelo.
- `GameLoopBootstrap` e composicao global podem registrar artefatos semelhantes em ordens diferentes.
- Fluxo IntroStage tem servicos dedicados e tambem gatilhos via `LevelStageOrchestrator`.

**Risco de mudanca:** High  
**Ordem recomendada de limpeza:** 6/10.

## SceneFlow
**Canonical Rail:** `SceneTransitionService` publica eventos de transicao (`Started`, `ScenesReady`, `Completed`) e usa gate composto (`MacroLevelPrepareCompletionGate` + `WorldLifecycleResetCompletionGate`) para controlar `FadeOut/Completed`. Servicos auxiliares (`SignatureCache`, `LoadingHudOrchestrator`, `GameReadinessService`) escutam o mesmo barramento.

**Top 5 redundancias**
- Varios consumidores do mesmo par `SceneTransitionStarted/Completed` (InputModes, GameLoop, Loading, Readiness, SignatureCache, LevelFlow).
- Dedupe por assinatura aparece em `SceneTransitionService`, `SceneFlowSignatureCache`, `LoadingHudOrchestrator`.
- Readiness e InputMode ambos tratam semantica de profile/frontend/gameplay.
- Adaptadores `NoFadeAdapter`/`SceneFlowFadeAdapter` convivem com fallback em `DegradedFadeService`.
- Logica de validacao/editor em `Modules/SceneFlow/Editor/**` extensa para contratos ja validados em runtime.

**Risco de mudanca:** High  
**Ordem recomendada de limpeza:** 3/10.

## WorldLifecycle
**Canonical Rail:** Para reset macro via SceneFlow, owner canonico e `WorldResetOrchestrator` (via `WorldResetService`), acionado por `WorldLifecycleSceneFlowResetDriver` no `ScenesReady`. `WorldLifecycleResetCompletionGate` consome `WorldLifecycleResetCompletedEvent` para liberar a transicao.

**Top 5 redundancias**
- Dois orquestradores de nome semelhante: `WorldLifecycleOrchestrator` e `WorldResetOrchestrator`.
- `WorldLifecycleSceneFlowResetDriver` publica `WorldLifecycleResetCompletedEvent` em SKIP/fallback, enquanto trilho principal tambem publica no `WorldResetOrchestrator`.
- Eventos V1 e V2 coexistem (`WorldLifecycleResetCompletedEvent` e `WorldLifecycleResetCompletedV2Event`).
- `WorldResetCommands` e `WorldResetService` coexistem com contratos parcialmente sobrepostos.
- Caminhos de reason/signature espalhados entre Runtime, WorldRearm e README.

**Risco de mudanca:** High  
**Ordem recomendada de limpeza:** 2/10.

## Navigation
**Canonical Rail:** `GameNavigationService` executa navegacao por route/profile. Restart canonico e `MacroRestartCoordinator` ouvindo `GameResetRequestedEvent` e disparando `ILevelFlowRuntimeService.StartGameplayDefaultAsync`.

**Top 5 redundancias**
- `Modules/Navigation/Legacy/RestartNavigationBridge.cs` mantido como legado desativado (fora do trilho canonico).
- `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs` mantido como no-op legado (fora do trilho canonico).
- `GameNavigationService` mantem APIs compat marcadas como legacy.
- `LevelSelectedRestartSnapshotBridge` e `IRestartContextService` duplicam parte do fluxo de contexto de restart.
- Catalogos de intents/rotas/estilos cruzam `Modules/Navigation/**` e `Modules/SceneFlow/Navigation/**`.

**Risco de mudanca:** Med  
**Ordem recomendada de limpeza:** 5/10.

## LevelFlow
**Canonical Rail:** `LevelFlowRuntimeService` inicia gameplay default/restart, `LevelMacroPrepareService` prepara/limpa o nivel no gate macro do SceneFlow, `LevelSwapLocalService` aplica swaps locais intra-macro e `LevelStageOrchestrator` coordena IntroStage por `SceneTransitionCompletedEvent` + `LevelSwapLocalAppliedEvent`.

**Top 5 redundancias**
- Dupla publicacao de `LevelSelectedEvent` (prepare macro + swap local), com ownership dividido por contexto.
- Dedupe por `selectionVersion/levelSignature` concentrado em `LevelStageOrchestrator` (manter alinhado com producers).
- Trilha de catalogo antiga (`LevelCatalogAsset` + interfaces antigas) era estruturalmente redundante ao trilho canonico.
- API legacy por `LevelId` continua presente em overloads obsoletos para compat (fail-fast).
- Rotas de QA/Dev exercitam caminhos nao-canonicos e podem mascarar redundancias em runtime.

**Risco de mudanca:** High  
**Ordem recomendada de limpeza:** 7/10.

**LF-1.1 aplicado:** isolamento de compat para `Modules/LevelFlow/Legacy/**`:
- `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs`
## ContentSwap
**Canonical Rail:** `ContentSwapContextService` mantem `Current/Pending` e publica eventos de contexto; `InPlaceContentSwapService` (registrado como `IContentSwapChangeService`) e o owner canonico do request in-place/commit. O consumer canonico externo e `Modules/WorldLifecycle/Runtime/WorldResetCommands.cs` no reset de nivel.

**Top 5 redundancias**
- Instalacao Dev potencialmente duplicada: `GlobalCompositionRoot.DevQA.cs` e `ContentSwapDevBootstrapper`.
- Consumidor legado externo (`Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs`) ainda escuta `ContentSwapCommittedEvent`.
- Eventos `ContentSwapPendingSet/Cleared` hoje servem majoritariamente observabilidade (poucos consumers runtime).
- Overlap semantico de contrato de nivel (`contentId` legado em LevelFlow) vs token canonico `level-ref:<name>` usado por WorldReset+ContentSwap.
- Coexistencia de snapshot antigo (`Modules/ContentSwap.md`) com snapshot CS-1.1 (`Modules/ContentSwap-Cleanup-Audit-v1.md`) requer indice claro.

**Risco de mudanca:** Med  
**Ordem recomendada de limpeza:** 8/10.

**CS-1.1 aplicado (behavior-preserving):**
- Snapshot criado em `Docs/Reports/Audits/2026-03-06/Modules/ContentSwap-Cleanup-Audit-v1.md`.
- Doc vivo criado em `Docs/Modules/ContentSwap.md`.
- Nenhum move de `.cs` nesta etapa (resultado DOC-only).

## DevQA
**Canonical Rail:** instalacao Dev/QA centralizada em `CompositionInstallStage.DevQA` (`GlobalCompositionRoot.Pipeline.cs`) via `InstallDevQaServices()` e `GlobalCompositionRoot.DevQA.cs`, incluindo IntroStage/ContentSwap/SceneFlow/LevelFlow installers, `IntroStageRuntimeDebugGui` e hotkey de WorldLifecycle.

**Top 5 redundancias**
- `ContentSwapDevBootstrapper` coexistia com installer central de ContentSwap (paralelo).
- Hotkey de WorldLifecycle era instalado fora do trilho central DevQA (bootstrap runtime independente).
- ContextMenus QA em arquivos de runtime (`PauseOverlayController`, `PostGameOverlayController`) mantem acoplamento de debug com codigo de producao.
- Flags nao totalmente unificadas no repositorio (`NEWSCRIPTS_QA` permanece em tooling de Gameplay/Editor).
- Tooling editor continua distribu?do por multiplos modulos (`SceneFlow/Editor`, `Navigation/Dev/Editor`, `Gameplay/Editor/RunRearm`).

**Risco de mudanca:** Low-Med  
**Ordem recomendada de limpeza:** 10/10 (apos runtime est?vel).

**DQ-1.2 aplicado (behavior-preserving):**
- `ContentSwapDevBootstrapper` desativado como caminho paralelo (LEGACY no-op com log `[OBS][LEGACY][DevQA]`).
- Hotkey DEV de WorldLifecycle centralizado via `RegisterWorldLifecycleQaInstaller()` no trilho DevQA canonico.
- Guards dos arquivos alterados consolidados em `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Snapshot criado em `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Cleanup-Audit-v2.md`.

## Global Redundancy Hotspots (Top 10)
1. `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`
2. `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
3. `Modules/Navigation/Runtime/MacroRestartCoordinator.cs`
4. `Modules/Navigation/Legacy/RestartNavigationBridge.cs`
5. `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs`
6. `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`
7. `Modules/WorldLifecycle/WorldRearm/Application/WorldResetOrchestrator.cs`
8. `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs`
9. `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`
10. `Core/Events/FilteredEventBus.Legacy.cs`

## Event Ownership Matrix (required checks)
### GameResetRequestedEvent
- **Publishers:** `Modules/GameLoop/Commands/GameCommands.cs`
- **Consumers:** `Modules/Navigation/Runtime/MacroRestartCoordinator.cs`, `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs`
- **Canonical owner:** `Modules/Navigation/Runtime/MacroRestartCoordinator.cs`

### SceneTransitionStartedEvent
- **Publisher:** `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- **Consumers:** `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`, `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`, `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs`, `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs`, `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs`, `Modules/LevelFlow/Dev/LevelFlowDevContextMenu.cs`
- **Canonical owner:** `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`

### SceneTransitionCompletedEvent
- **Publisher:** `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- **Consumers:** `Modules/InputModes/Interop/SceneFlowInputModeBridge.cs`, `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`, `Modules/SceneFlow/Runtime/SceneFlowSignatureCache.cs`, `Modules/SceneFlow/Loading/Runtime/LoadingHudOrchestrator.cs`, `Modules/SceneFlow/Readiness/Runtime/GameReadinessService.cs`, `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs`
- **Canonical owner:** `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`

### WorldLifecycleResetCompletedEvent
- **Publishers:** `Modules/WorldLifecycle/WorldRearm/Application/WorldResetOrchestrator.cs` (canonico), `Modules/WorldLifecycle/WorldRearm/Application/WorldResetService.cs` (fallback), `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs` (SKIP/fallback)
- **Consumers:** `Modules/WorldLifecycle/Runtime/WorldLifecycleResetCompletionGate.cs`, `Modules/GameLoop/Runtime/Bridges/GameLoopSceneFlowCoordinator.cs`
- **Canonical owner:** `Modules/WorldLifecycle/WorldRearm/Application/WorldResetOrchestrator.cs`

### LevelSelectedEvent
- **Publishers:** `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs`, `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs`
- **Consumers:** `Modules/Navigation/LevelSelectedRestartSnapshotBridge.cs`
- **Canonical owner:** `Modules/LevelFlow/Runtime/LevelMacroPrepareService.cs` (macro entry) + `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs` (intra-macro local swap)

### LevelSwapLocalAppliedEvent
- **Publisher:** `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs`
- **Consumers:** `Modules/LevelFlow/Runtime/LevelStageOrchestrator.cs`
- **Canonical owner:** `Modules/LevelFlow/Runtime/LevelSwapLocalService.cs`

## InputModes
**Canonical Rail:** `GlobalCompositionRoot.InputModes.cs` registra `IInputModeService` e, quando o servico existe, habilita `InputModeCoordinator` + `SceneFlowInputModeBridge`; `InputModeCoordinator` e o unico writer canonico de `IInputModeService`; requestors publicam `InputModeRequestEvent`; `InputModeService` continua executor concreto dos action maps.

**IM-1.2e freeze (DOC-only):**
- owner: `InputModeCoordinator` e o unico writer real; `InputModeService` permanece executor.
- request rail: `SceneFlowInputModeBridge`, `PauseOverlayController`, `PostGameOwnershipService`, `PostGameOverlayController`, `GameLoopService` e `ConfirmToStartIntroStageStep` publicam `InputModeRequestEvent`.
- hardening/gating: `GlobalCompositionRoot.InputModes.cs` so registra coordinator/bridge quando `IInputModeService` esta presente; coordinator faz warn-once e no-op quando o servico nao existe.
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/InputModes-Cleanup-Audit-v6.md`.

## What I did NOT do
- Nao alterei nenhum arquivo `.cs`.
- Nao movi, nao deletei e nao renomeei arquivos.
- Nao executei refactor de runtime; a entrega foi estritamente DOC-only em `Docs/` (escopo SF-1.2a).

## SF-1.2a (inventory only)
- Snapshot criado: `Docs/Reports/Audits/2026-03-06/Modules/SceneFlow-Cleanup-Audit-v2.md`.
- Escopo: inventario de redundancias/compat/no-op no SceneFlow (sem alteracao de runtime).
- Resultado curto:
  - KEEP: `NoOpTransitionCompletionGate`, `NoFadeAdapter`, `SceneFlowSignatureCache`.
  - NEED MANUAL CONFIRMATION: fallback "completion gate falhou/abortou -> prosseguindo" e sobreposicao de dedupe/assinatura entre `SceneTransitionService` / `SceneFlowSignatureCache` / `LoadingHudOrchestrator`.

## SF-1.2b (hardening minimo, behavior-preserving)
- Snapshot criado: `Docs/Reports/Audits/2026-03-06/Modules/SceneFlow-Cleanup-Audit-v3.md`.
- Mudancas de runtime:
  - `LoadingHudService`: dedupe same-frame de log/ensure (`[OBS][Loading] ... dedupe_same_frame`).
  - `SceneTransitionService`: dedupe seguro por assinatura (same-frame + in-flight coalesce) e aceite explicito apos completed.
  - observabilidade de fallback do completion gate (`[OBS][SceneFlow] CompletionGateFallback ...`).
- Contratos publicos e ordem de pipeline de transicao preservados.




## GRS-1.3b (behavior-preserving)
- Snapshot criado: `Docs/Reports/Audits/2026-03-06/Modules/GRS-ExitToMenu-Coordinator-Audit-v1.md`.
- `ExitToMenuCoordinator` agora e o owner can?nico de `GameExitToMenuRequestedEvent`, executando `RequestReady()` -> `ExitToMenuAsync(reason)`.
- `GameLoopCommandEventBridge` nao consome mais ExitToMenu; `ExitToMenuNavigationBridge` virou stub LEGACY/no-op; o pipeline continua chamando o registro no mesmo ponto logico.
- Leak sweep runtime permaneceu limpo e o register do evento caiu para um unico arquivo.

## GRS-1.3a (behavior-preserving)
- Snapshot criado: `Docs/Reports/Audits/2026-03-06/Modules/GRS-Pause-Resume-Exit-Audit-v1.md`.
- Runtime consumers de `GamePauseCommandEvent`, `GameResumeRequestedEvent` e `GameExitToMenuRequestedEvent` agora fazem dedupe same-frame com logs `[OBS][GRS] ... consumed ...` e `[OBS][GRS] ... dedupe_same_frame ...`.
- Escopo endurecido: `GamePauseGateBridge`, `StateDependentService`, `GameLoopCommandEventBridge`, `PauseOverlayController` e `ExitToMenuNavigationBridge`.
- Sem mudanca de payload/contrato de evento, sem novo owner unico e sem leak de tooling fora de `Dev/Editor/Legacy/QA`.

## GRS-1.2 (behavior-preserving)
- Snapshot criado: `Docs/Reports/Audits/2026-03-06/Modules/Gates-Readiness-StateDependent-Cleanup-Audit-v2.md`.
- Acoes de cleanup:
  - inventario de ownership real de `SceneTransitionStartedEvent`, `SceneTransitionCompletedEvent`, `GameResetRequestedEvent` com evidencia `rg`.
  - hardening idempotente local em `SceneFlowInputModeBridge` (started same-frame) e `StateDependentService` (reset same-frame).
- Sem novo owner de gate/readiness e sem alteracao de contratos/pipeline.

## DQ-1.3 (behavior-preserving)
- Snapshot criado: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Cleanup-Audit-v3.md`.
- `WorldLifecycleHookLoggerA` e seu callsite em composition isolados por `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `Modules/Gameplay/Editor/RunRearm/**` normalizado para `#if UNITY_EDITOR`.
- `PostGameOverlayController` teve helpers QA isolados no guard DevQA; `PauseOverlayController` ja estava no padrao.
- Sem alteracao de comportamento de producao.
- Release excludes DevQA by compile guards; DevBuild required for QA harness.

## GL-1.2 (behavior-preserving)
- Snapshot criado: `Docs/Reports/Audits/2026-03-06/Modules/GameLoop-Cleanup-Audit-v2.md`.
- Inventario A/B/C consolidado com evidencia rg.
- `IntroStageDevTools` isolado em `#if UNITY_EDITOR`.
- `GameStartRequestEmitter` e `GamePauseHotkeyController` mantidos no trilho runtime canonico.
- Restart canonico preservado; nenhum listener ativo de `GameResetRequestedEvent` em `Modules/GameLoop/**`.

## GL-1.3 (behavior-preserving)
- Snapshot criado: `Docs/Reports/Audits/2026-03-06/Modules/GameLoop-Cleanup-Audit-v3.md`.
- `GameStartRequestEmitter` migrou para trilho DevQA canonico (`EnsureInstalled`) e perdeu bootstrap automatico legado.
- `GamePauseHotkeyController` foi movido para `Modules/GameLoop/Legacy/Bindings/Inputs/` com guard de Editor/DevBuild.
- Nenhum listener ativo de `GameResetRequestedEvent` no GameLoop; restart canonico preservado.
- Release excludes DevQA by compile guards; DevBuild required for QA harness.

## WL-1.2 update (behavior-preserving)
- V1 gate publish centralizado em `WorldResetOrchestrator` (helpers V1).
- Fallbacks em `WorldResetService` e `WorldLifecycleSceneFlowResetDriver` roteados para helper V1 (sem `Raise` direto).
- V2 permanece exclusivo em `WorldResetCommands`.
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/WorldLifecycle-Cleanup-Audit-v2.md`.

## CS-1.2 update (behavior-preserving)
- `ContentSwapContextService` confirmado como publisher unico de eventos de estado (PendingSet/PendingCleared/Committed).
- `InPlaceContentSwapService` mantido como executor canonico delegando publish ao context service.
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/ContentSwap-Cleanup-Audit-v2.md`.


## IC-1.3 update (behavior-preserving)
- Mecanismo `Infrastructure/Composition/Modules/*.cs` removido (boilerplate trivial de stage-gate).
- `InstallCompositionModules()` agora faz dispatch direto por `_compositionInstallStage` no `GlobalCompositionRoot.Pipeline.cs`.
- Ordem de `RegisterEssentialServicesOnly()` preservada; sem mudancaa em `Modules/**`.
- Snapshot: `Docs/Reports/Audits/2026-03-06/Infra-Composition-Cleanup-v2.md`.

## IC-1.4 update (behavior-preserving)
- `SceneScopeCompositionRoot` deixou de ser monolitico: split em `SceneScopeCompositionRoot.cs` + `SceneScopeCompositionRoot.RunRearm.cs` + `SceneScopeCompositionRoot.DevQA.cs`.
- Bloco DevQA (WorldLifecycleHookLoggerA) isolado em arquivo com guard `UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Ordem de boot de cena, contratos e pipeline preservados.
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Infra-SceneScope-Cleanup-Audit-v1.md`.

## GP-1.1 update (behavior-preserving)
- Gameplay separado por trilho: runtime canonico mantido e tooling orfao de QA isolado em `Modules/Gameplay/Legacy/Editor/RunRearm/**`.
- Moves aplicados com prova estatica (sem callsites canonicos, sem refs em assets, GUID scans sem matches).
- Sem alteracao de contratos/payloads/order/callsites do pipeline global.
- Live doc: `Docs/Modules/Gameplay.md`
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Gameplay-Cleanup-Audit-v1.md`



## PostGame
**Canonical Rail:** `GameLoopService` aciona entrada/saida de pos-jogo e resolve `IPostGameOwnershipService`; `PostGameOverlayController` apenas exibe UI e delega acoes para `IPostLevelActionsService`; restart macro continua em `GameCommands -> GameResetRequestedEvent -> Navigation/MacroRestartCoordinator`.

**Top 5 redundancias**
- Bloco QA/ContextMenu coexistia dentro do arquivo runtime de overlay.
- `UnityEditor` e dialogo de confirmacao ficavam acoplados ao controller principal.
- Guard de reentrancia QA (`_qaGuardBusy`) residia no mesmo arquivo do trilho canonico.
- Sinais de DevQA e runtime apareciam no mesmo artefato, elevando ruido de manutencao.
- Ausencia de doc vivo especifico de PostGame no baseline.

**Risco de mudanca:** Low-Med
**Ordem recomendada de limpeza:** 11/11 (apos estabilizacao dos trilhos macro).

## PG-1.1 update (behavior-preserving)
- `PostGameOverlayController` virou `partial` e manteve somente runtime canonico em `Modules/PostGame/Bindings/PostGameOverlayController.cs`.
- DevQA foi isolado para `Modules/PostGame/Dev/PostGameOverlayController.DevQA.cs` sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Uso de `UnityEditor` ficou restrito a bloco `#if UNITY_EDITOR` no arquivo DevQA.
- Sem alteracao de contratos/eventos/payloads e sem mudancaa de ownership de restart macro.
- Live doc: `Docs/Modules/PostGame.md`
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/PostGame-Cleanup-Audit-v1.md`

## PA-1.1 update (behavior-preserving)
- `PauseOverlayController` mantido no mesmo arquivo runtime (`Modules/GameLoop/Pause/Bindings/PauseOverlayController.cs`) e convertido para `partial`.
- Bloco DevQA/ContextMenu isolado em `Modules/GameLoop/Pause/Dev/PauseOverlayController.DevQA.cs` com guard `UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `UnityEditor` restrito ao parcial DevQA sob `#if UNITY_EDITOR`.
- Fluxo runtime (pause/resume/overlay/input mode) e logs ancora preservados.
- Live doc: `Docs/Modules/GameLoop.md`
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/Pause-Cleanup-Audit-v1.md`

## DQ-1.4 update (behavior-preserving)
- Leak sweep global executado em `Modules/**` para simbolos Editor/DevQA, com filtro de suspeitos fora de `Dev/Editor/Legacy`.
- `TransitionStyleCatalogAsset` (SceneFlow Navigation bindings) teve tooling de validacao (`ContextMenu`) extraido para parcial DevQA:
  - runtime: `Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs`
  - dev: `Modules/SceneFlow/Navigation/Dev/TransitionStyleCatalogAsset.DevQA.cs`
- `PauseOverlayController.DevQA` validado conforme hardening PA-1.1 (`using UnityEditor` somente no arquivo Dev sob guard editor).
- Suspeitos remanescentes em runtime classificados como `A` (fail-fast/config/runtime critical) ou `C` (manual confirmation) sem mudancaa nesta etapa.
- Live doc: `Docs/Modules/DevQA.md`
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`

## DQ-1.4.1 update (behavior-preserving)
- `SceneRouteDefinitionAsset` (runtime binding) convertido para `partial` sem mudancaa de logica runtime.
- Bloco editor-only (OnValidate + AssetDatabase + build-index editor helper) movido para `Modules/SceneFlow/Navigation/Dev/SceneRouteDefinitionAsset.DevQA.cs`.
- Runtime file ficou sem `UnityEditor`, `AssetDatabase`, `ContextMenu` e `MenuItem`.
- Live doc: `Docs/Modules/DevQA.md`
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`

## DQ-1.4.2 update (behavior-preserving)
- `GameNavigationCatalogAsset` convertido para `partial` sem mover campos serializados nem alterar logica/runtime.
- Chamada editor-only (`EditorApplication.isPlaying=false`) saiu do runtime e foi para `Modules/Navigation/Dev/GameNavigationCatalogAsset.DevQA.cs`.
- P?s-check confirma runtime limpo de `UnityEditor/AssetDatabase/ContextMenu/MenuItem` no arquivo alvo.
- Live doc: `Docs/Modules/DevQA.md`
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`

## DQ-1.4.3 update (behavior-preserving)
- `SceneRouteResetPolicy` convertido para `partial` sem alterar contratos/regras de decisao de reset.
- `EditorApplication.isPlaying` saiu do runtime e foi isolado em `Modules/WorldLifecycle/Dev/SceneRouteResetPolicy.DevQA.cs`.
- P?s-check confirma runtime limpo de `UnityEditor/EditorApplication/AssetDatabase/ContextMenu/MenuItem` no arquivo alvo.
- Live doc: `Docs/Modules/DevQA.md`
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`

## DQ-1.4.4+ update (behavior-preserving)
- Batch leak sweep aplicado em 6 arquivos runtime: `MenuQuitButtonBinder`, `GameNavigationIntentCatalogAsset`, `GameLoopSceneFlowCoordinator`, `SceneTransitionService`, `SceneBuildIndexRef`, `SceneRouteCatalogAsset`.
- Extracoes criadas em `Modules/**/Dev/*.DevQA.cs` com padrao `partial + guard de build/editor`; runtime principal preservado no mesmo path.
- Prova pos-cleanup: varredura global fora de `Dev/Editor/Legacy` para `UnityEditor|EditorApplication|AssetDatabase|FindAssets|ContextMenu|MenuItem|InitializeOnLoad|RuntimeInitializeOnLoadMethod` retornou sem matches.
- Live doc: `Docs/Modules/DevQA.md`
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v1.md`

## DQ-1.5 update (behavior-preserving)
- Full Editor API leak sweep applied in `Core`/`Infrastructure` runtime files and QA tooling path.
- `QA/**` moved to `Editor/QA/**` (with `.meta`), keeping editor harness out of runtime scan/build.
- Runtime leaks (`UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu`) outside `Dev/Editor/Legacy/QA` now return 0 matches.
- Remaining matches in requested broader regex are only `RuntimeInitializeOnLoadMethod` sites (allowed by policy).
- Live doc: `Docs/Modules/DevQA.md`
- Snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-LeakSweep-Audit-v2.md`

## DQ-1.6 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v1.md`
- status: Guard governance contract formalized and evidenced from local workspace only (behavior-preserving).
- notes: Release remains isolated from Editor APIs; DevBuild/Editor keeps QA harness coverage.
- notes: `NEWSCRIPTS_QA` and `NEWSCRIPTS_DEV` are absent from code; `NEWSCRIPTS_MODE` and `NEWSCRIPTS_BASELINE_ASSERTS` remain only as deprecated documented residuals.
## DQ-1.9 Status
- live doc: `Docs/Modules/DevQA.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/DevQA-Guard-Governance-Audit-v4.md`
- status: RuntimeInitializeOnLoadMethod allowlist documented and frozen for runtime-governance review (DOC-only).
- notes: Outside `Dev/Editor/Legacy/QA`, only `Core/Logging/DebugUtility.cs` and `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` are allowed.
















## RuntimeMode + Logging
**Canonical Rail:** `Core/Logging/DebugUtility.cs` aplica a policy canônica de logging em runtime; `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` é o entrypoint de boot que chama `InitializeLogging()`, e `Infrastructure/RuntimeMode/RuntimeModeConfig.cs` + `IRuntimeModeProvider` + `ConfigurableRuntimeModeProvider` governam strict/release e degraded reporting.

**Top 5 redundancias**
- `GlobalCompositionRoot.Entry.InitializeLogging()` e `DebugManagerConfig.ApplyConfiguration()` continuam tocando a mesma policy de `DebugUtility`, mas agora o runtime delega o apply real ao single owner e o writer residual ficou restrito ao trilho dev-only.
- `NEWSCRIPTS_MODE` e `RuntimeModeConfig.modeOverride` influenciam runtime em camadas diferentes do boot.
- `RuntimeModeConfigLoader` e `GlobalCompositionRoot.BootstrapConfig` relêm a mesma config para responsabilidades diferentes.
- `NEWSCRIPTS_BASELINE_ASSERTS` entra no pipeline de boot fora do eixo principal de runtime mode/logging.
- `DebugLogSettings` continua como asset dev-only sem loader runtime canônico.

**Risco de mudanca:** Med  
**Ordem recomendada de limpeza:** 10/10 (governanca/documentacao; sem urgencia de runtime).

**RM-1.1 status:** inventory done (DOC-only).
- live doc: `Docs/Modules/RuntimeMode-Logging.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Logging-Cleanup-Audit-v1.md`
- owners canônicos: `DebugUtility`, `GlobalCompositionRoot.Entry`, `RuntimeModeConfigLoader`, `IRuntimeModeProvider`/`ConfigurableRuntimeModeProvider`
- candidatos `B dev-only`: `DebugManagerConfig`, `DebugLogSettings`
- riscos reais: duplo writer de policy de logging (runtime default vs dev-only), toggle layering (`NEWSCRIPTS_MODE` + `RuntimeModeConfig.modeOverride`), leitura duplicada de `RuntimeModeConfig`
**RM-1.2 status:** DONE (behavior-preserving).
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Logging-Cleanup-Audit-v2.md`
- runtime writer consolidado em `Core/Logging/DebugUtility.cs`; `GlobalCompositionRoot.Entry.cs` virou delegador de bootstrap policy.
- idempotencia adicionada com anchors `[OBS][RuntimeMode] LoggingPolicyApplied`, `dedupe_same_frame` e `dedupe_same_key`.
**RM-1.3 status:** DONE (behavior-preserving).
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Logging-Cleanup-Audit-v3.md`
- dedupe logs normalizados para `LoggingPolicyApplySkipped reason=...`; trigger dev-only adicionado para gerar evidencia sob demanda.
- menu editor-only: `ImmersiveGames/NewScripts/Dev/Force LoggingPolicy Reapply Evidence`.

**RM-1.4 status:** DONE (behavior-preserving).
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Logging-Cleanup-Audit-v4.md`
- `policyKey` agora e emitida apenas por `DebugUtility.BuildLoggingPolicyKey(...)`, com ordem fixa de campos e contrato explicito para dedupe/evidencia.
- sem mudanca de boot order; `GlobalCompositionRoot.Entry.cs` permaneceu somente como callsite de `ApplyLoggingPolicyFromBootstrap(...)`.

**RM-1.5 status:** DONE (DOC-only).
- shared source of truth criada em `Docs/Shared/Build-Matrix.md`; `RuntimeMode-Logging.md` e `DevQA.md` agora so referenciam a matriz.
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Guard-Governance-Audit-v1.md`
- layering contract congelado: `DevQA` pode fornecer harness/evidencia, mas `RuntimeMode/Logging` continua owner do boot logging e da allowlist de bootstrap.
**RM-1.6 status:** DONE (tooling + docs only).
- gate canonico: `Tools/Gates/Run-NewScripts-RgGates.ps1`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Guard-Governance-Audit-v1.md`
- o script executa os gates A / A2 / B sem ambiguidade de substring entre `InitializeOnLoadMethod` e `RuntimeInitializeOnLoadMethod`.
## InputModes
**Canonical Rail:** `GlobalCompositionRoot.InputModes` registra `IInputModeService` e `SceneFlowInputModeBridge`; `InputModeService` faz o switch de action maps e os consumers de UI/estado delegam a ele por contrato.

**Top 5 redundancias**
- `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs` permanece apenas como shim legado explícito; o registro canônico já foi consolidado no boot global.
- Defaults/fallbacks de map names foram consolidados em `Modules/InputModes/Runtime/InputModesDefaults.cs`; qualquer novo desvio agora deve ser tratado como regressão de governança.
- Vários consumers pedem `IInputModeService` diretamente (`PauseOverlayController`, `PostGameOwnershipService`, `PostGameOverlayController`, `GameLoopService`, `ConfirmToStartIntroStageStep`).
- `SceneFlowInputModeBridge` acumula ownership de InputMode e sync de GameLoop no mesmo ponto.
- Pause ownership fica espalhado entre overlay, gate bridge e state service, com impacto indireto no trilho de input.

**Risco de mudança:** Med-High  
**Ordem recomendada de limpeza:** 6/10.

**IM-1.1 status:** DONE (DOC-only).
- live doc: `Docs/Modules/InputModes.md`
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/InputModes-Cleanup-Audit-v1.md`
- owners canônicos: `GlobalCompositionRoot.InputModes`, `IInputModeService`, `InputModeService`, `SceneFlowInputModeBridge`, `RuntimeModeConfig.InputModesSettings`
- principal candidato IM-1.2 remanescente: reduzir callers diretos de `IInputModeService` e separar melhor o ownership do `SceneFlowInputModeBridge`.
**IM-1.2a status:** DONE (behavior-preserving).
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/InputModes-Cleanup-Audit-v2.md`
- `InputModeBootstrap` foi movido para `Modules/InputModes/Legacy/Bootstrap/InputModeBootstrap.cs` e neutralizado como shim explícito/no-op.
- o único registro restante de `IInputModeService` no runtime canônico está em `GlobalCompositionRoot.InputModes.cs`.

**IM-1.2b status:** DONE (behavior-preserving).
- snapshot: `Docs/Reports/Audits/2026-03-06/Modules/InputModes-Cleanup-Audit-v3.md`
- defaults de ActionMap centralizados em `Modules/InputModes/Runtime/InputModesDefaults.cs`.
- `GlobalCompositionRoot.InputModes.cs` agora resolve nomes finais via helper canônico; `RuntimeModeConfig` referencia as mesmas constantes.
- sem mudança de pipeline e sem mudança de contrato público.



**IM-1.2c status:** DONE (DOC-only).
- snapshot: Docs/Reports/Audits/2026-03-06/Modules/InputModes-Cleanup-Audit-v4.md
- inventario confirmou multiplos writers diretos de IInputModeService em Pause, PostGame, GameLoop, IntroStage e SceneFlow.
- contrato-alvo registrado: InputModeService segue executor, mas o runtime deve convergir para um unico writer/coordinator com os demais modulos virando requestors.


