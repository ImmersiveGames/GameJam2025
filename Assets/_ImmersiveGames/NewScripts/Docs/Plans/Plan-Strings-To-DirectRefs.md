# Plano de Execução (Incremental): **Strings → Referências Diretas** (SOs + Enums)
**Projeto:** Unity 6 / `NewScripts` (SceneFlow + Navigation + LevelFlow)
**Data:** 2026-02-13
**Status:** visão geral (uma fase por vez; prompts do Codex são isolados por fase).

## Escopo do problema (no repo atual)
Hoje o “wiring” ainda depende de **strings** principalmente em dois pontos:

1) **Resources.Load por path (múltiplos)**
- `GlobalCompositionRoot.NavigationInputModes.cs` carrega 3 assets por Resources:
    - `Navigation/GameNavigationCatalog`
    - `Navigation/TransitionStyleCatalog`
    - `Navigation/LevelCatalog`
- `GlobalCompositionRoot.SceneFlowRoutes.cs` carrega:
    - `SceneFlow/SceneRouteCatalog`
- `GlobalCompositionRoot.SceneFlowTransitionProfiles.cs` carrega:
    - `SceneFlow/SceneTransitionProfileCatalog` (via `SceneTransitionProfileCatalogAsset.DefaultResourcesPath`)

2) **Dados de cena por string (nomes de cenas)**
- `SceneRouteCatalogAsset` guarda `scenesToLoad/scenesToUnload/targetActiveScene` como string.

> Observação: o domínio já está bem encaminhado com ids tipados (`LevelId`, `SceneRouteId`, `TransitionStyleId`) e com “F3: Route como fonte única de Scene Data” (LevelDefinition legacy ignorado).

---

## Objetivos (fechado =)
1. Substituir ligações por string por **referências diretas** entre ScriptableObjects onde for seguro.
2. Criar um **SO raiz** de configuração para o bootstrap (single-load) que referencia:
    - `GameNavigationCatalogAsset`
    - `TransitionStyleCatalogAsset`
    - `LevelCatalogAsset`
    - `SceneRouteCatalogAsset`
    - `SceneTransitionProfileCatalogAsset`
3. Migração gradual com **fallback temporário** + logs `[OBS]` (evidência).
4. Isolar strings inevitáveis (nome de cena) dentro de `SceneKeyAsset` para reduzir typo.

**Não-objetivo:** Addressables (fora; apenas preparar terreno).
**Restrições:** mudanças pequenas/verificáveis (baseline/logs), evitar churn em GameLoop/WorldLifecycle.

---

## O que ainda precisa ser string (por enquanto)
| Item | Por quê | Mitigação |
|---|---|---|
| Nome da cena | Unity runtime carrega por nome/path (sem Addressables) | encapsular em `SceneKeyAsset` |
| `reason` / anchors | contrato de evidência/baseline | manter string (não renomear) |
| `routeId` (intents) | UI/Bindings já usam strings canônicas (`to-menu`, `to-gameplay`) | manter como constantes (`GameNavigationIntents`) |

---

## Fases (uma por vez)

### Fase 0 — Documentação + “âncora” de observabilidade (zero risco)
**Objetivo:** colocar o plano dentro do repo e imprimir 1 log âncora de versão (para rastrear execução).

**Mudanças**
- Adicionar este documento em:
  `Assets/_ImmersiveGames/NewScripts/Docs/Plans/Plan-Strings-To-DirectRefs.md`
- Adicionar 1 log no boot:
  `"[OBS][Config] Plan=StringsToDirectRefs v1"` (uma vez, verbose)

**Aceite**
- Compila.
- Nenhuma mudança funcional; apenas doc + 1 linha de log.

---

### Fase 1 — SO raiz “single-load” + fallback (reduz Resources.Load múltiplos)
**Objetivo:** introduzir `NewScriptsBootstrapConfigAsset` e usar **preferência por referência direta** no bootstrap.

