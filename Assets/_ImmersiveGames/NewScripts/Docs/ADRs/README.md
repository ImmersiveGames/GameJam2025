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

## Checkpoints Base 2.0

- `SessionOperational` permanece com checkpoint funcional/arquitetural parcial aceito na frente de ownership stabilization.
- `SessionActivity` fica congelado temporariamente em `SA-14E - SessionActivity decomposition closure matrix`.
- `SA-14B1` segue como o último corte runtime validado de `SessionActivity`.
- `SA-13D`, `SA-14C`, `SA-14D` e `SA-14E` ficaram fechados como auditorias/matriz de débitos.
- Movement, ActivityContent, RouteActivitySave e pending-operation callback path não devem ser reabertos sem regressão concreta.
- A próxima frente runtime deve ser escolhida fora de `SessionActivity`, salvo regressão.
- `ADR-2.0-0002` congelou a fronteira `ENTRY-BOUNDARY-DOC-0` de `SessionActivity`: `ResetEndpoint`, `SnapshotProvider`, `SnapshotRestoreEndpoint` e `ReleaseEndpoint` permanecem válidos no scanner; `ObjectEmitter` é local ao Actor; `PermissionTarget` fica limitado a referência mínima; `PresentationEndpoint`, `AttributeEndpoint` e `CameraTarget` pertencem a `ActorCapabilitySurface` e aos seus planos de setup/binding, não ao inventory transversal.
- `ADR-2.0-0002` também registra `ENTRY-BOUNDARY Closure â€” PASS funcional + PASS arquitetural parcial`: `ActivityGateBindingStage` é owner explícito do gate binding; Presentation/Attribute usam `SetupContributions`; Camera usa `BindingContributions`; `ObjectEmission` participa de `ActivityGameplayControl` por provider/contribution e não volta ao inventory; o próximo bloco pode iniciar runtime/pooling/audio sem reabrir inventory como behavior catalog.
- `ADR-2.0-0002` tambem registra `ACT-EMIT-2 â€” ObjectEmission Pool/Rent/Return MVP â€” PASS`: `FirePrimary` ja chega ao `ActorObjectEmitterEndpoint`; `ObjectEmissionRuntimeComposer`/`ObjectEmissionPoolRuntimeBridge` resolvem `IPoolService`; `ObjectEmissionPoolAdapter` e owner unico de `Rent`; `ObjectEmissionPoolReturnSink` e owner explicito de `Return`; `ObjectEmissionPooledObject` recebe payload e solicita retorno por sink; o MVP nao reabre scanner/inventory/setup/binding/gate nem adiciona audio/damage/collision/VFX.
- `SA-15C` fechou o `RouteActivitySave` com `RouteActivitySaveContributorScopePolicy` como policy normativa.
- `CurrentActivityObjectSnapshot` é o único scope funcional ativo hoje.
- `CurrentRouteSaveContributors` e `RouteAndActivitySaveContributors` permanecem como contrato/policy futura, sem infraestrutura ativa.
- `RouteActivitySave last useful payload` continua sendo policy futura explícita; não é bug local nem fallback implícito.

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

### Checkpoint SessionActivity Base 2.0 — SA-10 / SA-11B

- `SA-10 — Permission identity separation final`: `CLOSED / DOCUMENTATION ONLY`. Auditoria confirmou que `Permission` usa `ActorInstanceRuntimeId` como target funcional; `PlayerActorId` e `PlayerSlotId` permanecem observabilidade/log/fact/payload.
- `SA-11B — Fact recorder hygiene`: `CLOSED / PASS funcional + PASS arquitetural do corte`. O smoke mais recente confirmou o ordering correto: cleanup final antes de `ActivityContentReleaseCompleted`, com `pendingReleaseContextPresentAfter='false'`, `loadedSetPresentAfter='false'` e `awaitingContinuationAfter='false'`.
- `SA-11B-H2 — ActivityContentReleaseCompleted ordering fix`: `CLOSED / PASS funcional + PASS arquitetural do corte`.
- A ausência de `PlayerActorParticipationExitStageCompleted` em alguns smokes permanece aceita como caminho condicional: auditoria estática confirmou que o substage só executa quando `exitedPlayerActors.Count > 0`, e que o patch alinha fact/snapshot quando executado.



