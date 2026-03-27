# ADR-0002 LoggingConfigAsset Implementation Round 1

## 1. Resumo
Implementacao do contrato canonico ADR-0002 no modulo de logging de `NewScripts`, restaurando policy asset-driven por `LoggingConfigAsset`, com resolucao por namespace (`StartsWith` + `longest-prefix match`), precedencia final canonica e boot em estagio unico (`EarlyDefault` -> tentativa de policy final -> fallback hardcoded explicito quando necessario).

Pontos-chave entregues:
- `LoggingConfigAsset` adicionado como fonte unica da policy operacional.
- `BootstrapConfigAsset` renomeado e reduzido ao papel de seletor de `LoggingConfigAsset`.
- `DebugUtility` com engine de policy por namespace + cache/invalidation.
- Fluxo de boot do logging ajustado para `RuntimeModeConfig -> BootstrapConfigAsset -> LoggingConfigAsset` sem pipeline paralelo.
- Assets canonicos em `Assets/Resources` atualizados para policy funcional de ponta a ponta.

## 2. Estado anterior encontrado
Estado auditado antes da implementacao:
- `DebugUtility` aplicava policy hardcoded no boot (`defaultLevel=Verbose`, flags por `Application.isEditor`) via `ApplyLoggingPolicyFromBootstrap`, sem uso de `LoggingConfigAsset`.
- Nao existia `Core/Logging/Config/LoggingConfigAsset.cs` no codigo.
- `DebugUtility` tinha precedencia parcial (instance + type override + atributo/default), mas sem camada de regras por namespace do asset.
- `GlobalCompositionRoot.InitializeLogging()` aplicava apenas fallback hardcoded.
- `GlobalCompositionRoot.BootstrapConfig` tinha apenas caminho fail-fast (para fluxos estruturais), sem caminho nao fatal para resolver config de logging no early boot.

Problemas concretos do ADR confirmados na auditoria:
1. Prefixo sem `_` inicial causa mismatch real de namespace (`ImmersiveGames.NewScripts.*` nao casa com `_ImmersiveGames.NewScripts.*`).
2. Classe critica fora da taxonomia modular ampla: `PostGameOverlayController` em namespace `_ImmersiveGames.NewScripts.Gameplay.PostGame` (fora de `_ImmersiveGames.NewScripts.Modules.*`).

## 3. Contrato do ADR aplicado
Contrato aplicado no codigo:
- Fonte de verdade operacional: `LoggingConfigAsset`.
- `BootstrapConfigAsset` referencia apenas `LoggingConfigAsset` para aspecto de logging.
- Sem overrides operacionais por tipo/classe no asset.
- Sem pipeline paralelo e sem segundo estagio de policy.
- Sem alteracao de call sites de `DebugUtility`.
- `BOOT`, `STARTUP`, `RUNTIME` usados como tags semanticas nos logs, sem alterar enum `DebugLevel`.

## 4. Precedencia final implementada
Precedencia efetiva em `DebugUtility.ShouldLog`/`ResolveEffectiveLevel`:
1. local instance override (`_localLevels`)
2. type override runtime (`_scriptDebugLevels` via `RegisterScriptDebugLevel`)
3. namespace rule (`_activeNamespaceRules`, `StartsWith`, longest-prefix)
4. `[DebugLevel]`
5. `defaultLevel`

Garantias implementadas:
- Regra de namespace vence atributo `[DebugLevel]`.
- Override runtime por tipo continua vencendo namespace/atributo/default.
- Override local por instancia continua no topo.

## 5. Resolucao por namespace (`StartsWith` + `longest-prefix match`)
Implementacao em `DebugUtility`:
- Leitura de `type.Namespace`.
- Regras ativas compiladas a partir de `LoggingConfigAsset.rules`.
- Ordenacao por comprimento de prefixo descendente (mais especifica primeiro), com desempate estavel por `ruleId`.
- Match por `typeNamespace.StartsWith(namespacePrefix, StringComparison.Ordinal)`.
- Fallback para `defaultLevel` quando nenhum prefixo casar.

Isso permite:
- configurar modulo pai para um nivel;
- configurar submodulo mais especifico para nivel diferente;
- manter classes sem match sob `defaultLevel`.

## 6. Boot flow (`EarlyDefault` -> BootstrapConfigAsset/RuntimeModeConfig -> LoggingConfigAsset`)
Fluxo final em `GlobalCompositionRoot.InitializeLogging()`:
1. aplica `EarlyDefault` conservador:
   - `globalEnabled=true`
   - `defaultLevel=Logs`
   - `verboseEnabled=false`
   - `fallbacksEnabled=false`
   - `repeatedVerboseEnabled=false`
2. tenta resolver bootstrap cedo via caminho nao fatal:
   - `RuntimeModeConfigLoader.LoadOrNull()`
   - `RuntimeModeConfig.BootstrapConfig`
3. se `BootstrapConfigAsset.LoggingConfig` existir:
   - aplica policy final asset-driven via `DebugUtility.ApplyLoggingPolicyFromAsset`
4. se bootstrap existir mas `loggingConfig == null`:
   - aplica fallback hardcoded legado com log explicito
5. se bootstrap nao resolver:
   - aplica fallback hardcoded legado com motivo explicito

Sem segundo estagio e sem trilho paralelo.

## 7. Cache e invalidacao
Caches adicionados/ajustados em `DebugUtility`:
- `Type -> effective level` (`_effectiveLevels`)
- `Type -> matched rule` (`_matchedNamespaceRules`)
- cache de atributo (`_attributeLevels`) reaproveitado

