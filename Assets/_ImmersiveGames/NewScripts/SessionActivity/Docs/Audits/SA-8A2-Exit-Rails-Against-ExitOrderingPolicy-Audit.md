# SA-8A2 — Exit Rails Against ExitOrderingPolicy Audit

Status: `AUDITED / NO RUNTIME CHANGE`

Base usada para a auditoria:

- Fonte base reconstruída a partir de `output.zip` + pacotes SA-ACTOR/SA-7/SA-8A1 aplicados em ordem.
- Policy normativa passiva criada em `SA-8A1`:
  - `NewScripts/SessionActivity/Contracts/ActivityExitOrderingPolicyContracts.cs`
  - `NewScripts/SessionActivity/Docs/Audits/SA-8A1-ExitOrderingPolicy-Canonical-Freeze.md`

Este documento não altera runtime, não altera rails ativos e não muda comportamento.

---

## 1. Decisão normativa usada como referência

A policy canônica congelou quatro cenários:

| Cenário | DeactivationWindowRule | Continuation | SnapshotBeforeTeardown | ReleaseAfterDeactivationWindow | OperationalMayContinueBeforeRouteExitCompleted |
|---|---|---|---:|---:|---:|
| `CompleteCurrentActivity` | `UseDeclaredWindow` | `StartNextActivity` | `true` | `true` | `true` |
| `RestartCurrentActivity` | `SkipByExplicitRestartPolicy` | `RestartSameActivity` | `false` | `true` | `true` |
| `RouteExitFromActivityRunning` | `UseDeclaredWindow` | `CompleteRouteExitHandoff` | `true` | `true` | `false` |
| `RouteExitFromDeactivationWindowReady` | `ReuseActiveWindow` | `CompleteRouteExitHandoff` | `true` | `true` | `false` |

Regra central:

```text
DeactivationWindow é lifecycle de saída.
Release/teardown vem depois da DeactivationWindow, exceto quando a própria policy explicita skip.
RouteExit não pode pular lifecycle de saída.
Operational só continua após ActivityRouteExitCompleted / ClosedForRouteExit.
```

---

## 2. Resumo executivo

| Rail ativo | Status contra policy | Severidade | Decisão |
|---|---|---:|---|
| `CompleteCurrentActivity` | `MOSTLY_ALIGNED` | Baixa | Não mexer primeiro. Usar como referência de ordem. |
| `RestartCurrentActivity` | `MISALIGNED` | Alta | Corrigir antes de extrair qualquer pipeline de exit. |
| `RouteExitFromActivityRunning` | `MISALIGNED` | Alta | Corrigir antes de considerar PASS arquitetural de RouteExit 2.0. |
| `RouteExitFromDeactivationWindowReady` | `PARTIALLY_ALIGNED` | Média/Alta | Corrigir para não auto-completar janela já ativa. |
| `Operational AwaitRouteExitTeardownAsync` | `PARTIALLY_ALIGNED` | Média | Mantém await correto, mas depende do rail interno corrigido. |

Conclusão:

```text
Não criar ActivityExitPipeline agora.
O problema real é ordering divergente entre rails ativos.
O patch mínimo deve normalizar Restart e RouteExit contra a policy canônica.
```

---

## 3. Auditoria por rail

### 3.1 `CompleteCurrentActivity`

Arquivo/classe/método:

```text
NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs
SessionActivityPipeline.EmitCompleteAsync(...)
```

Evidência de código:

```text
L2227-L2274  EmitCompleteAsync
L2254        bloqueia movement/control
L2256-L2259  inicia DeactivationWindow
L2263-L2269  se sem window, registra skip e finaliza deactivation
L2273        se additive, carrega DeactivationWindow
L2436-L2450  FinalizeDeactivationAndContinuation após CompleteDeactivationWindow/unload
L2453-L2480  teardown/release só após deactivation/blackout
L1108-L1124  content release executa snapshot capture antes de object release
```

Leitura:

- O rail começa em `ActivityRunning`.
- Bloqueia gameplay/movement antes da janela de saída.
- Inicia `DeactivationWindow` antes de chamar o teardown principal de continuidade.
- O teardown da continuidade ocorre depois da deactivation, dentro de `EnsureContinuationExitTeardownAfterBlackoutOrStartRelease`.
- Object snapshot/release ficam dentro do release de content.

Status:

```text
MOSTLY_ALIGNED
```

