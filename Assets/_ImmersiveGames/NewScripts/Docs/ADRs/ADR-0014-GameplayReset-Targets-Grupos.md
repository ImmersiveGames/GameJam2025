# ADR-0014 – Gameplay Reset: Targets e Grupos

**Status:** Implementado (baseline + QA em reports)
**Data:** 2025-12-28
**Escopo:** `GameplayReset` (NewScripts), `WorldLifecycle`, spawn services (Player/Eater)

---

## 1. Contexto

Durante o desenvolvimento foi necessário suportar resets parciais e previsíveis em gameplay, sem:

- destruir/recriar toda a cena;
- depender de objetos legados;
- introduzir resets implícitos “por acaso”.

Além do reset “hard” por `WorldLifecycle`, há casos de QA e debug onde precisamos resetar apenas
um subconjunto dos atores (ex.: somente Player).

---

## 2. Decisão

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

---

## 3. Regras de classificação (baseline)

- A classificação é feita preferencialmente pelo **`ActorRegistry`** (determinística e rápida).
- Se não houver dados no registry, o orchestrator faz **fallback por scan de cena** (`IActor`).
- Para `PlayersOnly` e `ByActorKind`, o filtro principal é o `ActorKind`.
- Para `ActorIdSet`, a fonte de verdade é a lista `ActorIds` do request.
- Para `EaterOnly`, aplica-se `ActorKind.Eater` com fallback string-based (`EaterActor`) quando necessário.

---

## 4. Integração com WorldLifecycle

- O hard reset acionado em runtime (ScenesReady) equivale semanticamente a `AllActorsInScene`.
- Targets parciais são usados principalmente para QA/debug e para futuras features (ex.: respawn individual).

---

## 5. Consequências

### Benefícios
- Resets parciais com semântica explícita.
- Facilita QA (validar despawn/spawn por grupo).
- Base para evoluções: waves, checkpoints, respawn por morte, etc.

### Riscos
- Se a classificação estiver incorreta, o reset pode deixar atores “órfãos” no mundo.
- Novos tipos de ator exigem atualização no classificador e/ou nos spawn services.

---

## 6. Evidência e validação

- [QA-GameplayReset-RequestMatrix.md](../Reports/QA-GameplayReset-RequestMatrix.md): valida `AllActorsInScene`, `PlayersOnly`, `EaterOnly`, `ActorIdSet`, `ByActorKind`.
- [QA-GameplayResetKind.md](../Reports/QA-GameplayResetKind.md): valida `ByActorKind` e EaterOnly com probes.
- [QA/GameplayReset-QA.md](../QA/GameplayReset-QA.md): passos mínimos para reset completo e parcial em gameplay.
