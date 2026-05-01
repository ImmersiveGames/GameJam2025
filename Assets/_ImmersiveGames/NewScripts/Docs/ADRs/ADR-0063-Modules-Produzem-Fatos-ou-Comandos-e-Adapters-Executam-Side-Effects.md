# ADR-0063 - Modules produzem fatos ou comandos e adapters executam side-effects

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

A Base 1.0 separou semantica, seam e execucao de forma util, mas ainda permitiu leituras ambíguas sobre quem decide e quem apenas executa.
Na Base 1.1 essa separacao precisa virar regra de sistema.

## Decisao

Adota-se a regra:

- modulos produzem fatos ou comandos;
- pipelines decidem ordem, lifecycle, policies e handoffs;
- adapters executam side-effects.

Regra complementar:

- nenhum adapter cria politica propria;
- nenhum modulo operacional reescreve identidade do ciclo;
- nenhum pipeline depende de leitura foreign/stale para decidir o ativo.

## Consequencias

- A responsabilidade de decisao fica concentrada no pipeline.
- O lado executor deixa de ser confundido com o lado decisor.
- Integracao externa fica por adaptacao, nao por ownership oculto.
- Falhas de side-effect nao podem ser mascaradas como decisao de lifecycle.

## Invariantes

- comando nao e efeito.
- fato nao e decisao.
- adapter nao e owner de semantica.
- side-effect executa o que foi comandado, nao inventa o que deve ser feito.

## Relacao com Base 1.0 e Base 2.0

- Base 1.0 estabeleceu os blocos e rails que permitiram enxergar a separacao.
- Base 1.1 transforma essa separacao em contrato normativo central.
- Base 2.0 futura so deve reutilizar a regra se ela continuar provada por evidencias de runtime.

## ADRs historicos relacionados

- `ADR-0055`
- `ADR-0056`
- `ADR-0057`
- `ADR-0058`
- `ADR-0059`
- `ADR-0040`
- `ADR-0038`
