# ADR-0002 - Run Pipeline Canonical e Deactivation/Continuity

## Status
- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

O conceito histórico de `macro` juntou em um único vocabulário o que hoje precisa ser lido como `Run Pipeline`: identidade, sequência, lifecycle, handoffs e deactivation. Ao mesmo tempo, os conceitos de `RunResult`, `RunDecision` e `PostRun` foram historicamente tratados como owners globais da arquitetura, quando na verdade pertencem ao eixo de `Deactivation / Continuity`.

## Decisão

Adota-se o `Run Pipeline` como rail canônico de run e o eixo de `Deactivation / Continuity` como parte integral desse pipeline.

### Princípios

1. **Run Pipeline** substitui `macro` como conceito normativo.
2. `RunResult`, `RunDecision` e `PostRun` pertencem ao eixo de `Deactivation / Continuity`, não a um owner global histórico.
3. O pipeline decide a ordem da run, o lifecycle da run e os `Pipeline Handoffs` entre etapas.
4. A execução concreta ocorre em `Pipeline Adapters` ou componentes operacionais comandados pelo pipeline.

### Deactivation / Continuity

- `RunResult`, `RunDecision` e `PostRun` não são owners globais da arquitetura.
- O pipeline decide quando o ciclo ativo fecha e quando a continuidade inicia.
- A ausência de stage válida gera `skip/no-content` explícito.
- Run ativa e deactivation não podem coexistir como owners concorrentes.

## Invariantes

- Nenhuma etapa de run pode ser movida por foreign/stale events.
- O pipeline ativo é unicamente o que corresponde à identidade válida da run.
- Adapters não decidem continuidade final.
- Ordens derivadas por compatibilidade histórica não podem reescrever o `Run Pipeline`.
- Run ativa e deactivation não podem coexistir como owners concorrentes.
- Foreign/stale events não podem reabrir o ciclo fechado.
- Continuidade sempre depende de identidade explícita.
- Ausência de presenter local válido não rompe o contrato; gera skip/no-content.

## Consequências

- `macro` passa a ser vocabulário histórico.
- A run deixa de depender de owners difusos por conveniência de fluxo.
- O fechamento de run passa a ser lido por identidade explícita de ciclo.
- A Base 1.1 ganha um rail único para orquestrar run sem colapsar em baseline ou scene-local code.
- A continuidade passa a ser uma derivação do fechamento, não um owner difuso.
- O resultado de run deixa de competir com o pipeline ativo.
- A deactivation pode ser diferente por identidade de ciclo sem quebrar o contrato.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 é histórico de consolidação do conceito de macro e dos rails que o cercavam.
- Base 1.1 redefine o rail final como pipeline determinístico de run com deactivation/continuity integrada.
- Base 2.0 futura só pode generalizar o que o `Run Pipeline` provar na prática.


