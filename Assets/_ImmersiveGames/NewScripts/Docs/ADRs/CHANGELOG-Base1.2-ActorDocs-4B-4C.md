# Changelog — Base 1.2 Actor Docs update após 4B/4C

## Criado

- `ADR-1.2-0008-Actor-Typing-ActorCapabilitySurface-e-ActorInventory-Convergence.md`

## Atualizado

- `ADR-1.2-0004-ActorAttributes-como-ActorCapability.md`
- `ADR-1.2-0005-SessionActivityPipeline-Decomposition-e-Capability-Stages.md`
- `ADR-1.2-0006-ActivityCapabilityPermission-e-Reacao-Local-de-Capabilities.md`
- `ADR-1.2-0007-Capability-Discovery-e-Activity-Capability-Inventory.md`
- `README.md`

## Decisões incorporadas

- `Actor` é raiz abstrata de entidade de gameplay.
- `PlayerActor` e `NonPlayerActor` são especializações de `Actor`, não lifecycles paralelos.
- `ActorCapabilitySurface` é fonte local primária de endpoints.
- Scanners de Presentation, Camera, Permission e Attributes consomem `ActorScanTarget + ActorCapabilitySurface`.
- `ActorAttributes` saiu do trilho nominal `NonPlayerActorAttribute*`.
- `ActorAttributeSetupCompleted` e `ActorAttributeReleaseCompleted` são obrigatórios para fechamento de stage.
- `NonPlayerActorParticipation` permanece transitório e será tratado em 4D.
- `ADR-2.0-0001` deve ser removido/tratado como rascunho histórico incorreto.
