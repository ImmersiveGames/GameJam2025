# SA-8A3-H1 — RouteExit active path enforcement

Status: PATCH / aguardando smoke.

## Motivo

O smoke pedido executou o cenário correto:

```text
Boot -> Menu -> Sandbox -> activity_01 -> CompleteActivationWindow -> BackToMenu
```

O fluxo passou funcionalmente, mas o runtime ativo ainda executou:

```text
ActorPresentationReleaseStarted rail='RouteExit'
```

antes da `DeactivationWindow`.

Isso viola a `ExitOrderingPolicy` congelada no SA-8A1:

```text
DeactivationWindow faz parte do lifecycle de saída.
Release/teardown vem depois dela.
RouteExit não pode pular esse lifecycle.
```

## Correção

Este hotfix reforça o SA-8A3 em dois níveis:

1. `EmitCloseForRouteExit` continua movendo o RouteExit vindo de `ActivityRunning` para:

```text
ActivityRunning
-> ActivityRouteExitRequested
-> MovementControlDisabled
-> RouteExitActorTeardownDeferredUntilAfterDeactivation
-> DeactivationWindowStarted/Ready
-> CompleteDeactivationWindow ou skip no-content
-> RouteExitActorTeardownAfterDeactivationStarted
-> ActorPresentationReleaseStarted rail='RouteExit'
-> RouteExitActorTeardownAfterDeactivationCompleted
-> ClosedForRouteExit
```

2. `ExecuteActivityExitActorTeardown` agora possui um guard de fronteira:

```text
RouteExitActorTeardownGuardDeferred
```

Se algum caminho antigo tentar executar `ActorTeardown(RouteExit)` antes de `DeactivationWindowCompleted` ou `DeactivationWindowSkippedNoContent`, o teardown é bloqueado e adiado. Esse guard impede regressão mesmo que um branch antigo volte a chamar o stage cedo demais.

## Critérios de smoke

Hard checks:

```text
sem erro CS
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
```

Ordering esperado para `activity_01 -> ActivityRunning -> BackToMenu`:

```text
ActivityExitOrderingPolicyApplied
RouteExitActorTeardownDeferredUntilAfterDeactivation
DeactivationWindowStarted
DeactivationWindowReady
CompleteDeactivationWindow
RouteExitActorTeardownAfterDeactivationStarted
ActorPresentationReleaseStarted rail='RouteExit'
RouteExitActorTeardownAfterDeactivationCompleted
ClosedForRouteExit
RouteExitBackToMenu PASS
```

Critério negativo:

```text
ActorPresentationReleaseStarted rail='RouteExit'
```

não pode aparecer antes de `DeactivationWindowCompleted` ou `DeactivationWindowSkippedNoContent`.

Se aparecer `RouteExitActorTeardownGuardDeferred`, isso é evidência de que o guard bloqueou um caminho antigo. O ideal final é que apareça 0 vezes; se aparecer e o teardown pós-deactivation ocorrer corretamente, o hotfix ainda corrige a regressão de ordering.
