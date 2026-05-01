# ADR-0066 - Gates, InputModes e GameLoop como executores de estado e efeitos

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canonica deste contrato: este ADR.

## Contexto

Gates, `InputModes` e `GameLoop` ficaram historicamente perto da decisao de lifecycle porque executam passos relevantes do runtime.
Na Base 1.1 isso precisa ser separado com clareza: executar estado e efeitos nao e o mesmo que decidir lifecycle.

## Decisao

Adota-se a regra:

- `Gates` executam validacao ou transicao de estado.
- `InputModes` executa request/application de modos de input.
- `GameLoop` executa estado e handoff operacional do loop.
- nenhum desses blocos decide lifecycle por conta propria.

## Consequencias

- o runtime ganha rails executores claros.
- decisao fica no pipeline; aplicacao fica nos executores.
- config obrigatoria continua fail-fast.
- fallback silencioso continua proibido.

## Invariantes

- estado executado nao e politica.
- efeito executado nao e ownership.
- gate nao decide uma identidade de ciclo.
- event foreign/stale nao pode reconfigurar o pipeline ativo.

## Relacao com Base 1.0 e Base 2.0

- Base 1.0 tratou esses blocos como executores tecnicos e rails de apoio.
- Base 1.1 fecha o contrato: executam estado/efeitos, nao lifecycle.
- Base 2.0 futura pode abstrair o conjunto se o comportamento provar ser reutilizavel.

## ADRs historicos relacionados

- `ADR-0031`
- `ADR-0037`
- `ADR-0040`
- `ADR-0056`
- `ADR-0013`