**Criar**
- `NewScriptsBootstrapConfigAsset` (ScriptableObject) com campos:
    - `GameNavigationCatalogAsset navigationCatalog`
    - `TransitionStyleCatalogAsset transitionStyleCatalog`
    - `LevelCatalogAsset levelCatalog`
    - `SceneRouteCatalogAsset sceneRouteCatalog`
    - `SceneTransitionProfileCatalogAsset transitionProfileCatalog`

**Alterar (mínimo)**
- `GlobalCompositionRoot.NavigationInputModes.cs`
    - `RegisterGameNavigationService()` deve:
        1) tentar obter `NewScriptsBootstrapConfigAsset` (via Resources path único ou via `RuntimeModeConfig` se você preferir)
        2) se existir e os campos estiverem setados: usar referências diretas
        3) senão: fallback para Resources paths atuais (com `[OBS]`)
- `GlobalCompositionRoot.SceneFlowRoutes.cs`
    - idem: preferir `sceneRouteCatalog` do bootstrap config
- `GlobalCompositionRoot.SceneFlowTransitionProfiles.cs`
    - idem: preferir `transitionProfileCatalog` do bootstrap config

**Logs `[OBS]`**
- `[OBS][Config] BootstrapConfigResolvedVia=... asset=...`
- `[OBS][Config] CatalogResolvedVia=Bootstrap field=<x>`
- `[OBS][Config] CatalogResolvedVia=LegacyResources path=<x>`

**Aceite**
- Se o bootstrap config não existir, nada quebra (fallback).
- Se existir, reduz recursos loads para **1** (o root config).

---

### Fase 2 — `SceneKeyAsset`: encapsular nome de cena (sem Addressables)
**Objetivo:** parar de usar string solta para cena em rotas (mas ainda carregar por string internamente).

**Criar**
- `SceneKeyAsset` com `string sceneName` (validação Editor opcional depois)

**Alterar**
- `SceneRouteCatalogAsset.RouteEntry`: trocar `string[]` por `SceneKeyAsset[]` (e `SceneKeyAsset activeScene`)
- `SceneRouteDefinition`: expor `IReadOnlyList<string>` ainda (por enquanto), mas construído a partir de `SceneKeyAsset.SceneName`.
- Manter campos string legados temporários (se necessário) com fallback + `[OBS]`

**Aceite**
- Rotas principais migradas (pelo menos frontend e gameplay).
- Logs confirmam resolução via `SceneKeyAsset`.

---

### Fase 3 — “Referência direta” entre assets (reduzir ids tipados no wiring)
**Objetivo:** onde fizer sentido, preferir referência direta a SO em vez de `SceneRouteId`/`LevelId`.

**Exemplos (incrementais)**
- `LevelDefinition` pode ganhar `SceneRouteDefinitionAsset routeRef` (se você criar um SO por rota)
  *OU* manter `SceneRouteId` como está (já é tipado e razoável).
- `GameNavigationCatalogAsset.RouteEntry` pode referenciar diretamente uma “RouteAsset” (em vez de `SceneRouteId`) — opcional.

> Esta fase é opcional; só vale a pena se os ids estiverem causando churn. O maior ganho prático vem das Fases 1 e 2.

---

### Fase 4 — Hardening (remover legado quando evidência estiver sólida)
**Objetivo:** remover paths/resources e campos legacy onde já não são usados em produção.

**Aceite**
- Baseline passa.
- Nenhum log `[OBS] ...Via=LegacyResources` nos fluxos de produção.

---

## Operação com Codex
- **1 prompt por fase** (não misturar).
- Sempre pedir:
    - fallback temporário
    - logs `[OBS]`
    - evitar tocar em GameLoop/WorldLifecycle

---

## Checklist rápido de validação
- [ ] Compila
- [ ] Boot → Menu OK
- [ ] Menu → Gameplay OK
- [ ] Restart OK
- [ ] Logs `[OBS]` aparecem conforme fase
