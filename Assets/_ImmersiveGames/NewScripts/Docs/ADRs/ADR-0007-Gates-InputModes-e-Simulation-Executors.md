# ADR-0007 - Gates, InputModes e Simulation Executors

## Status
- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

`Gates`, `InputModes` e `GameLoop` ficaram historicamente perto da decisão de lifecycle porque executam passos relevantes do runtime. Na Base 1.1 isso precisa ser separado com clareza: **executar estado e efeitos não é o mesmo que decidir lifecycle**.

## Decisão

Adota-se a regra:

### 1. Gates

- `Gates` executam validação ou transição de estado.
- Baseado em condições/requisitos explícitos.
- Reportam sucesso/falha ao pipeline.
- Não criam decisão de lifecycle por conta própria.

Exemplos canônicos:
- `SimulationGate` valida condições baseadas em estado/contexto.
- `AwaitBeforeFadeOut` aguarda confirmação de readiness antes de prosseguir fade out.

### 2. InputModes

- `InputModes` executa request e application de modos de input.
- Pode ser `FrontendMenu`, `Gameplay`, `PauseOverlay`, etc.
- Não decide que modo está ativo baseado em inferência.
- O pipeline ou um coordenador explícito decide que modo aplicar; o executor aplica.

#### Canonical Input Mode Requests
- `FrontendMenu` (modo de entrada para UI de menu)
- `Gameplay` (modo de entrada para gameplay)
- `PauseOverlay` (modo de entrada para pause/overlay)

### 3. GameLoop

- `GameLoop` executa estado e `Pipeline Handoff` operacional do loop.
- Não decide que pipeline ativo está rodando.
- Executa update/render/input gathering conforme configurado.

### 4. Invariante Geral

- Nenhum desses blocos decide lifecycle por conta própria.
- Config obrigatória continua fail-fast.
- Fallback silencioso continua proibido.
- Estado executado não é política.
- Efeito executado não é ownership.
- Gate não decide uma identidade de ciclo.
- Foreign/stale events não podem reconfigurar o pipeline ativo.

## Consequências

- O runtime ganha rails executores claros.
- Decisão fica no pipeline; aplicação fica nos executores.
- Integração de features novas fica mais clara.
- Depuração de estado/input fica localizável.
- Policy continua no pipeline; execução nos blocos.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 tratou esses blocos como executores técnicos e rails de apoio.
- Base 1.1 fecha o contrato: executam estado/efeitos, não lifecycle.
- Base 2.0 futura pode abstrair o conjunto se o comportamento provar ser reutilizável.


