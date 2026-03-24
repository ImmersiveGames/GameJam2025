# ResetInterop

## Estado atual

- `ResetInterop` concentra a ponte entre `SceneFlow` e o reset.
- O modulo reune driver, eventos, completion gate e tokens da superficie publica de reset.
- Os tipos de runtime ainda carregam parte do naming legado `WorldLifecycle*`, mas o papel arquitetural atual do modulo e de interop.

## Ownership

- `WorldLifecycleSceneFlowResetDriver`: handoff entre `SceneFlow` e `WorldReset`.
- `WorldLifecycleResetStartedEvent` / `WorldLifecycleResetCompletedEvent`: eventos publicos do reset.
- `WorldLifecycleResetCompletionGate`: gate de completion usado no pipeline macro.
- `WorldLifecycleTokens`: tokens publicos de reset.

## Regras praticas

- `ResetInterop` nao e owner do reset macro nem do reset local.
- O modulo existe para ponte, surface area e correlacao com `SceneFlow`.
- Nao empurre policy macro para o driver; policy continua em `WorldReset`.

## Leitura cruzada

- `Docs/Modules/WorldReset.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/SceneFlow.md`
