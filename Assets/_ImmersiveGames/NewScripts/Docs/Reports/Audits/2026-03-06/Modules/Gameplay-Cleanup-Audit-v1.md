# GP-1.1 - Gameplay cleanup audit v1 (behavior-preserving)

Date: 2026-03-07  
Source of truth: workspace local files.

## Escopo e regras
- Escopo de código: `Modules/Gameplay/**`.
- Escopo de evidência de callsite: `Infrastructure/Composition/**` + demais `Modules/**`.
- Sem mudança de contratos públicos, payloads de eventos, ordem/callsites do pipeline global.
- Mudanças desta etapa: isolamento estrutural e moves seguros com prova estática.

## Inventário (A/B/C)

| Component | FilePath | Category (A Runtime / B DevQA / C Legacy/Dead) | EvidenceCallsite | EvidenceAssetRef | Notes/Risks | Recommendation |
|---|---|---|---|---|---|---|
| StateDependentService | `Modules/Gameplay/Runtime/Actions/States/StateDependentService.cs` | A Runtime | `Infrastructure/Composition/GlobalCompositionRoot.StateDependentCamera.cs` registra `IStateDependentService` | n/a | Serviço runtime canônico | KEEP |
| CameraResolverService | `Modules/Gameplay/Runtime/View/CameraResolverService.cs` | A Runtime | `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs` registra `ICameraResolver` | n/a | Serviço runtime canônico | KEEP |
| RunRearmOrchestrator | `Modules/Gameplay/Runtime/RunRearm/Core/RunRearmOrchestrator.cs` | A Runtime | `Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs` registra `IRunRearmOrchestrator` | n/a | Owner de reset gameplay por cena | KEEP |
| PlayersRunRearmWorldParticipant | `Modules/Gameplay/Runtime/RunRearm/Interop/PlayersRunRearmWorldParticipant.cs` | A Runtime | `Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs` registra `IRunRearmWorldParticipant` | n/a | Bridge runtime para WorldLifecycle | KEEP |
| RunRearmRequestDevDriver | `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmRequestDevDriver.cs` | C Legacy/Dead | Sem callsite canônico (`rg` só no próprio arquivo) | Nome + GUID sem matches em `.unity/.prefab/.asset` | Tooling QA órfão | MOVE TO LEGACY |
| RunRearmKindDevEaterActor | `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmKindDevEaterActor.cs` | C Legacy/Dead | Sem callsite canônico (`rg` só no próprio arquivo) | Nome + GUID sem matches em `.unity/.prefab/.asset` | Tooling QA órfão | MOVE TO LEGACY |
| RunRearmDevStepLogger | `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmDevStepLogger.cs` | C Legacy/Dead | Sem callsite canônico (`rg` só no próprio arquivo) | Nome + GUID sem matches em `.unity/.prefab/.asset` | Tooling QA órfão | MOVE TO LEGACY |

## Mudanças aplicadas (CODE)
- Move seguro (com `.meta` junto):
  - `Modules/Gameplay/Editor/RunRearm/RunRearmRequestDevDriver.cs` -> `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmRequestDevDriver.cs`
  - `Modules/Gameplay/Editor/RunRearm/RunRearmKindDevEaterActor.cs` -> `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmKindDevEaterActor.cs`
  - `Modules/Gameplay/Editor/RunRearm/RunRearmDevStepLogger.cs` -> `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmDevStepLogger.cs`
- Os três arquivos permaneceram `EditorOnly` (`#if UNITY_EDITOR`) como estavam.

## Evidências rg (inventário obrigatório)

### 1) Gameplay namespace/class map
```text
rg -n "namespace\s+_ImmersiveGames\.NewScripts\.Modules\.Gameplay|class\s+" Modules/Gameplay -g "*.cs"
... (mapeamento completo de Runtime/Infrastructure/Editor)
```

