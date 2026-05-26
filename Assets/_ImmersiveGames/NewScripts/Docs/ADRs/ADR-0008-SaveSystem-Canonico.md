<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

# ADR-0008 - SaveSystem Canonical

## Status
- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 estabeleceu save como um dos módulos da composição, mas não formalizou seu contrato canonical em relação aos pipelines. Base 1.1 exige que save seja claramente posicionado como um adapter comandado pelos pipelines operacionais.

## Decisão

Adota-se o `SaveSystem` como módulo produtor de facts e executor de save commands via adapters.

## Decisao Congelada Base 1.1 (Save)

- Preferences e Progression sao scopes diferentes.
- Preferences persistem escolhas do jogador/sistema (audio, video, input bindings, layout, idioma/acessibilidade quando existirem).
- Owner de decisao de Preferences: `PreferencesRuntimePipeline`.
- Progression persiste evolucao de jogo (activities, objetos, run, posicoes/status/checkpoints/inventario e equivalentes).
- Progression nao tem owner unico generico.
- O pipeline dono do ciclo decide quando coletar, carregar ou persistir progression.
- Objetos/dominios produzem conteudo por contrato de snapshot/registro.
- Objetos/dominios nao chamam backend diretamente.
- `SaveRuntime` fornece API estavel e executor comum para persistencia de progression comandada.
- Backend/core de `SaveRuntime` e mutavel/substituivel.
- Backend atual pode ser PlayerPrefs apenas como backend tecnico provisorio.
- PlayerPrefs nao deve permanecer como write path direto de Preferences; quando usado, deve ficar atras de `SaveRuntime` como backend tecnico.
- `RouteActivitySave` permanece especifico de rota/activity no `SessionOperationalPipeline`.
- `SessionOperationalPipeline` nao vira owner generico de persistencia.
- `RunPipeline` sera owner de run save/continuity quando esse fluxo for materializado.
- Todo `Pipeline Command` de save deve carregar `Pipeline Identity`.
- Foreign/stale events devem ser rejeitados ou gerar skip explicito.
- Sem fallback silencioso.
- Config obrigatoria ausente e fail-fast.
- Nao manter dois write paths ativos.

## Checkpoint SaveRuntime API Base 1.1 - PASS estrutural

Superficie publica canonica de `ISaveService`:

```text
TryLoad(SaveAddress, out SaveResult, out reason)
TrySave(SaveRequest, out SaveResult, out reason)
TryDelete(SaveAddress, out SaveResult, out reason)
```

- `SaveAddress`, `SaveRequest` e `SaveResult` sao o contrato publico unico de persistencia.
- APIs publicas legadas baseadas em `SaveIdentity`, `SaveRecord` e `TrySaveCurrent` sairam da superficie ativa.
- `SaveIdentity` e `SaveRecord` podem existir apenas como detalhe tecnico interno do `SaveCoreService` e dos backends.
- `SaveCoreService` encapsula o mapeamento interno `SaveAddress -> SaveIdentity/SaveRecord` quando o backend tecnico ainda exigir esse formato.
- Metadados de endereco devem usar apenas `save.address.*`.
- Metadados divergentes como `preferences.address.*` nao fazem parte do contrato canonico.
- Nenhum pipeline ou adapter deve chamar save por `SaveIdentity`/`SaveRecord`.

## Checkpoint Save/Preferences Base 1.1 - PASS funcional com PlayerPrefsSaveBackend

Shape canonico ativo:

```text
PreferencesRuntimePipeline
-> PreferencesSaveAdapter
-> ISaveService / SaveRuntime
-> PlayerPrefsSaveBackend
```

- `PreferencesRuntimePipeline` e owner de decisao de Preferences:
    - load bootstrap;
    - preview;
    - commit;
    - restore defaults.
