# ADR-0013 – Ciclo de Vida do Jogo (Reset por Escopos e Fases Determinísticas)

**Status:** Implementado (baseline de produção)
**Data:** 2025-12-28
**Escopo:** `WorldLifecycle` (NewScripts), `SceneFlow`, `SimulationGateService`, `GameLoop` (integração)

---

## 1. Contexto

O projeto NewScripts precisava de um mecanismo consistente para:

- Reset determinístico do “mundo” ao entrar (ou reiniciar) gameplay.
- Evitar dependência de scripts legados e reduzir condições de corrida entre:
    - carregamento de cenas;
    - inicialização de objetos;
    - habilitação de input;
    - transição visual (fade/loading).

Além do reset “hard” por `WorldLifecycle`, era necessário um contrato simples para coordenar sistemas:
- Scene Flow deve esperar o reset concluir antes do FadeOut.
- GameLoop deve iniciar gameplay apenas quando o mundo estiver pronto.

---

## 2. Decisão

Adotar um ciclo de vida do mundo baseado em:

1. **Orquestração por fases determinísticas**, centralizada em `WorldLifecycleOrchestrator`.
2. **Escopos (global vs cena)** para serviços, evitando estado “vazando” entre cenas.
3. **Driver de runtime global** (`WorldLifecycleRuntimeCoordinator`) para disparar reset após `SceneTransitionScenesReadyEvent` em perfis de gameplay.
4. **Evento de conclusão padronizado**:
    - `WorldLifecycleResetCompletedEvent(contextSignature, reason)`
5. **Integração com gating**:
    - `SimulationGateService` é adquirido durante transição e durante o reset (tokens distintos),
      e liberado apenas após conclusão.

---

## 3. Detalhes operacionais

### 3.1 Fases do reset

A sequência do reset é:

1. Acquire gate token `WorldLifecycle.WorldReset`
2. Hooks: `OnBeforeDespawn`
3. Despawn (ordem por spawn service)
4. Hooks: `OnAfterDespawn`
5. Hooks: `OnBeforeSpawn`
6. Spawn (ordem por spawn service)
7. Hooks por ator: `OnAfterActorSpawn` (quando aplicável)
8. Hooks: `OnAfterSpawn`
9. Release gate token `WorldLifecycle.WorldReset`

### 3.2 Disparo em runtime

- O runtime observa `SceneTransitionScenesReadyEvent`.
- Para gameplay, dispara hard reset com reason `'ScenesReady/<ActiveScene>'`.
- Para startup/frontend, pode ocorrer **SKIP**, mas o evento de conclusão é **sempre emitido**
  para manter o contrato com o Scene Flow e evitar deadlocks.

### 3.3 Assinatura de contexto

- O reset e o gate de Scene Flow usam uma `ContextSignature` calculada a partir do `SceneTransitionContext`.
- Isso evita “cross-talk” quando múltiplas transições ocorrem em sequência.

---

## 4. Consequências

### Benefícios
- Reset previsível e observável via logs.
- Integração robusta com Scene Flow (Fade/LoadingHUD) via completion gate.
- Bloqueio de input/movimento durante transição e reset via `SimulationGateService` + `IStateDependentService`.
- Suporte incremental a múltiplos atores por cena (via `WorldDefinition` e spawn services).

### Custos/Trade-offs
- Mais infraestrutura (orchestrator + hooks + driver + gate).
- Necessidade de disciplina para registrar hooks/serviços no escopo correto (cena vs global).
- Exige que o Scene Flow esteja corretamente configurado com o completion gate.

---

## 5. Alternativas consideradas

1. **Reset no `Start()` do `WorldLifecycleController`**
    - Rejeitado para produção: acopla o reset ao timing do Unity e dificulta testes e Scene Flow.

2. **Reset acoplado ao SceneTransitionService diretamente**
    - Rejeitado: mistura responsabilidades e torna difícil evoluir o WorldLifecycle independentemente.

3. **Manter o fluxo legado**
    - Rejeitado: acumulava acoplamentos e dificultava previsibilidade.

---

## 6. Estado atual e próximos passos

**Implementado e validado em produção** para:
- Startup → Menu (reset SKIP com evento de conclusão)
- Menu → Gameplay (hard reset após ScenesReady)
- Spawn multi-actor inicial (Player + Eater) via `WorldDefinition`

Próximos passos (documentação/arquitetura):
- Padronizar `reason` e `contextSignature` em todos os pontos que publicam `WorldLifecycleResetCompletedEvent`.
- Consolidar ownership/limpeza global vs cena (evitar serviços globais dependentes de cena).
- Expandir hooks por ator quando surgirem necessidades (UI binds, resources, etc.).
