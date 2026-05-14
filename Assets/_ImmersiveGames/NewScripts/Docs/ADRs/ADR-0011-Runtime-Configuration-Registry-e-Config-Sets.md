# ADR-0011 - Runtime Configuration Registry e Config Sets

## Status

- Estado: Applied (parcial)
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
- SaveRuntimeConfigGroup
- InputModesRuntimeConfigGroup
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

### InputModesRuntimeConfigGroup

Agrupa configurações técnicas de input modes e inicialização operacional de input UI.

Exemplos de Input Modes:

- nomes de maps;
- flags de habilitação;
- bindings técnicos usados pelo executor.

Exemplo de Inicialização Operacional de Input UI (ADR-0009):

- `maxPlayerSlots` (capacidade operacional de entrada);
- `uiActionsAsset` (asset de UI actions canonico);
- 10 `InputActionReferences` canonicas obrigatórias:
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

- Config é read-only via snapshot;
- Todas as 10 referencias devem pertencer ao mesmo `uiActionsAsset`;
- Ausência de qualquer referência é erro fail-fast no bootstrap;
- Binding é executado por `UnityOperationalInputRuntimeAdapter` (ADR-0009);
- Não há reconfiguração de binding em runtime.

A decisão de quando trocar input mode continua pertencendo ao pipeline.

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

### 8. BootstrapConfigAsset não vira fonte canônica da Base 1.1

`BootstrapConfigAsset` pode continuar existindo como infraestrutura/legado de boot quando necessário.

Mas a fonte canônica de modo na Base 1.1 permanece:

```text
RuntimeModeConfig
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


## Checkpoint aplicado (2026-05-13)

### Escopo concluido

- RuntimeConfigRegistry inicializa no boot canonico (step RuntimePolicy) e valida RuntimeConfigSetAsset.
- RuntimePolicyConfigGroup aplicado via snapshot/read-only.
- AudioRuntimeConfigGroup aplicado via snapshot/read-only.
- SessionOperationalRuntimeConfigGroup aplicado via snapshot/read-only.
- SaveRuntimeConfigGroup aplicado via snapshot/read-only.
- InputModesRuntimeConfigGroup aplicado com:
  - Configuracao de input modes (nomes de maps, flags, bindings).
  - **NOVO (2026-05-14)**: Inicializacao operacional de input UI (ADR-0009):
    - `maxPlayerSlots`;
    - `uiActionsAsset`;
    - 10 `InputActionReferences` canonicas;
    - Snapshot read-only, nao reconfiguração em runtime.
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

### Item adiado

- Nenhum item adiado neste checkpoint. InputModesRuntimeConfigGroup foi congelado com inicializacao operacional de input UI (ADR-0009).

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
- **ADR-0009**: InputModesRuntimeConfigGroup fornece `maxPlayerSlots`, `uiActionsAsset` e 10 `InputActionReferences` canonicas para inicializacao operacional de input UI.
- **ADR-0010**: evolucoes de Actor Preparation devem entrar por grupos/catálogos explícitos, não por expansão indefinida do `RuntimeModeConfig`.

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




