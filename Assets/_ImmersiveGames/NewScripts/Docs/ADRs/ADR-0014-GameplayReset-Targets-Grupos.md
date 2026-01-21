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

Introduzir um classificador de alvo de reset (`IGameplayResetTargetClassifier`) que define **targets** suportados.

No baseline atual, os targets são:

| Target | Descrição | Impacto esperado |
|---|---|---|
| `AllActorsInScene` | Reseta todos os atores registrados no `ActorRegistry` da cena (com fallback por scan). | Despawn+Spawn de Player e Eater (quando presentes). |
| `PlayersOnly` | Reseta apenas atores do tipo Player. | Despawn+Spawn de Player; Eater permanece. |
| `EaterOnly` | Reseta apenas atores do tipo Eater. | Despawn+Spawn de Eater; Player permanece. |
| `ActorIdSet` | Reseta um subconjunto explícito por `ActorIds`. | Despawn+Spawn somente dos IDs informados. |
| `ByActorKind` | Reseta por `ActorKind` arbitrário (ex.: Dummy). | Despawn+Spawn apenas do kind requisitado. |

> Nota: nomes devem refletir exatamente os existentes no projeto (enum `GameplayResetTarget`).

## Fora de escopo

- (não informado)

## Consequências

### Benefícios
- Resets parciais com semântica explícita.
- Facilita QA (validar despawn/spawn por grupo).
- Base para evoluções: waves, checkpoints, respawn por morte, etc.

### Trade-offs / Riscos
- Se a classificação estiver incorreta, o reset pode deixar atores “órfãos” no mundo.
- Novos tipos de ator exigem atualização no classificador e/ou nos spawn services.

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

## Evidências

- Metodologia: [`Reports/Evidence/README.md`](../Reports/Evidence/README.md)
- Evidência canônica (LATEST): [`Reports/Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
- Snapshot  (2026-01-17): [`Baseline-2.1-Evidence-2026-01-17.md`](../Reports/Evidence/2026-01-17/Baseline-2.1-Evidence-2026-01-17.md)
- Contrato: [`Observability-Contract.md`](../Reports/Observability-Contract.md)

## Referências

- [WORLD_LIFECYCLE.md](../WORLD_LIFECYCLE.md)
