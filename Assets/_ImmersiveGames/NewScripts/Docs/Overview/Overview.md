# Overview — NewScripts (Arquitetura + WorldLifecycle)

Este documento consolida a visão geral do módulo **NewScripts** em um único arquivo para reduzir fragmentação.

## Links canônicos

- Contrato de observabilidade: `../Standards/Standards.md#observability-contract`
- Política Strict/Release: `../Standards/Standards.md#politica-strict-vs-release`
- Evidência vigente: `../Reports/Evidence/LATEST.md` (log bruto mais recente: `../Reports/lastlog.log`)

---

## Arquitetura

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
    - `IFadeService` (Fade)
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

> Referências: ADR-0016 (ContentSwap InPlace-only).

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
- `IFadeService` controla `FadeScene` (Additive) e o `FadeController` (CanvasGroup).
- `ILoadingHudService` controla o `LoadingHudScene` (Additive).
- `SceneTransitionProfile` define parâmetros (durations/curves) por **profileName**.
- Resolução por Resources:
    - `Resources/SceneFlow/Profiles/<profileName>`

### 7) World Lifecycle
#### Reset por escopos vs Reset de gameplay

- **WorldLifecycle (infra)**: reset determinístico do mundo (gate + hooks + spawn/despawn) e **soft reset por escopos** via `ResetScope` e `IResetScopeParticipant`.
- **RunRearm (gameplay)**: módulo em `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/RunRearm/Runtime/` que define **alvos** (`RunRearmTarget`) e **etapas** (`RunRearmStep`) para resets de componentes (`IRunRearmable`). Pode ser acionado via Dev (direto) ou via bridges (ex.: `PlayersRunRearmWorldParticipant`).

- Executa reset determinístico por escopos e etapas (despawn/spawn/hook).
- Integrado ao Scene Flow via `WorldLifecycleSceneFlowResetDriver`:
    - reage ao `SceneTransitionScenesReadyEvent`,
    - executa reset (ou SKIP em Menu/Startup),
    - emite `WorldLifecycleResetCompletedEvent` para destravar o Coordinator.

