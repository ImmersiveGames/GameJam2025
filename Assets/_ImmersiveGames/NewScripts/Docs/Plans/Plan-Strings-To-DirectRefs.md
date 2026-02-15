# Plano de Execução (Incremental): **Strings → Referências Diretas** (SOs + Enums)
**Projeto:** Unity 6 / `NewScripts` (SceneFlow + Navigation + LevelFlow)
**Data:** 2026-02-13
**Status:** em andamento (alinhado ao estado real de runtime + tooling)

## Status atual (2026-02-15)

| Fase | Status | Resumo objetivo |
|---|---|---|
| **F0** | **DONE** | Documento no repositório e âncora de observabilidade ativa no boot (`Plan=StringsToDirectRefs v1`). |
| **F1** | **DONE (Strict)** | Bootstrap root único implementado; política oficial em runtime é **strict fail-fast** quando bootstrap/root obrigatório está ausente. |
| **F2** | **DONE** | `SceneKeyAsset` em uso no fluxo de rotas, com resolução para nomes de cena no boundary com API da Unity. |
| **F3** | **PARTIAL** | Estratégia **direct-ref-first** implementada de forma incremental (`routeRef`), porém IDs tipados (`routeId`) ainda existem como compatibilidade temporária. |
| **F4** | **PARTIAL** | Hardening avançado, mas ainda há resíduos legados em tooling/dev e APIs `[Obsolete]` aguardando janela de remoção. |

---

## Escopo do problema (estado histórico + estado atual)
Historicamente, o “wiring” dependia de **strings** em dois pontos principais:

1) **[Histórico] Resources.Load por path (múltiplos)**
- `GlobalCompositionRoot.NavigationInputModes.cs` carregava 3 assets por Resources:
  - `Navigation/GameNavigationCatalog`
  - `Navigation/TransitionStyleCatalog`
  - `Navigation/LevelCatalog`
- `GlobalCompositionRoot.SceneFlowRoutes.cs` carregava:
  - `SceneFlow/SceneRouteCatalog`
- `GlobalCompositionRoot.SceneFlowTransitionProfiles.cs` carregava:
  - `SceneFlow/SceneTransitionProfileCatalog` (via `SceneTransitionProfileCatalogAsset.DefaultResourcesPath`)

2) **Dados de cena por string (nomes de cenas)**
- `SceneRouteCatalogAsset` mantinha campos string legacy (`scenesToLoad/scenesToUnload/targetActiveScene`).

> Situação atual: o domínio já opera com ids tipados (`LevelId`, `SceneRouteId`, `TransitionStyleId`), `SceneKeyAsset` e hardening em fail-fast no pipeline principal.

---

## Objetivos (fechado =)
1. Substituir ligações por string por **referências diretas** entre ScriptableObjects onde for seguro.
2. Manter um **SO raiz** de configuração para o bootstrap (single-load) que referencia:
   - `GameNavigationCatalogAsset`
   - `TransitionStyleCatalogAsset`
   - `LevelCatalogAsset`
   - `SceneRouteCatalogAsset`
   - `SceneTransitionProfileCatalogAsset`
3. Operar com política explícita de **strict fail-fast** para dependências obrigatórias de configuração.
4. Isolar strings inevitáveis (nome de cena) dentro de `SceneKeyAsset` para reduzir typo.

**Não-objetivo:** Addressables (fora; apenas preparar terreno).
**Restrições:** mudanças pequenas/verificáveis (baseline/logs), evitar churn em GameLoop/WorldLifecycle.

---

## O que ainda precisa ser string (por enquanto)
| Item | Por quê | Mitigação |
|---|---|---|
| Nome da cena | Unity runtime carrega por nome/path (sem Addressables) | encapsular em `SceneKeyAsset` |
| `reason` / anchors | contrato de evidência/baseline | manter string (não renomear) |
| `routeId` (intents) | UI/Bindings já usam strings canônicas (`to-menu`, `to-gameplay`) | manter como constantes (`GameNavigationIntents`) enquanto durar compatibilidade |

---

## Fases (uma por vez)

