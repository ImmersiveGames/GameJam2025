# Folder Naming Audit - NewScripts

Auditoria baseada no conteúdo real de `Assets/_ImmersiveGames/NewScripts/**`, com foco em semântica de nome de pasta e não em arquitetura ideal, documentação ou histórico.

## Resumo executivo

### Pastas com nome bom hoje
- `Core`
- `Orchestration/SceneFlow`
- `Orchestration/WorldReset`
- `Orchestration/ResetInterop`
- `Orchestration/Navigation`
- `Game/Content/Definitions/Levels`
- `Experience/Audio`
- `Experience/Save`
- `Experience/Preferences`
- `Experience/Frontend`

### Pastas com nome ruim, enganoso ou histórico
- `Game/Gameplay/Rearm`
- `Orchestration/GameLoop`
- `Orchestration/LevelLifecycle`
- `Game/Gameplay/State`
- `Orchestration/SceneReset`
- `Experience/PostRun`  <span style="white-space:nowrap;">(parcialmente histórico)</span>
- `Experience/Camera`  <span style="white-space:nowrap;">(amplo demais)</span>

### As 5 piores
1. `Game/Gameplay/Rearm`
2. `Orchestration/GameLoop`
3. `Orchestration/LevelLifecycle`
4. `Game/Gameplay/State`
5. `Orchestration/SceneReset`

### Veredito sobre `SceneReset`
- Não está totalmente errado.
- O conteúdo real mostra um pipeline local de reset com gate, hooks, despawn/spawn e queue/controller.
- O problema é de precisão: o nome atual aponta para o tema, mas não para a responsabilidade principal.
- Se houver rename, `SceneResetExecution` seria mais honesto do que `SceneReset`.

## Tabela principal