Detalhes em: [Overview/Overview.md](Overview/Overview.md).

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
    - Em `startup/frontend`, SKIP (profile != gameplay) e emite `WorldLifecycleResetCompletedEvent` com reason `Skipped_StartupOrFrontend:profile=<...>;scene=<...>`.
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
- [Observability-Contract.md](../Standards/Standards.md#observability-contract)

### Baseline 2.0 (status fechado)
- **Status:** FECHADO / OPERACIONAL (2026-01-05).
- **ADR de fechamento:** [ADR-0015-Baseline-2.0-Fechamento](ADRs/ADR-0015-Baseline-2.0-Fechamento.md).
- Contrato do pipeline: **SceneFlow → ScenesReady → WorldLifecycleResetCompleted → Gate → FadeOut/Completed**.
- Spec/checklist antigos foram removidos; a verificação vigente usa contrato de observabilidade + evidência datada.

## Documentos relacionados
- [Overview/Overview.md](Overview/Overview.md)
- [ADR-0009-FadeSceneFlow.md](ADRs/ADR-0009-FadeSceneFlow.md)
- [ADR-0010-LoadingHud-SceneFlow.md](ADRs/ADR-0010-LoadingHud-SceneFlow.md)
- [ADR-0013-Ciclo-de-Vida-Jogo.md](ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md)
- [ADR-0014-GameplayReset-Targets-Grupos.md](ADRs/ADR-0014-GameplayReset-Targets-Grupos.md)
- [CHANGELOG.md](../CHANGELOG.md)
- [Reports/GameLoop.md](Reports/GameLoop.md)
- [Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md](Reports/WORLDLIFECYCLE_RESET_ANALYSIS.md)
- [Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md](Reports/WORLDLIFECYCLE_SPAWN_ANALYSIS.md)

> Nota: no GameLoop, o “COMMAND” de start não é um evento separado; ele é a chamada `GameLoop.RequestStart()` feita pelo Coordinator somente quando o runtime está “ready” (TransitionCompleted + WorldLifecycleResetCompleted/SKIP).

## Evidências (log)
- **Última evidência (log bruto):** `../Reports/lastlog.log`
- `GlobalCompositionRoot` registra `ISceneTransitionService`, `IFadeService`, `IGameNavigationService`,
  `GameLoop`, `InputModeService`, `GameReadinessService`, `WorldLifecycleSceneFlowResetDriver`.
- Startup profile `startup` com reset **SKIPPED** e emissão de
  `WorldLifecycleResetCompletedEvent(reason=SceneFlow/ScenesReady)` (com log de *ignored* em profile não-gameplay).
- `MenuPlayButtonBinder` desativa botão e dispara `RequestGameplayAsync`.
- Transição para `gameplay` executa reset e `PlayerSpawnService` spawna `Player_NewScripts`.
- Completion gate aguarda `WorldLifecycleResetCompletedEvent` antes do `FadeOut`.
- `PauseOverlay` publica `GamePauseCommandEvent`, `GameResumeRequestedEvent`, `GameExitToMenuRequestedEvent`
  e tokens `state.pause` / `flow.scene_transition` aparecem no gate.

---

## WorldLifecycle

# WorldLifecycle (NewScripts)

## Objetivo

O WorldLifecycle define o **reset determinístico** do mundo (spawn/despawn/hooks), alinhado ao **SceneFlow** e ao **GameLoop**, com foco em:

- previsibilidade (reset canônico)
- observabilidade (logs como contrato)
- gating (SimulationGate)
- extensibilidade (hooks por fase/ator)

> **Fonte de verdade de observabilidade**:
> veja **[Standards/Standards.md#observability-contract](../Standards/Standards.md#observability-contract)**.

---

## Resumo do pipeline

### 1) SceneFlow (transição de cenas)

Ordem canônica:

1. `SceneTransitionStarted`
2. `SceneTransitionScenesReady`
3. `SceneTransitionCompleted`

Durante a transição:

- token adquirido: `flow.scene_transition`
- Loading HUD:
    - inicia com “ensure only”
    - `Show` após `FadeIn`
    - `Hide` antes de `FadeOut`

### 2) WorldLifecycle (reset)

Quando `ScenesReady` chega:

- se profile for `gameplay`:
    - `ResetRequested` (OBS)
    - reset executa pipeline de despawn/spawn/hooks
    - emite `WorldLifecycleResetCompletedEvent`
- se profile for `startup/frontend`:
    - reset é **skipped**
    - ainda assim emite `WorldLifecycleResetCompletedEvent` (invariante)

### 3) GameLoop (estado)

Estados relevantes:

- `Ready`
- `IntroStage` (opcional)
- `Playing`
- `PostGame` (quando existente)

### 4) IntroStage (opcional, pós-reveal)

Quando presente:

- ocorre após `SceneFlow/Completed`
- bloqueia gameplay via token `sim.gameplay`
- aguarda confirmação UI ou QA
- libera gameplay e transita para `Playing`

> IntroStage **não participa do completion gate do SceneFlow**.

---

## Invariantes globais (Baseline 2.0)

> Ver contrato completo em **[Standards/Standards.md#observability-contract](../Standards/Standards.md#observability-contract)**.

- `ScenesReady` acontece antes de `Completed`.
- `WorldLifecycleResetCompletedEvent` é sempre emitido (reset/skip/fail).
- Completion gate do SceneFlow aguarda `ResetCompleted` antes do `FadeOut`.
- Loading HUD só aparece após FadeIn e some antes do FadeOut.

---

## Observabilidade (strings canônicas)

Este doc não lista todas as strings de reason para evitar divergência.

**Contrato canônico**:
- [Standards/Standards.md#observability-contract](../Standards/Standards.md#observability-contract)

---

## Validações (evidência por log)

### Item 7 — PASS (Reset em Gameplay)

#### Evidência

**Reset via Hotkey (Gameplay/HotkeyR)**:

- `ResetRequested` reason=`ProductionTrigger/Gameplay/HotkeyR`
- reset executado na `GameplayScene`
- `WorldLifecycleResetCompletedEvent` emitido

Trechos relevantes:

- `[OBS][ContentSwap] ResetRequested ... reason='ProductionTrigger/Gameplay/HotkeyR' target='GameplayScene'`
- `[WorldLifecycleController] Reset iniciado. reason='ProductionTrigger/Gameplay/HotkeyR', scene='GameplayScene'.`
- `[WorldLifecycleOrchestrator] World Reset Completed`
- `Emitting WorldLifecycleResetCompletedEvent ... reason='ProductionTrigger/Gameplay/HotkeyR'`

**Reset via QA (qa_marco0_reset)**:

- `ResetRequested` reason=`ProductionTrigger/qa_marco0_reset`
- reset executado na `GameplayScene`
- `WorldLifecycleResetCompletedEvent` emitido

Trechos relevantes:

- `[OBS][ContentSwap] ResetRequested ... reason='ProductionTrigger/qa_marco0_reset' target='GameplayScene'`
- `[WorldLifecycleController] Reset iniciado. reason='ProductionTrigger/qa_marco0_reset', scene='GameplayScene'.`
- `Emitting WorldLifecycleResetCompletedEvent ... reason='ProductionTrigger/qa_marco0_reset'`

#### Observações

- Reset em `MenuScene` falha com `Failed_NoController:MenuScene` quando chamado fora da gameplay (esperado, não há controller no menu).

---

## Referências

- [ADR-0013 — Ciclo de Vida do Jogo](ADRs/ADR-0013-Ciclo-de-Vida-Jogo.md)
- [ADR-0016 — ContentSwap no WorldLifecycle](ADRs/ADR-0016-ContentSwap-WorldLifecycle.md)
- [ADR-0016 — ContentSwap InPlace-only](ADRs/ADR-0016-ContentSwap-WorldLifecycle.md)
- [ADR-0010 — Loading HUD + SceneFlow](ADRs/ADR-0010-LoadingHud-SceneFlow.md)
- [ADR-0009 — Fade + SceneFlow](ADRs/ADR-0009-FadeSceneFlow.md)
- [Standards/Standards.md#observability-contract](../Standards/Standards.md#observability-contract)
