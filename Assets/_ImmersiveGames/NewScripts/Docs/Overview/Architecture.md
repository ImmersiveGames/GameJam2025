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

### 3) ContentSwap vs LevelManager

- **ContentSwap** é o executor técnico de troca de conteúdo em runtime:
  - troca **in-place** (sem SceneFlow).
  - usa os contratos `IContentSwapChangeService`, `ContentSwapPlan`, `ContentSwapOptions` e `IContentSwapContextService`.
  - representa a troca de conteúdo como **troca de conteúdo**, não como progresso de nível.
- **LevelManager** é o orquestrador de **progressão**:
  - decide quando avançar/retroceder de nível,
  - delega a troca de conteúdo para o ContentSwap,
  - **sempre dispara IntroStage** ao entrar em um nível (política deste ciclo).

> Referências: ADR-0016 (ContentSwap InPlace-only) e ADR-0018 (mudança semântica para ContentSwap + LevelManager).

### 4) Navigation (produção)
- `IGameNavigationService` é a entrada de produção para ir **Menu ↔ Gameplay**.
- `GameNavigationService` encapsula `SceneTransitionRequest` e executa `SceneTransitionService.TransitionAsync(...)`.
- `GameNavigationService` **não** chama `GameLoop.RequestStart()`; o start é responsabilidade exclusiva do `GameLoopSceneFlowCoordinator`.

### 5) Scene Flow (SceneTransitionService)
Orquestra a transição de cenas (load/unload/active) com:
- evento `SceneTransitionStarted`
- evento `SceneTransitionScenesReady`
- evento `SceneTransitionCompleted`

Durante a transição:
- `GameReadinessService` adquire o gate (`flow.scene_transition`) para bloquear gameplay.
- O Fade e o Loading HUD (NewScripts) são executados antes/depois do carregamento.

### 6) Fade + Loading HUD ([ADR-0009](ADRs/ADR-0009-FadeSceneFlow.md), [ADR-0010](ADRs/ADR-0010-LoadingHud-SceneFlow.md))
- `INewScriptsFadeService` controla `FadeScene` (Additive) e o `NewScriptsFadeController` (CanvasGroup).
- `INewScriptsLoadingHudService` controla o `LoadingHudScene` (Additive).
- `NewScriptsSceneTransitionProfile` define parâmetros (durations/curves) por **profileName**.
- Resolução por Resources:
    - `Resources/SceneFlow/Profiles/<profileName>`

### 7) World Lifecycle
#### Reset por escopos vs Reset de gameplay

- **WorldLifecycle (infra)**: reset determinístico do mundo (gate + hooks + spawn/despawn) e **soft reset por escopos** via `ResetScope` e `IResetScopeParticipant`.
- **Gameplay Reset (gameplay)**: módulo em `Gameplay/Reset/` que define **alvos** (`GameplayResetTarget`) e **etapas** (`GameplayResetStep`) para resets de componentes (`IGameplayResettable`). Pode ser acionado via QA (direto) ou via bridges (ex.: `PlayersResetParticipant`).

- Executa reset determinístico por escopos e etapas (despawn/spawn/hook).
- Integrado ao Scene Flow via `WorldLifecycleSceneFlowResetDriver`:
    - reage ao `SceneTransitionScenesReadyEvent`,
    - executa reset (ou SKIP em Menu/Startup),
    - emite `WorldLifecycleResetCompletedEvent` para destravar o Coordinator.

Detalhes em: [Overview/WorldLifecycle.md](Overview/WorldLifecycle.md).

## Fluxo de transição (canônico)
**Resumo:** `SceneTransitionStarted` → `SceneTransitionScenesReady` → (reset/skip) → `SceneTransitionCompleted`.

**Ordem observada (UseFade=true):**
1. `SceneTransitionStarted` → gate `flow.scene_transition` **fecha**.
2. `FadeIn`.
3. `LoadingHUD.Show`.
4. Load/Unload/Active.
5. `SceneTransitionScenesReady`.
6. `WorldLifecycleSceneFlowResetDriver` executa **reset** (gameplay) ou **SKIP** (startup/frontend).
7. `WorldLifecycleResetCompletedEvent` libera o completion gate.
8. `LoadingHUD.Hide`.
9. `FadeOut`.
10. `SceneTransitionCompleted` → gate `flow.scene_transition` **abre**.

**Fallback (UseFade=false):** `LoadingHUD.Show` pode ocorrer no `Started`, com `Hide` antes do `FadeOut` (safety hide no `Completed`).

## Gate / Readiness (tokens)
- `flow.scene_transition`: durante transições de cena (Readiness/Gate).
- `WorldLifecycle.WorldReset`: durante hard reset do mundo.
- `state.pause`: durante pausa/overlay.
- `state.postgame`: durante overlay de pós-game.

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
7. WorldLifecycleSceneFlowResetDriver: SKIP (startup/menu) e emite `WorldLifecycleResetCompletedEvent`
8. Completion gate libera → FadeOut → `SceneTransitionCompletedEvent`
9. Gate reabre → Coordinator chama `GameLoop.RequestStart()`

