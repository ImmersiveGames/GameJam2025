# World Lifecycle (NewScripts)

## Visão geral do reset determinístico
O reset do mundo segue a ordem garantida pelo `WorldLifecycleOrchestrator`: Acquire Gate → Hooks → Despawn → Hooks → Spawn → Hooks → Release. O fluxo realiza:
- Acquire: tenta adquirir o `ISimulationGateService` usando o token `WorldLifecycle.WorldReset` para serializar resets.
- Hooks (pré-despawn): executa hooks registrados por serviços de spawn, hooks de cena (registrados no provider da cena) e hooks explícitos no `WorldLifecycleHookRegistry`.
- Despawn: chama `DespawnAsync()` de cada `IWorldSpawnService` registrado, mantendo logs de início/fim.
- Hooks (pós-despawn/pré-spawn): executa `OnAfterDespawnAsync()` e `OnBeforeSpawnAsync()` seguindo a mesma ordem determinística de coleções.
- Spawn: chama `SpawnAsync()` dos serviços e, em seguida, hooks de atores e de mundo para `OnAfterSpawnAsync()`.
- Release: devolve o gate adquirido e finaliza com logs de duração.
【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L13-L111】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L161-L258】

## Onde o registry é criado e como injetar
- Criação: o `NewSceneBootstrapper` instancia e registra `WorldLifecycleHookRegistry` no escopo da cena durante o `Awake`, junto com `IActorRegistry` e `IWorldSpawnServiceRegistry` (sem `allowOverride`).
- Injeção: qualquer componente de cena pode declarar `[Inject] private WorldLifecycleHookRegistry _hookRegistry;` e chamar `DependencyManager.Provider.InjectDependencies(this);` para obter a instância da cena atual.
- Diagnóstico: há log verbose confirmando o registro do registry na cena.
【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs†L13-L73】

## Hooks disponíveis
- **`IWorldLifecycleHook`**: permite observar o ciclo de reset de mundo. Pode vir de três fontes na execução: (1) serviços que também implementam `IWorldLifecycleHook`, (2) hooks de cena registrados via `IDependencyProvider.GetAllForScene`, (3) hooks registrados explicitamente no `WorldLifecycleHookRegistry`. A ordem de execução é determinística e logada por fase.
- **`IActorLifecycleHook`**: componentes `MonoBehaviour` anexados a atores. São descobertos pelo orquestrador ao percorrer `Transform` dos atores registrados e executados nas fases de ator (`OnBeforeActorDespawnAsync` e `OnAfterActorSpawnAsync`).
【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/IWorldLifecycleHook.cs†L5-L17】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/Actors/IActorLifecycleHook.cs†L5-L17】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L161-L258】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L261-L345】

## Como registrar um hook no registry
1. Garanta que o componente tenha recebido injeção de dependências na cena:
   ```csharp
   public sealed class MySceneHookInstaller : MonoBehaviour
   {
       [Inject] private WorldLifecycleHookRegistry _hookRegistry;

       private void Awake()
       {
           DependencyManager.Provider.InjectDependencies(this);
           _hookRegistry.Register(new AnalyticsResetHook());
       }
   }
   ```
2. Implemente o hook herdando de `IWorldLifecycleHook`:
   ```csharp
   public sealed class AnalyticsResetHook : IWorldLifecycleHook
   {
       public Task OnBeforeDespawnAsync() => Task.CompletedTask;
       public Task OnAfterDespawnAsync() => Task.CompletedTask;
       public Task OnBeforeSpawnAsync() => Task.CompletedTask;
       public Task OnAfterSpawnAsync() => Task.CompletedTask;
   }
   ```
Hooks registrados aqui serão executados em todas as fases, após os hooks de spawn services e hooks de cena.
【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleHookRegistry.cs†L8-L27】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L67-L111】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L161-L258】

## Como criar um hook em ator
Use `ActorLifecycleHookBase` para componentes por ator:
```csharp
public sealed class NotifyHUDHook : ActorLifecycleHookBase
{
    public override Task OnAfterActorSpawnAsync()
    {
        // Ex.: sinalizar HUD local que o ator spawnou.
        return Task.CompletedTask;
    }

    public override Task OnBeforeActorDespawnAsync()
    {
        // Ex.: limpar indicadores antes do despawn.
        return Task.CompletedTask;
    }
}
```
Anexe o componente ao GameObject do ator. O orquestrador irá chamá-lo automaticamente nas fases de ator durante o reset.
【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/Actors/ActorLifecycleHookBase.cs†L5-L17】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L261-L345】

## QA: como reproduzir e o que esperar
1. Abra a cena **NewBootstrap** no Editor.
2. No `WorldLifecycleController`, use o menu de contexto `QA/Reset World Now` para disparar um reset determinístico manual.
3. Espere ver no Console:
   - Log verbose confirmando `WorldLifecycleHookRegistry registrado para a cena '<scene>'` vindo do `NewSceneBootstrapper`.
   - Logs de início/fim do reset e de cada fase (gate acquired/released, hooks, spawn/despawn) emitidos pelo `WorldLifecycleOrchestrator`.
4. Não deve haver logs dizendo que o registry foi criado pelo controller; ele apenas consome via DI.
【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleController.cs†L31-L88】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs†L24-L75】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L42-L111】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L161-L258】
