# CHANGELOG — Base 1.2 Actors Convergence 4D + H4D

## Status

CLOSED / PASS

## Fechamentos

### 4D — ActorParticipation genérico

- Caminho nominal `NonPlayerActorParticipation*` removido.
- Participation passa a emitir eventos genéricos:
  - `ActorParticipationEntered`;
  - `ActorReady`;
  - `ActorParticipationExited`;
  - `ActorParticipationEnterCompleted`;
  - `ActorParticipationExitCompleted`.

### H4D-B1

- Removido `NonPlayerActorParticipationStage.cs`.
- Removidos outcomes/result structs legados de NonPlayerActorParticipation.
- Busca final sem referências.

### H4D-B2

- Removido fallback para `target.ActorId` em Camera/Permission identity.
- `PlayerActorIdentity` passa a ser obrigatório para endpoints player-specific.
- Missing identity fica observável por evento/unresolved.
- Smoke válido sem `CameraTargetIdentityUnresolved`/`PermissionTargetIdentityUnresolved`.

### H4D-B3

- Store ativo de ActorAttributes migrado para `ActorInstanceId`.
- Removido acoplamento nominal `_activeActorAttributeCapabilitiesByNonPlayerActorId`.
- Readiness de participation usa chave genérica.

### H4D-B4

- `ResolveOwnerKind` e `ResolveActorKindLabel` classificados como metadata/log.
- `ActivityCapabilityKind.Custom` no validator classificado como warning passivo, sem ação imediata.

---

## Evidência de smoke

Critérios confirmados:

- `RestartCurrentActivity PASS`;
- `Activity01ToActivity02 PASS`;
- `RouteExitBackToMenu PASS`;
- `PresentationEndpoint 3/3/2`;
- `AttributeEndpoint 2/2/1`;
- `CameraTarget 1/1/1`;
- `PermissionTarget 1/1/1`;
- `ActorPresentationSetupCompleted`;
- `ActorAttributeSetupCompleted`;
- `ActorParticipationEntered`;
- `ActorReady`;
- `ActorParticipationExited`;
- `ActorParticipationEnterCompleted`;
- `ActorParticipationExitCompleted`;
- sem `NonPlayerActorParticipation*`;
- sem `NonPlayerActorAttribute*`;
- sem `NonPlayerActorPresentation*`;
- sem `capability_kind_unsupported`;
- sem `required_presentation_not_ready`;
- sem `FATAL`, `Exception`, `error CS`.

---

## Débitos movidos para próxima fase

- `transitional_non_registry_actor` para PlayerActor.
- `ActivityPlayerActorRegistry` / `ActivityNonPlayerActorRegistry` transitórios.
- `NonPlayerActorDiscovery` como feed transitório.
- `playerActorId/playerSlotId` em Camera/Permission/Movement.
- revisão final de diagnostics e nomes legados.