### Checkpoint SessionActivity Base 2.0 — SA-12 command hygiene

- `SA-12 — Commands e contracts finais`: `PARTIAL / IN PROGRESS`.
- `SA-12-AUDIT`: `AUDITED / NEEDS SMALL COMMAND HYGIENE PATCH`. Auditoria não encontrou `Action`, `Func<T>`, adapters, delegates de execução ou `SessionActivityRuntimeState` embutidos nos commands auditados; o débito é higiene de contract.
- `SA-12B/C — Command boundary + identity duplication cleanup`: `CLOSED / PASS funcional + PASS arquitetural do corte`.
  - `ActivityObjectContributorUnregisterStageCommand` não carrega mais `SessionActivityStage Stage`.
  - `ActivityObjectResetCommand`, `ActivityObjectReleaseCommand`, `ActivityObjectSnapshotRestoreCommand` e `ActivityContentSceneUnloadCommand` não duplicam mais `PipelineId`, `SessionStateId`, `ActivityId`, `ActivityOrdinal` e `EntrySequence` quando `SessionActivityIdentity` já é a fonte do ciclo.
- `SA-12D — ActorAttributeCommand typed identity`: `CLOSED / PASS funcional + PASS arquitetural do corte`.
  - `ActorAttributeCommand` passou a carregar `SessionActivityIdentity` e `ActorInstanceRuntimeId`.
  - Removidos do command contract os campos livres `string PipelineIdentity`, `string ActivityIdentity` e `string ActorInstanceId`.
  - Smoke preservou `ActorAttributeSetupStarted`, `ActorAttributeProfileResolved`, `ActorAttributeReady`, `ActorAttributeSetupCompleted`, `ActorAttributeReleased` e checkpoints macro.
- `SA-12E — ActivityContent SceneKeyAsset/runtime scene reference`: `CLOSED / PASS funcional + PASS arquitetural do corte`.
  - `ActivityEntryContentLoadCommand` passou a carregar `ActivityContentLoadPlan`.
  - `ActivityContentLoadedSceneRecord` e unload passaram a operar por runtime scene reference, sem `SceneKeyAsset` como payload runtime.
  - `activity_01` preserva `loadedScenes='1'`; `activity_02` preserva no-content/skip explícito.
- `SA-12F — Reduce SessionActivityDefinition from ActivityEntry*Command`: `PARTIAL / IN PROGRESS`.
  - Fechados: `SA-12F1A/B`, `SA-12F2`, `SA-12F3A`, `SA-12F3B`, `SA-12F3C`, `SA-12F4A`, `SA-12F4B`, `SA-12F4C`.
  - `ActivityEntryContentLoadCommand`, binding commands, ActorPresentation/ActorAttribute setup commands, ParticipantBinding e ObjectSetup deixaram de carregar `SessionActivityDefinition` nos subfluxos tratados.
  - `ActivityEntryObjectSetupCommand` foi dividido entre `ActivityObjectSetupInventoryPlan` e `ActivityObjectResetRestorePlan` e não carrega mais `SessionActivityDefinition`.
- `SA-12F-BLOCKER-MOVEMENT-ACTIVITY02`: `CLOSED / PASS funcional + PASS arquitetural parcial`.
  - Loading voltou a completar/esconder.
  - `activity_02` preserva no-content, mas projeta o `PlayerActor SessionScoped` retido para `ActivityParticipationContext`, capability inventory, PermissionTarget, PlayerInput, MovementBinding e MovementControl.
  - Smoke aceito: `ActivityParticipantRetainedBindingChosen`, `ActivityParticipationContextPrepared activityParticipants='1'`, `ActivityEntryPermissionTargetPreparationCompleted receivers='1'`, `PlayerMovementPermissionApplied state='Allowed'`, `MovementControlEnabled activityId='activity_02' affectedActors='1'`, `Activity01ToActivity02 PASS`.
  - Débito aceito: `SA-12F-MOV-H1 — Retained PlayerActor target projection ownership hygiene`; mover a projection bridge hoje em `SessionActivityPipeline` para `ActivityEntryPipeline` / `ActivityEntryActorInventoryStage` em corte futuro.
