# ADRs - Base 1.1

Este diretório mantém o acervo de ADRs e sua precedência normativa.

## Base 1.1 Viva - Fonte Normativa Atual

A partir da reorganização de Base 1.1, estes ADRs são a **única fonte normativa** para decisões arquiteturais:

1. **ADR-0001** - Base 1.1: Pipeline Convergence, Identidade Explícita e Isolamento contra Foreign Events
2. **ADR-0002** - Run Pipeline Canonical e Deactivation/Continuity
3. **ADR-0003** - Session Operational Pipeline e Session Transition Envelope
4. **ADR-0004** - Session Activity Pipeline
5. **ADR-0005** - Modules Produzem Facts/Commands, Adapters Executam Side-Effects
6. **ADR-0006** - Route, Scene Composition, Fade, Loading e Audio Adapters
7. **ADR-0007** - Gates, InputModes e Simulation Executors
8. **ADR-0008** - SaveSystem Canonical

## ADRs Checkpoints Normativos Aceitos/Congelados (Base 1.1 Viva)

9. **ADR-0009** - Session Player Slots e Operational Input Runtime (CONGELADO - 2026-05-14)
10. **ADR-0010** - Player Preparation Flow, Player Slots e Unity PlayerInput (CLOSED - 2026-05-17)
11. **ADR-0011** - Runtime Configuration Registry and Config Sets (CLOSED - 2026-05-17)
12. **ADR-0012** - Operational Camera Runtime e Future Activity Camera Binding (CONGELADO - 2026-05-14)
13. **ADR-0013** - Camera Presentation Runtime e Activity Camera Director (ACEITO / IMPLEMENTADO NO MVP SINGLE-PLAYER)

**Estes ADRs (0009-0013) são fonte normativa Base 1.1 no mesmo nível do ADR-0001 a 0008. Não são "complementares".**

Notas:
- **ADR-0009** (congelado) congela o contrato operacional de:
  - slots e validacao de PlayerInputManager;
  - inicializacao de EventSystem persistente;
  - inicializacao de InputSystemUIInputModule persistente;
  - binding canonico de 10 UI actions via `OperationalInputRuntimeProfileAsset` referenciado por `InputModesRuntimeConfigGroup`.
  - checkpoint 2026-05-21: `InputRuntimeRoot` em `UIGlobalScene` como root persistente obrigatório de input operacional, contendo `PlayerInputManager`, `EventSystem` e `InputSystemUIInputModule`; `NewBootstrap` não é owner desse runtime.
  - `SessionOperationalInputPolicy` explicita por rota operacional; `OperationalSurfaceKind` permanece semantico e nao decide input mode.
  - pipeline resolve policy -> mode e emite `SessionOperationalInputModeCommand`; `InputModes` aplica modo/action map.
  - 11 decisoes congeladas sobre fail-fast, integridade, sequencia de binding.
- **ADR-0010** (CLOSED / atualizado) depende de ADR-0009 para validacao/init de slots e input operacional, permanece focado em PlayerPreparation (somente players) como **intencao/payload para handoff**, sem materializacao Unity de player no `SessionOperationalPipeline`.
- **ADR-0011** (CLOSED - 2026-05-17):
  - `RuntimeModeConfig` permanece entrada canônica
  - `RuntimeConfigSetAsset` agrupa configs por domínio
  - `RuntimeConfigRegistry` valida e expõe snapshots read-only
  - 6 grupos obrigatórios: RuntimePolicy, SessionOperational, Audio, Save, InputModes, Camera
  - InputModesRuntimeConfigGroup: referencia obrigatoria a `OperationalInputRuntimeProfileAsset` (profileId + `maxPlayerSlots`, `uiActionsAsset`, 10 `InputActionReferences` canonicas) (ADR-0009)
  - disponibilidade física do input operacional: `InputRuntimeRoot` deve estar em persistent scene do modo (`UIGlobalScene` no Base11Sandbox), não em `NewBootstrap`.
  - CameraRuntimeConfigGroup: `operationalCameraPrefab`, `operationalCameraIdentity` (ADR-0012)
  - Checkpoint completo: nenhum item adiado.
- **ADR-0012** (congelado) congela o contrato operacional de:
  - camera operacional como infraestrutura de composition/bootstrap (não stage do SessionOperationalPipeline);
  - ordem canônica: RuntimePolicy → OperationalCameraRuntime → RuntimePersistentScenes → SessionOperationalRuntime → SceneComposition;
  - SessionOperationalPipeline assume que câmera operacional já existe;
  - UnityOperationalCameraRuntimeAdapter como executor técnico;
  - Sem fallback para Camera.main;
  - Activity Camera Binding é futura, fora deste checkpoint.
