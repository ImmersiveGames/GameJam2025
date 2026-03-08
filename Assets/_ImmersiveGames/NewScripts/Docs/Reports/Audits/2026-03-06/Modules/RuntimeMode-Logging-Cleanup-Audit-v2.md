# RM-1.2 - LoggingPolicy single owner + idempotencia

Date: 2026-03-07
Status: behavior-preserving

## Objetivo
- consolidar o writer runtime de logging policy em um unico owner
- manter 2-phase apply (`EarlyDefault` + `BootstrapPolicy`)
- adicionar idempotencia por key/frame sem alterar a semantica do Baseline 3.1

## Evidencia pre-change
### Writers / apply sites
Comando:
```text
rg -n "ConfigureLogging|Configure.*Debug|Apply.*Log|DebugManagerConfig|DebugLogSettings|Log(Level|Settings)|Initialize.*Logging|logging configured" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Resultado curto:
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:76: InitializeLogging();`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:96: private static void InitializeLogging()`
- `Dev/Core/Logging/DebugManagerConfig.cs` com `DebugUtility.SetVerboseLogging/SetLogFallbacks/SetRepeatedCallVerbose/SetDefaultDebugLevel`
Resumo:
- writer runtime real antes: `GlobalCompositionRoot.Entry.InitializeLogging()`
- writer dev-only fora do escopo: `DebugManagerConfig.ApplyConfiguration()`

### Entrypoints
Comando:
```text
rg -n "RuntimeInitializeOnLoadMethod|InitializeOnLoadMethod" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Resultado curto:
- `Core/Logging/DebugUtility.cs:54: [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61: [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`

### Toggles usados na decisao
Comando:
```text
rg -n "NEWSCRIPTS_MODE|UNITY_EDITOR|DEVELOPMENT_BUILD|NEWSCRIPTS_BASELINE_ASSERTS" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Resultado curto:
- `Core/Logging/DebugUtility.cs`: `NEWSCRIPTS_MODE`
- `Infrastructure/RuntimeMode/UnityRuntimeModeProvider.cs`: `UNITY_EDITOR`, `DEVELOPMENT_BUILD`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`: `!NEWSCRIPTS_MODE`

## Implementacao aplicada
- `DebugUtility` virou o single owner runtime da escrita da policy via `ApplyLoggingPolicyInternal(...)`.
- `DebugUtility.Initialize()` passou a aplicar `EarlyDefault` usando a nova trilha idempotente, sem escrever flags inline.
- `GlobalCompositionRoot.Entry.InitializeLogging()` passou a delegar para `DebugUtility.ApplyLoggingPolicyFromBootstrap(...)`.
- A chave deterministica de policy ficou centralizada no `DebugUtility`: `buildVariant|newscriptsMode|verbosity|colors|source`.

## Writers antes vs depois
| Contexto | Antes | Depois |
|---|---|---|
| Runtime early | `DebugUtility.Initialize` resetava flags inline | `DebugUtility.Initialize` chama `ApplyLoggingPolicyInternal(..., source="EarlyDefault")` |
| Runtime bootstrap | `GlobalCompositionRoot.Entry.InitializeLogging` fazia `SetDefaultDebugLevel(DebugLevel.Verbose)` | `GlobalCompositionRoot.Entry.InitializeLogging` so delega para `DebugUtility.ApplyLoggingPolicyFromBootstrap(...)` |
| Dev-only | `DebugManagerConfig.ApplyConfiguration()` escreve direto em `DebugUtility` | inalterado por restricao de escopo desta etapa |

## Prova pos-change
### Writer runtime consolidado
Comando:
```text
rg -n "ApplyLoggingPolicyFromBootstrap|ApplyLoggingPolicyInternal|SetDefaultDebugLevel|SetVerboseLogging|SetLogFallbacks|SetRepeatedCallVerbose|InitializeLogging|logging configured|DebugManagerConfig" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Resultado curto:
- `Core/Logging/DebugUtility.cs`: `ApplyLoggingPolicyInternal(...)` + `ApplyLoggingPolicyFromBootstrap(...)`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`: chama `DebugUtility.ApplyLoggingPolicyFromBootstrap(...)`
- `Dev/Core/Logging/DebugManagerConfig.cs`: writer dev-only preservado fora do escopo
Resumo:
- no runtime, o apply real ficou concentrado no `DebugUtility`
- fora do runtime, `DebugManagerConfig` continua como writer dev-only por restricao explicita da tarefa

### Anchors novos de idempotencia
Comando:
```text
rg -n "LoggingPolicyApplied|LoggingPolicyApply dedupe_same_frame|LoggingPolicyApply dedupe_same_key" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Resultado:
- `Core/Logging/DebugUtility.cs` contem os tres anchors novos

### Entrypoints preservados
Comando:
```text
rg -n "RuntimeInitializeOnLoadMethod|InitializeOnLoadMethod" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Resultado curto:
- `DebugUtility` continua no `SubsystemRegistration`
- `GlobalCompositionRoot.Entry` continua no `BeforeSceneLoad`
- nenhum entrypoint novo foi introduzido

## Checklist manual de validacao (nao executado aqui)
- Rodar Smoke A-E do Baseline 3.1.
- Confirmar no log `1x` de `[OBS][RuntimeMode] LoggingPolicyApplied source='EarlyDefault' ...`.
- Confirmar no log no maximo `1x` de `[OBS][RuntimeMode] LoggingPolicyApplied source='BootstrapPolicy' ...`.
- Confirmar ausencia de repeticao desses anchors em transicoes normais.
- Confirmar que nenhuma funcionalidade baseline mudou alem da centralizacao/idempotencia.

## Mudancas de comportamento intencional
- nenhuma mudanca funcional pretendida; apenas centralizacao do writer runtime e guard idempotente

## Arquivos tocados
- `Core/Logging/DebugUtility.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
- `Docs/Modules/RuntimeMode-Logging.md`
- `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Logging-Cleanup-Audit-v2.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`
