# Gameplay

## Status documental

- O canon vivo do gameplay esta em `Docs/ADRs/ADR-0045-Gameplay-Runtime-Composition-Centro-Semantico-do-Gameplay.md`, `Docs/ADRs/ADR-0046-GameplaySessionFlow-como-primeiro-bloco-interno-do-Gameplay-Runtime-Composition.md`, `Docs/ADRs/ADR-0047-Gameplay-Phase-Construction-Pipeline-dentro-do-GameplaySessionFlow.md`, `Docs/ADRs/ADR-0049-Fluxo-Canonico-de-Fim-de-Run-e-PostRun.md` e `Docs/ADRs/ADR-0050-IntroStage-Canonical-Content-Presenter-Hook.md`.
- `Gameplay` ainda concentra setup de mundo, spawn, state, GameplayReset e apoio de camera.
- A camera de gameplay saiu para `Experience/GameplayCamera`.

## Estrutura atual

- `Spawn`: definicoes, contexto, registry e factories de spawn do mundo.
- `State/Core`: snapshot e contrato de estado jogavel.
- `State/RuntimeSignals`: adaptador de sinais do runtime.
- `State/Gate`: gate de execucao e logs de decisao.
- `GameplayReset/Coordination`: orchestrador do GameplayReset local.
- `GameplayReset/Policy`: policy do GameplayReset.
- `GameplayReset/Discovery`: resolucao de alvos.
- `GameplayReset/Execution`: aplicacao concreta do GameplayReset.

## Responsabilidades atuais

- `Game/Content/Definitions/Worlds/WorldDefinition.asset` e `Game/Content/Definitions/Worlds/Config/WorldDefinition` definem o authoring do conjunto de atores e o setup inicial do mundo.
- `GameplayStateGate` bloqueia e libera acoes de gameplay conforme estado e readiness.
- `ActorGroupGameplayResetOrchestrator` coordena o GameplayReset local.
- `PlayerActorGroupGameplayResetWorldParticipant` e a ponte de GameplayReset de players para `ByActorKind(Player)`.
- `Experience/GameplayCamera` resolve a camera gameplay fora do owner de gameplay.

## Dependencias e limites

- `SceneReset` e `WorldReset` continuam sendo o trilho material de reset.
- `ActorGroupGameplayReset` depende de `ActorKind` e `ActorIdSet`.
- `Game/Content/Definitions/Levels` guarda definitions/content de level; `Gameplay` nao e owner desse boundary.
- `GameplaySessionFlow` e o bloco interno que organiza a composicao da sessao e o handoff para phase.

## Fora de escopo

- Nao e owner de `SceneFlow`.
- Nao e owner de `Navigation`.
- Nao e owner de `GameLoop`.
- Nao e owner de `PostRun`.

## Limites conhecidos

- O nome `Gameplay` ainda cobre mais de uma camada.
- O GameplayReset continua sendo um caminho operacional dentro da area de gameplay.
- `WorldLifecycle` e termo historico; o runtime presente usa `WorldReset` e `SceneReset`.

## Hooks / contratos publicos

- `ActorGroupGameplayResetOrchestrator`
- `GameplayStateGate`
- `WorldDefinition`

## Regras praticas

- Prefira `ByActorKind` como trilho principal.
- Use `ActorIdSet` apenas quando a selecao tecnica explicita for necessaria.
- `GameplaySessionFlow` e a leitura canonica para composicao da sessao; `Gameplay` nao e owner de phase.

## Leitura cruzada

- `Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/WorldReset.md`
