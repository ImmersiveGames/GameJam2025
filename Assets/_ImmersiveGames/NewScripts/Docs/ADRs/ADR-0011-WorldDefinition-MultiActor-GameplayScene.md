# ADR-0011 — WorldDefinition multi-actor para GameplayScene (NewScripts)

## Status

- Estado: Implementado
- Data (decisão): 2025-12-28
- Última atualização: 2026-01-31
- Escopo: `GameplayScene`, `SceneBootstrapper`, spawn pipeline (Player/Eater), WorldLifecycle

## Contexto

O jogo precisa suportar uma cena de gameplay capaz de spawnar **múltiplos atores** (ex.: Player + Eater) de forma **determinística** e **auditável** (Baseline 2.x). A fonte de verdade para *o que* spawnar e *como* spawnar deve ser dados (assets), não código hardcoded.

Há também um requisito operacional: o projeto possui uma política **Strict/Release**. Em **Strict**, erros de contrato devem ser *detectáveis cedo* (preferencialmente falhando com logs/asserts), enquanto em **Release** o sistema deve ser mais tolerante quando isso não compromete gameplay.

## Decisão

Introduzir um asset `WorldDefinition` que descreve o conjunto de **SpawnEntries** (multi-actor) e integrá-lo ao pipeline de bootstrap/spawn da `GameplayScene`.

A presença de `WorldDefinition` é **obrigatória em cenas classificadas como gameplay** (ex.: `GameplayScene`) e **opcional/permitida** em cenas de frontend (ex.: `MenuScene`).

## Detalhamento

### Conceitos

- **WorldDefinition**: asset contendo uma lista ordenada de `SpawnEntries` (tipo/config + regras) que define quais atores existirão ao final do reset.
- **SpawnEntry**: definição de um ator a ser instanciado/registrado (ex.: `Player`, `Eater`).
- **IWorldSpawnContext / ISpawnDefinitionService / ISpawnRegistry**: serviços de suporte ao spawn (registro, resolução, tracking) e evidência via logs.

### Ponto de integração

A integração acontece no `SceneBootstrapper` (nome atual observado em evidências). Ele:

1. Registra serviços gerais de cena (ex.: `LoadingHudService`, `InputModeService`, etc.).
2. **Se** houver `WorldDefinition` atribuída, registra/instancia os serviços do spawn pipeline e expõe a definição via `ISpawnDefinitionService`.
3. **Se não** houver `WorldDefinition`, emite log explícito informando que a ausência é permitida para cenas que não fazem spawn.

> Observação: versões anteriores do texto referiam “NewSceneBootstrapper”. Na evidência canônica atual, o componente observado é `SceneBootstrapper`.

## Política Strict/Release

### Regra de contrato

- **Cenas de gameplay (ex.: GameplayScene)**
  - `WorldDefinition` **deve** existir.
  - `WorldDefinition.SpawnEntries` **deve** conter pelo menos 1 entrada válida.
  - Em **Strict**: violações são tratadas como *blocker* (log de erro/assert + falha detectável no smoke).
  - Em **Release**: violações continuam sendo erro, mas a estratégia pode ser “fail-fast com fallback controlado” apenas se não corromper estado (preferir manter o contrato, não mascarar).

- **Cenas de frontend/menus (ex.: MenuScene)**
  - `WorldDefinition` **não é exigida**.
  - Em **Strict**: a ausência é permitida **desde que** a cena seja classificada como não-gameplay (ver *classifier* abaixo).

### Classificação (classifier)

A decisão de exigir `WorldDefinition` deve ser guiada por um classificador (ex.: `IGameplayResetTargetClassifier`) e/ou pelo profile do `SceneFlow`.

Evidência canônica mostra:

- `profile=startup` e `profile=frontend`: reset pode ser SKIP.
- `profile=gameplay`: reset executa e o spawn pipeline roda.

## Evidência

### Log canônico (Baseline 2.2)

- **Arquivo:** `Docs/Reports/Evidence/2026-01-31/Baseline-2.2-Evidence-2026-01-31.md`

#### MenuScene permite ausência de WorldDefinition

```
[SceneBootstrapper] Setup OK :: hasWorldDefinition=False :: scene='MenuScene'
[SceneBootstrapper] WorldDefinition is null (allowed in scenes that do not spawn actors). :: scene='MenuScene'
```

#### GameplayScene exige WorldDefinition e registra spawn services

```
[SceneBootstrapper] Setup OK :: hasWorldDefinition=True :: scene='GameplayScene'
[SceneBootstrapper] WorldDefinition loaded :: entries=2 :: scene='GameplayScene'
[SceneBootstrapper] Registered IWorldSpawnContext.
[SceneBootstrapper] Registered ISpawnDefinitionService.
[SceneBootstrapper] Registered ISpawnRegistry.
```

#### Reset executa e resulta em multi-actor

```
[WorldLifecycle] ResetWorld START :: profile=gameplay :: id=2 :: reason='SceneFlow/ScenesReady'
[WorldLifecycle] Spawns COMPLETE :: spawnCount=2 :: registryCount=2 :: actors=[Player,Eater]
[WorldLifecycle] ResetWorld COMPLETE :: id=2
```

### Auditoria Strict/Release

- **Arquivo:** `Docs/Reports/Audits/2026-01-31/Invariants-StrictRelease-Audit.md`

## Consequências

### Positivas

- **Dados como fonte de verdade** para a composição do mundo (multi-actor) em gameplay.
- Melhor **auditabilidade** (logs determinísticos e verificáveis) e integração com Baseline.
- Separação clara entre **frontend** (sem spawn) e **gameplay** (com spawn obrigatório).

### Trade-offs

- Requer disciplina de content pipeline: `WorldDefinition` precisa existir e ser mantida correta em cenas classificadas como gameplay.
- Strict pode aumentar fricção durante iteração (o que é intencional para evitar regressões silenciosas).

## Alternativas consideradas

1. **Hardcode de spawns em código**
   - Rejeitada: baixa auditabilidade e alto risco de divergência com content.

2. **Um único “WorldDefinition global” para todas as cenas**
   - Rejeitada: menus/frontend não devem pagar custo nem sofrer contrato de gameplay.

## Ações futuras

- Garantir que o **classifier** de “cena de gameplay” seja a fonte canônica da exigência de `WorldDefinition`.
- Se necessário, adicionar uma checagem explícita no `SceneBootstrapper`:
  - `if (isGameplayScene && worldDefinition == null) -> error/assert`.

## Implementação (arquivos impactados)

> **TBD:** este ADR não contém caminhos de implementação explicitados no documento atual. Preencher quando o mapeamento de código/scene/prefab for consolidado.
