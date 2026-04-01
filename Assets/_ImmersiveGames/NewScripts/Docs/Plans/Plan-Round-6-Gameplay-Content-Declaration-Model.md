# Plan - Round 6 - Gameplay Content Declaration Model

## Resumo

Esta rodada define como o conteudo de gameplay deve ser declarado na arquitetura.
Ela parte da Round 5 e fica acima dos trilhos operacionais ja congelados.

## Objetivo da Rodada

- definir a forma canonica de declaracao de conteudo de gameplay;
- manter a declaracao separada de materializacao, registry, reset e observability;
- manter `Level` como contexto de entrada e operacao;
- preservar backbone, round 2, round 3, round 4 e round 5 congelados.

## Declaracao Canonica de Conteudo

A forma canonica de declaracao e um **manifesto declarativo de gameplay por level**.

Esse manifesto declara o que pode entrar no `Level`, nao como isso e spawnado.
Ele e consumido pela baseline, mas nao e o trilho operacional.

Leitura canonica:

- o `Entry Boundary` autoriza a entrada;
- a declaracao descreve o conteudo admitido;
- a baseline materializa o que foi declarado;
- o runtime acompanha e reconstroi o que entrou.

## Relacao com Level e Entry Boundary

- `Level` e o contexto que recebe e organiza a declaracao;
- o `Entry Boundary` e a porta canonica de admissao;
- a declaracao fica ligada ao `Level`, nao ao spawn;
- a baseline valida a declaracao antes de qualquer materializacao;
- o conteudo entra no `Level` como intencao declarada, nao como instancia pronta.

## Estrutura Inicial

Estrutura inicial e enxuta:

1. `Main gameplay content`
   - conteudo principal da experiencia futura;
   - inclui `player` e inimigos como leitura inicial.
2. `Aux gameplay content`
   - conteudo auxiliar que suporta a experiencia sem virar taxonomia final.
3. `Prototype content`
   - `Dummy`, placeholders e mocks;
   - servem para prototipo e validacao, nao para modelagem final.

Cada entrada declarada deve separar, no minimo:

- identificacao declarativa;
- papel no level;
- referencia de configuracao;
- indicacao de materializacao prevista;
- indicacao de observabilidade esperada.

## O que Fica Fora

- spawn e materializacao operacional;
- registry runtime;
- reset e reconstituicao;
- observability de ciclo de vida;
- migracao de legado de `Scripts`;
- taxonomia final de objetos, entidades ou componentes;
- definicao detalhada de comportamento final de cada tipo de gameplay.

## Criterios de Aceite

1. Existe uma declaracao canonica de conteudo por level.
2. O `Entry Boundary` continua separado da materializacao operacional.
3. `Main`, `Aux` e `Prototype` ficam como divisao inicial e enxuta.
4. `player` e inimigos entram como conteudo principal, sem fechar taxonomia final.
5. `Dummy`, placeholders e mocks continuam como suporte de prototipo.

## Decisao Final

A declaracao de conteudo de gameplay fica aprovada como manifesto level-scoped, consumido pela baseline e separado dos trilhos operacionais congelados, sem abrir taxonomia final nem migrar o legado.
