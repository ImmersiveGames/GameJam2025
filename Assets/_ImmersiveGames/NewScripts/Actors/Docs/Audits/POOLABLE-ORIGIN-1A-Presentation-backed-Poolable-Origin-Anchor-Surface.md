# POOLABLE-ORIGIN-1A - Presentation-backed Poolable Origin Anchor Surface

Status: implemented / pending manual smoke.

## Objetivo

Criar um contrato genérico mínimo para anchors/origins de poolables baseados na presentation materializada, sem ligar o projectile fire a esse contrato neste corte.

## Audit curto

- `ActorPresentationEndpoint` é o owner local correto para indexar anchors materializados.
- `ActorPresentationContainer` permanece container visual, não owner de gameplay.
- `ActivityEntryActorPresentationStage` e `ActorPooledPresentationPreparer` chamam o mesmo rebuild após materializar/retener presentation.
- `ActorPooledPresentationPreparer` continua materializando a presentation no lifecycle do pool.
- `PooledActorProjectileSpawnAdapter` não resolve anchor nem lê hierarquia de origem.
- `ActivityEntryActorPresentationStage` continua fora do fluxo de projectile runtime-spawned.

## Shape final escolhido

```text
PoolableSpawnOriginId
PoolableSpawnOriginKind
PoolableSpawnOriginResolutionMode
PoolableSpawnOriginResolved
IPoolableSpawnOriginSurface
PoolableSpawnOriginAnchor
```

### Owner da surface/index

`ActorPresentationEndpoint` indexa anchors materializados por `originId` e expõe resolução explícita.

### Como os anchors são registrados

`ActorPresentationEndpoint.RebuildPoolableSpawnOriginSurface(...)` varre `PoolableSpawnOriginAnchor` na apresentação materializada e monta o índice runtime local.

### Como fallback funciona sem silencioso

- `RequireTypedOrigin`: falha se o anchor não existir.
- `PreferTypedOriginUseEmitterRoot`: tenta typed origin e, se faltar, aplica fallback explícito para o `visual.root` materializado.
- `UseEmitterRoot`: resolve diretamente o `visual.root` materializado.
- Ausência de anchors no rebuild é observação `optional_no_anchors` quando não há requirement explícito.

## Logs esperados

```text
PoolableSpawnOriginAnchorRegistered
PoolableSpawnOriginSurfaceBuilt
PoolableSpawnOriginResolved
PoolableSpawnOriginMissing
PoolableSpawnOriginFallbackApplied
```

## Fora do corte

- ligar `ActorProjectileFireEndpoint` ao surface;
- mudar motion/collision/damage/lifetime/impact/audio;
- criar manager/scanner/pipeline novo;
- alterar `ActorPresentationContainer` como owner de gameplay.

## Smoke esperado

```text
PoolableSpawnOriginAnchorRegistered
PoolableSpawnOriginSurfaceBuilt
originId='actor.player.primary.origin'
surfaceOwner='ActorPresentationEndpoint'
PoolableSpawnOriginResolved
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
```
