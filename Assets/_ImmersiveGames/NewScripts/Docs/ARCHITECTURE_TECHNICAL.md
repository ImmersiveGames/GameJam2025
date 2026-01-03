# Arquitetura Técnica — NewScripts

Este documento detalha responsabilidades e fronteiras entre módulos, com foco em evitar acoplamento ao legado.

## Princípios
- **Escopos de DI claros:** Global vs. Scene.
- **Reset determinístico:** resets são orquestrados por fases fixas e executados por participantes/hook registries.
- **Scene Flow event-driven:** o estado de prontidão do jogo deriva de eventos de transição (não de “polling”).
- **Semântica explícita:** `SceneTransitionContext` é `readonly struct` (evitar `null`, evitar object initializer).

## Módulos e responsabilidades (alto nível)

### Infrastructure / DI
- `DependencyManager` e registries de serviço:
    - `GlobalServiceRegistry` (global)
    - `SceneServiceRegistry` (por cena)
- `IDependencyProvider` é a superfície de acesso (TryGet/Resolve) usada por bridges/adapters.

### Scene (Scope de cena)
- `NewSceneBootstrapper`:
    - cria o scope de cena,
    - registra serviços por cena (ex.: `IActorRegistry`, `WorldLifecycleHookRegistry`, etc.),
    - registra participantes de reset (ex.: PlayersResetParticipant),
    - opcionalmente registra spawn services com base em `WorldDefinition`.

### GameLoop
- `IGameLoopService` mantém a FSM macro (Boot/Menu/Playing).
- `GameLoopRuntimeDriver` é `DontDestroyOnLoad` e “ticka” o serviço.
- `GameLoopSceneFlowCoordinator` faz ponte entre:
    - o estado do GameLoop,
    - eventos do SceneFlow,
    - e `RequestStart()` após transição + reset/skip concluídos.

### Readiness / Gate
- `ISimulationGateService` implementa tokens (Acquire/Release).
- `GameReadinessService`:
    - adquire token na transição,
    - publica snapshots (gateOpen/gameplayReady/activeTokens),
    - libera token no `SceneTransitionCompleted`.
- `WorldLifecycleOrchestrator` adquire `WorldLifecycleTokens.WorldResetToken` durante reset e libera ao final.

### Scene Flow (transições)
- `ISceneTransitionService` (implementação: `SceneTransitionService`) é o orquestrador de:
    - load/unload additive,
    - set active scene,
    - emissão de eventos (started/scenesReady/completed).
- Enquanto o loader nativo NewScripts não está migrado:
    - `NewScriptsSceneFlowAdapters` podem usar `SceneManagerLoaderAdapter` como fallback.

#### Loading inclui Reset/Spawn
No pipeline atual, “loading real” **não termina** com `ScenesReady`. Ele só termina quando:
- o **WorldLifecycle** conclui o reset (com hooks/participants), e
- o mundo já passou por **spawn/preparação** quando aplicável.

Isso é formalizado pelo `WorldLifecycleResetCompletedEvent`, que precisa ocorrer **antes**
do `FadeOut`. O jogo só é considerado pronto após o ResetCompleted.

#### Evolução futura: Addressables
Diretriz (sem implementação): tratar o loading como um **conjunto de tarefas agregadas**
para expor progresso/estado ao HUD e ao pipeline.

Exemplo **PSEUDOCÓDIGO / FUTURO** de tarefas agregadas:
- `SceneLoadTask` (load/unload/additive + active scene)
- `WorldResetTask` (reset + spawn/preparação do mundo)
- `AddressablesWarmupTask` (warmup/preload de assets)

Esses nomes são apenas vocabulário de planejamento; não representam APIs existentes.

### Fade (NewScripts)
- `INewScriptsFadeService`:
    - carrega `FadeScene` (Additive) on-demand,
    - encontra `NewScriptsFadeController`,
    - executa FadeIn/FadeOut.
- `NewScriptsSceneTransitionProfile`:
    - ScriptableObject com parâmetros de fade (durations/curves),
    - criado via menu `ImmersiveGames/NewScripts/SceneFlow/Transition Profile`.
- `NewScriptsSceneTransitionProfileResolver`:
    - carrega via `Resources.Load<NewScriptsSceneTransitionProfile>()`,
    - padrão de paths:
        - `SceneFlow/Profiles/<profileName>`
        - `<profileName>`

### World Lifecycle (integração)
#### Gameplay Reset (grupos) — módulo de gameplay, acionável por QA

