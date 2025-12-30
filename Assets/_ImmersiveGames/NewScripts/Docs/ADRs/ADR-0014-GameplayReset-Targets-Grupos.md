# ADR-0014 – Gameplay Reset: Targets e Grupos

**Status:** Implementado (baseline)
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
| `AllActorsInScene` | Reseta todos os atores registrados no `ActorRegistry` da cena. | Despawn+Spawn de Player e Eater (quando presentes). |
| `PlayersOnly` | Reseta apenas atores do tipo Player. | Despawn+Spawn de Player; Eater permanece. |
| `EaterOnly` | Reseta apenas atores do tipo Eater. | Despawn+Spawn de Eater; Player permanece. |

> Nota: nomes devem refletir exatamente os existentes no projeto (target enum/string usado no QA).

---

## 3. Regras de classificação (baseline)

- A classificação deve ser feita a partir do **tipo/kind** do ator (ex.: Player, Eater).
- O reset deve operar sobre:
    - `ActorRegistry` (fonte de verdade dos atores vivos);
    - spawn services responsáveis (ex.: `PlayerSpawnService`, `EaterSpawnService`).

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

O QA `GameplayReset-QA.md` documenta verificações mínimas para:
- reset completo (`AllActorsInScene`);
- reset parcial de Player (`PlayersOnly`);
- reset parcial de Eater (`EaterOnly`).
