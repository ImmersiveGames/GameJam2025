# NewScripts — Documentação

Este conjunto de documentos descreve a arquitetura **NewScripts** (Unity) e o estado atual do pipeline de **Scene Flow** + **Fade** e do **World Lifecycle** (reset determinístico por escopos).

## Onde está a documentação
Arquivos canônicos (este pacote):
- [README.md](README.md) — índice e orientação rápida.
- [ARCHITECTURE.md](ARCHITECTURE.md) — visão arquitetural de alto nível.
- [ARCHITECTURE_TECHNICAL.md](ARCHITECTURE_TECHNICAL.md) — detalhes técnicos, módulos e responsabilidades.
- [WORLD_LIFECYCLE.md](WORLD_LIFECYCLE.md) — semântica operacional do reset determinístico do mundo.
- [DECISIONS.md](DECISIONS.md) — decisões/ADRs resumidos (o “porquê”).
- [EXAMPLES_BEST_PRACTICES.md](EXAMPLES_BEST_PRACTICES.md) — exemplos e práticas recomendadas.
- [GLOSSARY.md](GLOSSARY.md) — glossário de termos.
- [CHANGELOG-docs.md](CHANGELOG-docs.md) — histórico de alterações desta documentação.
- [ADR-0009-FadeSceneFlow](ADRs/ADR-0009-FadeSceneFlow.md) — ADR específico do Fade + SceneFlow (NewScripts).
- [ADR-0010-LoadingHud-SceneFlow](ADRs/ADR-0010-LoadingHud-SceneFlow.md) — ADR específico do Loading HUD integrado ao SceneFlow.
- [ADR-0013-Ciclo-de-Vida-Jogo](ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md) — ADR de reset por escopos + gate no ciclo de vida do jogo.

## Reports (evidências)
Status: evidências atualizadas em 2025-12-31.

### Validação de Produção (master)
- [SceneFlow-Production-EndToEnd-Validation](Reports/SceneFlow-Production-EndToEnd-Validation.md)
- [SceneFlow-Assets-Checklist](Reports/SceneFlow-Assets-Checklist.md)

### Reports históricos (SceneFlow)
- [SceneFlow-Smoke-Result](Reports/SceneFlow-Smoke-Result.md)
- [SceneFlow-Gameplay-To-Menu-Report](Reports/SceneFlow-Gameplay-To-Menu-Report.md)

### Outros reports
- [WORLDLIFECYCLE_RESET_STATUS.md](WORLDLIFECYCLE_RESET_STATUS.md)
- [GameLoop](Reports/GameLoop.md)
- [QA-Audit-2025-12-27](Reports/QA-Audit-2025-12-27.md)
- [QA-GameplayReset-RequestMatrix](Reports/QA-GameplayReset-RequestMatrix.md)
- [Legacy-Cleanup-Report](Reports/Legacy-Cleanup-Report.md)
- [WorldLifecycle-Reset-Analysis](Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md)
- [WorldLifecycle-Spawn-Analysis](Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md)

## Status atual (resumo)
- Added: **Gameplay Reset module** ([Gameplay/Reset/](../Gameplay/Reset/)) com contratos e semântica estável:
    - `GameplayResetPhase` (Cleanup/Restore/Rebind) e `GameplayResetTarget` (AllActorsInScene/PlayersOnly/EaterOnly/ActorIdSet).
    - `GameplayResetRequest` + `GameplayResetContext`.
    - `IGameplayResettable` (+ `IGameplayResettableSync`), `IGameplayResetOrder`, `IGameplayResetTargetFilter`.
    - `IGameplayResetOrchestrator` + `IGameplayResetTargetClassifier` (serviços por cena).
- Added: **QA isolado para validar reset por grupos** (sem depender de Spawn 100%):
    - `GameplayResetQaSpawner` cria atores de teste (ex.: Players) e registra `IGameplayResettable` de prova.
    - `GameplayResetQaProbe` confirma execução das fases via logs (Cleanup/Restore/Rebind).
- Added: **Loading HUD integrado ao SceneFlow** com sinal de HUD pronto e ordenação acima do Fade.
- Updated: integração **WorldLifecycle → Gameplay Reset** via `PlayersResetParticipant` (gameplay) plugado como `IResetScopeParticipant` no soft reset por escopos.

## Explicação simples
Quando o jogador sai do menu e entra no gameplay, o jogo passa por uma “esteira de preparação”.
Essa preparação inclui **carregar cenas**, **resetar o mundo** e **spawnação/preparação de entidades**.
O carregamento “real” só termina quando o reset conclui e o `WorldLifecycleResetCompletedEvent`
é emitido — **antes do FadeOut**. Ou seja: o jogo só é considerado pronto depois do reset.

## Quando remover o SKIP e por quê
O SKIP existe hoje para **não rodar reset/spawn** no `MenuScene` e no profile `startup`,
evitando comportamento diferente em cenas que não têm mundo de gameplay.

