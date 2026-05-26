<!--
STATUS: HISTÓRICO PARA CONSULTA.
Este ADR foi reclassificado pelo ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory.
Use como evidência, histórico e intenção funcional. Em conflito, ADR-2.0-0001 prevalece.
-->

# ADR-0011 - Runtime Configuration Registry e Config Sets

## Status

- Estado: CLOSED
- Data: 2026-05-13
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR, após aceite.

---

## Contexto

A Base 1.1 usa `RuntimeModeConfig` como entrada canônica do modo runtime.

Com a evolução do Base11Sandbox e dos pipelines da Base 1.1, o `RuntimeModeConfig` passou a acumular referências de vários domínios:

- política runtime;
- composição;
- persistent scenes;
- rota inicial;
- loading profile;
- audio defaults;
- save config;
- input modes;
- strictness/reporter.

Isso cria risco de transformar o `RuntimeModeConfig` em um grande container de configuração, difícil de validar, difícil de manter e perigoso como fonte de decisões implícitas.

A Base 1.1 precisa separar:

```text
entrada do modo
```

de:

```text
conjunto validado de configs por domínio
```

sem criar um framework genérico de configuração, sem auto-scan de pasta e sem mover lifecycle/policies para assets de config.

---

## Decisão

Adota-se o modelo:

```text
RuntimeModeConfig
-> RuntimeConfigSetAsset
-> RuntimeConfigRegistry
-> RuntimeConfigSnapshot read-only
```

### 1. RuntimeModeConfig

`RuntimeModeConfig` continua sendo a entrada canônica do modo.

Ele deve permanecer pequeno e carregar apenas informações de bootstrap/mode-level, como:

- `modeOverride`;
- `compositionProfile`;
- strictness/reporter quando ainda forem transversais ao modo;
- referência explícita para `RuntimeConfigSetAsset`.

Regra:

```text
RuntimeModeConfig não deve virar um container gigante de configs por domínio.
```

### 2. RuntimeConfigSetAsset

`RuntimeConfigSetAsset` é o asset explícito que agrupa configs por domínio/módulo.

Ele não decide lifecycle.
Ele não executa nada.
Ele não resolve fallback.
Ele apenas declara referências obrigatórias/opcionais e permite validação.

Modelo mínimo:

```text
RuntimeConfigSetAsset
- RuntimePolicyConfigGroup
- SessionOperationalRuntimeConfigGroup
- AudioRuntimeConfigGroup
- PreferencesRuntimeConfigGroup
- SaveRuntimeConfigGroup
- InputModesRuntimeConfigGroup
- CameraRuntimeConfigGroup
```

### 3. RuntimeConfigRegistry

`RuntimeConfigRegistry` valida o `RuntimeConfigSetAsset` no bootstrap e expõe um snapshot read-only para consumidores.

Responsabilidades:

- validar config obrigatória;
- falhar explicitamente se algo obrigatório estiver ausente;
- expor interfaces ou snapshots read-only;
- impedir mutação runtime dos configs;
- centralizar acesso runtime às configs canônicas.

Não responsabilidades:

- decidir rota;
- decidir lifecycle;
- decidir handoff;
- decidir policy dinâmica;
- aplicar efeitos;
- criar fallback;
- procurar assets por pasta/nome/convenção.

### 4. RuntimeConfigSnapshot

O snapshot representa a leitura validada do conjunto de configuração para a lifetime do modo.

Regra:

```text
Config é imutável durante a lifetime do modo.
```

Mudança de config exige troca/restart do modo, não mutação silenciosa em runtime.

---

## Grupos mínimos de configuração

### RuntimePolicyConfigGroup

Agrupa configurações transversais de política runtime.

Exemplos:

- strictness;
- reporter/degradation settings;
- logging policy (`LoggingConfigAsset`) para bootstrap/runtime;
- políticas globais de validação;
- flags de diagnóstico, quando forem mode-level.

### SessionOperationalRuntimeConfigGroup

Agrupa configurações do rail operacional de sessão.

Exemplos:

- rota inicial;
- default loading mode;
- default loading profile;
- persistent scenes policy, se ficar no domínio operacional;
- profiles usados pelo `SessionOperationalPipeline`.

### AudioRuntimeConfigGroup

Agrupa configurações do runtime de áudio.

