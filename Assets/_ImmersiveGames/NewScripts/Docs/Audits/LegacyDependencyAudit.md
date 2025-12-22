# Auditoria de Dependências Legadas — NewScripts

## Sumário
- Total de referências ao legado `_ImmersiveGames.Scripts.*`: **82**.
- Distribuição por tipo: **Código: 82**, **Asmdef: 0**, **Docs: 0**.
- Arquivos de código afetados: **36** em `Assets/_ImmersiveGames/NewScripts`.

## Tabela 1 — Código
| Caminho | Linhas/Trechos com referência | Tipos legados usados | Categoria |
| --- | --- | --- | --- |
| Gameplay/Player/Movement/NewPlayerMovementController.cs | 13-19 (usings de ActorSystems, GameplaySystems.Domain/Reset, PlayerControllerSystem.Movement, StateMachineSystems, Utils.DebugSystems/DependencySystems) | IActor, IPlayerDomain, IResetInterfaces/IResetScopeFilter/IResetOrder, ActionType, DebugUtility, DependencyManager | Reset |
| Gameplay/GameLoop/GameLoopDriver.cs | 1-2 | DebugUtility, DependencyManager | DI |
| Gameplay/GameLoop/GameLoopEventInputBridge.cs | 2-5 | GameStartEvent/GamePauseEvent/GameResumeRequestedEvent/GameResetRequestedEvent, EventBus/EventBinding, DebugUtility, DependencyManager | DI |
| Gameplay/GameLoop/GameLoopBootstrap.cs | 1-2 | DebugUtility, DependencyManager | DI |
| Infrastructure/Execution/Gate/GamePauseGateBridge.cs | 6-9 | GamePauseEvent/GameResumeRequestedEvent, EventBus/EventBinding, DebugUtility, DependencyManager | DI |
| Infrastructure/Execution/Gate/SimulationGateService.cs | 3 | DebugUtility | Debug |
| Infrastructure/Scene/GameReadinessService.cs | 2-4 | SceneTransitionStarted/ScenesReady/Completed events, EventBus/EventBinding, DebugUtility | Outro |
| Infrastructure/Scene/NewSceneBootstrapper.cs | 3-4 | DebugUtility, DependencyManager | DI |
| Infrastructure/Actors/PlayerActorAdapter.cs | 2 | LegacyActor alias (_ImmersiveGames.Scripts.ActorSystems.IActor_) | Outro |
| Infrastructure/Actors/ActorRegistry.cs | 3 | DebugUtility | Debug |
| Infrastructure/Actors/PlayerActor.cs | 3-4 | DebugUtility, DependencyManager | DI |
| Infrastructure/Fsm/ITransition.cs | 1 | IPredicate (Utils.Predicates) | Outro |
| Infrastructure/Fsm/StateMachineBuilder.cs | 1 | Preconditions/IPredicate (Utils.Predicates) | Outro |
| Infrastructure/Fsm/StateMachine.cs | 3 | IPredicate (Utils.Predicates) | Outro |
| Infrastructure/Fsm/Transition.cs | 2 | IPredicate (Utils.Predicates) | Outro |
| Infrastructure/GlobalBootstrap.cs | 9-10,17 | DebugUtility, DependencyManager, IStateDependentService (StateMachineSystems) | DI |
| Infrastructure/State/NewScriptsStateDependentService.cs | 5-9 | GameStart/GamePause/GameResumeRequested events, EventBus/EventBinding, ActionType (StateMachineSystems), DebugUtility, DependencyManager | DI |
| Infrastructure/Cameras/NewGameplayCameraBinder.cs | 7-8 | DebugUtility, DependencyManager | DI |
| Infrastructure/Cameras/CameraResolverService.cs | 9 | DebugUtility | Debug |
| Infrastructure/QA/WorldLifecycleBaselineRunner.cs | 10-15 | GameManagerSystems.Events, EventBus, DebugUtility, ActionType (StateMachineSystems), DependencyManager | DI |
| Infrastructure/QA/BaselineDebugBootstrap.cs | 2 | DebugUtility | Debug |
| Infrastructure/World/WorldSpawnServiceRegistry.cs | 3 | DebugUtility | Debug |
| Infrastructure/World/SceneLifecycleHookLoggerB.cs | 2 | DebugUtility | Debug |
| Infrastructure/World/PlayerSpawnService.cs | 4 | DebugUtility | Debug |
| Infrastructure/World/DummyActorSpawnService.cs | 4 | DebugUtility | Debug |
| Infrastructure/World/Scopes/Players/PlayersResetParticipant.cs | 6-17 | GameplaySystems.Domain/Reset, DebugUtility, DependencyManager, aliases para ResetContext/ResetRequest/ResetScope/ResetStructs, LegacyActor | Reset |
| Infrastructure/World/WorldLifecycleRuntimeDriver.cs | 5-8 | SceneTransition events, EventBus/EventBinding, DebugUtility, DependencyManager | DI |
| Infrastructure/World/WorldLifecycleController.cs | 6-7 | DebugUtility, DependencyManager | DI |
| Infrastructure/World/WorldLifecycleOrchestrator.cs | 9-10 | DependencyManager.IDependencyProvider, DebugUtility | DI |
| Infrastructure/World/SceneLifecycleHookLoggerA.cs | 2 | DebugUtility | Debug |
| Infrastructure/World/IWorldSpawnContext.cs | 1 | DebugUtility | Debug |
| Infrastructure/World/WorldSpawnServiceFactory.cs | 3-4 | DebugUtility, DependencyManager | DI |
| QA/WorldLifecycleQATester.cs | 8-9 | DebugUtility, DependencyManager | DI |
| QA/QAFaultySceneLifecycleHook.cs | 4 | DebugUtility | Debug |
| QA/ActorLifecycleHookLogger.cs | 3 | DebugUtility | Debug |
| QA/WorldLifecycleAutoTestRunner.cs | 5-7 | GameManagerSystems.Events, EventBus, DebugUtility | DI |

