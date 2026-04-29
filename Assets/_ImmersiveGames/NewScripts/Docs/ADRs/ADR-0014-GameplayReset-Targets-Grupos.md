# ADR-0014 - GameplayReset: grupos canônicos de atores

> STATUS NORMATIVO: HISTORICO - NAO NORMATIVO PARA DECISOES DE OWNERSHIP DA BASE 1.0.
> Em conflito, prevalecem ADR-0057, ADR-0056, ADR-0055, ADR-0058, ADR-0054 e ADR-0052.

## Status

- Estado: Implementado
- Data (decisão): 2026-02-01
- Última atualização: 2026-03-25
- Tipo: Implementação
- Escopo:
    - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/GameplayReset/Core/*`
    - `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/GameplayReset/Integration/*`
    - `Assets/_ImmersiveGames/NewScripts/Modules/WorldReset/Policies/*`

## Contexto

O fluxo de `ResetWorld` (WorldReset + SceneReset) precisa executar um **reset local de gameplay** de forma:

- **determinística** (mesmas regras e ordem entre execuções)
- **auditável** (fica claro quem foi rearmado e por que)
- **com contrato explícito** (sem targets especiais herdados de compat)

A versão anterior de `ActorGroupGameplayReset` misturava um contrato geral (`ByActorKind`) com superfícies especiais (`PlayersOnly`, `EaterOnly`, `AllActorsInScene`) e um fallback legado para inferir `Eater` por componente/string. Isso deixava o subsistema parcialmente canônico, mas ainda `MIXED`.

## Decisao

`ActorGroupGameplayReset` passa a ser o mecanismo canonico de **soft reset local por grupo de atores**.

A selecao de targets fica reduzida a dois contratos publicos e simetricos:

1. `ByActorKind`
    - caminho principal e canonico para grupos de atores
    - depende do contrato canônico do ator (`IActorKindProvider`)
2. `ActorIdSet`
    - mantido como contrato canonico suportado para selecao tecnica/deterministica por ids

Foram removidos da superficie publica:

- `PlayersOnly`
- `EaterOnly`
- `AllActorsInScene`

O caso real de produto que hoje rearmava players continua existindo, mas agora como simples instancia de:

- `ActorGroupGameplayResetRequest.ByActorKind(ActorKind.Player, reason)`

## Contratos

Os contratos ficam em `ActorGroupGameplayResetContracts.cs`:

- `ActorGroupGameplayResetStep` (enum)
- `ActorGroupGameplayResetTarget` com apenas:
    - `ByActorKind`
    - `ActorIdSet`
- `ActorGroupGameplayResetRequest` (target + reason + actorIds/kind)
- `ActorGroupGameplayResetContext` (contexto do reset)
- `IActorGroupGameplayResettable` / `IActorGroupGameplayResettableSync`
- `IActorGroupGameplayResetOrder` / `IActorGroupGameplayResetTargetFilter`
- `IActorGroupGameplayResetTargetClassifier`
- `IActorGroupGameplayResetOrchestrator`

## Regras e invariantes

### 1) Selecao por grupo usa contrato canonico do ator

- `ByActorKind` seleciona atores somente por `IActorKindProvider`
- nao existe inferencia residual por nome, componente ou string para decidir o grupo
- se um ator precisa participar de um grupo e nao expoe `IActorKindProvider`, ele esta fora do contrato canonico desse grupo

### 2) Ordem deterministica

No `ActorGroupGameplayResetOrchestrator`:

- os targets sao ordenados por `ActorId`
- para cada target, os componentes sao ordenados por `IActorGroupGameplayResetOrder` e nome do tipo
- as etapas sao executadas em ordem fixa: `Cleanup -> Restore -> Rebind`

### 3) Fail-fast do contrato

- `ByActorKind` com `ActorKind.Unknown` falha explicitamente
- `ActorIdSet` sem ids falha explicitamente
- target sem match:
    - `Strict`: falha com `STRICT_VIOLATION`
    - `Release`: degrada com log explicito

### 4) Discovery

- discovery principal: `IActorRegistry + IActorGroupGameplayResetTargetClassifier`
- scene scan continua apenas como caminho opt-in de policy para recuperacao operacional em Strict/QA
- o scene scan respeita o mesmo contrato canonico de selecao (`ByActorKind` ou `ActorIdSet`)
- nao existe mais scene scan com heuristica especial para `Eater`

## Implementacao

### Implementacao default (sem assets dedicados)

- `ActorGroupGameplayResetDefaultTargetClassifier.cs`
    - resolve somente `ByActorKind` e `ActorIdSet`
- `ActorKindMatching.cs`
    - faz matching apenas por `IActorKindProvider`
- `ActorGroupGameplayResetOrchestrator.cs`
    - valida request
    - resolve atores por registry-first
    - executa `Cleanup -> Restore -> Rebind`
- `PlayerActorGroupGameplayResetWorldParticipant.cs`
    - ponte do `WorldResetScope.Players` para `ByActorKind(Player)`

### Bridge do caso atual de produto

O soft reset real de players continua ativo via WorldReset/SceneReset:

- `WorldResetScope.Players` -> `PlayersActorGroupGameplayResetWorldParticipant` -> `ActorGroupGameplayResetRequest.ByActorKind(ActorKind.Player)`

### Contrato canônico do ator de player

Para preservar o comportamento real do produto, `PlayerActor` expõe `IActorKindProvider` com `ActorKind.Player`.
Isso elimina a necessidade de superficie especial para players e mantem o bridge funcionando mesmo quando o ator de player vier pelo actor canônico.

## Remocoes explicitas

Removidos do codigo ativo:

- fallback legado de `Eater` por componente/string
- flag de policy `AllowLegacyActorKindFallback`
- logs/degraded relacionados exclusivamente a esse fallback
- targets especiais `PlayersOnly`, `EaterOnly` e `AllActorsInScene`

## Consequencias

### Beneficios

- contrato menor, explicito e simetrico
- `ByActorKind` vira o centro do reset por grupo
- caso real de players passa a ser apenas um uso do contrato geral
- o modulo deixa de ser `MIXED`

### Trade-offs

- atores que precisam participar por grupo devem expor `IActorKindProvider`
- requests invalidos agora falham cedo em vez de depender de superfice especial ou heuristica residual

## Touchpoints

Quando editar o reset de gameplay, revisar tambem:

- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.ActorGroupGameplayReset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/GameplayReset/Core/ActorGroupGameplayResetContracts.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/ActorGroupGameplayReset/Core/ActorGroupGameplayResetDefaultTargetClassifier.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/GameplayReset/Core/ActorGroupGameplayResetOrchestrator.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/ActorGroupGameplayReset/Interop/PlayerActorGroupGameplayResetWorldParticipant.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldReset/Policies/ProductionWorldResetPolicy.cs`

## Implementacao (arquivos impactados)

### Runtime

- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/GameplayReset/Core/ActorGroupGameplayResetContracts.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/ActorGroupGameplayReset/Core/ActorGroupGameplayResetDefaultTargetClassifier.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/GameplayReset/Core/ActorGroupGameplayResetOrchestrator.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/GameplayReset/Core/ActorKindMatchRules.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/GameplayReset/Strategy/ActorGroupGameplayResetSceneScanDiscoveryStrategy.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/ActorGroupGameplayReset/Interop/PlayerActorGroupGameplayResetWorldParticipant.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Infrastructure/Actors/Bindings/Player/PlayerActor.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldReset/Policies/IWorldResetPolicy.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldReset/Policies/ProductionWorldResetPolicy.cs`

### Docs

- `Assets/_ImmersiveGames/NewScripts/Docs/ADRs/ADR-0014-GameplayReset-Targets-Grupos.md`
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/LATEST.md`