Exemplos:

- audio defaults;
- profiles de audio;
- referências de audio runtime obrigatórias.

### SaveRuntimeConfigGroup

Agrupa configurações do save runtime.

Exemplos:

- `SaveConfigAsset`;
- backend de save;
- policies declarativas de save/checkpoint.

A decisão de quando salvar continua pertencendo aos pipelines.

Regras canonicas de ownership (Base 1.1):

- `SaveRuntime` e executor comum/API estavel; nao e owner generico de decisao.
- `ISaveService` expoe apenas a API publica por `SaveAddress`/`SaveRequest`/`SaveResult`.
- `SaveIdentity` e `SaveRecord` sao detalhe tecnico interno do core/backend, nao contrato publico de pipelines/adapters.
- `RouteActivitySave` permanece restrito ao scope de rota/activity no `SessionOperationalPipeline`.
- `SessionOperationalPipeline` nao vira owner generico de persistencia.
- `RunPipeline` sera owner de run save/continuity quando esse fluxo existir.
- Commands de save devem carregar `Pipeline Identity`.
- Eventos foreign/stale devem ser rejeitados ou gerar skip explicito.
- Sem fallback silencioso e sem dual write path ativo para o mesmo scope.
- `PlayerPrefsSaveBackend` e backend tecnico provisorio atras de `SaveRuntime`, nao caminho direto de Preferences.

Checkpoint SaveRuntime API Base 1.1:

```text
TryLoad(SaveAddress, out SaveResult, out reason)
TrySave(SaveRequest, out SaveResult, out reason)
TryDelete(SaveAddress, out SaveResult, out reason)
```

- APIs publicas legadas por `SaveIdentity`/`SaveRecord`/`TrySaveCurrent` foram removidas da superficie de `ISaveService`.
- Metadados de endereco sao unificados em `save.address.*`.


Checkpoint de triagem Progression Save real (2026-05-22):

```text
Progression Save real não é ação ativa agora.
SaveRuntimeConfigGroup mantém SaveConfig/backend como capacidade técnica.
Config não deve forçar UI de slots, ProgressionManager, autosave/manual/checkpoint ou manifest funcional enquanto não houver progressão concreta.
```

Regras adicionais:

- `SaveConfigAsset.defaultSlotId` pode continuar como seed técnico/default de backend, não como policy canônica de Progression.
- `RuntimeConfigRegistry` valida config obrigatória existente, mas não cria necessidade funcional de Progression Save.
- Não adicionar config nova de progressão sem consumidor real e pipeline owner definido.
- Não criar fallback silencioso para slot/snapshot ausente em fluxo que ainda não existe.


### PreferencesRuntimeConfigGroup

Agrupa configurações do runtime de preferences.

Exemplos:

- `AudioDefaultsAsset`;
- `VideoDefaultsAsset`;
- presets/defaults de vídeo obrigatórios para bootstrap de preferences.

Regras:

- `audioDefaults` é obrigatório;
- `videoDefaults` é obrigatório;
- ausência de `audioDefaults` ou `videoDefaults` é erro fail-fast;
- source canônica de defaults de preferência (`AudioDefaults` e `VideoDefaults`) para `PreferencesRuntime` é `RuntimeConfigRegistry` snapshot read-only.

`BootstrapConfigAsset` foi removido do caminho ativo; o trilho canônico não depende desse asset.

Ownership do ciclo runtime de Preferences (PASS funcional com PlayerPrefsSaveBackend):

- AudioPreferencesOptionsBinder e VideoPreferencesOptionsBinder publicam apenas intencao de UI;
- PreferencesRuntimePipeline e owner canonico para decisao de load bootstrap, preview, commit e restore defaults;
- PreferencesSaveAdapter e o Pipeline Adapter de persistencia de Preferences;
- PreferencesSaveAdapter usa `ISaveService` exclusivamente por `SaveAddress`/`SaveRequest`/`SaveResult`;
- PreferencesService permanece owner de estado/aplicacao runtime/defaults/presets;
- SaveRuntime (`ISaveService`) executa a persistencia de Preferences;
- PlayerPrefsSaveBackend permanece backend tecnico provisorio atras de SaveRuntime;
- IPreferencesBackend, IPreferencesSaveService e PlayerPrefsPreferencesBackend sairam do caminho ativo;
- Nao existe backend proprio de Preferences ativo;
- Nao existe dual write path ativo em Preferences;
- SaveRuntime / RouteActivitySave nao participam de preferences no trilho de decisao.
- Preferences e Progression sao scopes distintos; Preferences nao e parte de RouteActivitySave.
- Smoke funcional validou defaults limpos, commit de audio/video e novo bootstrap carregando valores persistidos via PlayerPrefsSaveBackend.

