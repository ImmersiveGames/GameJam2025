# ADR-0019 - Navigation Catalog as the Single Canonical Navigation Asset

## Status

- Estado: **Implementado**
- Data (decisao): **2026-02-16**
- Ultima atualizacao: **2026-03-12**
- Escopo: `Assets/_ImmersiveGames/NewScripts/Modules/Navigation` + SceneFlow wiring via `SceneRouteDefinitionAsset`

## Evidencias canonicas (atualizado em 2026-03-12)

- `Docs/Reports/Evidence/LATEST.md`
- `Docs/Reports/lastlog.log`
- `Docs/Reports/Audits/2026-03-11/NAVIGATION-INTENTCATALOG-SIMPLIFY.md`
- `Docs/Reports/Audits/2026-03-11/NAVIGATION-INTENTCATALOG-REMOVE.md`
- `Docs/Reports/Audits/2026-03-11/TRANSITIONSTYLE-DIRECTREF-REFACTOR.md`
- `Docs/Reports/Audits/2026-03-11/TRANSITION-DIRECTREF-FAILFAST-PURGE.md`

## Context

A arquitetura de navegacao ja havia sido consolidada em torno de um unico asset canonico de navigation, mas ainda existiam trilhos residuais em que `styleId` e `profileId` apareciam em requests, logs, signatures e validacoes como se ainda fossem parte do contrato estrutural.

A consolidacao final exige:

- `GameNavigationCatalogAsset` configurado apenas com `routeRef + transitionStyleRef`
- `TransitionStyleAsset` como owner direto de `profileRef + useFade`
- `NewScriptsBootstrapConfigAsset.startupTransitionStyleRef` como unica fonte valida de startup transition
- fail-fast quando uma referencia obrigatoria estiver ausente
- nenhuma resolucao estrutural por `styleId`, `profileId` ou catalogos nominais paralelos

## Decision

### 1) `GameNavigationCatalogAsset` continua como unico asset canonico de navigation

- **Path canonico:** `Assets/Resources/Navigation/GameNavigationCatalog.asset`
- Responsavel por:
  - Expor slots core explicitos (menu/gameplay + opcionais)
  - Ser owner unico de `routeRef + transitionStyleRef`
  - Manter extras/custom em `routes`

### 2) `TransitionStyleAsset` vira o unico owner estrutural de style

- Cada `TransitionStyleAsset` carrega:
  - `profileRef` como referencia estrutural obrigatoria
  - `useFade` como politica estrutural obrigatoria
- O runtime resolve styles apenas por referencia direta ao asset.
- Qualquer nome exposto (`style`, `profile`) passa a ser apenas metadata descritiva derivada do asset (`asset.name`).

### 3) Bootstrap usa apenas `startupTransitionStyleRef`

`NewScriptsBootstrapConfigAsset` passa a exigir apenas:

- `navigationCatalog`
- `sceneRouteCatalog`
- `startupTransitionStyleRef`
- `fadeSceneKey`

Nao existe mais fallback para:

- `startupTransitionProfile`
- `style.startup` em catalogo legado

### 4) `styleId` e `profileId` deixam de participar do runtime estrutural

- Nao entram mais na resolucao de navigation
- Nao entram mais na resolucao de transition
- Nao entram mais na classificacao de comportamento em InputMode/Readiness/IntroStage
- Quando aparecem, aparecem apenas como labels descritivos de asset para observabilidade

## Consequences

- Ha uma unica forma valida de configurar navigation/transition.
- A stack fica realmente direct-ref-first e fail-fast.
- `styleId` e `profileId` deixam de ter papel estrutural; a observabilidade usa labels derivados de asset.
- Assets legados precisam existir ja preenchidos com refs diretas; nao ha mais trilho de compatibilidade silencioso.

## Validation / Evidence

- Audit da fase de simplificacao: `Docs/Reports/Audits/2026-03-11/NAVIGATION-INTENTCATALOG-SIMPLIFY.md`
- Audit da fase de remocao: `Docs/Reports/Audits/2026-03-11/NAVIGATION-INTENTCATALOG-REMOVE.md`
- Audit da fase direct-ref-first: `Docs/Reports/Audits/2026-03-11/TRANSITIONSTYLE-DIRECTREF-REFACTOR.md`
- Audit da purge fail-fast: `Docs/Reports/Audits/2026-03-11/TRANSITION-DIRECTREF-FAILFAST-PURGE.md`
- MenuItem de validacao (Editor): `ImmersiveGames/NewScripts/Tools/SceneFlow/Validate Config`

