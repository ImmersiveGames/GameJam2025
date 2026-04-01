# Plan - Round 4 - Pre-Objects Closure

## Resumo

Esta fase fecha o periodo pre-objetos.
O projeto continua em prototipo, com placeholders e mocks, mas passa a ter um boundary canonico para declarar futura populacao de gameplay sem confundir isso com spawn operacional.

## Boundary Canonico

O boundary canonico e o **Entry Boundary de Populacao Futura de Gameplay**.

Ele existe para declarar o que pode entrar no jogo futuro, antes de qualquer materializacao.
Ele nao e o spawn, nao e o registry, nao e o reset e nao e a observabilidade.

## O que ele resolve

- separa declaracao de conteudo futuro de execucao operacional atual;
- evita tratar `WorldDefinition` como se fosse a declaracao canonica da populacao;
- evita que ordem de services, spawn e registry sejam lidos como modelagem final;
- reduz o ruido entre prototipo atual e populacao de gameplay que ainda nao existe.

## O que fica fora

- spawn e materializacao operacional;
- registro runtime de instancias vivas;
- reset e reconstituicao;
- observabilidade de spawn, reset e lifecycle;
- taxonomia final de objects, entidades ou conteudo;
- modelagem final de `Player`, `Eater` e `Dummy`.

## Relacao com WorldDefinition

`WorldDefinition` deve ser lido como definicao operacional da ordem de spawn de servicos no baseline atual.

Ele e:

- trilho de entrada operacional;
- configuracao de composicao de spawn;
- evidence of runtime existing.

Ele nao e:

- boundary canonico de declaracao da populacao futura;
- manifesto final de conteudo;
- taxonomia de gameplay;
- contrato de modelagem de objects.

## Criterios de Encerramento

1. Existe uma leitura unica de boundary de entrada para futura populacao.
2. `WorldDefinition` fica explicitamente separado dessa declaracao.
3. Os cinco trilhos ja existentes permanecem distintos:
   - Definition
   - Materialization
   - Registry
   - Reset/Reconstitution
   - Observability
4. Nao ha tentativa de transformar placeholders e mocks em modelagem final.
5. Backbone, round 2 e trilhos operacionais atuais permanecem congelados.

## Decisao Final

A fase pre-objetos fica encerrada quando a baseline passa a expor um boundary proprio para declarar futura populacao de gameplay, enquanto `WorldDefinition` permanece apenas como entrada operacional do runtime atual.
