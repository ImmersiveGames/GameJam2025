# Plano (P-001) — Execução (Incremental): **Strings → Referências Diretas** (SOs + Enums)
**Projeto:** Unity 6 / `NewScripts` (SceneFlow + Navigation + LevelFlow)
**Data:** 2026-02-13
**Status:** DONE (concluído após fechamento de F0–F5)

## Status

- ActivityId: **P-001**
- Estado: **DONE**
- Última atualização: **2026-02-17**

### Fonte de verdade (referências)

- Contrato canônico: `Docs/Standards/Standards.md#observability-contract`
- Política Strict/Release: `Docs/Standards/Standards.md#politica-strict-vs-release`
- Evidência vigente: `Docs/Reports/Evidence/LATEST.md` (log bruto: `Docs/Reports/lastlog.log`)

### Evidências (P-001)

- Auditoria final: `Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Step-06-Final.md`
- Smoke runtime: `Docs/Reports/lastlog.log`
- Smoke datado (DataCleanup v1): `Docs/Reports/Audits/2026-02-17/Smoke-DataCleanup-v1.md`
- Validator de suporte (DataCleanup v1): `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`

### Auditorias relacionadas (status atual)

- `Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Steps-01-02.md`
- `Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Step-06-Final.md`

> Regra: qualquer nova checagem deve gerar um arquivo em `Docs/Reports/Audits/<YYYY-MM-DD>/...`.

## Status atual (2026-02-17)

| Fase | Status | Resumo objetivo |
|---|---|---|
| **F0** | **DONE** | Documento no repositório e âncora de observabilidade ativa no boot (`Plan=StringsToDirectRefs v1`). |
| **F1** | **DONE** | Bootstrap root único implementado; política oficial em runtime é **strict fail-fast** quando bootstrap/root obrigatório está ausente. |
| **F2** | **DONE** | `SceneKeyAsset` em uso no fluxo de rotas, com resolução para nomes de cena no boundary com API da Unity. |
| **F3** | **DONE** | Estratégia **direct-ref-first** consolidada no fluxo principal, com compatibilidade residual tratada no DataCleanup v1. |
| **F4** | **DONE** | Hardening concluído para o escopo v1; resíduos remanescentes migrados/encerrados no DataCleanup v1. |
| **F5** | **DONE** | Fechamento final com validação/smoke e evidências canônicas registradas. |

## Checklist rastreável (alto nível)

- [x] **F0** — Documento no repo + âncora de observabilidade
- [x] **F1** — Bootstrap root único + strict fail-fast
- [x] **F2** — `SceneKeyAsset` no boundary de Unity
- [x] **F3** — Rota como fonte única de SceneData (remover duplicidades)
- [x] **F4** — Hardening final + remoção controlada de compat/legado
- [x] **F5** — Fechamento final com validação/smoke e evidências canônicas

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
- [x] Compila
- [x] Boot → Menu OK
- [x] Menu → Gameplay OK
- [x] Restart OK
- [x] Logs `[OBS]` aparecem conforme fase
