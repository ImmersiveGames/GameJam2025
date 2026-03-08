# CORE-1.2f - Preconditions classification

Date: 2026-03-07
Decision: C reserve

## Objetivo
- classificar `Core/Validation/Preconditions.cs` com base apenas no workspace local
- evitar move sem evidencia objetiva

## Evidencia
### 1) Callsites
Comando:
```text
rg -n -w "Preconditions|Require\(|Ensure\(|Check\(" . -g "*.cs"
```

Resultado curto:
- `Core/Validation/Preconditions.cs:7: public static class Preconditions`
- `Core/Fsm/StateMachine.cs:50: Preconditions.CheckNotNull(state, "Estado não pode ser nulo.");`
- fora disso, apareceu `Ensure();` em `Modules/GameLoop/Bindings/Bootstrap/GameLoopBootstrap.cs`, sem relacao com `Preconditions`

Leitura:
- unico uso real de `Preconditions` e interno ao `Core`
- nenhum callsite real em `Modules/**` ou `Infrastructure/**`

### 2) Leak Editor
Comando:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod|RuntimeInitializeOnLoadMethod" Core/Validation/Preconditions.cs
```

Resultado:
- 0 hits

### 3) GUID scan
- script GUID (`Preconditions.cs.meta`): `5dc26d807148a5640bc1e3cdff036bc5`

Comando:
```text
rg -n "5dc26d807148a5640bc1e3cdff036bc5" . -g "*.unity" -g "*.prefab" -g "*.asset"
```

Resultado:
- 0 hits

## Decisao final
- classificacao: `C reserve`
- rationale:
  - sem callsites em `Modules/**`/`Infrastructure/**`
  - sem leak Editor e sem refs em assets
  - ainda existe uso interno no `Core`, portanto nao e dead

## Mudancas de codigo
- nenhuma
