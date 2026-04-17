# Core.Logging — DebugUtility e níveis

## Contexto

O projeto depende de logs como **fonte de evidência** (baseline + auditorias).
Este módulo centraliza:

- `DebugUtility`: API única para logs (Verbose/Info/Warning/Error) + cores.
- `DebugLevelAttribute`: anota classes para controle de verbosidade.
- `DebugLogSettings` (`.asset` + `...cs`): configurações de logging.
- `ResetLogTags`: tags utilitárias para padronizar mensagens em resets/flows.

## Como usar

### 1) Anotar o nível padrão da classe (opcional)

```csharp
using _ImmersiveGames.NewScripts.Core.Logging;

[DebugLevel(DebugLevel.Verbose)]
public sealed class ExampleService
{
}
```

### 2) Escrever logs

```csharp
using _ImmersiveGames.NewScripts.Core.Logging;

DebugUtility.LogVerbose(typeof(ExampleService), "Mensagem verbose");
DebugUtility.LogInfo(typeof(ExampleService), "Mensagem info");
DebugUtility.LogWarning(typeof(ExampleService), "Mensagem warning");
DebugUtility.LogError(typeof(ExampleService), "Mensagem error");
```

### 3) Usar cores (quando fizer sentido)

> Use com moderação: cores ajudam em logs longos, mas podem virar ruído.

```csharp
DebugUtility.LogVerbose(
    typeof(ExampleService),
    "Evento importante no pipeline.",
    DebugUtility.Colors.CrucialInfo);
```

## Regras práticas (para não quebrar observabilidade)

- Prefira mensagens curtas e com campos-chave (`reason`, `signature`, `profile`, `target`) quando o log for parte de evidência.
- Evite logs por frame em produção.
- Não invente formatos paralelos: siga o contrato de observabilidade do projeto.

