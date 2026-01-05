# Próximos passos — Plano atualizado (2026-01-05)

**Estado atual:** Baseline 2.0 aprovado via log; contrato documentado em `WORLD_LIFECYCLE.md` e `CHANGELOG-docs.md`.

---

## P0 — Remover risco de deadlock por assinatura (alto valor / baixo churn)

### 0.1 Normalizar uso de `ContextSignature` (fonte de verdade)
**Problema típico:** diferentes pontos do pipeline recalculam assinatura e podem divergir (mesmo que “raramente”), quebrando correlação do completion gate.

**Ação:**
- Substituir `SceneTransitionSignatureUtil.Compute(context)` por `context.ContextSignature` em:
  - `SceneTransitionService` (logs + await completion gate)
  - `WorldLifecycleResetCompletionGate`
  - `WorldLifecycleRuntimeCoordinator`
  - `GameLoopSceneFlowCoordinator`

**Critério de aceite (log):**
- Todas as mensagens que referenciam “signature” usam `ContextSignature` idêntico ao do evento.
- Completion gate resolve em todas as rotas (sem timeout).

> Entrega sugerida nesta etapa: aplicar patch de assinatura (arquivos completos já preparados).

---

## P1 — GameplayScene: desbloquear fluxo “produção” (alto valor / médio risco)

### 1.1 Atacar os 3 blockers críticos (ordem recomendada)
1) **Exceção fatal** durante bootstrap/load (interrompe ScenesReady→Completed).
2) **DI registration missing** de serviço essencial (quebra runtime coordinator / game loop).
3) **Null refs** por dependência de objeto de cena inexistente (prefab/cena).

**Critério de aceite:**
- Menu → Gameplay completa sem erro crítico.
- Baseline B permanece PASS e passa a ser executável “sem QA”.

---

## P2 — Hardening leve do Baseline (médio valor / baixo churn)

### 2.1 Asserts e logs de contrato (dev-only)
- Assert: `ScenesReady` sempre antes de `Completed`.
- Assert: `ResetCompleted` sempre emitido (inclusive SKIP).
- Log estruturado do gate: tokens ativos no Started/Completed.

**Objetivo:** tornar regressões detectáveis sem depender do parser.

---

## Próximo passo imediato (recomendado)

Aplicar **P0.1** (normalização de `ContextSignature`) agora, pois:
- é alteração pequena,
- reduz risco de regressão silenciosa,
- melhora previsibilidade do completion gate.

Em seguida, partir para **P1.1** (GameplayScene blockers).
