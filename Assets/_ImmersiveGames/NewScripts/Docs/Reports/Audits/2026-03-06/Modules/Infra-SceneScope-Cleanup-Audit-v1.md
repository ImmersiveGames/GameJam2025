# IC-1.4 - Infra SceneScope cleanup audit v1 (behavior-preserving)

Date: 2026-03-07
Source of truth: local workspace files.

## Scope
- `Infrastructure/Composition/SceneScopeCompositionRoot.cs`
- `Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs` (new)
- `Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs` (new)

## Inventory (before refactor)

| Método de registro | Serviço/Interface registrada | Dependências (TryGetGlobal / Require) | Tipo | Gate/Events | Callsite |
|---|---|---|---|---|---|
| `provider.RegisterForScene<ISceneScopeMarker>` | `ISceneScopeMarker` | `DependencyManager.Provider` | Runtime | none | `Awake()` |
| `provider.RegisterForScene<IWorldSpawnContext>` | `IWorldSpawnContext` | `EnsureWorldRoot(...)` | Runtime | none | `Awake()` |
| `provider.RegisterForScene<IActorRegistry>` | `IActorRegistry` | none | Runtime | none | `Awake()` |
| `provider.RegisterForScene<IWorldSpawnServiceRegistry>` | `IWorldSpawnServiceRegistry` | none | Runtime | none | `Awake()` |
| `provider.RegisterForScene<IRunRearmTargetClassifier>` | `IRunRearmTargetClassifier` | `TryGetGlobal<IRuntimeModeProvider>`, `TryGetGlobal<IDegradedModeReporter>` | Runtime | reset flow (no event publish here) | `Awake()` |
| `provider.RegisterForScene<IRunRearmOrchestrator>` | `IRunRearmOrchestrator` | none | Runtime | reset flow (no event publish here) | `Awake()` |
| `provider.RegisterForScene<WorldLifecycleHookRegistry>` | `WorldLifecycleHookRegistry` | none | Runtime | WorldLifecycle hook registry | `Awake()` |
| `provider.RegisterForScene<IRunRearmWorldParticipant>` | `IRunRearmWorldParticipant` | none | Runtime | WorldLifecycle soft reset -> gameplay reset | `Awake()` |
| `RegisterHookIfMissing(...)` (+ `EnsureHookComponent<WorldLifecycleHookLoggerA>`) | `IWorldLifecycleHook` dev logger | none | DevQA | hook lifecycle only | `RegisterSceneLifecycleHooks(...)` |
| `registry.Register(service)` | spawn service instances | `WorldDefinition`, `WorldSpawnServiceFactory` | Runtime | none | `RegisterSpawnServicesFromDefinition(...)` |

## Refactor applied (structural only)

### Before
- Monolith: `SceneScopeCompositionRoot.cs` contained runtime + RunRearm + DevQA hook install in one file.

### After
- Split by domain with partial class:
  - `SceneScopeCompositionRoot.cs` (wrapper/core runtime)
  - `SceneScopeCompositionRoot.RunRearm.cs` (RunRearm registration block)
  - `SceneScopeCompositionRoot.DevQA.cs` (Dev-only hook/logger install)
- DevQA file wrapped entirely with:
  - `#if UNITY_EDITOR || DEVELOPMENT_BUILD`

### Methods moved
- To `SceneScopeCompositionRoot.RunRearm.cs`:
  - `RegisterRunRearmServices(...)`
- To `SceneScopeCompositionRoot.DevQA.cs`:
  - `RegisterSceneLifecycleHooksDevQa(...)`
  - `EnsureHookComponent<T>(...)`
  - `RegisterHookIfMissing(...)`
