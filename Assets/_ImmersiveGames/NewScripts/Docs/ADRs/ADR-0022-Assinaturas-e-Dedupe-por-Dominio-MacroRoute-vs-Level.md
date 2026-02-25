# ADR-0022 — Assinaturas e Dedupe por Domínio (MacroRoute vs Level)

## Status

- Estado: **Aceito (Implementado)**
- Data (decisão): 2026-02-19
- Última atualização: 2026-02-25
- Tipo: Implementação
- Escopo: NewScripts/Modules (SceneFlow, Navigation, LevelFlow, WorldLifecycle)

## Resumo

Padronizar **assinaturas** e **dedupe** separando claramente dois domínios:

- **MacroRoute / SceneFlow**: assinatura macro para transições de cenas (load/unload/active/fade/profile).
- **Level / LevelFlow**: assinatura de level para seleção e conteúdo (levelId + contentId) dentro de um macro.

Objetivo: evitar acoplamento/ambiguidade (ex.: usar “macroSignature” como se fosse “levelSignature”), e permitir resets locais sem disparar transição macro.

## Contexto

O projeto possui:

- **SceneFlow** orquestrando transições macro por `routeId` (Menu, Gameplay base etc).
- **LevelFlow** selecionando level e conteúdo dentro de um macro que suporta levels.
- **WorldLifecycle** executando reset macro (spawn/despawn) e reset local (quando aplicável).

Sem separação de assinaturas, o dedupe pode bloquear ações legítimas (ex.: reset local) e a observabilidade fica confusa (logs misturam “macro route” com “level”).

## Decisão

### 1) Duas assinaturas canônicas

#### Assinatura Macro (SceneFlow)

- Nome: `macroSignature` (ou simplesmente `signature` nos eventos de SceneFlow).
- Fonte: `SceneTransitionService` / `SceneFlowSignatureCache`.
- Componentes mínimos:
  - `routeId`
  - `styleId`
  - `profileId` e `profileAsset`
  - `activeScene`
  - `useFade` (ou equivalente)
  - `scenesToLoad` / `scenesToUnload` (ou um hash determinístico delas)

> Esta assinatura **não** carrega `levelId`/`contentId`. Level é um domínio separado.

#### Assinatura Level (LevelFlow)

- Nome: `levelSignature`.
- Fonte: `LevelFlowRuntimeService` (ou serviço equivalente de seleção).
- Componentes mínimos:
  - `levelId`
  - `routeId` (micro-route/route do level, se existir)
  - `contentId`
  - `reason`
  - `v` (versão incremental, monotônica, por macro)

### 2) Dedupe por domínio

- **Macro dedupe**:
  - `SceneTransitionService` pode dedupar transições macro por `macroSignature`.
  - `SceneFlowInputModeBridge` e similares podem dedupar re-aplicações por `macroSignature`.

- **Level dedupe**:
  - `LevelFlowRuntimeService` dedupa eventos de seleção/aplicação por `{macroRouteId, levelId, contentId, v}` (ou por `levelSignature`).
  - Um reset local (LevelReset) **não** deve ser bloqueado por dedupe macro.

### 3) Contrato de logs [OBS]

- Logs do SceneFlow devem sempre incluir a **macro signature**.
- Logs do LevelFlow devem sempre incluir a **level signature**.
- Bridges (Navigation/Restart/ContentSwap) devem registrar **ambas** quando fizer sentido:
  - `macroSignature` para “onde estou” (contexto macro)
  - `levelSignature` para “o que está selecionado/aplicado” (contexto de level)

## Implementação atual (2026-02-25)

### Evidências (anchors do log canônico)

- SceneFlow usa assinatura macro (`signature='r:...|s:...|p:...|pa:...|a:...|f:...'`) em:
  - `TransitionStarted`, `ScenesReady`, `TransitionCompleted`.
- LevelFlow publica e propaga assinatura de level:
  - `levelSignature='level:level.1|route:level.1|content:content.default|reason:Menu/PlayButton'` com `v='1'`/`v='2'`.
- Há dedupe explícito por assinatura macro em bridges:
  - `SceneFlowInputModeBridge ... reset dedupe. signature='...'`.
- Restart usa snapshot com contexto de level e não depende da assinatura macro para decidir “o que reiniciar”:
  - `RestartUsingSnapshot routeId='level.1', levelId='level.1', contentId='content.default', styleId='style.gameplay' ...`.

## Implicações

- Dedupe macro fica estável e previsível (não interfere em trocas locais de level/conteúdo).
- Observabilidade melhora: cada log “fala do seu domínio” e fica simples correlacionar.
- Simplifica a evolução do F4/F5 do LevelFlow: “StartGameplayAsync(levelId)” passa a ser trilho canônico.

## Critérios de aceite (DoD)

- [x] Existe distinção clara entre `macroSignature` (SceneFlow) e `levelSignature` (LevelFlow).
- [x] Logs [OBS] exibem assinaturas corretas em seus domínios.
- [x] Dedupe macro não bloqueia reset/troca local de level/conteúdo.
- [ ] Hardening: adicionar testes/QA que validem colisão (mesmo macroSignature com levelSignature diferentes) sem transição macro.

## Changelog

- 2026-02-25: Marcado como **Implementado**, adicionadas evidências e alinhamento do contrato de logs/dedupe com o comportamento observado no log canônico.