## Tabela 2 — Asmdef
Nenhuma `.asmdef` em `Assets/_ImmersiveGames/NewScripts` referencia assemblies do legado `_ImmersiveGames.Scripts.*`.

## Tabela 3 — Docs
Nenhuma referência ao legado encontrada em documentação dentro de `Assets/_ImmersiveGames/NewScripts/Docs`.

## Parte B — Propostas de Ação por Categoria (não executadas)
Cada categoria abaixo segue as opções solicitadas e uma recomendação preliminar.

- **Debug**
  - A) Adaptar via Bridge: criar um wrapper `DebugUtility` em `NewScripts/Gameplay/Bridges` para mapear chamadas para um logger interno.
  - B) Extrair Infra: duplicar utilitário de logging equivalente em `NewScripts/Infrastructure` com API compatível e redirecionar usos.
  - C) Recriar: implementar logger mínimo nativo do NewScripts, reduzindo superfície de API.
  - **Recomendação:** B — facilita substituição gradual preservando assinaturas atuais e reduz acoplamento ao legado.

- **DI**
  - A) Adaptar via Bridge: criar adaptadores em `NewScripts/Gameplay/Bridges` que encapsulem `DependencyManager`/`Inject` e exponham interfaces próprias.
  - B) Extrair Infra: trazer um contêiner leve para `NewScripts/Infrastructure` (ex.: provedor de serviços simples) mantendo contratos esperados pelo código atual.
  - C) Recriar: definir um novo mecanismo de resolução nativo (factory/service locator mínimo) e migrar pontos de uso.
  - **Recomendação:** B — extrair um provedor compatível dentro do NewScripts para eliminar dependência direta sem reescrever todas as entradas de DI de imediato.

- **Reset**
  - A) Adaptar via Bridge: criar interfaces/bridges em `NewScripts/Gameplay/Bridges` que traduzam `IResetInterfaces`/`ResetScope`/`ResetContext` para contratos nativos.
  - B) Extrair Infra: copiar contratos essenciais de reset para `NewScripts/Infrastructure` garantindo compatibilidade binária com chamadas existentes.
  - C) Recriar: definir modelo de reset próprio (escopos/contextos) e migrar participantes gradualmente.
  - **Recomendação:** A — manter contratos legados através de bridges enquanto se modela um ciclo de reset próprio, reduzindo risco para gameplay já integrado.

- **Outro (Predicates/EventBus/ActorSystems/SceneTransition)**
  - A) Adaptar via Bridge: criar adaptadores específicos (ex.: `EventBusBridge`, `PredicateAdapter`, `LegacyActorAdapter` estendido) em `NewScripts/Gameplay/Bridges`.
  - B) Extrair Infra: portar utilitários mínimos de predicates/event bus/scene flow para `NewScripts/Infrastructure` preservando assinaturas usadas.
  - C) Recriar: implementar versões nativas simplificadas e atualizar chamadas para os novos contratos.
  - **Recomendação:** A — iniciar com bridges pontuais para desatrelar rapidamente pontos sensíveis (event bus/scene flow/actors) antes de decidir por extração completa.

## Doc Placement Issues
Nenhum problema de localização ou duplicação de docs relacionado a NewScripts foi identificado fora de `Assets/_ImmersiveGames/NewScripts/Docs`.
