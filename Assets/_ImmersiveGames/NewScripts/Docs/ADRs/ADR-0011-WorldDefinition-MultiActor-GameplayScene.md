# ADR-0011 — WorldDefinition multi-actor para GameplayScene (NewScripts)

## Status

- Estado: Implementado
- Data: 2025-12-28
- Escopo: `GameplayScene`, `NewSceneBootstrapper`, spawn services (Player/Eater), WorldLifecycle

## Contexto

Para suportar gameplay com múltiplos atores, o NewScripts precisava de um mecanismo declarativo para:

- definir quais atores são spawnados em uma cena;
- manter ordem de spawn consistente;
- evitar “spawns escondidos” em `Awake/Start` espalhados pelo projeto.

## Decisão

### Objetivo de produção (sistema ideal)

Definir um 'WorldDefinition' capaz de montar um GameplayScene com múltiplos atores (ex.: Player + Eater) de forma determinística, alinhada ao reset/spawn pipeline do WorldLifecycle.

### Contrato de produção (mínimo)

- WorldDefinition descreve **o que** instanciar e **onde** (configs/prefabs/ids), sem side-effects fora do pipeline.
- Spawn de atores ocorre durante ResetWorld/WorldLifecycle (ou fase equivalente) e registra atores no ActorRegistry.
- Falhas de referência (prefab/config ausente) são fail-fast (não auto-criar).
- A lista mínima de atores é verificável via log anchors.

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

## Fora de escopo

- Balanceamento/IA dos atores (fora do escopo do contrato de montagem).
- Spawns ad-hoc em runtime fora do pipeline de reset.

- Adição de novos kinds de ator além de Player/Eater (ver notas de evolução).

## Consequências

### Benefícios
- Configuração declarativa e inspecionável (asset).
- Facilita QA e debugging (comparar `WorldDefinition` com serviços registrados).
- Permite evoluir para outros tipos de ator sem alterar o fluxo base.

### Trade-offs / Riscos
- Se o `WorldDefinition` estiver ausente na cena, nenhum spawn service será registrado (e o reset não spawnará atores).
- Requer que as fábricas/registries de spawn sejam mantidas consistentes (mapeamento Kind → Service).

### Política de falhas e fallback (fail-fast)

- Em Unity, ausência de referências/configs críticas deve **falhar cedo** (erro claro) para evitar estados inválidos.
- Evitar "auto-criação em voo" (instanciar prefabs/serviços silenciosamente) em produção.
- Exceções: apenas quando houver **config explícita** de modo degradado (ex.: HUD desabilitado) e com log âncora indicando modo degradado.


### Critérios de pronto (DoD)

- GameplayScene, após reset, registra o conjunto mínimo de atores esperado.
- Logs confirmam contagem/registro (ActorRegistry=2 no baseline).

## Notas de implementação

### Regras (baseline)

- Entradas desabilitadas (`Enabled=False`) são ignoradas.
- A ordem do spawn é definida pelo próprio spawn service (ex.: Player ordem 1, Eater ordem 2).
- A `GameplayScene` pode ter 0 entries em cenários de menu/ready (isso é permitido).

### Próximos passos

- Adicionar novos kinds de ator conforme o gameplay evoluir (NPCs, objetivos, etc.).
- Padronizar validações (ex.: warning quando `GameplayScene` tiver 0 entries, se isso for inesperado).

## Evidência

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Âncoras/assinaturas relevantes:**
  - [WorldLifecycle] ResetCompleted ... (ver evidência canônica).
  - Spawns Player + Eater; `ActorRegistry=2`.
- **Contrato de observabilidade:** [`Observability-Contract.md`](../Reports/Observability-Contract.md)

## Evidências

- Metodologia: [`Reports/Evidence/README.md`](../Reports/Evidence/README.md)
- Evidência canônica (LATEST): [`Reports/Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
- Snapshot  (2026-01-17): [`Baseline-2.1-Evidence-2026-01-17.md`](../Reports/Evidence/2026-01-17/Baseline-2.1-Evidence-2026-01-17.md)
- Contrato: [`Observability-Contract.md`](../Reports/Observability-Contract.md)

## Referências

- [WORLD_LIFECYCLE.md](../WORLD_LIFECYCLE.md)
- [`Observability-Contract.md`](../Reports/Observability-Contract.md)
- [`Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
