# Base 1.1 - Activity Execution Executors Checkpoint

**Data**: 2026-05-12  
**Status**: Checkpoint consolidado  
**Escopo**: Activity Execution Executors / InputModes / ActivityExecutionBlocking  
**Fonte operacional**: Auditoria `Base-1.1-Activity-Execution-Executors-Audit.md` e patches subsequentes.

---

## 1. Objetivo

Consolidar a higienização de ownership dos executores de activity na Base 1.1.

Esta frente teve como objetivo garantir que:

```text
Pipelines decidem lifecycle, ordem, policies e handoffs.
Executores aplicam estado/efeitos comandados.
Adapters executam fronteiras externas.
Foreign/stale events não alteram execução ativa.
```

---

## 2. Escopo tratado

Foram tratados os três pontos reais encontrados na auditoria:

| Peça | Problema encontrado | Resultado |
|---|---|---|
| `SessionOperationalInputModeAdapter` | Adapter decidia policy de `InitialInputMode != FrontendMenu => observed_noop` | Policy movida para `SessionOperationalPipeline`; adapter virou conversor técnico |
| `InputModeCoordinator` | Coordinator deduplicava requests por frame/chave, suprimindo request válido | Dedupe local removido; coordinator delega todo request válido |
| `SessionActivitySimulationGate` | Gate rejeitava duplicate block/release como decisão de transição local | Duplicidade virou idempotência técnica aceita |

---

## 3. Decisões consolidadas

### 3.1 InputMode policy pertence ao pipeline

`SessionOperationalInputModeAdapter` não decide mais se um modo inicial deve ou não emitir request.

A decisão agora pertence ao owner semântico:

```text
SessionOperationalPipeline
```

O adapter mantém apenas:

```text
- validação estrutural mínima;
- guarda anti-foreign/stale por identidade;
- conversão de SessionOperationalInputModeCommand para InputModeRequestEvent;
- report de resultado.
```

Conversão explícita:

```text
FrontendMenu -> InputModeRequestKind.FrontendMenu
ActivityDefault -> InputModeRequestKind.Gameplay
valor não suportado -> InvalidOperationException
```

A policy atual fica no pipeline:

```text
CurrentInitialInputMode == FrontendMenu
    -> emite SessionOperationalInputModeCommand

outros modos
    -> observed_noop reason='initial_input_mode_policy'
```

---

### 3.2 InputModeCoordinator é executor/orquestrador técnico

`InputModeCoordinator` não aplica mais policy temporal de dedupe.

Removido:

```text
_lastRequestFrame
_lastRequestKey
InputModeRequestDeduped
supressão por Time.frameCount + requestKey
```

O coordinator agora faz apenas:

```text
InputModeRequestEvent válido
-> IInputModeService
-> InputModeRequestDelegated
```

Request estruturalmente inválido continua falhando explicitamente.

Ausência de `IInputModeService` obrigatório continua sendo erro fail-fast.

---

### 3.3 SessionActivitySimulationGate não decide lifecycle

`SessionActivitySimulationGate` permanece como executor técnico de estado/efeito.

Ele mantém:

```text
- estado técnico mínimo;
- proteção anti-foreign/stale;
- validação estrutural;
- report de fact/result.
```

Ele não deve mais tratar duplicate block/release como decisão local de lifecycle.

Novo comportamento idempotente:

```text
BlockActivityExecution já bloqueado com mesma identidade
-> accepted
-> fact ActivityExecutionBlocked
-> mensagem: Idempotent no-op applied

ReleaseActivityExecution já liberado
-> accepted
-> fact ActivityExecutionReleased
-> mensagem: Idempotent no-op applied
```

Permanece rejeição para:

```text
- stale_or_foreign_gate_command;
- command sem identidade válida;
- command unknown/inválido.
```

---

## 4. Ownership final

| Área | Owner de decisão | Executor / Adapter | Papel final |
|---|---|---|---|
| Initial input mode | `SessionOperationalPipeline` | `SessionOperationalInputModeAdapter` | Adapter de fronteira para request de input |
| Aplicação de input mode | Pipeline emissor do command | `InputModeCoordinator` + `IInputModeService` | Delegação técnica sem dedupe local |
| Blocking/release de activity execution | `SessionActivityPipeline` | `SessionActivitySimulationGate` | Executor idempotente com guarda de identidade |
| Proteção contra foreign/stale | Pipeline + validação de identidade no executor | Executors/adapters | Bloquear eventos incompatíveis sem criar policy própria |