- `PreferencesSaveAdapter` e o Pipeline Adapter de persistencia de Preferences.
- `PreferencesSaveAdapter` usa a API canonica de `ISaveService` por `SaveAddress`/`SaveRequest`/`SaveResult`.
- `SaveRuntime` permanece API/executor comum.
- `PlayerPrefsSaveBackend` permanece backend tecnico provisorio, atras de `SaveRuntime`.
- `PreferencesService` fica restrito a estado/aplicacao runtime/defaults/presets.
- `IPreferencesBackend`, `IPreferencesSaveService` e `PlayerPrefsPreferencesBackend` sairam do caminho ativo.
- Nao existe backend proprio de Preferences ativo.
- Nao existe dual write path ativo em Preferences.
- `RouteActivitySave` permanece separado no `SessionOperationalPipeline`.
- `SessionOperationalPipeline` nao salva Preferences.
- Preferences nao usa `RouteActivitySave` nem `ProgressionSlotContext`.
- Sem fallback silencioso.
- Config/backend obrigatorio ausente permanece fail-fast.
- Smoke funcional validou o ciclo: defaults limpos -> commit de audio/video -> novo bootstrap carregando valores persistidos via `PlayerPrefsSaveBackend`.
- Progression funcional ainda nao foi implementada neste checkpoint e segue como contrato separado.

## Decisao Congelada Base 1.1 (Progression Save com slots e snapshots)

- Preferences e Progression sao scopes diferentes.
- Preferences nao usa slots de progressao.
- Progression usa slots e snapshots.
- `SaveSlot` e container logico de progressao.
- `SaveSnapshot` e captura versionada de estado.
- `CurrentSave` nao e slot fisico; e ponteiro para slot/snapshot ativo.
- `AutoSave`, `ManualSave` e `Checkpoint` sao Pipeline Policies, nao comportamento de backend.
- `SaveRuntime` nao decide slot.
- `SaveRuntime` nao decide snapshot.
- `SaveRuntime` apenas executa persistencia no endereco recebido.
- Pipelines decidem ou validam slot/snapshot antes de emitir Pipeline Command.
- `RouteActivitySave` usa `ProgressionSlotContext` resolvido, mas nao escolhe slot sozinho.
- `SaveConfigAsset.defaultSlotId` pode existir como config tecnica/legada, mas nao e policy canonica de Progression.

Conceitos previstos para Progression Save:

- `SaveSlotId`
- `SaveSlotKind`
- `SaveSnapshotId`
- `SaveSlotDescriptor`
- `SaveSnapshotHeader`
- `SaveSlotManifest`
- `ProgressionSlotContext`
- `ProgressionSnapshotEnvelope`
- `IProgressionSnapshotProvider`
- `IProgressionSnapshotReceiver`

Regra central canonica:

```text
Objeto/dominio produz snapshot.
Pipeline decide quando coletar, carregar ou persistir.
Adapter executa.
SaveRuntime persiste.
Backend armazena.
```

Protecoes obrigatorias:

- Todo `Pipeline Command` de Progression Save deve carregar `Pipeline Identity`.
- Comando foreign/stale nao pode aplicar nem sobrescrever progression ativa.
- `slotId`/`snapshotId` incompativel deve ser rejeitado ou gerar skip explicito, conforme policy.
- Ausencia de `ProgressionSlotContext` em fluxo obrigatorio deve ser fail-fast.
- Rota/activity nao save-eligible deve gerar skip explicito.

Progression Save Fase 1 / 1.1 / 1.2 - PASS estrutural, sem progressao funcional completa:

- Fase 1 criou contratos de slot/snapshot/contexto/envelope e encaixou `ProgressionSlotContext` de forma passiva no `RouteActivitySave`.
- Fase 1.1 introduziu `IProgressionSlotContextResolver` como fronteira explicita para resolver `ProgressionSlotContext` fora do core de `SaveRuntime`.
- `SessionOperationalPipeline` consome `ProgressionSlotContext` via resolver explicito, sem consultar diretamente `ISaveStateService.CurrentRecord`.
- `SaveConfigAsset.defaultSlotId` permanece apenas como seed tecnico/legado quando necessario, com observabilidade propria, sem virar policy canonica de Progression.
- Fase 1.2 fez `RouteActivitySave` usar a API nativa de `ISaveService` por `SaveAddress`/`SaveRequest`.
- `SessionOperationalActivitySaveAdapter` usa `SaveScope.Progression` e `SaveGroup.RouteActivity` no contrato semantico.
- Ainda nao ha `SaveSlotManifest`/`SaveSnapshotHeader` real como fonte de snapshotId canonico.
- Ha MVP funcional de provider/payload de objeto para `RouteActivitySave` (`test_object_01`) com capture/save/load/restore real de `Transform`; ainda nao ha receiver/restore generico de gameplay persistence.
- Ainda nao salvar actors;
- Ainda nao salvar world objects de forma geral; ha apenas MVP de snapshot de objeto de Activity para `test_object_01`;
- Ainda nao salvar inventory;
- Ainda nao implementar run save;
- Ainda nao criar UI de slots;
- Ainda nao criar autosave real;
- Ainda nao criar ProgressionManager;
- Ainda nao criar auto-scan global.

