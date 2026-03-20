# ADR-0011 â€” WorldDefinition multi-actor para GameplayScene (NewScripts)

## Status

- Estado: Implementado
- Data (decisÃ£o): 2025-12-28
- Ãšltima atualizaÃ§Ã£o: 2026-02-04
- Tipo: ImplementaÃ§Ã£o
- Escopo: `GameplayScene`, `SceneScopeCompositionRoot`, spawn pipeline (Player/Eater), WorldLifecycle

## Contexto

O jogo precisa suportar uma cena de gameplay capaz de spawnar **mÃºltiplos atores** (ex.: Player + Eater) de forma **determinÃ­stica** e **auditÃ¡vel** (Baseline 2.x). A fonte de verdade para *o que* spawnar e *como* spawnar deve ser dados (assets), nÃ£o cÃ³digo hardcoded.

HÃ¡ tambÃ©m um requisito operacional: o projeto possui uma polÃ­tica **Strict/Release**. Em **Strict**, erros de contrato devem ser *detectÃ¡veis cedo* (preferencialmente falhando com logs/asserts), enquanto em **Release** o sistema deve ser mais tolerante quando isso nÃ£o compromete gameplay.

## DecisÃ£o

### Objetivo de produÃ§Ã£o (sistema ideal)

Introduzir um asset `WorldDefinition` que descreve o conjunto de **SpawnEntries** (multi-actor) e integrÃ¡-lo ao pipeline de bootstrap/spawn da `GameplayScene`.

### Contrato de produÃ§Ã£o (mÃ­nimo)

A presenÃ§a de `WorldDefinition` Ã© **obrigatÃ³ria em cenas classificadas como gameplay** (ex.: `GameplayScene`) e **opcional/permitida** em cenas de frontend (ex.: `MenuScene`).

### NÃ£o-objetivos (resumo)

- Criar um sistema de levels/catalog (ver ADR-0017).
- Alterar o pipeline de SceneFlow/Fade/LoadingHUD.

## Detalhamento

### Conceitos

- **WorldDefinition**: asset contendo uma lista ordenada de `SpawnEntries` (tipo/config + regras) que define quais atores existirÃ£o ao final do reset.
- **SpawnEntry**: definiÃ§Ã£o de um ator a ser instanciado/registrado (ex.: `Player`, `Eater`).
- **IWorldSpawnContext / ISpawnDefinitionService / ISpawnRegistry**: serviÃ§os de suporte ao spawn (registro, resoluÃ§Ã£o, tracking) e evidÃªncia via logs.

### Ponto de integraÃ§Ã£o

A integraÃ§Ã£o acontece no `SceneScopeCompositionRoot` (nome atual observado em evidÃªncias). Ele:

1. Registra serviÃ§os gerais de cena (ex.: `LoadingHudService`, `InputModeService`, etc.).
2. **Se** houver `WorldDefinition` atribuÃ­da, registra/instancia os serviÃ§os do spawn pipeline e expÃµe a definiÃ§Ã£o via `ISpawnDefinitionService`.
3. **Se nÃ£o** houver `WorldDefinition`, emite log explÃ­cito informando que a ausÃªncia Ã© permitida para cenas que nÃ£o fazem spawn.

> ObservaÃ§Ã£o: versÃµes anteriores do texto referiam â€œNewSceneBootstrapperâ€. Na evidÃªncia canÃ´nica atual, o componente observado Ã© `SceneScopeCompositionRoot`.

## PolÃ­tica Strict/Release

### Regra de contrato

- **Cenas de gameplay (ex.: GameplayScene)**
  - `WorldDefinition` **deve** existir.
  - `WorldDefinition.SpawnEntries` **deve** conter pelo menos 1 entrada vÃ¡lida.
  - Em **Strict**: violaÃ§Ãµes sÃ£o tratadas como *blocker* (log de erro/assert + falha detectÃ¡vel no smoke).
  - Em **Release**: violaÃ§Ãµes continuam sendo erro, mas a estratÃ©gia pode ser â€œfail-fast com fallback controladoâ€ apenas se nÃ£o corromper estado (preferir manter o contrato, nÃ£o mascarar).

