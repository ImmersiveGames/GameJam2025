# Audit — StringsToDirectRefs v1 — Step 04 (P-001 F4.1)

- **Data:** 2026-02-16
- **Escopo:** `Assets/_ImmersiveGames/NewScripts/**`
- **Foco:** eliminar resolução por string / varredura de rotas em código Dev/Editor/QA/ContextMenu quando o caminho pode ser direct-ref-first.

## 1) Comandos de auditoria executados

```bash
rg -n "\[ContextMenu\(|MenuItem\(" Assets/_ImmersiveGames/NewScripts --glob '*.cs'
```

```bash
rg -n "\"to-menu\"|\"to-gameplay\"|\"victory\"|\"defeat\"|\"restart\"|\"exit-to-menu\"|\"gameover\"|Resources\.Load<.*(Catalog|Route)|Resources\.Load\(" Assets/_ImmersiveGames/NewScripts/Modules Assets/_ImmersiveGames/NewScripts/Infrastructure Assets/_ImmersiveGames/NewScripts/Editor
```

```bash
rg -n "NavigationIntentId\.FromName|GameNavigationIntents\.|SceneRouteId\.From|RouteId\.Value|FindRouteByIntent|FindAssets\(\"t:SceneRouteDefinitionAsset\"\)|ISceneRouteCatalog|GameNavigationIntentCatalogAsset|GameNavigationCatalogAsset" Assets/_ImmersiveGames/NewScripts --glob '*Dev*' --glob '*Editor*' --glob '*QA*' --glob '*.cs'
```

```bash
rg -n "FindRouteByIntent\(|RouteId\.Value.*intent|FindAssets\(\"t:SceneRouteDefinitionAsset\"\)" Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Dev/Editor/GameNavigationCatalogNormalizer.cs || true
```

```bash
rg -n "Resources\.Load<.*(Catalog|Route)|Resources\.Load\(" Assets/_ImmersiveGames/NewScripts --glob '*Dev*' --glob '*Editor*' --glob '*QA*' --glob '*.cs' || true
```

## 2) Achados

- **Arquivo crítico encontrado:**
  - `Modules/Navigation/Dev/Editor/GameNavigationCatalogNormalizer.cs`
- **Problema identificado (pré-correção):**
  - Resolução de rota por string (`FindRouteByIntent`) com varredura de assets (`AssetDatabase.FindAssets("t:SceneRouteDefinitionAsset")`) e comparação com `route.RouteId.Value`.
  - Esse caminho contrariava o objetivo **direct-ref-first** do P-001 F4.1.

## 3) Correção aplicada

- Migrado o normalizer para **direct-ref-first**:
  - resolve `routeRef` obrigatório de `to-menu` e `to-gameplay` diretamente do bloco `core` do `GameNavigationIntentCatalogAsset`;
  - usa esses `routeRef` para preencher intents extras e slots no `GameNavigationCatalogAsset`;
  - remove varredura e matching por string de `SceneRouteDefinitionAsset`.
- Política Dev/Editor de falha explícita:
  - qualquer wiring ausente/inválido agora gera `Debug.LogError` com prefixo `[FATAL][Config]` + `InvalidOperationException`.
- Garantia arquitetural:
  - alteração ficou restrita a arquivo **Editor-only** (`Modules/Navigation/Dev/Editor`), sem introduzir dependência de runtime em Dev/Editor.

## 4) Evidência pós-correção

- `FindRouteByIntent` / `FindAssets("t:SceneRouteDefinitionAsset")` no normalizer: **sem matches**.
- `Resources.Load` em Dev/Editor/QA no escopo `NewScripts`: **sem matches**.
- Presença de fail-fast explícito no normalizer:
  - `GetRequiredCoreRouteRefOrFail(...)`
  - `SetIntentCatalogReferenceOrFail(...)`
  - `FailFastEditor(...)`

## 5) Arquivos alterados nesta etapa

- `Assets/_ImmersiveGames/NewScripts/Modules/Navigation/Dev/Editor/GameNavigationCatalogNormalizer.cs`
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-02-16/Audit-StringsToDirectRefs-v1-Step-04.md`
