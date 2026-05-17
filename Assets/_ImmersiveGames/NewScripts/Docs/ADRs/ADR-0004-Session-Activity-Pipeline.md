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

Nota curta (fronteira de persistência de activity):
- `SessionActivityPipeline` não salva progression diretamente; snapshot de activity para `RouteActivitySave` deve vir de provider explícito futuro (`IProgressionSnapshotProvider`), mantendo `SessionOperationalPipeline` como owner do timing operacional.

## Checkpoint de Estado Real - Sandbox Funcional Minimo (2026-05-17)

- `SessionActivity` permanece congelada como sandbox funcional minimo Base 1.1.
- Entrada canonica ativa: `SessionOperationalPipeline -> SessionActivityEntryHandoff -> SessionActivityPipeline.StartFromPreparedHandoff`.
- `DebugStartActivity` e tooling/QA; nao e contrato de entrada de producao.
- `autoStart` nao e contrato de producao e nao inicia lifecycle em runtime normal.
- `SessionActivityPipeline` decide ciclo local de activation/running/pause/resume/completion local.
- Deactivation local por activity existe parcialmente no sandbox atual.
- Run-level deactivation/continuity nao pertence a `SessionActivity`; pertence ao `RunPipeline` futuro (ADR-0002).
- `ActivitySetup` real, gameplay input final e `PlayerActor` final ainda nao fazem parte deste checkpoint.
- `Activity Snapshot Provider` real ainda nao existe; `no_snapshot_provider` em `RouteActivitySave` permanece estado esperado.
- Gates/InputModes/adapters executam efeitos e observabilidade; nao decidem lifecycle semantico.
- Protecao contra `foreign/stale` permanece obrigatoria para impedir troca da activity ativa.

## Checkpoint de Lifecycle Local Explicito - Completion/Deactivation (2026-05-17)

Sequencia canonica local consolidada no sandbox Base 1.1:

`ActivityRunning`
-> `ActivityCompletionRequested`
-> `ActivityCompleting`
-> `DeactivationWindowStarted`
-> `DeactivationWindowSkippedNoContent`
-> `ActivityDeactivated`
-> `Completed` ou `NextActivity`.

Regras aplicadas:
- `CompleteCurrentActivity` nao pula direto para deactivation.
- `ContinueToNextActivity` so e aceito apos `ActivityDeactivated`.
- Nesta etapa, `DeactivationWindowSkippedNoContent` so e emitido quando `DeactivationWindowMode=None`.
- Se `DeactivationWindowMode=AdditiveScene`, o pipeline entra no trilho de janela explicita (`DeactivationWindowReady` -> `CompleteDeactivationWindow`) antes de `ActivityDeactivated`.
- Campo autoral legado `HasActivityResult` foi removido do contrato ativo de lifecycle local.
- Nao ha trilho paralelo de `ActivityResultPresentation*` no lifecycle local ativo.

## Checkpoint de ActivationWindow Nominal - Sem Conteudo Visual (2026-05-17)

Sequencia canonica de entrada local nesta etapa:

`SessionActivityEntryHandoffAccepted`
-> `ActivityActivationStarted`
-> `ActivationWindowStarted`
-> `ActivationWindowSkippedNoContent`
-> `ActivityRunning`.

Regras aplicadas:
- Nesta etapa nao existe presenter visual real de `ActivationWindow`; o suporte atual cobre apenas carga aditiva nominal e gate de conclusao explicita.
- `ActivationWindowSkippedNoContent` so e emitido quando `ActivationWindowMode=None`.
- Se `ActivationWindowMode=AdditiveScene`, o pipeline abre a janela por cena aditiva e para em `ActivationWindowReady` aguardando conclusao explicita.
- Campo autoral legado `HasActivation` foi removido do contrato ativo de lifecycle local.
- Nao ha trilho paralelo `IntroStage`/ativacao antiga no fluxo ativo.

## Checkpoint de Contrato Autoral das Activity Windows (2026-05-17)

Contrato autoral explicito adicionado para as janelas locais da Activity:

- `ActivationWindowMode` (por activity): `None`, `AdditiveScene`.
- `DeactivationWindowMode` (por activity): `None`, `AdditiveScene`.
- Campos legacy `HasActivation` e `HasActivityResult` permanecem removidos.