### Fase 0 — Documentação + “âncora” de observabilidade (zero risco)
**Objetivo:** manter plano no repo e log âncora de versão para rastrear execução.

**Aceite**
- Compila.
- Nenhuma mudança funcional; apenas doc + log de evidência.

---

### Fase 1 — SO raiz “single-load” com política strict fail-fast
**Objetivo:** usar `NewScriptsBootstrapConfigAsset` como root único de config em runtime.

**Política oficial (atualizada)**
- Para dependências obrigatórias do bootstrap/root, a política é **strict fail-fast**.
- Se bootstrap/root obrigatório faltar, o sistema **não** entra em fallback silencioso para múltiplos `Resources.Load` de produção.

**Logs `[OBS]` esperados**
- `[OBS][Config] BootstrapConfigResolvedVia=... asset=...`
- `[OBS][Config] CatalogResolvedVia=Bootstrap field=<x>`

**Aceite**
- Com bootstrap válido: catálogos resolvidos por referência direta.
- Sem bootstrap obrigatório: erro explícito (fail-fast), com diagnóstico por log.

---

### Fase 2 — `SceneKeyAsset`: encapsular nome de cena (sem Addressables)
**Objetivo:** evitar string solta para cena em rotas, mantendo boundary string apenas no carregamento Unity.

**Aceite**
- Rotas principais migradas para `SceneKeyAsset`.
- Resolução de `SceneRouteDefinition` baseada em referências, sem regressão de fluxo.

---

### Fase 3 — “Direct-ref-first” entre assets (com compatibilidade temporária por IDs)
**Objetivo:** consolidar modelo **direct-ref-first** no wiring de conteúdo.

**Diretriz**
- Referências diretas (`routeRef`/SO) devem ser priorizadas em novos conteúdos e fluxos críticos.
- IDs tipados (`SceneRouteId`, `LevelId`) permanecem como **compatibilidade temporária**, com plano de retirada progressiva.

**Critério de saída (DONE)**
1. `routeRef` obrigatório para rotas críticas (ex.: Menu e Gameplay) nos assets relevantes.
2. Validação de Editor impedindo configuração incompleta para essas rotas críticas.
3. Logs `[OBS]` confirmando resolução via direct-ref no caminho principal.
4. Ausência de fallback degradado em runtime para rotas críticas.

---

### Fase 4 — Hardening (remoção de legado remanescente)
**Objetivo:** fechar resíduos de legado após estabilização de evidências.

**Itens restantes (exatos)**
1. Remover fallback `Resources` do tooling dev em `SceneFlowDevContextMenu`.
2. Remover/encapsular helpers legados de `Resources` em `GlobalCompositionRoot.NavigationInputModes`.
3. Planejar remoção das APIs `[Obsolete]` após janela de migração.

**Critério para remoção de `[Obsolete]`**
- Todos os consumidores migrados para trilhos oficiais (`ILevelFlowRuntimeService` / APIs canônicas).
- Janela de compatibilidade encerrada e registrada em changelog.
- Smoke/baseline sem chamadas aos métodos obsoletos.

---

## Evidências canônicas

### Logs `[OBS]`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Entry.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.BootstrapConfig.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`

### Arquivos-chave de configuração e catálogo
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Config/NewScriptsBootstrapConfigAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/SceneRouteCatalogAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Navigation/Bindings/TransitionStyleCatalogAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/GameNavigationCatalogAsset.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/LevelFlow/Runtime/LevelDefinition.cs`

### Tooling/legado em hardening
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Dev/SceneFlowDevContextMenu.cs`
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.NavigationInputModes.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/IGameNavigationService.cs`

---

## Operação com Codex
- **1 prompt por fase** (não misturar).
- Solicitar sempre:
  - logs `[OBS]`
  - validação explícita de fail-fast em configurações obrigatórias
  - evitar tocar em GameLoop/WorldLifecycle fora do escopo da fase

---

## Checklist rápido de validação
- [ ] Compila
- [ ] Boot → Menu OK
- [ ] Menu → Gameplay OK
- [ ] Restart OK
- [ ] Logs `[OBS]` aparecem conforme fase