### 1. Componentes Canônicos do SaveSystem

#### SaveConfigAsset
- Configuração declarativa de save behavior.
- Não define ownership de quando salvar; apenas parâmetros declarativos de execução.
- Propriedade: qual save backend usar.
- Propriedade: slots, serialization strategy, etc.

#### SaveBackendAsset
- Define implementação concreta de persistence (local file, cloud, etc.).
- Implementa `ISaveBackend` interface.

#### ISaveBackend Interface
- Contrato tecnico para save backends.
- Métodos: `SaveAsync`, `LoadAsync`, `DeleteAsync`.
- Usado por `SaveCoreService` para executar operações.
- Pode continuar usando `SaveIdentity`/`SaveRecord` internamente enquanto esses modelos estiverem encapsulados pelo core.
- Nao e superficie publica para pipelines/adapters.

#### SaveCoreService
- Core service que coordena save/load operations.
- Interage com `SaveBackendAsset` via `ISaveBackend`.
- Nao decide quando salvar; executa o que foi comandado.
- Nao decide slot/snapshot/current save.
- Expoe persistencia via `SaveAddress`/`SaveRequest`/`SaveResult` por meio de `ISaveService`.
- Encapsula qualquer conversao tecnica para `SaveIdentity`/`SaveRecord`.
- Reporta completion/failure ao pipeline/adapter solicitante.

### 2. Save Pipeline Integration

#### SaveSystem as Adapter
- `SaveSystem` executa comandos de save/load vindos de pipelines.
- Os pipelines (`SessionOperationalPipeline`, `RunPipeline`, etc.) decidem quando e o quê salvar.
- Save não é owner de lifecycle; é executor de comandos.

#### SaveEligibility
- Decidida por `SceneRouteProfile` ou `RunPipeline` policy.
- Não inferida por `routeKind`.
- Explícita: rota pode ou não ser save-eligible.

#### Save Policies
- Decididas pelo pipeline, não pelo save module.
- Exemplo: "salvar ao mudar rota", "salvar ao completar run", etc.

### 2.1 Checkpoint SessionOperational (2026-05-14)

- `RouteActivitySavePlanReady` é plano/observabilidade, não execução.
- `loadActivitySaveOnEnter`:
    - executa no `SessionOperationalPipeline` após `SceneCompositionCompleted`;
    - ocorre antes de `InputCapability`, `PlayerPreparation` e handoff para `SessionActivityPipeline`;
    - ausência de snapshot salvo gera `RouteActivitySaveLoadSkipped skipReason='no_snapshot'`.
- `saveActivityOnExit` (Fase 2):
    - executa somente em troca de rota operacional;
    - decisão usa a rota anterior completa (não a rota atual);
    - executa antes de descarregar cena da rota anterior;
    - sem `Activity Snapshot Provider` canônico, gera `RouteActivitySaveSaveSkipped skipReason='no_snapshot_provider'`.
- `SessionOperationalActivitySaveAdapter` executa side-effect via `ISaveService`; não decide lifecycle.
- Ausência de adapter/config obrigatória permanece fail-fast.

### 2.2 Checkpoint CLOSED - RouteActivitySave Boundary (2026-05-17)

Boundary de `RouteActivitySave` no nível Session Operational marcado como **CLOSED**.

Contrato congelado neste checkpoint:

- `OperationalRouteAsset` declara policy explícita:
    - `loadActivitySaveOnEnter`
    - `saveActivityOnExit`
- `SessionOperationalPipeline` é owner de timing/policy:
    - `save-on-exit` da rota anterior antes do unload;
    - `load-on-enter` da rota atual após `SceneCompositionCompleted`.
- `IProgressionSlotContextResolver` resolve contexto operacional (`slotId`/`snapshotId`) para o trilho de rota/activity.
- `SessionOperationalActivitySaveAdapter` executa side-effect; não decide policy/lifecycle.
- `ISaveService`/`SaveRuntime` persiste por `SaveAddress`/`SaveRequest`; não decide lifecycle.
- `SessionActivity` não salva diretamente no trilho canônico.

