# NewScripts Docs

Esta pasta documenta o estado operacional atual de `Assets/_ImmersiveGames/NewScripts/**`.

## Superficie oficial vigente

Leia nesta ordem:
1. `Docs/Canon/Canon-Index.md`
2. `Docs/Guides/Production-How-To-Use-Core-Modules.md`
3. `Docs/Guides/Pooling-How-To.md`
4. `Docs/Guides/Pooling-Quick-Access.html`
5. `Docs/Guides/Event-Hooks-Reference.md`
6. `Docs/Modules/SceneFlow.md`
7. `Docs/Modules/Navigation.md`
8. `Docs/Modules/LevelFlow.md`
9. `Docs/Modules/GameLoop.md`
10. `Docs/Modules/Gameplay.md`
11. `Docs/Modules/WorldLifecycle.md`
12. `Docs/ADRs/README.md`
13. `Docs/Reports/Audits/LATEST.md`
14. `Docs/Reports/Evidence/LATEST.md`
15. `Docs/CHANGELOG.md`

## Guias de uso

Fonte canonica em Markdown:
- `Docs/Guides/Production-How-To-Use-Core-Modules.md`
- `Docs/Guides/Pooling-How-To.md`
- `Docs/Guides/Event-Hooks-Reference.md`

Camada visual completa em HTML:
- `Docs/Guides/Manual-Operacional.html`
- `Docs/Guides/Pooling-Quick-Access.html`
- `Docs/Guides/Hooks-Reference.html`

Regra:
- o Markdown continua sendo a fonte canonica
- o HTML replica o conteudo operacional principal em formato visual, sem trocar a fonte de verdade
- o loading de producao aparece nessa mesma cadeia principal: guia canonico + modulo de `SceneFlow` + camada visual HTML

## Estado atual resumido

- Baseline V3 vigente fechado em `PASS`.
- A referencia canonica atual de auditoria e `Docs/Reports/Audits/2026-03-19/Audit-NewScripts-Canonical-Cleanup-Round1.md`.
- A evidencia vigente e consolidada em `Docs/Reports/Evidence/LATEST.md`.
- `startup` pertence ao bootstrap.
- `frontend` e `gameplay` pertencem a `SceneRouteKind`.
- Navigation e transition operam por direct-ref + fail-fast.
- `GameNavigationCatalogAsset` e o asset canonico de navigation.
- `TransitionStyleAsset` e o asset canonico de style.
- `SceneTransitionProfile` e asset leaf visual.
- `LoadingHudScene` e a HUD canonica de loading do macro flow.
- `ILoadingPresentationService` e `LoadingHudService` cuidam apenas da apresentacao de loading.
- `IntroStage` e level-owned e opcional.
- `PostGame` e global, com resultados formais `Victory`, `Defeat` e `Exit`.
- `Restart` nao passa por post hook.
- O level atual pode expor apenas um hook opcional para complementar o `PostGame` global.
- `ActorGroupRearm` e a nomenclatura canonica de rearm local de gameplay.
- `InputModes`, `SimulationGate` e `SceneComposition` pertencem a `Infrastructure`, nao a `Modules`.
- `Victory/Defeat` fazem parte do baseline atual por mock explicito e controlado.