### 2) Dev/Debug/bootstrap scan
```text
rg -n "Dev|Debug|Cheat|Hotkey|MenuItem|ContextMenu|RuntimeInitializeOnLoadMethod|InitializeOnLoad|ExecuteAlways" Modules/Gameplay -g "*.cs"
Matches relevantes: apenas tooling `Editor/RunRearm/*` com ContextMenu; sem RuntimeInitializeOnLoadMethod/InitializeOnLoad/ExecuteAlways em Gameplay.
```

### 3) Guards scan
```text
rg -n "#if\s+UNITY_EDITOR|#if\s+DEVELOPMENT_BUILD|UNITY_EDITOR\s*\|\|\s*DEVELOPMENT_BUILD" Modules/Gameplay -g "*.cs"
Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmRequestDevDriver.cs:1:#if UNITY_EDITOR
Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmKindDevEaterActor.cs:1:#if UNITY_EDITOR
Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmDevStepLogger.cs:1:#if UNITY_EDITOR
Modules/Gameplay/Infrastructure/Actors/Bindings/Player/Movement/PlayerMovementController.cs:527:#if UNITY_EDITOR || DEVELOPMENT_BUILD
Modules/Gameplay/Infrastructure/Actors/Bindings/Player/Movement/PlayerInputReader.cs:79:#if UNITY_EDITOR || DEVELOPMENT_BUILD
```

### 4) Callsites cross-module
```text
rg -n "Modules\.Gameplay" Infrastructure/Composition Modules -g "*.cs"
Matches relevantes em composition:
- Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs
- Infrastructure/Composition/SceneScopeCompositionRoot.cs
- Infrastructure/Composition/GlobalCompositionRoot.StateDependentCamera.cs
- Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs
```

### 5) Asset scan geral
```text
rg -n "Gameplay" -g "*.unity" -g "*.prefab" -g "*.asset" .
./Modules/SceneFlow/Navigation/Bindings/SceneKeys/GameplayScene.asset:13:  m_Name: GameplayScene
./Modules/SceneFlow/Navigation/Bindings/SceneKeys/GameplayScene.asset:15:  sceneName: GameplayScene
```

## GUID scans (itens movidos) - prova de dead/legacy

1) `RunRearmRequestDevDriver.cs.meta`  
GUID: `4b5cea7fdfc2456b9286a8c1679b2844`
```text
rg -n "4b5cea7fdfc2456b9286a8c1679b2844" -g "*.unity" -g "*.prefab" -g "*.asset" .
(no matches)
```

2) `RunRearmKindDevEaterActor.cs.meta`  
GUID: `8a4fec27d65745d0b88f72380bf45587`
```text
rg -n "8a4fec27d65745d0b88f72380bf45587" -g "*.unity" -g "*.prefab" -g "*.asset" .
(no matches)
```

3) `RunRearmDevStepLogger.cs.meta`  
GUID: `898f8b99e1cfa5c4ebe93238753b7ea0`
```text
rg -n "898f8b99e1cfa5c4ebe93238753b7ea0" -g "*.unity" -g "*.prefab" -g "*.asset" .
(no matches)
```

## Checks pós-change (obrigatórios)

```text
rg -n "Modules/Gameplay/Bindings|Modules/Gameplay/Legacy|Modules/Gameplay/Editor" Modules/Gameplay -g "*.cs"
(no matches)
```

```text
rg -n "RuntimeInitializeOnLoadMethod|InitializeOnLoad|ExecuteAlways" Modules/Gameplay -g "*.cs"
(no matches)
```

```text
rg -n "Gameplay.*Legacy" Infrastructure/Composition Modules -g "*.cs"
(no matches)
```

```text
rg -n "Gameplay.*(RunRearmRequestDevDriver|RunRearmKindDevEaterActor|RunRearmDevStepLogger)" -g "*.unity" -g "*.prefab" -g "*.asset" .
(no matches)
```

## Confirmação behavior-preserving
- Nenhum contrato público/payload de evento alterado.
- Nenhuma mudança de ordem/callsite do pipeline global.
- Nenhuma mudança de semântica runtime de gameplay/loops.
- Mudança limitada a isolamento estrutural e realocação de tooling órfão para `Legacy/Editor`.
