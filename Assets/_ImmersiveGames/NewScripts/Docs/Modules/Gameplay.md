# Gameplay (Live)

Data de referência: 2026-03-07  
Fonte da verdade: workspace local.

## Ownership real
- `Runtime/Actions/States/StateDependentService`: owner de integração de estado dependente de gameplay com gates/reset (consumer auxiliar de eventos globais).
- `Runtime/Spawning/*` + `Runtime/Spawning/Definitions/WorldDefinition`: owner do spawn runtime por definição de mundo.
- `Runtime/RunRearm/Core/RunRearmOrchestrator` + `Runtime/RunRearm/Interop/*`: owner do reset de gameplay (RunRearm) no escopo de cena.
- `Runtime/View/CameraResolverService`: owner de resolução de câmera gameplay para consumers globais.

## Trilho canônico (runtime)
1. Composition de cena registra serviços gameplay em `SceneScopeCompositionRoot` (spawn + RunRearm).
2. Composition global registra integrações de gameplay (`StateDependentService`, `ICameraResolver`) sem alterar pipeline/callsites canônicos.
3. `WorldLifecycle` aciona participantes de `IRunRearmWorldParticipant` e delega para `RunRearmOrchestrator`.

## Trilho DevQA / Editor
- Tooling de RunRearm de QA foi isolado em `Modules/Gameplay/Legacy/Editor/RunRearm/**`.
- Esses scripts permanecem `EditorOnly` (`#if UNITY_EDITOR`) e sem callsite canônico no pipeline.
- Release não incorpora esse tooling; comportamento runtime de produção permanece inalterado.

## Boundary Runtime vs DevQA
- Runtime: `Modules/Gameplay/Runtime/**` e bindings de infraestrutura usados por spawn/atores.
- EditorOnly: `Modules/Gameplay/Legacy/Editor/**` (sem wiring canônico).
- Não há bootstrap automático de Gameplay via `RuntimeInitializeOnLoadMethod` dentro de `Modules/Gameplay/**`.
## BATCH-CLEANUP-STD-2
- Removed in `BATCH-CLEANUP-STD-2`:
  - `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmRequestDevDriver.cs`
  - `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmKindDevEaterActor.cs`
  - `Modules/Gameplay/Legacy/Editor/RunRearm/RunRearmDevStepLogger.cs`
- Rationale: tooling legacy/editor sem callsite em `.cs` fora dos proprios arquivos e sem referencia por GUID em assets.
