# Foundation / Core / Identifiers

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Utilitário para geração de IDs runtime únicos e legíveis.

## Para que serve

- Criar IDs de rastreio.
- Criar assinaturas curtas de runtime.
- Melhorar logs e observabilidade.
- Evitar colisões em execuções com Domain Reload desativado.

## Quando usar

Use para identificar instâncias runtime, ciclos de execução, trace IDs e assinaturas temporárias.

## Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Identifiers;

public sealed class ExampleIdConsumer
{
    private readonly UniqueIdFactory _idFactory = new();

    public string CreateRuntimeId(UnityEngine.GameObject owner)
    {
        return _idFactory.GenerateId(owner, prefix: "example");
    }
}
```

## Quando não usar

- Não usar como identidade autoral.
- Não usar como chave persistida de save.
- Não substituir IDs semânticos de domínio.

## Arquivos principais

- `Foundation/Core/Identifiers/UniqueIdFactory.cs`
- `Foundation/Core/Identifiers/README.md`

## Cuidados

- IDs são para runtime/observabilidade.
- Prefixos devem ser curtos e estáveis.
- Identidade semântica pertence ao módulo dono.
