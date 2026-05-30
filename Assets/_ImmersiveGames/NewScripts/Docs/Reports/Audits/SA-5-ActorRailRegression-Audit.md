# SA-5 — Actor Rail Regression Audit

## Status

SA-5A0 `NonPlayerActorDiscovery` como corte próprio: **REJEITADO / não aplicar**.

## Causa raiz

A premissa errada veio de três fontes combinadas:

1. O plano antigo listava `PlayerActor readiness` como subcorte antes de `Actor scan/capability discovery`.
2. O ADR 2.0 ainda mencionava `PlayerActor readiness dentro da Activity` na lista de owners do `ActivityEntryPipeline`.
3. O código atual ainda possui nomes transitórios como `NonPlayerActorDiscoveryStage`, herdados da fase anterior, e isso foi interpretado incorretamente como eixo de corte arquitetural.

## Decisão corretiva

`Actor` é a única entrada arquitetural para discovery/readiness/setup de actors.

`PlayerActor`, `NonPlayerActor`, `ObjectActor`, `EnemyActor`, `NpcActor` e outros tipos concretos podem existir como:

- especializações concretas;
- metadata;
- endpoint local;
- authoring;
- fonte transitória para feed;
- detalhe de implementação.

Eles não podem existir como:

- subcorte canônico de lifecycle;
- stage owner final;
- branch global `player/nonplayer` no pipeline;
- critério primário de ordering;
- fallback de identidade.

## Regra para próximos prompts

Qualquer prompt ou patch que proponha `NonPlayerActorDiscovery`, `PlayerActorReadiness`, `PlayerReset`, `NonPlayerReset` ou equivalente como stage/corte canônico deve ser rejeitado antes da implementação.

O próximo corte autorizado é auditoria de `ActorDiscovery` genérico e `ActorInventoryFeed`, não implementação de rail por especialização concreta.

## Arquivos corrigidos neste pacote

- `ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md`
- `Plan-2.0-SessionActivity-Refactor.md`

## Arquivos que NÃO foram alterados

Nenhum runtime code foi alterado neste pacote.

O patch SA-5A0 anterior deve ser descartado/não aplicado. Se já tiver sido aplicado, reverter:

- `SessionActivity/Pipeline/ActivityEntryPipeline.cs`
- `SessionActivity/Pipeline/SessionActivityPipeline.cs`
- `SessionActivity/Pipeline/Stages/NonPlayerActorDiscoveryStage.cs`
- `ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md`
