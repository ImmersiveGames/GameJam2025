# RuntimeMode + Logging

## Build matrix
- Fonte unica: `Docs/Shared/Build-Matrix.md`.
- Este modulo referencia a matriz compartilhada para evitar drift entre RuntimeMode/Logging e DevQA.
- Leitura local: `RuntimeMode/Logging` consome o contrato `Editor / DevBuild / Release`, mas nao redefine a tabela aqui.

## Governanca de Flags
| Flag / family | Tipo | Owner canonico | Proposito | Risco de drift |
|---|---|---|---|---|
| `UNITY_EDITOR` | compile-time | `Editor/**`, `Modules/**/Editor/**`, blocos editor-only em `Dev/**` | liberar tooling, `MenuItem`, `ContextMenu`, validacao e evidencia editor-only | alto se vazar para runtime fora de `Editor/**` |
| `DEVELOPMENT_BUILD` | compile-time | trilho `Dev/**`, `GlobalCompositionRoot.DevQA`, helpers DevBuild | habilitar harness DevQA em player distribuido e hooks de evidencia | medio se virar substituto informal de policy runtime |
| `NEWSCRIPTS_BASELINE_ASSERTS` | compile-time | `Infrastructure/Composition/GlobalCompositionRoot.Baseline.cs`, `Infrastructure/Observability/Baseline/**` | ligar asserts/invariantes de baseline sem alterar o boot principal | medio se se espalhar para features fora de baseline |
| `NEWSCRIPTS_MODE` | runtime gate / compile symbol observado | `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs` + observabilidade em `Core/Logging/DebugUtility.cs` | ligar/desligar o bootstrap canonico do NewScripts no startup | alto porque impacta o boot inteiro |
| `NEWSCRIPTS_*` residual | familia de guards | inventario em `Infrastructure/**`, `Modules/**`, `Core/**` | documentar simbolos remanescentes e evitar policy paralela | alto se novos simbolos entrarem sem doc shared |

## Layering contract
- Build/guard governance tem uma unica fonte de verdade em `Docs/Shared/Build-Matrix.md`.
- `RuntimeMode/Logging` e owner do boot logging e da policy runtime: `EarlyDefault`, `BootstrapPolicy`, dedupe e `policyKey`.
- `DevQA` pode oferecer harness, menu de evidencia e installers de dev, mas nao muda boot order nem cria writer paralelo de logging policy.
- A allowlist de `RuntimeInitializeOnLoadMethod` fora de `Dev/**`, `Editor/**`, `Legacy/**`, `QA/**` permanece congelada em `DebugUtility` e `GlobalCompositionRoot.Entry`.
- Qualquer novo toggle `NEWSCRIPTS_*` ou novo bootstrap point exige evidencia local e atualizacao no doc shared.

## Ownership table
| Component | Owner de | Nao-owner de | Anchors |
|---|---|---|---|
| `Infrastructure/Composition/GlobalCompositionRoot.Entry` | entrypoint canonico de boot; logging default inicial | policy fina de runtime mode; configuracao persistente de logging | `[RuntimeInitializeOnLoadMethod]`, `InitializeLogging()`, `NewScripts logging configured.`, `DependencyManager created for global scope.` |
| `Core/Logging/DebugUtility` | superficie canonica da logging policy runtime (`SetDefaultDebugLevel`, `SetVerboseLogging`, `SetLogFallbacks`, `SetRepeatedCallVerbose`) | selecao de strict/release; carregamento de config asset | `DebugUtility.Initialize`, `DebugUtility inicializado antes de todos os sistemas.` |
| `Infrastructure/RuntimeMode/RuntimeModeConfigLoader` | resolucao canonica de `RuntimeModeConfig` via DI global + `Resources` | decisao de severidade de log; bootstrap entrypoint | `Resolve RuntimeModeConfig via DI global e fallback por Resources` |
| `Infrastructure/RuntimeMode/UnityRuntimeModeProvider` | fallback canonico de modo (`Strict/Release`) a partir de `UNITY_EDITOR`/`DEVELOPMENT_BUILD` | config asset; degraded reporting | comentario de contrato `Strict/Release` + `Current` |
| `Infrastructure/RuntimeMode/ConfigurableRuntimeModeProvider` | override canonico de `RuntimeModeConfig.modeOverride` sobre o fallback | logging config dev; boot | `ForceStrict`, `ForceRelease`, `Auto` |
| `Infrastructure/Composition/GlobalCompositionRoot.RuntimePolicy` | registro DI de `RuntimeModeConfig`, `IRuntimeModeProvider`, `IDegradedModeReporter`, `IWorldResetPolicy` | aplicacao de flags de `DebugUtility` | `[RuntimePolicy] RuntimeModeConfig carregado`, `[RuntimePolicy] IRuntimeModeProvider + IDegradedModeReporter + IWorldResetPolicy registrados no DI global.` |
| `Infrastructure/RuntimeMode/DegradedModeReporter` | owner canonico de policy de `DEGRADED_MODE` e `DEGRADED_SUMMARY` | boot do app; debug verbosity global | `DEGRADED_MODE`, `DEGRADED_SUMMARY` |
| `Dev/Core/Logging/DebugManagerConfig` | aplicacao manual/dev-only de `DebugLogSettings` sobre `DebugUtility` | policy runtime canonica de release; runtime mode | `[DebugManagerConfig] Settings aplicados` |
| `Dev/Core/Logging/DebugLogSettings` | storage dev-only para flags de debug | carregamento automatico em runtime canonico | `DebugLogSettings` asset |