- Wrapper kept in main file:
  - `RegisterSceneLifecycleHooks(...)` calls `RegisterSceneLifecycleHooksDevQa(...)` only under `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

## Mandatory evidence (pre-change commands)

### 1) class/register/install scan
```text
rg -n "class\s+SceneScopeCompositionRoot|Register|Install|EnsureInstalled|RuntimeInitializeOnLoadMethod" Infrastructure/Composition -g "*.cs"
Infrastructure/Composition/GlobalCompositionRoot.Entry.cs:61:[RuntimeInitializeOnLoadMethod(...)]
Infrastructure/Composition/SceneScopeCompositionRoot.cs:17:public sealed partial class SceneScopeCompositionRoot : MonoBehaviour
Infrastructure/Composition/SceneScopeCompositionRoot.cs:91:RegisterRunRearmServices(provider, hookRegistry, worldRoot);
Infrastructure/Composition/SceneScopeCompositionRoot.cs:254:private void RegisterSceneLifecycleHooks(...)
Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs:13:private void RegisterRunRearmServices(...)
Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs:11:private void RegisterSceneLifecycleHooksDevQa(...)
```

### 2) RunRearm/Dev/guards scan
```text
rg -n "RunRearm|IRunRearm|WorldLifecycleHookLogger|Hotkey|Dev|UNITY_EDITOR|DEVELOPMENT_BUILD" Infrastructure/Composition Modules -g "*.cs"
Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs:13:private void RegisterRunRearmServices(...)
Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs:22:...TryGetForScene<IRunRearmTargetClassifier>...
Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs:35:...TryGetForScene<IRunRearmOrchestrator>...
Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs:46:RegisterForScene<IRunRearmWorldParticipant>(...)
Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs:1:#if UNITY_EDITOR || DEVELOPMENT_BUILD
Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs:32:EnsureHookComponent<WorldLifecycleHookLoggerA>(worldRoot)
Infrastructure/Composition/SceneScopeCompositionRoot.cs:258:#if UNITY_EDITOR || DEVELOPMENT_BUILD
```

### 3) SceneScope references scan
```text
rg -n "SceneScopeCompositionRoot" Modules Infrastructure -g "*.cs"
Infrastructure/Composition/SceneScopeCompositionRoot.cs:17:public sealed partial class SceneScopeCompositionRoot : MonoBehaviour
Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs:11:public sealed partial class SceneScopeCompositionRoot
Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs:9:public sealed partial class SceneScopeCompositionRoot
```

### 4) asset scan safety
```text
rg -n "SceneScopeCompositionRoot|RunRearm|WorldLifecycleHookLoggerA" -g "*.unity" -g "*.prefab" -g "*.asset" .
(no matches)
```

## Mandatory post-refactor checks

```text
rg -n "partial\s+class\s+SceneScopeCompositionRoot" Infrastructure/Composition -g "*.cs"
Infrastructure/Composition/SceneScopeCompositionRoot.cs:17:public sealed partial class SceneScopeCompositionRoot : MonoBehaviour
Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs:11:public sealed partial class SceneScopeCompositionRoot
Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs:9:public sealed partial class SceneScopeCompositionRoot
```

```text
rg -n "SceneScopeCompositionRoot\." Infrastructure/Composition -g "*.cs"
0 matches
```

```text
rg -n "WorldLifecycleHookLoggerA|RunRearm" Infrastructure/Composition -g "*.cs"
Infrastructure/Composition/SceneScopeCompositionRoot.cs:91:RegisterRunRearmServices(provider, hookRegistry, worldRoot);
Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs:13:private void RegisterRunRearmServices(...)
Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs:32:EnsureHookComponent<WorldLifecycleHookLoggerA>(worldRoot)
```

```text
rg -n "WorldLifecycleHookLoggerA|RunRearm" -g "*.unity" -g "*.prefab" -g "*.asset" .
0 matches
```

Duplicate-method safety check:
```text
rg -n "private\s+(static\s+)?[A-Za-z0-9_<>,\s]+\s+(RegisterRunRearmServices|RegisterSceneLifecycleHooks|RegisterSceneLifecycleHooksDevQa|EnsureHookComponent|RegisterHookIfMissing)\s*\(" Infrastructure/Composition -g "SceneScopeCompositionRoot*.cs"
Infrastructure/Composition/SceneScopeCompositionRoot.RunRearm.cs:13:private void RegisterRunRearmServices(...)
Infrastructure/Composition/SceneScopeCompositionRoot.cs:254:private void RegisterSceneLifecycleHooks(...)
Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs:11:private void RegisterSceneLifecycleHooksDevQa(...)
Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs:36:private static T EnsureHookComponent<T>(...)
Infrastructure/Composition/SceneScopeCompositionRoot.DevQA.cs:48:private static void RegisterHookIfMissing(...)
```

## Behavior-preserving confirmation
- No public contract changed.
- No pipeline order changed.
- No `GlobalCompositionRoot` callsite changed.
- Runtime boot order inside SceneScope preserved (same sequence of registrations; only extracted methods).
- Unity execution not run; validation is static (`rg` + file inspection).
