# ADR-0011 – WorldDefinition multi-actor para GameplayScene (NewScripts)

**Status:** Implementado (baseline Player + Eater)
**Data:** 2025-12-28
**Escopo:** `GameplayScene`, `NewSceneBootstrapper`, spawn services (Player/Eater), `WorldLifecycle`

---

## 1. Contexto

Para suportar gameplay com múltiplos atores, o NewScripts precisava de um mecanismo declarativo para:

- definir quais atores são spawnados em uma cena;
- manter ordem de spawn consistente;
- evitar “spawns escondidos” em `Awake/Start` espalhados pelo projeto.

---

## 2. Decisão

Adotar um asset `WorldDefinition` referenciado pelo `NewSceneBootstrapper` da cena.

Durante o bootstrap da cena:

1. `NewSceneBootstrapper` carrega o `WorldDefinition`.
2. Para cada entry `Enabled=True`, cria/registra o spawn service correspondente no `IWorldSpawnServiceRegistry` da cena.
3. O `WorldLifecycleController` coleta os spawn services do registry e executa despawn/spawn de forma determinística via `WorldLifecycleOrchestrator`.

---

## 3. Regras (baseline)

- Entradas desabilitadas (`Enabled=False`) são ignoradas.
- A ordem do spawn é definida pelo próprio spawn service (ex.: Player ordem 1, Eater ordem 2).
- A `GameplayScene` pode ter 0 entries em cenários de menu/ready (isso é permitido).

---

## 4. Evidência

Logs observados em produção durante Menu → Gameplay:

- `WorldDefinition entries count: 2`
- `Spawn entry #0: Kind=Player ...`
- `Spawn entry #1: Kind=Eater ...`
- `Spawn service registrado: PlayerSpawnService (ordem 1)`
- `Spawn service registrado: EaterSpawnService (ordem 2)`
- `Actor spawned: ... Player ...`
- `Actor spawned: ... Eater ...`

---

## 5. Consequências

### Benefícios
- Configuração declarativa e inspecionável (asset).
- Facilita QA e debugging (comparar `WorldDefinition` com serviços registrados).
- Permite evoluir para outros tipos de ator sem alterar o fluxo base.

### Riscos
- Se o `WorldDefinition` estiver ausente na cena, nenhum spawn service será registrado (e o reset não spawnará atores).
- Requer que as fábricas/registries de spawn sejam mantidas consistentes (mapeamento Kind → Service).

---

## 6. Próximos passos

- Adicionar novos kinds de ator conforme o gameplay evoluir (NPCs, objetivos, etc.).
- Padronizar validações (ex.: warning quando `GameplayScene` tiver 0 entries, se isso for inesperado).
