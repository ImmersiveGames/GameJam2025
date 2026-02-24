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

## F1 (ATUAL): Profiles por referência direta (fallback por Resources é histórico)

### Problema (histórico)
Durante a migração, quando faltava referência direta, o resolver podia buscar `SceneTransitionProfile` por path (`Resources.Load`).
No estado atual, o runtime principal está alinhado em catálogo/referências via BootstrapConfig/DI.

### Decisão
Introduzir um **catálogo de profiles por referência direta** (ScriptableObject), e fazer o resolver:
1. Tentar **catálogo** primeiro.
2. **[Histórico]** Durante janela de migração, havia fallback legado opcional via `Resources`.

### Resultado esperado
- Transições passam a usar **referência direta** para `SceneTransitionProfile` quando o catálogo estiver preenchido.
- No estado atual, o trilho de produção usa fail-fast para dependências obrigatórias de catálogo/config.
- Um único “ponto de configuração” para desligar o legado quando você quiser endurecer.

### Configuração recomendada (estado atual)
- Criar os profiles (`SceneTransitionProfile`) normalmente.
- Criar 1 catálogo (`SceneTransitionProfileCatalogAsset`) e preencher ao menos:
  - `startup`, `frontend`, `gameplay`.
- Registrar catálogo/referências no `NewScriptsBootstrapConfigAsset` e validar resolução via DI no boot.
- Política atual de produção: dependências obrigatórias ausentes => fail-fast com logs `[FATAL][Config]`/`[OBS]`.


## Resources layout (histórico)

> **Histórico:** esta seção descreve o layout usado no período de migração.
> Estado atual de produção: catálogos/referências são resolvidos via BootstrapConfig/DI, com uso de `Resources` restrito ao bootstrap root single-load quando aplicável.

### Evidência (logs)
- Quando resolver via catálogo:
  - `resolvedPath='catalog'`
- **[Histórico]** Durante migração: `[OBS] ... usando fallback legado via Resources ...`.

- Boot (antes do clique Play):
  - `[OBS][Navigation] Catalog boot snapshot: ... rawRoutesCount=... builtRouteIdsCount=... hasToGameplay=...`

---

## Status do plano (v2.1.3)

- ✅ **F1 (concluído no escopo principal):** catálogo de profiles por referência direta via BootstrapConfig/DI; fallback legado por `Resources` tratado como histórico.
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
