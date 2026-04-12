# ADR-0051 - Fluxo canonico de continuidade apos o fechamento da run

## Status
- Estado: Aceito
- Data: 2026-04-12
- Tipo: Direction / Canonical architecture

## 1. Contexto

Os ADRs atuais ja separam o centro semantico do gameplay, o pipeline de montagem da phase e o fim de run canonico:

- `ADR-0001` define o vocabulario fundamental de contexto, rota, resultado e intencao.
- `ADR-0044` estabelece o centro semantico do gameplay e a separacao entre resultado, continuidade e navegacao.
- `ADR-0045` consolida o `Gameplay Runtime Composition` como centro semantico do gameplay.
- `ADR-0047` fixa o pipeline canonico da phase dentro de `GameplaySessionFlow`.
- `ADR-0049` fecha o contrato canonico de fim de run com `RunEndIntent`, `RunResultStage` e `RunDecision`.
- `ADR-0050` fixa a `IntroStage` como rail local de entrada da phase apos `SceneTransitionCompleted`.

Na pratica, o runtime atual ja distingue o fechamento da run, consolida um resultado e abre o rail de pos-run. O ponto que ainda precisa de formalizacao canonica e a separacao entre:

1. o motivo do fechamento da run atual
2. o contexto canonico de continuidade apos o fechamento
3. a decisao ou apresentacao opcional dessa continuidade
4. a execucao downstream da continuidade escolhida
5. a navegacao pura de phase, mantida como dominio separado

Este ADR formaliza esse boundary sem detalhar implementacao.

## 2. Problema

Hoje a continuacao apos o fechamento da run ainda pode ser lida a partir de payload estreito demais.

Mesmo com `RunEndIntent`, `RunResult` e `RunDecision`, falta um contrato canonico explicito que carregue a semantica de continuidade apos o fechamento e sirva como fonte de verdade para:

- quais continuidades estao permitidas
- se a continuidade exige decisao do jogador
- qual continuidade ja foi selecionada por regra
- como tratar casos automaticos e casos com escolha
- como manter `PhaseNavigation` neutra em relacao ao fechamento da run

Sem esse contrato, o sistema tende a misturar:

- motivo de fechamento
- decisao de continuidade
- apresentacao da continuidade
- navegacao de phase
- execucao downstream

Essa mistura enfraquece o boundary entre o fim de run e a progressao editorial ou operacional da phase.

## 3. Decisao

Este ADR congela a existencia de um contexto canonico de continuidade apos o fechamento da run.

Nome canonico recomendado:

- `RunContinuationContext`

`RunContinuationContext` e o contrato que nasce depois do fechamento semantico da run e centraliza a continuidade possivel daquela run encerrada.

Payload minimo esperado de `RunContinuationContext`:

- `closing reason`
- `allowed continuations`
- `requires player decision`
- `selected continuation` opcional ate ser resolvida

Ele nao substitui `RunEndIntent`.
Ele nao substitui `RunResult`.
Ele nao substitui `RunDecision`.

Ele existe para representar a continuidade canonica disponivel apos o fechamento, com ownership semantico explicito e sem depender de UI local, de `PhaseNavigation` ou de heuristica de presenter.

## 4. Boundaries e ownership

### Ownership canonico

- `RunContinuationContext` e owned por `GameplaySessionFlow` / centro semantico do gameplay.
- `RunResultStage` e `RunDecision` consomem o contexto, mas nao sao owners da semantica de continuidade.

### Boundaries proibidos

Nao e permitido colocar a semantica de continuidade em:

- `PhaseNavigation`
- UI/presenter local
- catalogo de phase
- `RunResultStage` local
- executor downstream

### Separacao obrigatoria

O contrato canonico deve separar explicitamente:

1. motivo do fechamento da run
2. contexto de continuidade
3. continuidades permitidas
4. continuidade final selecionada
5. execucao downstream da continuidade
6. navegacao pura de phase

## 5. Fluxo canonico

O fluxo canonico passa a ser lido assim:

1. a run fecha semanticamente
2. `RunContinuationContext` e materializado
3. `RunResultStage`, quando existir, consome o contexto
4. `RunDecision`, quando existir ou quando necessario, consome o contexto
5. a continuidade final e escolhida ou confirmada
6. a continuidade resolvida e executada downstream
7. `PhaseNavigation` so entra quando a continuidade escolhida for explicitamente `AdvancePhase`

Este fluxo continua valido quando `RunResultStage` e pulado.
O contexto de continuidade existe mesmo no caminho `skip/no-content`, porque o fechamento da run ainda precisa carregar a semantica de continuidade.

