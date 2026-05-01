# Foundation / Platform / Composition

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Infraestrutura de composição, registro e resolução de serviços.

Inclui tanto registros por escopo quanto o pipeline global de composição modular.

## Para que serve

- Registrar serviços globais, por cena e por objeto.
- Resolver dependências por interfaces.
- Injetar dependências quando necessário.
- Coordenar installers e runtime composers.
- Manter boot determinístico e auditável.

## Quando usar

Use em bootstraps, installers, runtime composers e composition roots.

## Como usar

Registro simples:

```csharp
using _ImmersiveGames.NewScripts.Core.Composition;

public sealed class ExampleInstaller
{
    public void Install()
    {
        DependencyManager.Provider.RegisterGlobal<IExampleService>(new ExampleService());
    }
}
```

Resolução simples:

```csharp
using _ImmersiveGames.NewScripts.Core.Composition;

public sealed class ExampleConsumer
{
    public bool TryExecute()
    {
        if (!DependencyManager.Provider.TryGet<IExampleService>(out var service))
        {
            return false;
        }

        service.Execute();
        return true;
    }
}
```

## Quando não usar

- Não registrar serviço estrutural tarde para corrigir boot incompleto.
- Não usar service locator no hot path quando dependência obrigatória pode ser explícita.
- Não colocar wiring interno de módulo no root global.
- Não depender de `Awake`, `Start` ou `OnEnable` como bootstrap canônico.

## Arquivos principais

- `Foundation/Platform/Composition/CompositionModuleDescriptor.cs`
- `Foundation/Platform/Composition/CompositionPipelineStep.cs`
- `Foundation/Platform/Composition/DependencyInjector.cs`
- `Foundation/Platform/Composition/DependencyManager.cs`
- `Foundation/Platform/Composition/GlobalCompositionRoot.*.cs`
- `Foundation/Platform/Composition/GlobalServiceRegistry.cs`
- `Foundation/Platform/Composition/IDependencyProvider.cs`
- `Foundation/Platform/Composition/ObjectServiceRegistry.cs`
- `Foundation/Platform/Composition/SceneScopeCompositionRoot.cs`
- `Foundation/Platform/Composition/SceneScopeCompositionRoot.ActorGroupRearm.cs`
- `Foundation/Platform/Composition/SceneServiceCleaner.cs`
- `Foundation/Platform/Composition/SceneServiceRegistry.cs`
- `Foundation/Platform/Composition/ServiceRegistry.cs`
- `Foundation/Platform/Composition/README.md`

## Cuidados

- Installer registra contratos e serviços.
- Runtime composer ativa runtime e bridges.
- Dependência obrigatória ausente deve falhar cedo.
- O root global coordena; o módulo continua dono do seu wiring interno.
