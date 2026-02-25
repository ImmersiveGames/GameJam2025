# ADR-0024 — LevelCatalog por MacroRoute e Contrato de Seleção de Level Ativo

## Status

- Estado: **Em implementação (parcial em produção)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-25
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, Navigation, LevelFlow, WorldLifecycle)

## Resumo

Formalizar que:

- **MacroRoutes** representam espaços macro (Menu, Gameplay, Tutorial, Hub).
- Alguns macros possuem **catálogo de levels**.
- Ao entrar em um macro com levels, existe **exatamente 1 level ativo**, com seleção determinística e observável.

## Contexto

Hoje já existem evidências de:

- `LevelCatalog` construindo entradas e mapeando levels para rotas.
- `StartGameplayAsync(levelId)` como trilho canônico.
- Snapshot de restart com `{levelId, routeId, contentId, v}`.

O que falta consolidar é o vínculo explícito: **macroRoute → catálogo de levels**, e evitar que `routeId` do level seja usado como se fosse o “macroRouteId”.

## Decisão

Definir um contrato explícito:

- Cada **MacroRouteDefinition** (ou `RouteKind=Gameplay/Tutorial`) pode opcionalmente ter:
  - `LevelCollectionRef` (catálogo/coleção de `LevelDefinition`)
  - `DefaultLevelId` (ou “primeiro do catálogo”)

- Existe **exatamente 1 level ativo por macro**:
  - `ILevelSelectionState` guarda `{ macroRouteId, levelId, contentId, version }`
  - Mudança de macro invalida seleção anterior e força seleção do default.

- `LevelCatalog` é *source-of-truth* para produção, mas QA/Dev pode inserir entradas auxiliares (qa.*) para evidência.

### Regras

- `LevelDefinition` **não** carrega/unload “cenas macro”.
- `LevelDefinition` descreve apenas:
  - conteúdo do level (cenas/Addressables futuramente);
  - variantes (contentId/contentRef);
  - flags/stages (`hasIntroStage`, `hasPostLevel`, etc.).

## Implementação atual (2026-02-25)

### O que está comprovado pelo log

- `LevelCatalogBuild levelsResolved=4 ...`
- `MenuPlay -> StartGameplayAsync levelId='level.1'`
- Publicação de seleção:
  - `LevelSelectedEventPublished levelId='level.1' routeId='level.1' contentId='content.default' v='1' levelSignature='...'`
- Snapshot para restart:
  - `RestartContextService GameplayStartSnapshotUpdated levelId='level.1' routeId='level.1' styleId='style.gameplay' contentId='content.default' v='1' ...`

### Lacunas (ainda não fechadas)

- **Separar `macroRouteId` de `routeId` do level**:
  - No log atual, o `routeId` usado na transição para gameplay é `level.1` (ou seja, o level ainda está “virando” a route macro).
- Vínculo explícito **macro → catálogo** ainda não está evidenciado (só vemos catálogo e seleção, não o “owner macro”).

## Critérios de aceite (DoD)

- [ ] Ao entrar em um macro com catálogo, o sistema seleciona automaticamente o default level (sem depender do Menu/Play).
- [x] `StartGameplayAsync(levelId, reason)` é trilho canônico e observado em log.
- [x] Logs [OBS] mostram:
  - Macro: route/policy/started/ready/completed
  - Level: level selecionado + conteúdo aplicado (levelSignature + v)
- [ ] QA/Dev demonstra N→1 sem transição macro:
  - mesmo macroRouteId;
  - levelId distintos;
  - contentId distintos;
  - sem `TransitionStarted`.

## Changelog

- 2026-02-25: Atualizado para **parcial** com base no log; adicionadas evidências de LevelCatalog/seleção/snapshot e registradas lacunas (macroRouteId vs level routeId).
