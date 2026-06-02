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
- Actor é a entrada canônica; `PlayerActor`/`NonPlayerActor` são nomes/resíduos concretos do corte atual, não categorias normativas nem trilhos separados.
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
- `SA-IDREF-4A — Camera target by ActorInstanceRuntimeId` está CLOSED / PASS após smoke manual.
- `SA-IDREF-4B — Permission identity audit` está AUDITED / NO RUNTIME CHANGE.
- `SA-IDREF-4C — Permission target by ActorInstanceRuntimeId` está CLOSED / PASS após smoke manual.
- `SA-IDREF-4D — Reset identity / endpoint reference audit` está AUDITED / NO RUNTIME CHANGE.
- Em Permission, `ActorInstanceRuntimeId` é o target funcional; `PlayerActorId` e `PlayerSlotId` permanecem apenas como observabilidade/log/fact/payload.



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


### Checkpoint SA-IDREF-4A

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- Camera target runtime reference agora casa endpoint com `PlayerActorRuntimeHandle` por `ActorInstanceRuntimeId`.
- `ActorId` permanece como guarda tipada adicional.
- `PlayerActorId` permanece observável para log/fact/payload, mas não participa como chave primária de lookup de câmera.
- PASS confirmado por smoke: `CameraBindingCompleted`, `RestartCurrentActivity`, `Activity01ToActivity02` e `RouteExitBackToMenu` preservados; sem `FATAL`, `Exception`, `route_transition_failed` ou `foreign/stale` indevido.

### Checkpoint SA-IDREF-4B

Status: AUDITED / NO RUNTIME CHANGE.

- Permission identity audit concluída.
- Permission ainda usa `PlayerActorId` como target/key operacional em command, binding, receiver identity, receiver id e `PermissionKey`.
- Isso está funcional no smoke atual, mas é bridge transitória frente ao contrato SA-IDREF.
- Próximo corte recomendado: `SA-IDREF-4C — Permission target by ActorInstanceRuntimeId`.
- Regra congelada: `ActorInstanceRuntimeId` deve virar target funcional de Permission; `PlayerActorId` e `PlayerSlotId` permanecem apenas como observabilidade/log/fact/payload.
- Não houve alteração runtime neste checkpoint; portanto não há PASS funcional novo.


### Checkpoint SA-IDREF-4C

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- Permission target funcional passou para `ActorInstanceRuntimeId`.
- `ActivityCapabilityPermissionCommand`, `ActivityCapabilityPermissionBinding`, `ActivityCapabilityPermissionReceiverIdentity` e `ActivityCapabilityPermissionReceiverReference` passaram a carregar `ActorInstanceRuntimeId`.
- `PermissionKey` deixou de usar `PlayerActorId` e passou a usar `ActorInstanceRuntimeId`.
- `PlayerMovementPermissionReceiver` filtra por `ActorInstanceRuntimeId`.
- `PlayerActorId` e `PlayerSlotId` permanecem em logs/facts/payloads observáveis.
- PASS confirmado por smoke manual: sem `FATAL`, `Exception`, `route_transition_failed`, `foreign/stale` indevido ou `PermissionTargetIdentityUnresolved`; checkpoints principais preservados.

### Checkpoint SA-IDREF-4D

Status: AUDITED / NO RUNTIME CHANGE.

- Reset identity / endpoint reference audit concluída.
- `PlayerActorResetEndpointResolver` já resolve handle por `ActorInstanceRuntimeId`, não por `PlayerActorId`.
- `SessionActivityPipeline.BuildActorResetActorRef(...)` já parte de `ActivityParticipantBinding`/`PlayerActorRuntimeHandle` e extrai `ActorInstanceRuntimeId` do `Actor` runtime.
- Resíduo encontrado: `ActorResetActorRef.IsValid` e `PlayerActorResetEndpointResolver.EnsureIdentityMatchesOrFail(...)` ainda tratam `PlayerActorId`/`PlayerSlotId` como validade/guarda funcional para player reset.
- Regra congelada: em Reset, `ActorInstanceRuntimeId + ActorId` devem ser o alvo funcional; `PlayerActorId`/`PlayerSlotId` devem permanecer observabilidade/log/fact/payload.
- `ActivityObjectReset` por `targetId` é domínio separado de objeto de Activity; não deve ser misturado com Actor runtime identity neste corte.
- Próximo corte recomendado: `SA-IDREF-4E — Reset actor ref validity by ActorInstanceRuntimeId`.
- Não houve alteração runtime; portanto não há compile/smoke novo exigido.


