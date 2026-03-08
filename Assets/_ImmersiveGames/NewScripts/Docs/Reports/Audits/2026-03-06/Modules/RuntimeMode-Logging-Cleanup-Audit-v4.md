# RM-1.4 - PolicyKey Contract estavel/deterministico

Date: 2026-03-08
Status: behavior-preserving

## Objetivo
- endurecer o contrato da `policyKey` de logging para que a mesma entrada gere exatamente a mesma string
- centralizar a montagem da key em um unico builder canônico
- preservar a ordem do boot e os anchors de observabilidade existentes

## Invariantes
- `EarlyDefault` continua aplicando antes de `BootstrapPolicy`.
- `BootstrapPolicy` continua aplicando depois porque sua key legitima e diferente.
- `LoggingPolicyApplied` e `LoggingPolicyApplySkipped` permanecem com os mesmos anchors.
- `GlobalCompositionRoot.Entry.cs` nao foi alterado nesta etapa.

## Arquivos tocados
- `Core/Logging/DebugUtility.cs`
- `Docs/Modules/RuntimeMode-Logging.md`
- `Docs/Reports/Audits/2026-03-06/Modules/RuntimeMode-Logging-Cleanup-Audit-v4.md`
- `Docs/Reports/Audits/2026-03-06/Audit-Index.md`
- `Docs/Reports/Audits/2026-03-06/Module-Audit-Summary.md`

## Implementacao
- a construcao inline de `policyKey` foi substituida por `BuildLoggingPolicyKey(...)`
- o builder preserva o mesmo shape humano da string atual, mas concentra o contrato num unico ponto
- o comentario de contrato foi adicionado acima do builder para deixar explicito o papel da key em dedupe e evidencia
- nao ha colecoes participando da key hoje; se surgirem no futuro, a serializacao deve ordenar deterministicamente antes do join

## Evidencia estatica obrigatoria
### A) Unicidade do builder
Comando:
```text
rg -n "BuildLoggingPolicyKey\(" Core/Logging/DebugUtility.cs
```
Resultado:
```text
332:            string policyKey = BuildLoggingPolicyKey(
375:        private static string BuildLoggingPolicyKey(
```

### B) Onde `policyKey` e usado nos logs ancora
Comando:
```text
rg -n "LoggingPolicyApplied|LoggingPolicyApplySkipped|policyKey" Core/Logging/DebugUtility.cs
```
Resultado:
```text
333:            string policyKey = BuildLoggingPolicyKey(
343:            if (policyFrame == _lastPolicyFrame && string.Equals(policyKey, _lastPolicyKey, StringComparison.Ordinal))
345:                LogRuntimeModeObs($"[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_frame' key='{policyKey}'");
349:            if (string.Equals(policyKey, _lastPolicyKey, StringComparison.Ordinal))
351:                LogRuntimeModeObs($"[OBS][RuntimeMode] LoggingPolicyApplySkipped reason='dedupe_same_key' key='{policyKey}'");
361:            _lastPolicyKey = policyKey;
371:            LogRuntimeModeObs($"[OBS][RuntimeMode] LoggingPolicyApplied source='{source}' key='{policyKey}'");
374:        // policyKey e um contrato estavel/deterministico usado para dedupe e evidencia.
```

### C) Leak sweep de UnityEditor fora do escopo permitido
Comando:
```text
rg -n "using UnityEditor|UnityEditor\.|EditorApplication|AssetDatabase|FindAssets" . -g "*.cs" -g "!**/Dev/**" -g "!**/Editor/**" -g "!**/Legacy/**" -g "!**/QA/**"
```
Resultado:
```text
0 matches
```

### D) Entry continua apenas chamando o apply
Comando:
```text
rg -n "ApplyLoggingPolicyFromBootstrap\(" Infrastructure/Composition/GlobalCompositionRoot.Entry.cs
```
Resultado:
```text
101:            DebugUtility.ApplyLoggingPolicyFromBootstrap(
```

## Leitura final
- `policyKey` agora vem exclusivamente do builder canônico `BuildLoggingPolicyKey(...)`.
- nao houve mudanca de boot order nem de ownership do writer.
- o contrato da key ficou explicito e pronto para futuras extensoes sem perder determinismo.

