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

Base 1.2 — Actors Convergence 4A–4D + H4D Hygiene + ActorReset-1B: CLOSED / PASS funcional.

Fases fechadas:

- 4A — Actor Typing Foundation;
- 4B — ActorCapabilitySurface como fonte primária de scanners;
- 4C — ActorAttributes genérico;
- 4D — ActorParticipation genérico;
- H4D-B1 — remoção de mortos claros de NonPlayerActorParticipation;
- H4D-B2 — remoção de fallback implícito de identity em Camera/Permission;
- H4D-B3 — ActorAttributes store por ActorInstanceId;
- H4D-B4 — revisão de metadata/validator sem ação de código;
- ActorReset-1 — contrato neutro `ActorReset*` e remoção de shape `PlayerActorReset*` como contrato canônico;
- ActorReset-1B — QA probe canônico `Reset Current Player Actor` via `ActorResetCommand`.

---

## Invariantes congeladas

- Actor é raiz abstrata.
- PlayerActor/NonPlayerActor são especializações concretas.
- ActorKind/Role/Scope são metadata/log/transição, não rail funcional.
- ActorCapabilitySurface é a fonte local primária de endpoints.
- Scanners migrados usam ActorScanTarget + ActorCapabilitySurface.
- ActorPresentation, ActorAttributes e ActorParticipation usam caminho genérico.
- ActorReset usa contrato neutro `ActorReset*`.
- `ActorResetAdapter` é executor neutro.
- Endpoint obrigatório de reset deve existir no authoring do Actor/prefab; ausência obrigatória é erro explícito.
- Identity obrigatória não pode ser fabricada por fallback.
- Strings/IDs são opacos para decisão funcional.
- Ausência obrigatória é fail-fast ou failure explícita.
- Ausência opcional é skip explícito.
- QA/debug pode solicitar comando canônico, mas não vira owner de lifecycle.

---

## Checkpoint de smoke congelado

O checkpoint atual aceita como evidência:

- `ActorResetQaRequested`;
- `ActorResetQaApplied`;
- `QaResetCurrentPlayerActor outcomeKind='Applied'`;
- `RestartCurrentActivity PASS`;
- `Activity01ToActivity02 PASS`;
- `RouteExitBackToMenu PASS`;
- sem `FATAL`;
- sem `Exception`;
- sem `actor_reset_endpoint_missing`;
- sem `ActorResetQaRejected` no caminho válido.

---

## Débitos futuros

- 4E: policy/registry genérica de ActorParticipation incluindo PlayerActor.
- 4E/4F: stores/registries genéricos por ActorInstanceId.
- 4F: redução de playerActorId/playerSlotId em Camera/Permission/Movement.
- ActorReset futuro: resolver por `ActorInstanceId`/Actor registry genérico, não por resolver player-specific transitório.
- Reset por escopo: desenhar `World`, `Route`, `Activity`, `Actor/Object` em fase própria, sem `EventBus` global e sem `ResetManager` monolítico.
- Hygiene final: revisão de nomes legados e diagnostics passivos.

---

## ADRs Base 2.0 — Session Architecture Convergence

- ADR-2.0-0001 — SessionOperational Ownership Stabilization e Regra Anti-Deslocamento
- ADR-2.0-0002 — SessionActivity Ownership Decomposition e ActivityEntryPipeline
- ADR-2.0-0003 — PlayerParticipation, PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary
- ADR-2.0-0004 — SA-IDREF Typed Runtime References e PlayerActor Runtime Identity

### Checkpoint conceitual Base 2.0

O ADR-2.0-0003 congela que:

