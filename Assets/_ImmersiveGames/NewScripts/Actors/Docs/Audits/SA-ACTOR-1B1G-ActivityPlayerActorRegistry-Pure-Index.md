# SA-ACTOR-1B1G — ActivityPlayerActorRegistry Pure Index

## Resumo objetivo

Este corte transforma `ActivityPlayerActorRegistry` em índice técnico puro.

O registry não destrói mais `GameObject` de player actor retido. O release físico passou a acontecer nos owners de orchestration já existentes:

- `SessionActivityPipeline` no reset de início de pipeline;
- `ActivityHandoffRuntimeResetStage` no reset de handoff.

## Arquivos alterados

- [ActivityPlayerActorRegistry.cs](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Actors/Players/ActivitySetup/ActivityPlayerActorRegistry.cs:211)
- [SessionActivityPipeline.cs](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs:2187)
- [ActivityHandoffRuntimeResetStage.cs](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/SessionActivity/Pipeline/Stages/ActivityHandoffRuntimeResetStage.cs:62)
- [SA-ACTOR-1B1G-ActivityPlayerActorRegistry-Pure-Index.md](/C:/Projetos/GameJam2025/Assets/_ImmersiveGames/NewScripts/Actors/Docs/Audits/SA-ACTOR-1B1G-ActivityPlayerActorRegistry-Pure-Index.md:1)

## Métodos removidos ou renomeados

- Renomeado: `ClearAllRouteRetained()` -> `ClearAllRouteScopedIndexes()`
- Adicionado: `GetIndexedRouteScopedHandles()`

`GetIndexedRouteScopedHandles()` existe apenas para permitir que o owner canônico de orchestration libere fisicamente as instâncias antes de limpar o índice.

## Onde o release físico passou a acontecer

- `SessionActivityPipeline.ReleaseIndexedRouteScopedPlayerActors()`
- `ActivityHandoffRuntimeResetStage.ReleaseIndexedRouteScopedPlayerActors(...)`

Nesses pontos, o pipeline/stage:

1. consulta os handles route-scoped indexados;
2. executa `Object.Destroy(instance)`;
3. limpa o índice técnico no registry.

## O que o registry ainda faz como índice técnico

- registrar actor materializado;
- registrar reentrada de actor route-scoped;
- resolver handle por `SessionParticipantId`;
- resolver handle por `ActorInstanceRuntimeId`;
- listar identities route-scoped para consumo técnico;
- expor handles indexados route-scoped para orchestration;
- limpar os índices ativos/route-scoped sem side-effect físico.

## Riscos

- Se existir algum call site fora do escopo atual esperando o nome antigo `ClearAllRouteRetained()`, a compilação apontará.
- O registry ainda é lookup técnico dos handles route-scoped; este corte remove ownership de release, não o papel de índice.

## Smoke recomendado

Não executei build, tests, playmode, batchmode ou smoke neste corte.

Quando validar manualmente:

- `RestartCurrentActivity` continua sem destruir player actor route-scoped reutilizável.
- `Activity01ToActivity02` continua reutilizando `RouteScoped` entre activities.
- `RouteExitBackToMenu` continua destruindo player actors route-scoped no reset/handoff canônico.
- `ActivityPlayerActorRegistry` continua resolvendo retained handles para `ActivityEntryParticipantBindingStage`.
- Não existe mais `Destroy` dentro de `ActivityPlayerActorRegistry`.
