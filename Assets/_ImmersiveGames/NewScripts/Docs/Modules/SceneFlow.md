# SceneFlow

## Estado atual

- `SceneTransitionService` e o owner da timeline de transicao.
- `startup` pertence ao bootstrap por `NewScriptsBootstrapConfigAsset.startupTransitionStyleRef`.
- `frontend` e `gameplay` pertencem a `SceneRouteKind`.
- Navigation e transition operam em direct-ref + fail-fast.
- `GameNavigationCatalogAsset` resolve `routeRef + transitionStyleRef`.
- `TransitionStyleAsset` resolve `profileRef + useFade`.
- `SceneTransitionProfile` permanece asset leaf visual.
- `LoadingHudScene` e a HUD canonica do macro flow.

## Ownership

- `SceneTransitionService`: fases da transicao e timeline.
- `SceneRouteCatalogAsset` + `SceneRouteDefinitionAsset`: definicao de rota, `RouteKind`, target scene e reset policy.
- `TransitionStyleAsset`: style estrutural da transicao.
- `SceneFlowFadeAdapter`: aplicacao do style no fade.
- `WorldLifecycleResetCompletionGate` (em `ResetInterop`) e `MacroLevelPrepareCompletionGate`: gates do pipeline.
- `ILoadingPresentationService` + `LoadingHudService`: apresentacao visual de loading.
- `LoadingHudOrchestrator` + `LoadingProgressOrchestrator`: ponte entre pipeline e HUD, sem ownership da transicao.

## Regras praticas

- Nao existe semantica de fluxo em style ou profile.
- `startup` nao passa por navigation.
- Rota `Gameplay` exige reset macro e `LevelCollection` valida.
- Rota `Frontend` nao pode exigir reset de mundo nem carregar `LevelCollection`.
- A HUD de loading cobre `startup`, `menu -> gameplay`, `gameplay -> menu` e `restart macro`.
- O loading entra como apresentacao; o pipeline continua em `SceneFlow + WorldReset + ResetInterop + LevelFlow`.

## Loading no SceneFlow atual

### Owner do fluxo vs owner da apresentacao

- owner do fluxo:
  - `SceneTransitionService`
  - `WorldLifecycle`
  - `LevelFlow`
- owner da apresentacao:
  - `ILoadingPresentationService`
  - `LoadingHudService`
  - `LoadingHudScene`

Leitura pratica:
- quem decide a transicao continua sendo o pipeline macro
- quem desenha a HUD e o servico de loading

### Papel de `ILoadingPresentationService`

O contrato atual existe para:
- garantir que a `LoadingHudScene` esteja pronta
- mostrar e esconder a HUD
- atualizar mensagem
- aplicar `LoadingProgressSnapshot`

Ele nao existe para:
- decidir rota
- decidir reset
- decidir prepare de level

### Ordem atual com fade e loading

Com fade:
1. a transicao macro comeca
2. o fade cobre a tela
3. a HUD de loading aparece
4. o pipeline roda
5. o progresso e atualizado
6. a HUD fecha no fim real
7. o fade revela

Sem fade:
1. a transicao macro comeca
2. a HUD aparece no inicio
3. o pipeline roda
4. o progresso fecha em `100%`
5. a HUD some no fim real

### Como o progresso entra

- progresso real:
  - `SceneTransitionService` reporta progresso de load e unload de cena por `SceneFlowRouteLoadingProgressEvent`
- progresso por marcos:
  - `LoadingProgressOrchestrator` fecha reset, prepare, readiness e finalizacao com pesos atuais

O resultado visivel na HUD:
- barra
- porcentagem
- etapa atual
- spinner

## Leitura cruzada

- `Docs/Modules/Navigation.md`
- `Docs/Modules/LevelFlow.md`
- `Docs/Modules/WorldReset.md`
- `Docs/Modules/SceneReset.md`
- `Docs/Modules/ResetInterop.md`
