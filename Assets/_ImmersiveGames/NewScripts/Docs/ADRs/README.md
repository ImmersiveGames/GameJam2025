# ADRs â€” Base 1.2

Este diretÃ³rio contÃ©m os ADRs vivos da Base 1.2 â€” Actors Convergence / ConvergÃªncia de Atores.

Fonte normativa anterior: Base 1.1 jÃ¡ concluÃ­da.  
Base 1.2 nÃ£o substitui a Base 1.1; ela adapta atores ao shape da Base 1.1.

---

## SA-16A closure

- `SA-16A` - `Movement / GameplayControl / Reset / Save boundary`: `CLOSED`.
- `SA-16A1` - `Initial Movement Blocked state ownership cleanup`: `PASS funcional + PASS arquitetural do corte`.
- `SA-16A2` - `MovementTransient reset endpoint support`: `PASS funcional + PASS arquitetural do corte`.
- `MovementBindingAdapter` deixou de publicar gate state.
- `ActivityEntryMovementBindingStage` continua publicando o `Blocked` inicial.
- `PlayerActorDefaultResetEndpoint` passou a suportar `MovementTransient`.
- `Movement` continua fora de Save/Snapshot.
- Nao houve alteracao em gate/permission/control.
- Nao houve fallback global, first player, lookup textual ou cruzamento indevido de identidades.

## ADRs ativos

- ADR-1.2-0004 â€” ActorAttributes como ActorCapability
- ADR-1.2-0005 â€” SessionActivityPipeline Decomposition e Capability Stages
- ADR-1.2-0006 â€” ActivityCapabilityPermission e ReaÃ§Ã£o Local de Capabilities
- ADR-1.2-0007 â€” Capability Discovery e Activity Capability Inventory
- ADR-1.2-0008 â€” Actor Typing, ActorCapabilitySurface e Actor Inventory Convergence

---

## Checkpoints Base 2.0

- `SessionOperational` permanece com checkpoint funcional/arquitetural parcial aceito na frente de ownership stabilization.
- `SessionActivity` fica congelado temporariamente em `SA-14E - SessionActivity decomposition closure matrix`.
- `SA-14B1` segue como o Ãºltimo corte runtime validado de `SessionActivity`.
- `SA-13D`, `SA-14C`, `SA-14D` e `SA-14E` ficaram fechados como auditorias/matriz de dÃ©bitos.
- Movement, ActivityContent, RouteActivitySave e pending-operation callback path nÃ£o devem ser reabertos sem regressÃ£o concreta.
- A prÃ³xima frente runtime deve ser escolhida fora de `SessionActivity`, salvo regressÃ£o.
- `SA-15C` fechou o `RouteActivitySave` com `RouteActivitySaveContributorScopePolicy` como policy normativa.
- `CurrentActivityObjectSnapshot` Ã© o Ãºnico scope funcional ativo hoje.
- `CurrentRouteSaveContributors` e `RouteAndActivitySaveContributors` permanecem como contrato/policy futura, sem infraestrutura ativa.
- `RouteActivitySave last useful payload` continua sendo policy futura explÃ­cita; nÃ£o Ã© bug local nem fallback implÃ­cito.

- `SA-16B` - `ActivityContent async release completion boundary`: `CLOSED`.
- `SA-16B1` - `ActivityContent unload callback boundary cleanup`: `PASS funcional + PASS arquitetural do corte`.
---

## Checkpoint atual

Base 1.2 â€” Actors Convergence 4Aâ€“4D + H4D Hygiene + ActorReset-1B: CLOSED / PASS funcional.

Fases fechadas:

- 4A â€” Actor Typing Foundation;
- 4B â€” ActorCapabilitySurface como fonte primÃ¡ria de scanners;
- 4C â€” ActorAttributes genÃ©rico;
- 4D â€” ActorParticipation genÃ©rico;
- H4D-B1 â€” remoÃ§Ã£o de mortos claros de NonPlayerActorParticipation;
- H4D-B2 â€” remoÃ§Ã£o de fallback implÃ­cito de identity em Camera/Permission;
- H4D-B3 â€” ActorAttributes store por ActorInstanceId;
- H4D-B4 â€” revisÃ£o de metadata/validator sem aÃ§Ã£o de cÃ³digo;
- ActorReset-1 â€” contrato neutro `ActorReset*` e remoÃ§Ã£o de shape `PlayerActorReset*` como contrato canÃ´nico;
- ActorReset-1B â€” QA probe canÃ´nico `Reset Current Player Actor` via `ActorResetCommand`.