- `PlayerInputManager` é infraestrutura técnica de input criada/validada no boot.
- `InputModes` aplica modo de input por rota/superfície e funciona também sem PlayerActor materializado.
- `PlayerParticipation` resolve `PlayerSlotReservation`, `PlayerSelection` e `SessionParticipationContext`.
- Rotas com `SessionActivityEntry` handoff precisam de participação resolvida antes do handoff.
- Se a rota exige participação e nenhuma seleção existe, `SessionOperational/PlayerParticipation` aplica default explícito.
- `SessionActivity/ActivityEntryPipeline` materializa Actors a partir da participação resolvida.
- `PlayerInput` concreto do player nasce com o `PlayerActor` materializado pela Activity.
- `PlayerInputBinding`, Camera, Movement e Permission binding ocorrem após materialização.
- Runtime join futuro via `PlayerInputManager.Join` deve passar pelo mesmo domínio `PlayerParticipation`, sem trilho paralelo.

O ADR-2.0-0004 congela que:

- IDs textuais podem existir para authoring, serialização, logs e debug, mas não como referência runtime entre domínios.
- `ActivityParticipantBinding` é a referência primária dentro da Activity para participant.
- `PlayerActorRuntimeHandle` é a referência primária do PlayerActor materializado.
- Stages consumidores como Input, Movement, Camera, Permission e Reset não fabricam `PlayerActorId`.
- `SA-IDREF-2H5 — Centralizar PlayerActorId no PlayerActorRuntimeHandle / Registry` está CLOSED / PASS após smoke manual.
- `SA-IDREF-3A-H1/H2/H3` registrou a regressão e congelou a separação entre lookup operacional e identidade observável.
- `PlayerActorId` permanece exposto em `PlayerActorIdentityRecord` / `PlayerActorRuntimeHandle` como identidade observável, mas não é chave operacional primária de lookup runtime.
- `PlayerInputBindingStage` e `PlayerMovementBindingStage` não fabricam `PlayerActorId`; consumers usam binding/handle.
- Nenhum corte `SA-IDREF` futuro é PASS sem smoke/log.



### Checkpoint SA-IDREF-2H5

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

Evidência aceita:

- sem `FATAL`;
- sem `Exception`;
- sem `route_transition_failed`;
- sem foreign/stale indevido;
- `RestartCurrentActivity PASS`;
- `Activity01ToActivity02 PASS`;
- `RouteExitBackToMenu PASS`;
- `PlayerInputActionsReboundToCanonical` observado;
- `MovementBindingCompleted` preservado;
- `MovementBindingRetained` preservado em `activity_02`;
- `MovementControlEnabled/Disabled` preservados;
- `CameraBindingCompleted` preservado;
- `BuildPlayerActorId` ausente no log.


### Checkpoint SA-IDREF-3A

Status: Applied / Pending smoke.

Resumo:

- `ActivityPlayerActorRegistry` deixou de manter índice ativo por `PlayerActorId`.
- Lookup operacional de handle continua por `SessionParticipantId` quando existe `ActivityParticipantBinding`.
- Consumers de exit/reset passam a resolver `PlayerActorRuntimeHandle` por `ActorInstanceRuntimeId`.
- `PlayerActorId` permanece como identidade observada/logada depois do handle resolvido, não como chave ativa de lookup runtime.
- PASS exige novo smoke/log.

- `ADR-2.0-0004-SA-IDREF-Typed-Runtime-References.md` — Typed runtime references / IDREF cleanup. Status: SA-IDREF-3A-H1/H2/H3 applied; contract regression registered; next runtime cut blocked until identity audit.


### Contrato corretivo SA-IDREF-3A-H1/H2/H3

- Remover `PlayerActorId` do contrato observável é regressão.
- O proibido é usar `PlayerActorId` como chave primária de lookup runtime fora do owner canônico.
- Lookup ativo de `PlayerActorRuntimeHandle` deve partir de `SessionParticipantId` ou `ActorInstanceRuntimeId` conforme a fronteira.
- Consumers observam `PlayerActorId` somente depois do handle resolvido.
- Actor `RouteScoped` não pode ser validado contra a entry ativa atual; deve ser validado por sessão/scope/participant ou instância runtime.
- Próximos cortes de identity devem declarar: identidade removida, identidade preservada, quem cria, quem observa, quem pode usar como lookup, quem não pode comparar e qual smoke prova ausência de regressão.
