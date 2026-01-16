# ADR-0013 — Ciclo de vida do jogo (NewScripts)

## Status
- Estado: Implementado
- Data: (não informado)
- Escopo: GameLoop + SceneFlow + WorldLifecycle (NewScripts)

## Contexto

O NewScripts precisava de um ciclo de vida de jogo consistente, que:

- Separe “transição de cenas” (SceneFlow) de “reset de mundo” (WorldLifecycle).
- Garanta ordenação determinística e rastreável (logs + signatures).
- Centralize a evolução de estados do jogo (GameLoop) sem depender de scripts QA para disparar fluxo real.

## Decisão

1. Introduzir um `GameLoop` explícito com estados:

- `Boot` (inicialização global)
- `Ready` (apto a receber comando para iniciar/continuar, mas simulação não ativa)
- `Playing` (simulação ativa)
- `Paused` (simulação travada via gate/eventos)

2. Integrar o GameLoop com SceneFlow via coordinator:

- `GameLoopSceneFlowCoordinator` reage ao evento de start (`GameStartRequestedEvent`) e dispara uma transição inicial (StartPlan).
- O coordinator espera:
    - `SceneTransitionCompletedEvent`
    - `WorldLifecycleResetCompletedEvent` (ou skip)
      para sincronizar o GameLoop com o estado correto.

3. Manter a simulação “gate-aware”:

- `GameReadinessService` usa o `ISimulationGateService` para fechar/abrir simulação durante transições.
- `IStateDependentService` bloqueia/libera ações (ex.: `Move`) com base em:
    - gate aberto/fechado
    - `gameplayReady`
    - estado do GameLoop
    - pausa

## Fora de escopo

- (não informado)

## Consequências

### Benefícios

- O fluxo de produção fica determinístico:
    - Start → Menu (`startup`) → Play → Gameplay (`gameplay`) → pós-gameplay → Restart/Exit.
- O WorldLifecycle executa reset somente quando apropriado (ex.: gameplay) e sempre sinaliza conclusão.
- O SceneFlow não conclui a transição antes do reset completar (completion gate).

### Trade-offs / Riscos

- (não informado)

## Evidências

- `Reports/Archive/2025/Report-SceneFlow-Production-Log-2025-12-31.md` (recortes do log: Boot → Ready em startup; Ready → Playing em gameplay; Paused em pós-gameplay; sincronização via SceneFlow/WorldLifecycle).

### Validação (PASS) — Item 7 (Reset fora de transição)
**Data:** 2026-01-16

**Objetivo:** confirmar que o reset “fora de SceneFlow” (produção/dev/QA) é observável e conclui via
`WorldLifecycleResetCompletedEvent`, com `contextSignature` sintético e `reason` canônico.

**Evidência observada (log):**
- **Caso A — Reset disparado em `MenuScene` (sem `WorldLifecycleController`)**
    - `Reset REQUESTED ... scene='MenuScene' reason='ProductionTrigger/qa_marco0_reset'`
    - Falha esperada: `WorldLifecycleController não encontrado na cena 'MenuScene'. Reset abortado.`
    - **Mesmo assim:** `Emitting WorldLifecycleResetCompletedEvent ... reason='Failed_NoController:MenuScene'`

- **Caso B — Reset disparado em `GameplayScene` (com `WorldLifecycleController`)**
    - `Reset REQUESTED ... scene='GameplayScene' reason='ProductionTrigger/Gameplay/HotkeyR'`
    - Pipeline completo (determinístico): `World Reset Started` → despawn → spawn (Player + Eater) → `World Reset Completed`
    - **Conclusão:** `Emitting WorldLifecycleResetCompletedEvent ... reason='ProductionTrigger/Gameplay/HotkeyR'`

- **Caso C — QA `qa_marco0_reset` em `GameplayScene`**
    - `Reset REQUESTED ... scene='GameplayScene' reason='ProductionTrigger/qa_marco0_reset'`
    - Pipeline completo + `WorldLifecycleResetCompletedEvent` emitido com reason canônico.

**Conclusão:** o contrato “ResetCompleted sempre emitido” está preservado em falha (frontend) e sucesso (gameplay),
e o ciclo de vida mantém rastreabilidade e determinismo conforme a decisão desta ADR.

## Referências

- [WORLD_LIFECYCLE.md](../WORLD_LIFECYCLE.md)
- [ARCHITECTURE.md](../ARCHITECTURE.md)
- [Observability-Contract.md](../Reports/Observability-Contract.md) — contrato canônico de reasons, campos mínimos e invariantes