---

## Invariantes congeladas

- Actor Ã© raiz abstrata.
- Actor Ã© a entrada canÃ´nica; `PlayerActor`/`NonPlayerActor` sÃ£o nomes/resÃ­duos concretos do corte atual, nÃ£o categorias normativas nem trilhos separados.
- ActorKind/Role/Scope sÃ£o metadata/log/transiÃ§Ã£o, nÃ£o rail funcional.
- ActorCapabilitySurface Ã© a fonte local primÃ¡ria de endpoints.
- Scanners migrados usam ActorScanTarget + ActorCapabilitySurface.
- ActorPresentation, ActorAttributes e ActorParticipation usam caminho genÃ©rico.
- ActorReset usa contrato neutro `ActorReset*`.
- `ActorResetAdapter` Ã© executor neutro.
- Endpoint obrigatÃ³rio de reset deve existir no authoring do Actor/prefab; ausÃªncia obrigatÃ³ria Ã© erro explÃ­cito.
- Identity obrigatÃ³ria nÃ£o pode ser fabricada por fallback.
- Strings/IDs sÃ£o opacos para decisÃ£o funcional.
- AusÃªncia obrigatÃ³ria Ã© fail-fast ou failure explÃ­cita.
- AusÃªncia opcional Ã© skip explÃ­cito.
- QA/debug pode solicitar comando canÃ´nico, mas nÃ£o vira owner de lifecycle.

---

## Checkpoint de smoke congelado

O checkpoint atual aceita como evidÃªncia:

- `ActorResetQaRequested`;
- `ActorResetQaApplied`;
- `QaResetCurrentPlayerActor outcomeKind='Applied'`;
- `RestartCurrentActivity PASS`;
- `Activity01ToActivity02 PASS`;
- `RouteExitBackToMenu PASS`;
- sem `FATAL`;
- sem `Exception`;
- sem `actor_reset_endpoint_missing`;
- sem `ActorResetQaRejected` no caminho vÃ¡lido.

---

## DÃ©bitos futuros

- 4E: policy/registry genÃ©rica de ActorParticipation incluindo PlayerActor.
- 4E/4F: stores/registries genÃ©ricos por ActorInstanceId.
- 4F: reduÃ§Ã£o de playerActorId/playerSlotId em Camera/Permission/Movement.
- ActorReset futuro: resolver por `ActorInstanceId`/Actor registry genÃ©rico, nÃ£o por resolver player-specific transitÃ³rio.
- Reset por escopo: desenhar `World`, `Route`, `Activity`, `Actor/Object` em fase prÃ³pria, sem `EventBus` global e sem `ResetManager` monolÃ­tico.
- Hygiene final: revisÃ£o de nomes legados e diagnostics passivos.

---

## ADRs Base 2.0 â€” Session Architecture Convergence

- ADR-2.0-0001 â€” SessionOperational Ownership Stabilization e Regra Anti-Deslocamento
- ADR-2.0-0002 â€” SessionActivity Ownership Decomposition e ActivityEntryPipeline
- ADR-2.0-0003 â€” PlayerParticipation, PlayerSlot, PlayerSelection, SessionParticipation e ActorMaterialization Boundary
- ADR-2.0-0004 â€” SA-IDREF Typed Runtime References e PlayerActor Runtime Identity

### Checkpoint SessionActivity Base 2.0 â€” SA-10 / SA-11B

- `SA-10 â€” Permission identity separation final`: `CLOSED / DOCUMENTATION ONLY`. Auditoria confirmou que `Permission` usa `ActorInstanceRuntimeId` como target funcional; `PlayerActorId` e `PlayerSlotId` permanecem observabilidade/log/fact/payload.
- `SA-11B â€” Fact recorder hygiene`: `CLOSED / PASS funcional + PASS arquitetural do corte`. O smoke mais recente confirmou o ordering correto: cleanup final antes de `ActivityContentReleaseCompleted`, com `pendingReleaseContextPresentAfter='false'`, `loadedSetPresentAfter='false'` e `awaitingContinuationAfter='false'`.
- `SA-11B-H2 â€” ActivityContentReleaseCompleted ordering fix`: `CLOSED / PASS funcional + PASS arquitetural do corte`.
- A ausÃªncia de `PlayerActorParticipationExitStageCompleted` em alguns smokes permanece aceita como caminho condicional: auditoria estÃ¡tica confirmou que o substage sÃ³ executa quando `exitedPlayerActors.Count > 0`, e que o patch alinha fact/snapshot quando executado.