Risco residual:

- O rail ainda é monolítico e mistura policy/ordem/side-effect dispatch no `SessionActivityPipeline`.
- Não é prioridade corrigir agora, porque a ordem principal já bate com a policy.

Ação recomendada:

```text
Não mexer no primeiro patch SA-8A3.
Usar como referência de ordering para Restart e RouteExit.
```

---

### 3.2 `RestartCurrentActivity`

Arquivo/classe/método:

```text
NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs
SessionActivityPipeline.EmitRestartCurrentActivityAsync(...)
```

Evidência de código:

```text
L2276-L2342  EmitRestartCurrentActivityAsync
L2316-L2317  cria PendingRestartTransition
L2319-L2323  executa ActivityExitActorTeardown antes de DeactivationWindowStarted
L2325-L2328  inicia DeactivationWindow depois do actor teardown
L2331-L2338  se window None, finaliza restart
L2341        se additive, carrega DeactivationWindow
L2856-L2890  FinalizePendingRestartTransition
L2893-L2925  StartPendingRestartEntry
```

Divergência contra policy:

| Regra da policy | Estado atual | Resultado |
|---|---|---|
| `DeactivationWindowRule = SkipByExplicitRestartPolicy` | runtime não expressa policy de skip; ele inicia window declarada | Diverge |
| `releaseAfterDeactivationWindow = true` | actor teardown roda antes da window | Diverge |
| `snapshotBeforeTeardown = false` | ok para restart técnico | Alinhado |
| `Continuation = RestartSameActivity` | existe `PendingRestartTransition` e nova entry | Alinhado |

Problema central:

```text
Restart faz ActorTeardown antes de decidir/aplicar a policy de DeactivationWindow.
```

Isso mistura duas intenções incompatíveis:

1. Restart parece querer fazer teardown técnico rápido.
2. Mas ainda inicia `DeactivationWindow` declarada depois do teardown.

Status:

```text
MISALIGNED / HIGH
```

Ação recomendada para SA-8A3:

```text
Aplicar policy explícita de restart antes de teardown.
```

Patch mínimo recomendado:

```text
RestartCurrentActivity:
1. ActivityRunning
2. Mark ActivityRestartRequested / ActivityRestartAccepted
3. Block gameplay/movement
4. Apply ExitOrderingPolicy.RestartCurrentActivity
5. If policy is SkipByExplicitRestartPolicy:
   - emit DeactivationWindowSkippedByRestartPolicy ou equivalente já existente se possível
   - do NOT load declared DeactivationWindow
6. Run Actor/Object exit teardown after policy decision
7. Release ActivityContent
8. Start same Activity entry
```

Restrição:

```text
Não criar fallback.
Não criar ActivityExitPipeline.
Não alterar RouteExit no mesmo patch se o corte ficar grande.
```

---

### 3.3 `RouteExitFromActivityRunning`

Arquivo/classe/método:

```text
NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs
SessionActivityPipeline.EmitCloseForRouteExit(...)
```

Evidência de código:

```text
L2344-L2411  EmitCloseForRouteExit
L2358-L2390  branch ActivityRunning
L2360-L2368  marca route-exit/completing
L2369        bloqueia movement/control
L2370        executa ActivityExitActorTeardown antes de DeactivationWindowStarted
L2372-L2375  inicia DeactivationWindow depois do actor teardown
L2377-L2388  carrega ou pula DeactivationWindow
L2682-L2712  FinalizeDeactivationForRouteExit
L2714-L2750  CompleteRouteExitClosure / ActivityRouteExitCompleted
```

Divergência contra policy:

| Regra da policy | Estado atual | Resultado |
|---|---|---|
| `UseDeclaredWindow` | usa declared window | Alinhado parcial |
| `SnapshotBeforeTeardown = true` | actor teardown ocorre antes de deactivation e antes do content release/snapshot object | Diverge |
| `ReleaseAfterDeactivationWindow = true` | actor teardown ocorre antes da window | Diverge |
| `OperationalMayContinueBeforeRouteExitCompleted = false` | `AwaitRouteExitTeardownAsync` espera completion | Alinhado no boundary, depende do rail |

Problema central:

```text
RouteExit em ActivityRunning executa ActorTeardown antes da DeactivationWindow.
```

Isso viola a decisão congelada:

```text
RouteExit não pode pular lifecycle de saída.
Release/teardown deve vir depois da DeactivationWindow.
```