- **Checkpoint SessionOperational (2026-05-14)**:
  - `RouteActivitySavePlanReady` permanece plano;
  - `load-on-enter` executa após `SceneCompositionCompleted` e antes de `InputCapability`/`PlayerPreparation`;
  - ausência de save gera skip explícito `no_snapshot`;
  - `save-on-exit` por troca de rota usa a rota anterior completa e ocorre antes do unload da cena anterior;
  - sem `Activity Snapshot Provider`, `save-on-exit` gera skip `no_snapshot_provider`;
  - rota QA `route-sandbox-menu` habilita smoke manual `Menu -> Sandbox -> Menu`.
- **Checkpoint RouteActivitySave boundary (CLOSED - 2026-05-17)**:
  - `OperationalRouteAsset` declara policy (`loadActivitySaveOnEnter`/`saveActivityOnExit`);
  - `SessionOperationalPipeline` decide timing/policy (`save-on-exit` antes do unload, `load-on-enter` após `SceneCompositionCompleted`);
  - `IProgressionSlotContextResolver` resolve contexto operacional;
  - `SessionOperationalActivitySaveAdapter` executa side-effect e `ISaveService`/`SaveRuntime` persiste por `SaveAddress`/`SaveRequest`;
  - `SaveRuntime` não decide lifecycle;
  - `SessionActivity` não salva diretamente no trilho canônico;
  - `no_snapshot` e `no_snapshot_provider` permanecem skips explícitos (não fallback silencioso).
- **Save Base 1.1 (congelado)**:
  - Preferences e Progression sao scopes distintos.
  - **Checkpoint SaveRuntime API Base 1.1: PASS estrutural**.
    - `ISaveService` expoe somente `TryLoad(SaveAddress)`, `TrySave(SaveRequest)` e `TryDelete(SaveAddress)`.
    - `SaveAddress`, `SaveRequest` e `SaveResult` sao a superficie publica canonica.
    - `SaveIdentity` e `SaveRecord` permanecem apenas como detalhe tecnico interno do `SaveCoreService`/backends.
    - Metadados de endereco usam `save.address.*`; nao ha metadados paralelos `preferences.address.*`.
  - **Checkpoint Save/Preferences Base 1.1: PASS funcional com PlayerPrefsSaveBackend**.
  - Shape canonico ativo: `PreferencesRuntimePipeline -> PreferencesSaveAdapter -> ISaveService/SaveRuntime -> PlayerPrefsSaveBackend`.
  - Owner de decisao de Preferences: `PreferencesRuntimePipeline` (load bootstrap, preview, commit, restore defaults; fora de `RouteActivitySave`).
  - `PreferencesSaveAdapter` e adapter canonico de persistencia e usa `ISaveService` por `SaveAddress`/`SaveRequest`.
  - `PreferencesService` restrito a estado/aplicacao runtime/defaults/presets.
  - `IPreferencesBackend`, `IPreferencesSaveService` e `PlayerPrefsPreferencesBackend` sairam do caminho ativo.
  - Nao existe backend proprio de Preferences nem dual write path ativo em Preferences.
  - Smoke funcional validou: defaults limpos -> commit de audio/video -> novo bootstrap carregando valores persistidos via `PlayerPrefsSaveBackend`.
  - Progression e decidida pelo pipeline dono do ciclo (sem owner generico unico).
  - `SaveRuntime` e executor/API comum; backend e substituivel.
  - `RouteActivitySave` permanece especifico de rota/activity no `SessionOperationalPipeline`.
  - `SessionOperationalPipeline` nao salva Preferences; Preferences nao usa `RouteActivitySave` nem `ProgressionSlotContext`.
  - Todo comando de save deve carregar identidade canonica e rejeitar/skipar foreign/stale.
  - Sem fallback silencioso e sem dual write path ativo para o mesmo scope.
  - **Progression Save (decisao congelada + Fases 1/1.1/1.2 em PASS estrutural):**
    - Progression usa slots e snapshots; Preferences nao usa slots de progressao.
    - `SaveSlot` e container logico; `SaveSnapshot` e captura versionada; `CurrentSave` e ponteiro para slot/snapshot ativo.
    - `AutoSave`, `ManualSave` e `Checkpoint` sao policies de pipeline, nao comportamento de backend.
    - `SaveRuntime` nao decide slot/snapshot; apenas executa persistencia no endereco recebido.
    - Pipeline owner resolve/valida `ProgressionSlotContext` antes do comando.
    - `IProgressionSlotContextResolver` e a fronteira explicita para resolver contexto no trilho `SessionOperational`.
    - `RouteActivitySave` usa contexto resolvido, sem escolher slot sozinho, e usa `ISaveService` por `SaveAddress`/`SaveRequest`.
    - `SaveConfigAsset.defaultSlotId` pode existir como detalhe tecnico/legado, sem virar policy canonica.
    - Contratos existentes/previstos: `SaveSlotId`, `SaveSlotKind`, `SaveSnapshotId`, `SaveSlotDescriptor`, `SaveSnapshotHeader`, `SaveSlotManifest`, `ProgressionSlotContext`, `ProgressionSnapshotEnvelope`, `IProgressionSnapshotProvider`, `IProgressionSnapshotReceiver`.
    - Protecoes: comando com `Pipeline Identity`; foreign/stale rejeitado ou skip explicito; mismatch de `slotId`/`snapshotId` rejeitado/skip; ausencia de contexto obrigatorio fail-fast; rota/activity nao save-eligible com skip explicito.
    - Ainda sem actors/world objects/inventory/run save/UI de slots/autosave real/ProgressionManager/auto-scan global.