Progression Save (Fases 1/1.1/1.2 - PASS estrutural, sem progressao funcional completa):

- Progression usa slots e snapshots; Preferences nao usa slots de progressao.
- `SaveRuntime` e executor comum e nao decide slot/snapshot.
- Pipeline owner do ciclo deve resolver/validar `ProgressionSlotContext` antes de emitir comando de persistencia.
- `RouteActivitySave` consome `ProgressionSlotContext` resolvido, mas nao escolhe slot sozinho.
- `IProgressionSlotContextResolver` e a fronteira explicita para resolver contexto no trilho `SessionOperational`.
- `SaveConfigAsset.defaultSlotId` pode permanecer como detalhe tecnico/legado, sem virar policy canonica de Progression.
- `RouteActivitySave` usa a API nativa de `ISaveService` por `SaveAddress`/`SaveRequest`.

Conceitos existentes/previstos de contrato para Progression:

- `SaveSlotId`, `SaveSlotKind`, `SaveSnapshotId`
- `SaveSlotDescriptor`, `SaveSnapshotHeader`, `SaveSlotManifest`
- `ProgressionSlotContext`, `ProgressionSnapshotEnvelope`
- `IProgressionSnapshotProvider`, `IProgressionSnapshotReceiver`

Protecoes obrigatorias no trilho de Progression Save:

- todo comando deve carregar `Pipeline Identity`;
- comando foreign/stale deve ser rejeitado ou gerar skip explicito;
- incompatibilidade de `slotId`/`snapshotId` deve ser rejeitada ou gerar skip explicito por policy;
- ausencia de `ProgressionSlotContext` em fluxo obrigatorio deve ser fail-fast;
- rota/activity nao save-eligible deve gerar skip explicito.

Ainda adiado:

- `SaveSlotManifest`/`SaveSnapshotHeader` real como fonte de `snapshotId` canonico;
- providers/receivers reais de gameplay persistence;
- autosave/manual/checkpoint reais;
- UI de slots;
- Run save/continuity;
- ProgressionManager e auto-scan global continuam proibidos neste shape.

### InputModesRuntimeConfigGroup

`InputModesRuntimeConfigGroup` agora carrega apenas a referencia obrigatoria `operationalInputRuntimeProfile`.

Agrupa configuracoes tecnicas de input modes e inicializacao operacional de input UI.

Exemplo de Inicializacao Operacional de Input UI (ADR-0009):

- `operationalInputRuntimeProfile` (obrigatorio) com `profileId`;
- `maxPlayerSlots` (capacidade operacional de entrada) dentro do profile;
- `uiActionsAsset` (asset de UI actions canonico) dentro do profile;
- 10 `InputActionReferences` canonicas obrigatorias dentro do profile:
    - `uiPoint`
    - `uiLeftClick`
    - `uiRightClick`
    - `uiMiddleClick`
    - `uiScrollWheel`
    - `uiMove`
    - `uiSubmit`
    - `uiCancel`
    - `uiTrackedDevicePosition`
    - `uiTrackedDeviceOrientation`

Regras sobre UI Actions Binding:

- Config e read-only via snapshot;
- Todas as 10 referencias devem pertencer ao mesmo `uiActionsAsset`;
- Ausencia de qualquer referencia e erro fail-fast no bootstrap;
- Binding e executado por `UnityOperationalInputRuntimeAdapter` (ADR-0009);
- Nao ha reconfiguracao de binding em runtime.

Regra complementar de disponibilidade fisica do input operacional:

