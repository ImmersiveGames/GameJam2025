# SceneReset

## Estado atual

- `SceneResetController` controla fila, serializacao e lifecycle do reset local.
- `SceneResetRunner` monta as dependencias efemeras do trilho local.
- `SceneResetFacade` delega para o pipeline local.
- `Runtime/SceneReset/*` concentra o pipeline deterministico por fases.

## Ownership

- `SceneResetController`: fila e lifecycle do reset local.
- `SceneResetRunner`: composicao do reset local para a cena corrente.
- `SceneResetFacade`: superficie fina sobre o pipeline local.
- `SceneResetPipeline`: execucao do pipeline por fases.
- `Phases/*`: acquire gate, hooks, despawn, scoped reset, spawn e finalizacao.
- `Hooks/*`: hooks locais do reset.
- `Spawn/*`: contratos e registry do spawn local.

## Regras praticas

- `SceneReset*` representa apenas o reset local de cena.
- O modulo nao e owner do reset macro; esse papel pertence a `WorldReset`.
- O modulo nao e owner do bridge com `SceneFlow`; esse papel pertence a `ResetInterop`.

## Leitura cruzada

- `Docs/Modules/WorldReset.md`
- `Docs/Modules/ResetInterop.md`
- `Docs/Modules/Gameplay.md`
