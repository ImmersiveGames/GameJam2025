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
10. **ADR-0010** - Player Preparation Flow, Player Slots e Unity PlayerInput (CONGELADO - 2026-05-14)
11. **ADR-0011** - Runtime Configuration Registry and Config Sets (IMPLEMENTADO)
12. **ADR-0012** - Operational Camera Runtime e Future Activity Camera Binding (CONGELADO - 2026-05-14)
13. **ADR-0013** - Camera Presentation Runtime e Activity Camera Director (ACEITO / IMPLEMENTADO NO MVP SINGLE-PLAYER)

**Estes ADRs (0009-0013) são fonte normativa Base 1.1 no mesmo nível do ADR-0001 a 0008. Não são "complementares".**

Notas:
- **ADR-0009** (congelado) congela o contrato operacional de:
  - slots e validacao de PlayerInputManager;
  - inicializacao de EventSystem persistente;
  - inicializacao de InputSystemUIInputModule persistente;
  - binding canonico de 10 UI actions via `OperationalInputRuntimeProfileAsset` referenciado por `InputModesRuntimeConfigGroup`.
  - 11 decisoes congeladas sobre fail-fast, integridade, sequencia de binding.
- **ADR-0010** (congelado) depende de ADR-0009 para validacao/init de slots e input operacional, permanece focado em PlayerPreparation (somente players), com materializacao minima de `PrototypePlayer` quando aplicavel, **sem** materializacao de gameplay input ou player selection.
- **ADR-0011** (implementado):
  - `RuntimeModeConfig` permanece entrada canônica
  - `RuntimeConfigSetAsset` agrupa configs por domínio
  - `RuntimeConfigRegistry` valida e expõe snapshots read-only
  - 6 grupos obrigatórios: RuntimePolicy, SessionOperational, Audio, Save, InputModes, Camera
  - InputModesRuntimeConfigGroup: referencia obrigatoria a `OperationalInputRuntimeProfileAsset` (profileId + `maxPlayerSlots`, `uiActionsAsset`, 10 `InputActionReferences` canonicas) (ADR-0009)
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
- ADR-0011 (implementado - 2026-05-13)
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
- `PlayerPreparation` pode resultar em `materialized` (materializacao minima de `PrototypePlayer`) ou `planned_only`/`observed_noop` conforme o contexto da rota.
- Não há `Activity Snapshot Provider` canônico.
- Não há gameplay input canônico neste checkpoint.
- Não há `PlayerActor` materializado neste checkpoint.
- Camera pré-reveal está fechada no MVP single-player (Route/Surface + Activity + release determinístico entre rotas).
- Status formal Base 1.1: **CameraPresentation pré-reveal single-player — CLOSED**.
- Activity Camera Runtime durante a Activity permanece fora do MVP atual.

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

  - checkpoint de `PlayerPreparation` atualizado: `PlayerPreparationStarted` -> materializacao minima de `PrototypePlayer` (required com prefab) / skip explicito (optional sem prefab) -> `PlayerPreparationCompleted(outcome=materialized quando aplicavel)` -> `MaterializationCompleted` -> handoff;
  - limites mantidos: sem gameplay input, sem `PlayerInput` no player, sem camera de player, sem Cinemachine, sem movimento/controle, sem player final;
  - actors nao-player continuam fora do checkpoint operacional de PlayerPreparation (futuro `ActivitySetup`/SessionActivity).
