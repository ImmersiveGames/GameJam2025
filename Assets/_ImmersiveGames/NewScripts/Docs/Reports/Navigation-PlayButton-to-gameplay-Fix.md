# Navigation Play Button (`to-gameplay`) — Root Cause & Fix

## Contexto
Erro observado ao clicar Play:

```text
[ERROR] [GameNavigationService] [Navigation] Rota desconhecida ou sem request. routeId='to-gameplay'.
```

## Causa raiz encontrada
A causa não estava no `MenuPlayButtonBinder` nem no `SceneRouteCatalog`, e sim no **build do `GameNavigationCatalogAsset`**:

- `MenuPlayButtonBinder` chama `_navigation.RestartAsync(actionReason)`.
- `RestartAsync` chama `ExecuteIntentAsync(GameNavigationIntents.ToGameplay)` com valor canônico `to-gameplay`.
- Em `ExecuteIntentAsync`, o erro é emitido quando `_catalog.TryGet(routeId, out entry)` retorna `false` **ou** `entry.IsValid == false`.
- No `GameNavigationCatalogAsset`, uma `RouteEntry` só entra no cache se:
  - `routeId` preenchido,
  - `sceneRouteId` válido,
  - `styleId` válido.

No asset `Assets/Resources/Navigation/GameNavigationCatalog.asset`, os campos serializados ainda estavam com nomes legados (`routes` e `transitionStyleId`). O código atual esperava `_routes` e `styleId`, então a hidratação podia falhar/parcialmente falhar e a rota `to-gameplay` não era construída como entry válida.

## Evidência do catálogo/resolver em runtime (DI)
`GlobalCompositionRoot.RegisterGameNavigationService()`:

- carrega via Resources:
  - `Navigation/GameNavigationCatalog`
  - `SceneFlow/SceneRouteCatalog`
  - `SceneFlow/TransitionStyleCatalog`
- registra `ISceneRouteCatalog` e cria/resolve `ISceneRouteResolver`.
- injeta `GameNavigationService` com `(sceneFlow, catalogAsset, sceneRouteResolver, styleCatalogAsset, levelCatalogAsset)`.

Foi adicionado log observável `[OBS][Navigation]` no bootstrap para registrar em runtime:
- `catalogType`
- `resolverType`
- resultado de `TryResolve('to-gameplay')`
- route efetivamente resolvida.

## Fix aplicado (mínimo e robusto)
### 1) Compatibilidade de serialização no `GameNavigationCatalogAsset`
- Adicionado `[FormerlySerializedAs("routes")]` no campo `_routes`.
- Adicionado `[FormerlySerializedAs("transitionStyleId")]` no campo `styleId` de `RouteEntry`.

Isso garante retrocompatibilidade com assets já existentes no projeto, sem mexer em fluxo de navegação, mantendo arquitetura e DI atuais.

### 2) Observabilidade de wiring (sem alterar boot flow)
- Adicionado log `[OBS][Navigation] Runtime wiring check` em `RegisterGameNavigationService()` com probe explícito de `TryResolve('to-gameplay')`.

## Checklist de validação (logs esperados)
- [ ] Ao iniciar, aparece:
  - `[OBS][Navigation] Runtime wiring check: ... tryResolve('to-gameplay')=True ...`
- [ ] Ao clicar Play:
  - aparece log verbose do binder: `Play solicitado`.
  - aparece log de navegação do service com `intentId='to-gameplay'` e `sceneRouteId='to-gameplay'`.
- [ ] **Não** aparece mais:
  - `[ERROR] [GameNavigationService] [Navigation] Rota desconhecida ou sem request. routeId='to-gameplay'.`
