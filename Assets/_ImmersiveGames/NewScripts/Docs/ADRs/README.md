# ADRs

Este diretório mantém os ADRs ativos do projeto e sua **ordem de precedência**.

## Regra de precedência

Quando dois ADRs falarem da mesma superfície, vale a regra:

1. **o ADR de número maior prevalece sobre o menor** no trecho em que houver conflito;
2. o ADR menor continua valendo apenas no que **não** foi sobrescrito;
3. quando existir um ADR canônico de consolidação, ele passa a ser a leitura operacional primária;
4. ADRs movidos para `Obsolete/` permanecem apenas como histórico.

## Baseline canônica atual — eixo SceneFlow / LevelFlow

A partir desta reorganização, o eixo SceneFlow/LevelFlow deve ser lido principalmente por estes ADRs:

| ADR | Papel canônico atual |
|---|---|
| `ADR-0030` | fronteiras modulares canônicas entre `Navigation`, `SceneFlow`, `SceneRoute`, `LevelFlow`, `WorldReset/ResetInterop`, `Loading` e `Fade` |
| `ADR-0031` | pipeline macro canônico de transição |
| `ADR-0032` | semântica canônica de route, level, reset e dedupe |
| `ADR-0033` | política canônica de resiliência de fade e loading |

## Leitura mínima recomendada para entender o módulo

1. `ADR-0030`
2. `ADR-0031`
3. `ADR-0032`
4. `ADR-0033`

Com isso, não é mais necessário usar a baseline `ADR-0009` a `ADR-0027` como leitura primária do stack.

## Histórico consolidado

Os ADRs da baseline incremental anterior do eixo SceneFlow/LevelFlow foram movidos para:

- `Obsolete/SceneFlow-Baseline/`

Eles mantêm numeração e conteúdo histórico, mas não devem mais ser usados como contrato operacional primário.

## ADRs ativos fora do eixo consolidado

Continuam ativos, sem alteração de papel nesta reorganização:

- `ADR-0002`
- `ADR-0007`
- `ADR-0008`
- `ADR-0011`
- `ADR-0013`
- `ADR-0014`
- `ADR-0028`
- `ADR-0029`

## Observação

Se o repositório principal já possuir ADRs acima de `0029` fora deste pacote, renumere os novos ADRs na integração preservando a ordem relativa e a cadeia de supersedência.