### Checkpoint SA-IDREF-4E

Status: Applied / Pending compile + smoke.

- Reset actor ref validity passou a depender funcionalmente de `ActorInstanceRuntimeId + ActorId`, com contexto `PipelineId + SessionId`.
- `ActorResetActorRef.IsValid` não exige mais `PlayerActorId`/`PlayerSlotId` para `ActorKind.Player`.
- `PlayerActorResetEndpointResolver.ResolveOrFail(...)` não rejeita mais player reset por `PlayerActorId` inválido quando `ActorInstanceRuntimeId` é válido.
- `PlayerActorResetEndpointResolver.EnsureIdentityMatchesOrFail(...)` não usa mais `PlayerActorId`/`PlayerSlotId` como guarda funcional.
- `PlayerActorId` e `PlayerSlotId` permanecem em logs/facts/payload como observabilidade.
- `ActivityObjectReset`, Camera, Permission, Presentation e Attributes não foram alterados.
- PASS depende de compile sem erros CS e smoke com `ActorResetQaApplied`, `ActivityObjectReset PassedApplied`, Movement/Camera preservados e checkpoints principais passando.
- `SA-IDREF-4E-H1` está CLOSED / PASS funcional + PASS arquitetural do corte: removeu o alias textual `player1` do QA reset current player e validou seleção automática somente quando há exatamente um player target válido na entry atual.


### Checkpoint SA-IDREF-5A

Status: AUDITED / NO RUNTIME CHANGE.

- Auditoria residual de strings/IDs executada sobre `NewScripts/**/*.cs`, ignorando logs/evidências históricas e markdowns.
- Não há literais `player1`/`player2` em código C# ativo.
- Não há `Dictionary<PlayerActorId, ...>` nem lookup ativo `TryResolveHandleForPlayerActor`.
- `BuildPlayerActorId` permanece restrito ao ponto de criação de `PlayerActorIdentityRecord` em materialização/binding de participante.
- Usos de `.Value`/`ToString()` em scanners, logs, policy entries, binding states, receiverId e paths Unity foram classificados como observabilidade/serialização técnica aceitável neste checkpoint.
- Resíduos funcionais encontrados: `routeParticipantHint` textual comparado com `PlayerSlotId.Value`, role inferida por `ContainsOrdinalToken(primary/support)` e overload QA interno ainda aceitando slot textual opcional.
- Próximo corte recomendado: `SA-IDREF-5B — typed participant requirement binding / no semantic role parsing`.

### Checkpoint SA-IDREF-5B

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- Removeu o binding funcional por `routeParticipantHint` string contra `PlayerSlotId.Value`.
- Removeu inferência de role por `ContainsOrdinalToken(primary/support)`.
- `ParticipantRequirement` agora carrega `SessionParticipantId` tipado e `ExpectedSessionRole` explícito.
- `ActivityParticipantRequirementAuthoring` ganhou `expectedSessionRole`, default `PrimaryPlayer`.
- `TryQaResetCurrentPlayerActor` não aceita mais slot textual opcional; QA current reset só seleciona automaticamente quando há exatamente um player target válido.
- PASS confirmado por smoke completo: sem `FATAL`, `Exception`, `route_transition_failed`, `foreign/stale` indevido ou `checkpointStatus='Failed'`.
- O smoke confirmou `ActorResetQaApplied`, `ActivityParticipantBindingCompleted`, `ActivityParticipantActorMaterialized`/`ActivityParticipantActorMaterializationRetained`, `MovementBindingCompleted`, `CameraBindingCompleted`, `RestartCurrentActivity PASS`, `Activity01ToActivity02 PASS` e `RouteExitBackToMenu PASS`.
- `SA-IDREF-5B-H1` fechado como PASS: logging de ambiguidade do QA reset agora observa `ActorInstanceRuntimeId` via `PlayerActorRuntimeHandle`, não por propriedade inexistente em `PlayerActorIdentityRecord`.
- O caminho QA current reset não reintroduziu `player1`, `player2`, slot textual opcional ou lookup primário por `PlayerActorId`.

