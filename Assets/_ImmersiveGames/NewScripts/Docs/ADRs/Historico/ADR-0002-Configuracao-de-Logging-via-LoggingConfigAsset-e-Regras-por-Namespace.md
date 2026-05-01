# ADR-0002 - Configuracao de Logging via LoggingConfigAsset e Regras por Namespace

## Status

- Estado: Aceito
- Data (decisao): 2026-03-18
- Ultima atualizacao: 2026-03-18
- Tipo: Implementacao
- Escopo: Core/Logging + Infrastructure/Bootstrap (NewScripts)
- Decisores: Time World / Arquitetura NewScripts
- Tags: Logging, Bootstrap, Observability, Core, Infrastructure

## Contexto

O modulo de logging do projeto perdeu a configuracao asset-driven no boot e passou a depender de policy hardcoded combinada com flags globais e mecanismos runtime ja existentes em `DebugUtility`.

Essa situacao gerou dois problemas principais:

1. perda de controle operacional por dominio arquitetural
2. mistura indevida entre selecao de bootstrap e detalhes internos da policy de logging

Na auditoria do codigo real, dois problemas concretos explicaram logs que continuavam aparecendo mesmo com o `LoggingConfigAsset` restringindo namespaces:

1. prefixo incorreto no asset canonico
   - observado: `ImmersiveGames.NewScripts.Infrastructure`
   - namespace real: `_ImmersiveGames.NewScripts.Infrastructure`
   - efeito: a regra de `Infrastructure` nao casava e os logs caiam no `defaultLevel`

2. namespace fora da taxonomia modular esperada
   - observado: `PostRunOverlayController` em namespace fora de `_ImmersiveGames.NewScripts.Modules.*`
   - efeito: a regra ampla de `Modules` nao casava e o `[DebugLevel(DebugLevel.Verbose)]` permanecia ativo

O objetivo desta decisao e restaurar a policy asset-driven, sem criar pipeline paralelo, sem mudar os call sites atuais de `DebugUtility` e sem usar override operacional por classe/tipo no Inspector.

## Decisao

### 1. Fonte de verdade

A policy operacional de logging do runtime fica no `LoggingConfigAsset`.

O `BootstrapConfigAsset` apenas seleciona qual `LoggingConfigAsset` esta ativo no boot atual.

O asset de bootstrap nao deve carregar:

- listas de excecao de logging
- overrides operacionais por classe
- detalhes internos da policy do modulo

### 2. Modelo de configuracao

O `LoggingConfigAsset` deve conter, no minimo:

- `globalEnabled`
- `verboseEnabled`
- `fallbacksEnabled`
- `repeatedVerboseEnabled`
- `defaultLevel`
- `rules`

Cada entrada de `rules` deve conter:

- `ruleId`
- `enabled`
- `namespacePrefix`
- `level`

O `namespacePrefix` e uma string operacional.
Nao ha `TypeReference` nem override operacional por classe/tipo no asset.

### 3. Resolucao por namespace

Ao resolver o nivel efetivo para um tipo:

1. obter `type.Namespace`
2. localizar todas as regras cujo `namespacePrefix` casa via `StartsWith`
3. selecionar a regra mais especifica
4. aplicar `longest-prefix match`
5. se nao houver match, usar `defaultLevel`

Exemplo:

- `_ImmersiveGames.NewScripts.Modules.SceneFlow` -> `Warning`
- `_ImmersiveGames.NewScripts.Modules.SceneFlow.Transition` -> `Verbose`

Nesse caso:

- `SceneFlow.*` usa `Warning`
- `SceneFlow.Transition.*` usa `Verbose`
- tipos sem match usam `defaultLevel`

### 4. Precedência efetiva

A ordem final obrigatória é:

1. local instance override já existente
2. type override runtime já existente
3. namespace rule do `LoggingConfigAsset`
4. `[DebugLevel]`
5. `defaultLevel`

Essa ordem preserva compatibilidade com o comportamento anterior de `DebugUtility` e corrige o controle por namespace para que ele tenha precedência sobre o atributo.

### 5. Boot e source da policy

`GlobalCompositionRoot.InitializeLogging()` deve tentar resolver cedo:

`RuntimeModeConfig -> BootstrapConfigAsset -> LoggingConfigAsset`

Comportamento:

- se `LoggingConfigAsset` existir:
  - aplicar a policy asset-driven
  - registrar observabilidade clara com `source='BootstrapConfigAsset/RuntimeModeConfig'` ou equivalente
- se `BootstrapConfigAsset` existir mas `loggingConfig` estiver nulo:
  - manter fallback hardcoded
  - registrar fallback explícito
- se ainda não for possível resolver `BootstrapConfigAsset`:
  - manter fallback hardcoded
  - registrar o motivo

Não existe segundo estágio de policy.
Não existe pipeline paralelo de logging.

### 6. Semântica de fases e severidade

`BOOT`, `STARTUP` e `RUNTIME` são tags/fases semânticas, não `DebugLevel`.