- `OperationalInputRuntimeProfileAsset` declara dados de capacidade/config, nao cria root fisico.
- O root fisico canonico e `InputRuntimeRoot`.
- No Base11Sandbox, `InputRuntimeRoot` deve estar em `UIGlobalScene`, persistent scene garantida pelo `RuntimePersistentScenesPolicyAsset`.
- `InputRuntimeRoot` deve conter `PlayerInputManager`, `EventSystem` e `InputSystemUIInputModule` canonicos conforme ADR-0009.
- `NewBootstrap` nao deve hospedar a unica instancia canonica do runtime operacional de input, pois pode ser descarregada pela rota inicial.
- Config obrigatoria e cena persistente obrigatoria continuam fail-fast; nao ha fallback silencioso por cena transiente.

A decisao de quando trocar input mode continua pertencendo ao pipeline.

### CameraRuntimeConfigGroup

Agrupa configurações técnicas de runtime da camera operacional.

Exemplos:

- `operationalCameraPrefab` (obrigatório);
- `operationalCameraIdentity` (obrigatório, estável);
- campos futuros: `operationalCameraClearFlags`, `operationalCameraCullingMask`, `operationalCameraDepth`, etc.

Regras sobre Camera Operacional (ADR-0012):

- Config é read-only via snapshot;
- `operationalCameraPrefab` é obrigatório e deve conter exatamente uma `Camera`;
- `operationalCameraIdentity` é obrigatório e estável;
- Ausência de campos obrigatórios é erro fail-fast no bootstrap;
- Inicialização de câmera operacional é executada por `UnityOperationalCameraRuntimeAdapter` (ADR-0012);
- Camera operacional é garantida no composition/bootstrap, **antes** de `SessionOperationalPipeline` iniciar;
- Não há fallback para `Camera.main`.

---

## Invariantes

### 1. Config assets não decidem lifecycle

Configs fornecem dados.
Pipelines decidem lifecycle, ordem, policies e handoffs.

```text
Config informa.
Pipeline decide.
Adapter executa.
```

### 2. Config não é módulo produtor de facts/commands

Config não substitui módulo.
Módulos continuam produzindo `Pipeline Facts` ou `Pipeline Commands`.

### 3. Config é imutável após bootstrap

Após o `RuntimeConfigRegistry` inicializar, consumidores recebem apenas leitura.

Não deve existir mutação runtime do `RuntimeConfigSetAsset` ou dos grupos.

### 4. Referências são explícitas

Não usar:

- auto-scan de pasta;
- busca por nome;
- convenção implícita;
- fallback por Resources genérico, exceto a entrada canônica já existente do modo.

### 5. Config obrigatória ausente falha cedo

Ausência de config obrigatória deve causar falha explícita no bootstrap ou na validação do grupo.

Não pode haver fallback silencioso.

### 6. Hierarquia deve ser rasa

A hierarquia canônica é:

```text
RuntimeModeConfig
-> RuntimeConfigSetAsset
-> Config Groups
```

Evitar árvores profundas de configs que escondem ownership.

### 7. Adapters não devem reconsultar config como decisão

Adapters podem ler config para executar um comando.
Eles não devem reconsultar config continuamente para decidir lifecycle.

### 8. BootstrapConfigAsset fora do trilho canônico da Base 1.1

`BootstrapConfigAsset` não participa do runtime canônico.

Mas a fonte canônica ativa na Base 1.1 permanece:

```text
RuntimeModeConfig
-> RuntimeConfigSetAsset
-> RuntimeConfigRegistry
-> RuntimeConfigSnapshot read-only
```

---

## Relação com pipelines e adapters

### Pipelines

Pipelines podem consumir configurações validadas como entrada de policy/default.

Eles continuam sendo owners de:

- lifecycle;
- ordem;
- readiness;
- decisions;
- handoffs;
- proteção contra `foreign/stale events`.

### Adapters

Adapters podem consumir config para executar side-effects comandados.

Exemplos:

- `AudioAdapter` lê config de áudio para executar playback comandado;
- `LoadingAdapter` lê profile de loading para executar loading comandado;
- `SaveAdapter` lê backend/config para executar save comandado;
- `InputModeCoordinator` usa config técnica para aplicar request comandado.

Adapters não decidem quando a ação deve ocorrer.

---

## Organização física recomendada

Criar uma pasta canônica de assets de configuração é permitido para organização editorial.

Exemplo:

```text
Assets/_ImmersiveGames/NewScripts/Config/
ou
Assets/_ImmersiveGames/NewScripts/RuntimeConfig/
```

Mas essa pasta não é fonte runtime por si só.

A fonte runtime deve ser a referência explícita:

