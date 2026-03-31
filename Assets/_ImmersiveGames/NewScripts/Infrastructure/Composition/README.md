# Core.Composition — Registro de serviços + Injeção

## Contexto

O NewScripts precisa de uma forma **simples e determinística** de compor dependências sem depender de frameworks externos.
Este módulo fornece:

- Um **Service Registry** com 3 escopos: **Global**, **Cena**, **Objeto**.
- Um **Injector** (reflection) que injeta campos marcados com `[Inject]`.

> Filosofia: features dependem de **interfaces** (DIP). O bootstrap registra implementações.

---

## Componentes

- `DependencyManager` (MonoBehaviour singleton): entrypoint em runtime, expõe `IDependencyProvider`.
- `IDependencyProvider`: API de registro/resolução.
- `DependencyInjector`: injeta dependências em campos `[Inject]`.
- `GlobalServiceRegistry` / `SceneServiceRegistry` / `ObjectServiceRegistry`: armazenamento por escopo.
- `SceneServiceCleaner`: utilitário para limpar serviços de cena quando apropriado.

---

## How to use

### 1) Registrar serviços (bootstrap/installer)

Use o escopo mais restrito possível:

- **Global**: serviços cross-scene (ex.: EventBus global, políticas, navigation, etc.).
- **Cena**: serviços que pertencem ao lifecycle de uma cena (ex.: registries, controllers de cena, etc.).
- **Objeto**: dependência específica por instância/owner (`objectId`).

```csharp
using _ImmersiveGames.NewScripts.Core.Composition;

public sealed class ExampleInstaller
{
    public void InstallGameplaySceneServices()
    {
        // Global
        DependencyManager.Provider.RegisterGlobal<IMyService>(new MyService());

        // Cena (use o nome da cena carregada)
        DependencyManager.Provider.RegisterForScene<IGameplayRules>("GameplayScene", new GameplayRules());

        // Objeto (quando você tem um owner com id estável)
        DependencyManager.Provider.RegisterForObject<IHealthService>("actor.player.1", new HealthService());
    }
}
```

### 2) Resolver serviços (sem injeção)

```csharp
using _ImmersiveGames.NewScripts.Core.Composition;

public sealed class ExampleConsumer
{
    public void Tick()
    {
        if (DependencyManager.Provider.TryGet<IGameplayRules>(out var rules))
        {
            rules.Apply();
        }
    }
}
```

> `TryGet<T>(objectId)` segue a ordem: **Objeto → Cena do target → Global**.

### 3) Injeção via atributo `[Inject]`

Marque campos com `[Inject]` e chame `InjectDependencies(this)` quando o objeto estiver pronto.

```csharp
using UnityEngine;
using _ImmersiveGames.NewScripts.Core.Composition;

public sealed class ExampleBehaviour : MonoBehaviour
{
    [Inject] private IMyService _myService;

    private void Awake()
    {
        DependencyManager.Provider.InjectDependencies(this);
    }
}
```

**Observação importante (cenas aditivas):** o injector tenta usar a cena do próprio `MonoBehaviour` (via `gameObject.scene`).
Se estiver em `DontDestroyOnLoad`/inválido, cai para `SceneManager.GetActiveScene()`.

---

## Limpeza (scene lifecycle)

Quando um serviço é registrado como **Scene**, ele precisa ser limpo quando aquela cena não for mais válida.
O `DependencyManager` expõe:

- `ClearSceneServices(sceneName)`
- `ClearAllSceneServices()`

Use `SceneServiceCleaner` no pipeline de unload/transition (ex.: SceneFlow/WorldLifecycle) para manter determinismo.

---

## Anti-patterns

- Registrar tudo como global (vira *service locator* onipresente e aumenta risco de vazamentos).
- Resolver dependências “no meio” do gameplay sem um contrato claro (prefira injeção + eventos).
- Fazer fallback silencioso quando um serviço crítico não existe (seguir a política Strict/Release do projeto).
