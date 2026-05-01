# Foundation / Core / Validation

> Fonte de leitura: snapshot do projeto em 2026-04-29.


## O que é

Conjunto simples de precondições para validação defensiva.

## Para que serve

- Validar argumentos.
- Evitar estados inválidos logo na entrada.
- Centralizar checks simples.

## Quando usar

Use em constructors, métodos públicos, serviços e pontos de fronteira onde parâmetros obrigatórios precisam ser claros.

## Como usar

```csharp
using _ImmersiveGames.NewScripts.Core.Validation;

public sealed class ExampleValidator
{
    public void Execute(string reason)
    {
        Preconditions.RequireNotNullOrWhiteSpace(reason, nameof(reason));
    }
}
```

## Quando não usar

- Não usar para substituir validação semântica complexa.
- Não usar para mascarar erro que deveria ser fail-fast específico do módulo.

## Arquivos principais

- `Foundation/Core/Validation/Preconditions.cs`

## Cuidados

- Precondition é validação local.
- Política de domínio continua no módulo dono.