```text
RuntimeModeConfig.configSet
```

---

## Roadmap de migração

### Fase 1 - Estrutura mínima

Criar:

```text
RuntimeConfigSetAsset
RuntimeConfigRegistry
RuntimeConfigSnapshot
grupos mínimos vazios ou quase vazios
```

Adicionar ao `RuntimeModeConfig` uma referência explícita:

```text
runtimeConfigSet
```

Não remover campos antigos ainda.

### Fase 2 - Primeiro grupo real

Migrar um domínio simples primeiro.

Recomendado:

```text
SessionOperationalRuntimeConfigGroup
```

ou:

```text
AudioRuntimeConfigGroup
```

Critério:

- baixo risco;
- fácil validação;
- pouco acoplamento;
- sem mudança de comportamento.

### Fase 3 - Migração por domínio

Migrar grupos em passos pequenos:

```text
SessionOperationalRuntime
AudioRuntime
SaveRuntime
InputModesRuntime
RuntimePolicy
```

Cada grupo deve ter:

- validação própria;
- snapshot/read-only;
- logs de inicialização;
- ausência de fallback silencioso.

### Fase 4 - Limpeza do RuntimeModeConfig

Depois que os consumidores canônicos usarem `RuntimeConfigRegistry`, remover os campos antigos migrados do `RuntimeModeConfig`.

Não manter dois owners ativos.

### Fase 5 - Validação e documentação

Adicionar validações manuais/automáticas conforme necessário:

- config set ausente;
- grupo obrigatório ausente;
- referência obrigatória ausente;
- referência duplicada, quando aplicável;
- logs de snapshot carregado.

---


## Checkpoint RuntimeConfig / wiring obrigatório - CLOSED (2026-05-17)

- `RuntimeConfigRegistry` + `RuntimeConfigSetAsset` é o trilho canônico de configuração.
- `RuntimeModeConfig` permanece entry-point de modo e referencia explicitamente `RuntimeConfigSetAsset`.
- `RuntimeModeConfig` não carrega policy de domínio duplicada.
- `RuntimeModeConfig.inputModes` foi removido.
- `RuntimeModeConfigLoader` foi removido.
- `BootstrapConfigAsset` foi removido do caminho ativo da Base 1.1.
- `CompositionProfileKind.Base11Sandbox` é o profile canônico ativo.
- Profiles não suportados falham explicitamente (fail-fast).
- Config fornece dados; não decide lifecycle.
- Pipelines decidem lifecycle/ordem/policy/handoff.
- Adapters executam side-effects comandados.
- Sem fallback silencioso e sem compat paralelo no trilho canônico.

## Checkpoint aplicado (2026-05-13)

### Escopo concluido

- RuntimeConfigRegistry inicializa no boot canonico (step RuntimePolicy) e valida RuntimeConfigSetAsset.
- RuntimePolicyConfigGroup aplicado via snapshot/read-only.
- AudioRuntimeConfigGroup aplicado via snapshot/read-only.
- PreferencesRuntimeConfigGroup aplicado via snapshot/read-only.
- SessionOperationalRuntimeConfigGroup aplicado via snapshot/read-only.
- SaveRuntimeConfigGroup aplicado via snapshot/read-only.
- InputModesRuntimeConfigGroup aplicado com:
    - Configuracao de input modes (nomes de maps, flags, bindings).
    - **NOVO (2026-05-14)**: Inicializacao operacional de input UI (ADR-0009):
        - `maxPlayerSlots`;
        - `uiActionsAsset`;
        - 10 `InputActionReferences` canonicas;
        - Snapshot read-only, nao reconfiguração em runtime.
    - **Checkpoint (2026-05-21)**: disponibilidade fisica do runtime operacional de input:
        - `InputRuntimeRoot` em `UIGlobalScene`;
        - `PlayerInputManager`, `EventSystem` e `InputSystemUIInputModule` sob root persistente;
        - `NewBootstrap` fora do ownership do input operacional persistente.
- RuntimeModeConfig higienizado para permanecer asset puro/entry point.

### Campos removidos de RuntimeModeConfig (migrados)

- runtimePersistentScenesPolicy
- audioDefaults
- defaultLoadingMode
- defaultLoadingProfile
- reporter
- strictness
- startupRouteDefinition
- saveConfig