### Checkpoint SessionActivity Base 2.0 â€” SA-12 command hygiene

- `SA-12 â€” Commands e contracts finais`: `PARTIAL / IN PROGRESS`.
- `SA-12-AUDIT`: `AUDITED / NEEDS SMALL COMMAND HYGIENE PATCH`. Auditoria nÃ£o encontrou `Action`, `Func<T>`, adapters, delegates de execuÃ§Ã£o ou `SessionActivityRuntimeState` embutidos nos commands auditados; o dÃ©bito Ã© higiene de contract.
- `SA-12B/C â€” Command boundary + identity duplication cleanup`: `CLOSED / PASS funcional + PASS arquitetural do corte`.
  - `ActivityObjectContributorUnregisterStageCommand` nÃ£o carrega mais `SessionActivityStage Stage`.
  - `ActivityObjectResetCommand`, `ActivityObjectReleaseCommand`, `ActivityObjectSnapshotRestoreCommand` e `ActivityContentSceneUnloadCommand` nÃ£o duplicam mais `PipelineId`, `SessionStateId`, `ActivityId`, `ActivityOrdinal` e `EntrySequence` quando `SessionActivityIdentity` jÃ¡ Ã© a fonte do ciclo.
- `SA-12D â€” ActorAttributeCommand typed identity`: `CLOSED / PASS funcional + PASS arquitetural do corte`.
  - `ActorAttributeCommand` passou a carregar `SessionActivityIdentity` e `ActorInstanceRuntimeId`.
  - Removidos do command contract os campos livres `string PipelineIdentity`, `string ActivityIdentity` e `string ActorInstanceId`.
  - Smoke preservou `ActorAttributeSetupStarted`, `ActorAttributeProfileResolved`, `ActorAttributeReady`, `ActorAttributeSetupCompleted`, `ActorAttributeReleased` e checkpoints macro.
- `SA-12E â€” ActivityContent SceneKeyAsset/runtime scene reference`: `CLOSED / PASS funcional + PASS arquitetural do corte`.
  - `ActivityEntryContentLoadCommand` passou a carregar `ActivityContentLoadPlan`.
  - `ActivityContentLoadedSceneRecord` e unload passaram a operar por runtime scene reference, sem `SceneKeyAsset` como payload runtime.
  - `activity_01` preserva `loadedScenes='1'`; `activity_02` preserva no-content/skip explÃ­cito.
- `SA-12F â€” Reduce SessionActivityDefinition from ActivityEntry*Command`: `PARTIAL / IN PROGRESS`.
  - Fechados: `SA-12F1A/B`, `SA-12F2`, `SA-12F3A`, `SA-12F3B`, `SA-12F3C`, `SA-12F4A`, `SA-12F4B`, `SA-12F4C`.
  - `ActivityEntryContentLoadCommand`, binding commands, ActorPresentation/ActorAttribute setup commands, ParticipantBinding e ObjectSetup deixaram de carregar `SessionActivityDefinition` nos subfluxos tratados.
  - `ActivityEntryObjectSetupCommand` foi dividido entre `ActivityObjectSetupInventoryPlan` e `ActivityObjectResetRestorePlan` e nÃ£o carrega mais `SessionActivityDefinition`.
- `SA-12F-BLOCKER-MOVEMENT-ACTIVITY02`: `CLOSED / PASS funcional + PASS arquitetural parcial`.
  - Loading voltou a completar/esconder.
  - `activity_02` preserva no-content, mas projeta o `PlayerActor SessionScoped` retido para `ActivityParticipationContext`, capability inventory, PermissionTarget, PlayerInput, MovementBinding e MovementControl.
  - Smoke aceito: `ActivityParticipantRetainedBindingChosen`, `ActivityParticipationContextPrepared activityParticipants='1'`, `ActivityEntryPermissionTargetPreparationCompleted receivers='1'`, `PlayerMovementPermissionApplied state='Allowed'`, `MovementControlEnabled activityId='activity_02' affectedActors='1'`, `Activity01ToActivity02 PASS`.
  - DÃ©bito aceito: `SA-12F-MOV-H1 â€” Retained PlayerActor target projection ownership hygiene`; mover a projection bridge hoje em `SessionActivityPipeline` para `ActivityEntryPipeline` / `ActivityEntryActorInventoryStage` em corte futuro.
