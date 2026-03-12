# NewScripts Docs

Esta pasta documenta apenas o estado operacional atual de `Assets/_ImmersiveGames/NewScripts/**`.

Estados intermediarios, trilhos de migracao e snapshots antigos nao sao mais documentacao operacional vigente. O historico que permanece existe apenas para rastreabilidade em:
- `Docs/CHANGELOG.md`
- `Docs/ADRs/**` vigentes
- `Docs/Reports/Audits/2026-03-12/DOCS-CURRENT-STATE-CLEANUP.md`

## Superficie canonica

Leia nesta ordem:
1. `Docs/Canon/Canon-Index.md`
2. `Docs/Modules/SceneFlow.md`
3. `Docs/Modules/Navigation.md`
4. `Docs/Modules/LevelFlow.md`
5. `Docs/Modules/GameLoop.md`
6. `Docs/Modules/Gameplay.md`
7. `Docs/Modules/WorldLifecycle.md`
8. `Docs/Modules/InputModes.md`
9. `Docs/ADRs/README.md`
10. `Docs/Reports/Audits/LATEST.md`
11. `Docs/Reports/Evidence/LATEST.md`
12. `Docs/Plans/Plan-Continuous.md`
13. `Docs/CHANGELOG.md`

## Estado canonico atual

- `startup` pertence ao bootstrap.
- `frontend` e `gameplay` pertencem a `SceneRouteKind`.
- Navigation/Transition operam em direct-ref + fail-fast.
- `GameNavigationCatalogAsset` e o asset canonico de navigation.
- `TransitionStyleAsset` e o asset canonico de style.
- `SceneTransitionProfile` e asset leaf visual.
- `ActorGroupRearm` e a nomenclatura canonica de rearm/reset de gameplay.

## Leitura historica

Qualquer documento historico remanescente fora da cadeia acima deve ser tratado como referencia arquivistica, nao como contrato operacional.
