# Audit — SceneFlow / Navigation / RouteResetPolicy (P-004)

- Data/Hora (UTC): **2026-02-18 00:23:42 UTC**
- Escopo: `Assets/_ImmersiveGames/NewScripts/**` (inspeção estática read-only + evidência documental)
- Modo: **AUDITORIA + DOCS ONLY**
- Objetivo: revalidar P-004 no estado atual (pós DataCleanup v1) sem alterar runtime/editor code.

---

## Comandos usados

```bash
rg -n "RegisterSceneFlowNative|RegisterSceneFlowRouteResetPolicy|RegisterGameNavigationService|RegisterSceneFlowRoutesRequired|SceneTransitionService|ResolveOrRegisterRouteResolver|ISceneRouteResolver|ISceneRouteCatalog|Resources.Load|SceneRouteResetPolicy|RouteKind|RequiresWorldReset|policy:missing|routePolicy" Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle Assets/_ImmersiveGames/NewScripts/Modules/Navigation -g '*.cs'

nl -ba Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs | sed -n '1,220p'
nl -ba Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlowRoutes.cs | sed -n '1,260p'
nl -ba Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlowWorldLifecycle.cs | sed -n '1,220p'
nl -ba Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs | sed -n '240,420p'
nl -ba Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs | sed -n '1,260p'
nl -ba Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs | sed -n '1,220p'

rg -n "ResetPolicy|policy:missing" Assets/_ImmersiveGames/NewScripts/Docs/Reports/lastlog.log
rg -n "SceneFlow Config Validation Report|VERDICT:" Assets/_ImmersiveGames/NewScripts/Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md
```

Arquivos inspecionados:
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlowRoutes.cs`
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlowWorldLifecycle.cs`
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
- `Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs`
- `Modules/WorldLifecycle/Runtime/WorldLifecycleSceneFlowResetDriver.cs`
- `Docs/Reports/lastlog.log`
- `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md`

---

## Achados

### A) Ordem de DI/pipeline: ainda existe cenário com `SceneTransitionService` sem `ISceneRouteResolver`?

**Conclusão:** **foi resolvido no pipeline canônico atual (fail-fast, sem resolver nulo no caminho esperado)**.

Evidências:
1. Pipeline registra rotas obrigatórias **antes** de SceneFlow native:
   - `RegisterSceneFlowRoutesRequired()` vem antes de `RegisterSceneFlowNative()` no pipeline principal.
2. `RegisterSceneFlowNative()` resolve resolver via método obrigatório:
   - `var routeResolver = ResolveOrRegisterRouteResolverRequired();`
   - instanciação de `SceneTransitionService(..., routeResolver, ...)` com resolver já resolvido.
3. Se resolver não existir, há `InvalidOperationException` (fail-fast) em `ResolveOrRegisterRouteResolverRequired()`.
4. `RegisterSceneFlowRoutesRequired()` registra `ISceneRouteCatalog` via bootstrap obrigatório e cria `ISceneRouteResolver` (`SceneRouteCatalogResolver`) se ausente.

Trechos curtos relevantes:
- Pipeline order:
  - `RegisterSceneFlowRoutesRequired();`
  - `RegisterSceneFlowNative();`