## Timeline de boot (ordem real)
1. `Core/Logging/DebugUtility.Initialize` roda em `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)` e reseta estado interno de logging.
Anchor: `NEWSCRIPTS_MODE ativo: DebugUtility.Initialize executando reset de estado.` e `DebugUtility inicializado antes de todos os sistemas.`
2. `Infrastructure/Composition/GlobalCompositionRoot.Entry.Initialize` roda em `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)` e verifica `NEWSCRIPTS_MODE`.
Anchor: `NEWSCRIPTS_MODE desativado: GlobalCompositionRoot ignorado.`
3. `GlobalCompositionRoot.InitializeLogging()` calcula os inputs de bootstrap e delega o apply para `DebugUtility.ApplyLoggingPolicyFromBootstrap(...)`.
Anchor: `NewScripts logging configured.` + `[OBS][RuntimeMode] LoggingPolicyApplied source='BootstrapPolicy' ...`
4. `EnsureDependencyProvider()` cria o DI global, ainda antes da policy de runtime mode/config.
Anchor: `DependencyManager created for global scope.`
5. `RegisterEssentialServicesOnly()` entra no stage `RuntimePolicy` e chama `RegisterRuntimePolicyServices()`.
Anchor: stage inicial `CompositionInstallStage.RuntimePolicy` em `GlobalCompositionRoot.Pipeline.cs`.
6. `RegisterRuntimePolicyServices()` carrega `RuntimeModeConfig` por loader canonico, registra `IRuntimeModeProvider`, `IDegradedModeReporter` e `IWorldResetPolicy`.
Anchors: `[RuntimePolicy] RuntimeModeConfig carregado (asset=...)` e `[RuntimePolicy] IRuntimeModeProvider + IDegradedModeReporter + IWorldResetPolicy registrados no DI global.`
7. Com a config disponivel, `RegisterInputModesFromRuntimeConfig()` e consumers como `LoadingHudService`, `ProductionWorldResetPolicy` e `InPlaceContentSwapService` passam a obedecer o runtime mode/policy gate.
Anchor: `[InputMode] InputModes desabilitado via RuntimeModeConfig; IInputModeService nao sera registrado.`

## Redundancias candidatas (DOC-only)
- Writer duplo de policy de logging: `GlobalCompositionRoot.Entry.InitializeLogging()` (canonico runtime) e `DebugManagerConfig.ApplyConfiguration()` (dev-only).
- Toggle em duas camadas: `NEWSCRIPTS_MODE` liga/desliga o boot inteiro, enquanto `RuntimeModeConfig.modeOverride` governa strict/release depois do boot.
- `RuntimeModeConfigLoader` e `GlobalCompositionRoot.BootstrapConfig` fazem duas leituras correlatas da mesma config para props diferentes; hoje e intencional, mas e hotspot de acoplamento.

## Status atual
- `RM-1.1`: inventory done (DOC-only).
- Nao houve alteracao de `.cs` nesta etapa.


## RM-1.2
- Single owner runtime: `DebugUtility` agora concentra o apply real da logging policy no runtime.
- 2-phase apply preservado: `EarlyDefault` em `SubsystemRegistration` e `BootstrapPolicy` quando `GlobalCompositionRoot.Entry` inicializa o bootstrap.
- `GlobalCompositionRoot.Entry` deixou de escrever policy diretamente; ele agora apenas calcula inputs e chama `DebugUtility.ApplyLoggingPolicyFromBootstrap(...)`.
- Idempotencia adicionada por `policyKey` + frame guard, com anchors atuais:
  - `[OBS][RuntimeMode] LoggingPolicyApplied source=... key=...`
  - `[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_frame' key=...`
  - `[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_key' key=...`
- Comportamento intencional preservado: o boot continua com default early e promote para default verbose no bootstrap, sem tocar no trilho `Dev/Core/Logging` nesta etapa.




## RM-1.3
- Invariante 1: o boot continua em 2 fases, `EarlyDefault` seguido de `BootstrapPolicy`; keys diferentes nao devem ser dedupadas.
- Invariante 2: reaplicacao da mesma `policyKey` deve gerar observabilidade explicita, sem reaplicar a policy.
- Anchors canonicos de hardening:
  - `[OBS][RuntimeMode] LoggingPolicyApplied source=... key=...`
  - `[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_frame' key=...`
  - `[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_key' key=...`
- Evidencia manual em Editor: usar o menu `ImmersiveGames/NewScripts/Dev/Force LoggingPolicy Reapply Evidence` durante Play Mode.
- Evidencia manual em DevBuild: chamar `DebugUtility.Dev_ForceReapplyLastLoggingPolicyForEvidence()` a partir do harness Dev/QA existente.
- Sequencia esperada de log para a evidencia: primeiro um `dedupe_same_frame`, depois um `dedupe_same_key`, sem alterar a ordem do boot.

## RM-1.4
- `policyKey` agora nasce exclusivamente em `DebugUtility.BuildLoggingPolicyKey(...)`.
- O contrato da key e estavel/deterministico: ordem fixa de campos, shape fixo de string e sem depender de iteracao de colecoes ou valores nao-deterministicos.
- Se a key passar a incluir colecoes no futuro, elas devem ser ordenadas deterministicamente antes da serializacao.
- O contrato continua servindo aos anchors `LoggingPolicyApplied` e `LoggingPolicyApplySkipped`, sem mudar a ordem do boot `EarlyDefault -> BootstrapPolicy`.

## PolicyKey Contract (RM-1.4)
- Ordem fixa atual: `buildVariant|newscriptsMode|global=...;verbose=...;default=...;repeated=...|fallbacks=...|source`.
- A serializacao deve permanecer previsivel e culture-safe; quando houver formatos numericos, usar `InvariantCulture`.
- A key nao pode incluir frame, timestamp, GUID aleatorio ou qualquer dado que inviabilize o dedupe por igualdade textual.



