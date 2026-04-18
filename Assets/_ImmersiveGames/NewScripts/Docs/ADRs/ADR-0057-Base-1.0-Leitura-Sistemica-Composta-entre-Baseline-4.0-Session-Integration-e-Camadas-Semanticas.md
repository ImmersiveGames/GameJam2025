# ADR-0057 - Base 1.0 como leitura sistemica composta com ownership explicito

## Status
- Estado: Aceito
- Data: 2026-04-17
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.
- Leitura normativa principal do sistema: este ADR, em conjunto com `ADR-0056` e `ADR-0055`.

## 1. Contexto

`ADR-0056` congela o baseline como executor tecnico/macro fino.
`ADR-0055` congela `Session Integration` como seam explicito entre semantica e operacao adjacente.

Faltava congelar, de forma unica e normativa, a leitura composta do sistema inteiro para evitar regressao de ownership por conveniencia operacional.

Esta decisao formaliza a **Base 1.0** como leitura estrutural canonica do runtime.

## 2. Problema

Sem leitura sistemica unica, o projeto tende a voltar para uma leitura difusa onde ownership e decidido por gravidade de runtime:

- quem executa passa a ser tratado como owner semantico
- bootstrap/composition absorvem costura de dominio
- seams viram orquestradores concretos
- camadas semanticas sao puxadas para baixo por conveniencia
- `InputModes`, spawn, `ActorRegistry`, reset e adjacentes passam a parecer owners por executarem o trilho

Essa leitura e incorreta para a arquitetura alvo.

## 3. Decisao

Adota-se a **Base 1.0** como sistema composto de ownership explicito, com quatro papeis arquiteturais distintos:

1. **Camada semantica acima** (significado, composicao, politica, ordem)
2. **Seam de integracao explicito** (`Session Integration`)
3. **Baseline tecnico/macro fino**
4. **Dominios operacionais consumidores/executores**

Esta decisao substitui a leitura generica de "tres camadas" por uma leitura **normativa de ownership**.

## 4. Estrutura normativa da Base 1.0

| Papel arquitetural | Responsabilidade canonica | Nao deve fazer |
|---|---|---|
| Camada semantica acima | definir significado, composicao, politica e ordem da sessao/runtime | executar materializacao concreta, virar binder operacional, colapsar para bootstrap |
| `Session Integration` (seam) | traduzir verdade semantica canonica em intencao operacional canonica | virar owner semantico, executar spawn/reset/input concretos |
| Baseline tecnico/macro | executar trilho tecnico e macro (boot, scene macro, loading/fade, gates tecnicos, dispatch macro, rails tecnicos) | definir semantica, decidir ownership de dominio por conveniencia |
| Dominios operacionais | consumir intencao canonica e executar comportamento concreto | reivindicar ownership semantico apenas porque executam runtime |

## 5. Papel do baseline

O baseline e executor tecnico e macro.
Ele serve o sistema com infraestrutura e rails de execucao.

Regra normativa:
- baseline **nao define semantica**
- baseline **nao decide ownership de dominio** por executar runtime
- baseline **nao absorve significado** de sessao, phase, participacao ou politicas de continuidade

## 6. Papel do seam (`Session Integration`)

`Session Integration` e costura explicita entre semantica e operacao.

Regra normativa:
- consome verdade semantica canonica
- emite intencao operacional canonica
- nao vira owner semantico
- nao vira executor concreto por conveniencia

## 7. Papel das camadas semanticas acima

As camadas semanticas acima existem para definir:
- significado
- composicao
- politica
- ordem

Regra normativa:
- nao devem ser puxadas para o baseline por gravidade operacional
- nao devem ser confundidas com binders, registries ou executores concretos

## 8. Papel dos dominios operacionais consumidores

`InputModes`, spawn, `ActorRegistry`, reset e adjacentes devem ser lidos como dominios operacionais consumidores/executores do sistema composto.

Regra normativa:
- executar nao implica ownership semantico
- ownership segue topologia arquitetural da Base 1.0, nao conveniencia de runtime

## 9. Regra normativa de leitura e decisao de ownership

