# Fix de duplicação — WorldLifecycleResetCompletedEvent

## Antes

- Driver (`WorldLifecycleSceneFlowResetDriver`) publicava `ResetCompleted` em paralelo ao `ResetWorldService`.
- Isso gerava duplicação quando o reset era executado via DI.

## Depois

- **Publisher canônico:** `ResetWorldService.TriggerResetAsync` (fluxo normal e catch).
- **Driver** só publica em **SKIP/fallback/defensivo** (profile != gameplay, sem controllers, assinatura inválida) ou quando executa reset via controllers.

## Verificação (rg)

Comando executado:

```
rg -n "EventBus<\s*WorldLifecycleResetCompletedEvent\s*>\.Raise" Assets/_ImmersiveGames/NewScripts
```

Saída:

```
Assets/_ImmersiveGames/NewScripts/Lifecycle/World/Bridges/SceneFlow/WorldLifecycleSceneFlowResetDriver.cs:258:            EventBus<WorldLifecycleResetCompletedEvent>.Raise(
Assets/_ImmersiveGames/NewScripts/Lifecycle/World/Runtime/ResetWorldService.cs:49:                EventBus<WorldLifecycleResetCompletedEvent>.Raise(new WorldLifecycleResetCompletedEvent(ctx, rsn));
Assets/_ImmersiveGames/NewScripts/Lifecycle/World/Runtime/ResetWorldService.cs:56:                EventBus<WorldLifecycleResetCompletedEvent>.Raise(new WorldLifecycleResetCompletedEvent(ctx, rsn));
```

> Observação: o publish no driver está restrito ao método `PublishResetCompleted(...)`, usado somente nos ramos SKIP/fallback/defensivo descritos acima.
