# CORE-1.2b - DebugManagerConfig isolation to Dev

Date: 2026-03-07
Scope:
- `Core/Logging/DebugManagerConfig.cs`
- companion partial em `Dev/Core/Logging/DebugManagerConfig.DevQA.cs`

## Objetivo
- remover `DebugManagerConfig` do rail `Core/Logging`
- consolidar o artefato no trilho explicito `Dev/Core/Logging`
- preservar namespace, tipo publico e comportamento em `UNITY_EDITOR || DEVELOPMENT_BUILD`

## Evidencia local
Comandos resumidos:
```text
rg -n -w "DebugManagerConfig" . -g "*.cs"
rg -n -w "DebugLogSettings" . -g "*.cs"
rg -n "DebugManagerConfig|DebugLogSettings" -g "*.unity" -g "*.prefab" -g "*.asset" .
```

Resumo:
- `DebugManagerConfig` tinha hits apenas no proprio arquivo em `Core/Logging` e no partial `Dev/Core/Logging/DebugManagerConfig.DevQA.cs`
- `DebugLogSettings` segue referenciado apenas pelo proprio tipo e por `DebugManagerConfig`
- assets: unico hit e `Core/Logging/DebugLogSettings.asset`; sem refs de `DebugManagerConfig` em scene/prefab/asset

## Move executado
Arquivos movidos:
- `Core/Logging/DebugManagerConfig.cs` -> `Dev/Core/Logging/DebugManagerConfig.cs`
- `Core/Logging/DebugManagerConfig.cs.meta` -> `Dev/Core/Logging/DebugManagerConfig.cs.meta`

## Confirmacao
- behavior-preserving
- namespace intacto: `_ImmersiveGames.NewScripts.Core.Logging`
- tipo publico intacto: `DebugManagerConfig`
- partial `DebugManagerConfig.DevQA.cs` permaneceu no mesmo trilho `Dev/Core/Logging`
- `DebugLogSettings.cs` nao foi alterado nesta etapa
