# NewScripts Docs

Esta pasta documenta o estado operacional atual de `Assets/_ImmersiveGames/NewScripts/**`.

## Superficie oficial vigente

### Baseline 4.0 - cadeia canonica e operacional

- Canon: [`ADR-0044`](./ADRs/ADR-0044-Baseline-4.0-Ideal-Architecture-Canon.md)
- Ancora: [`ADR-0043`](./ADRs/ADR-0043-Ancora-de-Decisao-para-o-Baseline-4.0.md)
- Alvo: [`Blueprint-Baseline-4.0-Ideal-Architecture.md`](./Plans/Blueprint-Baseline-4.0-Ideal-Architecture.md)
- Guardrail operacional: [`Plan-Baseline-4.0-Execution-Guardrails.md`](./Plans/Plan-Baseline-4.0-Execution-Guardrails.md)
- Backlog auxiliar: [`Plan-Baseline-4.0-Reorganization.md`](./Plans/Plan-Baseline-4.0-Reorganization.md)
- Auditoria de alinhamento: [`Baseline-4.0-Docs-Alignment-Audit.md`](./Reports/Audits/2026-03-29/Baseline-4.0-Docs-Alignment-Audit.md)

Regra:
- o Baseline 4.0 deve ser lido por essa cadeia, e nao pelo estado atual do runtime como contrato final
- `Canon Index` e indice de estado atual/historico; nao e autoridade canonica do Baseline 4.0

Rodada estrutural consolidada:
- `ADR-0038` fecha o pipeline modular em duas fases canonicas, com `Gameplay` installer-only, `Audio` como modulo canonico e `Loading` como subcapability de `SceneFlow`.
- `ADR-0039` fecha o contrato minimo de `Pause`, com `GameLoop` como owner de `Paused`, hooks oficiais e overlay reativo.
- `ADR-0008`, `ADR-0028` e `ADR-0007` fecham `RuntimeModeConfig`, `Audio` e `InputModes` no estado validado.

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
10. `Docs/Modules/PostGame.md`
11. `Docs/Modules/Gameplay.md`
12. `Docs/Modules/WorldLifecycle.md`
13. `Docs/Modules/InputModes.md`
14. `Docs/ADRs/README.md`
15. `Docs/Reports/Audits/LATEST.md`
16. `Docs/Reports/Evidence/LATEST.md`
17. `Docs/CHANGELOG.md`

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
- `IntroStage` e level-owned, opcional, disparada pelo hook `LevelEnteredEvent` e finalizada por `LevelIntroCompletedEvent`.
- O presenter canonico da intro e resolvido por `ILevelIntroStagePresenterRegistry` + `ILevelIntroStagePresenterScopeResolver`.
- `PostGame` e global no runtime atual, com `PostStage` implementado e validado.
- `Audio` esta no pipeline modular canonico via `AudioCompositionDescriptor`, `AudioInstaller` e `AudioRuntimeComposer`.
- Slice 4 do Baseline 4.0 esta fechado documentalmente: `Navigation primary dispatch -> Audio contextual reactions`, com BGM contextual, ducking e rail semantico de entidade validados.
- Slice 5 do Baseline 4.0 foi aberto como corte curto de `SceneFlow`, focado em trilho tecnico, readiness, loading e fade.
- A Fase 2 do Slice 5 foi fechada apos saneamento final de readiness/loading; o proximo passo segue sendo a Fase 3 com bridges temporarias.
- `RuntimeModeConfig` e obrigatorio no `BootstrapConfigAsset` e e resolvido por referencia direta.
- `InputModes` usa `InputModeRequestKind`, `IInputModeStateService` e `IPlayerInputLocator`.
- O contrato oficial de `PostStage` esta em `Docs/ADRs/ADR-0012-Fluxo-Pos-Gameplay-GameOver-Vitoria-Restart.md`.
- Default operacional: ausencia de presenter implica `PostStageSkipped reason='PostStage/NoPresenter'`.
- Levels com presenter explicito executam `PostStage` real e validam GUI minima.
- `Restart` nao passa por post hook.
- O level atual pode expor apenas um hook opcional para complementar a resposta ao resultado.
- `ActorGroupRearm` e a nomenclatura canonica de rearm local de gameplay.
- `Victory/Defeat` fazem parte do baseline atual por mock explicito e controlado.