Skips canônicos congelados:

- `no_snapshot`: skip explícito aceitável quando não há dado salvo para load-on-enter.
- `no_snapshot_provider`: skip explícito aceitável enquanto `Activity Snapshot Provider` canônico não existe.
- Esses skips são observabilidade explícita e **não** fallback silencioso.

Fora do escopo deste checkpoint:

- `Activity Snapshot Provider` real (`IProgressionSnapshotProvider`/`IProgressionSnapshotReceiver`);
- Save/Progression real completo (manifest/header reais, policies completas de auto/manual/checkpoint, UI de slots).


### 2.3 Checkpoint Progression Save MVP — Activity Object Snapshot Capture/Save/Load/Restore (2026-05-22)

Status:

```text
Progression Save MVP — Etapa 1 Capture-only = PASS funcional
Progression Save MVP — Etapa 2 Save-only = PASS funcional
Progression Save MVP — Etapa 3 Load-only = PASS funcional
Progression Save MVP — Etapa 4 Restore-only = PASS funcional e semântico
Progression Save MVP — Ordem SnapshotContractValidation -> ObjectReset = PASS funcional e semântico
```

Objetivo do checkpoint:

```text
Persistir, carregar e restaurar um snapshot mínimo de objeto de Activity,
sem implementar autosave, manual save, checkpoint save ou progression completo.
```

Objeto validado no MVP:

```text
test_object_01
```

Payload mínimo validado:

```text
schemaId = progression.route_activity.object_snapshot.v1
sessionStateId
activityId
entrySequence
objects[]
  targetId
  position
  rotation
  scale
```

#### Etapa 1 — Capture-only

O `SessionActivityPipeline` captura o snapshot de objeto como dado da entry, antes de `ObjectRelease` e antes de `ActivityContentRelease`.

Ponto de lifecycle congelado:

```text
ActivityDeactivated
-> ActivityObjectSnapshotCaptureStarted
-> ActivityObjectSnapshotCaptured
-> ActivityObjectSnapshotCaptureCompleted
-> ObjectReleaseStarted
-> ActivityContentReleaseStarted
```

Decisões congeladas:

1. Capture não salva.
2. Capture não chama `ISaveService`.
3. Capture não altera `SessionOperationalPipeline`.
4. Capture não implementa load.
5. Capture não implementa restore.
6. Capture não implementa autosave.
7. O objeto/provider apenas produz snapshot.
8. O pipeline decide quando capturar.
9. O snapshot capturado carrega `Pipeline Identity` e `entrySequence`.
10. Capture deve ocorrer antes de `ObjectRelease`, pois depois do release/unload o objeto pode não existir mais.

Evidência funcional congelada:

```text
checkpoint='ActivityObjectSnapshotCapture'
checkpointStatus='Passed'
activityId='activity_01'
entrySequence='1'
captureStarted='true'
capturedCount='1'
targetIds='test_object_01'
hasTransformPayload='true'
captureCompleted='true'
captureFailed='false'
```

#### Etapa 2 — Save-only

O `SessionOperationalPipeline`, no trilho `RouteActivitySave save-on-exit`, resolve o payload capturado pela `SessionActivity` e persiste via adapter existente.

Shape congelado:

```text
SessionActivityPipeline
-> ISessionActivitySnapshotPayloadProvider read-only
-> SessionOperationalPipeline / RouteActivitySave save-on-exit
-> SessionOperationalActivitySaveAdapter
-> ISaveService / SaveRuntime
-> backend tecnico configurado
```

Decisões congeladas:

1. `SessionActivityPipeline` expõe payload capturado como read-only.
2. `SessionActivityPipeline` não salva.
3. Objeto/provider não salva.
4. `SessionOperationalPipeline` decide `save-on-exit` pela policy da rota anterior.
5. `SessionOperationalActivitySaveAdapter` executa persistência.
6. `SaveRuntime` persiste no endereço recebido.
7. Ausência de payload mantém skip explícito; não há fallback silencioso.
8. `SessionOperationalActivitySaveAdapter` continua usando `SaveAddress`/`SaveRequest`/`ISaveService`.
9. Falha de capture/config obrigatória é falha semântica (`RouteActivitySaveCaptureFailed`), não `no_snapshot_provider`.
10. Não há autosave/manual/checkpoint save nesta etapa.

