# Base 1.2 — Actors Convergence 4A–4D + H4D Hygiene Checkpoint

Status: CLOSED / PASS

## Escopo fechado

- Actor Typing Foundation
- ActorCapabilitySurface
- Capability Discovery via ActorScanTarget
- ActorPresentation genérico
- ActorAttributes genérico
- ActorParticipation genérico
- Higiene pós-4D

## Smoke final

O smoke final validou:

- RestartCurrentActivity
- Activity01ToActivity02
- RouteExitBackToMenu
- PresentationEndpoint 3/3/2
- AttributeEndpoint 2/2/1
- CameraTarget 1/1/1
- PermissionTarget 1/1/1
- ActorPresentationSetupCompleted
- ActorAttributeSetupCompleted
- ActorParticipationEntered
- ActorReady
- ActorParticipationExited
- ActorParticipationEnterCompleted
- ActorParticipationExitCompleted

Sem regressões críticas:

- FATAL: 0
- Exception: 0
- error CS: 0
- capability_kind_unsupported: 0
- required_presentation_not_ready: 0

## Decisão

A convergência de atores 4A–4D está fechada como checkpoint funcional e arquitetural suficiente para seguir.

## Débitos

Não bloqueantes:

- PlayerActor ainda aparece como `transitional_non_registry_actor` na participation formal.
- Registries transitórios ainda existem.
- Camera/Permission/Movement ainda carregam `playerActorId/playerSlotId`.
- Discovery de NonPlayer ainda é feed transitório.

Esses itens pertencem às próximas fases, não ao checkpoint atual.
