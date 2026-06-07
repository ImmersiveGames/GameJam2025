# ADR-1.2-0005 — SessionActivityPipeline Decomposition e Capability Stages

Status: Accepted / Base 1.2  
Área: SessionActivityPipeline / Pipeline Stages / Actor Capabilities  
Atualização: 4D + H4D Hygiene + ActorReset-1B — CLOSED / PASS funcional

> Historical Base 1.2 reference. For the active Base 2.0 boundary, see `ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md` and `ENTRY-BOUNDARY-DOC-0` inside it. Any wording that says stages "use inventory" is historical and does not authorize inventory-backed setup/binding in Base 2.0.

---

## Contexto

Durante a Base 1.2, houve risco de o `SessionActivityPipeline` virar owner de comportamento local de Actor. A intenção correta é outra:

- pipeline decide lifecycle, ordem, readiness, setup/release e handoff;
- endpoints/capabilities executam efeitos locais;
- ações locais de gameplay não devem subir para o pipeline quando podem ser resolvidas localmente;
- QA/debug pode solicitar comandos canônicos, mas não vira owner de lifecycle.

A decomposição também reduziu trechos internos do `SessionActivityPipeline` sem alterar o owner do ciclo. O pipeline continua coordenando o fluxo, enquanto stages/adapters/endpoints executam partes específicas.

---

## Decisão

### 1. Pipeline decide ciclo, não comportamento local

O `SessionActivityPipeline` é owner de:

- entry;
- setup;
- reset;
- placement;
- camera/movement/input binding;
- participation;
- release;
- snapshot;
- route-exit/restart;
- readiness para reveal.

O pipeline não deve virar dispatcher de qualquer ação local de gameplay.

### 2. Capability stages usam inventory

Stages que dependem de objetos/actors consumiam `ActivityCapabilityInventory` no corte histórico da Base 1.2 quando o caminho já havia sido migrado. Isso não descreve o shape ativo da Base 2.0.

Caminhos migrados historicamente:

- ActorPresentation setup/release;
- ActorAttributes setup/release;
- CameraTarget scanner;
- PermissionTarget scanner;
- ActorParticipation enter/exit.

Caminhos com boundary explícito:

- `ActivityObjectReset` usa `ActivityResetStage` e pode construir snapshot local transitório de inventory para a entry corrente;
- esse snapshot local de reset não substitui o preview canônico persistido em `_state.CurrentActivityCapabilityInventoryPreview`;
- `ActivityCapabilityInventoryPreviewStage` continua dono do preview canônico observado por stages posteriores.

### 3. Participation é genérico no caminho nominal

`NonPlayerActorParticipationStage` foi removido.

O caminho nominal é `ActorParticipation`, com observabilidade per-actor e aggregates.

### 4. Completed observável é obrigatório

Stages relevantes devem emitir completion observável:

- `ActorPresentationSetupCompleted`;
- `ActorPresentationReleaseCompleted`;
- `ActorAttributeSetupCompleted`;
- `ActorAttributeReleaseCompleted`;
- `ActorParticipationEnterCompleted`;
- `ActorParticipationExitCompleted`.

### 5. Skip explícito é parte do contrato

Ausência opcional ou transição ainda não suportada deve aparecer como skip explícito, não fallback silencioso.

Exemplo aceito atualmente:

- `transitional_non_registry_actor` para PlayerActor fora da registry formal de ActorParticipation.

### 6. ActorReset é comando/capability neutro

O contrato canônico de reset de Actor usa shape neutro:

- `ActorResetCommand`;
- `ActorResetResult`;
- `ActorResetGroup`;
- `ActorResetTargetRef`;
- `ActorResetContext`;
- `IActorResetEndpoint`;
- `IActorResetAdapter`.

`PlayerActorReset*` não é contrato canônico e não deve voltar como rail paralelo.

`ActorResetAdapter` é executor neutro. Ele não cria endpoint via runtime, não injeta `AddComponent`, não conhece `PlayerReset`/`NonPlayerReset` e não decide lifecycle.

O endpoint concreto atual do Player é:

- `PlayerActorDefaultResetEndpoint`, implementando `IActorResetEndpoint`.

### 7. QA probe de reset individual é permitido apenas como comando explícito

`SessionActivityDebugPanel` pode expor botão QA para disparar reset individual do actor atual, desde que:

- use `ActorResetCommand`;
- valide `ActivityRunning`;
- valide `Pipeline Identity`, `activityId` e `entrySequence` ativos;
- execute pelo caminho canônico `IActorResetAdapter` + `IActorResetEndpoint`;
- emita observabilidade explícita:
  - `ActorResetQaRequested`;
  - `ActorResetQaApplied`;
  - `ActorResetQaRejected`.

Esse caminho QA não é owner de lifecycle e não substitui `ActivityEntryPipeline`.

---

## Proibições

Não criar:

- stage separado por `PlayerActor`, `NonPlayerActor`, `EnemyActor` etc. quando o domínio já é Actor;
- switch funcional por `ActorKind`;
- fallback por string/path/scene;
- compat paralelo;
- novo core genérico fora do escopo Base 1.2;
- `ResetManager`;
- `EventBus`/broadcast global para reset;
- `PlayerReset` ou `NonPlayerReset` como contratos paralelos;
- `AddComponent<PlayerActorDefaultResetEndpoint>()` em runtime.

---

## Checkpoint ActorReset-1 + ActorReset-1B

Congelado como PASS funcional:

- `ActorReset*` é contrato canônico neutro;
- `PlayerActorDefaultResetEndpoint` expõe `IActorResetEndpoint` no prefab `PlayerActor_v0`;
- prefab obrigatório ausente falha explicitamente, sem fallback silencioso;
- `ActorResetAdapter` permanece neutro;
- reset individual QA executa `ActorResetCommand`;
- `ActorResetQaApplied` observado em `ActivityRunning`;
- smoke normal preservado após QA reset;
- `RestartCurrentActivity`, `Activity01ToActivity02` e `RouteExitBackToMenu` preservados.

Não congelado como shape final:

- botão QA ainda é orientado ao `Current Player Actor`;
- resolver concreto ainda depende de registry/player identity transitório;
- policy/registry genérica de ActorParticipation incluindo PlayerActor fica para fase futura.

---

## Débitos

- Migrar PlayerActor para policy/registry genérica de ActorParticipation.
- Reduzir registries transitórios.
- Generalizar resolver de ActorReset para `ActorInstanceId`/`ActorInstanceRecord` quando o contrato final existir.
- Revisar tamanho do `SessionActivityPipeline` em fase própria sem mudar ownership.
