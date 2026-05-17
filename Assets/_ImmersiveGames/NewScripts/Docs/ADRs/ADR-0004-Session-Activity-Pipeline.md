# ADR-0004 - Session Activity Pipeline

## Status
- Estado: Accepted
- Data: 2026-05-12
- Tipo: Direction / Canonical architecture
- Fonte de verdade canônica deste contrato: este ADR.

## Contexto

A Base 1.0 tratou o engagement com activities como localmente operacional, com phases, stages e engagement owned por módulos dispersos. Na Base 1.1, o ciclo de activity ganha sua própria pipeline com identidade explícita e policy clara de ativação via `IntroStage`.

## Decisão

### 1. Session Activity Pipeline

Adota-se o `SessionActivityPipeline` como owner do lifecycle de ativação, engagement e ciclo interno de activity.

#### Princípios

- `SessionActivityPipeline` coordena a entrada, ativação e fecho de activities.
- O pipeline decide ordem, phases, policies de ativação e transitions internas.
- `ActivityAsset` e `ActivityCatalogAsset` definem dados autorais mas não decidem lifecycle.
- A ordem do `ActivityCatalog` define navegação.
- `ActivityCatalogLooped` e `CatalogLoopCount` registram loops e repetição.
- O pipeline decide quando a activity entra, ativa, pausa, retoma e sai.
- `SessionActivityHost` e bridge/composition surface; nao e owner de lifecycle.
- Em runtime normal, a entrada canonica de Activity ocorre apenas por `SessionActivityEntryHandoff`.
- `autoStart` e `DebugStartActivity` sao tooling/QA e nao contrato canonico de entrada.

#### IntroStage: Activation Stage e Pipeline Policy

`IntroStage` passa a ser lido como `Activation Stage`, uma `Pipeline Policy` de ativação executada pelo pipeline, não um ownership de ativação local.

##### Princípios de IntroStage

- `IntroStage` não é owner de ativação.
- A decisão de abrir, pular ou encerrar a ativação pertence ao pipeline.
- Quando existir presenter válido para a identidade atual, a stage executa.
- Quando não existir presenter válido, o ciclo registra `skip/no-content` explícito.
- Resolução concreta da instância ocorre somente no momento canônico da pipeline.

#### Invariantes da Activity Pipeline

- O pipeline decide ativação, engagement e closure.
- Toda activity relevante possui identidade canônica e entry handoff.
- O catalogo preserva precedência de navegação.
- Loops e repetições são rastreáveis por metadata canônica.
- Foreign/stale events não podem trocar a activity ativa.
- A ausência de presenter válido não causa fallback; gera skip/no-content.
- A resolução local da instância concreta não decide lifecycle.
- Comando/evento sem identity valida nao inicia nem altera Activity ativa.
- Host local nao pode iniciar Activity automaticamente por `autoStart`.

## Consequências

- Ativação deixa de ser decidida por conveniência local.
- A ausência válida de conteúdo não gera fallback silencioso.
- A ativação fica semanticamente vinculada ao ciclo correto.
- Foreign/stale events não podem reabrir ou trocar a stage ativa.
- O host local resolve a instância concreta, não a regra de ativação.
- `Skip/no-content` é sinal válido quando o conteúdo estiver ausente.

## Materializacao Base11Sandbox - Checkpoint Congelado

No checkpoint `Base11Sandbox Minimal Route + Session Activity Cycle - PASS`:

- `SessionActivityPipeline` é owner do ciclo de activity.
- `SessionActivityMiniFlowHost` e `SessionActivityPipeline` não decidem rota ou transição de sessão.
- `ActivityAsset` fornece dados autorais; o pipeline decide entrada e ativação.
- `ActivityCatalog` define precedência; a ordem é aceita pelo pipeline como parte de policy.
- `entrySequence` pertence ao `SessionActivityPipeline`, separado de `routeSequence` operacional.

## Checkpoint de Fronteira - SessionActivityHost (2026-05-16)

- `SessionActivityHost` nao inicia Activity automaticamente por `autoStart`.
- `autoStart=true` fora de QA/editor e bloqueado por fail-fast (`SessionActivityHostAutoStartBlocked`).
- Em QA/editor, `autoStart=true` gera apenas observabilidade (`SessionActivityHostAutoStartIgnored`) e nao inicia lifecycle.
- `DebugStartActivity` permanece tooling explicito de QA/debug com source/reason `SessionActivityHost/QA/*`.
- Em runtime normal, `DebugStartActivity` e bloqueado (`SessionActivityHostDebugStartBlocked`).
- Caminho canonico de entrada mantido: `SessionOperationalPipeline -> SessionActivityEntryHandoff -> SessionActivityPipeline.StartFromPreparedHandoff`.
- Fechamento de observabilidade da fronteira: `StartFromPreparedHandoff` registra log operacional explicito `SessionActivityEntryHandoffAccepted` em `[OBS][SessionActivityPipeline][Handoff]`.
- O trace interno (`_state.AppendTrace`) de aceite/rejeicao continua preservado.

## Relação com Base 1.0 e Base 2.0

- Base 1.0 é histórico de leitura phase-owned de `IntroStage` e engagement disperso.
- Base 1.1 converte `IntroStage` em `Pipeline Policy` e centraliza lifecycle em `SessionActivityPipeline`.
- Base 2.0 futura pode extrair padrões de ativação se a Base 1.1 os provar.