Para reduzir acoplamento com spawn enquanto ele evolui, o baseline introduz um reset de gameplay em `Gameplay/Reset/` com contratos estáveis e fases explícitas:

- **Serviços por cena** (registrados no `NewSceneBootstrapper`):
    - `IGameplayResetOrchestrator`: resolve targets, aplica ordenação e executa `Cleanup → Restore → Rebind`.
    - `IGameplayResetTargetClassifier`: mapeia `GameplayResetTarget` para os GameObjects/atores participantes no escopo da cena (ex.: players).
- **Participantes**:
    - `IGameplayResettable` (async) é o contrato recomendado.
    - `IGameplayResetOrder` define ordenação dentro de cada fase (menor primeiro).
    - `IGameplayResetTargetFilter` permite ignorar targets específicos (ex.: um componente que só participa em `PlayersOnly`).

Integração com WorldLifecycle:
- `PlayersResetParticipant` (gameplay) é registrado como `IResetScopeParticipant` e funciona como bridge: ao receber `ResetScope.Players`, emite uma requisição `GameplayResetRequest(Target=PlayersOnly, ...)` para o `IGameplayResetOrchestrator` da cena.

Validação (QA):
- `GameplayResetQaSpawner` cria instâncias de teste (ex.: `QA_Player_00/01`) e dispara `IGameplayResetOrchestrator.RequestResetAsync(...)`.
- `GameplayResetQaProbe` registra logs por fase para confirmar que o pipeline está completo, independentemente do spawn.

- `WorldLifecycleRuntimeCoordinator`:
    - consome `SceneTransitionScenesReadyEvent`,
    - decide executar reset ou SKIP,
    - emite `WorldLifecycleResetCompletedEvent(contextSignature, reason)`.
- Regra atual (para estabilizar startup/menu):
    - SKIP quando `profile == 'startup'` **ou** `activeScene == 'MenuScene'`.

### Navigation (produção)
- `IGameNavigationService` encapsula pedidos **Menu ↔ Gameplay**.
- `GameNavigationService`:
    - constrói `SceneTransitionRequest` com profile `startup`/`gameplay`;
    - executa `SceneTransitionService.TransitionAsync(...)`;
    - **não** chama `GameLoop.RequestStart()` (responsabilidade exclusiva do `GameLoopSceneFlowCoordinator`).

### InputMode / PauseOverlay
- `InputModeService` alterna os action maps (`FrontendMenu`, `Gameplay`, `PauseOverlay`).
- `InputModeSceneFlowBridge` aplica modo com base em `SceneTransitionCompletedEvent`:
    - `profile=gameplay` → `Gameplay`
    - `profile=startup/frontend` → `FrontendMenu`
- `PauseOverlayController` publica:
    - `GamePauseCommandEvent` (Show)
    - `GameResumeRequestedEvent` (Hide/Resume)
    - `GameExitToMenuRequestedEvent` (ReturnToMenuFrontend)
  e alterna `InputMode` para `PauseOverlay`/`Gameplay`/`FrontendMenu`, além de chamar
  `IGameNavigationService.RequestToMenu(...)` no retorno ao menu.
- `GamePauseGateBridge` traduz pause/resume/exit para token `SimulationGateTokens.Pause`.

## Logging (diagnóstico)
- Preferir logs com tags:
    - `[SceneFlow]`, `[Fade]`, `[WorldLifecycle]`, `[Readiness]`, `[GameLoop]`
- Para investigação de profile:
    - logar nome + path resolvido + type do asset.
- Evitar logs “crípticos”: sempre incluir `contextSignature` ou `profileName` em eventos relevantes.

## Pontos de atenção
- `WorldLifecycleController` em bootstrap pode existir com `AutoInitializeOnStart` desabilitado.
    - Isso evita executar reset/spawn em cenas que ainda não são o alvo de integração (ex.: evitar contaminar testes do Fade).
- `LoadingHudScene` é carregada de forma aditiva pelo `NewScriptsLoadingHudService`; o HUD (`NewScriptsLoadingHudController`) deve ser independente de lógica de gameplay.


## GameLoop

### Correlação de contexto: onde ela existe e onde NÃO deve existir

- A **correlação por assinatura/contexto** é um requisito do pipeline **Scene Flow + World Lifecycle**:
    - `SceneTransition*Event` carrega `SceneTransitionContext`.
    - `WorldLifecycleResetCompletedEvent` carrega `ContextSignature` (e `Reason`).
