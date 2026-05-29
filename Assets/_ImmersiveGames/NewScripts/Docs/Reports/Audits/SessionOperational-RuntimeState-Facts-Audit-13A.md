# SessionOperationalPipeline — RuntimeState/Facts Owner Audit 13A

## Fonte

Snapshot atual analisado: `output.zip` + cortes aceitos até 12C no fluxo local de trabalho.

## Objetivo

Mapear quem ainda altera `SessionOperationalRuntimeState`, quem registra `SessionOperationalFact`, quem apenas emite observabilidade via log e onde ainda há owner duplicado de lifecycle/facts.

Este corte é auditoria + correção mecânica de compilação detectada localmente. Não muda política funcional.

## Resumo executivo

O `SessionOperationalPipeline` continua sendo o owner macro correto da ordem, lifecycle, policy e handoff da rota.

Depois dos cortes 11A-12C, os `Operational*Command` estão mais limpos e não devem carregar infraestrutura. Porém o ownership de `SessionOperationalRuntimeState` e `SessionOperationalFact` ainda não está completamente hegemonizado.

Atualmente há três pontos ativos que mutam `SessionOperationalRuntimeState`:

1. `SessionOperationalPipeline`
2. `OperationalInputPreparationStage`
3. `OperationalRouteCompletionStage`

Isso é melhor do que antes, mas ainda não é o shape final Base 2.0. O estado/fact ledger deve ter owner canônico único ou uma facade explícita de recorder.

## Matriz de ownership

| Arquivo / classe / método | Responsabilidade atual | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---:|---|---|---|
| `SessionOperationalPipeline.TryBeginRouteOperation` | Reseta runtime state e grava `RouteOperationStarted` | Pipeline ou recorder canônico | Pipeline ainda grava facts diretamente | Média | Manter por enquanto; futuro mover para `OperationalFactRecorder` | Médio se mexer agora | `_state.Reset`, `TryRecordStage` |
| `SessionOperationalPipeline.TryRecordStage` | Valida ordem via policy, cria `SessionOperationalIdentity`, cria `SessionOperationalFact`, atualiza state e trace | `OperationalFactRecorder` futuro | Pipeline contém implementação de fact/state, além da ordem | Alta arquitetural | Extrair depois para recorder injetado, sem mudar ordem | Alto | `_state.SetCurrentIdentity`, `AppendFact`, `AppendTrace` |
| `SessionOperationalPipeline.Reject` | Registra reject/stale/foreign como fact/trace | `OperationalFactRecorder` futuro | Rejeição de stage/fact está colada no pipeline | Média | Extrair junto com `TryRecordStage` | Médio | `_state.AppendFact`, `_state.AppendTrace` |
| `OperationalInputPreparationStage.RecordInputStageOrReject` | Registra `InputCapabilityPrepared` e `InitialInputModePrepared` no runtime state | Stage pode produzir facts; recorder deve persistir facts | Stage grava direto no estado global | Alta | No próximo corte, stage deve retornar facts/result; recorder aplica | Médio/alto | `_runtimeState.SetCurrentIdentity`, `AppendFact` |
| `OperationalInputPreparationStage.SubmitInitialInputModeOrFail` | Lê state para garantir que `InitialInputModePrepared` ocorreu antes do side-effect | Policy/recorder/snapshot da etapa | Stage depende do estado global para validar ordem | Média | Substituir por resultado local do próprio stage ou recorder query explícita | Médio | `_runtimeState.CurrentStage`, `RouteOperationId`, `TransitionId` |
| `OperationalInputPreparationStage.Reject` | Registra fact de reject no runtime state | `OperationalFactRecorder` futuro | Duplicação do reject do pipeline | Alta | Remover duplicação no corte de recorder | Médio | `_runtimeState.AppendFact`, `AppendTrace` |
| `OperationalRouteCompletionStage.ApplyCompletedState` | Marca state completed e cria result final | Completion stage + recorder canônico | Stage grava direto no runtime state e monta resultado a partir de `_runtimeState.Facts` | Alta | Stage retorna completion fact/snapshot; recorder aplica e constrói result | Alto | `_runtimeState.MarkCompleted`, `AppendFact`, `_runtimeState.Facts` |
| `SessionOperationalRuntimeState` | Guarda estado mutável e lista de facts/trace | State container técnico | API permite múltiplos writers diretos | Alta | Tornar mutação interna ao recorder ou reduzir setters públicos/internal | Médio | `SetCurrentIdentity`, `AppendFact`, `AppendTrace` |
| Demais `Operational*Stage` | Executam side-effects/policies e logam eventos OBS | Stage/adapters | Logs não são facts canônicos | Baixa/média | Manter por enquanto; futuro decidir quais logs viram facts reais | Baixo | `DebugUtility.Log` apenas |

