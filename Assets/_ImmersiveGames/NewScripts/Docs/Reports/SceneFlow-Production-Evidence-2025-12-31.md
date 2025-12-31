# Evidência — SceneFlow/Navigation end-to-end (produção)

Data: 2025-12-31  
Escopo: NewScripts (Unity) — fluxo de produção **NewBootstrap → Menu → Gameplay → Pause/Resume → Restart → ExitToMenu → Menu**.

> Esta evidência é um recorte do log fornecido no chat, usado para demonstrar que a **Etapa 3** do plano (fluxo completo e blockers iniciais da GameplayScene) está operacional.

## Checklist de evidência (marcos)

### 1) Infra global sobe e registra serviços chave
- `✅ NewScripts global infrastructure initialized`
- `ISceneTransitionService` / `IGameNavigationService` / `INewScriptsFadeService` / `INewScriptsLoadingHudService`
- Bridges: Pause, Restart, ExitToMenu, InputMode

**Excertos**
```text
[GlobalBootstrap] ✅ NewScripts global infrastructure initialized (Commit 1 minimal).
[GlobalBootstrap] [SceneFlow] SceneTransitionService nativo registrado ...
[GlobalBootstrap] [Navigation] GameNavigationService registrado no DI global.
[GlobalBootstrap] [Loading] INewScriptsLoadingHudService registrado no DI global.
```

### 2) Startup: transição para Menu com Skip de Reset (frontend)
- `SceneTransitionContext(... Profile='startup')`
- `WorldLifecycle Reset SKIPPED (startup/frontend)` + `WorldLifecycleResetCompletedEvent`
- `SceneFlow` completa (Fade + LoadingHUD + gate)

**Excertos**
```text
[SceneTransitionService] Iniciando transição: ... Active='MenuScene' ... Profile='startup'
[WorldLifecycleRuntimeCoordinator] Reset SKIPPED (startup/frontend) ...
[WorldLifecycleRuntimeCoordinator] Emitting WorldLifecycleResetCompletedEvent. profile='startup' ...
[SceneTransitionService] Transição concluída com sucesso.
```

### 3) Menu → Gameplay via Play (produção)
- `MenuPlayButtonBinder` desabilita botão, chama `IGameNavigationService.RequestGameplayAsync`
- `NavigateAsync -> routeId='to_gameplay' ... Profile='gameplay'`
- Fade/LoadingHUD aplicados

**Excertos**
```text
[MenuPlayButtonBinder] Button interactable=OFF (reason='RequestGameplayAsync').
[GameNavigationService] NavigateAsync -> routeId='to_gameplay' ... Profile='gameplay'
[SceneTransitionService] Iniciando transição: ... TargetActive='GameplayScene' ... Profile='gameplay'
```

### 4) Gameplay: Reset hard após ScenesReady + spawn funcional
- `WorldLifecycleRuntimeCoordinator` dispara hard reset após `SceneTransitionScenesReady`
- `WorldLifecycleOrchestrator` executa fases e libera gate
- Spawn services registrados e spawns efetuados (Player + Eater)

**Excertos**
```text
[WorldLifecycleRuntimeCoordinator] Disparando hard reset após ScenesReady. reason='ScenesReady/GameplayScene'
[WorldLifecycleOrchestrator] World Reset Started
[WorldLifecycleOrchestrator] Spawn service completed: PlayerSpawnService
[WorldLifecycleOrchestrator] Spawn service completed: EaterSpawnService
[WorldLifecycleOrchestrator] World Reset Completed
```

### 5) GameLoop entra em Playing e StateDependent libera ações
- `GameLoop ENTER: Playing (active=True)`
- `StateDependent ... Action 'Move' liberada`

**Excertos**
```text
[GameLoopService] ENTER: Playing (active=True)
[NewScriptsStateDependentService] Action 'Move' liberada ...
```

### 6) Pause/Resume, Restart e ExitToMenu operacionais
- Pause: `GamePauseGateBridge` adquire token + GameLoop vai para Paused
- Resume: token liberado + GameLoop volta para Playing
- Restart: `RestartNavigationBridge` -> `to_gameplay` + reset executado (com despawn de atores anteriores)
- ExitToMenu: `ExitToMenuNavigationBridge` -> `to_menu` + Skip reset (frontend) + Menu reabilita proteção contra auto-click

**Excertos**
```text
[GamePauseGateBridge] Acquire token='state.pause' ...
[GameLoopService] ENTER: Paused (active=False)
[GamePauseGateBridge] Release token='state.pause' ...
[RestartNavigationBridge] GameResetRequestedEvent recebido -> RequestGameplayAsync.
[WorldLifecycleOrchestrator] Actor removed ... (despawn) ...
[ExitToMenuNavigationBridge] ExitToMenu recebido -> RequestMenuAsync.
[WorldLifecycleRuntimeCoordinator] Reset SKIPPED (startup/frontend). profile='frontend'
```

## Observações
- Os warnings de “Chamada repetida no frame X” do `DebugUtility` aparecem como ruído de diagnóstico e **não impedem** o fluxo.
- “Movement blocked by IStateDependentService” durante transição/reset é **esperado** (gate fechado / gameLoop não em Playing).

## Arquivos relacionados (implementação)
- UI: `MenuPlayButtonBinder` (OnClick via Inspector)
- Navigation: `IGameNavigationService` / `GameNavigationService` + bridges (`RestartNavigationBridge`, `ExitToMenuNavigationBridge`)
- SceneFlow: `ISceneTransitionService` + adapters (Fade/LoadingHUD) + `WorldLifecycleResetCompletionGate`
- WorldLifecycle: `WorldLifecycleRuntimeCoordinator`, `WorldLifecycleController`, `WorldLifecycleOrchestrator`
- State/Gate: `ISimulationGateService`, `GameReadinessService`, `IStateDependentService`