Evidência funcional congelada:

```text
RouteActivitySnapshotPayloadResolved
activityIdentity='SessionActivitySandboxSession'
sourceActivityId='activity_01'
sourceEntrySequence='1'
payloadObjectCount='1'
targetIds='test_object_01'
schemaId='progression.route_activity.object_snapshot.v1'
payloadSize='292'

checkpoint='RouteActivitySaveSnapshotPayload'
checkpointStatus='Passed'
payloadResolved='true'
payloadObjectCount='1'
targetIds='test_object_01'

RouteActivitySaveSaveCompleted
requestAddress='scope='Progression' group='RouteActivity' ownerId='SessionActivitySandboxSession' recordId='snapshot-rev-0' slotId='save' schemaId='progression.route_activity' schemaVersion='1''
```

#### Etapa 3 — Load-only

O `SessionOperationalPipeline`, no trilho `RouteActivitySave load-on-enter`, carrega o payload salvo e o mantém como dado pending/read-only.

Shape congelado:

```text
SessionOperationalPipeline / RouteActivitySave load-on-enter
-> SessionOperationalActivitySaveAdapter
-> ISaveService / SaveRuntime
-> LoadedSessionActivitySnapshotPayload pending/read-only
```

Decisões congeladas:

1. Load lê payload salvo.
2. Load valida schema `progression.route_activity.object_snapshot.v1`.
3. Load mantém payload como dado pending/read-only.
4. Load não aplica restore.
5. Load não altera `Transform`.
6. Load não chama receiver.
7. Load não altera `ObjectReset`.
8. Load não altera `Placement`.
9. Load não implementa autosave/manual/checkpoint save.
10. Payload inválido deve falhar explicitamente; não pode virar payload vazio silencioso.

Evidência funcional congelada:

```text
ProgressionSlotContextResolved
snapshotId='snapshot-rev-0'

RouteActivitySaveLoadStarted
snapshotId='snapshot-rev-0'

RouteActivitySnapshotPayloadLoaded
activityIdentity='SessionActivitySandboxSession'
sourceActivityId='activity_01'
sourceEntrySequence='1'
payloadObjectCount='1'
targetIds='test_object_01'
schemaId='progression.route_activity.object_snapshot.v1'
payloadSize='292'

checkpoint='RouteActivitySaveSnapshotLoad'
checkpointStatus='Passed'
payloadLoaded='true'
payloadObjectCount='1'
targetIds='test_object_01'

RouteActivitySaveLoadCompleted
requestAddress='scope='Progression' group='RouteActivity' ownerId='SessionActivitySandboxSession' recordId='snapshot-rev-0' slotId='save' schemaId='progression.route_activity' schemaVersion='1''
```

#### Etapa 4 — Restore-only funcional e semântico

O `SessionActivityPipeline`, durante o setup da Activity, aplica o payload previamente carregado pelo `SessionOperationalPipeline` como pending/read-only.

Ponto de lifecycle congelado:

```text
ActivityContentLoad
-> ActivityObjectContributorDiscovery
-> ActivitySetupInventory
-> ActivityObjectSnapshotContractValidation
-> ObjectReset
-> ActivityObjectSnapshotRestore
-> ActivitySetupCompleted
-> ActivationWindow
```

Decisões congeladas:

1. `SessionOperationalPipeline` carrega payload e mantém dado pending/read-only.
2. `SessionOperationalPipeline` não aplica `Transform`.
3. `SessionActivityPipeline` decide timing do restore.
4. `ActivityObjectSnapshotContractValidation` roda antes de `ObjectReset`.
5. Restore roda depois de `ObjectReset`.
6. Endpoint de objeto apenas aplica estado comandado.
7. Endpoint não chama `SaveRuntime`.
8. Objeto não decide lifecycle, load, save, restore ou release.
9. `targetTransform` é binding obrigatório e explícito para provider/restore endpoint.
10. Capability quebrada falha em `ActivityObjectSnapshotContractValidation`, antes de `ObjectReset` e antes de `ActivityRunning`.
11. Restore sem payload carregado é `Skipped`, não `Passed`.
12. Restore funcional só passa quando há restore aplicado e `restoreVerified=true`.

Evidência funcional e semântica congelada:

