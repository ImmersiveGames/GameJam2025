# SceneFlow / Navigation / LevelFlow Refactor Plan v2.1.3

> Objetivo macro (inalterado): ter pontos de configuração e modularidade para **cenas**, **transições** e **níveis**, com o mínimo de duplicação de dados e decisões explícitas (evidence-based).

## Correção de nomenclatura

- **Não existe `SceneTransitionProfileId`** no código atual.
- O identificador tipado hoje é **`SceneFlowProfileId`** (ex.: `startup`, `frontend`, `gameplay`).
- Portanto, qualquer passo do plano que mencione “profile id” deve ler como:
  - `SceneFlowProfileId` → resolve um `SceneTransitionProfile`.

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

### Evidência (logs)
- Quando resolver via catálogo:
  - `resolvedPath='catalog'`
- Quando cair no legado (apenas 1x por sessão):
  - `[OBS] ... usando fallback legado via Resources ...`

---

## Próximos passos (inalterados)

- **F3:** Rota como fonte única de “scene data” (ScenesToLoad/Unload/Active só na rota; LevelDefinition referencia RouteId; Navigation não duplica).
- **F2:** Decisão de Reset/WorldLifecycle por rota/policy (RouteKind/RequiresWorldReset no SceneRouteDefinition; driver usa isso).
- **F4:** LevelFlow end-to-end (StartGameplayAsync(levelId) como trilho oficial; QA/Dev usa só ele).
- **F5:** Hardening (logs [OBS] em Navigation/LevelFlow + ContextMenu QA para Start/Restart/ExitToMenu).

**Ordem recomendada permanece:** F1 → F3 → F2 → F4 → F5.
