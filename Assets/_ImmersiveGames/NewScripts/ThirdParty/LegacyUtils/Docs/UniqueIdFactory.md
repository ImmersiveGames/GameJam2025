# 🏷️ `UniqueIdFactory` — Guia de Uso

## Visão Geral

`UniqueIdFactory` gera identificadores consistentes para objetos e atores no ambiente multiplayer local. O serviço implementa `IUniqueIdFactory` e é registrado globalmente pelo `DependencyBootstrapper`, garantindo disponibilidade em qualquer cena.

## Estratégia de Geração

1. **Busca por `IActor` ancestral** — Determina contexto do objeto.
2. **Ator principal (possui `IActor` no próprio GameObject`)**
   * Se possuir `PlayerInput`, gera `Player_{playerIndex}`.
   * Caso contrário, usa contador incremental: `NPC_{ActorName}_{n}`.
3. **Filhos de um ator** — Reutilizam `actor.ActorId`, evitando conflitos.
4. **Objetos sem ator** — Gera `Obj_{BaseName}_{n}`.
5. **Prefixo opcional** — Quando informado, concatena ao final: `ActorId_{prefix}`.

Todos os passos são logados em nível Verbose através do `DebugUtility`, auxiliando auditorias.

## Exemplos

```csharp
[Inject] private IUniqueIdFactory _idFactory;

private string EnsureId(GameObject owner)
{
    return _idFactory.GenerateId(owner, prefix: "HUD");
}
```

```csharp
int spawnedCount = _idFactory.GetInstanceCount("NPC_Slime");
```

## Integração

* **Registro** — `DependencyBootstrapper` chama `EnsureGlobal<IUniqueIdFactory>(() => new UniqueIdFactory())` durante o bootstrap.
* **Injeção** — Utilize `[Inject]` para receber a implementação ou resolva diretamente via `DependencyManager.Instance.TryGetGlobal`.
* **Uso em Pools** — Ideal para nomear instâncias de `ObjectPool` sem duplicatas.

## Boas Práticas

| Situação | Recomendações |
| --- | --- |
| Atores controlados pelo jogador | Certifique-se de adicionar `PlayerInput` para que o ID seja `Player_{index}`. |
| Objetos compartilhados entre jogadores | Utilize `prefix` para diferenciar (`ActorId_UI`, `ActorId_FX`). |
| Testes automatizados | Limpe o singleton (`DependencyManager.ClearGlobalServices`) entre cenários para resetar contadores. |
| Logs excessivos | Use o `DebugManager` para reduzir o nível padrão ou desligar Verbose no Player. |

A `UniqueIdFactory` segue SRP e facilita rastreamento de entidades, reduzindo riscos de colisões de ID em cenários de coop local.