- PendÃªncias de `SA-12`: `SA-12F5 â€” residual SessionActivityDefinition command hygiene`; `SA-12F-MOV-H1 â€” hygiene de ownership da projeÃ§Ã£o de PlayerActor SessionScoped retido`.
- `SA-11B` segue `CLOSED / PASS funcional + PASS arquitetural do corte`; `SA-12` permanece parcial atÃ© fechamento dos resÃ­duos finais.

### Checkpoint conceitual Base 2.0

O ADR-2.0-0003 congela que:

- `PlayerInputManager` Ã© infraestrutura tÃ©cnica de input criada/validada no boot.
- `InputModes` aplica modo de input por rota/superfÃ­cie e funciona tambÃ©m sem PlayerActor materializado.
- `PlayerParticipation` resolve `PlayerSlotReservation`, `PlayerSelection` e `SessionParticipationContext`.
- Rotas com `SessionActivityEntry` handoff precisam de participaÃ§Ã£o resolvida antes do handoff.
- Se a rota exige participaÃ§Ã£o e nenhuma seleÃ§Ã£o existe, `SessionOperational/PlayerParticipation` aplica default explÃ­cito.
- `SessionActivity/ActivityEntryPipeline` materializa Actors a partir da participaÃ§Ã£o resolvida.
- `PlayerInput` concreto do player nasce com o `PlayerActor` materializado pela Activity.
- `PlayerInputBinding`, Camera, Movement e Permission binding ocorrem apÃ³s materializaÃ§Ã£o.
- Runtime join futuro via `PlayerInputManager.Join` deve passar pelo mesmo domÃ­nio `PlayerParticipation`, sem trilho paralelo.

O ADR-2.0-0004 congela que:

- IDs textuais podem existir para authoring, serializaÃ§Ã£o, logs e debug, mas nÃ£o como referÃªncia runtime entre domÃ­nios.
- `ActivityParticipantBinding` Ã© a referÃªncia primÃ¡ria dentro da Activity para participant.
- `PlayerActorRuntimeHandle` Ã© a referÃªncia primÃ¡ria do PlayerActor materializado.
- Stages consumidores como Input, Movement, Camera, Permission e Reset nÃ£o fabricam `PlayerActorId`.
- `SA-IDREF-2H5 â€” Centralizar PlayerActorId no PlayerActorRuntimeHandle / Registry` estÃ¡ CLOSED / PASS apÃ³s smoke manual.
- `SA-IDREF-3A-H1/H2/H3` registrou a regressÃ£o e congelou a separaÃ§Ã£o entre lookup operacional e identidade observÃ¡vel.
- `PlayerActorId` permanece exposto em `PlayerActorIdentityRecord` / `PlayerActorRuntimeHandle` como identidade observÃ¡vel, mas nÃ£o Ã© chave operacional primÃ¡ria de lookup runtime.
- `PlayerInputBindingStage` e `PlayerMovementBindingStage` nÃ£o fabricam `PlayerActorId`; consumers usam binding/handle.
- Nenhum corte `SA-IDREF` futuro Ã© PASS sem smoke/log.
- `SA-IDREF-4A â€” Camera target by ActorInstanceRuntimeId` estÃ¡ CLOSED / PASS apÃ³s smoke manual.
- `SA-IDREF-4B â€” Permission identity audit` estÃ¡ AUDITED / NO RUNTIME CHANGE.
- `SA-IDREF-4C â€” Permission target by ActorInstanceRuntimeId` estÃ¡ CLOSED / PASS apÃ³s smoke manual.
- `SA-IDREF-4D â€” Reset identity / endpoint reference audit` estÃ¡ AUDITED / NO RUNTIME CHANGE.
- Em Permission, `ActorInstanceRuntimeId` Ã© o target funcional; `PlayerActorId` e `PlayerSlotId` permanecem apenas como observabilidade/log/fact/payload.



### Checkpoint SA-IDREF-2H5

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

EvidÃªncia aceita:

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

- `ActivityPlayerActorRegistry` deixou de manter Ã­ndice ativo por `PlayerActorId`.
- Lookup operacional de handle continua por `SessionParticipantId` quando existe `ActivityParticipantBinding`.
- Consumers de exit/reset passam a resolver `PlayerActorRuntimeHandle` por `ActorInstanceRuntimeId`.
- `PlayerActorId` permanece como identidade observada/logada depois do handle resolvido, nÃ£o como chave ativa de lookup runtime.
- PASS exige novo smoke/log.

- `ADR-2.0-0004-SA-IDREF-Typed-Runtime-References.md` â€” Typed runtime references / IDREF cleanup. Status: SA-IDREF-3A-H1/H2/H3 applied; contract regression registered; next runtime cut blocked until identity audit.


