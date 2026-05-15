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
- Ainda nao ha provider/receiver real de gameplay persistence.
- Ainda nao salvar actors;
- Ainda nao salvar world objects;
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
| Route Activity Save | `SessionOperationalPipeline` (scope de rota/activity) | `RouteActivitySave` + `SessionOperationalActivitySaveAdapter` usando `ProgressionSlotContext` resolvido | `SaveRuntime` (`ISaveService` por `SaveAddress`/`SaveRequest` + backend configurado) | PASS estrutural de encaixe passivo; sem provider real de activity ainda |
| Run Save / Continuity | `RunPipeline` (quando materializado) | `RunPipeline` + adapter de save do dominio run | `SaveRuntime` (backend substituivel) | Owner ainda futuro, mas ja congelado no trilho canonico |
| Progression de objetos/dominios | Pipeline dono do ciclo correspondente | Pipeline do ciclo + adapter de save do dominio | `SaveRuntime` (backend substituivel) | Dominios produzem snapshot/registro; dominio nao chama backend direto |

## Consequências

- Save deixa de ser espalhado em points of interest.
- Decisão fica no pipeline; execução em `SaveCoreService`.
- Backend é plugável e testável.
- Save eligibility é explícita e rastreável.
- Falhas de save são observáveis.

## Roadmap Futuro

- Implementar `SaveSlotManifest` e `SaveSnapshotHeader` reais para substituir `snapshotId` sintetico.
- Implementar o primeiro `IProgressionSnapshotProvider`/`IProgressionSnapshotReceiver` real quando houver activity/objeto concreto para persistir.
- Definir owner de `CurrentSave` completo fora do core de `SaveRuntime`.
- Evoluir Run save/continuity pelo `RunPipeline` quando esse fluxo for materializado.
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
