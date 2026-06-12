# RESET-SCOPE-1 — QA Object Reset Policy Alignment

## Status

Closed / PASS.

## Baseline congelado

`RESET-SCOPE-1` fecha o alinhamento do reset manual de QA de objetos ao contrato canônico de boundary/scope criado em `RESET-SCOPE-0`.

## Objetivo

Alinhar o reset manual de QA de objetos ao mesmo contrato de boundary/scope criado em `RESET-SCOPE-0`, sem alterar comportamento e sem ativar filtro real de grupos.

## Decisão

O QA não decide política de reset localmente. O QA continua sendo endpoint de acionamento manual, mas resolve um `ActivityResetScopePlan` canônico antes de chamar o stage de reset.

## Owner

- `SessionActivityPipeline`: owner macro do comando QA e da validação do activity cycle ativo.
- `ActivityResetBoundaryPolicy`: owner da classificação de boundary/scope.
- `ActivityResetStage`: executor técnico do reset manual de QA, recebendo plano resolvido.
- `ActivityObjectResetEndpoint`: aplica side-effect local quando comandado.

## Alterações aplicadas

- `ActivityResetBoundaryPolicy.ResolveForQaManual(ActivityResetCommand)` criado.
- `TryQaResetCurrentActivityObjects(...)` monta `ActivityResetCommand`, resolve `ActivityResetScopePlan` e registra `ActivityObjectResetQaScopePlanResolved`.
- `ActivityResetStage.Execute(...)` exige `ActivityResetScopePlan` válido e do mesmo cycle.
- Logs/facts de QA object reset registram:
  - `resetBoundaryKind`
  - `resetTargetScope`
  - `resetPolicyId`
  - `behaviorMode='PassiveNoFiltering'`

## Comportamento preservado neste corte

O corte foi passivo:

- não filtrou `ActivityStateResetGroup`;
- não alterou quais comandos eram emitidos;
- não mudou `ActivityEntryObjectResetStage`;
- não alterou `RouteExit`;
- não alterou `SessionReset`;
- não recriou `ActivityResetScopeResolver` antigo;
- não criou manager/coordinator/fallback.

## Evidência de smoke

Smoke específico exercitou QA object reset em `activity_01` e em `activity_02`.

Critérios observados:

- sem `FATAL`;
- sem `Exception`;
- sem `route_transition_failed`;
- sem `error CS`;
- sem `checkpointStatus='Failed'`;
- `ActivityObjectResetQaScopePlanResolved` observado;
- `resetBoundaryKind='QaManual'` observado;
- `resetTargetScope='CurrentActivityEntry'` observado;
- `resetPolicyId='qa_manual_reset_all_declared_groups'` observado;
- `behaviorMode='PassiveNoFiltering'` observado;
- `ActivityObjectResetQaApplied` observado em `activity_01`;
- `ActivityObjectResetQaSkipped` observado em `activity_02` no-content;
- `Activity01ToActivity02 checkpointStatus='Passed'` preservado.

## Fechamento arquitetural

- `SessionActivityPipeline` segue owner do comando QA.
- `ActivityResetBoundaryPolicy` decide boundary/scope/policy.
- `ActivityResetStage` executa com plano resolvido.
- Endpoint/adapter não decide policy.
- `activity_01` aplica reset.
- `activity_02` preserva `NoCommands`.
- Nenhum fallback/trilho paralelo novo foi introduzido.

## Próximo corte

`RESET-SCOPE-2 — Reset Scope Plan Filtering`.

Objetivo: ativar filtragem real de grupos por `ActivityResetScopePlan` antes da emissão dos commands, preservando o mesmo owner de policy e impedindo que adapters/endpoints decidam boundary/scope.