### Contrato corretivo SA-IDREF-3A-H1/H2/H3

- Remover `PlayerActorId` do contrato observÃ¡vel Ã© regressÃ£o.
- O proibido Ã© usar `PlayerActorId` como chave primÃ¡ria de lookup runtime fora do owner canÃ´nico.
- Lookup ativo de `PlayerActorRuntimeHandle` deve partir de `SessionParticipantId` ou `ActorInstanceRuntimeId` conforme a fronteira.
- Consumers observam `PlayerActorId` somente depois do handle resolvido.
- Actor `RouteScoped` nÃ£o pode ser validado contra a entry ativa atual; deve ser validado por sessÃ£o/scope/participant ou instÃ¢ncia runtime.
- PrÃ³ximos cortes de identity devem declarar: identidade removida, identidade preservada, quem cria, quem observa, quem pode usar como lookup, quem nÃ£o pode comparar e qual smoke prova ausÃªncia de regressÃ£o.


### Checkpoint SA-IDREF-4A

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- Camera target runtime reference agora casa endpoint com `PlayerActorRuntimeHandle` por `ActorInstanceRuntimeId`.
- `ActorId` permanece como guarda tipada adicional.
- `PlayerActorId` permanece observÃ¡vel para log/fact/payload, mas nÃ£o participa como chave primÃ¡ria de lookup de cÃ¢mera.
- PASS confirmado por smoke: `CameraBindingCompleted`, `RestartCurrentActivity`, `Activity01ToActivity02` e `RouteExitBackToMenu` preservados; sem `FATAL`, `Exception`, `route_transition_failed` ou `foreign/stale` indevido.

### Checkpoint SA-IDREF-4B

Status: AUDITED / NO RUNTIME CHANGE.

- Permission identity audit concluÃ­da.
- Permission ainda usa `PlayerActorId` como target/key operacional em command, binding, receiver identity, receiver id e `PermissionKey`.
- Isso estÃ¡ funcional no smoke atual, mas Ã© bridge transitÃ³ria frente ao contrato SA-IDREF.
- PrÃ³ximo corte recomendado: `SA-IDREF-4C â€” Permission target by ActorInstanceRuntimeId`.
- Regra congelada: `ActorInstanceRuntimeId` deve virar target funcional de Permission; `PlayerActorId` e `PlayerSlotId` permanecem apenas como observabilidade/log/fact/payload.
- NÃ£o houve alteraÃ§Ã£o runtime neste checkpoint; portanto nÃ£o hÃ¡ PASS funcional novo.


### Checkpoint SA-IDREF-4C

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- Permission target funcional passou para `ActorInstanceRuntimeId`.
- `ActivityCapabilityPermissionCommand`, `ActivityCapabilityPermissionBinding`, `ActivityCapabilityPermissionReceiverIdentity` e `ActivityCapabilityPermissionReceiverReference` passaram a carregar `ActorInstanceRuntimeId`.
- `PermissionKey` deixou de usar `PlayerActorId` e passou a usar `ActorInstanceRuntimeId`.
- `PlayerMovementPermissionReceiver` filtra por `ActorInstanceRuntimeId`.
- `PlayerActorId` e `PlayerSlotId` permanecem em logs/facts/payloads observÃ¡veis.
- PASS confirmado por smoke manual: sem `FATAL`, `Exception`, `route_transition_failed`, `foreign/stale` indevido ou `PermissionTargetIdentityUnresolved`; checkpoints principais preservados.

### Checkpoint SA-IDREF-4D

Status: AUDITED / NO RUNTIME CHANGE.

- Reset identity / endpoint reference audit concluÃ­da.
- `PlayerActorResetEndpointResolver` jÃ¡ resolve handle por `ActorInstanceRuntimeId`, nÃ£o por `PlayerActorId`.
- `SessionActivityPipeline.BuildActorResetActorRef(...)` jÃ¡ parte de `ActivityParticipantBinding`/`PlayerActorRuntimeHandle` e extrai `ActorInstanceRuntimeId` do `Actor` runtime.
- ResÃ­duo encontrado: `ActorResetActorRef.IsValid` e `PlayerActorResetEndpointResolver.EnsureIdentityMatchesOrFail(...)` ainda tratam `PlayerActorId`/`PlayerSlotId` como validade/guarda funcional para player reset.
- Regra congelada: em Reset, `ActorInstanceRuntimeId + ActorId` devem ser o alvo funcional; `PlayerActorId`/`PlayerSlotId` devem permanecer observabilidade/log/fact/payload.
- `ActivityObjectReset` por `targetId` Ã© domÃ­nio separado de objeto de Activity; nÃ£o deve ser misturado com Actor runtime identity neste corte.
- PrÃ³ximo corte recomendado: `SA-IDREF-4E â€” Reset actor ref validity by ActorInstanceRuntimeId`.
- NÃ£o houve alteraÃ§Ã£o runtime; portanto nÃ£o hÃ¡ compile/smoke novo exigido.