- Fail-fast do resolver:
  - `throw new InvalidOperationException("[SceneFlow] ISceneRouteResolver obrigatório ausente...`)`

---

### B) `SceneRouteResetPolicy`: decisão primária por rota (`RouteKind`/`RequiresWorldReset`) + fallback

**Conclusão:** **PASS** — a policy decide reset com fonte primária em rota; fallback por profile não está na policy, ocorre somente no `SceneTransitionService` quando policy está ausente.

Evidências de implementação:
1. `SceneRouteResetPolicy.Resolve(...)`:
   - valida `routeId`;
   - resolve definição via `routeDefinition` direto (quando já fornecido) ou `_routeResolver.TryResolve(...)`;
   - falha em config inválida (`RouteKind.Unspecified`) com `[FATAL][Config]`;
   - retorna `RouteResetDecision(shouldReset: resolvedDefinition.RequiresWorldReset, decisionSource: "routePolicy:<RouteKind>", reason: "RoutePolicy")`.
2. `SceneTransitionService.BuildContextWithResetDecision(...)`:
   - se não conseguir policy: usa fallback `requiresWorldReset=false`, `decisionSource="policy:missing"`;
   - com policy presente: aplica decisão retornada por `IRouteResetPolicy`.
3. Logs observáveis no smoke (`lastlog.log`):
   - `decisionSource='routePolicy:Frontend'` para `routeId='to-menu'`.
   - `decisionSource='routePolicy:Gameplay'` para `routeId='level.1'`.

Logs [OBS]/[FATAL] relevantes:
- [OBS] `ResetPolicy routeId='to-menu' ... decisionSource='routePolicy:Frontend'`
- [OBS] `ResetPolicy routeId='level.1' ... decisionSource='routePolicy:Gameplay'`
- [FATAL][Config] (na policy) para:
  - `routeId` inválido,
  - rota não resolvida,
  - `RouteKind.Unspecified`.

---

### C) Regressão proibida: ainda existe fallback por `Resources.Load` para resolver catálogo/resolver?

**Conclusão:** **não encontrado no caminho canônico de composição SceneFlow/Routes/WorldLifecycle**.

Evidências:
1. Em `GlobalCompositionRoot.SceneFlowRoutes.cs`, a origem do catálogo é `NewScriptsBootstrapConfigAsset.sceneRouteCatalog` (fail-fast se ausente), sem `Resources.Load`.
2. Busca por `Resources.Load` no escopo de composição (`Infrastructure/Composition`) não retornou fallback para `SceneRouteCatalog`/resolver.
3. Registro de `ISceneRouteCatalog` e `ISceneRouteResolver` é feito via DI (`RegisterGlobal`) com objeto resolvido do bootstrap.

Observação:
- Existe fallback defensivo `policy:missing` em `SceneTransitionService` caso `IRouteResetPolicy` não esteja disponível em runtime; porém no smoke atual há evidência de policy ativa e decisões `routePolicy:*`.

---

## Status vs Critérios de Aceitação (P-004)

Critérios (P-004) avaliados no estado atual:

1. **`SceneTransitionService` não deve nascer sem resolver no pipeline canônico**
   - **PASS** (ordem + fail-fast + resolver obrigatório).
2. **`SceneRouteResetPolicy` decide por rota (kind/requiresWorldReset)**
   - **PASS** (implementação direta + logs `routePolicy:Frontend/Gameplay`).
3. **Sem regressão para fallback de catálogo por `Resources.Load` no caminho canônico**
   - **PASS** (catálogo via bootstrap config + DI).
4. **Evidência runtime e validator DataCleanup v1**
   - **PASS** (`lastlog.log` contém âncoras de reset policy; validator report em `VERDICT: PASS`).

**Veredito geral da auditoria:** **PASS**.

---

## Ações recomendadas

Como todos os critérios acima estão em PASS:

1. **Manter P-004 em DONE** nos planos canônicos (`Plan-Continuous` e plano dedicado P-004).
2. **Manter links de evidência sincronizados** para:
   - `Docs/Reports/lastlog.log` (âncoras `routePolicy:*`),
   - `Docs/Reports/SceneFlow-Config-ValidationReport-DataCleanup-v1.md` (`VERDICT: PASS`),
   - este audit datado.
3. **Monitorar regressão operacional**: em novos smokes, verificar explicitamente ausência de `policy:missing` e presença de `routePolicy:Frontend/Gameplay`.

> Esta auditoria substitui leituras anteriores que marcavam P-004 como `IN_PROGRESS` por snapshot histórico.