| pasta atual | arquivos analisados | o que essa pasta realmente faz hoje | o nome atual combina? | problema do nome atual | nome sugerido | risco de renomear agora | confiança |
|---|---|---|---|---|---|---|---|
| `Core` | `Core/Composition/DependencyManager.cs`; `Core/Events/EventBus.cs`; `Core/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`; `Core/Infrastructure/SceneComposition/SceneCompositionExecutor.cs`; `Core/Infrastructure/SimulationGate/SimulationGateService.cs` | Infra base: DI, eventos, bootstrap, composição de cenas e gate de simulação | sim | guarda-chuva amplo, mas fiel ao conteúdo | sem mudança | baixo | alta |
| `Orchestration/SceneReset` | `Orchestration/SceneReset/Runtime/SceneResetFacade.cs`; `Orchestration/SceneReset/Runtime/SceneResetPipeline.cs`; `Orchestration/SceneReset/Bindings/SceneResetController.cs`; `Orchestration/SceneReset/Runtime/Phases/SpawnPhase.cs`; `Orchestration/SceneReset/Runtime/Phases/DespawnPhase.cs` | Execução local de reset de mundo/cena com gate, hooks, queue e fases de spawn/despawn | parcialmente | o nome não revela que é o executor local do reset, não só um conceito de cena | `SceneResetExecution` | alto | média |
| `Orchestration/SceneFlow` | `Orchestration/SceneFlow/Bootstrap/SceneFlowBootstrap.cs`; `Orchestration/SceneFlow/Transition/Runtime/SceneTransitionService.cs`; `Orchestration/SceneFlow/Navigation/Runtime/SceneRouteDefinition.cs`; `Orchestration/SceneFlow/Readiness/Runtime/GameReadinessService.cs`; `Orchestration/SceneFlow/Interop/SceneFlowInputModeBridge.cs` | Fluxo macro de cena: navegação, loading, readiness, fade e bridges | sim | amplo, mas correto | sem mudança | baixo | alta |
| `Orchestration/WorldReset` | `Orchestration/WorldReset/Application/WorldResetService.cs`; `Orchestration/WorldReset/Application/WorldResetOrchestrator.cs`; `Orchestration/WorldReset/Runtime/WorldResetRequestService.cs`; `Orchestration/WorldReset/Validation/WorldResetValidationPipeline.cs`; `Orchestration/WorldReset/Policies/SceneRouteResetPolicy.cs` | Pipeline canônico de reset de mundo com policy, guards, validação e execução | sim | nenhum relevante | sem mudança | baixo | alta |
| `Orchestration/ResetInterop` | `Orchestration/ResetInterop/Runtime/SceneFlowWorldResetDriver.cs`; `Orchestration/ResetInterop/Runtime/WorldResetCompletionGate.cs`; `Orchestration/ResetInterop/Runtime/WorldResetTokens.cs` | Ponte entre SceneFlow e WorldReset, incluindo gate de conclusão | sim | genérico, mas semanticamente correto | sem mudança | baixo | alta |
| `Orchestration/Navigation` | `Orchestration/Navigation/GameNavigationService.cs`; `Orchestration/Navigation/GameNavigationCatalogAsset.cs`; `Orchestration/Navigation/GameNavigationIntents.cs`; `Orchestration/Navigation/NavigationTaskRunner.cs` | Catálogo de intents e despacho de navegação via SceneFlow | sim | um pouco amplo, mas fiel | sem mudança | baixo | alta |
| `Orchestration/LevelLifecycle` | `Orchestration/LevelLifecycle/Runtime/LevelFlowRuntimeService.cs`; `Orchestration/LevelLifecycle/Runtime/LevelMacroPrepareService.cs`; `Orchestration/LevelLifecycle/Runtime/LevelSwapLocalService.cs`; `Orchestration/LevelLifecycle/Runtime/PostLevelActionsService.cs`; `Orchestration/LevelLifecycle/Runtime/RestartContextService.cs` | Seleção, preparo, swap e restart de níveis; o vocabulário interno é `LevelFlow` | não | o conteúdo já não usa "lifecycle" como termo central; parece histórico | `LevelFlow` | médio | alta |
| `Orchestration/GameLoop` | `Orchestration/GameLoop/RunLifecycle/Core/GameLoopService.cs`; `Orchestration/GameLoop/RunLifecycle/Core/GameLoopStateMachine.cs`; `Orchestration/GameLoop/IntroStage/IntroStageControlService.cs`; `Orchestration/GameLoop/RunOutcome/GameRunOutcomeService.cs`; `Orchestration/GameLoop/Pause/GamePauseOverlayController.cs` | Lifecycle da run: intro stage, pause, outcome e sync com SceneFlow | não | "loop" é genérico demais para a superfície real | `RunLifecycle` | alto | alta |
| `Game/Content/Definitions/Levels` | `Game/Content/Definitions/Levels/Config/LevelCollectionAsset.cs`; `Game/Content/Definitions/Levels/Config/LevelDefinitionAsset.cs`; `Game/Content/Definitions/Levels/Runtime/LevelDefinition.cs`; `Game/Content/Definitions/Levels/Runtime/LevelFlowContentDefaults.cs` | Definições e coleções de level, com defaults de content | sim | amplo, mas honesto | sem mudança | baixo | alta |
| `Game/Gameplay/State` | `Game/Gameplay/State/Core/GameplayStateSnapshot.cs`; `Game/Gameplay/State/Gate/GameplayStateGate.cs`; `Game/Gameplay/State/RuntimeSignals/GameplayRuntimeSignalsAdapter.cs`; `Game/Gameplay/State/Core/GameplayAction.cs`; `Game/Gameplay/State/Core/SystemAction.cs`; `Game/Gameplay/State/Core/UiAction.cs` | Gate de ações de gameplay/UI/system baseado em readiness, pause e loop | não | "State" esconde que o núcleo é gating/autorização de ações | `GameplayStateGate` | médio | alta |
| `Game/Gameplay/Rearm` | `Game/Gameplay/Rearm/Coordination/ActorGroupRearmOrchestrator.cs`; `Game/Gameplay/Rearm/Discovery/ActorGroupRearmTargetResolver.cs`; `Game/Gameplay/Rearm/Execution/ActorGroupRearmExecutor.cs`; `Game/Gameplay/Rearm/Core/ActorGroupRearmContracts.cs` | Reset de gameplay por grupo de atores, com cleanup/restore/rebind | não | termo obscuro/histórico; não diz "reset" | `GameplayReset` | médio | alta |
| `Experience/PostRun` | `Experience/PostRun/Handoff/PostStageCoordinator.cs`; `Experience/PostRun/Result/PostRunResultService.cs`; `Experience/PostRun/Ownership/PostRunOwnershipService.cs` | Handoff, ownership, presentation e result do pós-run | parcialmente | o código fala mais `PostRun`/`PostStage` do que o nome antigo | `PostRun` | médio | média |
| `Experience/Audio` | `Experience/Audio/Runtime/Core/AudioBgmService.cs`; `Experience/Audio/Runtime/Core/AudioGlobalSfxService.cs`; `Experience/Audio/Semantics/AudioEntitySemanticService.cs`; `Experience/Audio/Runtime/AudioSettingsService.cs`; `Experience/Audio/Context/AudioBgmContextService.cs`; `Experience/Audio/Bridges/NavigationLevelRouteBgmBridge.cs` | Subsistema completo de áudio: BGM, SFX, semântica, settings e contexto | sim | amplo, mas fiel | sem mudança | baixo | alta |
| `Experience/Save` | `Experience/Save/Orchestration/SaveOrchestrationService.cs`; `Experience/Save/Checkpoint/CheckpointService.cs`; `Experience/Save/Progression/ProgressionService.cs` | Orquestra checkpoint + progression + hooks de save | sim | umbrella amplo, mas honesto | sem mudança | baixo | alta |
| `Experience/Preferences` | `Experience/Preferences/Runtime/PreferencesService.cs`; `Experience/Preferences/Bindings/AudioPreferencesOptionsBinder.cs`; `Experience/Preferences/Bindings/VideoPreferencesOptionsBinder.cs` | Estado, preview, persistência e aplicação de prefs de áudio/vídeo | sim | nenhum relevante | sem mudança | baixo | alta |
| `Experience/Frontend` | `Experience/Frontend/UI/Panels/FrontendPanelsController.cs`; `Experience/Frontend/UI/Runtime/FrontendQuitService.cs`; `Experience/Frontend/UI/Bindings/MenuPlayButtonBinder.cs` | UI de front-end/menu e wiring de intents | sim | nenhum relevante | sem mudança | baixo | alta |
| `Experience/Camera` | `Experience/Camera/GameplayCameraResolver.cs`; `Experience/Camera/GameplayCameraBinder.cs` | Registro e resolução de câmera de gameplay por player | parcialmente | "Camera" é amplo demais para o conteúdo real | `GameplayCamera` | baixo | alta |