- Pendências de `SA-12`: `SA-12F5 — residual SessionActivityDefinition command hygiene`; `SA-12F-MOV-H1 — hygiene de ownership da projeção de PlayerActor SessionScoped retido`.
- `SA-11B` segue `CLOSED / PASS funcional + PASS arquitetural do corte`; `SA-12` permanece parcial até fechamento dos resíduos finais.

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
- `ACTOR-COMP-0C -> 0F` fechou a limpeza do `ActivityPlayerActorRegistry`: o registry ficou como índice técnico puro; compatibilidade de scope/reentry permanece em `ActivityActorScopeCompatibilityPolicy`; wrappers transitórios foram removidos do código ativo.

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

### Checkpoint SA-ACTOR-1C1 — ActorScope.SessionScoped structural lifetime

Status: CLOSED / PASS funcional.

- `PlayerParticipation` emite `ActorScope.SessionScoped` como invariant para player; `PlayerSetDefinition` não expõe mais `actorScope` editável.
- `PlayerActor` prefab não é owner de `ActorId`, `ActorScope` ou `ParticipationPolicy` runtime.
- `ActorScope.SessionScoped` decide lifetime estrutural do Actor, não lifetime automático de `Presentation`, `Attributes`, `Permission`, `Movement`, `Camera` ou outras capabilities.
- `SessionScoped + ActivityExit => Retain`.
- `SessionScoped + RouteExit => Retain`.
- `SessionScoped + ExitToMenu/SessionReset => Release`.
- `ExitToMenu` chama `SessionReset` canônico após `RouteExit`/save-on-exit.
- Smoke aceito: sem `FATAL`, sem `Exception`, sem `route_transition_failed`, sem `checkpointStatus='Failed'`; `RestartCurrentActivity`, `Activity01ToActivity02` e `RouteExitBackToMenu` passaram; `SessionResetCompleted sessionActorCount='0'` observado.



### Checkpoint SA-ACTOR-1C1-H8 — PlayerParticipation identity cleanup

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- H8A removeu `actorScope` editável do `PlayerSetDefinition`; player estrutural vindo de `PlayerParticipation` é sempre `SessionScoped`.
- H8C1 mudou materialization seed resolution para `PlayerSlotId -> SessionParticipantId`, mantendo `ActorDefinitionId` apenas como consistência de definition/archetype.
- H8C2 tornou `SessionParticipantId` estável por `PlayerSlotId`, com `participantIdPolicy='PlayerSlotIdDerived'`.
- H8C3 removeu `ActorId` de `ActorDefinitionAsset` no fluxo de player; `PlayerSetDefinitionEntry.actorId` passou a ser o owner autoral temporário do ActorId default do participante.
- Confirmado por smoke: `actorIdSource='PlayerSetDefinitionEntry'`, `seedActorIdSource='PlayerSetDefinitionEntry'`, `resolutionKey='PlayerSlotIdToSessionParticipantId'`, `RestartCurrentActivity Passed`, `Activity01ToActivity02 Passed`, `RouteExitBackToMenu Passed`, `SessionResetCompleted sessionActorCount='0'`, sem `FATAL`, `Exception`, `route_transition_failed` ou `checkpointStatus='Failed'`.


### Checkpoint SA-13C1 — ActorAttribute command execution owner extraction

Status: CLOSED / PASS funcional.

- `TryApplyActorAttributeCommand` deixou de executar aplicação concreta de `ActorAttributes` dentro do `SessionActivityPipeline`.
- A execução passou a ser delegada ao `ActorAttributeEndpoint.TryApplyCommand(...)`.
- `SessionActivityPipeline` permanece apenas como orquestrador do momento do command.
- Smoke confirmou `Subtract` e `Add` aplicados no atributo do actor, com mudança de valor observada.
- Checkpoints `RestartCurrentActivity`, `Activity01ToActivity02` e `RouteExitBackToMenu` preservados.

### Checkpoint SA-13C-OBJ1-FIX — ActivityObject exit correlation mirror

Status: CLOSED / PASS funcional + PASS arquitetural parcial.