- Input atual fora do contrato Base 1.1 permanece legado/teste e não é fonte canônica.

## Precedência Normativa

Em decisões de arquitetura e ownership, prevalecem os ADRs acima em ordem de precedência.

### Regra Obrigatória de Leitura

- **ADRs de ADR-0001 a ADR-0013 são a fonte normativa viva de Base 1.1.**
  - ADR-0001 a ADR-0008: Estruturais (pipeline, adapters, policies canônicas).
  - ADR-0009 a ADR-0013: Checkpoints normativos aceitos/congelados/implementados.
- ADRs anteriores (históricos) devem ser lidos apenas como referência contextual.
- Em caso de conflito entre um ADR histórico e um ADR Base 1.1, a **Base 1.1 prevalece**.
- Ownership não é decidido por conveniência operacional, e sim pelo papel arquitetural definido na Base 1.1.
- Foreign/stale events não podem alterar o pipeline ativo.

## Classificação Normativa

### NORMATIVO_ATUAL (Base 1.1 - Estrutural + Checkpoints)

**Estruturais (ADR-0001 a ADR-0008):**
- ADR-0001
- ADR-0002
- ADR-0003
- ADR-0004
- ADR-0005
- ADR-0006
- ADR-0007
- ADR-0008

**Checkpoints Normativos Aceitos/Congelados/Implementados (ADR-0009 a ADR-0013):**
- ADR-0009 (congelado - 2026-05-14)
- ADR-0010 (congelado - 2026-05-14)
- ADR-0011 (CLOSED - 2026-05-17)
- ADR-0012 (congelado - 2026-05-14)
- ADR-0013 (aceito/implementado no MVP single-player - 2026-05-15)

### HISTÓRICO (Referência Apenas)

Todos os ADRs anteriores a ADR-0001 foram movidos para `Docs/ADRs/Historico/` e devem ser lidos como referência histórica, **não como fonte normativa viva**.

Se um ADR histórico conflitar com a Base 1.1:

- Não abrir exceção local
- Não usar compatibilidade narrativa
- Aplicar a precedência normativa dos ADRs Base 1.1

## Conceitos Chave da Base 1.1

### Pipelines

- **Run Pipeline**: Orquestra run, deactivation e continuity
- **Session Operational Pipeline**: Orquestra transição, setup e handoff inicial de sessão
- **Session Activity Pipeline**: Orquestra ativação, engagement e ciclo interno de activity
  - entrada normal de runtime ocorre por `SessionActivityEntryHandoff` vindo do `SessionOperationalPipeline`
  - `SessionActivityHost` nao e owner de lifecycle e `autoStart/debug start` nao sao contrato canonico

### Separation of Concerns

- **Modules**: Produzem Pipeline Facts ou Pipeline Commands
- **Pipelines**: Decidem ordem, lifecycle, policy e handoffs
- **Adapters**: Executam side-effects comandados

### Identidade Explícita

- Todo ciclo relevante tem identidade canônica
- Foreign/stale events não podem alterar o pipeline ativo
- Identidade não é inferida por timing ou conveniência

### Componentes Executores

- **Gates**: Validam/transitam estado
- **InputModes**: Aplicam modo de input
- **Scene Composition**: Carrega/ativa cenas
- **Fade Adapter**: Executa fade visual
- **Loading Adapter**: Executa UI de loading
- **Audio Adapter**: Executa playback de áudio
- **SaveCoreService**: Executa save/load por `SaveAddress`/`SaveRequest` via `ISaveService`

## Checkpoint Congelado

Base11Sandbox Minimal Route + Session Activity Cycle foi aprovado e congelado com:

- Rail canônico: `RuntimeModeConfig` -> `SessionOperationalPipeline` -> Adapters -> `SessionActivityPipeline`
- Identidade explícita em todas as transições
- Foreign/stale events isolados pelo sistema
- Side-effects rastreáveis e corretos