Regras de execucao nesta etapa (Base 1.1 sandbox):

- `ActivationWindowMode=None`: `ActivationWindowSkippedNoContent` e emitido antes de `ActivityRunning`.
- `ActivationWindowMode=AdditiveScene`: o pipeline executa load aditivo da cena autoral, entra em `ActivationWindowReady` e aguarda conclusao explicita da window antes de `ActivityRunning`.
- `DeactivationWindowMode=None`: `DeactivationWindowSkippedNoContent` e emitido antes de `ActivityDeactivated`.
- `DeactivationWindowMode=AdditiveScene`: o pipeline executa load aditivo, entra em `DeactivationWindowReady` e aguarda conclusao explicita antes de `ActivityDeactivated`.

## Checkpoint de ActivationWindow AdditiveScene - Primeiro Suporte Real (2026-05-17)

Sequencia nominal quando `ActivationWindowMode=AdditiveScene`:

`SessionActivityEntryHandoffAccepted`
-> `ActivityActivationStarted`
-> `ActivationWindowStarted`
-> `ActivationWindowAdditiveSceneLoadStarted`
-> `ActivationWindowAdditiveSceneLoaded`
-> `ActivationWindowReady`
-> `CompleteActivationWindow` (comando explicito)
-> `ActivationWindowCompleted`
-> `ActivationWindowAdditiveSceneUnloadStarted`
-> `ActivationWindowAdditiveSceneUnloaded`
-> `ActivityRunning`.

Regras aplicadas:
- Campo autoral novo por activity: `activationWindowAdditiveSceneKey` (`SceneKeyAsset`).
- Se `activationWindowAdditiveSceneKey` estiver ausente com `ActivationWindowMode=AdditiveScene`, o pipeline falha explicitamente.
- Se `activationWindowAdditiveSceneKey.SceneName` estiver vazio, o pipeline falha explicitamente.
- Se a cena configurada nao puder ser carregada (`CanStreamedLevelBeLoaded=false`), o pipeline falha explicitamente.
- A cena de activation carregada de forma aditiva e descarregada explicitamente apos `ActivationWindowCompleted` e antes de `ActivityRunning`.
- Se a cena esperada nao estiver carregada no momento do unload, o pipeline falha explicitamente (sem skip silencioso).
- `ActivationWindowCompleted` so ocorre via comando explicito `CompleteActivationWindow` com identity ativa valida e stage `ActivationWindowReady`.
- `ActivityRunning` so ocorre apos `ActivationWindowAdditiveSceneUnloaded` (modo `AdditiveScene`) ou apos `ActivationWindowSkippedNoContent` (modo `None`).

## Checkpoint de DeactivationWindow AdditiveScene - Primeiro Suporte Real (2026-05-17)

Sequencia nominal quando `DeactivationWindowMode=AdditiveScene`:

`ActivityRunning`
-> `ActivityCompletionRequested`
-> `ActivityCompleting`
-> `DeactivationWindowStarted`
-> `DeactivationWindowAdditiveSceneLoadStarted`
-> `DeactivationWindowAdditiveSceneLoaded`
-> `DeactivationWindowReady`
-> `CompleteDeactivationWindow` (comando explicito)
-> `DeactivationWindowCompleted`
-> `DeactivationWindowAdditiveSceneUnloadStarted`
-> `DeactivationWindowAdditiveSceneUnloaded`
-> `ActivityDeactivated`
-> `Completed` ou `NextActivity`.

Regras aplicadas:
- Campo autoral novo por activity: `deactivationWindowAdditiveSceneKey` (`SceneKeyAsset`).
- Se `deactivationWindowAdditiveSceneKey` estiver ausente com `DeactivationWindowMode=AdditiveScene`, o pipeline falha explicitamente.
- Se `deactivationWindowAdditiveSceneKey.SceneName` estiver vazio, o pipeline falha explicitamente.
- Se a cena configurada nao puder ser carregada (`CanStreamedLevelBeLoaded=false`), o pipeline falha explicitamente.
- `DeactivationWindowCompleted` so ocorre via comando explicito `CompleteDeactivationWindow` com identity ativa valida e stage `DeactivationWindowReady`.
- A cena de deactivation carregada de forma aditiva e descarregada explicitamente apos `DeactivationWindowCompleted` e antes de `ActivityDeactivated`.
- Se a cena esperada nao estiver carregada no momento do unload, o pipeline falha explicitamente (sem skip silencioso).
- `ContinueToNextActivity` permanece aceito somente apos `ActivityDeactivated`.