## Casos prioritários

### `SceneReset`
- Sugere: reset de cena, algo local e curto.
- Realidade: pipeline local de reset com gate, hooks, despawn/spawn, queue e controller.
- Problema: o nome não destaca que é o executor operacional do reset.
- Nome mais honesto: `SceneResetExecution`.

### `GameLoop`
- Sugere: loop geral do jogo.
- Realidade: lifecycle da run, intro stage, pause, outcome e sincronização com SceneFlow.
- Problema: é genérico demais e não reflete o vocabulário interno real.
- Nome mais honesto: `RunLifecycle`.

### `LevelLifecycle`
- Sugere: ciclo de vida genérico de level.
- Realidade: preparo, seleção, swap, restart e limpeza de levels, com linguagem de `LevelFlow`.
- Problema: o nome parece histórico; o conteúdo não usa "lifecycle" como eixo principal.
- Nome mais honesto: `LevelFlow`.

### `PostRun`
- Sugere: tudo depois da partida.
- Realidade: handoff, ownership, presentation e result do pós-run.
- Problema: aceitável, mas menos preciso que o vocabulário real do código.
- Nome mais honesto: `PostRun`.

### `Save`
- Sugere: persistência.
- Realidade: checkpoint + progression + hooks de save e orquestração de eventos.
- Problema: nenhum relevante; o nome é amplo, mas na medida certa.
- Nome mais honesto: `Save` já serve.

### `Audio`
- Sugere: subsistema de áudio.
- Realidade: BGM, SFX, routing, semantics, settings e bridges de contexto.
- Problema: nenhum relevante; é um guarda-chuva apropriado.
- Nome mais honesto: `Audio` já serve.

## Renomeações mais seguras

- `Game/Gameplay/Rearm` -> `Game/Gameplay/GameplayReset`
  - Faz sentido porque o conteúdo é reset de gameplay por grupos de atores.
  - Risco: médio.
- `Orchestration/LevelLifecycle` -> `Orchestration/LevelFlow`
  - Faz sentido porque o próprio código já fala em `LevelFlow`.
  - Risco: médio.
- `Game/Gameplay/State` -> `Game/Gameplay/GameplayStateGate`
  - Faz sentido porque o centro real é autorização/gating de ações.
  - Risco: médio.

## Renomeações que devem esperar

- `Orchestration/SceneReset` -> `Orchestration/SceneResetExecution`
  - Ainda cruza com `WorldReset`, `ResetInterop` e `SceneFlow`.
  - Precisa alinhamento terminológico antes.
- `Orchestration/GameLoop` -> `Orchestration/RunLifecycle`
  - A superfície é grande e espalhada; rename agora teria impacto amplo.
  - Precisa revisão coordenada de contratos, pastas e referências.
- `Experience/PostRun` -> `Experience/PostRun`
  - É mais honesto, mas o nome atual ainda é aceitável.
  - Vale esperar uma decisão terminológica oficial do projeto.

## Conclusão objetiva

- Pasta com nome mais enganoso hoje: `Game/Gameplay/Rearm`.
- As 3 melhores renomeações para começar:
  1. `Game/Gameplay/Rearm` -> `GameplayReset`
  2. `Orchestration/LevelLifecycle` -> `LevelFlow`
  3. `Game/Gameplay/State` -> `GameplayStateGate`
- Nomes que devem permanecer: `SceneFlow`, `WorldReset`, `ResetInterop`, `Navigation`, `Audio`, `Save`, `Preferences`, `Frontend`, `Game/Content/Definitions/Levels` e os blocos `Core` analisados.
- Recomendação objetiva para `SceneReset`: não é a primeira renomeação a fazer; se houver mudança, `SceneResetExecution` é o nome mais honesto.