Limites atuais congelados:
- `PlayerPreparation` no trilho ativo operacional resulta em `planned_only`/`observed_noop` e produz payload minimo para handoff.
- Não há `Activity Snapshot Provider` canônico.
- Não há gameplay input canônico neste checkpoint.
- PlayerActor v0 MaterializedOnly nasce em SessionActivityPipeline/ActivitySetup; SessionOperational nao materializa PlayerActor.
- Camera pré-reveal está fechada no MVP single-player (Route/Surface + Activity + release determinístico entre rotas).
- Status formal Base 1.1: **CameraPresentation pré-reveal single-player — CLOSED**.
- Activity Camera Runtime durante a Activity permanece fora do MVP atual.
- Checkpoint ADR-0006 Audio operacional de rota: **CLOSED (trilho Cue validado)**.
  - Rail: `OperationalRouteAsset -> routeAudio* -> SessionOperationalPipeline -> RouteAudioPlanReady -> RouteRevealAudioStarted -> AudioAdapter playStarted/playSubmitted -> RouteRevealAudioSubmitted`.
  - Contratos congelados: `AudioAdapter` executa side-effect sem decidir lifecycle; `AudioRuntime` técnico não decide rota/scene/handoff/timing/lifecycle; `routeAudioMode=None` exige cue nulo; `routeAudioMode=Cue` exige cue válido; timing MVP validado `BeforeFadeOut`.
- Checkpoint RuntimeConfig/Wiring SessionOperational:
  - `RuntimeConfigRegistry` permanece a fonte canônica de resolução de config por domínio.
  - `InputModes` é resolvido exclusivamente por `RuntimeConfigRegistry`/`InputModesRuntimeConfigGroup`.
  - `InputRuntimeRoot` é dependência operacional persistente do modo e deve estar em `UIGlobalScene`, com `PlayerInputManager`, `EventSystem` e `InputSystemUIInputModule` canônicos.
  - `RuntimeModeConfig.inputModes`, `RuntimeModeConfigLoader` e `BootstrapConfigAsset` foram removidos do caminho ativo.
  - `CompositionProfileKind.Base11Sandbox` ativo; profiles não suportados falham explicitamente.
  - `SessionOperationalRuntimeComposer` registra apenas adapters canônicos.
- Fora do escopo deste checkpoint:
  - Save/Progression real;
  - Activity Snapshot Provider;
  - Activity lifecycle/deactivation;
  - PlayerActor final;
  - gameplay input final;
  - Run Pipeline.

Referências de materialização:

- `Docs/ADRs/Base-1.1-Consolidado-Atualizado-Base11Sandbox.md`
- Codebase atual em `Assets/_ImmersiveGames/NewScripts/`

## Para Desenvolvedores

### Como Decidir Ownership

1. Consulte **ADR-0001** para entender a arquitetura geral
2. Encontre o ADR que corresponde ao seu componente (ADR-0002 a ADR-0008)
3. Siga as regras e invariantes do ADR
4. Não crie fallback silencioso
5. Falhe cedo se precondições não forem atendidas

### Como Adicionar um Feature Novo

1. Verifique ADR-0005 (Modules/Adapters pattern)
2. Decida: seu feature é um módulo (fact/command produtor) ou um adapter (executor)?
3. Identifique qual pipeline decide o lifecycle
4. Documente a decisão em um ADR futuro se for mudança arquitetural

### Como Reportar Conflitos

Se encontrar um conflito entre um ADR histórico e um ADR Base 1.1:

1. Abra uma issue referenciando ambos
2. Cite o ADR Base 1.1 como fonte normativa
3. Não use compatibilidade narrativa; aplique o ADR

## Checkpoints

  - checkpoint de `PlayerPreparation` atualizado: `PlayerPreparationStarted` -> `PlayerPreparationIntentPrepared` -> `PlayerPreparationCompleted(outcome=planned_only/observed_noop)` -> handoff com payload minimo de `PlayerPreparation` (identidade + outcome + contagens);
  - checkpoint de `RouteActivitySave boundary` fechado: policy declarada na rota, timing/policy decidido por `SessionOperationalPipeline`, execução por adapter + persistência por `ISaveService`/`SaveRuntime`, com `no_snapshot` e `no_snapshot_provider` como skips explícitos;
  - limites mantidos: sem gameplay input, sem `PlayerInput` no player, sem camera de player, sem Cinemachine, sem movimento/controle, sem player final;
  - actors nao-player continuam fora do checkpoint operacional de PlayerPreparation (futuro `ActivitySetup`/SessionActivity).


### Checkpoint complementar - PlayerSelectionSnapshot e PlayerActor v0 (2026-05-18)

Decisao congelada:

```text
PlayerSlot pode ser ocupado antes da rota de gameplay.
PlayerSelectionSnapshot representa a intencao/configuracao de player.
PlayerActor v0 nasce no ActivitySetup da Gameplay Activity.
```

