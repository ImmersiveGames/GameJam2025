# ADR-0070 - Rail canonico de rotas, loading, fade e handoff do Base11Sandbox

## Status
- Estado: Accepted
- Data: 2026-05-07
- Tipo: Direction / Frozen checkpoint
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

O Base11Sandbox ja validou o ciclo minimo de rotas, loading, fade e handoff sem depender do rail legado de SceneFlow como owner do fluxo canonico.

A auditoria e os smokes aprovados mostraram que:

- `RuntimeModeConfig` e a entrada canonica de configuracao do modo;
- `RuntimePersistentScenesPolicyAsset` declara as cenas persistentes do modo;
- `SessionOperationalRouteAsset` descreve a rota operacional sem usar string serializada de cena;
- `SessionOperationalPipeline` decide lifecycle, completion e handoff;
- `Base11SandboxSessionOperationalLoadingAdapter` executa a UI de loading;
- `Base11SandboxSessionOperationalFadeAdapter` executa o fade;
- `Base11SandboxOperationalRouteTransitionAdapter` executa a aplicacao fisica da rota;
- `SceneCompositionExecutor` executa os side-effects de cena;
- `LoadingHudOrchestrator`, `LoadingProgressOrchestrator` e `LoadingHudService` legados sairam do caminho ativo;
- `SessionOperationalRouteTransitionBridge` saiu do caminho ativo;
- `SceneFlowBootstrap`, `SceneFlowInstaller`, `SceneTransitionService` e os seams legados de fade/navigation permanecem apenas como legado isolado ou seam tecnico nao canonico.

## Decisao

Congela-se o rail canonico do Base11Sandbox como:

```text
RuntimeModeConfig
-> RuntimePersistentScenesPolicyAsset
-> SessionOperationalRouteAsset
-> SessionOperationalPipeline
-> Base11SandboxSessionOperationalLoadingAdapter
-> Base11SandboxSessionOperationalFadeAdapter
-> Base11SandboxOperationalRouteTransitionAdapter
-> SceneCompositionExecutor
```

Regras canonicas:

1. `RuntimeModeConfig` e a entrada de configuracao do modo.
2. `BootstrapConfigAsset` pode existir apenas como compatibilidade/legado fora do rail novo.
3. `RuntimePersistentScenesPolicyAsset` declara cenas persistentes; persistent scenes nao sao route-owned.
4. `UIGlobalScene`, `FadeScene` e `LoadingHudScene` sao persistent scenes do modo.
5. `SessionOperationalRouteAsset` usa `SceneKeyAsset`, nao string serializada de cena.
6. `ActiveSceneKey` e obrigatorio e entra implicitamente como primeira cena a carregar.
7. `ScenesToLoad` representa apenas cenas adicionais da rota.
8. `ScenesToUnload` permanece apenas para excecoes explicitas.
9. `UnloadPreviousRouteOwnedScenes` descarrega somente cenas owned pela ultima rota operacional concluida.
10. `SessionOperationalPipeline` decide quando loading, fade e handoff acontecem.
11. `Base11SandboxSessionOperationalLoadingAdapter` executa a UI/progresso do loading.
12. `LoadingHudController` e executor visual puro.
13. `Base11SandboxSessionOperationalFadeAdapter` executa o side-effect de fade.
14. `SceneTransitionProfile` e referenciado diretamente pela rota; `TransitionStyleAsset` nao participa do rail canônico novo.
15. `SceneTransitionService` nao e owner do fade/loading no Base11Sandbox.
16. O rail canonico deve falhar cedo quando houver conflito entre persistent scene obrigatoria e rota.

## Invariantes

- Nao criar Base 2.0.
- Nao reorganizar fisicamente a arquitetura em Core/Concrete/UnityAdapter.
- Nao criar compat paralelo.
- Nao criar fallback silencioso.
- Nao permitir que rotas carreguem, descarreguem ou ativem persistent scenes.
- Nao usar `BootstrapConfigAsset` como fonte canonica do Base11Sandbox.
- Nao reativar `SceneFlow` legado como owner do loading/fade.
- `SceneFlow` legado pode existir apenas como legado isolado fora do rail canônico.
- `LoadingHudOrchestrator`, `LoadingProgressOrchestrator` e `LoadingHudService` nao participam do rail novo.

## Ordem canonica congelada

```text
RuntimePersistentScenes guaranteed
-> LoadingPlanReady
-> OperationalRouteCommand
-> TransitionPlanReady
-> LoadingStarted/showCompleted
-> fadeInStarted/fadeInCompleted, se transition ativa
-> SceneComposition
-> SceneCompositionCompleted
-> MaterializationCompleted
-> progress=1
-> progressVisualSettled
-> finalProgressHoldStarted/finalProgressHoldCompleted
-> LoadingCompleted
-> hideStarted/hideCompleted
-> LoadingHidden
-> fadeOutStarted/fadeOutCompleted, se transition ativa
-> OperationalRouteCompleted
-> SessionActivityEntryHandoffEmitted, se houver
```

## Evidencia de smoke

- `RuntimePersistentScenes` carrega `UIGlobalScene`, `FadeScene` e `LoadingHudScene`.
- `Route_MenuToSessionActivitySandbox` usa `LoadingMode=Profile` e `TransitionMode=Profile`.
- `LoadingHidden` ocorre antes de `fadeOutStarted`.
- `fadeOutCompleted` ocorre antes de `OperationalRouteCompleted`.
- `SessionActivityEntryHandoffEmitted` ocorre depois de `OperationalRouteCompleted`.
- `SessionActivityMiniFlowHost` pode inicializar durante o load, mas `autoStart=False` e `stage Unknown` nao significam inicio da activity.

## Fora do escopo do rail canonico

- `LoadingHudOrchestrator`, `LoadingProgressOrchestrator` e `LoadingHudService` legados foram removidos do caminho ativo;
- `SessionOperationalRouteTransitionBridge` foi removido;
- o startup/navigation operacional legado foi removido do caminho ativo;
- `SceneFlow` transition/fade/navigation legado pode existir como legado isolado, mas nao e owner do Base11Sandbox;
- a UI/QA local da `SessionActivitySandboxScene` pode aparecer cedo sem quebrar o rail canonico;
- o warning legado `routeId='to-menu'` com `PhaseDefinitionCatalog` ainda existe como residuo de configuracao legado;
- o segundo corte do grafo legado `SceneFlow/Navigation` ainda esta pendente;
- `RouteActorSetRefContext` deve ser extraido de `SceneFlowInstaller` antes da remocao final de `SceneFlowInstaller`/`SceneFlowBootstrap`.

## Consequencias

- O rail do Base11Sandbox fica congelado e rastreavel.
- O loading/fade nao volta a depender do owner legado.
- O `SessionActivityPipeline` continua comecando somente apos `SessionActivityEntryHandoff`.
- Mudancas futuras no rail devem ser tratadas como alteracao deliberada do checkpoint, nao como compatibilidade local.

## ADRs historicos relacionados

- `ADR-0060`
- `ADR-0061`
- `ADR-0062`
- `ADR-0063`
- `ADR-0064`
- `ADR-0065`
- `ADR-0066`
- `ADR-0067`
- `ADR-0068`
- `ADR-0069`
