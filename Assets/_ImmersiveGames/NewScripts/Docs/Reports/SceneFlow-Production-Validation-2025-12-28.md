# SceneFlow — Validação de Fluxo de Produção (2025-12-28)

## Objetivo
Consolidar o fluxo SceneFlow para produção: garantir **um único ponto de start**, remover/condicionar gatilhos de debug e registrar evidências mínimas.

## Checklist (fluxo validado)
- [x] **Start de produção único** via `GameStartRequestProductionBootstrapper` (emissão de `GameStartRequestedEvent` apenas uma vez).
- [x] **Coordinator como único emissor de `RequestStart()`** após `TransitionCompleted + WorldLifecycleResetCompleted`.
- [x] **Sem duplicidade de `TransitionAsync()` por caminhos de debug**: gatilhos de debug condicionados a `UNITY_EDITOR`/`DEVELOPMENT_BUILD`.
- [x] **Gatilhos de debug isolados**:
  - `GameNavigationDebugTrigger` (ContextMenu) condicionado a Editor/Dev.
  - `PauseOverlayDebugTrigger` (ContextMenu) condicionado a Editor/Dev.
  - `GameplayExitToMenuDebugTrigger` já possui guarda por `UNITY_EDITOR || DEVELOPMENT_BUILD` + flag explícita.
- [x] **Fluxo Menu → Gameplay** continua usando `IGameNavigationService` apenas para transição (sem `RequestStart()` duplicado).

## Auditoria de RequestStart (resultado resumido)
**Permitido (fonte da verdade)**
- `Gameplay/GameLoop/GameLoopSceneFlowCoordinator.cs` → `gameLoop.RequestStart()` (apenas após `TransitionCompleted` + `WorldLifecycleResetCompleted`).

**Permitido (teste/QA/dev-only)**
- `QA/Deprecated/WorldMovementPermissionQaRunner.cs` → `RequestStart()` usado apenas em QA (agora sob `UNITY_EDITOR || DEVELOPMENT_BUILD`).

**Não permitido**
- Nenhum encontrado após a auditoria.

## Evidência (trecho mínimo de log esperado)
```
[Production][StartRequest] Start solicitado (GameStartRequestedEvent).
[GameLoopSceneFlow] Start REQUEST recebido. Disparando transição de cenas...
[GameLoopSceneFlow] Ready: TransitionCompleted + WorldLifecycleResetCompleted. Chamando GameLoop.RequestStart().
```

> Nota: o trecho acima é mínimo por design (sem log gigante) e cobre o **start** consolidado do fluxo de produção.
