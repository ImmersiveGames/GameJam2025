# SceneFlow RouteCatalog Verification

Data da auditoria: 2026-02-10
Escopo: `Assets/_ImmersiveGames/NewScripts/`

## 1) Presença do catálogo e path de Resources.Load

- **Resultado:** ✅ Existe asset de catálogo de rotas com nome `SceneRouteCatalog`.
- **AssetDatabase path encontrado:** `Assets/Resources/SceneFlow/SceneRouteCatalog.asset`.
- **Está sob pasta Resources?** ✅ Sim (`Assets/Resources/...`).
- **Path de `Resources.Load` esperado no código:** `"SceneFlow/SceneRouteCatalog"`.
- **Compatibilidade path↔asset:** ✅ Correta, pois `Assets/Resources/SceneFlow/SceneRouteCatalog.asset` é carregável por `Resources.Load("SceneFlow/SceneRouteCatalog")`.

## 2) Evidência do conteúdo do catálogo (.asset/YAML)

Fonte analisada: `Assets/Resources/SceneFlow/SceneRouteCatalog.asset` (texto YAML serializado).

### 2.1 Estatísticas

- **Total de rotas:** `4`
- **Rotas com `routeKind=Unspecified`:** `4`
- **Observação técnica:** o YAML atual **não serializa** a chave `routeKind` nas entradas; portanto, o valor efetivo cai no default do enum (`Unspecified = 0`).

### 2.2 Rotas relevantes detectadas automaticamente

Heurística aplicada no `routeId` (substring): `menu`, `gameplay`, `postgame`, `restart`, `exit`.

- `to-menu` → `routeKind=Unspecified` (`targetActiveScene=MenuScene`)
- `to-gameplay` → `routeKind=Unspecified` (`targetActiveScene=GameplayScene`)

## 3) Confirmação de logs de observabilidade no código

### 3.1 Bootstrap SceneFlow via Resources

Existe log de observabilidade com a mensagem base abaixo (emitida quando catálogo/resolver são carregados via Resources):

- `"[OBS][SceneFlow] ISceneRouteCatalog/ISceneRouteResolver carregados via Resources antes do Navigation (path='SceneFlow/SceneRouteCatalog')."`

### 3.2 WorldLifecycle ResetRequested / ResetCompleted com decisionSource

Existem logs canônicos contendo `routeId` e `decisionSource`:

- `"[OBS][WorldLifecycle] ResetRequested signature='...' sourceSignature='...' routeId='...' profile='...' target='...' decisionSource='...' reason='...'."`
- `"[OBS][WorldLifecycle] ResetCompleted signature='...' routeId='...' profile='...' target='...' decisionSource='...' reason='...'."`

## 4) Checklist de logs esperados em runtime (strings exatas)

- [ ] `[OBS][SceneFlow] ISceneRouteCatalog/ISceneRouteResolver carregados via Resources antes do Navigation (path='SceneFlow/SceneRouteCatalog').`
- [ ] `[OBS][WorldLifecycle] ResetRequested signature='` *(linha contém também `routeId='` e `decisionSource='`)*
- [ ] `[OBS][WorldLifecycle] ResetCompleted signature='` *(linha contém também `routeId='` e `decisionSource='`)*

## 5) Como validar em runtime

1. **Boot do jogo**
   - Iniciar o jogo e verificar no Console o log `[OBS][SceneFlow] ... carregados via Resources ...`.
2. **Menu → Gameplay (ou Restart)**
   - Executar fluxo de transição para gameplay (ou restart) para forçar avaliação de policy de reset.
3. **Verificar decisão por RouteKind**
   - Confirmar nos logs de WorldLifecycle a presença de `decisionSource='routeKind:*'` quando o `routeKind` estiver setado na rota.

## 6) Nota DEV/QA (helper)

Foi adicionado item de contexto DEV:

- `QA/SceneFlow/Dump Route Catalog (RouteKind)` em `SceneFlowDevContextMenu`.

Comportamento:
- Tenta `Resources.Load<SceneRouteCatalogAsset>("SceneFlow/SceneRouteCatalog")`.
- Se encontrar: loga total de rotas, quantidade `Unspecified` e imprime rotas relevantes com `routeKind`.
- Se não encontrar: loga `[OBS]` de asset não encontrado via Resources.
