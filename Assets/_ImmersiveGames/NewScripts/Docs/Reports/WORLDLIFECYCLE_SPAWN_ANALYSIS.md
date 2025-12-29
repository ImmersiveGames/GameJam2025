> Moved from `Infrastructure/WorldLifecycle/WORLDLIFECYCLE_SPAWN_ANALYSIS.md` on 2025-12-29.

# WorldLifecycle Spawn — Análise (estrutura, completude, organização, SOLID)

Data: 2025-12-26  
Escopo: NewScripts — `Infrastructure/WorldLifecycle/Spawn` (baseline)

## 1) Estrutura atual (como as peças se encaixam)

**Fluxo de criação/registro (macro):**
1. `NewSceneBootstrapper` cria e registra serviços de cena:
   - `IActorRegistry`
   - `IWorldSpawnServiceRegistry`
   - `IWorldSpawnContext` (contendo `SceneName` + `WorldRoot`)
2. Se houver `WorldDefinition`, o bootstrapper percorre `WorldDefinition.Entries` e, para cada entry habilitada:
   - usa `WorldSpawnServiceFactory.Create(...)` para instanciar um `IWorldSpawnService`
   - registra o serviço no `IWorldSpawnServiceRegistry`
3. O `WorldLifecycleOrchestrator` (não incluído nesses anexos) consome o registry para executar **Despawn/Spawn** no reset de mundo.

**Modelos de dados e contracts (macro):**
- `WorldSpawnServiceKind` define “tipos” de serviços (ex.: `Players`, `DummyActor`).
- `WorldDefinition` fornece as entries (prefab + flags + kind) como configuração de cena.
- `IWorldSpawnService` define `SpawnAsync()` e `DespawnAsync()` (assíncronos).
- `IWorldSpawnContext` entrega dados mínimos de cena (`SceneName`, `WorldRoot`).

## 2) O que já está “completo” o suficiente para o baseline

**Arquitetura base e acoplamentos**  
- Há separação clara entre **configuração** (`WorldDefinition`), **construção** (`WorldSpawnServiceFactory`), **execução** (`IWorldSpawnService`) e **agregação** (`IWorldSpawnServiceRegistry`).
- O `IWorldSpawnContext` reduz a necessidade de serviços consultarem `SceneManager` ou dependerem de “ActiveScene” (bom para additive).

**Spawn mínimo de Player (baseline)**  
- `PlayerSpawnService` cobre o “happy path”:
  - Instancia prefab
  - Gera `actorId` via `IUniqueIdFactory` (ou fallback)
  - Inicializa e registra `PlayerActor` no `IActorRegistry`
  - Move o GameObject para a cena correta
  - Atribui parent opcional (`WorldRoot`)

**Suporte a QA / testes**  
- `DummyActorSpawnService` é útil como serviço de spawn que não “puxa” gameplay real (ajuda a isolar o pipeline).

## 3) Lacunas e riscos (o que ainda não está fechado)

### 3.1 Determinismo e ordenação (crítico para reset consistente)
Hoje, pelo contract fornecido:
- `IWorldSpawnServiceRegistry` não explicita **ordem determinística**.
- `IWorldSpawnService` não expõe **Kind** / **Order** para permitir ordenação estável por “macro fases” (ex.: Planets → Players → NPCs).

**Risco:** o `WorldLifecycleOrchestrator` pode executar spawn/despawn em ordem incidental (ordem de registro), que pode variar com mudanças no `WorldDefinition`, refactors ou diferenças de build.

**Recomendação mínima (sem grande refactor):**
- Padronizar que o orchestrator sempre ordena serviços por:
  1) `WorldSpawnServiceKind` (ordem do enum como macro)
  2) `Order` opcional (int) por serviço
  3) `Name` como tie-break
- Para viabilizar isso, vale adicionar ao contract:
  - `WorldSpawnServiceKind Kind { get; }`
  - `int Order { get; }` (default 0)

### 3.2 Registry “cego” (robustez e validação)
O registry, na forma apresentada, tende a ser um simples “bag” de serviços.
Problemas típicos se não houver guardrails:
- Registro duplicado (mesmo serviço / mesmo nome / mesmo kind)
- Serviços nulos
- Dificuldade de diagnosticar “por que meu spawn não roda”

**Recomendação:**
- `Register(IWorldSpawnService service)` deve:
  - validar `service != null`
  - impedir duplicatas (por referência e/ou por `Name`)
  - registrar logs verbosos de summary (total, por kind)

