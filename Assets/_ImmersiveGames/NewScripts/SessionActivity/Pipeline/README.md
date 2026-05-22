# SessionActivityPipeline - Estado Real Congelado (Base 1.1 Sandbox)

## Status
- Este modulo esta congelado como **sandbox funcional minimo Base 1.1**.
- O objetivo atual e validar trilho canonico de entrada e ciclo local minimo de activity.
- Este documento descreve o que **existe hoje** e o que **ainda nao e contrato final**.

## Entrada canonica
1. `SessionOperationalPipeline` prepara o handoff.
2. `SessionOperationalPipeline` emite `SessionActivityEntryHandoff`.
3. `SessionActivityPipeline` entra por `StartFromPreparedHandoff`.

Regras de fronteira:
- `DebugStartActivity` e apenas QA/tooling.
- `autoStart` nao e contrato de producao.
- `SessionActivityHost` nao e owner de lifecycle.
- `foreign/stale events` nao podem alterar a activity ativa.

## Ownership atual (implementado)
1. `SessionActivityPipeline` decide ciclo local de:
   - activation;
   - running;
   - pause/resume;
   - completion local (incluindo continue/handoff interno entre activities do catalogo).
2. `SimulationGate` executa block/release de simulacao.
3. `InputModeAdapter` e `PauseOverlayAdapter` executam efeitos observaveis.
4. Adapters/gates nao decidem lifecycle semantico; pipeline decide.

## Estado implementado hoje
1. Activation local existe (com possibilidade de skip/no-content).
2. Running local existe.
3. Pause/resume local existe com gate de simulacao.
4. Completion local existe (inclusive `PipelineCompleted` no fim do catalogo).
5. Deactivation local por activity existe **parcialmente**.

## Fora do contrato final (ainda nao implementado aqui)
1. `ActivitySetup` real.
2. Gameplay input final.
3. `PlayerActor` final.
4. `Activity Snapshot Provider` real.
5. `Run Pipeline` deactivation/continuity real.

## Fronteiras normativas
- Run-level deactivation/continuity **nao pertence** a `SessionActivity`; pertence ao `RunPipeline` futuro.
- `SessionActivity` nao salva progression diretamente.
- Enquanto nao existir provider real, `no_snapshot_provider` em `RouteActivitySave` e estado esperado.

## Resumo
`SessionActivity` permanece congelada como sandbox funcional minimo Base 1.1: entrada canonica por handoff, ciclo local controlado pelo pipeline e fronteiras explicitas para itens futuros que ainda nao sao contrato final.
