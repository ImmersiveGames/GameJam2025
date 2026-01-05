# ResetWorld Audit — 2026-01-05

## Objetivo
Garantir que o entry-point de produção para hard reset esteja explícito e auditável, com logs rastreáveis, sem quebrar o Baseline 2.0.

## Production trigger oficial (não-QA)
O gatilho canônico de produção continua sendo o fluxo do SceneFlow:
- `SceneTransitionService` emite `SceneTransitionScenesReadyEvent` após o load/unload/active.
- `WorldLifecycleRuntimeCoordinator.OnScenesReady` consome esse evento e dispara o hard reset.

Evidência via `rg` (arquivo + linhas + trecho):

```text
Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/SceneTransitionService.cs
130:                EventBus<SceneTransitionScenesReadyEvent>.Raise(new SceneTransitionScenesReadyEvent(context));

Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Runtime/WorldLifecycleRuntimeCoordinator.cs
55:        private void OnScenesReady(SceneTransitionScenesReadyEvent evt)
135:                $"[WorldLifecycle] Reset REQUESTED. reason='{resetReason}', signature='{signature}', profile='{profileId}'.",
```

## Wiring (DI / registro global)
Evidência via `rg` (arquivo + linhas + trecho):

```text
Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Runtime/WorldLifecycleRuntimeCoordinator.cs
21:    public sealed class WorldLifecycleRuntimeCoordinator : IWorldResetRequestService

Assets/_ImmersiveGames/NewScripts/Infrastructure/GlobalBootstrap.cs
131:            RegisterWorldResetRequestService();
303:        private static void RegisterWorldResetRequestService()
305:            if (DependencyManager.Provider.TryGetGlobal<IWorldResetRequestService>(out var existing) && existing != null)
308:                    "[WorldLifecycle] IWorldResetRequestService já registrado no DI global.",
316:                    "[WorldLifecycle] WorldLifecycleRuntimeCoordinator indisponível. IWorldResetRequestService não será registrado.");
320:            DependencyManager.Provider.RegisterGlobal<IWorldResetRequestService>(coordinator, allowOverride: false);
323:                "[WorldLifecycle] IWorldResetRequestService registrado no DI global (via WorldLifecycleRuntimeCoordinator).",

Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Runtime/WorldResetRequestHotkeyDev.cs
18:        private IWorldResetRequestService _resetRequestService;
30:                    "[WorldLifecycle] IWorldResetRequestService não disponível. Hotkey ignorado.");
44:            return DependencyManager.Provider.TryGetGlobal<IWorldResetRequestService>(out _resetRequestService) &&

Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Runtime/IWorldResetRequestService.cs
8:    public interface IWorldResetRequestService
```

## Pontos de chamada (produção / DEV)
- **Produção (SceneFlow → WorldLifecycle)**: `WorldLifecycleRuntimeCoordinator.OnScenesReady` (hard reset após ScenesReady).
- **Produção (manual via DI)**: `WorldLifecycleRuntimeCoordinator.RequestResetAsync(string source)`.
- **DEV**: `WorldResetRequestHotkeyBridge` (Shift+R) chama `RequestResetAsync("Gameplay/HotkeyR")`.

## Logs (código) — Reset REQUESTED / Reset IGNORED
Evidência via `rg` (arquivo + linhas + trecho):

```text
Assets/_ImmersiveGames/NewScripts/Infrastructure/WorldLifecycle/Runtime/WorldLifecycleRuntimeCoordinator.cs
96:                    $"[WorldLifecycle] Reset IGNORED (duplicate). signature='{signature}', profile='{profileId}'."
117:                $"[WorldLifecycle] Reset REQUESTED. reason='{resetReason}', signature='{signature}', profile='{profileId}'.",
184:                $"[WorldLifecycle] Reset REQUESTED. reason='{reason}', source='{safeSource}', scene='{activeSceneName}'.",
201:                    $"[WorldLifecycle] Reset IGNORED (scene-transition). reason='{reason}', scene='{activeSceneName}', detail='SceneTransition gate ativo', signature='{signatureInfo}', profile='{profileInfo}', targetScene='{targetSceneInfo}'."
```

## Logs (smoke) — evidência atual disponível
Log de referência existente no repositório:

```text
Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Smoke-LastRun.log
208:<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='startup', signature='p:startup|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:NewBootstrap', reason='Skipped_StartupOrFrontend:profile=startup;scene=MenuScene'. (@ 5,93s)</color>
315:<color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Disparando hard reset após ScenesReady. reason='ScenesReady/GameplayScene', profile='gameplay'</color>
409:<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 9,50s)</color>
534:<color=#A8DEED>[INFO] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Disparando hard reset após ScenesReady. reason='ScenesReady/GameplayScene', profile='gameplay'</color>
639:<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='gameplay', signature='p:gameplay|a:GameplayScene|f:1|l:GameplayScene|UIGlobalScene|u:MenuScene', reason='ScenesReady/GameplayScene'. (@ 15,14s)</color>
779:<color=#A8DEED>[VERBOSE] [WorldLifecycleRuntimeCoordinator] [WorldLifecycle] Emitting WorldLifecycleResetCompletedEvent. profile='frontend', signature='p:frontend|a:MenuScene|f:1|l:MenuScene|UIGlobalScene|u:GameplayScene', reason='Skipped_StartupOrFrontend:profile=frontend;scene=MenuScene'. (@ 19,61s)</color>
```

### Pendência (necessita rodar smoke local)
Para cumprir o requisito de evidência end-to-end dos novos logs, é necessário rodar o smoke e coletar as linhas com:
- `"[WorldLifecycle] Reset REQUESTED ..."`
- `"[WorldLifecycle] Reset IGNORED (scene-transition) ..."` (quando o request manual ocorre durante transição ativa)
- ausência de `"[Baseline][FAIL]"`

> Observação: este ambiente não executou o Unity Editor, então o log do smoke acima ainda não contém essas novas entradas.
