# Foundation / Platform / Pooling

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Sistema técnico de reutilização de `GameObject`.

## Para que serve

- Reduzir custo de `Instantiate`/`Destroy`.
- Padronizar `ensure`, `prewarm`, `rent` e `return`.
- Manter pooling como backend técnico, não como owner de gameplay.

## Quando usar

Use para objetos que são criados e descartados com frequência e podem ser reaproveitados.

Exemplos:

- SFX pooled;
- VFX pooled;
- objetos temporários;
- elementos runtime repetitivos.

## Como usar

Fluxo conceitual:

```text
PoolDefinitionAsset -> IPoolService.EnsureRegistered -> Prewarm/Rent/Return
```

Objetos pooled podem implementar hooks:

```csharp
using _ImmersiveGames.NewScripts.Foundation.Platform.Pooling.Contracts;

public sealed class ExamplePooledObject : PooledBehaviour
{
    public override void OnPoolRent()
    {
        // Reinicia estado local ao sair do pool.
    }

    public override void OnPoolReturn()
    {
        // Limpa estado local antes de voltar ao pool.
    }
}
```

## Quando não usar

- Não usar pooling para decidir identidade de gameplay object.
- Não usar como substituto de spawn/materialization owner.
- Não usar para objetos cuja reinicialização correta não está garantida.

## Arquivos principais

- `Foundation/Platform/Pooling/Config/PoolDefinitionAsset.cs`
- `Foundation/Platform/Pooling/Contracts/IPoolService.cs`
- `Foundation/Platform/Pooling/Contracts/IPoolableObject.cs`
- `Foundation/Platform/Pooling/Contracts/PooledBehaviour.cs`
- `Foundation/Platform/Pooling/Runtime/GameObjectPool.cs`
- `Foundation/Platform/Pooling/Runtime/PoolService.cs`
- `Foundation/Platform/Pooling/Runtime/PoolRuntimeHost.cs`
- `Foundation/Platform/Pooling/Runtime/PoolRuntimeInstance.cs`
- `Foundation/Platform/Pooling/Runtime/PoolAutoReturnTracker.cs`
- `Foundation/Platform/Pooling/QA/PoolingQaContextMenuDriver.cs`
- `Foundation/Platform/Pooling/QA/PoolingQaMockPooledObject.cs`
- `Foundation/Platform/Pooling/Pooling-How-To.md`
- `Foundation/Platform/Pooling/Pooling-Quick-Access.html`

## Cuidados

- Pooling é backend técnico.
- Spawn ou módulo consumidor continua dono da semântica do objeto.
- `prewarm` declara intenção; não deve virar execução implícita fora do owner correto.
- Todo objeto pooled precisa limpar estado corretamente no retorno.
