# SA-ACTOR-1B1* Closure — ActorScope Lifetime Cleanup

## Resumo objetivo

A frente `SA-ACTOR-1B1*` está fechada.

O modelo canônico atual de lifetime de `Actor` ficou congelado assim:

- `ActorScope` é a fonte canônica de `lifetime`, `retention` e `release`.
- `PlayerActor` e `NonPlayerActor` são especializações concretas, não owners de lifetime.
- `PlayerParticipation` decide `slot`, `selection` e `participant`, não retention.
- `MaterializationPolicy` decide materialização/reuso, não lifetime.
- `ActorParticipationRecord` representa participação, não retention.
- `ActivityActorExitRuntimeState` é `correlation store` / lookup técnico.
- `ActivityPlayerActorRegistry` é índice técnico, não owner de release físico.

## Tabela canônica atual

| ActorScope | Evento | Resultado canônico | Owner |
|---|---|---|---|
| `ActivityScoped` | `ActivityExit` | `Release` | exit rail canônico |
| `ActivityScoped` | `RouteExit` | `Release` se ainda existir | exit/route closure canônico |
| `RouteScoped` | `ActivityExit` | `Retain` | `ActorScope` + orchestration |
| `RouteScoped` | `RouteExit` | `Release` | route closure canônico |
| `Unknown` | qualquer | fail-fast | guards canônicos |

## Evidência de fechamento

- `ActorMaterializationPolicyKind.RetainRouteScoped` removido em `SA-ACTOR-1B1B`.
- `PlayerActorRetentionKind` removido em `SA-ACTOR-1B1C`.
- `PlayerActorRetainedForRoute` removido em `SA-ACTOR-1B1C`.
- `ActorParticipationRecord.RetainedForRoute` removido em `SA-ACTOR-1B1D`.
- `ActivityActorExitRuntimeState.retentionMode` removido e reclassificado como `correlationMode` em `SA-ACTOR-1B1E`.
- `ActivityPlayerActorRegistry` deixou de chamar `Destroy` e virou índice técnico puro em `SA-ACTOR-1B1G`.
- `RouteExit` ordering foi preservado ao longo da frente.
- Smoke funcional passou após `SA-ACTOR-1B1G`:
  - `RestartCurrentActivity PASS`
  - `Activity01ToActivity02 PASS`
  - `RouteExitBackToMenu PASS`

## Decisões congeladas

- `ActorScope` continua sendo a única fonte normativa para decidir retenção entre activities e release terminal.
- `PlayerParticipation` não pode reintroduzir semântica de retention por contract, fact, enum, state ou adapter.
- `ActivityActorExitRuntimeState` pode continuar player-aware apenas para correlação técnica de exit.
- `ActivityPlayerActorRegistry` pode continuar existindo apenas como índice técnico de lookup.
- `SessionScoped` não deve ser criado antes de existir identity, runtime store e root de lifecycle explícitos.

## Débitos restantes

- `ReleaseIndexedRouteScopedPlayerActors` em [SessionActivityPipeline.cs](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs:3554) e [ActivityHandoffRuntimeResetStage.cs](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/SessionActivity/Pipeline/Stages/ActivityHandoffRuntimeResetStage.cs:83) deve ser auditado futuramente para confirmar que não virou responsabilidade macro indevida do pipeline.
- `ActivityPlayerActorRegistry` continua temporário como índice técnico e pode ser reavaliado quando houver desenho explícito para `ActorLifetimePolicy` ou eventual `SessionScoped`.
- `ActivityActorExitRuntimeState` continua player-aware para correlação técnica de exit; aceitável por enquanto.
- Ainda não existe base suficiente para abrir `SessionScoped`.

## Próxima direção

- Fechar `SA-ACTOR-1B1*`.
- Retomar o plano macro de decomposição de `SessionActivity`.
- Não avançar para `SessionScoped` ainda.
