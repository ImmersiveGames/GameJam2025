# SA-ACTOR-1C1-CLOSURE — SessionScoped Structural Actor Lifetime

## Status

CLOSED / PASS funcional.

## Objetivo

Fechar o corte `SA-ACTOR-1C1` da Base 2.0 para `ActorScope.SessionScoped`, corrigindo ownership de scope/lifetime estrutural sem criar trilho `Player/NonPlayer` paralelo.

## Decisão congelada

`ActorScope.SessionScoped` significa que o Actor estrutural permanece vivo no escopo da sessão.

Isso não significa que todos os componentes/capabilities materiais do Actor sejam retidos. Presentation, Attributes, PowerUps, Permission, Movement, Camera, Input e demais capabilities devem ter policies próprias de lifetime.

```text
ActorScope decide lifetime estrutural do Actor.
ComponentScope/CapabilityPolicy decide lifetime de cada componente/capability.
```

## Owner correto

| Responsabilidade | Owner |
|---|---|
| Slot/seleção/participação do player | PlayerParticipation / SessionOperational |
| Scope do player materializado | PlayerSetDefinition.Entry.actorScope |
| Materialização/reuso do Actor | ActivityEntryPipeline |
| Store/root session-owned | SessionActorRuntimeStore como índice técnico + adapter/root runtime |
| Teardown estrutural do Actor | SessionActivityPipeline / ActivityExitActorTeardownStage / SessionReset |
| Lifetime de componentes/capabilities | Stages/policies locais |
| Fim de sessão ao sair para Menu | SessionOperational detecta; SessionActivity executa SessionReset |

## Cortes fechados

```text
H1/H2 — Placement por fontes autorizadas da ActivityEntry.
H3 — Runtime metadata do PlayerActor vem do binding.
H4 — actorScope do Player vem de PlayerSetDefinition.Entry.actorScope.
H5 — Placement owner restaurado para SessionScoped.
H6 — RouteExit emite decisão para SessionScoped em store.
H7A — ComponentLifetime observability + redução local de logs.
H7B — SessionReset libera SessionScoped estrutural.
H7B1 — ExitToMenu chama SessionReset canônico.
H7B2 — SessionReset pós-RouteExit terminal permitido.
```

## Smoke aceito

```text
Boot -> Menu -> Sandbox
Activity 01 entry
CompleteActivationWindow
RestartCurrentActivity
CompleteActivationWindow
CompleteCurrentActivity
Activity 01 -> Activity 02
BackToMenu / ExitToMenu
```

Critérios aceitos:

```text
sem erro CS
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
SessionScoped + ActivityExit => Retain
SessionScoped + RouteExit => Retain
SessionScoped + SessionReset => Release
SessionResetCompleted sessionActorCount='0'
UnloadSceneCompleted scene='SessionActivitySandboxScene'
visual pós-BackToMenu correto
```

## Invariantes

```text
PlayerActor prefab não decide ActorScope runtime.
PlayerSetDefinition.Entry.actorScope é a fonte de scope do player materializado.
SessionActorRuntimeStore não decide lifecycle.
RouteExit genérico não libera SessionScoped.
ExitToMenu encerra sessão e libera SessionScoped via SessionReset.
SessionReset pós-ClosedForRouteExit não reabre Activity lifecycle.
Component/capability lifetime não é derivado automaticamente de ActorScope.
```

## Fora do escopo

```text
Progression Save real de actor.
Runtime join real.
Multiplayer/split-screen.
Pooling real de Presentation.
Policies avançadas de power-ups/attributes.
Redução global de logs fora do corte local.
```
