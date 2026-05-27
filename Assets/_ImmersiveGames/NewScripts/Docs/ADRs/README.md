# ADRs — Base 1.2

Este diretório contém os ADRs vivos da Base 1.2 — Actors Convergence / Convergência de Atores.

Fonte normativa anterior: Base 1.1 já concluída.  
Base 1.2 não substitui a Base 1.1; ela adapta atores ao shape da Base 1.1.

---

## ADRs ativos

- ADR-1.2-0004 — ActorAttributes como ActorCapability
- ADR-1.2-0005 — SessionActivityPipeline Decomposition e Capability Stages
- ADR-1.2-0006 — ActivityCapabilityPermission e Reação Local de Capabilities
- ADR-1.2-0007 — Capability Discovery e Activity Capability Inventory
- ADR-1.2-0008 — Actor Typing, ActorCapabilitySurface e Actor Inventory Convergence

---

## Checkpoint atual

Base 1.2 — Actors Convergence 4A–4D + H4D Hygiene: CLOSED / PASS.

Fases fechadas:

- 4A — Actor Typing Foundation;
- 4B — ActorCapabilitySurface como fonte primária de scanners;
- 4C — ActorAttributes genérico;
- 4D — ActorParticipation genérico;
- H4D-B1 — remoção de mortos claros de NonPlayerActorParticipation;
- H4D-B2 — remoção de fallback implícito de identity em Camera/Permission;
- H4D-B3 — ActorAttributes store por ActorInstanceId;
- H4D-B4 — revisão de metadata/validator sem ação de código.

---

## Invariantes congeladas

- Actor é raiz abstrata.
- PlayerActor/NonPlayerActor são especializações concretas.
- ActorKind/Role/Scope são metadata/log/transição, não rail funcional.
- ActorCapabilitySurface é a fonte local primária de endpoints.
- Scanners migrados usam ActorScanTarget + ActorCapabilitySurface.
- ActorPresentation, ActorAttributes e ActorParticipation usam caminho genérico.
- Identity obrigatória não pode ser fabricada por fallback.
- Strings/IDs são opacos para decisão funcional.
- Ausência obrigatória é fail-fast ou failure explícita.
- Ausência opcional é skip explícito.

---

## Débitos futuros

- 4E: policy/registry genérica de ActorParticipation incluindo PlayerActor.
- 4E/4F: stores/registries genéricos por ActorInstanceId.
- 4F: redução de playerActorId/playerSlotId em Camera/Permission/Movement.
- Hygiene final: revisão de nomes legados e diagnostics passivos.
