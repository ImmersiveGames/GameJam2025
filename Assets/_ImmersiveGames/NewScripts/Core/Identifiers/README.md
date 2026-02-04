# Core.Identifiers — IDs únicos

## Contexto

O runtime precisa de IDs estáveis para:

- rastrear instâncias em logs (observabilidade),
- criar assinaturas (ex.: contextSignature/reason) sem colisões,
- suportar testes em Editor (inclusive com Domain Reload desativado).

A `UniqueIdFactory` gera IDs com:

- **salt de sessão** (curto) + **contador monotônico** (thread-safe),
- *hint* opcional com o nome do `GameObject` (apenas para legibilidade),
- **prefix** opcional para sub-ids.

## Como usar

### 1) Instanciar e registrar (recomendado)

Registre `IUniqueIdFactory` no **escopo global** em um bootstrap.

```csharp
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Identifiers;

var factory = new UniqueIdFactory();
DependencyManager.Provider.RegisterGlobal<IUniqueIdFactory>(factory);
```

### 2) Gerar um ID

```csharp
using _ImmersiveGames.NewScripts.Core.Identifiers;

string id = uniqueIdFactory.GenerateId(gameObject);
string subId = uniqueIdFactory.GenerateId(gameObject, prefix: "sensor");
```

### 3) Contagem por nome (debug)

```csharp
int spawned = uniqueIdFactory.GetInstanceCount("Player");
```

## Boas práticas

- Não use o ID como “nome amigável” (isso é responsabilidade do domínio/UI).
- Evite persistir esses IDs como *save data* (eles são por sessão/runtime).
- Se o ID for parte de `reason`/`signature`, mantenha prefixos curtos e estáveis.
