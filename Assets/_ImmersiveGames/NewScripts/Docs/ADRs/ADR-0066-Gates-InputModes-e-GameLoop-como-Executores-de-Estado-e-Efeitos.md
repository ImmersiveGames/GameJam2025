# ADR-0066 - Gates, InputModes e GameLoop como executores de estado e efeitos

## Status
- Estado: Accepted
- Data: 2026-05-01
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

`Gates`, `InputModes` e `GameLoop` ficaram historicamente perto da decisão de lifecycle porque executam passos relevantes do runtime.
Na Base 1.1 isso precisa ser separado com clareza: executar estado e efeitos não é o mesmo que decidir lifecycle.

## Decisão

Adota-se a regra:

- `Gates` executam validação ou transição de estado.
- `InputModes` executa request/application de modos de input.
- `GameLoop` executa estado e `Pipeline Handoff` operacional do loop.
- nenhum desses blocos decide lifecycle por conta própria.

## Consequências

- o runtime ganha rails executores claros.
- decisão fica no pipeline; aplicação fica nos executores.
- config obrigatória continua fail-fast.
- fallback silencioso continua proibido.

## Invariantes

- estado executado não é política.
- efeito executado não é ownership.
- gate não decide uma identidade de ciclo.
- foreign/stale events não podem reconfigurar o pipeline ativo.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 tratou esses blocos como executores técnicos e rails de apoio.
- Base 1.1 fecha o contrato: executam estado/efeitos, não lifecycle.
- Base 2.0 futura pode abstrair o conjunto se o comportamento provar ser reutilizável.

## ADRs históricos relacionados

- `ADR-0031`
- `ADR-0037`
- `ADR-0040`
- `ADR-0056`
- `ADR-0013`