```text
checkpoint='ActivityObjectContributorDiscovery'
checkpointStatus='Passed'

checkpoint='ActivityObjectSnapshotContractValidation'
checkpointStatus='Passed'
validationStarted='true'
validatedCount='1'
failedCount='0'
targetIds='test_object_01'
mismatchReason='<none>'

checkpoint='ActivityObjectReset'
checkpointStatus='Passed'
resetGroups='TransformState'

// primeira entrada sem payload carregado
checkpoint='ActivityObjectSnapshotRestore'
checkpointStatus='Skipped'
payloadAvailable='false'
restoredCount='0'
restoreCompleted='true'
restoreFailed='false'

// segunda entrada com payload carregado
checkpoint='ActivityObjectSnapshotRestore'
checkpointStatus='Passed'
payloadAvailable='true'
payloadObjectCount='1'
matchedTargetCount='1'
restoredCount='1'
targetIds='test_object_01'
beforePosition='(960,540,0)'
payloadPosition='(2,3,0)'
afterPosition='(2,3,0)'
restoreVerified='true'
restoreFailed='false'
```

#### SnapshotContractValidation

O contrato de snapshot é validado no `ActivityEntryPipeline`, antes de `ObjectReset`.

Regras congeladas:

- Provider/restore endpoint existente exige `targetTransform`.
- Não há fallback para `this.transform`.
- Não há busca por nome/tag/singleton.
- Provider e restore endpoint do mesmo `targetId` devem apontar para o mesmo `targetTransform`.
- Capability quebrada não pode virar `SkippedOptional`.
- `SkippedOptional` só vale quando contributor opcional não declara capability de snapshot.
- Falha obrigatória bloqueia `ObjectReset`, `ActivationWindowReady` e `ActivityRunning`.

#### Semântica de checkpoint e falha

Semântica congelada:

```text
ActivityObjectSnapshotRestore Skipped = sem payload carregado, sem target matching ou nenhum restore aplicado.
ActivityObjectSnapshotRestore Passed = payload carregado, target matching, restore aplicado e restoreVerified=true.
ActivityObjectSnapshotRestore Failed = restoreVerified=false, endpoint falhou, mismatch de targetTransform ou restore obrigatório inválido.
RouteActivitySaveSnapshotPayload Passed = payload resolvido e serializado.
RouteActivitySaveSnapshotPayload Waiting = ausência aceitável de provider/payload.
RouteActivitySaveSnapshotPayload Failed = snapshot_capture_failed ou falha semântica de producer da Activity anterior.
```

`no_snapshot_provider` só significa ausência do provider global/read-only.
`snapshot_capture_failed` não pode ser mascarado como provider ausente.

#### Ownership do MVP funcional

```text
Object/provider
-> produz snapshot e aplica restore local comandado

SessionActivityPipeline
-> valida contrato
-> captura snapshot no rail de saída
-> aplica restore no setup

SessionOperationalPipeline
-> decide RouteActivitySave load/save por policy de rota

SaveRuntime
-> persiste por SaveAddress/SaveRequest
-> não decide lifecycle nem aplica estado em objetos
```

#### CurrentSnapshotId / recordId / revision

Correção congelada neste checkpoint:

```text
CurrentSnapshotId aponta para SaveAddress.recordId efetivamente salvo.
SaveResult.revision é metadado técnico e não é snapshotId.
RouteActivitySave load-on-enter usa CurrentSnapshotId.
```

Root cause corrigido:

```text
O resolver montava snapshotId com revision: snapshot-rev-{CurrentState.Revision}.
Após save-on-exit, revision mudava e o próximo load procurava outro recordId.
Isso gerava record_id_mismatch e no_snapshot.
```

Contrato congelado:

1. `SaveCurrentState.CurrentSnapshotId` é o ponteiro lógico para o snapshot atual.
2. `CurrentSnapshotId` deve receber o `SaveAddress.recordId` salvo.
3. `SaveResult.revision` não pode ser usado como `snapshotId`.
4. `revision` permanece metadado técnico do registro salvo.
5. `DefaultProgressionSlotContextResolver` usa `CurrentSnapshotId` quando disponível.
6. Fallback legado para `snapshot-rev-{revision}` é transitório e só aceitável enquanto não houver ponteiro salvo.
7. O contrato final deve evoluir para `SaveSlotManifest`/`SaveSnapshotHeader` reais.