Status:

```text
MISALIGNED / HIGH
```

Ação recomendada para SA-8A3 ou SA-8A4:

```text
Mover ExecuteActivityExitActorTeardown para FinalizeDeactivationForRouteExit ou para stage chamado depois de DeactivationWindowCompleted/Skipped.
```

Patch mínimo recomendado:

```text
RouteExitFromActivityRunning:
1. ActivityRunning
2. Mark ActivityRouteExitRequested
3. Block gameplay/movement
4. Start DeactivationWindow if declared
5. Await CompleteDeactivationWindow or no-content skip
6. Capture snapshots
7. Run Actor/Object teardown/release with RouteExit rail
8. Release ActivityContent
9. CompleteRouteExitClosure
10. CompletePendingRouteExitTeardownIfAny
```

---

### 3.4 `RouteExitFromDeactivationWindowReady`

Arquivo/classe/método:

```text
NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs
SessionActivityPipeline.EmitCloseForRouteExit(...)
```

Evidência de código:

```text
L2392-L2401  branch DeactivationWindowReady
L2394-L2397  marca DeactivationWindowCompleted imediatamente
L2398        executa ActivityExitActorTeardown
L2400        unload da DeactivationWindow
```

Alinhamento parcial:

- Não inicia outra DeactivationWindow.
- Reusa a window ativa.
- Teardown ocorre depois de marcar `DeactivationWindowCompleted`.

Divergência:

```text
CloseForRouteExit auto-completa a DeactivationWindow quando ela já está em Ready.
```

Pela decisão congelada, quando RouteExit chega enquanto a activity já está em `DeactivationWindowReady`, o correto é:

```text
marcar RouteExit pendente
não duplicar window
não executar teardown imediatamente
aguardar CompleteDeactivationWindow
seguir fechamento como RouteExit
```

Status:

```text
PARTIALLY_ALIGNED / MEDIUM_HIGH
```

Ação recomendada:

```text
Trocar auto-complete por pending route-exit intent.
```

Patch mínimo recomendado:

```text
RouteExitFromDeactivationWindowReady:
1. Set active rail = ActivityRouteExitRail
2. Clear handoff/internal transition state
3. Emit ActivityRouteExitRequested/ReusedActiveDeactivationWindow fact
4. Do not emit DeactivationWindowCompleted yet
5. Do not unload deactivation window yet
6. Return Started and let AwaitRouteExitTeardownAsync wait
7. CompleteDeactivationWindow later calls unload
8. On pending operation completion, FinalizeDeactivationForRouteExit runs because active rail is RouteExit
```

---

### 3.5 `CompleteDeactivationWindow`

Arquivo/classe/método:

```text
NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs
SessionActivityPipeline.EmitCompleteDeactivationWindowAsync(...)
CompletePendingOperation(...)
```

Evidência de código:

```text
L2413-L2433  CompleteDeactivationWindow exige DeactivationWindowReady e descarrega a scene
L1848-L1856  no completion de DeactivationWindowSceneUnload, se rail RouteExit => FinalizeDeactivationForRouteExit
L1858-L1866  se não RouteExit => FinalizeDeactivationAndContinuation
```

Leitura:

- Este método é o ponto certo para continuar a saída após a janela.
- Ele já possui branch por `_activeRailKind == ActivityRouteExitRail` após unload.
- Isso é compatível com o patch recomendado para `RouteExitFromDeactivationWindowReady`.

Status:

```text
ALIGNED_AS_TARGET_CONTINUATION_POINT
```

Ação recomendada:

```text
Reusar este ponto. Não criar outro rail paralelo para completar RouteExit.
```

---

### 3.6 `AwaitRouteExitTeardownAsync`

Arquivo/classe/método:

```text
NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs
SessionActivityPipeline.AwaitRouteExitTeardownAsync(...)
```

Evidência de código:

```text
L8719-L8790  AwaitRouteExitTeardownAsync
L8730-L8740  rejeita stale/foreign sessionStateId
L8743-L8745  immediate result se já fechado
L8748-L8758  chama CloseForRouteExit e falha se rejeitado
L8761-L8768  registra completion pendente quando não fecha imediatamente
L8858-L8881  CompletePendingRouteExitTeardownIfAny completa await
```

Leitura:

