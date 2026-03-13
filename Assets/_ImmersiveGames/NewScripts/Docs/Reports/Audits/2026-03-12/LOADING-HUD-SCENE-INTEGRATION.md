# Loading HUD Scene Integration

## Estado final
- `LoadingHudScene` segue como camada de apresentacao visual aditiva.
- `SceneFlow`, `WorldLifecycle` e `LevelFlow` continuam owners do fluxo.
- A apresentacao canonica agora passa por `ILoadingPresentationService`.
- `ILoadingHudService` foi mantido como contrato de compatibilidade interna para o orchestrator atual.

## Como a cena foi integrada
- O servico canonico garante a `LoadingHudScene` via carregamento aditivo e nao a promove para active scene.
- Depois do primeiro carregamento, a cena fica residente e o servico apenas faz `Show/Hide`.
- A resolucao do root deixou de ser global por `FindAnyObjectByType` e passou a acontecer dentro da propria `LoadingHudScene`.
- O root visual obrigatorio continua sendo o GameObject raiz que contem `LoadingHudController`.
- `Canvas` e `CanvasGroup` passaram a ser obrigatorios no root e falham explicitamente se estiverem ausentes.
- `TMP_Text` e spinner seguem opcionais como elementos visuais complementares.

## Owner do fluxo vs owner da apresentacao
- Owner do fluxo:
  - `SceneTransitionService`
  - gates de `WorldLifecycle`
  - prepare de `LevelFlow`
  - coordenadores de navigation e restart
- Owner da apresentacao:
  - `LoadingHudService` como implementacao de `ILoadingPresentationService`
  - `LoadingHudOrchestrator` apenas sincroniza visibilidade com os eventos do SceneFlow

## Transicoes macro cobertas
- bootstrap startup
- menu -> gameplay
- gameplay -> menu
- restart macro

## Arquivos modificados
- `Assets/_ImmersiveGames/NewScripts/Infrastructure/Composition/GlobalCompositionRoot.SceneFlow.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Bindings/LoadingHudController.cs`
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/LoadingHudService.cs`

## Arquivos criados
- `Assets/_ImmersiveGames/NewScripts/Modules/SceneFlow/Loading/Runtime/ILoadingPresentationService.cs`
- `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Audits/2026-03-12/LOADING-HUD-SCENE-INTEGRATION.md`

## Pendencias
- Nenhuma mudanca de cena foi feita nesta rodada porque o escopo ficou restrito a `NewScripts/**`.
- A cena existente ja atende ao contrato minimo atual porque o root resolve `Canvas`, `CanvasGroup` e `TMP_Text` a partir do binding configurado.
