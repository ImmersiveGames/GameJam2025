# RESET-SCOPE-4-H1 — Boundary Source Classification Fix

Status: Applied / pending compile + smoke.

## Objetivo

Corrigir o wiring do `RESET-SCOPE-4` para que o boundary real de entrada da activity seja carregado até o `ActivityEntryPipeline`.

O corte anterior corrigiu a semântica da policy para boundary eligibility, mas ainda classificava entrada por transição de activity como `Activity`.

## Decisão

A classificação do boundary acontece antes da entrada no `ActivityEntryPipeline`.

- entrada inicial de activity: `Activity`;
- restart da activity atual: `Activity`;
- entrada causada por `ContinueToNextActivity` / auto-continue: `ActivityTransition`;
- QA actor/object reset: `Local`;
- route transition permanece reservado para reset de boundary de rota, não teardown genérico.

## Alterações aplicadas

- `ActivityEntryCommand` passou a carregar `ResetBoundaryKind`.
- `SessionActivityPipeline.EnterActivity(...)` passou a receber o boundary real da entry.
- chamadas iniciais/restart usam `Activity`.
- chamada de `EmitContinueAsync(...)` para próxima activity usa `ActivityTransition`.
- continuação pós-load de conteúdo reaproveita o boundary pendente da entry.
- `ActivityResetBoundaryPolicy.ResolveForEntry(...)` usa o boundary carregado no command.
- `ResolveTargetScope(...)` classifica `ActivityTransition` como `CurrentActivity`.
- logs de eligibility passaram a formatar `All` como `All`, evitando `resetBoundaryEligibility='-1'`.

## Ownership

- `SessionActivityPipeline`: classifica o boundary de entrada conforme o rail ativo.
- `ActivityEntryPipeline`: consome command já classificado e mantém ordem da entry.
- `ActivityResetBoundaryPolicy`: resolve `ActivityResetScopePlan` e eligibility requerida.
- stages: filtram references/reports por boundary eligibility.
- adapters/endpoints: executam grupos declarados, sem decidir boundary/policy.

## Smoke esperado

O próximo smoke deve conter:

```text
activity_01 initial entry:
resetBoundaryKind='Activity'
boundaryEligibilityRequired='Activity'

QA actor/object reset:
resetBoundaryKind='Local'
boundaryEligibilityRequired='Local'

activity_01 -> activity_02:
resetBoundaryKind='ActivityTransition'
boundaryEligibilityRequired='ActivityTransition'
```

Não deve conter:

```text
resetBoundaryKind='QaManual'
FilteringByScopePlan
PassiveNoFiltering
resetBoundaryEligibility='-1'
```

## Critério de aceite

- sem `FATAL`;
- sem `Exception`;
- sem `route_transition_failed`;
- sem `error CS`;
- sem `checkpointStatus='Failed'`;
- sem foreign/stale indevido;
- `Activity01ToActivity02` preservado;
- `RouteExitBackToMenu` preservado;
- boundary `ActivityTransition` observado na entrada da `activity_02`;
- QA permanece `Local`.
