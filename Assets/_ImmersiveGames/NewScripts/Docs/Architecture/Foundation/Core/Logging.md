# Foundation / Core / Logging

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Base de logging, níveis de debug e fail-fast.

## Para que serve

- Padronizar logs.
- Controlar verbosidade por classe.
- Emitir evidência de fluxo.
- Falhar cedo em contrato/configuração obrigatória inválida.

## Quando usar

Use para lifecycle relevante, diagnóstico de runtime, erros estruturais e evidência de fluxo.

## Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Logging;

[DebugLevel(DebugLevel.Verbose)]
public sealed class ExampleService
{
    public void Execute(string reason)
    {
        DebugUtility.LogInfo(
            typeof(ExampleService),
            $"Example executed. reason='{reason}'");
    }
}
```

Para falha estrutural obrigatória:

```csharp
using _ImmersiveGames.NewScripts.Core.Logging;

public sealed class RequiredConfigConsumer
{
    public void Configure(object config)
    {
        if (config == null)
        {
            HardFailFastH1.Fail(
                typeof(RequiredConfigConsumer),
                "Required config is missing.");
        }
    }
}
```

## Quando não usar

- Não usar log como controle de fluxo.
- Não usar fail-fast para estado esperado de gameplay.
- Não esconder erro estrutural com fallback silencioso.
- Não criar formatos paralelos sem necessidade.

## Arquivos principais

- `Foundation/Core/Logging/DebugUtility.cs`
- `Foundation/Core/Logging/DebugLevelAttribute.cs`
- `Foundation/Core/Logging/HardFailFastH1.cs`
- `Foundation/Core/Logging/ResetLogTags.cs`
- `Foundation/Core/Logging/Config/LoggingConfigAsset.cs`
- `Foundation/Core/Logging/README.md`

## Cuidados

- Logs úteis carregam campos como `reason`, `source`, `signature`, `target`, `profile` ou `traceId`.
- Fail-fast é para contrato obrigatório quebrado.
- Logs por frame devem ser evitados.