### Checkpoint SA-IDREF-5C

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- Auditoria de authoring refs/assets executada com separação entre string serializada aceitável, ids autorais de asset/profile, logs/paths técnicos e referência funcional indevida.
- Corrigido resíduo de participant authoring: `roleId` textual foi removido de `ActivityParticipantRequirementAuthoring`, `ParticipantRequirement`, `ActivityParticipantBindCommand` e `ActivityContentProfile01.asset`.
- `ActivityParticipantRequirementAuthoring` ainda serializa `participantId` como string técnica de Unity, mas expõe `SessionParticipantId` tipado para o builder.
- `ParticipantRequirement` passou a carregar `SessionParticipantId + ExpectedSessionRole` como contrato primário.
- `ActivityParticipantBindCommand` passou a carregar `RequestedParticipantId` como `SessionParticipantId` tipado e não carrega mais `RoleId` textual.
- `ActivityObject` targetId/roleId, Activity asset ids, profile ids, Actor Presentation slot ids e Attribute ids foram classificados como domínios separados/ids autorais e ficaram fora do corte runtime.
- PASS depende de compile sem erros CS e smoke completo preservando participant binding, Movement, Camera, ActorReset QA e checkpoints principais.

### Checkpoint SA-IDREF-5D

Status: AUDITED / NO RUNTIME CHANGE.

- Auditoria de fronteira dos resíduos de string/ID restantes após `SA-IDREF-5C` concluída.
- Não há novo resíduo crítico no trilho Player/Participant/Handle: sem `player1/player2`, sem `routeParticipantHint`, sem `ContainsOrdinalToken`/`ResolveExpectedSessionParticipantRole`, sem `Dictionary<PlayerActorId, ...>` e sem lookup `TryResolveHandleForPlayerActor` no código ativo.
- `BuildPlayerActorId` permanece restrito ao ponto de criação/materialização do `PlayerActorIdentityRecord`.
- Strings restantes foram classificadas como authoring/Unity/log/domínio próprio: `participantId` serializado com projeção typed, `ActivityObject targetId/roleId`, ids de Activity/profile, capability ids/paths, receiver ids técnicos, Presentation slot local e Attribute definition ids.
- Correção de enquadramento registrada: `NonPlayer` não é categoria normativa; qualquer `NonPlayer*` remanescente é resíduo lexical/legado ou nome concreto ainda não convergido para `Actor + ActorRole + ActorScope + Capability/Endpoint`.
- Fechamento documental registrado: `SA-ACTOR-1B1*` concluiu a limpeza de ownership de lifetime dos `Actors`; `ActorScope` ficou congelado como fonte canônica de `lifetime/retention/release`, `ActivityActorExitRuntimeState` ficou como `correlation store` técnico, `ActivityPlayerActorRegistry` ficou como índice técnico puro e `SessionScoped` permanece fora de escopo.
- Risco futuro registrado: há superfícies de Actor ainda nomeadas `NonPlayer*` com filtro autoral por activity ids textuais; isso deve virar `ActivityId` tipado em corte próprio de Actor/Activity authoring refs, sem tratar `NonPlayer` como domínio.
- Próximos cortes possíveis, não automáticos: `SA-IDREF-6A` ActivityObject typed refs, `SA-IDREF-6B` Activity authoring refs, `SA-IDREF-6C` Presentation refs, `SA-IDREF-6D` Attribute refs, `SA-IDREF-6E` Actor ActivityId refs / remover resíduo de taxonomia NonPlayer.
- Não houve runtime change; não há compile/smoke novo exigido.
