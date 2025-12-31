# Arquitetura — NewScripts (visão geral)

## Objetivo
O NewScripts busca um pipeline previsível e testável para:
- inicialização global (infra),
- transições de cena (Scene Flow),
- controle de prontidão do jogo (Readiness/Gate),
- e reset determinístico do mundo (World Lifecycle) por escopos.

## Componentes principais

### 1) Global Bootstrap (infra)
- Configura logging (níveis e formatação).
- Inicializa DI global (`DependencyManager` / `GlobalServiceRegistry`).
- Registra serviços globais essenciais:
    - `ISimulationGateService`
    - `IGameLoopService` (+ runtime driver para tick)
    - `ISceneTransitionService` (SceneFlow)
    - `INewScriptsFadeService` (Fade)
    - `IStateDependentService` (bloqueio de ações por estado/prontidão)

### 2) GameLoop
Responsável por um estado global macro (ex.: Boot → Menu (Ready/Idle) → Playing) e por aceitar comandos (via bridge de input) que resultam em:
- pedidos de start,
- pedidos de navegação/transição (quando a camada de navigation estiver ativa),
- e controle de “atividade” (isActive).

### 3) Navigation (produção)
- `IGameNavigationService` é a entrada de produção para ir **Menu ↔ Gameplay**.
- `GameNavigationService` encapsula `SceneTransitionRequest` e executa `SceneTransitionService.TransitionAsync(...)`.
- `GameNavigationService` **não** chama `GameLoop.RequestStart()`; o start é responsabilidade exclusiva do `GameLoopSceneFlowCoordinator`.

### 4) Scene Flow (SceneTransitionService)
Orquestra a transição de cenas (load/unload/active) com:
- evento `SceneTransitionStarted`
- evento `SceneTransitionScenesReady`
- evento `SceneTransitionCompleted`

Durante a transição:
- `GameReadinessService` adquire o gate (`flow.scene_transition`) para bloquear gameplay.
- O Fade (NewScripts) pode ser executado antes e depois do carregamento.

### 5) Fade ([ADR-0009](ADRs/ADR-0009-FadeSceneFlow.md))
- `INewScriptsFadeService` controla `FadeScene` (Additive) e o `NewScriptsFadeController` (CanvasGroup).
- `NewScriptsSceneTransitionProfile` define parâmetros (durations/curves) por **profileName**.
- Resolução por Resources:
    - `Resources/SceneFlow/Profiles/<profileName>`

### 6) World Lifecycle
#### Reset por escopos vs Reset de gameplay

- **WorldLifecycle (infra)**: reset determinístico do mundo (gate + hooks + spawn/despawn) e **soft reset por escopos** via `ResetScope` e `IResetScopeParticipant`.
- **Gameplay Reset (gameplay)**: módulo em `Gameplay/Reset/` que define **alvos** (`GameplayResetTarget`) e **fases** (`GameplayResetPhase`) para resets de componentes (`IGameplayResettable`). Pode ser acionado via QA (direto) ou via bridges (ex.: `PlayersResetParticipant`).

- Executa reset determinístico por escopos e fases (despawn/spawn/hook).
- Integrado ao Scene Flow via `WorldLifecycleRuntimeCoordinator`:
    - reage ao `SceneTransitionScenesReadyEvent`,
    - executa reset (ou SKIP em Menu/Startup),
    - emite `WorldLifecycleResetCompletedEvent` para destravar o Coordinator.

Detalhes em: [WORLD_LIFECYCLE.md](WORLD_LIFECYCLE.md).

## Fluxo macro (startup/menu)
Diagrama simplificado:

1. Bootstrap global inicializa infra/serviços
2. GameLoop entra em `Menu`
3. Coordinator dispara `SceneTransitionService` com:
    - Load: `MenuScene`, `UIGlobalScene`
    - Unload: cena bootstrap
    - Profile: `startup`
4. Readiness fecha gate durante transição
5. Fade executa (FadeScene)
6. Scenes carregadas → `SceneTransitionScenesReadyEvent`
7. WorldLifecycleRuntimeCoordinator: SKIP (startup/menu) e emite `WorldLifecycleResetCompletedEvent`
8. Completion gate libera → FadeOut → `SceneTransitionCompletedEvent`
9. Gate reabre → Coordinator chama `GameLoop.RequestStart()`

