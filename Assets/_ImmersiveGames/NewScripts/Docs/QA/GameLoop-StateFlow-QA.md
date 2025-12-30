# QA – GameLoop State Flow (NewScripts)

Este QA valida o fluxo mínimo de produção:

**Startup → Menu → Gameplay → Pause → Resume**

e confirma que `GameLoop`, `SceneFlow`, `WorldLifecycle`, `SimulationGate` e `InputMode` permanecem sincronizados.

---

## Pré-requisitos

- Projeto rodando em **NEWSCRIPTS_MODE**.
- Cena inicial: `NewBootstrap` (ou a cena de bootstrap equivalente do NewScripts).
- `GlobalBootstrap` ativo (registra SceneFlow, Fade, LoadingHUD, WorldLifecycle runtime driver, GameLoop e Navigation).

---

## 1) Startup → Menu (profile `startup`)

### Ação
1. Dar Play no Editor.

### Esperado (logs)
1. `GameLoopSceneFlowCoordinator` registrado com startPlan `startup` (MenuScene + UIGlobalScene).
2. `GameStartRequestedEvent` disparado (bootstrap de produção).
3. Scene Flow:
    - `SceneTransitionService` inicia transição com profile `startup`
    - LoadingHUD: `Started → Ensure + Show`
    - Fade: `FadeIn` (alpha=1)
    - Carrega `MenuScene` e `UIGlobalScene`, descarrega `NewBootstrap`
4. `WorldLifecycleRuntimeCoordinator` recebe `SceneTransitionScenesReadyEvent` e executa **SKIP** (startup/frontend),
   emitindo `WorldLifecycleResetCompletedEvent` com reason `Skipped_StartupOrFrontend...`.
5. Scene Flow:
    - aguarda completion gate (deve concluir imediatamente via evento acima)
    - LoadingHUD: `BeforeFadeOut → Hide`
    - Fade: `FadeOut` (alpha=0)
    - `SceneFlow` conclui.
6. `GameLoop` avança para **Ready**.

> Nota: warnings de “Chamada repetida no frame” podem aparecer durante bootstrap. Eles não indicam reexecução funcional do bootstrap.

---

## 2) Menu → Gameplay (profile `gameplay`)

### Ação
1. No `MenuScene`, clicar no botão **Play**.

### Esperado (logs)
1. `GameNavigationService` dispara `TransitionAsync` com targetActive `GameplayScene` e profile `gameplay`.
2. Scene Flow:
    - LoadingHUD: `Started → Show`
    - FadeIn (alpha=1)
    - Carrega `GameplayScene` (Additive), mantém `UIGlobalScene`, descarrega `MenuScene`
3. `NewSceneBootstrapper` de `GameplayScene`:
    - cria scene scope
    - carrega `WorldDefinition`
    - registra spawn services (mínimo esperado: `PlayerSpawnService` e `EaterSpawnService`)
4. `WorldLifecycleRuntimeCoordinator` recebe `SceneTransitionScenesReadyEvent` e dispara hard reset:
    - `WorldLifecycleController` → reset
    - `WorldLifecycleOrchestrator` executa fases (despawn/spawn)
    - `ActorRegistry` termina com 2 atores (baseline: Player + Eater)
5. `WorldLifecycleResetCompletedEvent` é emitido com reason `ScenesReady/GameplayScene`.
6. Scene Flow:
    - aguarda completion gate (conclui via evento acima)
    - LoadingHUD: `BeforeFadeOut → Hide`
    - FadeOut (alpha=0)
    - `SceneFlow` conclui.
7. `GameLoop` avança para **Playing**.
8. `IStateDependentService` libera `Move` (e outras ações dependentes) quando:
    - gate aberto
    - gameplayReady=true
    - gameLoopState=Playing

---

## 3) Pause → Resume

### Ação
1. Abrir o Pause Overlay (UI/tecla conforme projeto).
2. Fechar o Pause Overlay.

### Esperado (logs)
1. Ao abrir pause:
    - `GamePauseGateBridge` adquire token `state.pause`
    - `GameLoop` entra em **Paused**
    - `InputModeService` alterna para mapa/UI (`PauseOverlay`)
    - `IStateDependentService` bloqueia `Move` com motivo `Paused`
2. Ao fechar pause:
    - `GamePauseGateBridge` libera token `state.pause`
    - `GameLoop` retorna para **Playing**
    - `InputModeService` volta para `Gameplay`
    - `IStateDependentService` libera `Move`

---

## Resultado

O QA passa se:

- Startup termina em `GameLoop=Ready` (menu frontend).
- Entrar em gameplay executa reset determinístico e termina em `GameLoop=Playing`.
- Pause/Resume bloqueia e libera ações conforme esperado.