- Corrigido `EXIT_CORRELATION_MISSING` entre entry setup de `ActivityObject` e stages de exit.
- A correlação `ActivityObjectContributorDiscoveryResult + ActivityCapabilityInventory preview + validation` agora é congelada no `ActivityObjectExitRuntimeState` após `ExecuteSetupAndReadiness(...)` e antes de `EnterActivationFlow(...)`.
- `ActivityObjectSnapshotCapture`, `ActivityObjectRelease` e `ActivityObjectContributorUnregister` voltaram a consumir `test_object_01` em `activity_01`.
- `activity_02` continua no-content explícito, com zero targets e sem fallback.
- `RouteActivitySave` permanece em watchlist para payload útil quando o save-on-exit ocorrer após activity com snapshot capturado.
  ACTOR-COMP-3D — Actor runtime identity cleanup
  Status: CLOSED / PASS funcional + PASS arquitetural.

### Checkpoint ActorInstanceRuntimeId tornou-se a identity runtime canônica para instâncias de Actor.
ActorInstanceId foi removido do runtime ativo.
A cadeia Actor/IActor, feed records, scan targets, scene-authored registry,
runtime states, command hub, permission e ObjectEmission passou a usar ActorInstanceRuntimeId.
Não houve factory inversa, compat alias, trilho paralelo, mudança de lifecycle,
scope, reentry, release ou retain.
Smoke preservou RestartCurrentActivity, Activity01ToActivity02 e RouteExitBackToMenu.
  ACTOR-COMP-3D â€” Actor runtime identity cleanup â€” CLOSED / PASS funcional + PASS arquitetural.
  ACTOR-COMP-4 â€” Actor role/archetype + specialization boundary â€” CLOSED / PASS funcional + PASS arquitetural.
---

## ACTOR-COMP-4 fechamento

- `ACTOR-COMP-4` está `CLOSED / PASS funcional + PASS arquitetural`.
- `ACTOR-COMP-4B` removeu o bridge de `PlayerMovementPermissionReceiverProvider` e limpou labels `non_player`.
- `ACTOR-COMP-4C` confirmou no-op; sem resíduos ativos de `NonPlayer` além da classe antiga naquele momento.
- `ACTOR-COMP-4D` concluiu que `NonPlayerActor` era marcador Unity serializado sem comportamento runtime próprio.
- `ACTOR-COMP-4E` criou `SceneAuthoredActor`, migrou prefabs e removeu `NonPlayerActor`.
- `ACTOR-COMP-4F` migrou `nonPlayerActorId` para `sceneActorId` com `[FormerlySerializedAs]` por preservação técnica de serialization Unity.
- `ACTOR-COMP-4G` renomeou `ActorDefinitionKind.NPC` para `ActorDefinitionKind.SceneActor`, preservando o valor numérico.
- `ACTOR-COMP-5A` fechou a fronteira Spawnable/Projectile como composition boundary.
- `ACTOR-COMP-5B` abriu a fronteira de lifecycle/reset para spawnable Actors.
- `ACTOR-COMP-5C` foi aplicado como shape passivo de Spawnable Actor, sem runtime wiring.
- `ACTOR-COMP-5D` foi aplicado como boundary passivo de command/result/payload para reset/release, sem runtime wiring.
- `ACTOR-COMP-5E` foi aplicado como boundary passivo de authoring/profile para spawnability, usando profile separado e sem migration ampla de assets.
- `ACTOR-COMP-5F` fechou a frente 5 como boundary conceitual/passiva pronta e `ACT-CMD-1B` foi aplicado como next runtime gate para FirePrimary readiness.
- Resíduos não bloqueantes continuam como authoring naming debt / asset naming debt: `NPC_Generic.prefab`, `NPC_Route_Generic.prefab`, `actor.presentation.npc.*`, `npc.attribute.*`, `npc.generic.01`, `npc.route.generic.01`, `ActorPresentationProfile_NpcGenerico*` e `NpcAttribute*`.
- Resumo: Spawnables de gameplay são Actors; Reset padrão de spawnable pooled é `ReturnToOriginPool`; pool é adapter técnico, não owner de lifecycle/policy.


