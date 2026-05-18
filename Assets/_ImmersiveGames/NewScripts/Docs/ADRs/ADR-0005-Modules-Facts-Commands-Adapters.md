# ADR-0005 - Modules Produzem Facts/Commands, Adapters Executam Side-Effects

## Status
- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 separou semântica, seam e execução de forma útil, mas ainda permitiu leituras ambíguas sobre quem decide e quem apenas executa. Na Base 1.1 essa separação precisa virar regra de sistema obrigatória.

## Decisão

Adota-se a regra separadora de responsabilidades:

### 1. Módulos Produzem Pipeline Facts ou Pipeline Commands

Módulos são partes da arquitetura que fornecem dados, estado ou intenções:

- **Pipeline Facts**: Representam observações, estado capturado ou eventos do domínio.
- **Pipeline Commands**: Representam intenções, requisições de ação ou directives operacionais.

Regra complementar:

- Nenhum módulo operacional reescreve a identidade do ciclo.
- Nenhum pipeline depende de leitura foreign/stale para decidir o ativo.
- Módulos expõem facts/commands via interfaces claras ou eventos canônicos.

### 2. Pipelines Decidem Ordem, Lifecycle, Policy e Handoffs

Pipelines orquestram a sequência de operações:

- **Decidi ordem**: Sequência de steps.
- **Decidem lifecycle**: Quando algo começa, continua ou encerra.
- **Decidem Pipeline Policies**: Regras de comportamento.
- **Decidem Pipeline Handoffs**: Transições entre fases/stages.

### 3. Pipeline Adapters Executam Side-Effects

Adapters são a camada de execução;

- Executam o que foi comandado pelo pipeline.
- Não criam política própria.
- Não reescrevem decisões do pipeline.
- Reportam completion, failure ou status back ao pipeline.

#### Exemplos Canônicos

- `SceneCompositionAdapter` executa carregamento/descarregamento de cenas comandado pelo pipeline.
- `FadeAdapter` executa fade/unfade comandado pelo pipeline.
- `LoadingAdapter` executa UI de loading comandada pelo pipeline.
- `AudioAdapter` executa playing/stopping de áudio comandado pelo pipeline.

Nota normativa curta (checkpoint de áudio operacional de rota):
- `AudioAdapter` é `Pipeline Adapter` de execução e observabilidade; não é owner de policy, timing ou lifecycle de rota.

## Invariantes

- `Pipeline Command` não é efeito; é decisão registrada.
- `Pipeline Fact` não é decisão; é observação ou estado.
- `Pipeline Adapter` não é owner de semântica; executa o contratado.
- Side-effect executa o que foi comandado, não inventa o que deve ser feito.
- Nenhum adapter cria fallback silencioso.
- Nenhum adapter muda a identidade do ciclo.
- Nenhum módulo decide sequência global sem passar pelo pipeline.

## Consequências

- A responsabilidade de decisão fica concentrada no pipeline.
- O lado executor deixa de ser confundido com o lado decisor.
- Integração externa fica por adaptação, não por ownership oculto.
- Falhas de side-effect não podem ser mascaradas como decisão de lifecycle.
- A auditoria do fluxo fica clara: comando -> adapt -> report -> próximo paso.

## Materializacao Base11Sandbox - Checkpoint Congelado

O checkpoint validado confirmou a regra deste ADR sem ambiguidade:

- `SessionOperationalPipeline` emite `OperationalRouteCommand` e `OperationalRouteCompleted`.
- `Base11SandboxOperationalRouteTransitionAdapter` só executa `load/unload/set-active`.
- `SessionActivityEntryHandoff` é produzido pelo pipeline, não pelo adapter.
- `SceneCompositionExecutor` permanece estritamente executor físico.
- `SessionActivityPipeline` decide a entrada de activity e o controle interno do ciclo.

Leitura congelada:

- Comando não é efeito.
- Fato não é decisão.
- Adapter não é owner de semântica.
- Policy continua concentrada no pipeline.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 estabeleceu os blocos e rails que permitiram enxergar a separação.
- Base 1.1 transforma essa separação em contrato normativo central.
- Base 2.0 futura só deve reutilizar a regra se ela continuar provada por evidências de runtime.


## Checkpoint complementar - ActivityWindowProfile (decisao futura, 2026-05-18)

- SessionActivityPipeline continua owner da decisao de lifecycle de ActivationWindow/DeactivationWindow.
- ActivityWindowProfileAsset sera contrato autoral de window (dados/policy), nao owner de fluxo.
- Adapters de window scene e window presentation executam side-effects comandados pelo pipeline.
- CameraPresentation/WindowPresentation nao decide ordem, ready ou fechamento da window.
## Checkpoint complementar - SessionActivity RestartCurrentActivity local (CLOSED/PASS estrutural, 2026-05-18)

- RestartCurrentActivity e Pipeline Command proprio do SessionActivityPipeline.
- Pipeline decide teardown/reentry/entrySequence/facts de restart.
- WindowScene adapter executa side-effects de load/unload; nao decide restart lifecycle.
- Host/DebugPanel apenas tooling/observabilidade; nao decide restart nem avanco de stage.