Modelo ideal futuro:

```text
CharacterSelection Activity
-> ocupa slots
-> escolhe player/skin/nome
-> produz PlayerSelectionSnapshot
-> Gameplay Activity materializa PlayerActor
```

MVP aceito:

```text
Menu
-> botao "1 Player" ou "2 Players"
-> produz PlayerSelectionSnapshot default
-> ActivitySetup materializa PlayerActor v0
```

Fronteira de ownership:

- `SessionOperationalPipeline` valida slots, `PlayerInputManager`, input operacional e prepara handoff.
- `SessionOperationalPipeline` nao materializa `PlayerActor` jogavel final.
- `PlayerPreparationStage` nao e `PlayerActorSetup`.
- `SessionActivityPipeline / ActivitySetup` e owner do nascimento do `PlayerActor v0`.
- `PlayerActorSetupStage` deve produzir/consumir `PlayerActorEntryPlan` e `PlayerActorResetPlan` minimos.

Campos conceituais minimos:

```text
PlayerActorEntryPlan:
  playerActorId
  playerSlotId
  playerDefinitionId
  activityId
  spawnPointId
  ownerScope
  releasePolicy
  resetPolicy

PlayerActorResetPlan:
  resetReason
  initialSpawnPointId
  initialTransformPolicy
  runtimeStatePolicy
  selectionRebindPolicy
```

Fora deste corte:

- gameplay input final;
- movimento/controle;
- camera por player;
- `PlayerInput` ligado ao `PlayerActor`;
- split-screen;
- save/progression de player.

#### Transporte do PlayerSelectionSnapshot (2026-05-18)

Complemento congelado ao checkpoint de `PlayerActor v0`:

```text
Menu / CharacterSelection
-> PlayerSelectionSnapshot
-> Route Request
-> SessionOperationalPipeline
-> SessionActivityEntryHandoff
-> SessionActivityPipeline
-> ActivitySetup
-> PlayerActorSetupStage
-> PlayerActorEntryPlan
-> PlayerActor v0
```

Regras:

- `PlayerSelectionSnapshot` e payload de intencao, nao runtime object.
- `SessionOperationalPipeline` pode validar capacidade/consistencia e transportar o payload no handoff.
- `SessionOperationalPipeline` nao materializa `PlayerActor` jogavel final.
- `ActivitySetup` consome o snapshot e resolve `PlayerActorEntryPlan`.
- O snapshot nao carrega `GameObject`, `Transform`, `PlayerInput`, `Camera`, Cinemachine, movement component ou runtime instance.
- Nao usar `static`, global mutavel ou objeto `DontDestroyOnLoad` como fonte canonica da selecao.

#### Validacao do PlayerSelectionSnapshot (2026-05-18)

Complemento congelado:

```text
PlayerSelectionSnapshot
-> PlayerSelectionValidation
-> PlayerActorEntryPlan
-> PlayerActorResetPlan
-> PlayerActor v0
```

Divisao de ownership:

- `SessionOperationalPipeline` valida capacidade e consistencia de transporte: snapshot obrigatorio, playerCount, `maxPlayerSlots`, slots duplicados e `Pipeline Identity`.
- `SessionActivityPipeline / ActivitySetup` valida materializacao: `PlayerDefinitionId`, `SpawnPointId`, `PlayerActorEntryPlan` e `PlayerActorResetPlan`.
- `SessionOperationalPipeline` nao materializa `PlayerActor` jogavel final.
- `ActivitySetup` so materializa `PlayerActor v0` depois da validacao do snapshot e do plano.

Regras:

```text
Ausencia obrigatoria = fail-fast.
Ausencia aceitavel = skip explicito.
Sem fallback silencioso para primeiro prefab, primeiro slot, primeiro spawn point ou primeiro player encontrado.
Defaults autorais, como skin/displayName, devem ser explicitos.
```

#### PlayerActorSetupStage no ActivitySetup (2026-05-18)

Complemento congelado ao checkpoint de `PlayerActor v0`:

```text
ActivitySetupStarted
-> PlayerActorSetupStageStarted
-> PlayerSelectionSnapshotValidated
-> PlayerActorEntryPlanResolved
-> PlayerActorResetPlanResolved
-> PlayerActorMaterializationCommand
-> PlayerActorMaterialized
-> PlayerActorReadyFact
-> PlayerActorSetupStageCompleted
-> ActivitySetupCompleted
```

Regras:

