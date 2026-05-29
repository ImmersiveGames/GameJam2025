# SessionOperationalPipeline — Auditoria de Hegemonização Pós-11

## Fonte analisada

- Snapshot base enviado como `output.zip`.
- Patches/diffs locais dos cortes 11A–13D.
- Smokes enviados após 11B, 11C, 11D, 11E, 12A, 12B, 12C, 13B e 13C.

Esta auditoria **não altera código**. O objetivo é interromper o ciclo de empurrar responsabilidade entre classes e estabilizar a fronteira arquitetural antes de qualquer novo patch.

## Resumo executivo

Até o corte 11, a decomposição tinha uma direção arquitetural consistente:

```text
OperationalRouteMaterializationStage deixou de ser mini-pipeline.
SessionOperationalPipeline voltou a explicitar a ordem.
Stages continuaram executando passos determinísticos.
Commands deixaram de carregar sub-stages.
```

Depois do corte 11, parte do trabalho continuou válida em intenção, mas começou a surgir um segundo padrão perigoso:

```text
Classe A parece grande
-> move responsabilidade para Classe B
-> Classe B parece grande
-> cria Classe C
-> repete
```

Isso não é decomposição estrutural. É deslocamento de responsabilidade.

## Veredito macro

| Faixa | Veredito |
|---|---|
| 11A–11E | Arquiteturalmente consistente. Pode ser tratado como último checkpoint estrutural confiável. |
| 12A | Provável correção válida, mas precisa ser revalidada pela nova regra anti-deslocamento. |
| 12B | Princípio correto: command não carrega infraestrutura. Execução foi ampla demais e gerou regressão de compilação. |
| 12C | Parcialmente correto, mas já toca em ownership de state/facts de forma ambígua. |
| 13A | Válido como auditoria. |
| 13B | Aceitável apenas como recorder passivo; perigoso como owner de state/order. |
| 13C | Funcionalmente passou smoke, mas arquiteturalmente suspeito: `OperationalFactRecorder` passou a construir command/result. |
| 13D | Rejeitado. Move lifecycle/reset/dump para recorder e confirma deslocamento de responsabilidade. |

## Último ponto seguro

### Último checkpoint arquitetural confiável

```text
Corte 11E — Materialization boundary cleanup
```

Motivo: ele corrige uma fronteira real. `OperationalRouteMaterializationStage` não era um stage determinístico; era um composite/mini-pipeline. Renomear para boundary e deixar a ordem explícita no `SessionOperationalPipeline` está alinhado à Base 2.0.

### Último checkpoint funcional observado

```text
Corte 13C — FactRecorder migration
```

Motivo: passou smoke. Porém **não deve ser aceito como shape final** sem revisão, porque o `OperationalFactRecorder` acumulou responsabilidades além de fact/trace.

## Matriz de cortes pós-11

| Corte | Responsabilidade atual | Categoria correta | Owner correto | Problema | Severidade | Ação recomendada | Risco | Evidência |
|---|---|---|---|---|---:|---|---|---|
| 11E — Materialization boundary cleanup | Renomeia `OperationalRouteMaterializationStage` para `OperationalRouteMaterializationBoundary`; mantém begin/complete de materialization. | Boundary/fact scope | `SessionOperationalPipeline` decide ordem; boundary registra início/fim. | Sem problema relevante. | Baixa | Manter. | Baixo | Remove composite stage e preserva logs `OperationalRouteMaterializationStarted/Completed`. |
| 12A — PreviousRouteExit flattening | Troca `OperationalPreviousRouteExitStage` por `OperationalPreviousRouteExitBoundary`; pipeline explicita handoff/release/camera/save. | Boundary + stages explícitos | Pipeline para ordem; stages para passos. | Pode ser válido, mas precisa confirmar que boundary não decide lifecycle/policy. | Média | Reauditar implementação; manter apenas se boundary for begin/complete puro. | Médio | O smoke passou, mas a motivação deve ser owner de ordem, não reduzir arquivo. |
| 12B — Command hygiene | Remove `Stage`, `Boundary`, `Adapter`, `Port`, `Func<T>` de vários commands; dependências vão para stages. | Command hygiene | Commands carregam payload; stages carregam dependência técnica. | Princípio correto, execução ampla e regressiva; compile quebrou duas vezes. | Média/Alta | Manter a regra; revisar patch em fatias menores por família de stage. | Médio | Diffs alteram muitos stages e exigiram correções mecânicas. |
| 12C — RuntimeState hygiene | Remove `SessionOperationalRuntimeState` de commands de Input e Completion. | Command hygiene / state ownership | Commands não devem carregar state; writer de state precisa ser explícito. | Correto no command, mas desloca o problema para stage/recorder sem resolver owner final. | Média | Não avançar nesse eixo sem ADR. | Médio | Stage/recorder passam a concentrar escrita/consulta de state. |
| 13A — RuntimeState/Facts audit | Mapeia writers de `RuntimeState`/facts. | Auditoria | Documento, não runtime. | Válido. | Baixa | Manter. | Baixo | Produz matriz de owners e evidencia writers duplicados. |
| 13B — OperationalFactRecorder passivo | Extrai `TryRecordStage`, `Reject`, `CanAcceptStage`, `BuildTransitionKey`, `MapFactKind`. | Fact recorder + policy guard | Fact recorder só deveria registrar facts; policy guard deveria ficar explícito. | Começa aceitável, mas mistura recorder com stage-order/stale/foreign. | Média | Manter apenas como transitório; não acrescentar lifecycle nele. | Médio | `OperationalFactRecorder` usa `SessionOperationalStageOrderPolicy` e escreve state. |
| 13C — FactRecorder migration | Move InputPreparation e RouteCompletion para usar recorder. | Fact/result/input command misturados | Input stage deveria produzir result; completion deveria fechar lifecycle; recorder deveria gravar fact. | `OperationalFactRecorder` passa a construir `OperationalInputModeRequest` e `SessionOperationalResult`. Isso cruza fact com command/result. | Alta | Não congelar como arquitetura final. Reverter ou redesenhar antes de continuar. | Alto | Métodos `BuildInitialInputModeRequestOrFail` e `BuildResult` aparecem no recorder. |
| 13D — RuntimeState lifecycle through recorder | Move `TryBeginRouteOperation`, `_state.Reset(...)` e `DumpState` para recorder. | Lifecycle/state lifecycle | Pipeline deve decidir begin/reset; recorder não deve comandar lifecycle. | Confirma o ciclo de deslocamento. | Crítica | Não aplicar. | Alto | `TryBeginRouteOperation` e `_state.Reset(...)` iriam para o recorder. |