Evidência funcional congelada:

```text
Save: recordId='snapshot-rev-0'
Next load: snapshotId='snapshot-rev-0'
RouteActivitySaveSnapshotLoad checkpointStatus='Passed'
```

#### Restore genérico e autosave continuam fora do escopo

Este checkpoint implementa restore mínimo de `Transform` para `test_object_01`, mas não implementa:

```text
ObjectRestore genérico além do MVP
ProgressionRestore genérico além do MVP
IProgressionSnapshotReceiver genérico
AutoSave
ManualSave
CheckpointSave
UI de slots
SaveSlotManifest real
SaveSnapshotHeader real
ProgressionManager
```

Regra futura congelada para Restore genérico:

```text
Load pode ocorrer no SessionOperationalPipeline.
Restore pertence ao ActivityEntryPipeline ou stage equivalente de Activity setup. O MVP atual validou essa regra para `test_object_01`.
Restore de TransformState não pode rodar antes de Placement e ObjectReset,
pois Placement/ObjectReset podem sobrescrever o estado salvo.
```

Ordem futura recomendada para restore de objeto:

```text
ActivityContentLoad
-> ActivityObjectContributorDiscovery
-> ActivitySetupInventory
-> Placement
-> ObjectReset
-> ProgressionRestore
-> CameraBinding / InteractionBinding / HudBinding
-> ActivitySetupCompleted
-> ActivationWindow
```

Para expansão futura, a policy preferida é:

```text
ObjectReset roda primeiro.
ProgressionRestore roda depois e vence para grupos salvos.
```

Qualquer alternativa como “ObjectReset pula grupos restaurados” deve ser definida por policy explícita posterior.


### 2.4 Checkpoint de triagem — Progression Save real fora do escopo ativo agora (2026-05-22)

Status:

```text
Progression Save real = FORA DE ESCOPO ATIVO AGORA.
SaveRuntime = PASS estrutural / capacidade técnica existente.
RouteActivitySave + ActivityObjectSnapshotRestore MVP = PASS funcional e semântico.
Progression funcional completa = futura, condicionada a jogo/progressão real.
```

Evidência de código atual:

```text
SaveRuntime/Contracts/ISaveService.cs
SaveRuntime/Core/SaveCoreService.cs
SaveRuntime/Models/SaveAddress.cs
SaveRuntime/Models/SaveRequest.cs
SaveRuntime/Models/SaveResult.cs
SaveRuntime/Models/ProgressionSlotContext.cs
SaveRuntime/Models/ProgressionSnapshotEnvelope.cs
SaveRuntime/Models/SaveSlotManifest.cs
SaveRuntime/Models/SaveSnapshotHeader.cs
SaveRuntime/Contracts/IProgressionSnapshotProvider.cs
SaveRuntime/Contracts/IProgressionSnapshotReceiver.cs
SaveRuntime/Backends/PlayerPrefs/PlayerPrefsSaveBackend.cs
SaveRuntime/Persistence/Bootstrap/SaveInstaller.cs
```

Classificação:

```text
Não é déficit ativo da Base 1.1 agora.
É capacidade técnica preparada + MVP funcional limitado.
Progression Save real não deve ser implementado por ansiedade arquitetural.
```

Decisão congelada:

- Não implementar `ProgressionManager` agora.
- Não implementar UI de slots agora.
- Não implementar autosave/manual/checkpoint save real agora.
- Não implementar providers/receivers genéricos agora.
- Não implementar save de actors, inventory, world objects ou run agora.
- Não expandir `SaveSlotManifest`/`SaveSnapshotHeader` para fluxo funcional real agora.
- Não criar auto-scan global de providers/receivers.
- Não transformar `RouteActivitySave` em owner genérico de Progression.
- Não transformar `SaveRuntime` em owner de slot/snapshot/lifecycle.
- Não introduzir fallback silencioso quando não houver progressão concreta a salvar.

Regra de retorno ao tema:

```text
Progression Save real só volta ao escopo quando houver gameplay/progressão concreta a preservar.
```

Exemplos de gatilhos válidos para retomar:

```text
inventário real
objetivos persistentes
desbloqueios
posição/estado de actors entre sessões
estado de mundo persistente
seleção real de slot/current save pelo jogador
continuidade de run materializada
```

