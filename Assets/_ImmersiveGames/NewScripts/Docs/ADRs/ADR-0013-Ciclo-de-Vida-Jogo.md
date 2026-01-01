# ADR-0013 — Ciclo de vida do jogo (NewScripts)

## Status
Aceito e implementado (baseline operacional validada em log).

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

## Consequências

- O fluxo de produção fica determinístico:
  - Start → Menu (`startup`) → Play → Gameplay (`gameplay`) → pós-gameplay → Restart/Exit.
- O WorldLifecycle executa reset somente quando apropriado (ex.: gameplay) e sempre sinaliza conclusão.
- O SceneFlow não conclui a transição antes do reset completar (completion gate).

## Evidência

- `Docs/Reports/Report-SceneFlow-Production-Log-2025-12-31.md` (recortes do log: Boot → Ready em startup; Ready → Playing em gameplay; Paused em pós-gameplay; sincronização via SceneFlow/WorldLifecycle).
