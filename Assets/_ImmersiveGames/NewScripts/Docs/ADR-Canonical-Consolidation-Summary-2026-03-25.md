# Resumo — Consolidação Canônica pós-0027 (SceneFlow / LevelFlow)

## O que foi feito

- criados 4 novos ADRs canônicos para leitura curta do stack;
- movida a baseline incremental antiga do eixo SceneFlow/LevelFlow para `Obsolete/SceneFlow-Baseline`;
- atualizada a `README.md` para refletir a nova leitura primária e a regra de supersedência.

## Novos ADRs

- `ADR-0030-Fronteiras-Canonicas-do-Stack-SceneFlow-Navigation-LevelFlow.md`
- `ADR-0031-Pipeline-Canonico-da-Transicao-Macro.md`
- `ADR-0032-Semantica-Canonica-de-Route-Level-Reset-e-Dedupe.md`
- `ADR-0033-Resiliencia-Canonica-de-Fade-e-Loading-no-Transito-Macro.md`

## ADRs movidos para histórico (`Obsolete/SceneFlow-Baseline`)

- `ADR-0009`
- `ADR-0010`
- `ADR-0016`
- `ADR-0017`
- `ADR-0018`
- `ADR-0019`
- `ADR-0020`
- `ADR-0022`
- `ADR-0023`
- `ADR-0024`
- `ADR-0025`
- `ADR-0026`
- `ADR-0027`

## Resultado prático

Agora o entendimento do stack atual pode ser feito lendo apenas `ADR-0030` a `ADR-0033`.

Os ADRs anteriores continuam acessíveis para rastreabilidade, mas deixam de ser a entrada principal para entendimento do módulo.

## Observação de integração

Se o repositório principal já possuir ADRs `0030+`, renumere estes arquivos na integração final mantendo:
- a ordem relativa;
- o bloco canônico de 4 ADRs;
- as referências de supersedência na `README.md`.
