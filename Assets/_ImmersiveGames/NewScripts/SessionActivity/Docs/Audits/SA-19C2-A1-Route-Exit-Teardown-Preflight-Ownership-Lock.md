# SA-19C2-A1 — Route-Exit Teardown Preflight Ownership Lock

**Status:** Applied / Pending compile + smoke  
**Runtime scope:** SessionActivity boundary + SessionOperational handoff-exit preflight  
**Data:** 2026-06-15

## Objetivo

Fechar o resíduo encontrado em `SA-19C2-AUDIT`: o `OperationalHandoffExitStage` ainda lia `CurrentStage`, `CurrentRailKind` e `HasPendingOperation` da boundary de `SessionActivity` e classificava localmente policy de route-exit teardown.

O corte move a classificação de preflight para o owner correto: `SessionActivityPipeline`, exposta pela boundary como resultado tipado.

## Alterações

- Criado `SessionActivityRouteExitTeardownPreflightKind`.
- Criado `SessionActivityRouteExitTeardownPreflightResult`.
- `ISessionActivityRouteExitTeardownBoundary` ganhou `EvaluateRouteExitTeardownPreflight(...)`.
- `ISessionActivityRouteExitTeardownBoundary` deixou de expor `CurrentStage`, `CurrentRailKind` e `HasPendingOperation` crus.
- `SessionActivityHost` apenas delega `EvaluateRouteExitTeardownPreflight(...)` ao pipeline.
- `SessionActivityPipeline` classifica:
  - session stale/foreign;
  - pipeline não iniciado;
  - pending operation ativo;
  - activation window ainda não concluída;
  - deactivation/transition em andamento fora de `ActivityRouteExitRail`;
  - accepted.
- `SessionActivityOperationalRouteHandoffExitAdapter` converte o resultado tipado de Activity para `OperationalRouteHandoffExitPreflightResult`.
- `OperationalHandoffExitStage` deixou de conhecer stage/rail/pending operation de `SessionActivity`.
- `OperationalHandoffExitStage` deixou de receber `ISessionActivityRouteExitTeardownBoundary` diretamente.

## Ownership

| Decisão | Owner |
|---|---|
| Classificar preflight de route-exit teardown | `SessionActivityPipeline` |
| Delegar boundary Unity/Operational | `SessionActivityHost` |
| Converter boundary Activity para port Operational | `SessionActivityOperationalRouteHandoffExitAdapter` |
| Orquestrar route transition operacional | `SessionOperationalPipeline` / `OperationalHandoffExitStage` |

## Anti-deslocamento

- O pipeline dono da decisão é `SessionActivityPipeline`.
- O corte mexe em boundary/policy surface, não em stage executor.
- A exposição crua de stage/rail/pending operation foi removida da boundary.
- A compatibilidade crua não era necessária para o plano atual.
- O erro estava na fronteira: Operational interpretava lifecycle interno de Activity.
- Não foi criado owner duplicado para teardown.
- Não foi criado manager/coordinator/processor.
- O adapter não decide lifecycle; apenas traduz resultado tipado.

## Fora do escopo

- Não mexer em executor de teardown.
- Não reabrir `SA-19B2`.
- Não reabrir `SA-19B3`.
- Não mexer em reset policy.
- Não mexer em Actor/Command/Projectile.
- Não mexer em movement/camera/content release.

## Smoke exigido

- `RestartCurrentActivity`
- `Activity01ToActivity02`
- `RouteExitBackToMenu`

Critérios mínimos:

- sem `error CS`;
- sem `FATAL`;
- sem `Exception`;
- sem `route_transition_failed`;
- sem `checkpointStatus='Failed'`;
- sem foreign/stale indevido;
- sem fallback silencioso;
- checkpoints canônicos preservados.