## Fluxo de produção (Menu → Gameplay → Pause → Resume → ExitToMenu → Menu)
1. **Menu → Gameplay**
    - UI (ex.: `MenuPlayButtonBinder`) chama `IGameNavigationService.RequestGameplayAsync(reason)` (via `Button.onClick` no Inspector; sem registro de listeners em código).
    - `GameNavigationService` dispara `SceneTransitionService.TransitionAsync(profile=gameplay)`.
2. **Scene Flow**
    - `SceneTransitionStarted` → `FadeIn` (alpha=1 / hide) → `FadeInCompleted`.
    - Após `FadeInCompleted`: `LoadingHud.Show()`.
    - Load/Unload/Active → `SceneTransitionScenesReady`.
    - Completion gate aguarda `WorldLifecycleResetCompletedEvent` (ou SKIP no profile).
    - `BeforeFadeOut`: `LoadingHud.Hide()` → `FadeOut` (alpha=0 / reveal) → `SceneTransitionCompleted` (com safety `LoadingHud.Hide()` no final).
3. **WorldLifecycle**
    - Em `gameplay`, executa reset após `ScenesReady` e emite `WorldLifecycleResetCompletedEvent(signature, reason)`.
    - Em `startup/frontend`, SKIP com reason `Skipped_StartupOrFrontend`.
4. **Pause / Resume / ExitToMenu**
    - `PauseOverlayController.Show()` publica `GamePauseCommandEvent` e alterna `InputMode` para `PauseOverlay`.
    - `PauseOverlayController.Hide()` publica `GameResumeRequestedEvent` e volta para `Gameplay`.
    - `PauseOverlayController.ReturnToMenuFrontend()` publica `GameExitToMenuRequestedEvent`,
      troca `InputMode` para `FrontendMenu` e chama `IGameNavigationService.RequestMenuAsync(...)`.
5. **GameLoop**
    - `GameLoopSceneFlowCoordinator` chama `GameLoop.RequestStart()` apenas após `TransitionCompleted + ResetCompleted`.

## Documentos relacionados
- [WORLD_LIFECYCLE.md](WORLD_LIFECYCLE.md)
- [ADR-0009-FadeSceneFlow.md](ADRs/ADR-0009-FadeSceneFlow.md)
- [ARCHITECTURE_TECHNICAL.md](ARCHITECTURE_TECHNICAL.md)
- [DECISIONS.md](DECISIONS.md)
- [EXAMPLES_BEST_PRACTICES.md](EXAMPLES_BEST_PRACTICES.md)
- [GLOSSARY.md](GLOSSARY.md)
- [CHANGELOG-docs.md](CHANGELOG-docs.md)
- [Reports/GameLoop.md](Reports/GameLoop.md)
- [Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md](Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md)
- [Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md](Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md)

> Nota: no GameLoop, o “COMMAND” de start não é um evento separado; ele é a chamada `GameLoop.RequestStart()` feita pelo Coordinator somente quando o runtime está “ready” (TransitionCompleted + WorldLifecycleResetCompleted/SKIP).

## Evidências (log)
- `GlobalBootstrap` registra `ISceneTransitionService`, `INewScriptsFadeService`, `IGameNavigationService`,
  `GameLoop`, `InputModeService`, `GameReadinessService`, `WorldLifecycleRuntimeCoordinator`.
- Startup profile `startup` com reset **SKIPPED** e emissão de
  `WorldLifecycleResetCompletedEvent(reason=Skipped_StartupOrFrontend)`.
- `MenuPlayButtonBinder` desativa botão e dispara `RequestGameplayAsync`.
- Transição para `gameplay` executa reset e `PlayerSpawnService` spawna `Player_NewScripts`.
- Completion gate aguarda `WorldLifecycleResetCompletedEvent` antes do `FadeOut`.
- `PauseOverlay` publica `GamePauseCommandEvent`, `GameResumeRequestedEvent`, `GameExitToMenuRequestedEvent`
  e tokens `state.pause` / `flow.scene_transition` aparecem no gate.