- Boundary com Operational está conceitualmente correto.
- O problema não está no await; está no `CloseForRouteExit` interno.
- Depois que `CloseForRouteExit` for corrigido, este método deve continuar sendo o boundary correto.

Status:

```text
PARTIALLY_ALIGNED / WAITING_ON_INTERNAL_RAIL_FIX
```

Ação recomendada:

```text
Não mexer no Operational nem no AwaitRouteExitTeardownAsync no primeiro patch.
Corrigir primeiro o rail interno de CloseForRouteExit.
```

---

## 4. Matriz de ações recomendadas

| Corte | Escopo | Tipo | Risco | Justificativa |
|---|---|---|---:|---|
| `SA-8A3` | Corrigir `RouteExitFromActivityRunning` ordering | Runtime patch | Alto controlado | Maior divergência contra policy e maior impacto em BackToMenu. |
| `SA-8A4` | Corrigir `RouteExitFromDeactivationWindowReady` para pending intent | Runtime patch | Médio | Evita auto-complete da janela ativa. |
| `SA-8A5` | Corrigir `RestartCurrentActivity` contra skip policy explícita | Runtime patch | Alto | Restart hoje mistura teardown antes da window com window declarada. |
| `SA-8A6` | Adicionar observabilidade de policy escolhida | Runtime/log | Baixo | Depois dos patches, logs devem expor `exitOrderingPolicyId`. |
| `SA-8A7` | Só então avaliar extração de stage/policy | Auditoria/patch | Médio | Evita criar ActivityExitPipeline por simetria. |

Ordem recomendada:

```text
1. RouteExitFromActivityRunning
2. RouteExitFromDeactivationWindowReady
3. RestartCurrentActivity
4. Observabilidade policy id
5. Só depois discutir extração estrutural
```

Motivo da ordem:

- `RouteExit` é boundary com Operational e pode bloquear troca de rota.
- `Restart` é local ao SessionActivity e pode ser corrigido depois, desde que os smokes continuem cobrindo restart.

---

## 5. Perguntas obrigatórias

### Qual pipeline é dono desta decisão?

```text
SessionActivityPipeline.
```

A decisão de exit ordering é macro lifecycle da Activity. `ActivityEntryPipeline` está fora desse escopo.

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

```text
Policy + lifecycle orchestration.
```

A policy foi congelada em `ActivityExitOrderingPolicyContracts.cs`; os rails ativos ainda são lifecycle orchestration dentro de `SessionActivityPipeline`.

### Isso é comportamento final ou bridge transitória?

```text
A policy é comportamento final.
Os métodos ativos ainda são shape transitório até serem normalizados contra a policy.
```

### Essa compatibilidade ainda é necessária?

```text
Não.
Não preservar ordering atual por compatibilidade.
Nada aqui é produção.
```

### O erro está no sintoma ou na fronteira arquitetural errada?

```text
Está na fronteira de ordering/lifecycle.
Não é problema de log, nome ou tamanho de arquivo.
```

### Existe owner duplicado para o mesmo lifecycle?

```text
Não há owner duplicado formal, mas há rails divergentes dentro do mesmo owner.
Esse é o problema: um owner com múltiplas ordens incompatíveis.
```

---

## 6. Critério de aceite dos próximos patches

Qualquer patch runtime contra esta auditoria só pode ser aceito com smoke/log contendo:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem error CS

RestartCurrentActivity PASS
Activity01ToActivity02 PASS
RouteExitBackToMenu PASS

RouteExitFromActivityRunning:
- ActivityRouteExitRequested
- DeactivationWindowStarted/Ready antes de ActorPresentationReleaseStarted RouteExit
- CompleteDeactivationWindow antes de RouteExit Actor/Object teardown
- ActivityRouteExitCompleted antes do Operational aplicar rota seguinte

RouteExitFromDeactivationWindowReady:
- não inicia segunda DeactivationWindow
- não auto-completa antes de CompleteDeactivationWindow
- RouteExit fica pendente até CompleteDeactivationWindow

RestartCurrentActivity:
- policy explícita de restart observável
- sem ActorTeardown antes da decisão de DeactivationWindowRule
- nova entry só depois de release/content cleanup
```

---

## 7. Decisão final da auditoria

```text
SA-8A2 — AUDITED / NO RUNTIME CHANGE
ActivityExitPipeline — STILL NOT APPROVED
SA-8A3 should patch RouteExitFromActivityRunning ordering first.
```
