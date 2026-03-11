# CORE-1.2g - SceneServiceCleaner classification

Date: 2026-03-07
Decision: C reserve

## Objetivo
- classificar `Core/Composition/SceneServiceCleaner.cs` com base apenas no workspace local
- evitar move sem evidencia objetiva

## Evidencia
### 1) Callsites
Comando:
```text
rg -n -w "SceneServiceCleaner|new\s+SceneServiceCleaner|Create\(|Initialize\(|Cleanup\(|Dispose\(" . -g "*.cs"
```

Resultado curto:
- `Core/Composition/SceneServiceRegistry.cs:14: private readonly SceneServiceCleaner _cleaner;`
- `Core/Composition/SceneServiceRegistry.cs:20: _cleaner = new SceneServiceCleaner(this);`
- `Core/Composition/SceneServiceRegistry.cs:222: _cleaner?.Dispose();`
- nenhum callsite real em `Infrastructure/**` ou `Modules/**`

Leitura:
- uso real existe, mas somente dentro do `Core`
- `SceneServiceCleaner` atua como detalhe de implementação do `SceneServiceRegistry`

### 2) Leak Editor
Comando:
```text
rg -n "UnityEditor|EditorApplication|AssetDatabase|FindAssets|MenuItem|ContextMenu|InitializeOnLoadMethod|RuntimeInitializeOnLoadMethod" Core/Composition/SceneServiceCleaner.cs
```

Resultado:
- 0 hits

### 3) GUID scan
- script GUID (`SceneServiceCleaner.cs.meta`): `cb566b8cc178ea64ab5871eaef4b1de6`

Comando:
```text
rg -n "cb566b8cc178ea64ab5871eaef4b1de6" . -g "*.unity" -g "*.prefab" -g "*.asset"
```

Resultado:
- 0 hits

### 4) Logs ancora
Comando:
```text
rg -n "\[SceneServiceCleaner\]|\[OBS\].*SceneServiceCleaner" . -g "*.cs"
```

Resultado:
- 0 hits com ancora literal; os logs existentes usam `typeof(SceneServiceCleaner)` em `DebugUtility.LogVerbose`

## Decisao final
- classificacao: `C reserve`
- rationale:
  - sem callsites em `Infrastructure/**`/`Modules/**`
  - sem leak Editor e sem refs em assets
  - ainda existe uso interno no `Core`, portanto nao e dead

## Mudancas de codigo
- nenhuma