### Checkpoint ACT-CMD-1D — ObjectEmission permission/gate isolation

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- `ActivityGameplayControl` registra somente o receiver de Movement.
- `ObjectEmission` não participa mais do gate ativo de Activity.
- `FirePrimary` permanece readiness passiva e rejeita dispatch por `missing_sink`.

### Checkpoint ACT-CMD-1E — ObjectEmission detached from Actor active capability surface

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- `ActorCapabilitySurface` não expõe mais `IActorObjectEmitterEndpoint`.
- `ActivityCapabilityPermissionScanner` não consulta nem registra receiver de ObjectEmission.
- `PlayerActor_v0` não carrega mais `ActorObjectEmitterEndpoint`.
- `ObjectEmission` permanece como bridge técnica transitória fora do caminho ativo de ActorCommand/ActivityGate.
- `FirePrimary` continua sem sink executável até existir capability runtime canônica.


### Checkpoint ACT-PROJ-0A — Projectile/Fire capability passive contracts + authoring boundary

Status: CLOSED / compile + smoke PASS / PASS arquitetural.

- Criados contratos passivos em `Actors/Projectile/Contracts/ActorProjectileContracts.cs`.
- Criado `ActorProjectileFireProfileAsset` como authoring passivo de fire modes.
- `FirePrimary` continua command semântico/readiness passivo; nenhum sink executável foi criado.
- Projectile fire exige `ActorSpawnabilityProfileAsset` com `RuntimeSpawned` e `ReturnToOriginPool`.
- Spawn patterns passivos: `Single`, `LinearBurst`, `RadialArc`.
- Não houve runtime de projectile, `ObjectEmission` wiring, pool, audio, collision, damage, VFX, lifetime, `IPoolService`, manager novo ou alteração de pipeline.
- Aceite: compile sem error CS; smoke não é obrigatório por ser corte passivo. Se rodar smoke, `FirePrimary` deve continuar `missing_sink`.


### Checkpoint ACT-PROJ-0B — Fire capability passive endpoint contract

Status: APPLIED / aguardando compile.

- Criado `Actors/Projectile/Contracts/ActorProjectileEndpointContracts.cs`.
- Adicionados `ActorProjectileFireEndpointId`, `ActorProjectileFireEndpointReadinessKind`, `ActorProjectileFireEndpointDescriptor`, `ActorProjectileFireEndpointReadiness` e `IActorProjectileFireEndpoint`.
- `IActorProjectileFireEndpoint` é contrato passivo: readiness + construção de command, sem execução de spawn.
- O endpoint contract não implementa `IActorCommandSink` e não foi registrado em `ActorCapabilitySurface`.
- `FirePrimary` continua `Prepared/passive-readiness-only` e deve continuar `missing_sink` em smoke.
- Não houve runtime de projectile, `ObjectEmission` wiring, pool, audio, collision, damage, VFX, lifetime, `IPoolService`, manager novo ou alteração de pipeline.
- Aceite: compile sem error CS; se rodar smoke, sem `ActorCommandSinkBound` para `FirePrimary` e sem `ObjectEmission` no gate/surface ativo.


### Checkpoint ACT-PROJ-0B — Fire capability passive endpoint contract

Status: CLOSED / compile + smoke PASS / PASS arquitetural.

- Criado `Actors/Projectile/Contracts/ActorProjectileEndpointContracts.cs`.
- `IActorProjectileFireEndpoint` permanece contrato passivo, sem implementação, sem `IActorCommandSink` e sem registro em `ActorCapabilitySurface`.
- Smoke preservou `FirePrimary` como `Prepared/passive-readiness-only` e `dispatchReason='missing_sink'`.
- Não houve runtime de projectile, `ObjectEmission` wiring, pool, audio, collision, damage, VFX, lifetime, `IPoolService`, manager novo ou alteração de pipeline.


### Checkpoint ACT-PROJ-0C — Projectile fire prefab authoring marker

Status: APPLIED / aguardando compile.

