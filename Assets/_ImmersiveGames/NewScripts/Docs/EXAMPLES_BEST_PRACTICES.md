# Exemplos e boas práticas (NewScripts)

Os exemplos abaixo priorizam clareza e compatibilidade com o que já existe no projeto.
Quando a assinatura de um tipo/método não é 100% certa, o trecho é marcado como **PSEUDOCÓDIGO**.

## 1) Exemplo: Hook de World Lifecycle (log seguro)

```csharp
// Exemplo conceitual: implemente a interface existente no seu projeto.
// O objetivo é ilustrar o tipo de log e o momento correto de execução.
public sealed class ExampleWorldLifecycleHook /* : IWorldLifecycleHook */
{
    public int Order => 100;

    public void OnBeforeDespawn(/* context */)
    {
        DebugUtility.LogVerbose(typeof(ExampleWorldLifecycleHook),
            "[WorldLifecycle] OnBeforeDespawn (Example)");
    }

    public void OnAfterSpawn(/* context */)
    {
        DebugUtility.LogVerbose(typeof(ExampleWorldLifecycleHook),
            "[WorldLifecycle] OnAfterSpawn (Example)");
    }
}
```

Boas práticas:
- Não acessar serviços globais diretamente dentro de hooks sem motivo (prefira DI/registries).
- Logar sempre o “context signature” quando disponível.

## 2) Exemplo: Registro de serviços por cena (NewSceneBootstrapper)

Checklist típico de cena NewScripts:
- registrar `INewSceneScopeMarker` e `IWorldSpawnContext`
- registrar registries (actor/spawn/hooks)
- registrar participantes de reset (ex.: PlayersResetParticipant)
- **se for MenuScene**, `WorldDefinition` pode ser `null` (sem spawn)

## 2.1) Exemplo: Componente de gameplay resettable (Cleanup/Restore/Rebind)

```csharp
using System.Threading.Tasks;
using UnityEngine;
using _ImmersiveGames.NewScripts.Gameplay.Reset;

public sealed class ExampleResourceBar : MonoBehaviour,
    IGameplayResettable,
    IGameplayResetOrder,
    IGameplayResetTargetFilter
{
    [SerializeField] private int max = 100;
    private int _value;

    public int ResetOrder => -10; // executa antes de componentes default (0)

    public bool ShouldParticipate(GameplayResetTarget target)
        => target == GameplayResetTarget.PlayersOnly || target == GameplayResetTarget.AllActorsInScene;

    public Task ResetCleanupAsync(GameplayResetContext ctx)
    {
        // Desfaz assinaturas/eventos transitórios e zera caches.
        return Task.CompletedTask;
    }

    public Task ResetRestoreAsync(GameplayResetContext ctx)
    {
        // Restaura estado base (sem rebind de dependências).
        _value = max;
        return Task.CompletedTask;
    }

    public Task ResetRebindAsync(GameplayResetContext ctx)
    {
        // Re-resolve serviços e re-assina eventos.
        return Task.CompletedTask;
    }
}
```

Notas:
- Use `IGameplayResetOrder` apenas quando houver dependências reais entre componentes.
- Use `IGameplayResetTargetFilter` para evitar participar em targets que não fazem sentido para o componente.

## 3) Exemplo: Transição de cena com profile (PSEUDOCÓDIGO)

Importante: `SceneTransitionContext` é `readonly struct`. Evite object initializer e `null`.

```csharp
// PSEUDOCÓDIGO: use o serviço e os tipos reais do seu projeto.
var transitionService = provider.ResolveGlobal<ISceneTransitionService>();

// O "profileName" deve mapear para Resources/SceneFlow/Profiles/<profileName>
string profileName = "startup";

// O request/context deve ser construído pelas fábricas/helpers existentes no projeto.
// Ex.: SceneTransitionRequestFactory.CreateMenuStartup(profileName), etc.
var request = /* construir request/context com:
                 Load=[MenuScene, UIGlobalScene],
                 Unload=[NewBootstrap],
                 Active='MenuScene',
                 UseFade=true,
                 Profile=profileName */;

await transitionService.TransitionAsync(request);
```

Boas práticas:
- Padronizar profiles em `Resources/SceneFlow/Profiles/`.
- Se profile não existir, degradar para defaults (não travar a transição).
- Garantir que `WorldLifecycleResetCompletedEvent` seja emitido (mesmo em SKIP) para destravar o Coordinator.

## 4) Depuração rápida (o que procurar no log)
- `[SceneFlow] Iniciando transição`
- `[SceneFlow] Carregando FadeScene`
- `[SceneFlow] Carregando cena 'MenuScene'`
- `[SceneFlow] Carregando cena 'UIGlobalScene'`
- `[WorldLifecycle] SceneTransitionScenesReady recebido`
- `Reset SKIPPED` (startup/menu) ou “reset executado”
- `[Readiness] ... gate liberado`

## GameLoop: como evitar ambiguidades REQUEST/COMMAND

...
## Anti-patterns (evitar)
- Tratar `CanPerform(...)` como autorização final de gameplay (use `IStateDependentService`).
- Emissão de start “definitivo” diretamente no REQUEST (iniciar GameLoop sem aguardar ready).
- Disparar múltiplas transições concorrentes esperando determinismo sem correlação extra (não suportado).
