# SA-8A1 — ExitOrderingPolicy Canonical Freeze

Status: **CLOSED / POLICY FROZEN / NO RUNTIME EXECUTION CHANGE**  
Data: 2026-06-01  
Escopo: `SessionActivity` / `RouteExit` / `CompleteCurrentActivity` / `RestartCurrentActivity`

## Objetivo

Congelar a policy canônica de ordering de saída da Activity antes de qualquer nova decomposição ou alteração runtime.

Este corte cria o contrato passivo:

```text
NewScripts/SessionActivity/Contracts/ActivityExitOrderingPolicyContracts.cs
```

A policy ainda não é chamada pelo runtime. Ela existe para impedir que os próximos patches reabram a decisão de ordem em cada método local.

## Decisão normativa

A `DeactivationWindow` faz parte do lifecycle de saída da Activity.

Portanto:

```text
DeactivationWindow vem antes de release/teardown.
Actor/Object teardown vem depois da janela de saída.
ActivityContent release vem depois do teardown/release de objetos/atores.
RouteExit não pode pular esse lifecycle.
Restart só pode pular DeactivationWindow por policy explícita.
```

## Owner correto

| Responsabilidade | Owner correto |
|---|---|
| Decidir que a Activity vai fechar | `SessionActivityPipeline` |
| Decidir ordering macro de saída | `ActivityExitOrderingPolicy` sob ownership do `SessionActivityPipeline` |
| Executar actor teardown | `ActivityExitActorTeardownStage` |
| Executar object snapshot/capture/release | `ActivityObjectSnapshotCaptureStage` / `ActivityObjectReleaseStage` |
| Executar ActivityContent release/unload | `ActivityContentRelease*Stage` |
| Solicitar RouteExit | `SessionOperationalPipeline` |
| Continuar troca de rota | `SessionOperationalPipeline`, somente depois de `RouteExitCompleted` |
| Entry/setup/readiness | `ActivityEntryPipeline`, fora do exit |

## Policy congelada

| Cenário | DeactivationWindow | Snapshot/Capture | Actor/Object teardown | ActivityContent release | Continuação |
|---|---|---|---|---|---|
| `CompleteCurrentActivity` | usar se declarada | antes do teardown | depois da window | depois do teardown | próxima Activity ou fim do catálogo |
| `RestartCurrentActivity` | pular por policy explícita de restart técnico | não salvar snapshot final por padrão | antes da nova entry | antes da nova entry | reiniciar mesma Activity |
| `RouteExitFromActivityRunning` | usar se declarada | antes do teardown | depois da window | depois do teardown | completar handoff de RouteExit |
| `RouteExitFromDeactivationWindowReady` | reutilizar janela ativa | antes do teardown | depois do complete da janela ativa | depois do teardown | completar handoff de RouteExit |

## Sequência canônica por cenário

### CompleteCurrentActivity

```text
ActivityRunning
→ MarkExitRequested
→ BlockGameplayControl
→ StartDeactivationWindowIfDeclared
→ AwaitExplicitDeactivationWindowCompletion
→ CompleteDeactivationWindow
→ CaptureActivitySnapshots
→ RunActorAndObjectExitTeardown
→ ReleaseActivityContent
→ CompleteActivityExit
→ StartNextActivityEntry ou StopActivityLifecycle
```

### RestartCurrentActivity

```text
ActivityRunning
→ MarkExitRequested
→ BlockGameplayControl
→ Skip DeactivationWindow por policy explícita
→ RunActorAndObjectExitTeardown
→ ReleaseActivityContent
→ CompleteActivityExit
→ RestartCurrentActivityEntry
```

Observação: se o projeto decidir futuramente que restart deve exibir `DeactivationWindow`, isso deve virar nova versão da policy. Não pode ser branch local escondido.

### RouteExitFromActivityRunning

```text
Operational solicita RouteExit
→ SessionActivityPipeline aceita CloseForRouteExit
→ MarkExitRequested
→ BlockGameplayControl
→ StartDeactivationWindowIfDeclared
→ AwaitExplicitDeactivationWindowCompletion
→ CompleteDeactivationWindow
→ CaptureActivitySnapshots
→ RunActorAndObjectExitTeardown
→ ReleaseActivityContent
→ CompleteRouteExitHandoff
→ Operational continua route transition
```