- Criado `Actors/Projectile/Authoring/ActorProjectileFireAuthoringMarker.cs`.
- `PlayerActor_v0.prefab` recebeu marcador passivo de authoring para `FirePrimary`/projectile fire.
- O marcador declara `endpointId`, `profileId`, `defaultFireModeId` e `acceptedCommandKind`.
- O marcador não implementa `IActorProjectileFireEndpoint`, não implementa `IActorCommandSink`, não registra capability e não é lido por pipeline neste corte.
- Nenhum YAML asset de projectile profile foi criado porque a base enviada não inclui metas de todos os scripts authoring antigos necessários para referências seguras.
- `FirePrimary` deve continuar `missing_sink` em smoke.

- `ACT-PROJ-1A`: aplicado como endpoint runtime de readiness para projectile/fire, usando referência tipada para `ActorProjectileFireProfileAsset`, sem `IActorCommandSink`, sem spawn, sem pool e sem `ObjectEmission`.

### Checkpoint ACT-PROJ-1B — Projectile fire command sink dry-run

Status: APPLIED / aguardando compile + smoke.

- `ActorProjectileFireEndpoint` passou a consumir `FirePrimary` como `IActorCommandSink` canônico da capability de fire.
- O sink é dry-run: monta `ActorProjectileFireCommand` e aceita o dispatch, mas não spawna, não chama pool e não instancia projectile.
- `ActorCommandBindingAdapter` só faz `BindCommandSink(FirePrimary, endpoint)` quando `ActorProjectileFireEndpointReadinessObserved` está `Prepared`.
- `FirePrimary` deve sair de `missing_sink` e gerar `ActorProjectileFireCommandBuilt`, `ActorProjectileFireDryRunAccepted` e `ActorCommandDispatchAccepted`.
- `ObjectEmission`, `IPoolService`, audio, collision, damage, VFX, lifetime, reset, save e pipelines permanecem fora do corte.

### Checkpoint ACT-PROJ-2A — Projectile spawn adapter boundary / no pool

Status: APPLIED / aguardando compile + smoke.

- Criado `ActorProjectileSpawnAdapterContracts.cs` com `IActorProjectileSpawnAdapter` e resultado explícito de adapter.
- Criado `ActorProjectileSpawnAdapterBoundary`, componente de fronteira que retorna `NotConfigured` sem executar spawn.
- `ActorProjectileFireEndpoint` agora monta `ActorProjectileFireCommand` e delega para a fronteira de spawn.
- `PlayerActor_v0.prefab` recebeu referência tipada do endpoint para `ActorProjectileSpawnAdapterBoundary`.
- `FirePrimary` deve continuar com sink executável e dispatch aceito, agora com `dispatchReason='projectile_spawn_adapter_not_configured'`.
- Não houve `ObjectEmission`, `IPoolService`, projectile prefab/runtime object, manager, pool, audio, collision, damage, VFX, lifetime, reset, save ou alteração de pipelines.

### Checkpoint ACT-PROJ-2B — Projectile spawn adapter uses canonical pool service

Status: APPLIED / aguardando compile + smoke.

- `ActorProjectileSpawnAdapterBoundary` passou a usar `IPoolService` canônico para `Rent`.
- `ActorSpawnabilityProfileAsset` agora tem referência tipada para `PoolDefinitionAsset`; `RuntimeSpawned`/`ReturnToOriginPool` executável exige essa referência.
- Criado `RuntimeSpawnedActor` como Actor concreto para prefabs spawnáveis de runtime.
- Criados `ProjectileActor_PlayerPrimary.prefab` e `PoolDefinition_PlayerPrimaryProjectile.asset`.
- `FirePrimary` deve gerar `ActorProjectileSpawnedFromPool`, `spawnExecuted='True'`, `poolCalled='True'` e `dispatchReason='projectile_spawned_from_pool'`.
- `ObjectEmission`, managers novos, collision, damage, VFX, audio, motion runtime, lifetime executor, reset/save e pipelines permanecem fora do corte.
### Checkpoint ACT-PROJ-2C — ObjectEmission legacy runtime deletion

- `ObjectEmissionRuntimeComposer` saiu do composition graph.
- `Actors/ObjectEmission/**` e assets autorais antigos de ObjectEmission devem ser removidos via `DELETE_FILES.txt`.
- `ActorProjectileFireEndpoint + ActorProjectileSpawnAdapterBoundary` permanecem como caminho ativo de `FirePrimary`.
- Próximo débito observado: `FirePrimary` ainda precisa de gate/policy para não executar fora de `ActivityRunning`.