- `BOOT`
  - logs emitidos antes da policy final do `LoggingConfigAsset`
  - inclui reset do `DebugUtility` e aplicação de `EarlyDefault`

- `STARTUP`
  - logs da montagem inicial principal
  - inclui composition roots, serviços registrados, scene scopes criados e policy final aplicada no boot

- `RUNTIME`
  - logs da operação normal após o startup

Os níveis continuam sendo:

- `ERROR`: falha real / impeditiva / inconsistente
- `WARNING`: degradação, fallback, comportamento inesperado, config parcial
- `INFO`: marco operacional importante
- `VERBOSE`: detalhe de continuidade, fluxo e rastreio fino

Definição semântica:

- `INFO` representa registrado, pronto, carregado, resolvido, operacional
- `VERBOSE` representa continuidade de fluxo, etapa interna, rastreio fino

### 7. EarlyDefault

Antes da policy final, o `DebugUtility` aplica uma policy conservadora:

- `globalEnabled = true`
- `defaultLevel = Logs`
- `verboseEnabled = false`
- `fallbacksEnabled = false`
- `repeatedVerboseEnabled = false`

Essa janela de `BOOT` preserva diagnóstico mínimo sem reabrir ruído excessivo no early boot.

### 8. Cache e reaplicação

Se houver cache `Type -> nível efetivo` ou `Type -> regra casada`:

- ele deve ser invalidado ao reaplicar policy
- ele deve ser invalidado ao trocar a source de policy
- a reaplicação da policy final não deve deixar resíduos de `EarlyDefault`

A observabilidade da policy deve registrar:

- source ativa
- `defaultLevel`
- quantidade de regras ativas
- invalidação de cache
- se a policy ativa é `EarlyDefault` ou `BootstrapConfigAsset`

### 9. Validações leves do asset

O `LoggingConfigAsset` deve, no mínimo:

- aplicar `Trim()` em `namespacePrefix`
- alertar `namespacePrefix` vazio
- alertar `ruleId` duplicado
- alertar `namespacePrefix` duplicado
- alertar prefixo suspeito sem underscore inicial quando parecer namespace do projeto

Sem fail-fast pesado aqui, salvo corrupção estrutural grave.

### 10. Não-objetivos

- não criar override operacional por classe/tipo no asset
- não criar dropdown gigante de tipos/classes
- não criar UI de runtime para alternância de logging
- não reescrever call sites atuais de `DebugUtility`
- não transformar `BootstrapConfigAsset` em container de detalhes internos do módulo

## Consequências

### Benefícios

- configuração por feature real, alinhada aos namespaces do projeto
- menor ruído operacional por domínio arquitetural
- granularidade natural por módulo e submódulo
- bootstrap menos acoplado aos detalhes da policy
- melhor auditabilidade da configuração

### Custos e riscos

- dependência de string por namespace
- necessidade de manter prefixos coerentes com a arquitetura real
- sobreposição de regras pode causar confusão sem disciplina
- namespaces fora da taxonomia esperada podem escapar de regras amplas

## Política de falhas e fallback

A ausência de `LoggingConfigAsset` não deve quebrar o boot por padrão.

A política recomendada é:

- fallback explícito e observável quando `LoggingConfigAsset` não existir
- fail-fast apenas em corrupção estrutural grave, quando não houver nem policy válida nem fallback seguro

## Critérios de pronto

- `BootstrapConfigAsset` referencia apenas `LoggingConfigAsset` no aspecto de logging
- `LoggingConfigAsset` concentra toda a policy operacional
- regras por `namespacePrefix` funcionam com `longest-prefix match`
- a regra de namespace tem precedência sobre `[DebugLevel]`
- é possível configurar um módulo inteiro para `None`, `Warning`, `Logs` ou `Verbose`
- é possível tornar um submódulo mais verboso que o módulo pai
- o boot aplica a policy do asset quando configurada
- o fallback hardcoded continua funcional quando a config não existir, com log explícito
- nenhum call site existente de logging precisa ser alterado
- `BOOT`, `STARTUP` e `RUNTIME` estão padronizados como fases/tags, não como levels
- o sistema não usa override operacional por classe no asset

## Implementacao (arquivos impactados)

- `Assets/_ImmersiveGames/NewScripts/Core/Logging/DebugUtility.cs`
- `Assets/_ImmersiveGames/NewScripts/Core/Logging/Config/LoggingConfigAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Config/BootstrapConfigAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.BootstrapConfig.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.cs`

## Evidencia

- evidencia bruta: `Docs/Reports/lastlog.log`
- evidencia canonica: `Docs/Reports/Evidence/LATEST.md`
- ancoras relevantes:
  - aplicacao de `EarlyDefault`
  - aplicacao da policy final via `BootstrapConfigAsset`
  - fallback explicito quando `LoggingConfigAsset` nao esta configurado

## Referencias

- `Assets/_ImmersiveGames/NewScripts/Core/Logging/DebugUtility.cs`
- `Assets/_ImmersiveGames/NewScripts/Core/Logging/Config/LoggingConfigAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Config/BootstrapConfigAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`