### 3.3 Factory com switch (OCP / extensão futura)
`WorldSpawnServiceFactory.Create` decide via `switch(entry.Kind)` e constrói manualmente serviços.
Isso é OK para baseline, mas:
- Cada novo Kind implica editar a factory (violando OCP).
- A factory vira “Deus” de instanciamento.

**Opções de evolução:**
- Map/Strategy por kind: `Dictionary<WorldSpawnServiceKind, IWorldSpawnServiceBuilder>`
- “Registradores” por kind no DI da cena (builders como serviços), e a factory só delega.

### 3.4 `PlayerSpawnService` concentrando responsabilidades (SRP)
O serviço hoje tende a:
- Instanciar (infra/Unity)
- Resolver ID (infra)
- Garantir componentes/contratos (`PlayerActor`)
- Registrar no `ActorRegistry` (infra)
- Definir parenting/scene move (infra/Unity)

**Risco:** ao crescer, vira ponto de acoplamento entre gameplay e infra.

**Recomendação (incremental):**
- Introduzir 1 abstração de infra para instanciamento:
  - `IWorldInstantiator` (Instantiate/Destroy/MoveToScene)
- Introduzir 1 policy para posicionamento:
  - `ISpawnPointResolver` (mesmo que inicialmente “sempre no WorldRoot”)

### 3.5 Ausência de integração “completa” com o loop de reset (observado em logs)
Nos logs do reset:
- Quando não há serviços registrados, o orchestrator dá warning e faz skip de spawn/despawn.
- Isso é bom para não bloquear o pipeline, mas pode mascarar regressões de spawn.

**Recomendação de QA:** criar um QA dedicado de spawn (independente de gameplay real) para validar:
- “registra 1+ spawn services”
- “spawn cria actors e registra no ActorRegistry”
- “despawn limpa actors do registry e destrói objetos”
- “reset completo volta para o mesmo estado (idempotência)”

## 4) Avaliação SOLID (direta)

**S — Single Responsibility**
- `WorldSpawnServiceFactory` e `PlayerSpawnService` tendem a acumular responsabilidades com a evolução.
- `WorldDefinition` (data + editor) costuma ser ok, desde que validação não vire “regra de negócio” pesada.

**O — Open/Closed**
- Factory com `switch` não escala bem. Estratégia por kind resolve.

**L — Liskov Substitution**
- `IWorldSpawnService` é simples; a substituição tende a ser segura.
- Atenção: contratos implícitos (“sempre registra no ActorRegistry”) devem ser documentados.

**I — Interface Segregation**
- `IWorldSpawnService` está mínimo (bom).
- Se surgir necessidade de hooks mais ricos, evite inflar a interface; prefira contracts opcionais (ex.: `IOrderedSpawnService`).

**D — Dependency Inversion**
- Bom uso de abstrações (registry/context/service).
- O ponto fraco é a dependência direta de `Object.Instantiate/Destroy` (inversão via `IWorldInstantiator` melhora testabilidade).

## 5) Checklist objetivo: “o que falta” para considerar spawn “funcional” como macro-estrutura

1. **Determinismo de ordem** (Kind/Order/tie-break documentado e implementado).  
2. **Registry robusto** (validação, duplicatas, diagnóstico).  
3. **QA de spawn** (isolado; não depende de WorldDefinition real).  
4. **Abstração mínima de instanciamento** (para testes/pooling no futuro).  
5. **Política de spawn position** (mesmo que trivial por enquanto).  

## 6) Proposta de próximos passos (curto prazo, baixo risco)

**Passo A — QA Spawn Baseline**
- Um QA que:
  - cria 2 serviços (`DummyActor` + `Players`) programaticamente (sem `WorldDefinition`)
  - executa `SpawnAsync` / `DespawnAsync`
  - valida contadores no `ActorRegistry` e logs
- Objetivo: provar que “spawn macro” funciona antes de conectar ao conteúdo real do jogo.

**Passo B — Determinismo (mínimo)**
- Adicionar `Kind` (e opcionalmente `Order`) no contract, ou documentar um método de ordenação no orchestrator.
- Ordenação consistente: `Kind` → `Order` → `Name`.

**Passo C — Consolidar responsabilidades**
- Introduzir `IWorldInstantiator` + `ISpawnPointResolver` (implementações default simples).
