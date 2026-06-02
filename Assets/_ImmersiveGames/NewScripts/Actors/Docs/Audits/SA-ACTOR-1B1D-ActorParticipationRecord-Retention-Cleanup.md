# SA-ACTOR-1B1D — ActorParticipationRecord Retention Cleanup

## Resumo objetivo

Este corte remove `ActorParticipationRecord.RetainedForRoute` do contrato canônico de participação.

O campo era apenas semântica de lifetime escrita no inventory/feed e não possuía consumers runtime para decidir entrada, saída, retenção ou release. A decisão de retenção permanece derivada de `ActorScope` no rail correto:

- `ActivityScoped` + `ActivityExit` => `Release`
- `ActivityScoped` + `RouteExit` => `Release`
- `RouteScoped` + `ActivityExit` => `Retain`
- `RouteScoped` + `RouteExit` => `Release`

## Arquivos alterados

- [ActorModelContracts.cs](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Actors/Foundation/ActorModelContracts.cs:287)
- [PlayerActorInstanceSource.cs](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Actors/Players/ActivitySetup/PlayerActorInstanceSource.cs:107)
- [SceneAuthoredActorInstanceSource.cs](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Actors/ActivitySetup/SceneAuthoredActorInstanceSource.cs:89)
- [SA-ACTOR-1B1D-ActorParticipationRecord-Retention-Cleanup.md](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Actors/Docs/Audits/SA-ACTOR-1B1D-ActorParticipationRecord-Retention-Cleanup.md:1)

## Símbolos removidos ou reclassificados

- Removido: `ActorParticipationRecord.RetainedForRoute`
- Removido: parâmetro `retainedForRoute` do construtor de `ActorParticipationRecord`

Nenhum símbolo equivalente foi criado. O record de participação volta a representar apenas:

- identidade de participação;
- vínculo com `ActorInstanceId`;
- participação na entry corrente;
- policy de participação;
- metadados descritivos do source.

## Onde a decisão agora deriva de ActorScope

- `ActorInstanceId.FromScopedRuntimeActorIdentity(...)` continua definindo identidade por `ActorScope`.
- `ActivityEntryParticipantBindingStage` continua permitindo reuso técnico apenas quando `participant.ActorScope == ActorScope.RouteScoped`.
- `ActivityExitActorTeardownStage` continua operando release/retention por `ActorScope` e pelo rail de exit, não por record de participação.
- `ActivitySceneActorRegistry` e `ActivityPlayerActorRegistry` permanecem apenas como índices técnicos temporários para actors route-scoped retidos.

## Itens técnicos mantidos temporariamente e por quê

- `ActivityPlayerActorRegistry`
  - Mantido como índice técnico temporário de handles retidos entre activities.
- `ActivityActorExitRuntimeState` player exit bindings
  - Mantido para correlação técnica de exit entre `ActorId`, `PlayerActorId` e `PlayerSlotId`.
- Rail player-specific de exit
  - Mantido apenas para correlação técnica local. Não decide lifetime.

## Riscos

- Algum consumidor externo ao grep atual pode ter dependido implicitamente do shape antigo de `ActorParticipationRecord`.
- O conceito de retenção ainda existe como dado técnico em registries e runtime state de exit; este corte remove o contrato semântico do record, não a infraestrutura temporária.

## Smoke recomendado

Não executei build, tests, playmode, batchmode ou smoke neste corte.

Quando validar manualmente:

- `RouteScoped` continua retido entre activities.
- `RouteScoped` continua liberado no `RouteExit`.
- `ActivityScoped` continua liberado no `ActivityExit`.
- Nenhum record de participação carrega retenção futura como contrato.
- `ActivityEntryActorParticipationStage` continua registrando participação ativa sem semântica de lifetime.
- Não surgem `FATAL`, `foreign/stale` ou regressões em `ActorParticipationEntered`, `ActorParticipationExited` e `ActorPresentationReady`.