---

## 5. Invariantes preservadas

- Pipeline decide lifecycle.
- Adapter não cria policy própria.
- Executor não decide lifecycle.
- InputMode não infere modo por conveniência local.
- Gate não libera/bloqueia por readiness própria.
- Duplicate block/release é idempotência técnica, não decisão semântica.
- Foreign/stale commands continuam rejeitados.
- Request inválido continua falhando explicitamente.
- Sem fallback silencioso.
- Sem compat paralelo.

---

## 6. Arquivos alterados nesta frente

```text
Assets/_ImmersiveGames/NewScripts/InputModes/Runtime/SessionOperationalInputModeAdapter.cs
Assets/_ImmersiveGames/NewScripts/SessionOperational/Pipeline/SessionOperationalPipeline.cs
Assets/_ImmersiveGames/NewScripts/InputModes/Runtime/InputModeCoordinator.cs
Assets/_ImmersiveGames/NewScripts/SessionActivity/Simulation/SessionActivitySimulationGate.cs
```

---

## 7. Validações manuais pendentes

### 7.1 InputMode inicial

Fluxo com `InitialInputMode=FrontendMenu`:

```text
- confirmar emissão de SessionOperationalInputModeCommand no pipeline;
- confirmar adapter publicando InputModeRequestEvent FrontendMenu;
- confirmar InputModeCoordinator delegando para IInputModeService.
```

Fluxo com `InitialInputMode=ActivityDefault`:

```text
- confirmar observed_noop no pipeline;
- confirmar reason='initial_input_mode_policy';
- confirmar ausência de SessionOperationalInputModeCommand.
```

### 7.2 InputModeCoordinator

Enviar dois `InputModeRequestEvent` idênticos no mesmo frame:

```text
- ambos devem ser delegados;
- não deve existir InputModeRequestDeduped.
```

Enviar request inválido:

```text
- Kind=Unspecified deve falhar explicitamente.
```

### 7.3 ActivityExecutionBlocking

Enviar `BlockActivityExecution` duas vezes com mesma identidade:

```text
- primeira chamada bloqueia;
- segunda chamada retorna accepted idempotente.
```

Enviar `ReleaseActivityExecution` duas vezes com mesma identidade:

```text
- primeira chamada libera;
- segunda chamada retorna accepted idempotente.
```

Enviar command com identidade diferente da ativa:

```text
- deve rejeitar como stale_or_foreign_gate_command.
```

### 7.4 Varredura de nomes antigos

Verificar se não restaram resíduos indevidos:

```text
GameRun
GamePlay
PauseStateChangedEvent
SessionActivitySimulationState
CurrentSimulationState
SetSimulationState
SimulationGateCommand
SimulationGateIdentity
SimulationGateResult
SimulationGateState
SimulationGateFactKind
BlockSessionSimulation
ReleaseSessionSimulation
ActivitySimulationBlocked
SessionSimulationBlocked
```

Exceção permitida nesta rodada:

```text
SessionActivitySimulationGate
```

---

## 8. Resultado do checkpoint

A frente **Activity Execution Executors** está fechada em nível de ownership inicial.

Estado final:

```text
SessionOperationalPipeline decide input operacional.
SessionOperationalInputModeAdapter aplica fronteira técnica.
InputModeCoordinator delega request válido.
SessionActivityPipeline decide blocking/release.
SessionActivitySimulationGate aplica estado idempotente.
```

A Base 1.1 agora tem um chão mais limpo para a próxima frente:

```text
Actor Preparation Flow
```

---

## 9. Próxima frente recomendada

Após validações manuais, retomar:

```text
ADR-0009 - Actor Preparation Flow e Actor Activity
```

Próximo passo sugerido:

```text
Auditoria focada de actors/player/materialization/readiness/bindings
-> matriz owner antigo -> owner final -> papel final -> ação necessária
```
