# Codex Validation Plan — SceneFlow / Navigation / RouteResetPolicy

## Contexto

Projeto Unity 6 (multiplayer local), escopo em `Assets/_ImmersiveGames/NewScripts/`.

Objetivo desta rodada:
1. Validar migração dos call-sites para o contrato explícito de `IGameNavigationService`.
2. Corrigir wiring de `SceneTransitionService` para evitar `ISceneRouteResolver` ausente no momento da criação.
3. Garantir que `SceneRouteResetPolicy` priorize resolução por rota via injeção e use fallback por profile apenas quando necessário.
4. Verificar redundâncias de contratos/classes e organização modular correta (sem tocar em `Scripts/` legado).

## Diagnóstico inicial (inventário)

### 1) Contrato de navegação
- `IGameNavigationService` **já expõe**:
  - `GoToMenuAsync(reason)`
  - `RestartAsync(reason)`
  - `ExitToMenuAsync(reason)`
  - `StartGameplayAsync(levelId, reason)`
- Métodos legados `[Obsolete]` **preservados**:
  - `NavigateAsync(routeId, reason)`
  - `RequestMenuAsync(reason)`
  - `RequestGameplayAsync(reason)`

### 2) Varredura de call-sites legados
Varredura em `NewScripts` (excluindo docs):
- **Nenhum call-site ativo** usando `RequestMenuAsync`, `RequestGameplayAsync` ou `NavigateAsync`.
- Ocorrências encontradas estão somente na própria interface/implementação para compatibilidade.

### 3) Candidatos de UI/bridge/dev
- `MenuPlayButtonBinder`: usa `RestartAsync(reason)` (sem `LevelId` explícito disponível no binder).
- `RestartNavigationBridge`: usa `RestartAsync(reason)`.
- `ExitToMenuNavigationBridge`: usa `ExitToMenuAsync(reason)`.
- `SceneFlowDevContextMenu`: usa `RestartAsync(reason)`.

### 4) Wiring DI atual (problema identificado)
Ordem atual em `GlobalCompositionRoot.Pipeline`:
1. `RegisterSceneFlowNative()`
2. `RegisterSceneFlowRouteResetPolicy()`
3. `RegisterGameNavigationService()`

Pontos críticos:
- `RegisterSceneFlowNative()` cria `SceneTransitionService` com `ISceneRouteResolver` via `TryGetGlobal`.
- `ISceneRouteResolver` normalmente só é registrado em `RegisterGameNavigationService()` (via `ResolveOrRegisterSceneRouteResolver(...)`).
- Resultado: há caminho de bootstrap em que `SceneTransitionService` nasce com `routeResolver = null`.

## Checklist de validação

- [x] Confirmar contrato de `IGameNavigationService` e wrappers legados `[Obsolete]`.
- [x] Auditar call-sites de APIs legadas (`RequestMenuAsync`, `RequestGameplayAsync`, `NavigateAsync`).
- [x] Verificar binders/bridges/dev menus principais.
- [ ] Ajustar wiring para registrar/obter `ISceneRouteResolver` antes (ou durante) criação do `SceneTransitionService`.
- [ ] Atualizar `SceneRouteResetPolicy` para preferir `ISceneRouteResolver` injetado e fallback por profile quando ausente.
- [ ] Auditar redundâncias de tipos (resolver/guard/reset policy/kinds) e consolidar se necessário.
- [ ] Sanity check estático (referências/namespace/assinaturas).

## Lista prevista de arquivos a tocar

- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlowWorldLifecycle.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/WorldLifecycle/Runtime/SceneRouteResetPolicy.cs`
- `Assets/_ImmersiveGames/NewScripts/Docs/Plans/Codex-Validation-SceneFlow-RouteResetPolicy.md`

> Observação: não há alteração planejada em `Scripts/` legado.

## Critérios de aceitação

1. Compilação sem erros de referência/assinatura (checagem estática das mudanças).
2. `SceneTransitionService` recebe `ISceneRouteResolver` válido quando existir catálogo/registro no DI.
3. Quando não houver catálogo/resolver, comportamento continua seguro com log `[OBS]` explícito e fallback preservado.
4. `SceneRouteResetPolicy` usa rota (via resolver) como fonte primária e profile como fallback.
5. Logs/contratos canônicos de observabilidade permanecem coerentes.


## Follow-up wiring fix

A primeira versão mitigava o problema com logs `[OBS]`, mas não se curava no boot padrão: quando `RegisterSceneFlowNative()` rodava antes de `RegisterGameNavigationService()`, o `ISceneRouteCatalog` ainda não estava no DI e o resolver continuava `null`.

Para fechar o gap de ordem do pipeline sem refactor amplo:
- `ResolveOrRegisterRouteResolverBestEffort()` agora tenta `Resources.Load<SceneRouteCatalogAsset>("SceneFlow/SceneRouteCatalog")` quando DI ainda não tem catálogo.
- Ao encontrar o asset, registra `ISceneRouteCatalog` e `ISceneRouteResolver` imediatamente (antes da navegação).
- Se não encontrar, mantém fallback backward-compatible com log `[OBS]` explícito.

Com isso, o SceneFlow consegue hidratar payload por rota já no bootstrap normal, e a `SceneRouteResetPolicy` consegue decidir por `RouteKind` na primeira transição quando o catálogo estiver disponível.
