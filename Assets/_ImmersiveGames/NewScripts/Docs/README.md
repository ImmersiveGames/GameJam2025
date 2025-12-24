# Documentação — NewScripts (WorldLifecycle)

Diretório oficial da documentação do **NewScripts**, organizado por **papéis claros**, **owners explícitos** e **fronteiras bem definidas** entre decisão arquitetural, operação, QA e evidências.

> **Regra central**
> Cada documento tem **um único papel** e **um único owner**.
>
> * **Decisões** vivem em ADRs
> * **Operação** vive em `WorldLifecycle/WorldLifecycle.md`
> * **Validação** vive em `QA/`
> * **Evidências** vivem em `Reports/`
>
> Quando precisar entender **como o sistema funciona em runtime**, consulte **apenas**
> `WorldLifecycle/WorldLifecycle.md`.

---

## Ordem Recomendada de Leitura (Essenciais)

> A ordem abaixo reflete **dependência conceitual**, não prioridade subjetiva.

1. **DECISIONS.md**
   Guardrails globais, política de convivência com legado e restrições arquiteturais.

2. **ARCHITECTURE.md**
   Visão **as-is** da arquitetura atual + roadmap curto (o que existe e o que está planejado).

3. **ADR/ADR.md**
   Índice e histórico dos ADRs aprovados.

4. **ADR/ADR-ciclo-de-vida-jogo.md**
   Decisão formal sobre:

    * fases do ciclo de vida,
    * escopos de reset,
    * reset-in-place,
    * relação Scene Flow × WorldLifecycle × GameLoop.

5. **WorldLifecycle/WorldLifecycle.md**
   **Contrato operacional único**:

    * pipeline determinístico,
    * gates,
    * hard reset vs soft reset,
    * troubleshooting,
    * semântica executável.

6. **QA/WorldLifecycle-Baseline-Checklist.md**
   QA **prescritivo de infraestrutura**, validado **exclusivamente por logs**, que confirma
   conformidade com o contrato operacional.

7. **QA/GameLoop-StateFlow-QA.md**
   QA **funcional de gameplay**, validando:

    * FSM do GameLoop,
    * start único (Opção B),
    * pausa, retomada e reset,
    * bloqueio de ações via `IStateDependentService`.

8. **Guides/UTILS-SYSTEMS-GUIDE.md**
   Infraestrutura transversal:
   DI, EventBus, Debug, Pooling, padrões de uso.

9. **GameLoop/GameLoop.md**
   Definição do **estado macro do jogo** (Boot / Playing / Paused),
   sinais aceitos e integração com gates e Scene Flow.

10. **ADR/ADR-0001-NewScripts-Migracao-Legado.md**
    Estratégia de migração incremental, bridges temporários e guardrails de isolamento.

11. **CHANGELOG-docs.md**
    Histórico de mudanças de documentação (governança).

---

## Papéis e Owners

| Documento                                  | Papel                                      | Owner            |
| ------------------------------------------ | ------------------------------------------ | ---------------- |
| DECISIONS.md                               | Guardrails globais e política de legado    | Arquitetura      |
| ARCHITECTURE.md                            | Arquitetura **as-is** + roadmap            | Arquitetura      |
| ADR/ADR.md                                 | Índice e histórico de ADRs                 | Arquitetura      |
| ADR/ADR-ciclo-de-vida-jogo.md              | Decisão de fases, escopos e reset-in-place | Arquitetura      |
| ADR/ADR-0001-NewScripts-Migracao-Legado.md | Migração incremental / bridges             | Arquitetura      |
| WorldLifecycle/WorldLifecycle.md           | Pipeline operacional e troubleshooting     | Operação         |
| QA/WorldLifecycle-Baseline-Checklist.md    | QA determinístico de infraestrutura        | QA               |
| QA/GameLoop-StateFlow-QA.md                | QA funcional (GameLoop + StateDependent)   | QA               |
| Guides/UTILS-SYSTEMS-GUIDE.md              | Infra transversal                          | Infra            |
| GameLoop/GameLoop.md                       | Estado global do loop e sinais             | Infra / GameLoop |
| CHANGELOG-docs.md                          | Governança e histórico                     | Arquitetura      |

---

## Relação entre QA de Infra e QA de Gameplay (Regra Importante)

Os QAs **não são redundantes** e **não se substituem**.

* **WorldLifecycle-Baseline-Checklist**

    * Valida **infraestrutura**
    * Ordem, gates, escopos, reset-in-place
    * Não valida gameplay nem FSM

* **GameLoop-StateFlow-QA**

    * Valida **comportamento funcional**
    * Estados, input, pausa, reset, player
    * Assume que o WorldLifecycle já está correto

> Infra correta **não garante** gameplay correto.
> Gameplay correto **não prova** infra correta.
> Ambos são necessários.

---

## Evidências e Relatórios

Conteúdos de auditoria, smoke tests, logs de validação e relatórios automáticos vivem em:

```
Reports/
```

Esses arquivos são:

* **fonte de evidência**
* **não são normas**
* **não substituem** ADRs, docs operacionais ou checklists

---

## Governança Documental

* **Fonte operacional única**:
  `WorldLifecycle/WorldLifecycle.md`

* **Fonte de decisões e porquês**:
  `ADR/`

* **Fonte de validação**:
  `QA/` (sempre referenciando o contrato operacional)

* **Guardrails e legado**:
  `DECISIONS.md`

* **Mudanças relevantes**:

    * Devem atualizar o documento afetado
    * Devem ser registradas em `CHANGELOG-docs.md`

---

## Atualizações (2025-12-24)

## Atualizações recentes (2025-12-24)

### SceneFlow + WorldLifecycle (produção)
- O pipeline de transição de cena emite `SceneTransitionStartedEvent` → `SceneTransitionScenesReadyEvent` → `SceneTransitionCompletedEvent`.
- `WorldLifecycleRuntimeDriver` reage a `ScenesReady` para disparar `ResetWorldAsync()` em Gameplay (e skip em `startup`/Menu).
- O `GameLoopSceneFlowCoordinator` destrava o GameLoop (`RequestStart`) somente quando `TransitionCompleted` **e** `WorldLifecycleResetCompleted` ocorreram para a mesma transição.

Veja também:
- `SceneFlow-GameLoop-Coordination.md`
- `GameLoop.md`
- `ADR-ciclo-de-vida-jogo.md`
