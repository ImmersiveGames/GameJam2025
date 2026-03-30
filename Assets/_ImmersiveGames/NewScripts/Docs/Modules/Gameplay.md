# Gameplay

## Status documental

- Parcial / leitura junto do runtime atual.
- `Gameplay` continua concentrando mais de uma responsabilidade: setup de mundo, spawn, gate de ação, rearm e apoio de câmera.

## Objetivo

- Descrever o runtime de gameplay atual como ele é hoje, sem fingir um domínio puro.
- Manter rastreável o que é entidade, o que é mecânica e o que ainda é orquestração local.

## Responsabilidades atuais

- `WorldDefinition` e o runtime de spawn definem o conjunto de atores e o setup inicial do mundo.
- `GameplayStateGate` bloqueia e libera ações de gameplay conforme estado e readiness.
- `ActorGroupRearmOrchestrator` executa o rearm local de grupos de atores.
- `PlayerActorGroupRearmWorldParticipant` é a ponte do reset de players para `ByActorKind(Player)`.
- `GameplayCameraResolver` atende consumers globais quando a câmera gameplay precisa ser resolvida.

## Dependências e acoplamentos atuais

- `SceneReset` e `WorldReset` continuam sendo o trilho material de reset.
- `ActorGroupRearm` depende de `ActorKind` e `ActorIdSet`.
- `Gameplay` ainda mistura entidade, mecânica e orquestração local em vez de separá-las completamente.

## Fora de escopo

- Não é owner de `SceneFlow`.
- Não é owner de `Navigation`.
- Não é owner de `GameLoop`.
- Não é owner de `PostGame`.

## Limites conhecidos / dívida atual

- O nome `Gameplay` ainda cobre mais de uma camada.
- O rearm continua sendo um caminho operacional dentro da área de gameplay.
- A câmera gameplay ainda aparece como suporte para consumers globais.
- `WorldLifecycle` é termo histórico; o runtime presente usa `WorldReset` e `SceneReset`.

## Hooks / contratos públicos

- `ActorGroupRearmOrchestrator`
- `GameplayStateGate`
- `WorldDefinition`

## Regras práticas

- Prefira `ByActorKind` como trilho principal.
- Use `ActorIdSet` apenas quando a seleção técnica explícita for necessária.
- Continue nomeando esse fluxo como `ActorGroupRearm`.

## Leitura cruzada

- `Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/WorldReset.md`
