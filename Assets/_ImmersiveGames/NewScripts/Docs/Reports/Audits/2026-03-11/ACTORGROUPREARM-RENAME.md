# ActorGroupRearm Rename Report

Data: 2026-03-11

## Escopo do rename

- Modulo runtime Gameplay RunRearm em Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/**
- Wiring de composition root, participants e consumers diretos em NewScripts/**
- ADRs e docs correntes que descrevem reset/rearm de gameplay

## Nomes antigos -> novos

- RunRearm -> ActorGroupRearm
- RunRearmTarget -> ActorGroupRearmTarget
- RunRearmRequest -> ActorGroupRearmRequest
- RunRearmContext -> ActorGroupRearmContext
- RunRearmStep -> ActorGroupRearmStep
- IRunRearmable -> IActorGroupRearmable
- IRunRearmableSync -> IActorGroupRearmableSync
- IRunRearmOrder -> IActorGroupRearmOrder
- IRunRearmTargetFilter -> IActorGroupRearmTargetFilter
- IRunRearmOrchestrator -> IActorGroupRearmOrchestrator
- IRunRearmTargetClassifier -> IActorGroupRearmTargetClassifier
- IActorDiscoveryStrategy -> IActorGroupRearmDiscoveryStrategy
- IRunRearmWorldParticipant -> IActorGroupRearmWorldParticipant
- PlayersRunRearmWorldParticipant -> PlayersActorGroupRearmWorldParticipant

## Arquivos impactados

- Runtime e interop do modulo em Modules/Gameplay/Runtime/ActorGroupRearm/**
- Composition root em Infrastructure/Composition/SceneScopeCompositionRoot.ActorGroupRearm.cs
- Consumers diretos em Modules/Gameplay/Infrastructure/** e Modules/WorldLifecycle/**
- Docs e ADRs correntes de reset/rearm em Docs/**

## Confirmacao semantica

- O rename foi de nomenclatura e coerencia; o contrato funcional ja consolidado foi preservado.
- O caso real de soft reset de players continua passando por ByActorKind(Player).
- Nao foi introduzida nova superficie funcional nesta rodada.

## Pendencias restantes

- Podem permanecer referencias historicas a RunRearm em relatorios antigos e ADRs fora da trilha corrente, preservadas como registro historico.
- Nao houve validacao manual em Play Mode nesta rodada; a confirmacao tecnica ficou baseada em wiring atualizado e gates estaticos.