**Decisão atual (usuário):** remover o SKIP **somente quando** a `GameplayScene` estiver pronta
para concluir **reset + spawn/preparação** **antes do FadeOut**. Até lá, o SKIP permanece.
Remover esse SKIP cedo demais muda o comportamento do pipeline (reset rodando em cenas
sem mundo) e pode gerar efeitos colaterais em boot/menu.

## QA (status)

### ATIVOS
- Smoke runner de infraestrutura:
    - [Infrastructure/QA/NewScriptsInfraSmokeRunner.cs](../Infrastructure/QA/NewScriptsInfraSmokeRunner.cs)
    - [Infrastructure/QA/EventBusSmokeQATester.cs](../Infrastructure/QA/EventBusSmokeQATester.cs)
    - [Infrastructure/QA/FilteredEventBusSmokeQATester.cs](../Infrastructure/QA/FilteredEventBusSmokeQATester.cs)
    - [Infrastructure/QA/DebugLogSettingsQATester.cs](../Infrastructure/QA/DebugLogSettingsQATester.cs)
    - [Infrastructure/QA/DependencyDISmokeQATester.cs](../Infrastructure/QA/DependencyDISmokeQATester.cs)
    - [Infrastructure/QA/FsmPredicateQATester.cs](../Infrastructure/QA/FsmPredicateQATester.cs)
    - [Infrastructure/QA/SceneTransitionServiceSmokeQATester.cs](../Infrastructure/QA/SceneTransitionServiceSmokeQATester.cs)
- Hooks/boots ativos:
    - [Infrastructure/QA/BaselineDebugBootstrap.cs](../Infrastructure/QA/BaselineDebugBootstrap.cs)
    - [Infrastructure/QA/PlayerMovementLeakSmokeBootstrap.cs](../Infrastructure/QA/PlayerMovementLeakSmokeBootstrap.cs)
    - [Infrastructure/QA/Editor/PlayerMovementLeakSmokeBootstrapCI.cs](../Infrastructure/QA/Editor/PlayerMovementLeakSmokeBootstrapCI.cs)
    - [Infrastructure/WorldLifecycle/Hooks/QA/SceneLifecycleHookLoggerA.cs](../Infrastructure/WorldLifecycle/Hooks/QA/SceneLifecycleHookLoggerA.cs)
    - [Infrastructure/GameLoop/QA/GameLoopStateFlowQATester.cs](../Infrastructure/GameLoop/QA/GameLoopStateFlowQATester.cs)

### DEPRECATED (tools manuais/legado)
- [QA/Deprecated/WorldLifecycleQATester.cs](../QA/Deprecated/WorldLifecycleQATester.cs)
- [QA/Deprecated/ActorLifecycleHookLogger.cs](../QA/Deprecated/ActorLifecycleHookLogger.cs)
- [QA/Deprecated/WorldLifecycleAutoTestRunner.cs](../QA/Deprecated/WorldLifecycleAutoTestRunner.cs)
- [QA/Deprecated/QAFaultySceneLifecycleHook.cs](../QA/Deprecated/QAFaultySceneLifecycleHook.cs)
- [QA/Deprecated/GameplayResetQaProbe.cs](../QA/Deprecated/GameplayResetQaProbe.cs)
- [QA/Deprecated/GameplayResetQaSpawner.cs](../QA/Deprecated/GameplayResetQaSpawner.cs)
- [QA/Deprecated/SceneFlowPlayModeSmokeBootstrap.cs](../QA/Deprecated/SceneFlowPlayModeSmokeBootstrap.cs)
- [QA/Deprecated/SceneFlowTransitionQAFrontend.cs](../QA/Deprecated/SceneFlowTransitionQAFrontend.cs)
- [QA/Deprecated/WorldLifecycleBaselineRunner.cs](../QA/Deprecated/WorldLifecycleBaselineRunner.cs)
- [QA/Deprecated/WorldLifecycleQATools.cs](../QA/Deprecated/WorldLifecycleQATools.cs)
- [QA/Deprecated/WorldSpawnPipelineQaRunner.cs](../QA/Deprecated/WorldSpawnPipelineQaRunner.cs)
- [QA/Deprecated/WorldMovementPermissionQaRunner.cs](../QA/Deprecated/WorldMovementPermissionQaRunner.cs)
- [QA/Deprecated/GameLoopStartRequestQAFrontend.cs](../QA/Deprecated/GameLoopStartRequestQAFrontend.cs)
- [QA/Deprecated/PauseOverlayDebugTrigger.cs](../QA/Deprecated/PauseOverlayDebugTrigger.cs) (dev-only)
- [QA/Deprecated/GameNavigationDebugTrigger.cs](../QA/Deprecated/GameNavigationDebugTrigger.cs) (dev-only)

Em 2025-12-27 (estado observado em runtime):
- Pipeline **GameLoop → Navigation → SceneTransitionService → Fade/Loading → WorldLifecycle → Gate → Completed** está ativo.
- `NewScriptsSceneTransitionProfile` é resolvido via **Resources** em:
    - `Resources/SceneFlow/Profiles/<profileName>`