### Estado atual de ownership

- Owner canonico das configs migradas: RuntimeConfigSetAsset (via RuntimeConfigRegistry snapshot).
- RuntimeModeConfig permanece com responsabilidades de entrada de modo e bootstrap-level.
- `InputModes` é resolvido exclusivamente por `InputModesRuntimeConfigGroup` via `RuntimeConfigRegistry`.
- `PreferencesRuntime` não usa `BootstrapConfigAsset.videoDefaults`; resolução canônica vem do `RuntimeConfigRegistry`.

### Estado atual - Save no SessionOperational

- `SaveRuntimeConfigGroup` fornece config read-only para execução.
- Decisão de lifecycle permanece no pipeline:
    - `load-on-enter` e `save-on-exit` são decididos pelo `SessionOperationalPipeline`.
- `SaveRuntime`/adapter executa side-effects comandados.
- `SessionOperationalActivitySaveAdapter` usa `ISaveService` por `SaveAddress`/`SaveRequest`.
- `RouteActivitySave` consome `ProgressionSlotContext` resolvido por `IProgressionSlotContextResolver`.
- Ausência de snapshot de activity não gera fallback silencioso; checkpoint atual observa `no_snapshot_provider`.

### Itens adiados

- InputModesRuntimeConfigGroup foi congelado com inicializacao operacional de input UI (ADR-0009).
- Save/Preferences esta em PASS funcional com backend PlayerPrefs tecnico provisorio.
- Progression Save esta em PASS estrutural de contratos/encaixe passivo, mas ainda sem persistencia funcional de gameplay.
- Permanecem adiados: manifest/header real de snapshots, providers/receivers reais, autosave/manual/checkpoint, UI de slots e Run save.
- Fora do escopo deste checkpoint:
    - Save/Progression real;
    - Activity Snapshot Provider;
    - Activity lifecycle/deactivation;
    - PlayerActor final;
    - gameplay input final;
    - Run Pipeline.

---
## Não objetivos

Este ADR não introduz:

- framework genérico de config;
- hot reload;
- runtime profile switching;
- herança de configs;
- auto-discovery de assets;
- override silencioso;
- fallback para defaults globais;
- Base 2.0;
- reestruturação física ampla do projeto.

---

## Consequências

- `RuntimeModeConfig` tende a emagrecer.
- Configs por domínio ficam mais fáceis de validar.
- O boot fica mais fail-fast.
- Pipelines continuam owners de decisão.
- Adapters continuam executores.
- O projeto ganha uma forma limpa de adicionar novos domínios de config sem inflar o `RuntimeModeConfig`.
- A organização física de assets pode melhorar sem virar fonte runtime implícita.

---

## Relação com ADRs existentes

- **ADR-0001**: preserva Base 1.1 como Pipeline Convergence, sem virar Base 2.0.
- **ADR-0003**: `SessionOperationalPipeline` continua owner do ciclo operacional.
- **ADR-0005**: configs fornecem dados; módulos produzem facts/commands; adapters executam side-effects.
- **ADR-0006**: rota, loading, fade, scene composition e audio continuam comandados por pipeline/adapters.
- **ADR-0007**: InputModes continuam executores, não owners de lifecycle.
- **ADR-0008**: Save usa config/backend, mas decisão de save continua nos pipelines.
- **ADR-0009**: InputModesRuntimeConfigGroup referencia `OperationalInputRuntimeProfileAsset`, que fornece `maxPlayerSlots`, `uiActionsAsset` e 10 `InputActionReferences` canonicas para inicializacao operacional de input UI.
- **ADR-0010**: evolucoes de Actor Preparation devem entrar por grupos/catálogos explícitos, não por expansão indefinida do `RuntimeModeConfig`.
- **ADR-0012**: CameraRuntimeConfigGroup fornece `operationalCameraPrefab` e `operationalCameraIdentity` para inicialização operacional de câmera.

---

## Critério de aceite

Este ADR será considerado aplicado quando:

```text
RuntimeModeConfig referenciar RuntimeConfigSetAsset.
RuntimeConfigRegistry validar o config set no bootstrap.
Ao menos um domínio usar config via registry.
Nenhuma config obrigatória ausente passar silenciosamente.
Nenhum pipeline perder ownership de lifecycle/policy/handoff.
```
