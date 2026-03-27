# ADRs

Este diretorio mantem os ADRs ativos do projeto e sua ordem de precedencia.

## Regra de precedencia

Quando dois ADRs falarem da mesma superficie, vale a regra:

1. o ADR de numero maior prevalece sobre o menor no trecho em que houver conflito;
2. o ADR menor continua valendo apenas no que nao foi sobrescrito;
3. quando existir um ADR canonico de consolidacao, ele passa a ser a leitura operacional primaria;
4. ADRs movidos para `Obsolete/` permanecem apenas como historico.

## Baseline canônica atual - eixo SceneFlow / LevelFlow

A partir desta reorganizacao, o eixo SceneFlow/LevelFlow deve ser lido principalmente por estes ADRs:

| ADR | Papel canonico atual |
|---|---|
| `ADR-0030` | fronteiras modulares canonicas entre `Navigation`, `SceneFlow`, `SceneRoute`, `LevelFlow`, `WorldReset/ResetInterop`, `Loading` e `Fade` |
| `ADR-0031` | pipeline macro canonico de transicao |
| `ADR-0032` | semantica canonica de route, level, reset e dedupe |
| `ADR-0033` | politica canonica de resiliencia de fade e loading |

Para IntroStage, a leitura operacional principal e `ADR-0027`. Para hooks oficiais, use `ADR-0037`. Para o fluxo implementado de PostStage, use `ADR-0012`.

## Leitura minima recomendada para entender o modulo

1. `ADR-0030`
2. `ADR-0031`
3. `ADR-0032`
4. `ADR-0033`
5. `ADR-0012`
6. `ADR-0027`
7. `ADR-0037`

Com isso, nao e mais necessario usar a baseline `ADR-0009` a `ADR-0026` como leitura primaria do stack; para IntroStage, leia `ADR-0027`.

## Historico consolidado

Os ADRs da baseline incremental anterior do eixo SceneFlow/LevelFlow foram movidos para:

- `Obsolete/SceneFlow-Baseline/`

Eles mantem numeracao e conteudo historico, mas nao devem mais ser usados como contrato operacional primario.

## ADRs ativos fora do eixo consolidado

Continuam ativos, sem alteracao de papel nesta reorganizacao:

- `ADR-0002`
- `ADR-0007`
- `ADR-0008`
- `ADR-0011`
- `ADR-0012`
- `ADR-0013`
- `ADR-0014`
- `ADR-0028`
- `ADR-0029`

## Observacao

Se o repositorio principal ja possuir ADRs acima de `0029` fora deste pacote, renumere os novos ADRs na integracao preservando a ordem relativa e a cadeia de supersedencia.