- **Cenas de frontend/menus (ex.: MenuScene)**
  - `WorldDefinition` **nÃ£o Ã© exigida**.
  - Em **Strict**: a ausÃªncia Ã© permitida **desde que** a cena seja classificada como nÃ£o-gameplay (ver *classifier* abaixo).

### ClassificaÃ§Ã£o (classifier)

A decisÃ£o de exigir `WorldDefinition` deve ser guiada por um classificador (ex.: `IGameplayResetTargetClassifier`) e/ou pelo profile do `SceneFlow`.

EvidÃªncia canÃ´nica mostra:

- `profile=startup` e `profile=frontend`: reset pode ser SKIP.
- `profile=gameplay`: reset executa e o spawn pipeline roda.

## EvidÃªncia

- **Ãšltima evidÃªncia (log bruto):** `Docs/Reports/Evidence/LATEST.md`

### Log canÃ´nico (Baseline 2.2)

- **Arquivo:** `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`

#### MenuScene permite ausÃªncia de WorldDefinition

```
[SceneScopeCompositionRoot] Setup OK :: hasWorldDefinition=False :: scene='MenuScene'
[SceneScopeCompositionRoot] WorldDefinition is null (allowed in scenes that do not spawn actors). :: scene='MenuScene'
```

#### GameplayScene exige WorldDefinition e registra spawn services

```
[SceneScopeCompositionRoot] Setup OK :: hasWorldDefinition=True :: scene='GameplayScene'
[SceneScopeCompositionRoot] WorldDefinition loaded :: entries=2 :: scene='GameplayScene'
[SceneScopeCompositionRoot] Registered IWorldSpawnContext.
[SceneScopeCompositionRoot] Registered ISpawnDefinitionService.
[SceneScopeCompositionRoot] Registered ISpawnRegistry.
```

#### Reset executa e resulta em multi-actor

```
[WorldLifecycle] ResetWorld START :: profile=gameplay :: id=2 :: reason='SceneFlow/ScenesReady'
[WorldLifecycle] Spawns COMPLETE :: spawnCount=2 :: registryCount=2 :: actors=[Player,Eater]
[WorldLifecycle] ResetWorld COMPLETE :: id=2
```

### Auditoria Strict/Release

- **Arquivo:** `Docs/CHANGELOG.md (entrada histÃ³rica de 2026-01-31)`

## Fora de escopo

- IntegraÃ§Ã£o com LevelManager/ContentSwap (ver ADR-0016/0017).
- Alterar contratos de SceneFlow ou WorldLifecycle.

## ConsequÃªncias

### Positivas

- **Dados como fonte de verdade** para a composiÃ§Ã£o do mundo (multi-actor) em gameplay.
- Melhor **auditabilidade** (logs determinÃ­sticos e verificÃ¡veis) e integraÃ§Ã£o com Baseline.
- SeparaÃ§Ã£o clara entre **frontend** (sem spawn) e **gameplay** (com spawn obrigatÃ³rio).

### Trade-offs

- Requer disciplina de content pipeline: `WorldDefinition` precisa existir e ser mantida correta em cenas classificadas como gameplay.
- Strict pode aumentar fricÃ§Ã£o durante iteraÃ§Ã£o (o que Ã© intencional para evitar regressÃµes silenciosas).

## Alternativas consideradas

1. **Hardcode de spawns em cÃ³digo**
   - Rejeitada: baixa auditabilidade e alto risco de divergÃªncia com content.

2. **Um Ãºnico â€œWorldDefinition globalâ€ para todas as cenas**
   - Rejeitada: menus/frontend nÃ£o devem pagar custo nem sofrer contrato de gameplay.

## AÃ§Ãµes futuras

- Garantir que o **classifier** de â€œcena de gameplayâ€ seja a fonte canÃ´nica da exigÃªncia de `WorldDefinition`.
- Se necessÃ¡rio, adicionar uma checagem explÃ­cita no `SceneScopeCompositionRoot`:
  - `if (isGameplayScene && worldDefinition == null) -> error/assert`.

## ImplementaÃ§Ã£o (arquivos impactados)

- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/SceneScopeCompositionRoot.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/Spawning/Definitions/WorldDefinition.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Spawn/WorldSpawnServiceFactory.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Spawn/WorldSpawnServiceRegistry.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/Spawning/PlayerSpawnService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Gameplay/Runtime/Spawning/EaterSpawnService.cs`