- `PlayerActorSetupStage` e sub-stage do `ActivitySetup`.
- `SessionActivityPipeline / ActivitySetup` decide lifecycle, falha e continuidade.
- `PlayerActorSetupStage` resolve `PlayerActorEntryPlan` e `PlayerActorResetPlan`.
- `PlayerActorMaterializationAdapter` executa side-effects Unity comandados.
- Em Activity que exige player, `ActivitySetupCompleted` nao pode ocorrer sem `PlayerActorReadyFact` obrigatorio.
- O stage nao implementa ainda `PlayerInput`, movimento, camera, save/progression, status/inventario ou split-screen.

Fronteira com `SessionOperationalPipeline`:

```text
SessionOperationalPipeline valida e transporta PlayerSelectionSnapshot.
SessionActivityPipeline / ActivitySetup materializa PlayerActor v0.
```

#### PlayerDefinition e ActivityPlayerSpawnPoint (2026-05-18)

Complemento congelado ao checkpoint de `PlayerActor v0`:

```text
PlayerSelectionSnapshot
-> PlayerDefinition
-> ActivityPlayerSpawnPoint
-> PlayerActorEntryPlan
-> PlayerActorResetPlan
-> PlayerActorMaterializationCommand
-> PlayerActorReadyFact
```

Regras:

- `PlayerDefinition` resolve o prefab/config autoral do player.
- `ActivityPlayerSpawnPoint` resolve onde o player nasce dentro da Activity.
- `PlayerActorSetupStage` resolve definition/spawn/plan dentro do `ActivitySetup`.
- `PlayerActorMaterializationAdapter` apenas instancia e posiciona conforme command ja resolvido.
- `SessionOperationalPipeline` nao escolhe prefab final nem spawn point.
- `PlayerInputManager`, `Camera.main`, nome de `GameObject` ou busca implicita nao sao fontes canonicas de materializacao.

Falhas obrigatorias:

```text
PlayerDefinitionId invalido = fail-fast.
PlayerActorPrefab obrigatorio ausente = fail-fast.
Activity que exige player sem spawn point valido = fail-fast.
Sem fallback silencioso.
```

#### PlayerActorIdentity e ActivityPlayerActorRegistry (2026-05-18)

Complemento congelado ao checkpoint de `PlayerActor v0`:

```text
PlayerActorMaterializationCommand
-> PlayerActorMaterializationAdapter
-> PlayerActorIdentity aplicada
-> ActivityPlayerActorRegistry.Register
-> PlayerActorReadyFact
```

Campos conceituais minimos de `PlayerActorIdentity`:

```text
PlayerActorId
PlayerSlotId
PlayerDefinitionId
ActivityId
EntrySequence
```

Regras:

- Todo `PlayerActor v0` materializado deve ter `PlayerActorIdentity` valida.
- Todo `PlayerActor v0` materializado deve ser registrado em `ActivityPlayerActorRegistry` scoped a Activity atual.
- `ActivityPlayerActorRegistry` nao decide lifecycle.
- `ActivityPlayerActorRegistry` nao cria player.
- `ActivityPlayerActorRegistry` nao escolhe prefab, spawn point, slot ou player definition.
- `ActivityPlayerActorRegistry` nao faz fallback para primeiro player, tag, nome de `GameObject` ou singleton global.
- O registry existe para consulta tecnica por identidade em stages/adapters futuros.

Fronteira:

```text
SessionOperationalPipeline valida e transporta intencao.
SessionActivityPipeline / ActivitySetup materializa, aplica identidade e registra PlayerActor v0.
```

Ainda fora do checkpoint:

```text
PlayerInput binding
movimento
camera target binding
save/progression
split-screen
```

#### PlayerActorReleasePlan e PlayerActorReleasedFact (2026-05-18)

Complemento congelado ao checkpoint de `PlayerActor v0`:

```text
PlayerActorReadyFact
-> PlayerActorResetPlan
-> PlayerActorReleasePlan
-> PlayerActorReleaseCommand
-> PlayerActorReleasedFact
-> ActivityPlayerActorRegistry.Unregister
```

Regras:

- Todo `PlayerActor v0` ActivityOwned deve possuir `PlayerActorReleasePlan` minimo.
- No MVP, `ReleasePolicy = ReleasePlayersOnRouteExit (default) ou PersistPlayersAcrossRoutes`.
- `SessionActivityPipeline / ActivitySetup / ActivityRelease` decide quando liberar.
- `PlayerActorReleaseAdapter` executa side-effects tecnicos comandados.
- `ActivityPlayerActorRegistry` remove o registro somente apos release valido.
- Registry nao decide release.
- Sem `PlayerActorReleasedFact` obrigatorio, o ciclo que exige release nao pode concluir.
- `SessionOperationalPipeline` nao destroi PlayerActor e nao limpa registry; ele apenas bloqueia unload/route-exit ate o fechamento canonico da `SessionActivity`, quando aplicavel.

Fora do checkpoint:

```text
ReturnToPool
RetainForRestart
RetainUntilRouteExit
Suspend
restore de checkpoint/save
release completo de input/camera/HUD/inventory/subscriptions
```