### Checkpoint SA-IDREF-4E

Status: Applied / Pending compile + smoke.

- Reset actor ref validity passou a depender funcionalmente de `ActorInstanceRuntimeId + ActorId`, com contexto `PipelineId + SessionId`.
- `ActorResetActorRef.IsValid` nÃ£o exige mais `PlayerActorId`/`PlayerSlotId` para `ActorKind.Player`.
- `PlayerActorResetEndpointResolver.ResolveOrFail(...)` nÃ£o rejeita mais player reset por `PlayerActorId` invÃ¡lido quando `ActorInstanceRuntimeId` Ã© vÃ¡lido.
- `PlayerActorResetEndpointResolver.EnsureIdentityMatchesOrFail(...)` nÃ£o usa mais `PlayerActorId`/`PlayerSlotId` como guarda funcional.
- `PlayerActorId` e `PlayerSlotId` permanecem em logs/facts/payload como observabilidade.
- `ActivityObjectReset`, Camera, Permission, Presentation e Attributes nÃ£o foram alterados.
- PASS depende de compile sem erros CS e smoke com `ActorResetQaApplied`, `ActivityObjectReset PassedApplied`, Movement/Camera preservados e checkpoints principais passando.
- `SA-IDREF-4E-H1` estÃ¡ CLOSED / PASS funcional + PASS arquitetural do corte: removeu o alias textual `player1` do QA reset current player e validou seleÃ§Ã£o automÃ¡tica somente quando hÃ¡ exatamente um player target vÃ¡lido na entry atual.


### Checkpoint SA-IDREF-5A

Status: AUDITED / NO RUNTIME CHANGE.

- Auditoria residual de strings/IDs executada sobre `NewScripts/**/*.cs`, ignorando logs/evidÃªncias histÃ³ricas e markdowns.
- NÃ£o hÃ¡ literais `player1`/`player2` em cÃ³digo C# ativo.
- NÃ£o hÃ¡ `Dictionary<PlayerActorId, ...>` nem lookup ativo `TryResolveHandleForPlayerActor`.
- `BuildPlayerActorId` permanece restrito ao ponto de criaÃ§Ã£o de `PlayerActorIdentityRecord` em materializaÃ§Ã£o/binding de participante.
- Usos de `.Value`/`ToString()` em scanners, logs, policy entries, binding states, receiverId e paths Unity foram classificados como observabilidade/serializaÃ§Ã£o tÃ©cnica aceitÃ¡vel neste checkpoint.
- ResÃ­duos funcionais encontrados: `routeParticipantHint` textual comparado com `PlayerSlotId.Value`, role inferida por `ContainsOrdinalToken(primary/support)` e overload QA interno ainda aceitando slot textual opcional.
- PrÃ³ximo corte recomendado: `SA-IDREF-5B â€” typed participant requirement binding / no semantic role parsing`.

### Checkpoint SA-IDREF-5B

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- Removeu o binding funcional por `routeParticipantHint` string contra `PlayerSlotId.Value`.
- Removeu inferÃªncia de role por `ContainsOrdinalToken(primary/support)`.
- `ParticipantRequirement` agora carrega `SessionParticipantId` tipado e `ExpectedSessionRole` explÃ­cito.
- `ActivityParticipantRequirementAuthoring` ganhou `expectedSessionRole`, default `PrimaryPlayer`.
- `TryQaResetCurrentPlayerActor` nÃ£o aceita mais slot textual opcional; QA current reset sÃ³ seleciona automaticamente quando hÃ¡ exatamente um player target vÃ¡lido.
- PASS confirmado por smoke completo: sem `FATAL`, `Exception`, `route_transition_failed`, `foreign/stale` indevido ou `checkpointStatus='Failed'`.
- O smoke confirmou `ActorResetQaApplied`, `ActivityParticipantBindingCompleted`, `ActivityParticipantActorMaterialized`/`ActivityParticipantActorMaterializationRetained`, `MovementBindingCompleted`, `CameraBindingCompleted`, `RestartCurrentActivity PASS`, `Activity01ToActivity02 PASS` e `RouteExitBackToMenu PASS`.
- `SA-IDREF-5B-H1` fechado como PASS: logging de ambiguidade do QA reset agora observa `ActorInstanceRuntimeId` via `PlayerActorRuntimeHandle`, nÃ£o por propriedade inexistente em `PlayerActorIdentityRecord`.
- O caminho QA current reset nÃ£o reintroduziu `player1`, `player2`, slot textual opcional ou lookup primÃ¡rio por `PlayerActorId`.