## Checkpoint de ActivityTransitionPolicy - Contrato Inicial (2026-05-17)

Contrato autoral por activity:

- `ActivityTransitionPolicy.CutWithCurtain`
- `ActivityTransitionPolicy.Seamless`

Regras desta etapa:

- Policy funcional atual: `CutWithCurtain`.
- `Seamless` existe apenas como contrato futuro e falha explicitamente como `unsupported`.
- Antes de preparar handoff para proxima activity, o pipeline registra observabilidade explicita da policy selecionada (`ActivityTransitionPolicySelected` + snapshot `activity_transition_policy_selected`).
- Nao existe fallback silencioso de `Seamless` para `CutWithCurtain`.
- A garantia de ordem permanece: `Current Activity -> DeactivationWindow -> ActivityDeactivated -> Next Activity`.

## Checkpoint de Navegacao sem Bypass de Deactivation (2026-05-17)

Regras aplicadas no lifecycle local:

- `GoToNextActivity`, `GoToPreviousActivity`, `GoToActivity` e `RestartCurrentActivity` nao podem mais desativar diretamente.
- Toda navegacao `Activity -> Activity` passa pelo mesmo trilho canonico de fechamento local:
  - `ActivityRunning`
  - `ActivityNavigationExitRequested` (ou `ActivityCompletionRequested` no fechamento regular)
  - `ActivityCompleting`
  - `DeactivationWindowStarted`
  - `DeactivationWindowSkippedNoContent` ou `DeactivationWindowReady -> CompleteDeactivationWindow -> DeactivationWindowCompleted`
  - `ActivityDeactivated`
  - `ActivityTransitionPolicySelected`
  - `ActivityHandoffPrepared`
  - entrada da proxima activity.
- `NextActivity` continua condicionado a ocorrer somente apos `ActivityDeactivated`.

Checkpoint de rota/scene unload (BackToMenu):

- Nesta etapa, saida de rota/scene com activity ativa sem fechamento canonico explicito e tratada como erro fatal em `SessionActivityHost` (`SessionActivityRouteExitWithoutCanonicalDeactivation`).
- Este fail-fast evita `PASS` falso com unload silencioso.
- O teardown canonico de saida de rota (sem handoff para outra activity) permanece como pendencia de integracao entre `SessionOperational` e `SessionActivity`.

Nota curta de governanca QA:

- O QA da `SessionActivity` deve validar o lifecycle canonico (activation/completion/deactivation/continue) e nao pode oferecer atalhos de navegacao que burlem `DeactivationWindow`.

## Checkpoint de Route Exit Teardown Integrado ao SessionOperational (2026-05-17)

- O fechamento de SessionActivity para saida de rota deixou de depender do OnDisable como mecanismo principal.
- Boundary ativo: SessionOperationalPipeline solicita teardown canonico a SessionActivity antes de SceneComposition quando houver unload da cena de Activity.
- SessionActivity executa somente seu lifecycle local (CompleteCurrentActivity/CompleteDeactivationWindow) e deve atingir ActivityDeactivated antes do unload.
- Se o teardown nao puder completar, o SessionOperationalPipeline bloqueia a rota com SessionActivityRouteExitBlocked e falha antes do unload.
- O fatal no SessionActivityHost permanece apenas como ultima protecao defensiva.

## Checkpoint - Distincao de Fechamento Local vs Route-Exit Close (2026-05-17)

- CompleteCurrentActivity continua sendo fechamento local de activity no catalogo e pode preparar ActivityHandoffPrepared para proxima activity.
- CloseForRouteExit e caminho canonico de fechamento para saida de rota/unload e **nao** prepara proxima activity no catalogo.
- Em CloseForRouteExit, o trilho encerra em ActivityDeactivated -> ClosedForRouteExit, sem ActivityTransitionPolicySelected, sem ActivityHandoffPrepared e sem ContinueToNextActivity.
