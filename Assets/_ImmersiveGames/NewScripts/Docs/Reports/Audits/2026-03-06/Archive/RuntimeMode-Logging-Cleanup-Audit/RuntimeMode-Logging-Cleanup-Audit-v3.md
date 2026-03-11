# RM-1.3 - Hardening + evidencia de idempotencia de LoggingPolicy

Date: 2026-03-08
Status: behavior-preserving

## Objetivo
- endurecer a observabilidade do dedupe de `LoggingPolicy`
- adicionar um trigger dev-only para gerar evidencia sem depender do smoke natural
- preservar boot order e comportamento de Release

## Implementacao
- `DebugUtility` agora loga dedupe com anchors explicitos `LoggingPolicyApplySkipped reason='dedupe_same_frame'` e `reason='dedupe_same_key'`.
- `DebugUtility.Dev_ForceReapplyLastLoggingPolicyForEvidence()` foi adicionado sob `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
- `Editor/Core/Logging/DebugUtility.LoggingPolicyEvidence.Editor.cs` adiciona um `MenuItem` editor-only para disparar a evidencia no Play Mode.
- `GlobalCompositionRoot.Entry.cs` permaneceu intocado nesta etapa.

## Invariantes RM-1.3
- `EarlyDefault` e `BootstrapPolicy` continuam sendo duas fases legitimas do boot; como usam `source` diferente, suas keys tambem diferem e nao devem ser dedupadas.
- Dedupe so ocorre quando a mesma `policyKey` reaparece.
- Release nao ganha tooling novo; o trigger compila apenas em `UNITY_EDITOR || DEVELOPMENT_BUILD`, e o menu fica isolado em `#if UNITY_EDITOR`.

## Evidencia estatica obrigatoria
### 1) Provar writer unico
Comando:
```text
rg -n "LoggingPolicyApplied" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Resultado:
```text
.\Core\Logging\DebugUtility.cs:367:            LogRuntimeModeObs($"[OBS][RuntimeMode] LoggingPolicyApplied source='{source}' key='{policyKey}'");
```

### 2) Provar unico callsite externo
Comando:
```text
rg -n "ApplyLoggingPolicyFromBootstrap" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Resultado:
```text
.\Core\Logging\DebugUtility.cs:120:        public static void ApplyLoggingPolicyFromBootstrap(
.\Infrastructure\Composition\GlobalCompositionRoot.Entry.cs:101:            DebugUtility.ApplyLoggingPolicyFromBootstrap(
```

### 3) Provar trigger DEV-only + dedupe logs
Comando:
```text
rg -n "Dev_ForceReapplyLastLoggingPolicyForEvidence|LoggingPolicyApplySkipped" Assets/_ImmersiveGames/NewScripts -g "*.cs"
```
Resultado:
```text
.\Editor\Core\Logging\DebugUtility.LoggingPolicyEvidence.Editor.cs:11:            DebugUtility.Dev_ForceReapplyLastLoggingPolicyForEvidence();
.\Core\Logging\DebugUtility.cs:136:        public static async void Dev_ForceReapplyLastLoggingPolicyForEvidence()
.\Core\Logging\DebugUtility.cs:341:                LogRuntimeModeObs($"[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_frame' key='{policyKey}'");
.\Core\Logging\DebugUtility.cs:347:                LogRuntimeModeObs($"[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_key' key='{policyKey}'");
```

### 4) Leak sweep
Comando:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod" Assets/_ImmersiveGames/NewScripts -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
```
Resultado observado no workspace local:
```text
.\Infrastructure\Composition\GlobalCompositionRoot.Entry.cs:61:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
.\Core\Logging\DebugUtility.cs:62:        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
```
Leitura:
- nao houve leak de `UnityEditor`, `EditorApplication`, `AssetDatabase`, `FindAssets`, `MenuItem`, `ContextMenu` ou `InitializeOnLoadMethod` fora de `Dev/**`/`Editor/**`
- os dois hits acima sao allowlist conhecida de `RuntimeInitializeOnLoadMethod`, por sobreposicao textual com o regex

## Como gerar evidencia em Editor/DevBuild
### Editor
1. Entrar em Play Mode com `NEWSCRIPTS_MODE` ativo.
2. Aguardar os dois anchors normais de boot:
   - `[OBS][RuntimeMode] LoggingPolicyApplied source='EarlyDefault' ...`
   - `[OBS][RuntimeMode] LoggingPolicyApplied source='BootstrapPolicy' ...`
3. Acionar `ImmersiveGames/NewScripts/Dev/Force LoggingPolicy Reapply Evidence` no menu.
4. Confirmar os novos anchors:
   - `[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_frame' ...`
   - `[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_key' ...`

### DevBuild
1. Aguardar o boot normal.
2. Disparar `DebugUtility.Dev_ForceReapplyLastLoggingPolicyForEvidence()` a partir do harness Dev/QA existente.
3. Confirmar os mesmos dois anchors de skip no log.

## Checklist runtime esperado
- `1x` `LoggingPolicyApplied source='EarlyDefault'`
- `1x` `LoggingPolicyApplied source='BootstrapPolicy'`
- `1x` `LoggingPolicyApplySkipped reason='dedupe_same_frame'` apos o trigger manual
- `1x` `LoggingPolicyApplySkipped reason='dedupe_same_key'` apos o trigger manual
- nenhuma repeticao espontanea desses anchors em transicoes normais

## Arquivos tocados
- `Core/Logging/DebugUtility.cs`
- `Editor/Core/Logging/DebugUtility.LoggingPolicyEvidence.Editor.cs`
- `Editor/Core/Logging/DebugUtility.LoggingPolicyEvidence.Editor.cs.meta`
- `Docs/Modules/RuntimeMode-Logging.md`
- `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Logging-Cleanup-Audit-v4.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`