Até lá, o contrato ativo é:

```text
SaveRuntime fornece API estável e backend técnico.
RouteActivitySave mantém MVP de Activity snapshot quando a rota declarar load/save.
SessionActivityPipeline decide capture/restore timing.
SessionOperationalPipeline decide load/save de rota.
Objetos/domínios só entram quando houver provider/receiver concreto e necessidade real.
```


### 3. Invariantes

- `SaveCoreService` não decide quando salvar.
- Backend é intercambiável; `ISaveBackend` define contrato.
- `SaveConfigAsset` é declarative, não imperative.
- Save failure deve ser reportado, não silenciado.
- Foreign/stale save commands são ignorados.
- Ausência de backend configurado é erro fail-fast.
- Sem fallback silencioso.
- Nao manter dois write paths ativos para o mesmo scope de dado.

### 3.1 Mapa Canonico Base 1.1

| Tipo de dado | Owner canonico | Pipeline/adapter responsavel | Executor/backend | Observacao/acao futura |
|---|---|---|---|---|
| Preferences (audio/video/input/layout/idioma/acessibilidade) | `PreferencesRuntimePipeline` | `PreferencesRuntimePipeline` (decide) + `PreferencesSaveAdapter` (executa persistencia por `SaveAddress`/`SaveRequest`) + binders apenas como intencao UI | `SaveRuntime` (`ISaveService`) + `PlayerPrefsSaveBackend` (provisorio) | PASS funcional: defaults -> commit -> reload persistido via PlayerPrefs |
| Route Activity Save | `SessionOperationalPipeline` (scope de rota/activity) | `RouteActivitySave` + `SessionOperationalActivitySaveAdapter` usando `ProgressionSlotContext` resolvido + payload read-only capturado por `SessionActivityPipeline` quando existir | `SaveRuntime` (`ISaveService` por `SaveAddress`/`SaveRequest` + backend configurado) | PASS funcional MVP: capture-only, save-only, load-only e restore-only de `test_object_01`; autosave ainda fora do escopo |
| Run Save / Continuity | `RunPipeline` (quando materializado) | `RunPipeline` + adapter de save do dominio run | `SaveRuntime` (backend substituivel) | Owner ainda futuro, mas ja congelado no trilho canonico |
| Progression de objetos/dominios | Pipeline dono do ciclo correspondente | Pipeline do ciclo + adapter de save do dominio | `SaveRuntime` (backend substituivel) | Dominios produzem snapshot/registro; dominio nao chama backend direto |

## Consequências

- Save deixa de ser espalhado em points of interest.
- Decisão fica no pipeline; execução em `SaveCoreService`.
- Backend é plugável e testável.
- Save eligibility é explícita e rastreável.
- Falhas de save são observáveis.

## Roadmap Futuro

- Melhorar observabilidade de `captureTargetTransformPath` no payload restaurado.
- Manter `SaveSlotManifest` e `SaveSnapshotHeader` reais como capacidade prevista; só torná-los fluxo funcional quando houver progressão concreta e seleção real de slot/snapshot.
- Expandir providers/receivers de Progression apenas a partir de casos concretos de gameplay/progressão real, sem transformar objeto/domínio em owner de save e sem implementar autosave sem policy explícita.
- Definir owner de `CurrentSave` completo fora do core de `SaveRuntime` somente quando houver fluxo real de slots/current save.
- Evoluir Run save/continuity pelo `RunPipeline` apenas quando esse fluxo for materializado.
- Considerar backend robusto futuro substituindo `PlayerPrefsSaveBackend`, sem alterar a API publica de `ISaveService`.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 estabeleceu save como módulo, mas deixou policy dispersa.
- Base 1.1 formaliza save como adapter executado por pipelines.
- Base 2.0 futura pode extrair save patterns se a Base 1.1 continuar provando estabilidade.

---

## Decisao Congelada de Ownership (Persistencia de Escolhas Runtime)

- Escolhas runtime de jogador pertencem ao produtor correto (Activity, selecao, profile/loadout, objeto de dominio ou sistema especifico).
- Inicializacao de sessao nao vira owner generico de persistencia dessas escolhas.
- Save continua executor comandado pelos owners corretos via `Pipeline Command` e `Pipeline Policy`.
- Preferences permanece fora de `RouteActivitySave`.
