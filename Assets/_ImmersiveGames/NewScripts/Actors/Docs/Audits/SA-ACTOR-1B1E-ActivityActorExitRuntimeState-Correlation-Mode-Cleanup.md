# SA-ACTOR-1B1E — ActivityActorExitRuntimeState Correlation Mode Cleanup

## Resumo objetivo

Este corte remove o vocabulário `retentionMode` de `ActivityActorExitRuntimeState`.

O state continua existindo apenas como store de correlação técnica para exit lookup de player bindings. Ele não decide `retain` ou `release`; esse ownership continua em `ActorScope` e no rail de exit do `SessionActivityPipeline`.

## Arquivos alterados

- [ActivityActorExitRuntimeState.cs](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/SessionActivity/Pipeline/Runtime/ActivityActorExitRuntimeState.cs:263)
- [SA-ACTOR-1B1E-ActivityActorExitRuntimeState-Correlation-Mode-Cleanup.md](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Actors/Docs/Audits/SA-ACTOR-1B1E-ActivityActorExitRuntimeState-Correlation-Mode-Cleanup.md:1)

## Símbolos e log fields removidos ou renomeados

- Renomeado localmente: `retentionMode` -> `correlationMode`
- Removido do log runtime: `retentionMode='...'`
- Adicionado no log runtime: `correlationMode='replace_from_current_activity_participation_context'`
- Adicionado no log runtime: `correlationMode='keep_existing_bindings_for_exit_lookup'`

Nenhum novo símbolo foi introduzido para expressar lifetime. O vocabulário novo descreve apenas estratégia de atualização/preservação de bindings para lookup técnico.

## Por que isso é correlação técnica e não lifetime

`ActivityActorExitRuntimeState` armazena:

- `ActorId`
- `PlayerActorId`
- `PlayerSlotId`
- `ActivityParticipantBinding`

Esse state existe para que `ActorParticipationPlayerExit` consiga resolver a correlação correta no momento do exit. Ele não classifica o actor como retido ou liberado.

A decisão canônica continua fora desse state:

- `ActivityScoped` + `ActivityExit` => `Release`
- `RouteScoped` + `ActivityExit` => `Retain`
- `RouteScoped` + `RouteExit` => `Release`

## Itens técnicos mantidos temporariamente e por quê

- `ActivityActorExitRuntimeState` player exit bindings
  - Mantidos porque o exit player-specific ainda precisa de lookup/correlação por actor.
- `ActivityPlayerActorRegistry`
  - Mantido como índice técnico temporário de handles route-scoped.
- `replace_from_current_activity_participation_context`
  - Mantido porque descreve corretamente substituição da correlação a partir do contexto corrente, sem semântica de lifetime.

## Riscos

- Ferramentas de observabilidade que procurem literalmente `retentionMode` nos logs precisarão atualizar o filtro para `correlationMode`.
- O state continua player-aware para correlação técnica; esse corte só remove a semântica errada de ownership de retention do vocabulário.

## Smoke recomendado

Não executei build, tests, playmode, batchmode ou smoke neste corte.

Quando validar manualmente:

- `RestartCurrentActivity` continua resolvendo player exit bindings corretamente.
- `Activity01ToActivity02` continua preservando actors `RouteScoped` entre activities.
- `RouteExitBackToMenu` continua liberando actors `RouteScoped` no route exit.
- Logs de `ActivityActorExitRuntimeStateActivityParticipationContextStored` não contêm `retentionMode`.
- Logs novos exibem `correlationMode` com valores de correlação técnica, sem sugerir owner de lifetime.
