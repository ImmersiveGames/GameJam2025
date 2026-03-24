# WorldLifecycle (historico)

## Estado atual

`WorldLifecycle` nao e mais o modulo ativo de reset.

A divisao atual e:

- `WorldReset` para o fluxo macro
- `SceneReset` para o fluxo local em cena
- `ResetInterop` para a ponte/superficie publica de reset

Este documento permanece apenas como ponte historica para leitura de materiais antigos, analises e ADRs que ainda usem o nome `WorldLifecycle`.

## Leitura correta hoje

- use `Docs/Modules/WorldReset.md` para o reset macro
- use `Docs/Modules/SceneReset.md` para o reset local
- use `Docs/Modules/ResetInterop.md` para a ponte com `SceneFlow`

## Leitura cruzada

- `Docs/Modules/WorldReset.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/ResetInterop.md`
