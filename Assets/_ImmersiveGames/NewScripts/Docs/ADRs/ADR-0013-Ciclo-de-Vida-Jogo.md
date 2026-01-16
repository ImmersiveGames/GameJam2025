# ADR-0013 — Ciclo de vida do jogo (NewScripts)

## Status
- Estado: Implementado
- Data: 2025-12-24
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

- Metodologia: [`Reports/Evidence/README.md`](../Reports/Evidence/README.md)
- Evidência canônica (LATEST): [`Reports/Evidence/LATEST.md`](../Reports/Evidence/LATEST.md)
- Snapshot arquivado (2026-01-16): [`Baseline-2.1-ContractEvidence-2026-01-16.md`](../Reports/Evidence/2026-01-16/Baseline-2.1-ContractEvidence-2026-01-16.md)
- Contrato: [`Observability-Contract.md`](../Reports/Observability-Contract.md)

## Referências

- [WORLD_LIFECYCLE.md](../WORLD_LIFECYCLE.md)
- [ARCHITECTURE.md](../ARCHITECTURE.md)
- [Observability-Contract.md](../Reports/Observability-Contract.md) — contrato canônico de reasons, campos mínimos e invariantes