## Achados principais

### 1. O problema pós-11 não é tamanho de arquivo

O problema é a ausência de uma regra rígida de ownership antes da extração.

Extração válida:

```text
SessionOperationalPipeline chama Stage.Execute(command)
Stage executa passo determinístico
Adapter executa side-effect
FactRecorder registra fato
```

Extração inválida:

```text
SessionOperationalPipeline está grande
-> mover bloco para qualquer classe nova
-> classe nova começa a decidir lifecycle/policy/command/result
```

### 2. O `OperationalFactRecorder` começou a ultrapassar a fronteira

Como fact recorder, ele poderia registrar fatos e traces.

Ele não deveria construir:

```text
OperationalInputModeRequest
SessionOperationalResult
Route operation lifecycle begin/reset
```

Esses itens são command/result/lifecycle, não fact.

### 3. `PreviousRouteExitBoundary` pode estar correto, mas deve ser tratado como suspeito até reauditoria

A ideia de achatar `PreviousRouteExit` é coerente se o objetivo for remover mini-pipeline. Porém, para ser final, o boundary precisa ser estritamente:

```text
Begin previous route exit
Complete previous route exit
```

Se ele tiver qualquer decisão de handoff, save, camera, consumer release ou policy, vira novo seam.

### 4. Command hygiene é regra final, mas não precisa gerar layer novo

O command não deve carregar infra. Porém isso não significa que todo dependency resolver precisa virar classe nova. Às vezes a dependência técnica pertence ao stage, e pronto.

### 5. Smoke não é arquitetura

Os smokes validaram comportamento funcional, mas não validam ownership final.

Logo:

```text
PASS funcional != PASS arquitetural
```

## Regras anti-ciclo infinito

Uma extração só pode ser feita quando todas as perguntas abaixo tiverem resposta objetiva:

1. Isto é pipeline lifecycle, stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?
2. Qual é o owner final dessa decisão?
3. A extração remove um owner duplicado ou apenas troca o owner de nome?
4. O novo componente tem uma fronteira menor e mais estável que a anterior?
5. O novo componente consegue ser descrito sem usar “gerencia”, “coordena”, “orquestra”, “controla” ou “processa” de forma genérica?
6. O patch reduz trilho paralelo ou cria mais um?
7. O command continua payload puro?
8. O stage continua determinístico?
9. O adapter continua sendo o único executor de side-effects?
10. O pipeline continua sendo o único owner de ordem/lifecycle/handoff?

Se qualquer resposta for ambígua, **não implementar**. Fazer auditoria primeiro.

## Decisão recomendada

### Congelar como arquitetura segura

```text
Corte 11E
```

### Tratar como funcional/provisório, não final

```text
12A, 12B, 12C, 13B, 13C
```

### Não aplicar / descartar

```text
13D
```

### Próximo trabalho permitido

Somente documentação/auditoria até o ADR estar aceito.

Depois do ADR, o próximo patch deve ser escolhido por matriz de owner, não por tamanho de arquivo.

## Próximo corte técnico permitido após ADR

Não há corte técnico autorizado automaticamente.

A próxima ação deve ser uma destas:

1. Reverter para 11E e reintroduzir apenas partes pós-11 que passem pela matriz.
2. Manter 13C como branch funcional temporário, mas auditar cada componente antes de aceitar como final.
3. Criar um patch documental adicionando o ADR e esta auditoria ao projeto.

## Critério de PASS arquitetural futuro

Um corte só pode ser marcado como PASS arquitetural se cumprir:

```text
sem fallback silencioso
sem trilho paralelo novo
sem command carregando infra
sem stage orquestrando sub-stages
sem recorder decidindo lifecycle/policy
sem adapter decidindo lifecycle/policy
sem owner duplicado para lifecycle
pipeline mantém ordem/lifecycle/handoff
stage executa passo determinístico
policy classifica decisões/skips/failures
fact registra ocorrido, não executa
adapter executa side-effect comandado
smoke/log confirma comportamento
```
