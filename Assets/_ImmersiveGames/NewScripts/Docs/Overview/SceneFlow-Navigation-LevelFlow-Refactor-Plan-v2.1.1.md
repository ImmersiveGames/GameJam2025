# SceneFlow / Navigation / LevelFlow Refactor Plan v2.1.1

> Objetivo macro (inalterado): ter pontos de configuração e modularidade para **cenas**, **transições** e **níveis**, com o mínimo de duplicação de dados e decisões explícitas (evidence-based).

## Atualização pontual (correção de nomenclatura)

- **Não existe `SceneTransitionProfileId`** no código atual.
- O identificador tipado hoje é **`SceneFlowProfileId`** (ex.: `startup`, `frontend`, `gameplay`).
- Portanto, qualquer passo do plano que mencione “profile id” deve ler como:
  - `SceneFlowProfileId` → resolve um `SceneTransitionProfile`.

---

## F1 (PRIORIDADE AGORA): Profiles sem `Resources.Load`

### Problema atual
O `SceneTransitionProfileResolver` resolve profiles via `Resources.Load(profileId.Value)`.
Isso acopla o ID a um path e obriga a manter assets em `Resources/`.

### Decisão
Introduzir um **catálogo de profiles por referência direta** (ScriptableObject), e fazer o resolver usar esse catálogo **antes** de cair no fallback legado.

### Resultado esperado
- Transições passam a usar **referência direta** para `SceneTransitionProfile` (via catálogo), sem `Resources.Load` na operação normal.
- Mantém fallback legado para não quebrar projetos que ainda estejam com profiles em `Resources/`.

### Implementação (patch incluído)
1. **Novo asset:** `SceneTransitionProfileCatalogAsset` (mapeia `SceneFlowProfileId` → `SceneTransitionProfile`).
2. **Update:** `SceneTransitionProfileResolver` tenta catálogo primeiro; se não achar, usa `Resources` (logando warning 1x por profile).
3. **Update:** `SceneFlowAdapterFactory` tenta obter o catálogo via `IDependencyProvider.TryGetGlobal<SceneTransitionProfileCatalogAsset>()` e injeta no resolver.

### Configuração (no projeto)
- Criar profiles (`SceneTransitionProfile`) normalmente (fora de `Resources/`).
- Criar 1 catálogo (`SceneTransitionProfileCatalogAsset`) e registrar:
  - entradas mínimas: `startup`, `frontend`, `gameplay`.
- Garantir que o catálogo esteja registrado como **global** no seu composition/bootstrap (mesmo padrão dos outros *CatalogAsset*).

### Evidência (logs)
Na primeira resolução de cada profile, deve aparecer:
- `Profile resolvido via catálogo ... catalog='<nome do asset>'`

---

## Próximos passos (inalterados)

- **F3:** Rota como fonte única de “scene data” (ScenesToLoad/Unload/Active só na rota; LevelDefinition referencia RouteId; Navigation não duplica).
- **F2:** Decisão de Reset/WorldLifecycle por rota/policy (RouteKind/RequiresWorldReset no SceneRouteDefinition; driver usa isso).
- **F4:** LevelFlow end-to-end (StartGameplayAsync(levelId) como trilho oficial; QA/Dev usa só ele).
- **F5:** Hardening (logs [OBS] em Navigation/LevelFlow + ContextMenu QA para Start/Restart/ExitToMenu).

**Ordem recomendada permanece:** F1 → F3 → F2 → F4 → F5.
