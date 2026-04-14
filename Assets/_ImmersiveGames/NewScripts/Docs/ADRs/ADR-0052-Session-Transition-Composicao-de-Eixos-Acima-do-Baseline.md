# ADR-0052 - Session Transition: composicao de eixos acima do baseline

## Status
- Estado: Proposto
- Data: 2026-04-13
- Tipo: Direction / Architectural shape

## 1. Problema

O baseline atual ja resolve:

- fim de run
- `RunContinuationContext`
- `RunResultStage`
- `RunDecision`
- selecao / confirmacao
- execucao downstream da continuidade escolhida

O proximo problema nao e mais decidir `o que continua`, e sim estruturar `como a sessao/runtime se transforma` para sustentar continuidades diferentes sem colapsar varios eixos num contrato unico.

Sem essa camada acima do baseline, a base tende a misturar:

- continuidade
- reset / restart
- world reconstruction
- phase transition
- content / spawn transition
- carry-over de atores e objetos

## 2. Nome recomendado

`Session Transition`

Em codigo, a leitura pode ser `SessionTransition`.

## 3. Papel da camada

Esta camada recebe a continuidade ja consolidada pelo baseline e decide a transformacao operacional da sessao/runtime que vai sustentala.

Ela nao substitui o baseline. Ela compoe politicas e eixos para produzir uma transformacao executavel, deterministica e modular.

Em termos simples:

- baseline: decide `o que continua`
- `Session Transition`: decide `como o runtime/sessao muda para suportar essa continuidade`

## 3.1 Precedencia e escopo

Este ADR e o owner canonico da camada acima do baseline para transformacao composta de sessao/runtime.

`ADR-0045`, `ADR-0046`, `ADR-0047` e `ADR-0048` continuam validos, mas apenas no escopo phase-side / baseline-side que ja descrevem.

Eles nao devem mais ser lidos como owner da composicao acima do baseline para:

- continuidade
- reset / restart
- world reconstruction
- content / spawn transition
- actor / object carry-over
- transformacao composta da sessao/runtime apos a continuidade resolvida

## 4. Boundaries

### Fica no baseline

- `RunEndIntent`
- `RunContinuationContext`
- `RunResultStage`
- `RunDecision`
- selecao e confirmacao da continuidade
- execucao downstream da continuidade ja escolhida

### Sobe para `Session Transition`

- composicao da transformacao da sessao/runtime
- coordenacao entre reset, reconstruicao de mundo e transicao de fase
- politica de content / spawn
- politica de carry-over de atores/objetos
- ordenacao deterministica dos eixos de transformacao
- resolucao de qual combinacao de politicas deve ser aplicada em cada jogo

## 5. Artefato central recomendado

O artefato central deve ser um `SessionTransitionPlan` composto, nao um enum gigante nem um contrato monolitico.

Recomendacao:

- `SessionTransitionContext`: entrada normalizada e dados de politica
- `SessionTransitionPlan`: plano executavel composto por subplanos por eixo
- `SessionTransitionOrchestrator`: executor fino do plano, sem virar dono semantico de tudo

O centro da camada deve ser o `plan`. O orchestrator executa; o context alimenta.

## 6. Eixos minimos

A camada deve compor, no minimo, estes eixos independentes:

1. continuidade
2. reset / restart
3. world reconstruction
4. content / spawn transition
5. carry-over de atores / objetos

Cada eixo precisa poder existir, ser omitido ou variar sem forcar os outros a mudar de forma.

## 7. Exemplos de combinacoes

1. Continuity only: continua a sessao, sem reset, sem reconstruir mundo, com carry-over total.
2. Continuity + full world reset: mesma continuidade macro, mas o mundo volta zero antes da proxima fase.
3. Continuity + partial reconstruction: reconstrucao de partes do mundo com preservacao seletiva de atores/objetos.
4. Continuity + phase transition + content swap: troca de fase com novo conjunto de spawn/content, sem reset global.
5. Continuity + reset + spawn rebuild + partial carry-over: reinicio controlado, mas preservando inventario, flags ou atores definidos pela politica do jogo.

## 8. Como evitar o contrato gigante

O desenho deve separar explicitamente:

- contrato de continuidade
- composicao de politicas
- transformacao de runtime/sessao
- execucao operacional

Isso evita colapsar tudo em um unico tipo que misture:

- decision making
- policy selection
- runtime mutation
- world rebuild
- spawn logic
- persistence/carry-over

Cada jogo pode fornecer suas politicas e combinacoes sem alterar o shape central da camada.

## 9. Suporte a jogos diferentes

A variacao entre jogos deve entrar por politicas e combinacoes de subplanos, nao por branches de contrato.

Assim, um jogo pode:

- usar apenas continuidade + content swap
- usar reset parcial com carry-over de certos atores
- usar world reconstruction total em transicoes criticas
- tratar phase transition como eixo independente do reset

O centro da camada permanece igual; o que muda e a composicao das politicas.

## 10. Risco de nao registrar agora

Se isso nao for registrado antes da implementacao, o risco principal e a base cristalizar um shape errado:

- continuidade e reset podem virar um unico contrato
- reconstruction pode ser tratado como detalhe de reset
- content/spawn pode contaminar o baseline
- carry-over pode virar regra ad hoc por jogo
- `RunContinuationContext` pode crescer alem do papel de entrada do baseline

O custo depois e refatorar contratos ja consumidos por varios eixos ao mesmo tempo.

## 11. Veredito

Vale registrar agora.

Nao vale esperar, porque este e o ponto certo para congelar a separacao entre baseline e camada de transformacao de sessao/runtime antes que o shape fique acoplado a um caso local.
