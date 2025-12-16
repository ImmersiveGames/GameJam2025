# World Lifecycle (NewScripts)

## Visão geral do reset determinístico
O reset do mundo segue a ordem garantida pelo `WorldLifecycleOrchestrator`: Acquire Gate → Hooks pré-despawn → Actor hooks pré-despawn → Despawn → Hooks pós-despawn/pré-spawn → Spawn → Actor hooks pós-spawn → Hooks finais → Release. O fluxo realiza:
- Acquire: tenta adquirir o `ISimulationGateService` usando o token `WorldLifecycle.WorldReset` para serializar resets.
- Hooks (pré-despawn): executa hooks registrados por serviços de spawn, hooks de cena (registrados no provider da cena) e hooks explícitos no `WorldLifecycleHookRegistry`.
- Actor hooks (pré-despawn): percorre atores registrados e executa `OnBeforeActorDespawnAsync()` de cada `IActorLifecycleHook` encontrado.
- Despawn: chama `DespawnAsync()` de cada `IWorldSpawnService` registrado, mantendo logs de início/fim.
- Hooks (pós-despawn/pré-spawn): executa `OnAfterDespawnAsync()` e `OnBeforeSpawnAsync()` seguindo a mesma ordem determinística de coleções.
- Spawn: chama `SpawnAsync()` dos serviços e, em seguida, hooks de atores e de mundo para `OnAfterSpawnAsync()`.
- Release: devolve o gate adquirido e finaliza com logs de duração.
 - Nota: se não houver hooks registrados para uma fase, o sistema emite log verbose `"<PhaseName> phase skipped (hooks=0)"` para diagnóstico e para confirmar que a fase foi considerada.
【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L13-L345】

## Onde o registry é criado e como injetar
- Criação (guardrail): o `NewSceneBootstrapper` instancia e registra `WorldLifecycleHookRegistry` no escopo da cena durante o `Awake`, junto com `IActorRegistry` e `IWorldSpawnServiceRegistry` (sem `allowOverride`). `WorldLifecycleController` e `WorldLifecycleOrchestrator` nunca criam ou registram o registry; eles apenas o consomem via DI. Qualquer tentativa de recriar ou registrar fora do bootstrapper deve ser tratada como erro.
- Injeção: qualquer componente de cena pode declarar `[Inject] private WorldLifecycleHookRegistry _hookRegistry;` e chamar `DependencyManager.Provider.InjectDependencies(this);` para obter a instância da cena atual.
- Diagnóstico: há log verbose confirmando o registro do registry na cena.
【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/Scene/NewSceneBootstrapper.cs†L13-L73】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleController.cs†L31-L88】

## Hooks disponíveis
- **`IWorldLifecycleHook`**: permite observar o ciclo de reset de mundo. Pode vir de três fontes na execução: (1) serviços que também implementam `IWorldLifecycleHook`, (2) hooks de cena registrados via `IDependencyProvider.GetAllForScene`, (3) hooks registrados explicitamente no `WorldLifecycleHookRegistry`. A ordem de execução segue exatamente o pipeline determinístico (pré-despawn → actor pré-despawn → despawn → pós-despawn/pré-spawn → spawn → actor pós-spawn → finais) e é logada por fase.
- **`IActorLifecycleHook`**: componentes `MonoBehaviour` anexados a atores. São descobertos pelo orquestrador ao percorrer `Transform` dos atores registrados e executados nas fases de ator (`OnBeforeActorDespawnAsync` e `OnAfterActorSpawnAsync`) preservando a ordem fixa do reset.

