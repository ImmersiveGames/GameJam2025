# Audit — SceneFlow / RouteResetPolicy (P-004, READ-ONLY)

**Data:** 2026-02-17  
**Escopo:** `Assets/_ImmersiveGames/NewScripts/**` (inspeção estática + docs)  
**Objetivo:** validar o estado do plano P-004 (RouteResetPolicy/SceneFlow/Navigation) sem alterar código.

---

## 1) Inventário de classes/serviços envolvidos

### Orquestração e DI (Composition)
- `Infrastructure/Composition/GlobalCompositionRoot.Pipeline.cs`
  - Ordem atual relevante: `RegisterSceneFlowRoutesRequired()` -> `RegisterSceneFlowNative()` -> `RegisterSceneFlowRouteResetPolicy()` -> `RegisterGameNavigationService()`.
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlowWorldLifecycle.cs`
  - `RegisterSceneFlowNative()` cria `SceneTransitionService`.
  - `RegisterSceneFlowRouteResetPolicy()` registra `IRouteResetPolicy` com `SceneRouteResetPolicy`.
  - `ResolveOrRegisterRouteResolverRequired()` exige `ISceneRouteResolver` (fail-fast se ausente).
- `Infrastructure/Composition/GlobalCompositionRoot.SceneFlowRoutes.cs`
  - `RegisterSceneFlowRoutesRequired()` valida/garante `ISceneRouteCatalog` e registra `ISceneRouteResolver`.

### Runtime SceneFlow/WorldLifecycle
- `Modules/SceneFlow/Transition/Runtime/SceneTransitionService.cs`
  - Recebe `ISceneRouteResolver` e `IRouteResetPolicy`.
  - `BuildRequestFromRouteDefinition(...)` falha em `routeId` inválido ou resolver ausente.
  - `BuildContextWithResetDecision(...)` usa `IRouteResetPolicy`; se policy ausente, aplica `requiresWorldReset=false` com `decisionSource='policy:missing'`.
- `Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs`
  - Fonte primária: `routeDefinition` direto (quando já resolvido) ou `ISceneRouteResolver`.
  - Decide reset por `RouteKind/RequiresWorldReset`.
  - Em config inválida (`routeId` inválido, rota não resolvida, `RouteKind.Unspecified`) faz fail-fast (`[FATAL][Config]`).

### Navegação (contrato e implementação)
- `Modules/Navigation/IGameNavigationService.cs`
  - Contrato principal por intent/core + wrappers legados `[Obsolete]`.
- `Modules/Navigation/GameNavigationService.cs`
  - Implementa navegação por intent e integração com SceneFlow (`SceneTransitionService`).

### Evidências/documentos relacionados
- `Docs/Plans/Plan-Continuous.md` (seção P-004: estado **IN_PROGRESS**).
- `Docs/Reports/lastlog.log` (smoke runtime com transições e `RouteExecutionPlan`).

---

## 2) Gaps vs contrato do plano P-004

Referência de contrato: seção P-004 em `Docs/Plans/Plan-Continuous.md` (objetivos + checklist + critérios de aceitação).

### Coberto (sem gap crítico observado)
1. **Contrato explícito de navegação** está presente (`IGameNavigationService` com intents core e wrappers legados isolados).
2. **Resolver obrigatório no bootstrap** foi endurecido: `SceneTransitionService` é criado com resolver exigido via `ResolveOrRegisterRouteResolverRequired()`.
3. **RouteResetPolicy prioriza rota** (definition direta/resolver), com decisão por `RouteKind` e `RequiresWorldReset`.
4. **Fail-fast de configuração inválida** em `SceneRouteResetPolicy` está coerente com política strict para config obrigatória.

### Gaps/remanescentes
1. **P-004 ainda não fechado documentalmente**: no plano canônico o checklist de execução permanece parcialmente pendente e o status está `IN_PROGRESS`.
2. **Fallback quando policy ausente** em `SceneTransitionService` (`policy:missing` -> `requiresWorldReset=false`) é seguro, mas pode mascarar wiring incompleto em ambientes não strict.
3. **Artefato datado específico de P-004** ainda é esperado no próprio plano (`Audit-SceneFlow-RouteResetPolicy.md`) — este arquivo atende esse requisito a partir desta auditoria.
4. **Redundâncias de contrato/tipos** (item explícito do checklist) não estão consolidadas como decisão final no plano.

---

## 3) Próximos passos (5–10 bullets)

1. Atualizar o bloco P-004 em `Docs/Plans/Plan-Continuous.md` com referência explícita a este audit datado.
2. Fechar (ou justificar) os itens pendentes do checklist P-004 relacionados a wiring e redundâncias.
3. Registrar evidência de runtime vinculando `lastlog.log` aos critérios de aceitação de P-004 (assinaturas e decisões de reset).
4. Tornar observável no log quando `policy:missing` ocorrer em runtime, com severidade diferenciada por modo (Strict/Release).
5. Adicionar nota de decisão no plano/ADR sobre quando o fallback `policy:missing` é aceitável.
6. Confirmar no plano se a ordem atual de DI é a ordem canônica final (evitar regressão por reorder futuro).
7. Consolidar documentação de responsabilidades entre `SceneTransitionService` e `SceneRouteResetPolicy` (fonte de decisão de reset).
8. Após fechamento dos pendentes, promover P-004 de `IN_PROGRESS` para `DONE` com evidência associada.

---

## 4) Comandos usados na auditoria

- `rg -n "Plano \(P-004\)|RouteResetPolicy|ISceneRouteResolver|SceneTransitionService|IGameNavigationService|..." ...`
- `sed -n` nos arquivos de composição, runtime e plano canônico para inspeção do estado atual.
- `rg -n "RequestMenuAsync\(|RequestGameplayAsync\(|NavigateAsync\(" Assets/_ImmersiveGames/NewScripts -g '!**/*.meta'`

> Auditoria executada em modo READ-ONLY (sem alteração de código/runtime).