- `WorldLifecycleRuntimeCoordinator` recebe `SceneTransitionScenesReadyEvent` e:
    - **SKIP** quando `profile='startup'` ou `activeScene='MenuScene'`
    - Executa reset em gameplay e emite `WorldLifecycleResetCompletedEvent`
- `WorldLifecycleResetCompletionGate` segura o final da transição até o reset concluir.
- `GameReadinessService` controla `SimulationGateTokens.SceneTransition` durante a transição.
- `InputModeService` alterna `FrontendMenu`/`Gameplay`/`PauseOverlay` com base em SceneFlow e PauseOverlay.

## Como ler (ordem sugerida)
1. [ARCHITECTURE.md](ARCHITECTURE.md)
2. [WORLD_LIFECYCLE.md](WORLD_LIFECYCLE.md)
3. [ADR-0009-FadeSceneFlow](ADRs/ADR-0009-FadeSceneFlow.md)
4. [ADR-0010-LoadingHud-SceneFlow](ADRs/ADR-0010-LoadingHud-SceneFlow.md)
5. [ARCHITECTURE_TECHNICAL.md](ARCHITECTURE_TECHNICAL.md)
6. [DECISIONS.md](DECISIONS.md)
7. [EXAMPLES_BEST_PRACTICES.md](EXAMPLES_BEST_PRACTICES.md)
8. [GLOSSARY.md](GLOSSARY.md)
9. [CHANGELOG-docs.md](CHANGELOG-docs.md)

## Convenções usadas nesta documentação
- Não presumimos assinaturas inexistentes. Onde necessário, exemplos são explicitamente marcados como **PSEUDOCÓDIGO**.
- `SceneTransitionContext` é um `readonly struct` (sem `null`, sem object-initializer).
- “NewScripts” e “Legado” coexistem: bridges podem existir, mas o **Fade** do NewScripts não possui fallback para fade legado.

## Fluxo de produção (Menu → Gameplay → Pause → Resume → ExitToMenu → Menu)
1. **Menu → Gameplay (Navigation)**
    - `MenuPlayButtonBinder` chama `IGameNavigationService.RequestToGameplay(reason)`.
    - `GameNavigationService` executa `SceneTransitionService.TransitionAsync` com profile `gameplay`.
2. **SceneTransitionService (pipeline)**
    - Emite `SceneTransitionStartedEvent` → `FadeIn`.
    - Load/Unload/Active → `SceneTransitionScenesReadyEvent`.
    - Aguarda completion gate (`WorldLifecycleResetCompletionGate`).
    - `FadeOut` → `SceneTransitionCompletedEvent`.
3. **WorldLifecycle**
    - `WorldLifecycleRuntimeCoordinator` escuta `ScenesReady`:
        - **Gameplay**: executa reset e emite `WorldLifecycleResetCompletedEvent(signature, reason)`.
        - **Startup/Frontend**: SKIP e emite `WorldLifecycleResetCompletedEvent` com reason `Skipped_StartupOrFrontend`.
4. **GameLoop**
    - `GameLoopSceneFlowCoordinator` aguarda `TransitionCompleted` + `ResetCompleted` antes de chamar `GameLoop.RequestStart()`.
5. **Pause / Resume / ExitToMenu**
    - `PauseOverlayController` publica:
        - `GamePauseCommandEvent` (Show)
        - `GameResumeRequestedEvent` (Hide)
        - `GameExitToMenuRequestedEvent` (ReturnToMenuFrontend)
    - `PauseOverlayController` alterna `InputMode` para `PauseOverlay`/`Gameplay`/`FrontendMenu` e chama
      `IGameNavigationService.RequestToMenu(...)` ao retornar ao menu.
    - `GamePauseGateBridge` mapeia pause/resume para `SimulationGateTokens.Pause`.

## Gate / Readiness
- `GameReadinessService` adquire o token `SimulationGateTokens.SceneTransition` em `SceneTransitionStartedEvent`
  e libera em `SceneTransitionCompletedEvent`.
- `WorldLifecycleOrchestrator` adquire o token `WorldLifecycleTokens.WorldResetToken` durante o reset e libera ao final.

## Evidências (log)
- `GlobalBootstrap` registrando serviços globais: `ISceneTransitionService`, `INewScriptsFadeService`,
  `IGameNavigationService`, `GameLoop`, `InputModeService`, `GameReadinessService`,
  `WorldLifecycleRuntimeCoordinator`, `SceneFlowLoadingService`.
- Startup profile `startup` com reset **SKIPPED** e emissão de
  `WorldLifecycleResetCompletedEvent(reason=Skipped_StartupOrFrontend)`.
- `MenuPlayButtonBinder` desativa botão e dispara `RequestToGameplay`.
- Transição para profile `gameplay` executa reset e o `PlayerSpawnService` spawna `Player_NewScripts`.
- Completion gate aguarda `WorldLifecycleResetCompletedEvent` e prossegue.
- `PauseOverlay` publica `GamePauseCommandEvent`, `GameResumeRequestedEvent`, `GameExitToMenuRequestedEvent`
  e tokens `state.pause` / `flow.scene_transition` aparecem no gate.