### Checkpoint ACT-PROJ-2D — Projectile fire gameplay-state gate alignment

Status: CLOSED / PASS funcional + PASS arquitetural do corte.

- `ProjectileFire` agora participa do mesmo `ActivityGameplayControl` gate usado por Movement.
- `ActivityCapabilityPermissionScanner` registra `projectile_fire.receiver` quando encontra `IActorProjectileFireEndpoint` no `ActorCapabilitySurface`.
- `ActorProjectileFireEndpoint` mantém estado local de enabled/disabled e retorna `RejectedInactive` com `projectile_fire_endpoint_inactive` quando o gate está Blocked/Unbound.
- `ActorProjectileSpawnAdapterBoundary` continua como único ponto técnico de pool/rent.
- Não houve `ObjectEmission`, gate paralelo, permission runtime novo, alteração de pipeline ou limpeza ampla de IDs/string neste corte.

### Checkpoint ACT-PROJ-2E — Projectile MVP closure documentation

Status: CLOSED / DOCUMENTATION ONLY.

- Fechamento documental do MVP atual de ProjectileFire.
- Caminho congelado: `PlayerActorCommandInputHub -> ActorProjectileFireEndpoint -> ActorProjectileSpawnAdapterBoundary -> IPoolService -> RuntimeSpawnedActor`.
- `FirePrimary` usa `ActorCommandHub` e respeita `ActivityGameplayControl` via `projectile_fire.receiver`.
- `Movement` e `ProjectileFire` compartilham o mesmo gate de gameplay; o smoke de fechamento confirmou `ActivityGateBindingCompleted receivers='2'`.
- `FirePrimary` é rejeitado como `projectile_fire_endpoint_inactive` fora de `ActivityRunning`.
- `FirePrimary` spawna via pool canônico durante `ActivityRunning`, com `spawnExecuted='True'` e `poolCalled='True'`.
- `ObjectEmission` permanece removido do caminho ativo e não deve ser reintroduzido como bridge de FirePrimary.
- Débitos fora do MVP: typed refs/IDREF para command/projectile/pool, alinhamento transversal Movement/Fire, lifetime/return-to-pool automático, motion, collision, damage, VFX, audio e determinismo de spread.


### Checkpoint ACT-CMD-IDREF-1 — ActorCommand/InputMode ownership cleanup

Status: APPLIED / aguardando compile + smoke.

- Remove `ActorCommandSourceKind` do path ativo de ActorCommand.
- Remove `SourceKind`, `ActionMapName` e `ActionName` de `ActorCommandInputBinding`.
- `ActorCommandInputBinding` passa a exigir `InputActionReference` tipado.
- `PlayerActorCommandInputHub` resolve a action tipada contra o `PlayerInput.actions` ativo por `InputAction.id`.
- `InputModes` permanece owner de ActionMap ativo; `PlayerInputBindingAdapter` não chama `SwitchCurrentActionMap`.
- Configuração obrigatória no prefab: `move -> PlayerInputActions/Player/Move`, `fire_primary -> PlayerInputActions/Player/Fire`.

### Checkpoint ACT-PROJ-3A — Projectile spawn adapter runtime binding cleanup

Status: APPLIED / aguardando compile + smoke.

- Remove o componente técnico `ActorProjectileSpawnAdapterBoundary` do prefab do Player.
- `PlayerActor_v0.prefab` mantém apenas `PlayerActorCommandInputHub` + `ActorProjectileFireEndpoint` para o fluxo de tiro.
- `ActorProjectileFireEndpoint` não serializa mais referência para adapter de spawn.
- `PooledActorProjectileSpawnAdapter` passa a ser classe runtime não-MonoBehaviour.
- `ActorCommandBindingAdapter` configura `IActorProjectileSpawnAdapter` no endpoint e mantém `BindCommandSink(FirePrimary, endpoint)`.
- `IPoolService` continua sendo chamado apenas pelo adapter técnico.
- `ObjectEmission` permanece fora do caminho ativo.
