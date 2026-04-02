# SceneReset

## Estado atual

- `ResetInterop/Bindings` concentra os entrypoints de cena do reset local.
- `SceneResetFacade` delega para o pipeline local e continua como compat historica.
- `Runtime/SceneReset/*` concentra o pipeline deterministico por fases.
- `Gameplay/Spawn/*` concentra o suporte de spawn usado pelo pipeline local.
- `SceneResetController` continua sendo o boundary local consumido por `WorldReset`.

## Ownership

- `ResetInterop/Bindings`: fila e lifecycle do reset local na borda de cena.
- `SceneResetFacade`: superficie fina sobre o pipeline local.
- `SceneResetPipeline`: execucao do pipeline por fases.
- `Phases/*`: acquire gate, hooks, despawn, scoped reset, spawn e finalizacao.
- `Hooks/*`: hooks locais do reset.
- `Gameplay/Spawn/*`: contratos e registry do spawn local.

## Regras praticas

- `SceneReset*` continua representando a implementacao concreta do reset local de cena.
- O modulo nao e owner do reset macro; esse papel pertence a `WorldReset`.
- O modulo nao e owner do bridge com `SceneFlow`; esse papel pertence a `ResetInterop`.
- O fato de `WorldReset` descobrir `SceneReset` hoje e apenas a implementacao local observada do boundary neutro; nao e a definicao canonica do boundary.

## Leitura cruzada

- `Docs/Modules/WorldReset.md`
- `Docs/Modules/ResetInterop.md`
- `Docs/Modules/Gameplay.md`
