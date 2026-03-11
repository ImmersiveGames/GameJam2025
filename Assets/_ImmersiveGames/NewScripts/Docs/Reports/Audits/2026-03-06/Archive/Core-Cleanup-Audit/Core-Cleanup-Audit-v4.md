# CORE-1.2b - DebugLogSettings classification and move

Date: 2026-03-07
Decision: B dev-only

## Objetivo
- classificar `DebugLogSettings` com evidencia estatica local
- mover script e asset apenas se a evidencia mostrasse ausencia de consumo runtime
- preservar GUIDs e comportamento observado

## Evidencia estatica
### 1) Script
- `DebugLogSettings.cs` nao usa `UnityEditor`, `AssetDatabase`, `ContextMenu`, `MenuItem`, `EditorApplication` ou `FindAssets`
- e um `ScriptableObject` com `CreateAssetMenu`, sem loader embutido

### 2) Consumo em codigo
Comandos:
```text
rg -n -w "DebugLogSettings" . -g "*.cs"
rg -n "Resources\.Load|Addressables|LoadAssetAtPath|FindAssets|GetAssetPath" . -g "*.cs"
```

Resumo:
- `DebugLogSettings` tinha hits apenas no proprio tipo e em `Dev/Core/Logging/DebugManagerConfig.cs`
- nenhum `Resources.Load`, `Addressables`, `LoadAssetAtPath`, `FindAssets` ou `GetAssetPath` apontou para `DebugLogSettings`
- nenhum owner runtime (`DebugUtility`, bootstrap, config loader) carrega `DebugLogSettings`

### 3) GUID checks
- script GUID (`DebugLogSettings.cs.meta`): `8c6d4ad0c2044d4bb4b6c1a5c07f9d5d`
- asset GUID (`DebugLogSettings.asset.meta`): `4830ec2e6ecdfa1469f83d906049d4e8`

Comando:
```text
rg -n "8c6d4ad0c2044d4bb4b6c1a5c07f9d5d|4830ec2e6ecdfa1469f83d906049d4e8" -g "*.unity" -g "*.prefab" -g "*.asset" .
```

Resultado:
- unico hit foi o proprio `DebugLogSettings.asset` referenciando o GUID do script
- nenhum `.unity`, `.prefab` ou outro `.asset` referencia o script ou o asset

## Decisao
- classificacao final: `B dev-only`
- justificativa: consumo restrito a `DebugManagerConfig` em `Dev/Core/Logging`, sem loader runtime e sem refs de asset fora do proprio asset

## Move executado
Arquivos movidos:
- `Core/Logging/DebugLogSettings.cs` -> `Dev/Core/Logging/DebugLogSettings.cs`
- `Core/Logging/DebugLogSettings.cs.meta` -> `Dev/Core/Logging/DebugLogSettings.cs.meta`
- `Core/Logging/DebugLogSettings.asset` -> `Dev/Core/Logging/DebugLogSettings.asset`
- `Core/Logging/DebugLogSettings.asset.meta` -> `Dev/Core/Logging/DebugLogSettings.asset.meta`

## Confirmacao
- nenhum split/partial foi necessario; nao havia leak Editor no script
- GUIDs preservados por move com `.meta`
- `DebugLogSettings` removido do rail `Core/Logging` por falta de consumo runtime local