## 6. Responsabilidades por componente

### `GameplaySessionFlow`

- materializa `RunContinuationContext`
- mantem o ownership semantico do fechamento da run e da continuidade
- fornece o contexto aos consumidores corretos

### `RunEndIntent`

- continua sendo o ato de encerramento da run atual
- carrega o fechamento inicial, nao o contrato completo de continuidade

### `RunResultStage`

- permanece um stage opcional e phase-owned
- pode consumir o contexto
- nao decide a semantica de continuidade
- nao deve ser a origem da continuidade

### `RunDecision`

- consome `RunContinuationContext`
- pode escolher uma continuidade
- ou pode apenas confirmar uma continuidade ja resolvida por regra
- nao deve inventar a semantica localmente a partir de payload estreito

### Execucao downstream

- executa a continuidade escolhida ou confirmada
- nao define o contrato semantico
- nao substitui o contexto

### `PhaseNavigation`

- continua sendo navegacao pura e editorial
- nao carrega semantica de fechamento de run
- so e acionada quando a continuidade for explicitamente `AdvancePhase`

## 7. Continuidade minima de v1

As continuidades minimas de v1 sao:

- `AdvancePhase`
- `RestartCurrentPhase`
- `ExitToMenu`
- `EndRun`

Leitura canonica:

- `Victory`, `Defeat` e `ExitToMenu` sao motivos de fechamento, nao destinos automaticos
- o destino da continuidade e decidido depois do fechamento, por contrato de continuidade
- `EndRun` nao e sinônimo automatico de `Defeat`
- `EndRun` representa o encerramento terminal da continuidade, nao um atalho de `PhaseNavigation`

## 8. Regras e invariantes

- `RunContinuationContext` nasce apos o fechamento semantico da run
- `RunContinuationContext` e canonicamente owned por `GameplaySessionFlow`
- `RunResultStage` e `RunDecision` consomem o contexto, nao o definem
- `RunDecision` pode escolher ou confirmar uma continuidade
- `RunDecision` nao define o conjunto canonico de continuidades validas
- o contexto deve suportar continuidades automaticas e continuidades com decisao
- `RunResultStage` continua opcional e sua ausencia nao invalida o contexto
- `PhaseNavigation` nao deve carregar semantica de fechamento de run
- o executor downstream nao deve ser o lugar de definicao do contrato semantico
- a decisao de continuidade nao deve morar em presenter local
- a decisao de continuidade nao deve morar em catalogo de phase

## 9. Consequencias

- o fim de run passa a ter um contrato explicito entre fechamento e continuidade
- `RunDecision` deixa de depender de payload estreito para inferir significado
- continuidades automaticas e com escolha ficam no mesmo modelo sem colapsar semantica
- `RunResultStage` pode continuar opcional sem enfraquecer o boundary
- `PhaseNavigation` permanece limpa e neutra
- a execucao downstream fica separada do contexto semantico

## 10. Nao objetivos

Este ADR nao define:

- implementacao concreta de presenter
- UI especifica de decisao
- mecanica de animacao ou overlay
- codigo de navegacao
- regras detalhadas de dispatch
- mapping de asset ou catalogo

Este ADR tambem nao reabre o contrato de `IntroStage`.
Ele apenas fecha o boundary de continuidade apos o fechamento da run.

## 11. Relacao com ADRs existentes

- `ADR-0001`: vocabulario fundamental para resultado, intencao e contexto
- `ADR-0044`: coluna dorsal do runtime e leitura canonica de resultado e continuidade
- `ADR-0045`: centro semantico do gameplay
- `ADR-0047`: pipeline canonico da phase dentro de `GameplaySessionFlow`
- `ADR-0049`: fim de run canonico com `RunEndIntent`, `RunResultStage` e `RunDecision`
- `ADR-0050`: `IntroStage` canonica e separacao da entrada da phase

Este ADR complementa `ADR-0049` sem substitui-lo.
Ele formaliza o que acontece depois do fechamento semantico da run e antes da execucao downstream da continuidade.

## 12. Proximos passos

1. Introduzir o contrato concreto de `RunContinuationContext` no dominio adequado.
2. Adaptar os consumidores para ler o contexto de continuidade, e nao apenas `RunEndIntent` + `RunResult`.
3. Separar claramente escolha de continuidade, apresentacao opcional e execucao downstream.
4. Manter `PhaseNavigation` como dominio neutro e editorial.