### Ordenação determinística
- **Hooks de mundo (`IWorldLifecycleHook`)**: ordenados por `Order` quando o hook implementa `IOrderedLifecycleHook` (default = 0) e, como desempate, por `Type.FullName` com comparação ordinal.
- **Hooks de ator (`IActorLifecycleHook`)**: coletados via `GetComponentsInChildren(...)` em cada ator, mas a execução não depende da ordem retornada; antes de executar, a lista é ordenada por (`Order`, `Type.FullName`) com o mesmo comparador usado nos hooks de mundo.
- **Observação**: o critério (`Order`, `Type.FullName`) garante que a ordem de execução permaneça estável entre resets e ambientes.
【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/IWorldLifecycleHook.cs†L5-L17】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/Actors/IActorLifecycleHook.cs†L5-L17】【F:Assets/_ImmersiveGames/NewScripts/Infrastructure/World/WorldLifecycleOrchestrator.cs†L161-L345】

## Do / Don't
- **Do:** criar o `WorldLifecycleHookRegistry` apenas no `NewSceneBootstrapper` e reutilizá-lo via injeção na cena.
- **Do:** manter a ordem do reset exatamente como: Acquire Gate → Hooks pré-despawn → Actor hooks pré-despawn → Despawn → Hooks pós-despawn/pré-spawn → Spawn → Actor hooks pós-spawn → Hooks finais → Release Gate.
- **Don't:** instanciar ou registrar manualmente o registry no controller ou no orquestrator; eles apenas consomem a instância de cena.
- **Don't:** alterar a sequência das fases do reset ou mover responsabilidades entre bootstrapper, controller e orquestrator.

## Como registrar um hook no registry
1. Garanta que o componente tenha recebido injeção de dependências na cena e registre no `Awake` (o registry já existe porque nasceu no bootstrapper):
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

## IOrderedLifecycleHook
- Interface opcional que adiciona a propriedade `Order` para controlar prioridade.
- Aplica-se a **hooks de mundo** e a **hooks de ator**; quem não implementa recebe o valor padrão 0 e ainda participa da ordenação determinística por (`Order`, `Type.FullName`).

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

## Troubleshooting: QA/Testers e Boot Order
- **Sintomas (console):**
  - QA/tester não encontra `WorldLifecycleHookRegistry` / `IActorRegistry`.
  - Reset não dispara ou aborta cedo.
  - Logs iniciais “de erro” na criação do QA/Tester ao entrar em Play Mode.
  - Mensagens de “registries inexistentes” logo no `Awake` do tester.
- **Causa provável:**
  - `NewSceneBootstrapper` ausente na cena ou executando depois do QA/tester.
  - QA rodando no `Awake()` sem lazy injection/retry, antes do bootstrap.
- **Checklist de ação (3 passos):**
  1. Garantir `NewSceneBootstrapper` presente na cena e ativo.
  2. Garantir que QA/testers usem lazy injection + retry curto + timeout (padrão já descrito).
  3. Se ainda falhar, abortar com mensagem acionável apontando ausência/ordem errada do bootstrapper.
- **Nota sobre `NEWSCRIPTS_MODE`:** quando ativo, inicializadores/bootstraps podem ser ignorados por design (modo de desenvolvimento); isso pode parecer falha de QA, mas não é bug de runtime.

## Boot order & Dependency Injection timing (Scene scope)
- Os serviços de cena (`IActorRegistry`, `IWorldSpawnServiceRegistry`, `WorldLifecycleHookRegistry`) são registrados pelo `NewSceneBootstrapper` durante o bootstrap da cena. Outros componentes não devem criar registries próprios nem assumir que eles existem antes do bootstrapper executar.
- Componentes que consomem esses serviços devem evitar injeção no `Awake()` quando a ordem de execução ainda não garantiu o bootstrapper; prefira `Start()` ou injeção lazy com retry curto.
- QA testers (ex.: `WorldLifecycleQATester`) adotam injeção lazy com retry e timeout: aguardam alguns frames/tempo curto se o registry ainda não existe, abortando com mensagem clara se o bootstrapper não rodou. Isso evita falsos negativos em cenas novas.
- O `WorldLifecycleOrchestrator` assume que dependências já foram resolvidas pelo fluxo de bootstrap; ele não corrige ordem de inicialização e não registra serviços de cena.