#### PlayerActorResetPlan e PlayerActorResetCompletedFact (2026-05-18)

Complemento congelado ao checkpoint de `PlayerActor v0`:

```text
PlayerActorReadyFact
-> PlayerActorResetPlan
-> PlayerActorResetCommand
-> PlayerActorResetCompletedFact
-> PlayerActorReleasePlan
-> PlayerActorReleaseCommand
-> PlayerActorReleasedFact
```

Regras:

- Todo `PlayerActor v0` ActivityOwned deve possuir `PlayerActorResetPlan` minimo.
- No MVP, `ResetPolicy = ResetToInitialActivitySpawn`.
- `SessionActivityPipeline / ActivitySetup` decide quando resetar.
- `PlayerActorResetAdapter` executa side-effects tecnicos comandados.
- `ActivityPlayerActorRegistry` pode localizar o player por `PlayerActorIdentity`, mas nao decide reset.
- Reset minimo retorna o player ao `ActivityPlayerSpawnPoint` inicial, restaura transform inicial e limpa estado runtime transitorio.
- `SelectionRebindPolicy` pode reaplicar dados do `PlayerSelectionSnapshot` quando necessario.
- Sem `PlayerActorResetCompletedFact` obrigatorio, o ciclo que exige reset nao pode concluir.
- `ActivityRestartCompleted` nao pode ocorrer antes de os `PlayerActors` obrigatorios estarem prontos novamente.
- `SessionOperationalPipeline` nao reseta PlayerActor; ele apenas aguarda/bloqueia quando o fechamento canonico da `SessionActivity` for pre-condicao de route-exit/unload.

Fora do checkpoint:

```text
input rebinding completo
camera rebinding
save/progression
restore de checkpoint/save
respawn gameplay complexo
vida/inventario/status complexo
```

#### Componentes minimos do PlayerActor v0 (2026-05-18)

Complemento congelado ao checkpoint de `PlayerActor v0`:

```text
PlayerActorRoot
PlayerActorIdentityComponent
PlayerActorLifecycleMarker
PlayerActorResetAnchor ou referencia equivalente de spawn inicial
```

Regras:

- `PlayerActorIdentityComponent` carrega `PlayerActorIdentity` aplicada pelo command/adapter.
- `PlayerActorLifecycleMarker` marca ownership minimo da Activity.
- No MVP: `OwnerScope = ActivityOwned`, `ReleasePolicy = ReleasePlayersOnRouteExit (default) ou PersistPlayersAcrossRoutes`, `ResetPolicy = ResetToInitialActivitySpawn`.
- O root/marker preserva dados suficientes para reset minimo: `InitialSpawnPointId`, posição, rotação e escala quando aplicável.
- `PlayerActor v0` nao contem `PlayerInput`, movimento final, camera final, Cinemachine binding, save/progression, health/inventory/status final ou split-screen data.
- Input, movimento, camera, save/progression e gameplay state entram depois por stages/adapters proprios comandados pelo pipeline dono do ciclo.
- `SessionOperationalPipeline` nao injeta componentes internos de player e nao transforma o player em jogavel por side-effect operacional.

#### PlayerActorReadyFact v0 (2026-05-18)

Complemento congelado ao checkpoint de `PlayerActor v0`:

```text
PlayerActorMaterializationCommand
-> PlayerActorMaterializedFact
-> PlayerActorReadyFact
```

Regras:

- `PlayerActorReadyFact` representa readiness minima do `PlayerActor v0`.
- No MVP, `ReadyStage = MaterializedOnly`.
- Ready significa: materializado, identificado, posicionado, registrado e com `PlayerActorEntryPlan`, `PlayerActorResetPlan` e `PlayerActorReleasePlan` validos.
- Ready nao significa input, movimento, camera, save/progression ou gameplay state final.
- Em Activity que exige player, `ActivitySetupCompleted` nao pode ocorrer sem `PlayerActorReadyFact` obrigatorio.
- Activity sem requisito de player deve gerar skip explicito.
- Camadas futuras de input, movimento, camera, save/progression e gameplay state devem produzir facts proprios.
- `SessionOperationalPipeline` nao emite `PlayerActorReadyFact`; o fact pertence ao `SessionActivityPipeline / ActivitySetup`.


### Checkpoint congelado - PlayerActor v0 MaterializedOnly + RouteOwned lifetime (2026-05-19)

Contrato ativo Base 1.1:

```text
PlayerPreparation no SessionOperational = planned_only/intencao/payload minimo para handoff.
PlayerActor v0 nasce somente em SessionActivityPipeline/ActivitySetup.
Lifetime padrao do PlayerActor v0 = RouteOwned/RouteScoped (nao ActivityOwned).
```

