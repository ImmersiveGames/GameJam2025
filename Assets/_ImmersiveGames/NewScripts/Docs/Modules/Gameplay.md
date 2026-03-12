# Gameplay (Live)

Data de refer�ncia: 2026-03-07  
Fonte da verdade: workspace local.

## Ownership real
- `Runtime/Actions/States/StateDependentService`: owner de integra��o de estado dependente de gameplay com gates/reset (consumer auxiliar de eventos globais).
- `Runtime/Spawning/*` + `Runtime/Spawning/Definitions/WorldDefinition`: owner do spawn runtime por defini��o de mundo.
- `Runtime/ActorGroupRearm/Core/ActorGroupRearmOrchestrator` + `Runtime/ActorGroupRearm/Interop/*`: owner do reset de gameplay (ActorGroupRearm) no escopo de cena.
- `Runtime/View/CameraResolverService`: owner de resolu��o de c�mera gameplay para consumers globais.

## Trilho can�nico (runtime)
1. Composition de cena registra servi�os gameplay em `SceneScopeCompositionRoot` (spawn + ActorGroupRearm).
2. Composition global registra integra��es de gameplay (`StateDependentService`, `ICameraResolver`) sem alterar pipeline/callsites can�nicos.
3. `WorldLifecycle` aciona participantes de `IActorGroupRearmWorldParticipant` e delega para `ActorGroupRearmOrchestrator`.

## Trilho DevQA / Editor
- Tooling historico de QA para este subsistema permanece isolado em `Modules/Gameplay/Legacy/Editor/**`.
- Esses scripts permanecem `EditorOnly` (`#if UNITY_EDITOR`) e sem callsite can�nico no pipeline.
- Release n�o incorpora esse tooling; comportamento runtime de produ��o permanece inalterado.

## Boundary Runtime vs DevQA
- Runtime: `Modules/Gameplay/Runtime/**` e bindings de infraestrutura usados por spawn/atores.
- EditorOnly: `Modules/Gameplay/Legacy/Editor/**` (sem wiring can�nico).
- N�o h� bootstrap autom�tico de Gameplay via `RuntimeInitializeOnLoadMethod` dentro de `Modules/Gameplay/**`.
## BATCH-CLEANUP-STD-2
- Removed in `BATCH-CLEANUP-STD-2`:
  - tooling legacy/editor de dev-driver de reset de gameplay em `Modules/Gameplay/Legacy/Editor/**`
  - probes legacy/editor de actor kind em `Modules/Gameplay/Legacy/Editor/**`
  - step loggers legacy/editor em `Modules/Gameplay/Legacy/Editor/**`
- Rationale: tooling legacy/editor sem callsite em `.cs` fora dos proprios arquivos e sem referencia por GUID em assets.