## Achados principais

### 1. Não há mais command carregando infraestrutura operacional

Após 12B/12C, a auditoria textual dos `Operational*Command` não encontrou propriedades de `Stage`, `Boundary`, `Adapter`, `Port`, `Func<T>` ou `RuntimeState`.

Status: **limpo o suficiente para seguir**.

### 2. O ledger de facts ainda tem três writers

Os writers atuais são:

```text
SessionOperationalPipeline
OperationalInputPreparationStage
OperationalRouteCompletionStage
```

Isto ainda é owner duplicado parcial. O problema não é funcional imediato, mas arquitetural: cada writer pode divergir em identity, policy de ordem, stale/foreign e trace.

### 3. `OperationalInputPreparationStage` mistura stage, policy, fact e adapter

Ele executa quatro coisas:

1. resolve input mode por policy;
2. prepara runtime de input;
3. registra facts no state;
4. submete side-effect para `IOperationalInputModeRequestPort`.

O shape final deveria separar pelo menos:

```text
Input policy/resolve -> stage result/facts -> recorder -> adapter side-effect
```

Não é obrigatório corrigir agora, mas é o próximo ponto com melhor custo-benefício.

### 4. `OperationalRouteCompletionStage` ainda é owner final do state completed

Ele monta o `SessionOperationalResult` usando `_runtimeState.Facts`. Isso mantém acoplamento forte entre completion stage e state container.

A correção final deve esperar um `OperationalFactRecorder`, para evitar mover o problema para o pipeline.

### 5. Correção mecânica detectada

No arquivo `OperationalInputPreparationStage.cs`, o método `Reject(...)` estava marcado como `static`, mas acessava `_runtimeState`. Isso é incompatível com C# e foi corrigido para método de instância.

Essa correção não altera comportamento runtime; apenas corrige a declaração do método.

## Respostas obrigatórias

| Pergunta | Resposta |
|---|---|
| Qual pipeline é dono desta decisão? | `SessionOperationalPipeline` continua dono da ordem/lifecycle/handoff da rota. O registro factual deveria ser delegado a um recorder canônico, não espalhado por stages. |
| Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data? | `SessionOperationalRuntimeState` é snapshot/state runtime técnico; `SessionOperationalFact` é fact; `TryRecordStage`/`Reject` deveriam virar recorder; `OperationalInputPreparationStage` e `OperationalRouteCompletionStage` são stages que hoje também escrevem facts. |
| Isso é comportamento final ou bridge transitória? | O estado atual é bridge transitória. Funciona, mas ainda não é owner único de facts/state. |
| Essa compatibilidade ainda é necessária? | Compatibilidade externa não é necessária. A preservação atual é apenas para reduzir risco entre cortes. |
| O erro está no sintoma ou na fronteira arquitetural errada? | Está na fronteira: facts/state são gravados por múltiplos owners. Corrigir sintomas locais criaria outro seam. |
| Existe owner duplicado para o mesmo lifecycle? | Sim. Pipeline, input stage e completion stage ainda podem alterar `SessionOperationalRuntimeState`/facts. |

## Próximo corte recomendado

### Corte 13B — `OperationalFactRecorder` passivo

Criar um recorder canônico pequeno e não intrusivo:

```text
OperationalFactRecorder
- ResetOperation(...)
- TryRecordStage(...)
- Reject(...)
- MarkCompleted(...)
- DumpState()
```

Primeiro corte recomendado:

1. Extrair `TryRecordStage`, `Reject`, `CanAcceptStage`, `BuildTransitionKey`, `MapFactKind` do `SessionOperationalPipeline` para `OperationalFactRecorder`.
2. O pipeline continua chamando o recorder nos mesmos pontos.
3. Não migrar ainda `OperationalInputPreparationStage` nem `OperationalRouteCompletionStage` no mesmo patch.
4. Smoke obrigatório depois.

Motivo: reduz owner duplicado sem misturar Input/Completion no mesmo corte.

## Critério de aceite futuro para 13B

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
RouteOperationStarted preservado
InputCapabilityPrepared preservado
InitialInputModePrepared preservado
OperationalRouteCompleted preservado
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```
