# RUNREARM-CANON-REFACTOR

## Escopo auditado

- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/RunRearm/**`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/WorldRearm/Policies/**`
- actors/callers diretamente envolvidos em `RunRearm` dentro de `NewScripts/**`
- docs/ADRs de reset/rearm sincronizados nesta rodada

## Contrato antigo vs contrato novo

### Antes
- `RunRearmTarget` misturava:
  - `PlayersOnly`
  - `EaterOnly`
  - `AllActorsInScene`
  - `ActorIdSet`
  - `ByActorKind`
- `PlayersOnly` era o caso real de produto
- `EaterOnly` ainda carregava fallback legado por actor-kind/componente/string
- policy expunha `AllowLegacyActorKindFallback`
- o modulo permanecia `MIXED`

### Agora
- `RunRearmTarget` fica reduzido a:
  - `ByActorKind`
  - `ActorIdSet`
- `ByActorKind` vira o contrato central e canonico para grupos de atores
- `ActorIdSet` permanece como selecao tecnica/deterministica suportada
- matching por grupo depende do contrato canonico do ator (`IActorKindProvider`)
- request invalido falha cedo (`ByActorKind(Unknown)` ou `ActorIdSet` vazio)

## O que foi removido

- `PlayersOnly`
- `EaterOnly`
- `AllActorsInScene`
- fallback legado de eater por componente/string
- `AllowLegacyActorKindFallback`
- logs/degraded ligados apenas a esse fallback legado

## O que foi promovido como canonico

- `RunRearmTarget.ByActorKind`
- `RunRearmRequest.ByActorKind(...)`
- `IActorKindProvider` como contrato canonico de grupo
- `ActorIdSet` como caminho tecnico explicito para selecao deterministica por ids

## Impacto no caso atual de players

- O soft reset real de players foi preservado.
- O bridge `WorldResetScope.Players` agora chama `RunRearmRequest.ByActorKind(ActorKind.Player)`.
- `PlayerActorAdapter` passa a expor `IActorKindProvider` com `ActorKind.Player`, garantindo que players adaptados continuam selecionaveis pelo contrato canonico.

## Pendencias restantes

- Nomenclatura `RunRearm` continua historica; o contrato, porem, deixou de ser `MIXED`.
- Nao foi introduzido novo participant runtime para outros grupos alem do caso atual de players; isso fica como extensao futura do contrato canonico, nao como compat residual.