Invalidacao garantida:
- ao reaplicar policy (`ApplyLoggingPolicyInternal`)
- ao trocar source/key de policy
- ao atualizar type override runtime (`RegisterScriptDebugLevel`)

Observabilidade de invalidacao:
- log canonico com contagem de entradas invalidadas por cache.

## 8. Validacoes do LoggingConfigAsset
Validacoes leves implementadas em `LoggingConfigAsset.OnValidate()`:
- `Trim()` em `ruleId` e `namespacePrefix`
- warning para `namespacePrefix` vazio
- warning para `ruleId` duplicado
- warning para `namespacePrefix` duplicado
- warning para prefixo suspeito sem `_` inicial quando aparenta namespace do projeto

Sem fail-fast pesado nessas validacoes.

## 9. Inconsistencias reais encontradas em namespaces/prefixos
Inconsistencias confirmadas e tratadas:
- Prefixos sem underscore inicial sao suspeitos e agora recebem warning no asset.
- `PostGameOverlayController` esta em `_ImmersiveGames.NewScripts.Gameplay.PostGame`, fora de `_ImmersiveGames.NewScripts.Modules.*`.

Acao aplicada para o ADR funcionar sem refactor de namespace:
- regra dedicada no `Assets/Resources/LoggingConfig.asset`:
  - `namespacePrefix: _ImmersiveGames.NewScripts.Gameplay.PostGame`

Tambem foram aplicadas regras com `_` inicial correto nos prefixos principais.

## 10. Arquivos alterados
Criados:
- `Assets/_ImmersiveGames/NewScripts/Core/Logging/Config/LoggingConfigAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Core/Logging/Config/LoggingConfigAsset.cs.meta`
- `Assets/_ImmersiveGames/NewScripts/Core/Logging/Config.meta`
- `Assets/Resources/LoggingConfig.asset`
- `Assets/Resources/LoggingConfig.asset.meta`

Renomeados:
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs` -> `Assets/_ImmersiveGames/NewScripts/Infrastructure/Config/BootstrapConfigAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs.meta` -> `Assets/_ImmersiveGames/NewScripts/Infrastructure/Config/BootstrapConfigAsset.cs.meta`

Alterados:
- `Assets/_ImmersiveGames/NewScripts/Core/Logging/DebugUtility.cs`
- `Assets/_ImmersiveGames/NewScripts/Core/Logging/DebugUtility.cs.meta`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/RuntimeModeConfig.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.BootstrapConfig.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Coordinator.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.LevelFlow.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Navigation.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlowRoutes.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Editor/Validation/SceneFlowConfigValidator.cs`
- `Assets/Resources/NewScriptsBootstrapConfig.asset`
- `Assets/Resources/RuntimeModeConfig.asset`

## 11. Logs/evidencias adicionados
Exemplos de logs canonicos adicionados/ajustados:
- `"[BOOT][Logging] EarlyDefault policy applied."`
- `"[STARTUP][Logging] Final policy applied from LoggingConfigAsset. source='BootstrapConfigAsset/<via>' asset='<name>'."`
- `"[STARTUP][Logging] Applied hardcoded fallback logging policy. reason='<reason>'."`
- `"[OBS][BOOT] LoggingPolicyCacheInvalidated reason='policy_reapplied' effectiveTypeCount=<n> matchedRuleCount=<n> attributeCount=<n>"`
- `"[OBS][BOOT|STARTUP] LoggingPolicyApplied source='<source>' policy='<EarlyDefault|BootstrapConfigAsset>' defaultLevel='<level>' activeRuleCount=<n> ..."`

Esses logs cobrem:
- aplicacao de `EarlyDefault`
- aplicacao de policy final via bootstrap/runtime mode
- fallback explicito
- source ativa
- contagem de regras
- invalidacao de cache

## 12. Sanity checks
Checks executados:
- Build `Assembly-CSharp.csproj`: **PASS** (0 erros, warnings preexistentes de nullability/assembly refs).
- Verificacao de call sites: sem alteracoes de assinatura/uso de `DebugUtility.Log*` nos consumidores.
- Verificacao de precedencia no codigo: implementada na ordem ADR (`instance > type runtime > namespace > attribute > default`).
- Verificacao de namespace matching: `StartsWith` + longest-prefix com fallback para `defaultLevel`.
- Verificacao de fallback de boot: caminho explicito quando bootstrap/logging config ausente.
- Verificacao de assets canonicos:
  - `RuntimeModeConfig.asset` aponta `bootstrapConfig`
  - `NewScriptsBootstrapConfig.asset` aponta `loggingConfig`
  - `LoggingConfig.asset` criado com regras iniciais corretas + excecao de PostGame legado.

## 13. Limitacoes e proximos passos
Limitacoes desta rodada:
- Nao houve refactor de namespace de `PostGameOverlayController` (decisao intencional para minimizar risco de serializacao).
- Validacoes de `LoggingConfigAsset` sao leves (warnings), sem hard fail por configuracao incompleta.

Proximos passos recomendados:
1. Avaliar migracao futura de `_ImmersiveGames.NewScripts.Gameplay.PostGame` para taxonomia modular (`Modules.PostGame`) para simplificar regras de logging.
2. Revisar e calibrar niveis das regras canonicas no `LoggingConfig.asset` com base em telemetria de ruido por modulo.
3. Opcional: adicionar testes editoriais automatizados de precedencia (namespace > atributo) para evitar regressao.
