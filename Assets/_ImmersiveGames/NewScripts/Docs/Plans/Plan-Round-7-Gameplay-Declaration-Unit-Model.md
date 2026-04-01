# Plan - Round 7 - Gameplay Declaration Unit Model

## Resumo

Esta rodada define a unidade declarativa minima do manifesto de conteudo de gameplay por level.
Ela parte da Round 6 e continua separada dos trilhos operacionais congelados.

## Objetivo da Rodada

- definir a menor unidade declarativa util do manifesto;
- manter declaracao, materializacao, registry, reset e observability separados;
- preservar `Level` como contexto da declaracao;
- evitar taxonomia final ou modelagem de runtime nesta fase.

## Unidade Declarativa Minima

A unidade declarativa minima e uma **Gameplay Content Entry**.

Ela representa uma entrada declarada do manifesto por level.
Nao e instancia runtime, nao e spawn e nao e comportamento operacional.

Uma entrada declarativa diz:

- o que pode entrar;
- em qual level pode entrar;
- qual papel inicial ocupa no manifesto;
- qual referencia de configuracao sera usada;
- qual expectativa operacional existe depois da entrada.

## Campos Conceituais Minimos

Uma `Gameplay Content Entry` precisa carregar, conceitualmente:

1. `Entry id`
2. `Level reference`
3. `Role in manifest`
4. `Configuration reference`
5. `Materialization expectation`
6. `Observability expectation`

Separacao canonica:

- `Entrada declarativa` = a declaracao da presenca futura;
- `Configuracao referenciada` = o dado que descreve como a entrada deve ser lida;
- `Instancia runtime` = o que a baseline materializa depois da validacao;
- `Comportamento operacional` = o que acontece no runtime congelado.

## Relacao com Main/Aux/Prototype

- `Main` usa a entrada declarativa para conteudo principal do level;
- `Aux` usa a entrada declarativa para suporte de gameplay;
- `Prototype` usa a entrada declarativa para `Dummy`, placeholders e mocks.

A unidade minima e a mesma para os tres casos.
O que muda e o papel do entry, nao o contrato base.

## O que Fica Fora

- spawn operacional;
- registry runtime;
- reset e reconstituicao;
- observability de lifecycle;
- taxonomia final de objetos, entidades ou componentes;
- definicao detalhada de cada futuro tipo de gameplay;
- migracao de legado de `Scripts`.

## Criterios de Aceite

1. Existe uma unidade minima clara para o manifesto por level.
2. A unidade nao mistura declaracao com instancia runtime.
3. `Main`, `Aux` e `Prototype` usam a mesma base declarativa.
4. `player` e inimigos continuam como leitura inicial, nao como forma final.
5. `Dummy`, placeholders e mocks continuam fora de qualquer modelagem final.

## Decisao Final

A unidade declarativa minima do manifesto fica aprovada como `Gameplay Content Entry`, level-scoped, declarativa e separada do runtime operacional, sem abrir taxonomia final nem mover o legado.
