# SceneFlow / Navigation / LevelFlow Refactor Plan v2.1.3

> Objetivo macro (inalterado): ter pontos de configuração e modularidade para **cenas**, **transições** e **níveis**, com o mínimo de duplicação de dados e decisões explícitas (evidence-based).

## Correção de nomenclatura

- **Não existe `SceneTransitionProfileId`** no código atual.
- O identificador tipado hoje é **`SceneFlowProfileId`** (ex.: `startup`, `frontend`, `gameplay`).
- O identificador navegável de estilo é **`TransitionStyleId`**. No fluxo alvo:
  - `TransitionStyleId` → resolve **`SceneFlowProfileId` + `UseFade`**.
- Portanto, qualquer passo do plano que mencione “profile id” deve ler como:
  - `SceneFlowProfileId` permanece o **ID tipado do profile de transição** e resolve um `SceneTransitionProfile`.

## As-Is vs Target

| Campo | As-Is (estado atual) | Target (estado desejado) |
| --- | --- | --- |
| `routeId` | Existe como `SceneRouteId`, mas ainda convive com duplicação de dados de cena em pontos adjacentes (Navigation/LevelFlow). | `SceneRouteId` vira fonte única de scene data (load/unload/active) e demais módulos só referenciam a rota. |
| `styleId` | `TransitionStyleId` já existe, mas sua semântica ainda não está explícita em todo o plano/documentação. | `TransitionStyleId` é contrato navegável e resolve deterministicamente `SceneFlowProfileId` + `UseFade`. |
| `profileId` | `SceneFlowProfileId` já é o ID tipado real; ainda há risco de confusão textual com o nome antigo (`SceneTransitionProfileId`). | `SceneFlowProfileId` permanece ID tipado do profile de transição, sem ambiguidade de nomenclatura. |
| `levelId` | Presente no LevelFlow, mas trilho end-to-end ainda não é o único caminho operacional (fluxos paralelos sobrevivem). | `levelId` entra pelo trilho oficial `StartGameplayAsync(levelId)`, com QA/Dev operando no mesmo caminho de produção. |

## Correções aplicadas nesta revisão (v2.1.3)

Correções para compilar sem “gambiarras” e alinhar contratos entre módulos:

- `SceneFlowProfilePaths` agora suporta **basePath** (overload):
  - `SceneFlowProfilePaths.For(SceneFlowProfileId id, string basePath)`
- `SceneTransitionProfileResolver` agora expõe overload que retorna **resolvedPath/origem** para logs/diagnóstico:
  - `Resolve(SceneFlowProfileId id, out string resolvedPath, string contextSignature = null)`
  - Mantém o overload antigo para compatibilidade:
    - `Resolve(SceneFlowProfileId id, string contextSignature = null)`
- Com isso, `SceneFlowFadeAdapter.ConfigureFromProfile(...)` pode logar o path/origem real sem quebrar a API do resolver.

---

## F1 (PRIORIDADE AGORA): Profiles por referência, com fallback controlado

### Problema atual
Hoje, quando não há referência direta, o resolver precisa buscar um `SceneTransitionProfile` por **path** (tipicamente via `Resources.Load`).
Isso acopla o ID a um path e obriga a manter assets em `Resources/`.

### Decisão
Introduzir um **catálogo de profiles por referência direta** (ScriptableObject), e fazer o resolver:
1. Tentar **catálogo** primeiro.
2. Se não houver catálogo (ou não houver entrada), usar **fallback legado** (opcional / controlável) via `Resources`.

### Resultado esperado
- Transições passam a usar **referência direta** para `SceneTransitionProfile` quando o catálogo estiver preenchido.
- Mantém comportamento atual (fallback) quando o catálogo não existir/ainda não estiver pronto.
- Um único “ponto de configuração” para desligar o legado quando você quiser endurecer.

### Configuração recomendada
- Criar os profiles (`SceneTransitionProfile`) normalmente.
- Criar 1 catálogo (`SceneTransitionProfileCatalogAsset`) e preencher ao menos:
  - `startup`, `frontend`, `gameplay`.
- Se quiser auto-load pelo bootstrap, colocar o catálogo em:
  - `Resources/SceneFlow/SceneTransitionProfileCatalog.asset`
  - (path de load: `SceneFlow/SceneTransitionProfileCatalog`)
- Quando estiver 100% coberto pelo catálogo, desligar legado:
  - `SceneTransitionProfileCatalogAsset.AllowLegacyResourcesFallback = false`


