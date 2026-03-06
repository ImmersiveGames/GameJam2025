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

## Infrastructure-Composition
**Canonical Rail:** `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` chama `RegisterEssentialServicesOnly` em `GlobalCompositionRoot.Pipeline.cs`, instala `IGlobalCompositionModule` por stage, e registra servicos canonicos de runtime por metodos especializados (`GameLoop`, `SceneFlow`, `WorldLifecycle`, `Navigation`, `Levels`, `ContentSwap`, `DevQA`).

**Top 5 redundancias**
- `Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs` teve o registro legacy `RegisterRestartSnapshotContentSwapBridge()` removido no cleanup canÃ´nico.
- `Infrastructure/Composition/Modules/*.cs` repetem padrao de gate por stage com logica quase identica.
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
- `Modules/Navigation/Legacy/RestartNavigationBridge.cs` mantido como legado desativado (fora do trilho canÃ´nico).
- `Modules/Navigation/Legacy/RestartSnapshotContentSwapBridge.cs` mantido como no-op legado (fora do trilho canÃ´nico).
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
- Rotas de QA/Dev exercitam caminhos nao-canônicos e podem mascarar redundancias em runtime.

**Risco de mudanca:** High  
**Ordem recomendada de limpeza:** 7/10.

**LF-1.1 aplicado:** isolamento de compat para `Modules/LevelFlow/Legacy/**`:
- `Modules/LevelFlow/Legacy/Bindings/LevelCatalogAsset.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelFlowService.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelMacroRouteCatalog.cs`
- `Modules/LevelFlow/Legacy/Runtime/ILevelContentResolver.cs`
## ContentSwap
**Canonical Rail:** `ContentSwapContextService` + `InPlaceContentSwapService` (registrado como `IContentSwapChangeService`) aplicam swap in-place orientado a contexto. Integracoes de restart compat foram explicitamente neutralizadas no Navigation.

**Top 5 redundancias**
- `RestartSnapshotContentSwapBridge` existe, mas e no-op no trilho canonico.
- `ContentSwapDevBootstrapper` e instalador dev coexistem com pipeline DevQA global.
- Eventos de ContentSwap coexistem com trilho LevelFlow de selecao/aplicacao.
- Modo e plano (`ContentSwapMode`, `ContentSwapPlan`) tem sobreposicao semantica com fluxo de level/local swap.
- Multiplas entradas dev para a mesma operacao de swap.

**Risco de mudanca:** Med  
**Ordem recomendada de limpeza:** 8/10.

## DevQA
**Canonical Rail:** Em `UNITY_EDITOR || DEVELOPMENT_BUILD`, `GlobalCompositionRoot.DevQA.cs` aciona instaladores (`IntroStageDevInstaller`, `ContentSwapDevInstaller`, `SceneFlowDevInstaller`, `LevelFlowDevInstaller`) e GUI runtime de debug (`IntroStageRuntimeDebugGui`).

**Top 5 redundancias**
- Instaladores Dev distribuidos por modulos com padrao quase identico de `EnsureInstalled()`.
- Context menus Dev de SceneFlow/LevelFlow/ContentSwap com responsabilidades parcialmente sobrepostas.
- Drivers dev de WorldLifecycle/Gameplay coexistem com fluxo de producao.
- Varios pontos de compile-flag (`UNITY_EDITOR`, `DEVELOPMENT_BUILD`, `NEWSCRIPTS_DEV`, `NEWSCRIPTS_QA`).
- Instrumentacao de QA espalhada entre modulos e composicao.

**Risco de mudanca:** Low  
**Ordem recomendada de limpeza:** 10/10 (por ultimo, apos estabilizar runtime).

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

## What I did NOT do
- Nao alterei nenhum arquivo `.cs`.
- Nao movi, nao deletei e nao renomeei arquivos.
- Nao executei refactor de runtime; a entrega foi estritamente DOC-only em `Docs/`.
