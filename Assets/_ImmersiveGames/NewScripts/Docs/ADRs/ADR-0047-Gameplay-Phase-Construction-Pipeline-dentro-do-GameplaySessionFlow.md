# ADR - Gameplay Phase Construction Pipeline dentro do GameplaySessionFlow

## Status
- Estado: Aceito
- Data: 2026-04-03
- Tipo: Direction / Canonical architecture

## Contexto

`Gameplay Runtime Composition` ja e o centro semantico do gameplay.
`GameplaySessionFlow` ja foi fechado como o primeiro bloco interno desse centro.

## Relacao com os ADRs anteriores

- `ADR-0045` define a direcao macro: `Gameplay Runtime Composition` como centro semantico do gameplay.
- `ADR-0046` define o primeiro bloco interno desse centro: `GameplaySessionFlow`.
- Este ADR define o pipeline minimo de construcao de fase dentro desse bloco.

O problema agora nao e mais definir o owner macro, mas formalizar o pipeline minimo que construi a fase jogavel em nivel de conjunto, antes de qualquer desdobramento para objetos individuais, presentation ou detalhes de implementacao.

## Decisao

Adotar um pipeline minimo de construcao de fase dentro de `GameplaySessionFlow` que organize a sessao jogavel como uma sequencia semantica unica, da definicao de contexto ate a liberacao para `Playing`.

Esse pipeline passa a ser a leitura canonica da montagem inicial da fase.

## Pipeline minimo

O pipeline minimo recomendado e:

1. Definir o contexto da sessao
2. Selecionar o phase / level runtime
3. Determinar a participacao de players
4. Fixar regras e objetivos do conjunto
5. Seedar o estado inicial da fase
6. Materializar o conteudo necessario
7. Executar intro / enter stage
8. Liberar para `Playing`

## Contrato semantico minimo da V1

Esta V1 precisa fechar, no minimo, estes quatro eixos:
- contexto da sessao
- phase / level runtime
- participacao de players
- regras / estado inicial do conjunto

Sem esses quatro eixos, ainda nao ha uma fase jogavel lida como conjunto.

## O que pertence ao GameplaySessionFlow

`GameplaySessionFlow` e dono da semantica do conjunto que monta a fase jogavel.

Isso inclui:
- contexto de sessao
- selecao do runtime de fase
- composicao dos participantes
- regras e objetivos ativos
- seed de estado inicial
- decisao sobre o que precisa existir para a fase ficar jogavel
- entrada canonica na fase
- transicao semantica ate `Playing`

## O que continua no backbone

O backbone continua responsavel apenas pela execucao operacional segura que viabiliza o fluxo.

Isso inclui:
- boot
- SceneFlow
- Fade / Loading
- gates
- reset e materializacao operacional
- garantia tecnica de readiness

O backbone nao define a semantica da fase.

## O que pode comecar mockado

Pode comecar mockado:
- selecao de phase / level runtime
- composicao de players
- regras e objetivos do conjunto
- seed de estado inicial
- materializacao de conteudo
- intro / enter stage como sequencia semantica

O mock existe apenas para permitir a leitura do pipeline antes da integracao real.

## Fora de escopo deste corte

Ficam explicitamente fora:
- objeto individual
- presentation
- UI local
- input detalhado
- camera
- audio contextual
- persistencia fina
- regras de lifecycle por entidade
- micro-orquestracao de spawn
- detalhes de layout ou runtime visual

## Consequencias praticas

- a fase passa a ser lida como um conjunto construido por semantica, nao como soma de modulos soltos
- `GameplaySessionFlow` ganha fronteira clara para a construcao inicial da experiencia jogavel
- o backbone continua abaixo, como executor tecnico
- o pipeline pode ser discutido e refinado sem reintroduzir o backbone como dono do significado da fase

## Proximos passos

1. Consolidar a leitura de fase como unidade de composicao dentro de `GameplaySessionFlow`
2. Derivar o contrato semantico minimo entre contexto, players, objetivos e estado inicial
3. Separar o que sera ponte transitiva do que sera ownership real
4. Somente depois disso abrir o corte de implementacao