### Checkpoint SA-IDREF-5C

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- Auditoria de authoring refs/assets executada com separaÃ§Ã£o entre string serializada aceitÃ¡vel, ids autorais de asset/profile, logs/paths tÃ©cnicos e referÃªncia funcional indevida.
- Corrigido resÃ­duo de participant authoring: `roleId` textual foi removido de `ActivityParticipantRequirementAuthoring`, `ParticipantRequirement`, `ActivityParticipantBindCommand` e `ActivityContentProfile01.asset`.
- `ActivityParticipantRequirementAuthoring` ainda serializa `participantId` como string tÃ©cnica de Unity, mas expÃµe `SessionParticipantId` tipado para o builder.
- `ParticipantRequirement` passou a carregar `SessionParticipantId + ExpectedSessionRole` como contrato primÃ¡rio.
- `ActivityParticipantBindCommand` passou a carregar `RequestedParticipantId` como `SessionParticipantId` tipado e nÃ£o carrega mais `RoleId` textual.
- `ActivityObject` targetId/roleId, Activity asset ids, profile ids, Actor Presentation slot ids e Attribute ids foram classificados como domÃ­nios separados/ids autorais e ficaram fora do corte runtime.
- PASS depende de compile sem erros CS e smoke completo preservando participant binding, Movement, Camera, ActorReset QA e checkpoints principais.

### Checkpoint SA-IDREF-5D

Status: AUDITED / NO RUNTIME CHANGE.

- Auditoria de fronteira dos resÃ­duos de string/ID restantes apÃ³s `SA-IDREF-5C` concluÃ­da.
- NÃ£o hÃ¡ novo resÃ­duo crÃ­tico no trilho Player/Participant/Handle: sem `player1/player2`, sem `routeParticipantHint`, sem `ContainsOrdinalToken`/`ResolveExpectedSessionParticipantRole`, sem `Dictionary<PlayerActorId, ...>` e sem lookup `TryResolveHandleForPlayerActor` no cÃ³digo ativo.
- `BuildPlayerActorId` permanece restrito ao ponto de criaÃ§Ã£o/materializaÃ§Ã£o do `PlayerActorIdentityRecord`.
- Strings restantes foram classificadas como authoring/Unity/log/domÃ­nio prÃ³prio: `participantId` serializado com projeÃ§Ã£o typed, `ActivityObject targetId/roleId`, ids de Activity/profile, capability ids/paths, receiver ids tÃ©cnicos, Presentation slot local e Attribute definition ids.
- CorreÃ§Ã£o de enquadramento registrada: `NonPlayer` nÃ£o Ã© categoria normativa; qualquer `NonPlayer*` remanescente Ã© resÃ­duo lexical/legado ou nome concreto ainda nÃ£o convergido para `Actor + ActorRole + ActorScope + Capability/Endpoint`.
- Fechamento documental registrado: `SA-ACTOR-1B1*` concluiu a limpeza de ownership de lifetime dos `Actors`; `ActorScope` ficou congelado como fonte canÃ´nica de `lifetime/retention/release`, `ActivityActorExitRuntimeState` ficou como `correlation store` tÃ©cnico, `ActivityPlayerActorRegistry` ficou como Ã­ndice tÃ©cnico puro e `SessionScoped` permanece fora de escopo.
- Risco futuro registrado: hÃ¡ superfÃ­cies de Actor ainda nomeadas `NonPlayer*` com filtro autoral por activity ids textuais; isso deve virar `ActivityId` tipado em corte prÃ³prio de Actor/Activity authoring refs, sem tratar `NonPlayer` como domÃ­nio.
- PrÃ³ximos cortes possÃ­veis, nÃ£o automÃ¡ticos: `SA-IDREF-6A` ActivityObject typed refs, `SA-IDREF-6B` Activity authoring refs, `SA-IDREF-6C` Presentation refs, `SA-IDREF-6D` Attribute refs, `SA-IDREF-6E` Actor ActivityId refs / remover resÃ­duo de taxonomia NonPlayer.
- NÃ£o houve runtime change; nÃ£o hÃ¡ compile/smoke novo exigido.

