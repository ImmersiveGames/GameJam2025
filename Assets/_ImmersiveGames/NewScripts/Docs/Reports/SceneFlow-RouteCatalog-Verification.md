# SceneFlow RouteCatalog Verification

Data da auditoria: 2026-02-10  
Escopo: `Assets/_ImmersiveGames/NewScripts/`

## 1) Presença do catálogo e path de Resources.Load

- **Resultado:** ✅ Existe asset de catálogo de rotas com nome `SceneRouteCatalog`.
- **AssetDatabase path encontrado:** `Assets/Resources/SceneFlow/SceneRouteCatalog.asset`.
- **Está sob pasta Resources?** ✅ Sim (`Assets/Resources/...`).
- **Path usado no código:** `Resources.Load<SceneRouteCatalogAsset>("SceneFlow/SceneRouteCatalog")`.
- **Compatibilidade path↔asset:** ✅ Correta.

## 2) Before (estado anterior)

Fonte auditada (antes): `SceneRouteCatalog.asset` sem chave `routeKind` serializada nas entries.

- **totalRoutes:** `4`
- **unspecifiedRoutes:** `4`
- **unspecifiedRelevantRoutes:** `2` (`to-menu`, `to-gameplay`)

## 3) After: RouteKind serialized

O asset foi atualizado para serializar `routeKind` explicitamente em todas as rotas:

- `to-menu` → `Frontend (1)`
- `to-gameplay` → `Gameplay (2)`
- `level.1` → `Gameplay (2)`
- `level.2` → `Gameplay (2)`

### 3.1 Estatísticas atualizadas

- **totalRoutes:** `4`
- **unspecifiedRoutes:** `0`
- **unspecifiedRelevantRoutes:** `0`

### 3.2 Tabela routeId -> routeKind

| routeId      | routeKind (enum) | routeKind (valor) |
|--------------|------------------|-------------------|
| to-menu      | Frontend         | 1 |
| to-gameplay  | Gameplay         | 2 |
| level.1      | Gameplay         | 2 |
| level.2      | Gameplay         | 2 |

## 4) Confirmação de observabilidade no código

### 4.1 Bootstrap SceneFlow via Resources

Mensagem base existente no código:

- `"[OBS][SceneFlow] ISceneRouteCatalog/ISceneRouteResolver carregados via Resources antes do Navigation (path='SceneFlow/SceneRouteCatalog')."`

### 4.2 WorldLifecycle ResetRequested / ResetCompleted

Mensagens canônicas existentes (com `routeId` + `decisionSource`):

- `"[OBS][WorldLifecycle] ResetRequested ... routeId='...' ... decisionSource='...' ..."`
- `"[OBS][WorldLifecycle] ResetCompleted ... routeId='...' ... decisionSource='...' ..."`

## 5) Impacto esperado após serializar RouteKind

- Quando a transição usar `routeId` válido com entrada no catálogo, a política (`SceneRouteResetPolicy`) poderá produzir `decisionSource='routeKind:Frontend'` ou `decisionSource='routeKind:Gameplay'`.
- Se a transição não carregar `routeId` (ou não resolver rota), o comportamento continua em fallback (`decisionSource='profile:fallback'`).

## 6) Checklist de logs esperados em runtime

- [ ] `[OBS][SceneFlow] ISceneRouteCatalog/ISceneRouteResolver carregados via Resources antes do Navigation (path='SceneFlow/SceneRouteCatalog').`
- [ ] `[OBS][WorldLifecycle] ResetRequested ... routeId='...' ... decisionSource='routeKind:*' ...`
- [ ] `[OBS][WorldLifecycle] ResetCompleted ... routeId='...' ... decisionSource='routeKind:*' ...`

## 7) Como validar em runtime

1. **Boot do jogo**
   - Confirmar no Console o log `[OBS][SceneFlow] ... carregados via Resources ...`.
2. **Menu → Gameplay (ou Restart)**
   - Executar transição com rota configurada no catálogo.
3. **Verificar decisão por RouteKind**
   - Confirmar logs com `decisionSource='routeKind:Frontend|Gameplay'` quando a rota usada tiver `routeKind` definido.

## 8) Nota DEV/QA (helper)

Item existente: `QA/SceneFlow/Dump Route Catalog (RouteKind)` em `SceneFlowDevContextMenu`.

Comportamento:
- Tenta `Resources.Load<SceneRouteCatalogAsset>("SceneFlow/SceneRouteCatalog")`.
- Se encontrar: loga total de rotas, quantidade `Unspecified` e rotas relevantes com `routeKind`.
- Se houver rota relevante ainda em `Unspecified`, loga `[OBS]` com a lista dessas rotas.
- Se não encontrar: loga `[OBS]` de asset não encontrado via Resources.