Regra dura:

```text
SessionOperationalPipeline não executa side-effect de troca de rota antes de CompleteRouteExitHandoff.
```

Planejamento é aceitável. Side-effects não.

### RouteExitFromDeactivationWindowReady

```text
DeactivationWindowReady
→ RouteExit solicitado
→ não iniciar segundo rail
→ não duplicar DeactivationWindow
→ marcar RouteExit pendente
→ AwaitExplicitDeactivationWindowCompletion
→ CompleteDeactivationWindow
→ CaptureActivitySnapshots
→ RunActorAndObjectExitTeardown
→ ReleaseActivityContent
→ CompleteRouteExitHandoff
→ Operational continua route transition
```

## Invariantes

1. `ActivityEntryPipeline` não decide exit, restart, deactivation ou route-exit.
2. `SessionOperationalPipeline` não executa route apply, route camera release, activity camera release ou route save antes de `RouteExitCompleted` quando há Activity ativa.
3. `RouteExit` vindo de `DeactivationWindowReady` reutiliza o rail ativo e não cria rail paralelo.
4. `RestartCurrentActivity` só pula `DeactivationWindow` porque a policy canônica permite explicitamente.
5. Release/teardown não roda antes de `DeactivationWindow` quando a policy manda usar janela declarada.
6. Adapters não decidem ordering; executam comandos/stages.
7. Config obrigatória ausente continua sendo erro; a policy não cria fallback.

## Matriz de resíduos para próximo corte

| Área | Estado atual | Problema | Severidade | Próxima ação |
|---|---|---|---|---|
| `CompleteCurrentActivity` | ordering precisa ser comparado contra policy | pode divergir de RouteExit/Restart | Alta | auditar métodos existentes contra `ActivityExitOrderingPolicy` |
| `RestartCurrentActivity` | restart técnico parece pular window | agora aceitável se explícito pela policy | Média | garantir log/policyId no rail de restart |
| `RouteExitFromActivityRunning` | auditoria apontou possível teardown antes da window | viola policy se confirmado no código ativo | Alta | patch somente se evidência confirmar divergência |
| `RouteExitFromDeactivationWindowReady` | precisa garantir single rail | risco de rail paralelo/pending handoff duplicado | Alta | auditar/patchear marcação de pending RouteExit |
| Operational preflight | ainda conhece detalhes internos do SessionActivity | boundary smell | Média | tratar depois do ordering estar alinhado |

## Critério de aceite para implementação futura

Um patch futuro de implementação só pode ser aceito com smoke/log demonstrando:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem fallback silencioso
ActivityExitOrderingPolicy policyId visível nos logs do rail alterado
RouteExit não aplica side-effect operacional antes de RouteExitCompleted
RestartCurrentActivity mantém PASS
Activity01ToActivity02 mantém PASS
RouteExitBackToMenu mantém PASS
```

## Perguntas obrigatórias respondidas

| Pergunta | Resposta |
|---|---|
| Qual pipeline é dono desta decisão? | `SessionActivityPipeline`, com policy canônica `ActivityExitOrderingPolicy`. |
| Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data? | Policy canônica passiva. |
| Isso é comportamento final ou bridge transitória? | Comportamento final de ordering, ainda sem enforcement runtime neste corte. |
| Essa compatibilidade ainda é necessária? | Não há compat. É contrato novo para substituir decisões locais difusas. |
| O erro está no sintoma ou na fronteira arquitetural errada? | Fronteira/ordering macro de exit. |
| Existe owner duplicado para o mesmo lifecycle? | Deve deixar de existir: policy pertence ao macro lifecycle de `SessionActivityPipeline`; stages só executam passos. |

## Decisão final

`SA-8A1` congela a policy canônica. O próximo corte não deve inventar novo pipeline nem novo rail; deve apenas auditar e alinhar o código ativo contra esta policy.