### Checkpoint SA-ACTOR-1C1 â€” ActorScope.SessionScoped structural lifetime

Status: CLOSED / PASS funcional.

- `PlayerParticipation` emite `ActorScope.SessionScoped` como invariant para player; `PlayerSetDefinition` nÃ£o expÃµe mais `actorScope` editÃ¡vel.
- `PlayerActor` prefab nÃ£o Ã© owner de `ActorId`, `ActorScope` ou `ParticipationPolicy` runtime.
- `ActorScope.SessionScoped` decide lifetime estrutural do Actor, nÃ£o lifetime automÃ¡tico de `Presentation`, `Attributes`, `Permission`, `Movement`, `Camera` ou outras capabilities.
- `SessionScoped + ActivityExit => Retain`.
- `SessionScoped + RouteExit => Retain`.
- `SessionScoped + ExitToMenu/SessionReset => Release`.
- `ExitToMenu` chama `SessionReset` canÃ´nico apÃ³s `RouteExit`/save-on-exit.
- Smoke aceito: sem `FATAL`, sem `Exception`, sem `route_transition_failed`, sem `checkpointStatus='Failed'`; `RestartCurrentActivity`, `Activity01ToActivity02` e `RouteExitBackToMenu` passaram; `SessionResetCompleted sessionActorCount='0'` observado.



### Checkpoint SA-ACTOR-1C1-H8 â€” PlayerParticipation identity cleanup

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- H8A removeu `actorScope` editÃ¡vel do `PlayerSetDefinition`; player estrutural vindo de `PlayerParticipation` Ã© sempre `SessionScoped`.
- H8C1 mudou materialization seed resolution para `PlayerSlotId -> SessionParticipantId`, mantendo `ActorDefinitionId` apenas como consistÃªncia de definition/archetype.
- H8C2 tornou `SessionParticipantId` estÃ¡vel por `PlayerSlotId`, com `participantIdPolicy='PlayerSlotIdDerived'`.
- H8C3 removeu `ActorId` de `ActorDefinitionAsset` no fluxo de player; `PlayerSetDefinitionEntry.actorId` passou a ser o owner autoral temporÃ¡rio do ActorId default do participante.
- Confirmado por smoke: `actorIdSource='PlayerSetDefinitionEntry'`, `seedActorIdSource='PlayerSetDefinitionEntry'`, `resolutionKey='PlayerSlotIdToSessionParticipantId'`, `RestartCurrentActivity Passed`, `Activity01ToActivity02 Passed`, `RouteExitBackToMenu Passed`, `SessionResetCompleted sessionActorCount='0'`, sem `FATAL`, `Exception`, `route_transition_failed` ou `checkpointStatus='Failed'`.


### Checkpoint SA-13C1 â€” ActorAttribute command execution owner extraction

Status: CLOSED / PASS funcional.

- `TryApplyActorAttributeCommand` deixou de executar aplicaÃ§Ã£o concreta de `ActorAttributes` dentro do `SessionActivityPipeline`.
- A execuÃ§Ã£o passou a ser delegada ao `ActorAttributeEndpoint.TryApplyCommand(...)`.
- `SessionActivityPipeline` permanece apenas como orquestrador do momento do command.
- Smoke confirmou `Subtract` e `Add` aplicados no atributo do actor, com mudanÃ§a de valor observada.
- Checkpoints `RestartCurrentActivity`, `Activity01ToActivity02` e `RouteExitBackToMenu` preservados.

### Checkpoint SA-13C-OBJ1-FIX â€” ActivityObject exit correlation mirror

Status: CLOSED / PASS funcional + PASS arquitetural parcial.

- Corrigido `EXIT_CORRELATION_MISSING` entre entry setup de `ActivityObject` e stages de exit.
- A correlaÃ§Ã£o `ActivityObjectContributorDiscoveryResult + ActivityCapabilityInventory preview + validation` agora Ã© congelada no `ActivityObjectExitRuntimeState` apÃ³s `ExecuteSetupAndReadiness(...)` e antes de `EnterActivationFlow(...)`.
- `ActivityObjectSnapshotCapture`, `ActivityObjectRelease` e `ActivityObjectContributorUnregister` voltaram a consumir `test_object_01` em `activity_01`.
- `activity_02` continua no-content explÃ­cito, com zero targets e sem fallback.
- `RouteActivitySave` permanece em watchlist para payload Ãºtil quando o save-on-exit ocorrer apÃ³s activity com snapshot capturado.


