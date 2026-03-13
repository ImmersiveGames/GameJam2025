# ADR-0019 - Navigation Catalog as the Single Canonical Navigation Asset

## Status

- Estado: **Implementado**
- Data (decisao): **2026-02-16**
- Ultima atualizacao: **2026-03-12**
- Escopo: `Assets/_ImmersiveGames/NewScripts/Modules/Navigation` + SceneFlow wiring via `SceneRouteDefinitionAsset`

## Evidencias canonicas

- `Docs/Reports/Audits/LATEST.md`
- `Docs/Reports/Audits/2026-03-12/DOCS-FINAL-CLOSEOUT.md`
- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/lastlog.log`

## Context

A arquitetura final de navigation/transition foi consolidada para um modelo direct-ref + fail-fast. O sistema nao depende mais de catalogos nominais paralelos para resolver intents, styles ou profiles.

## Decision

### 1) `GameNavigationCatalogAsset` e o unico asset canonico de navigation

- **Path canonico:** `Assets/Resources/Navigation/GameNavigationCatalog.asset`
- Responsavel por:
  - expor slots core explicitos
  - ser owner unico de `routeRef + transitionStyleRef`
  - manter extras/custom em `routes`

### 2) `TransitionStyleAsset` e o owner estrutural de style

- Cada `TransitionStyleAsset` carrega:
  - `profileRef` como referencia estrutural obrigatoria
  - `useFade` como politica estrutural obrigatoria
- O runtime resolve styles apenas por referencia direta ao asset.
- Nomes expostos de style/profile sao apenas metadata descritiva derivada do asset.

### 3) Bootstrap usa apenas `startupTransitionStyleRef`

`NewScriptsBootstrapConfigAsset` exige apenas:
- `navigationCatalog`
- `sceneRouteCatalog`
- `startupTransitionStyleRef`
- `fadeSceneKey`

Nao existe fallback para `startupTransitionProfile` nem para catalogos legados.

### 4) Semantica de fluxo fica fora de style/profile

- `startup` pertence ao bootstrap.
- `frontend/gameplay` pertencem a `SceneRouteKind`.
- `style` e `profile` permanecem apenas como labels de observabilidade.

## Consequences

- Ha uma unica forma valida de configurar navigation/transition.
- A stack fica direct-ref-first e fail-fast.
- `GameNavigationCatalogAsset` fica como owner unico de navigation.
- `TransitionStyleAsset` fica como owner unico de style.
- `SceneTransitionProfile` fica como asset leaf visual.

## Validation / Evidence

- Leitura operacional atual: `Docs/Reports/Audits/LATEST.md`
- Evidencia runtime atual: `Docs/Reports/lastlog.log`
- Fechamento docs-only do estado vigente: `Docs/Reports/Audits/2026-03-12/DOCS-FINAL-CLOSEOUT.md`