## Fluxo de produção (Menu → Gameplay → Pause → Resume → ExitToMenu → Menu)
1. **Menu → Gameplay**
    - UI chama `IGameNavigationService.RequestGameplayAsync(reason)`.
    - `GameNavigationService` dispara `SceneTransitionService.TransitionAsync(profile=gameplay)`.
2. **Scene Flow**
    - `SceneTransitionStarted` → `FadeIn` → `LoadingHUD.Show` → Load/Unload/Active → `SceneTransitionScenesReady`.
    - Completion gate aguarda `WorldLifecycleResetCompletedEvent`.
    - `LoadingHUD.Hide` → `FadeOut` → `SceneTransitionCompleted`.
3. **WorldLifecycle**
    - Em `gameplay`, executa reset após `ScenesReady` e emite `WorldLifecycleResetCompletedEvent(signature, reason)`.
    - Em `startup/frontend`, SKIP (profile != gameplay) e emite `WorldLifecycleResetCompletedEvent` com reason `SceneFlow/ScenesReady` (log explicita "ScenesReady ignorado").
4. **Pause / Resume / ExitToMenu**
    - `PauseOverlayController.Show()` publica `GamePauseCommandEvent` e alterna `InputMode` para `PauseOverlay`.
    - `PauseOverlayController.Hide()` publica `GameResumeRequestedEvent` e volta para `Gameplay`.
    - `PauseOverlayController.ReturnToMenuFrontend()` publica `GameExitToMenuRequestedEvent`,
      troca `InputMode` para `FrontendMenu` e chama `IGameNavigationService.RequestToMenuAsync(...)`.
5. **GameLoop**
    - `GameLoopSceneFlowCoordinator` chama `GameLoop.RequestStart()` apenas após `TransitionCompleted + ResetCompleted`.

## Baseline 2.0 (contrato)
A fonte vigente do Baseline 2.0 é o contrato de observabilidade + evidência datada:
- [ADR-0015 — Baseline 2.0: Fechamento Operacional](ADRs/ADR-0015-Baseline-2.0-Fechamento.md)
- [Evidence/LATEST.md](Reports/Evidence/LATEST.md)
- [Observability-Contract.md](Standards/Observability-Contract.md)

### Baseline 2.0 (status fechado)
- **Status:** FECHADO / OPERACIONAL (2026-01-05).
- **ADR de fechamento:** [ADR-0015-Baseline-2.0-Fechamento](ADRs/ADR-0015-Baseline-2.0-Fechamento.md).
- Contrato do pipeline: **SceneFlow → ScenesReady → WorldLifecycleResetCompleted → Gate → FadeOut/Completed**.
- Spec/checklist antigos foram removidos; a verificação vigente usa contrato de observabilidade + evidência datada.

## Documentos relacionados
- [Overview/WorldLifecycle.md](Overview/WorldLifecycle.md)
- [ADR-0009-FadeSceneFlow.md](ADRs/ADR-0009-FadeSceneFlow.md)
- [ADR-0010-LoadingHud-SceneFlow.md](ADRs/ADR-0010-LoadingHud-SceneFlow.md)
- [ADR-0013-Ciclo-de-Vida-Jogo.md](ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md)
- [ADR-0014-GameplayReset-Targets-Grupos.md](ADRs/ADR-0014-GameplayReset-Targets-Grupos.md)
- [CHANGELOG-docs.md](CHANGELOG-docs.md)
- [Reports/GameLoop.md](Reports/GameLoop.md)
- [Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md](Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md)
- [Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md](Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md)

> Nota: no GameLoop, o “COMMAND” de start não é um evento separado; ele é a chamada `GameLoop.RequestStart()` feita pelo Coordinator somente quando o runtime está “ready” (TransitionCompleted + WorldLifecycleResetCompleted/SKIP).

## Evidências (log)
- `GlobalBootstrap` registra `ISceneTransitionService`, `INewScriptsFadeService`, `IGameNavigationService`,
  `GameLoop`, `InputModeService`, `GameReadinessService`, `WorldLifecycleSceneFlowResetDriver`.
- Startup profile `startup` com reset **SKIPPED** e emissão de
  `WorldLifecycleResetCompletedEvent(reason=SceneFlow/ScenesReady)` (com log de *ignored* em profile não-gameplay).
- `MenuPlayButtonBinder` desativa botão e dispara `RequestGameplayAsync`.
- Transição para `gameplay` executa reset e `PlayerSpawnService` spawna `Player_NewScripts`.
- Completion gate aguarda `WorldLifecycleResetCompletedEvent` antes do `FadeOut`.
- `PauseOverlay` publica `GamePauseCommandEvent`, `GameResumeRequestedEvent`, `GameExitToMenuRequestedEvent`
  e tokens `state.pause` / `flow.scene_transition` aparecem no gate.
