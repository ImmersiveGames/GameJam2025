# RESET-SCOPE-4 — Boundary Eligibility Policy

Status: Applied / partial smoke observed; superseded by RESET-SCOPE-4-H1 for boundary source classification.

## Objetivo

Corrigir a direção da política de reset.

A política deste corte não decide quais grupos resetam. Os grupos continuam sendo capacidade do endpoint/contribution. A política decide apenas se o endpoint/contribution pode participar do reset para o boundary atual.

## Decisão

`ResetGroup` responde: o que o endpoint sabe resetar.

`ActivityResetBoundaryEligibility` responde: quando o endpoint pode participar do reset.

`ActivityResetBoundaryPolicy` compara o boundary runtime com a eligibility declarada pelo componente/contribution.

QA não possui boundary ou política própria. QA apenas dispara reset local usando `ActivityResetBoundaryKind.Local` e `source/reason` de QA.

## Boundaries canônicos

- `Local`;
- `Activity`;
- `ActivityTransition`;
- `RouteTransition`.

`QaManual`, `ActivityEntry`, `RestartCurrentActivity`, `RouteExit` e `SessionReset` deixam de ser boundaries reais desta policy.

## Ownership

- `SessionActivityPipeline`: owner do comando QA e source/reason.
- `ActivityEntryPipeline`: owner da ordem de entry e resolução do boundary de Activity.
- `ActivityResetBoundaryPolicy`: owner da classificação boundary/eligibility.
- `ActivityEntryParticipantResetStage`: filtra actor reset references por boundary eligibility.
- `ActivityEntryObjectResetStage`: filtra reports de object reset por boundary eligibility.
- `ActivityResetStage`: filtra QA object reset por boundary eligibility.
- `ActorResetAdapter` e object reset endpoints: executam os grupos declarados; não decidem policy.

## Alterações aplicadas

- Criado `ActivityResetBoundaryEligibility` com flags `Local`, `Activity`, `ActivityTransition`, `RouteTransition` e `All`.
- `ActivityResetBoundaryKind` reduzido para os quatro boundaries reais.
- `ActivityResetScopePlan` deixou de carregar `AllowedActorResetGroups` e `AllowedActivityObjectResetGroups`.
- `ActivityResetBoundaryPolicy` deixou de filtrar grupos.
- `ActivityResetBoundaryPolicy` passou a filtrar references/reports por boundary eligibility.
- QA actor reset usa `ResolveForLocal`.
- QA object reset usa `ResolveForLocal`.
- Activity entry reset usa `ResolveForActivity`.
- `ActivityObjectContributor` ganhou `resetBoundaryEligibility` serializado, default `All`.
- Actor reset contributors ganharam `ResetBoundaryEligibility`.
- Componentes atuais com reset actor ganharam campo serializado `resetBoundaryEligibility`, default `All`:
  - `PlayerActor`;
  - `PlayerMovementController`;
  - `PlayerActorParticipationState`;
  - `ActorProjectileSpawnRuntimeTracker`.

## Comportamento esperado

Por default, como `resetBoundaryEligibility = All`, o comportamento funcional deve permanecer equivalente ao corte anterior.

A diferença arquitetural é que agora um componente pode excluir participação por boundary sem alterar seus reset groups.

Exemplo:

- `MovementTransient` continua sendo grupo do `PlayerMovementController`.
- Se `resetBoundaryEligibility = Local | Activity`, ele participa de QA local e activity reset.
- Se `resetBoundaryEligibility` não contém `RouteTransition`, ele não participa de reset entre rotas.

## Logs esperados

- `behaviorMode='BoundaryEligibilityFiltering'`.
- `boundaryEligibilityRequired='Local'` em QA reset.
- `boundaryEligibilityRequired='Activity'` na entrada da activity.
- `resetBoundaryEligibility='...'` em references/reports filtrados.
- Ausência de `allowedActorResetGroups` e `allowedActivityObjectResetGroups` nos logs da policy.
- Ausência de `QaManual` como boundary.

## Critério de smoke

Smoke mínimo:

1. Boot -> Menu -> Sandbox.
2. CompleteActivationWindow.
3. QA Reset Current Player Actor.
4. QA Reset Current Activity Objects.
5. CompleteCurrentActivity.
6. CompleteDeactivationWindow.
7. Chegar em `activity_02` Running.
8. QA Reset Current Player Actor em `activity_02`.
9. QA Reset Current Activity Objects em `activity_02`.
10. RouteExitBackToMenu.

PASS exige:

- sem `FATAL`;
- sem `Exception`;
- sem `route_transition_failed`;
- sem `error CS`;
- sem `checkpointStatus='Failed'`;
- sem foreign/stale indevido;
- logs com `behaviorMode='BoundaryEligibilityFiltering'`;
- QA logs com `resetBoundaryKind='Local'`;
- entry logs com `resetBoundaryKind='Activity'`;
- sem `resetBoundaryKind='QaManual'`;
- sem logs de `allowedActorResetGroups` / `allowedActivityObjectResetGroups`;
- adapters/endpoints sem decisão de policy;
- grupos continuam aplicados pelo endpoint elegível.

## Observação

`ActivityTransition` e `RouteTransition` foram definidos no contrato, mas este corte não força uso onde o pipeline ainda não possui boundary explícito separado. O objetivo do corte é corrigir a semântica da policy e preservar comportamento por default.


## H1 follow-up

Smoke do RESET-SCOPE-4 mostrou que `QaManual` foi removido corretamente e QA passou a usar `Local`, mas a entrada de `activity_02` via auto-continue ainda era classificada como `Activity`.

O follow-up `RESET-SCOPE-4-H1 — Boundary Source Classification Fix` corrige o carregamento do boundary real no `ActivityEntryCommand`.
