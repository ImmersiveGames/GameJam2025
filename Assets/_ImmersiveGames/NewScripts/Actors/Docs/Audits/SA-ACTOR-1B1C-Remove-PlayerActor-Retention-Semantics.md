# SA-ACTOR-1B1C — Remove PlayerActor retention semantics

Status: IMPLEMENTED / DOCUMENTED / awaiting compile + smoke

## Resumo objetivo

Este corte remove a semântica player-specific de retention/lifetime do runtime de actors.

Remoções principais:

- `PlayerActorRetentionKind`;
- `PlayerActorRetainedForRoute`;
- estado de retention em `PlayerActorParticipationState`;
- records de participação de player que exigiam `RetainedForRoute=true` para serem válidos.

Após o patch:

- `ActorScope` continua sendo a única fonte canônica de lifetime;
- `PlayerActorParticipationState` guarda apenas estado de participação/correlação local;
- `PlayerActorParticipationAdapter` não decide mais retention;
- `ActivityExitActorTeardownStage` não emite mais fact player-specific de retention.

## Arquivos alterados

- `Actors/Players/Runtime/PlayerActorParticipationState.cs`
- `Actors/Players/ActivitySetup/PlayerActorParticipationAdapter.cs`
- `Actors/Players/ActivitySetup/PlayerActorSetupContracts.cs`
- `SessionActivity/Pipeline/Stages/ActivityExitActorTeardownStage.cs`
- `SessionActivity/Contracts/SessionActivityContracts.cs`
- `Actors/Docs/Audits/SA-ACTOR-1B1C-Remove-PlayerActor-Retention-Semantics.md`

## Símbolos removidos

- `PlayerActorRetentionKind`
- `PlayerActorRetainedForRoute`
- `PlayerActorRetainedForRouteFound`
- `PlayerActorParticipationState.Retention`
- `PlayerActorParticipationState.MarkExitedActivityRetainedForRoute()`

Também foi removida a exigência de `RetainedForRoute` dos contracts:

- `PlayerActorParticipationExitRecord`
- `PlayerActorParticipationEnterRecord`

## O que agora deriva de ActorScope

### Retenção entre activities

Não é mais expressa por:

- enum de player;
- fact player-specific;
- state local do `PlayerActor`.

Ela permanece consequência de:

- `ActorScope.RouteScoped`;
- registry técnico temporário que mantém handle reutilizável;
- eventos de exit/route-exit já existentes no pipeline.

### Release em ActivityExit / RouteExit

Continua pertencendo ao orchestration do pipeline:

- `ActivityExitActorTeardownStage`;
- `SessionActivityPipeline`;
- trilhos de exit/release já existentes.

`PlayerActorParticipationAdapter` não registra mais retention como decisão semântica.

## Itens mantidos temporariamente e por quê

### `ActivityPlayerActorRegistry`

Mantido porque ainda é o índice técnico de handles player materializados/retidos.

Neste corte ele não foi removido nem expandido.
O objetivo foi só retirar ownership semântico de lifetime do lado player-specific.

### `ActivityActorExitRuntimeState` player exit bindings

Mantido porque o exit player-specific ainda precisa correlacionar:

- actor exiting;
- `ActivityParticipantBinding`;
- `PlayerActorId` / `PlayerSlotId`.

Esse state continua permitido apenas como correlação técnica.

### `ActorParticipationRecord.RetainedForRoute`

Mantido porque ainda pode ser necessário como dado derivado técnico no inventory/feed.

Ele não foi promovido neste corte a owner de lifetime.

## Riscos

### Risco 1

Se algum código externo dependia de `PlayerActorParticipationState.Retention`, o compile apontará isso.

### Risco 2

Os records `PlayerActorParticipationExitRecord` e `PlayerActorParticipationEnterRecord` deixaram de carregar semântica de retention.
Se algum consumidor tratava esse bool como contrato obrigatório, o compile ou comportamento observável vai expor a dependência.

### Risco 3

O trilho player-specific de exit ainda existe para correlação técnica.
Este corte não colapsa ainda esse trilho para um fluxo puramente genérico de actor lifetime.

## Smoke recomendado

Não executar neste corte.

Quando validar:

- `RouteScoped` continua retido entre activities;
- `RouteScoped` continua liberado no `RouteExit`;
- `ActivityScoped` continua liberado no `ActivityExit`;
- não existe emissão runtime de `PlayerActorRetainedForRoute`;
- `PlayerActorParticipationState` continua refletindo entrada/saída da activity sem semântica de retention;
- não existe regressão em `ActorParticipationEntered`, `ActorParticipationExited`, `ActorPresentationReady` e retorno de rota;
- não existe `FATAL`, `foreign/stale` ou regressão de ordering em `RouteExit`.

## Ownership corrigido

- lifetime/retention: continua em `ActorScope` + orchestration do pipeline;
- participação local do player: permanece em `PlayerActorParticipationState`, mas agora sem decidir lifetime;
- correlação técnica: permanece temporariamente em `ActivityPlayerActorRegistry` e `ActivityActorExitRuntimeState`.
