# ADR-0014 — Gameplay Reset: Targets e Grupos

## Status

- Estado: Implementado
- Data: 2025-12-28
- Escopo: `GameplayReset` (NewScripts), WorldLifecycle, spawn services (Player/Eater)

## Contexto

Durante o desenvolvimento foi necessário suportar resets parciais e previsíveis em gameplay, sem:

- destruir/recriar toda a cena;
- depender de objetos legados;
- introduzir resets implícitos “por acaso”.

Além do reset “hard” por WorldLifecycle, há casos de QA e debug onde precisamos resetar apenas
um subconjunto dos atores (ex.: somente Player).

## Decisão

### Objetivo de produção (sistema ideal)

Controlar quais sistemas/targets participam do reset de gameplay via grupos declarativos, garantindo resets determinísticos e evitando 'vazamentos' entre runs.

### Contrato de produção (mínimo)

- ResetWorld recebe um conjunto de targets/grupos (global vs scene, gameplay-only etc.).
- Cada target é idempotente e observa a ordem (limpeza → spawn → gates).
- Falhas de target ausente/config inconsistente são fail-fast (não inventar targets).

### Não-objetivos (resumo)

Ver seção **Fora de escopo**.

## Fora de escopo

- Implementar pooling/cleanup genérico fora do contrato de reset.

- (não informado)

## Consequências

### Benefícios
- Resets parciais com semântica explícita.
- Facilita QA (validar despawn/spawn por grupo).
- Base para evoluções: waves, checkpoints, respawn por morte, etc.

### Trade-offs / Riscos
- Se a classificação estiver incorreta, o reset pode deixar atores “órfãos” no mundo.
- Novos tipos de ator exigem atualização no classificador e/ou nos spawn services.

### Política de falhas e fallback (fail-fast)

- Em Unity, ausência de referências/configs críticas deve **falhar cedo** (erro claro) para evitar estados inválidos.
- Evitar "auto-criação em voo" (instanciar prefabs/serviços silenciosamente) em produção.
- Exceções: apenas quando houver **config explícita** de modo degradado (ex.: HUD desabilitado) e com log âncora indicando modo degradado.


### Critérios de pronto (DoD)

- ResetRequested/ResetCompleted usam reason/contextSignature padronizados.
- Evidência mostra reset determinístico e spawn pipeline executando no perfil gameplay.

## Notas de implementação

### Regras de classificação (baseline)

- A classificação é feita preferencialmente pelo **`ActorRegistry`** (determinística e rápida).
- Se não houver dados no registry, o orchestrator faz **fallback por scan de cena** (`IActor`).
- Para `PlayersOnly` e `ByActorKind`, o filtro principal é o `ActorKind`.
- Para `ActorIdSet`, a fonte de verdade é a lista `ActorIds` do request.
- Para `EaterOnly`, aplica-se `ActorKind.Eater` com fallback string-based (`EaterActor`) quando necessário.

### Integração com WorldLifecycle

- O hard reset acionado em runtime (ScenesReady) equivale semanticamente a `AllActorsInScene`.
- Targets parciais são usados principalmente para QA/debug e para futuras features (ex.: respawn individual).

## Evidência

- **Fonte canônica atual:** [`LATEST.md`](../Reports/Evidence/LATEST.md)
- **Âncoras/assinaturas relevantes:**
  - `ResetRequested ... reason='SceneFlow/ScenesReady'`
  - `ResetCompleted ...` (profile gameplay) + spawns
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