- Os eventos do **GameLoop** (ex.: start/pause/resume/reset) são **intencionamente “context-free”**:
    - Start é um **REQUEST** (intenção) observado pelo `GameLoopSceneFlowCoordinator`.
    - O “COMMAND” de start é a **chamada** `IGameLoopService.RequestStart()` executada somente quando o runtime está **ready**
      (TransitionCompleted + WorldLifecycleResetCompleted/SKIP).
- Regra de engenharia: **não use eventos do GameLoop para correlacionar transições.** A correlação é feita no Coordinator por:
    - filtro de `TransitionProfileName` quando aplicável;
    - `expectedContextSignature` capturada no `SceneTransitionStartedEvent`;
    - comparação com a assinatura recebida nos eventos posteriores.

#### Invariantes (concorrência e determinismo)
- **Apenas 1 transição em voo é suportada.** O runtime assume que `ISceneTransitionService.TransitionAsync(...)` não roda concorrente.
- A infraestrutura de Readiness/Gate durante SceneFlow existe justamente para **serializar** o fluxo de transição (token de gate, snapshots e “ready”).
- Consequência: eventos do GameLoop **não precisam** carregar `contextSignature`, desde que:
    - o Start seja coordenado via `GameLoopSceneFlowCoordinator`, e
    - o “ready” seja derivado dos eventos do SceneFlow + WorldLifecycle.

#### Eventos do GameLoop (context-free por design)
- Eventos como `GameStartEvent` são **REQUEST** (intenção), propositalmente **sem contexto**.
- A correlação por assinatura ocorre somente em:
    - `SceneTransition*Event` (carrega `SceneTransitionContext`), e
    - `WorldLifecycleResetCompletedEvent` (`ContextSignature`).
- O `GameLoopSceneFlowCoordinator` é o único responsável por:
    - capturar `expectedContextSignature` do primeiro `SceneTransitionStartedEvent`,
    - aguardar `SceneTransitionCompletedEvent` + `WorldLifecycleResetCompletedEvent`,
    - e então executar o **COMMAND** chamando `IGameLoopService.RequestStart()`.

#### `CanPerform` (não é gate-aware)
- `CanPerform(...)` no GameLoop é um **helper de estado macro** (capability map).
- **Não é** uma autorização final de gameplay. Não consulta gate/readiness por design.
- A autorização final para execução de ações deve ser feita via `IStateDependentService` (gate-aware).


> Caso de borda (multi-transição concorrente): se o projeto vier a permitir duas transições simultâneas, a mitigação correta é
> introduzir correlação **no domínio de Scene Flow** (por contexto/assinatura) e/ou criar um `GameStartCommandEvent` opcional que
> carregue `ContextSignature` para consumo do bridge. Não é requisito do runtime atual.

### `CanPerform` não é autorização de gameplay (gate-aware)

- `GameLoopStateMachine.CanPerform(...)` é apenas um **mapa de capacidade por estado macro** (Boot/Menu/Playing/Paused).
- O bloqueio determinístico de ações por **gate/readiness** é responsabilidade de `IStateDependentService` (gate-aware),
  que combina:
    - estado do GameLoop (Playing vs NotPlaying),
    - `GameReadinessService` (gameplayReady),
    - `ISimulationGateService` (tokens ativos),
    - e pausa.
- Regra de engenharia: **input/gameplay deve consultar `IStateDependentService`**, não `CanPerform` diretamente.

## Evidências (log)
- `GlobalBootstrap` registra `ISceneTransitionService`, `INewScriptsFadeService`, `IGameNavigationService`,
  `GameLoop`, `InputModeService`, `GameReadinessService`, `WorldLifecycleRuntimeCoordinator`,
  `SceneFlowLoadingService`.
- `MenuPlayButtonBinder` desativa botão e dispara `RequestToGameplay`.
- `SceneTransitionService` executa `Started → FadeIn → Load/Unload → ScenesReady → gate → FadeOut → Completed`.
- `WorldLifecycleRuntimeCoordinator`:
    - Startup: SKIP com `WorldLifecycleResetCompletedEvent(reason=Skipped_StartupOrFrontend)`
    - Gameplay: reset executado antes do gate liberar.
- `PauseOverlay` publica `GamePauseCommandEvent`, `GameResumeRequestedEvent`,
  `GameExitToMenuRequestedEvent` e o gate mostra `state.pause` (confirmado em logs). O token
  `flow.scene_transition` está **implementado**, mas ainda sem evidência dedicada em report.

## Documentos relacionados
- [Reports/GameLoop.md](Reports/GameLoop.md)
- [Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md](Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md)
- [Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md](Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md)