## Resources layout canônico (Navigation)

Fonte de verdade para assets carregados via `Resources.Load` no fluxo de navegação:

- **GameNavigationCatalog**
  - Asset: `Assets/Resources/Navigation/GameNavigationCatalog.asset`
  - Load path: `"Navigation/GameNavigationCatalog"`
- **TransitionStyleCatalog**
  - Asset: `Assets/Resources/Navigation/TransitionStyleCatalog.asset`
  - Load path: `"Navigation/TransitionStyleCatalog"`
- **LevelCatalog (LevelFlow)**
  - Asset: `Assets/Resources/Navigation/LevelCatalog.asset`
  - Load path: `"Navigation/LevelCatalog"`
- **SceneRouteCatalog** (mantido como definido atualmente no docs/código)
  - Asset: `Assets/Resources/SceneFlow/SceneRouteCatalog.asset`
  - Load path: `"SceneFlow/SceneRouteCatalog"`

Regra: nenhum asset de Navigation deve ficar na raiz de `Resources/`.

### Evidência (logs)
- Quando resolver via catálogo:
  - `resolvedPath='catalog'`
- Quando cair no legado (apenas 1x por sessão):
  - `[OBS] ... usando fallback legado via Resources ...`

- Boot (antes do clique Play):
  - `[OBS][Navigation] Catalog boot snapshot: ... rawRoutesCount=... builtRouteIdsCount=... hasToGameplay=...`

---

## Status do plano (v2.1.3)

- ✅ **F1 (concluído no escopo principal):** catálogo de profiles por referência direta + fallback legado controlável.
- ✅ **F3 (concluído):** rota é a fonte única de scene data (ScenesToLoad/Unload/Active); Navigation e LevelFlow não duplicam dados em runtime.
- ✅ **Pendências reais:** nenhuma no escopo F1–F5 (F1, F2, F3, F4 e F5 concluídos).

## Próximos passos (atualizado)

- **F3 (status atual):** concluído no código atual — SceneRouteCatalog é a fonte única de Scene Data; LevelDefinition/GameNavigationCatalogAsset mantêm apenas campos LEGACY ignorados com warning de observabilidade.
- **F2 (concluído):** Decisão de Reset/WorldLifecycle centralizada em `IRouteResetPolicy.Resolve(...) -> RouteResetDecision`, aplicada pelo driver canônico do WorldLifecycle.
- **F4 (concluído):** LevelFlow end-to-end pelo trilho oficial `StartGameplayAsync(levelId)` para entrada inicial de gameplay (Menu Play e QA usando o mesmo caminho de produção).
- **F5 (concluído):** Hardening fechado com logs canônicos `[OBS][Navigation]` nos trilhos explícitos (`StartGameplayAsync`, `RestartAsync`, `ExitToMenuAsync`, `GoToMenuAsync/Navigate`) e ContextMenus QA operáveis para `StartGameplayAsync(levelId)`, `RestartAsync` e `ExitToMenuAsync`.
- **Evidência F4:** `MenuPlayButtonBinder` chama `StartGameplayAsync(LevelId.FromName(startLevelId), reason)` com log `[OBS][Navigation] MenuPlay -> StartGameplayAsync ...`.
- **Evidência F4:** `SceneFlowDevContextMenu` QA usa `StartGameplayAsync(level.1)` e expõe ações explícitas para `RestartAsync`/`ExitToMenuAsync` com logs `[OBS][Navigation]`.
- **Evidência F5:** `GameNavigationService.ExecuteEntryAsync(...)` publica anchor canônico `[OBS][Navigation] NavigateAsync -> ...` com `intentId`, `sceneRouteId`, `styleId`, `reason` e `signature`.
- **Evidência F2:** `SceneRouteDefinition` expõe `RouteKind` como metadado da rota para decisão de reset por policy.
- **Evidência F2:** `WorldLifecycleSceneFlowResetDriver` resolve decisão via `_routeResetPolicy.Resolve(context.RouteId, context)` sem duplicar regra no driver.
- **Evidência F2:** logs canônicos `[OBS][WorldLifecycle] ResetRequested ... decisionSource=... reason=...` e `[OBS][WorldLifecycle] ResetCompleted ... decisionSource=... reason=...` mantêm correlação por `signature/routeId/profile/target`.


**Ordem recomendada (histórico executado):** F1 → F3 → F2 → F4 → F5.

**Estado atual:** F1–F5 concluídos; próximos ciclos entram como hardening incremental e regressão contínua.
