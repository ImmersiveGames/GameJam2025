# ADR-0005 - Modules Produzem Facts/Commands, Adapters Executam Side-Effects

## Status
- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade can�nica deste contrato: este ADR.

## Contexto

A Base 1.0 separou sem�ntica, seam e execu��o de forma �til, mas ainda permitiu leituras amb�guas sobre quem decide e quem apenas executa. Na Base 1.1 essa separa��o precisa virar regra de sistema obrigat�ria.

## Decis�o

Adota-se a regra separadora de responsabilidades:

### 1. M�dulos Produzem Pipeline Facts ou Pipeline Commands

M�dulos s�o partes da arquitetura que fornecem dados, estado ou inten��es:

- **Pipeline Facts**: Representam observa��es, estado capturado ou eventos do dom�nio.
- **Pipeline Commands**: Representam inten��es, requisi��es de a��o ou directives operacionais.

Regra complementar:

- Nenhum m�dulo operacional reescreve a identidade do ciclo.
- Nenhum pipeline depende de leitura foreign/stale para decidir o ativo.
- M�dulos exp�em facts/commands via interfaces claras ou eventos can�nicos.

### 2. Pipelines Decidem Ordem, Lifecycle, Policy e Handoffs

Pipelines orquestram a sequ�ncia de opera��es:

- **Decidi ordem**: Sequ�ncia de steps.
- **Decidem lifecycle**: Quando algo come�a, continua ou encerra.
- **Decidem Pipeline Policies**: Regras de comportamento.
- **Decidem Pipeline Handoffs**: Transi��es entre fases/stages.

### 3. Pipeline Adapters Executam Side-Effects

Adapters s�o a camada de execu��o;

- Executam o que foi comandado pelo pipeline.
- N�o criam pol�tica pr�pria.
- N�o reescrevem decis�es do pipeline.
- Reportam completion, failure ou status back ao pipeline.

#### Exemplos Can�nicos

- `SceneCompositionAdapter` executa carregamento/descarregamento de cenas comandado pelo pipeline.
- `FadeAdapter` executa fade/unfade comandado pelo pipeline.
- `LoadingAdapter` executa UI de loading comandada pelo pipeline.
- `AudioAdapter` executa playing/stopping de �udio comandado pelo pipeline.

Nota normativa curta (checkpoint de �udio operacional de rota):
- `AudioAdapter` � `Pipeline Adapter` de execu��o e observabilidade; n�o � owner de policy, timing ou lifecycle de rota.

## Invariantes

- `Pipeline Command` n�o � efeito; � decis�o registrada.
- `Pipeline Fact` n�o � decis�o; � observa��o ou estado.
- `Pipeline Adapter` n�o � owner de sem�ntica; executa o contratado.
- Side-effect executa o que foi comandado, n�o inventa o que deve ser feito.
- Nenhum adapter cria fallback silencioso.
- Nenhum adapter muda a identidade do ciclo.
- Nenhum m�dulo decide sequ�ncia global sem passar pelo pipeline.

## Consequ�ncias

- A responsabilidade de decis�o fica concentrada no pipeline.
- O lado executor deixa de ser confundido com o lado decisor.
- Integra��o externa fica por adapta��o, n�o por ownership oculto.
- Falhas de side-effect n�o podem ser mascaradas como decis�o de lifecycle.
- A auditoria do fluxo fica clara: comando -> adapt -> report -> pr�ximo paso.

## Materializacao Base11Sandbox - Checkpoint Congelado

O checkpoint validado confirmou a regra deste ADR sem ambiguidade:

- `SessionOperationalPipeline` emite `OperationalRouteCommand` e `OperationalRouteCompleted`.
- `Base11SandboxOperationalRouteTransitionAdapter` s� executa `load/unload/set-active`.
- `SessionActivityEntryHandoff` � produzido pelo pipeline, n�o pelo adapter.
- `SceneCompositionExecutor` permanece estritamente executor f�sico.
- `SessionActivityPipeline` decide a entrada de activity e o controle interno do ciclo.

Leitura congelada:

- Comando n�o � efeito.
- Fato n�o � decis�o.
- Adapter n�o � owner de sem�ntica.
- Policy continua concentrada no pipeline.

## Rela��o com Base 1.0 e Base 2.0

- Base 1.0 estabeleceu os blocos e rails que permitiram enxergar a separa��o.
- Base 1.1 transforma essa separa��o em contrato normativo central.
- Base 2.0 futura s� deve reutilizar a regra se ela continuar provada por evid�ncias de runtime.


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
## Checkpoint congelado - PendingOperation como command em execucao (2026-05-19)

Contrato congelado para SessionActivity:

- `PendingOperation` representa `Pipeline Command` em execucao (lifecycle ativo), nao estado tecnico solto.
- `ISessionActivityPendingOperationRunner` e `ISessionActivityWindowSceneAdapter` permanecem executores de side-effect.
- Adapters nao decidem continuidade de rail, completion semantico ou autorizacao de unload.
- `ClearPendingOperation` so ocorre no ponto de consumo validado do callback de completion/failure do command pendente.
- Sem fallback silencioso para completar rail quando callback/identity nao confere.