Ownership no projeto **nao** e decidido por "quem roda o codigo".
Ownership e decidido pelo papel arquitetural dentro da Base 1.0.

Toda decisao deve classificar explicitamente o elemento em um destes papeis:

1. semantica (fonte de significado/politica)
2. seam/traducao (integracao semantica -> intencao operacional)
3. execucao tecnica/macro (baseline)
4. execucao operacional concreta (consumidores operacionais)

Se um componente mistura mais de um papel sem justificativa explicita, ha desvio de shape.

## 10. Reflexo estrutural canonico

A leitura canonica da Base 1.0 deve refletir explicitamente:

- camada semantica acima (owner do significado)
- seam de integracao explicito (costura e traducao)
- baseline tecnico/macro fino (infra e trilho tecnico)
- dominios operacionais como consumidores/executores

Esta e a referencia correta para evitar leitura difusa por acoplamento acidental.

## 11. Consequencias praticas

Esta leitura passa a ser filtro obrigatorio para decisoes futuras de ownership:

- nao decidir ownership por conveniencia operacional
- nao ressuscitar leitura onde semantica colapsa em spawn/reset/registry/bootstrap
- nao permitir que bootstrap/composition virem owner por concentrar wiring
- explicitar sempre quem e semantica, quem e seam e quem e executor

## 12. Relacao com ADRs anteriores

- `ADR-0056` permanece owner do baseline tecnico fino.
- `ADR-0055` permanece owner do seam de integracao.
- Este ADR e a leitura sistemica principal que organiza os dois em um sistema unico.
- `ADR-0052` e `ADR-0054` permanecem como camadas semanticas adjacentes a esta leitura.
- `ADR-0058` permanece como especializacao do eixo de actors acima desta base.

Este ADR nao reabre ADRs antigos nesta etapa.

## 13. Fechamento

A **Base 1.0** passa a ser a leitura canonica estrutural do sistema.

Toda discussao futura de modulo, boundary e ownership deve partir desta leitura:
- semantica acima
- seam explicito de traducao
- baseline tecnico/macro fino
- execucao operacional concreta como consumo

Decisoes que contrariem essa leitura devem ser tratadas como desvio arquitetural e nao como variacao aceitavel de implementacao.

## 14. Alinhamento de referencia formal (estado atual validado)

Com base no estado runtime validado em smoke interativo recente, registra-se o seguinte quadro formal de referencia da Base 1.0:

- `InputModes` = executor operacional puro
- `ResetFlow` = peca composta saudavel
- `Session Integration` = seam explicito puro
- `SceneFlow` = baseline tecnico/macro fino
- `GameplayParticipationFlowService` = semantica pura

Para `SceneFlow`, fica normativo que:
- o bootstrap permanece estritamente de wiring tecnico/macro
- loading/fade/gates/checkpoints macro permanecem no owner tecnico
- boundaries com `ResetFlow` e camadas acima ficam explicitos por handshakes/publicacoes macro canonicos
- semantica de sessao/participacao/phase/continuidade nao pertence ao owner `SceneFlow`

Para `GameplayParticipationFlowService`, fica normativo que:
- owner de truth semantica de participation/readiness
- entrada semantica minima explicita por `ParticipationSemanticInput`
- saida semantica canonica por `ParticipationSnapshot` + `ParticipationSnapshotChangedEvent`
- composicao externa (sem auto-bootstrap/composition root interno)
- consumidores operacionais reagem fora da peca


Imagens de referencia
![ChatGPT Image 17 de abr. de 2026, 10_05_10.png](../Plans/ChatGPT%20Image%2017%20de%20abr.%20de%202026%2C%2010_05_10.png)
![ChatGPT Image 17 de abr. de 2026, 10_30_59.png](../Plans/ChatGPT%20Image%2017%20de%20abr.%20de%202026%2C%2010_30_59.png)
![ChatGPT Image 17 de abr. de 2026, 10_46_07.png](../Plans/ChatGPT%20Image%2017%20de%20abr.%20de%202026%2C%2010_46_07.png)
