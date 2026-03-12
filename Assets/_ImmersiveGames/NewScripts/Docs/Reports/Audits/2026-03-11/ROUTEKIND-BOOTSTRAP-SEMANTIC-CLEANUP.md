# RouteKind + Bootstrap Semantic Cleanup

## Escopo

- `startup` consolidado como semantica exclusiva do bootstrap.
- `frontend` e `gameplay` consolidados como semantica exclusiva de `SceneRouteKind`.
- `IntroStage` passou a consumir `RouteKind` no proprio contexto, sem inferencia principal por `targetScene`.
- `SceneFlow` signatures deixaram de incorporar labels de style/profile.
- Residuos tipados antigos de style/profile foram removidos quando ficaram sem uso real.

## Resultado

- Runtime permanece direct-ref-first e fail-fast.
- `style` e `profile` continuam apenas como labels descritivos para logs/fade.
- `WorldLifecycle` passou a registrar `routeKind` como owner da decisao de reset/skip, mantendo `profileLabel` apenas como metadata.