Regras congeladas:

```text
Activity exit encerra participacao local do PlayerActor, sem destruicao padrao.
Deactivation nao implica Release.
Route exit exige policy explicita: ReleasePlayersOnRouteExit ou PersistPlayersAcrossRoutes.
SessionOperational nao destroi PlayerActor diretamente.
```

Reservas de ownership:

```text
ActivityOwned: NPCs, enemies, props e objetos exclusivos da Activity.
SceneOwned/SceneContributed: objetos descobertos nas cenas da Activity.
```

Proximo corte tecnico:

```text
PlayerActorParticipationExit v0
```


### Checkpoint curto - PlayerActorParticipationExit + Gate/InputMode boundary (2026-05-19)

```text
SessionActivityPipeline decide PlayerActorParticipationExit v0.
PlayerActorParticipationExit v0 ocorre antes da DeactivationWindow.
DeactivationWindow nao e gameplay ativo do player.
Gate/InputMode executam efeitos tecnicos por Pipeline Command; nao decidem lifecycle de participacao.
PlayerActorParticipationExit nao destroi PlayerActor e nao remove lifetime RouteOwned/RouteScoped.
Estado v0 esperado: participationState=ExitedActivity + retention=RetainedForRoute.
Route exit continua com policy explicita: ReleasePlayersOnRouteExit | PersistPlayersAcrossRoutes.
```

### Checkpoint curto - PlayerActor v0 RouteOwned lifecycle + Participation Enter/Reenter (2026-05-19)

Status:

```text
PlayerActor v0 - RouteOwned lifecycle + Participation Enter/Reenter - CLOSED
```

Resumo normativo:

```text
PlayerPreparation no SessionOperational permanece planned_only/intencao/payload.
PlayerActor nasce em SessionActivityPipeline/ActivitySetup.
ParticipationExit ocorre antes da DeactivationWindow sem destruir PlayerActor.
ParticipationEnter/Reenter reutiliza PlayerActor retido compativel sem nova materializacao.
Catalog LoopToFirst e respeitado pela SessionActivityPipeline para permitir activity_01 -> activity_02 -> activity_01.
QA/Host nao decide looping; apenas aciona comandos.
```

Smoke fechado de referencia:

```text
activity_01 materializa PlayerActor
activity_01 retem PlayerActor
activity_02 completa
catalogo loopa para activity_01
activity_01 reentra com PlayerActor retido sem nova PlayerActorMaterializationCommandIssued
```

Divida documentada (sem mudanca funcional agora):

```text
No caminho ativo de ActivitySetup, o fact alinhado e:
- PlayerActorActivityParticipationPlanResolved

Futuro reservado (fora do corte ativo):
- PlayerActorRouteExitReleasePlanResolved
```

### Checkpoint curto - PlayerActorReset v0 (2026-05-19)

Status:

```text
PlayerActorReset v0 - CLOSED
```

Resumo normativo:

```text
Reset ocorre no ActivitySetup, antes de PlayerActorReady*.
SessionActivityPipeline decide groups; PlayerActorResetAdapter executa.
PlayerActor aplica reset via endpoints por grupo; sem ResetAll cego.
Groups v0: Placement, ActivityParticipation, MovementTransient.
Skips sao auditaveis por skippedGroupReasons.
MovementTransient sem endpoint: no_endpoint_supports_group.
Placement auditavel: no_placement_declared | optional_placement_missing | placement_not_required.
Placement obrigatorio invalido: fail-fast.
```

Smoke fechado:

```text
materializacao/reenter sempre passam por reset antes de Ready
PlayerActorReleasePlanResolved nao reaparece
ResetAll, Destroy e SetActive do PlayerActor nao aparecem
```

### Checkpoint curto - SessionActivity lifecycle deterministico + route-exit handshake observavel (2026-05-19)

Status:

```text
FROZEN
```

Contrato congelado Base 1.1:

```text
1) SessionActivityPipeline deve ser deterministico.
2) Comandos sincronos nao podem fingir conclusao quando houver pending async.
3) Scene load/unload e o unico side-effect async permitido no lifecycle da Activity.
4) Adapters executam side-effects; pipeline decide continuidade.
5) Todo rail segue request -> started/in-progress -> completed/failed.
6) PendingOperation e Pipeline Command em execucao.
7) ClearPendingOperation so no consumo validado da completion.
8) Route-exit e handshake observavel SessionOperational <-> SessionActivity.
9) SessionOperational nao descarrega route scene com rail/pending ativo na SessionActivity.
10) Trilhos antigos/paralelos devem ser removidos no caminho de implementacao.
```

Rails canonicos:

```text
ActivityEntryRail
ActivityCompletionRail
ActivityRestartRail
ActivityNavigationRail
ActivityRouteExitRail
```
