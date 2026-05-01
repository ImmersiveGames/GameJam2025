# Base 1.1 — Plano de Migração para Pipeline Convergence

## Fonte normativa

Este plano usa apenas os ADRs vivos da **Base 1.1**, `ADR-0060` a `ADR-0067`.

ADRs anteriores são histórico de referência e não devem ser usados como fonte normativa de ownership, exceto quando explicitamente citados pelos ADRs Base 1.1.

## Objetivo do plano

Conduzir a migração conceitual e prática da **Base 1.0** para a **Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos**.

A Base 1.1 não é Base 2.0, não é uma transição descartável e não propõe reorganização física imediata em `Core/Concrete/Adapter`.

O objetivo é dar shape final de pipeline aos fluxos concretos já materializados na Base 1.0, removendo ambiguidade de ownership e consolidando decisões de lifecycle dentro dos pipelines corretos.

---

## Princípio central

Toda correção deve responder com precisão:

```text
Qual pipeline é dono desta decisão?
```

A implementação futura só deve avançar quando o ownership estiver claro.

### Regra base

```text
Pipelines decidem ordem, lifecycle, policies e handoffs.
Módulos produzem Pipeline Facts ou Pipeline Commands.
Adapters executam side-effects comandados.
Executores técnicos aplicam estado/efeitos, sem decidir lifecycle.
```

### Forma obrigatória de migração

Cada migração deve seguir o formato:

```text
owner antigo -> novo pipeline owner -> papel final -> remoção do poder decisório antigo
```

Exemplo:

```text
Readiness decide entrada
-> Session Pipeline decide entrada
-> Readiness vira Pipeline Fact Producer
-> nenhum caminho antigo continua podendo liberar gameplay sozinho
```

### Antipadrão proibido

```text
novo pipeline + trilho antigo ainda ativo
```

Isso cria dois owners concorrentes e viola a Base 1.1.

---

## Diretrizes de implementação futura

Quando a implementação começar, as seguintes regras devem ser mantidas:

- não preservar arquitetura ruim por conveniência;
- não criar compat paralelo;
- não manter dois owners ativos;
- não criar fallback silencioso;
- não fazer workaround para passar smoke;
- não remendar IntroStage, Gates, InputModes, GameLoop, Actors ou Readiness se o problema for ownership;
- não reorganizar fisicamente a arquitetura em `Core/Concrete/Adapter`;
- não transformar Base 1.1 em Base 2.0;
- fail-fast é preferido quando um contrato obrigatório estiver quebrado;
- ausência válida deve virar `skip/no-content` explícito;
- eventos `foreign/stale` não podem alterar o pipeline ativo;
- toda decisão relevante precisa de `Pipeline Identity`.

---

# Visão geral das fases

| Fase | Nome | Objetivo | Prioridade |
|---|---|---|---|
| 0 | Preparação de contrato | Fechar escopo e critérios antes de implementar | Obrigatória |
| 1 | Session Pipeline central | Mover readiness/input/entry decision para o `Session Pipeline` | Alta |
| 2 | Activation / IntroStage | Rebaixar IntroStage para stage/policy executada | Alta |
| 3 | GameLoop / Gates / InputModes | Rebaixar executores técnicos para execução de comandos | Alta |
| 4 | Fallbacks e wiring obrigatório | Remover retornos mudos e fallbacks implícitos | Média |
| 5 | Fechamento documental | Consolidar ownership final e atualizar documentos | Final |

---

# Fase 0 — Preparação de contrato

## Objetivo

Garantir que a implementação futura tenha um alvo claro e não replique a arquitetura Base 1.0 com nomes novos.

Esta fase não implementa comportamento. Ela consolida o contrato de migração.

## Entradas

- `Base-1.1-Matriz-Inicial-de-Migracao-Corrigida-Auditoria-1.md`
- `Base-1.1-Consolidado-Final-Auditorias.md`
- ADR-0060 a ADR-0067

## Decisões congeladas

- `Run Pipeline` substitui o conceito antigo de macro.
- `Session Pipeline` substitui o conceito antigo de local.
- Readiness não decide lifecycle.
- IntroStage não decide activation.
- Gates, InputModes e GameLoop não decidem lifecycle.
- RunResult, RunDecision e PostRun pertencem a Deactivation / Continuity.
- ActorsExecution não precisa de refatoração estrutural agora.
- PhaseCatalog / PhaseOrdinalNavigation não precisa de refatoração estrutural agora.
- RunEndRail / Deactivation não precisa de refatoração estrutural agora.

## Entregáveis

| Entregável | Descrição |
|---|---|
| Mapa de owners | Lista de owner antigo, owner Base 1.1 e papel final |
| Ordem de migração | Sequência de fases e dependências |
| Critério de aceite | Condições para considerar cada bloco migrado |
| Corte de escopo | Áreas que não serão alteradas por terem passado na auditoria |

## Critério de saída

A fase está concluída quando cada achado P1/P2 tiver:

- owner antigo identificado;
- novo pipeline owner definido;
- papel final definido;
- ação recomendada definida;
- dependências conhecidas;
- risco de regressão conceitual identificado.

---

# Fase 1 — Session Pipeline central

## Objetivo

Transformar o `Session Pipeline` no dono real da decisão de entrada, readiness, handoff local e comando de gameplay/input.

Esta é a primeira fase de implementação porque readiness e input command são a fronteira mais clara entre informação e decisão.

## Problema atual

Alguns serviços ainda carregam linguagem de decisão:

- `PhaseEntryReadinessCoordinator` decide `Blocked/Ready`;
- `GameplayInteractionReadinessService` decide quando pedir `Gameplay` no `InputModes`;
- `GameplaySessionFlowPrepareOperationalHandoffService` possui retorno silencioso em caso de rota incompatível ou inválida.

Esses comportamentos fazem parte da Base 1.0 porque funcionavam como rails locais convenientes. Na Base 1.1, eles precisam ser reclassificados.

## Alvo Base 1.1

```text
Session Pipeline
-> decide entrada, handoff, readiness policy e input command

PhaseEntryReadinessCoordinator
-> produz Pipeline Fact de prontidão

GameplayInteractionReadinessService
-> produz/encaminha Pipeline Command, sem decidir policy

InputModes
-> executa o comando recebido
```

## Escopo conceitual

### 1. `PhaseEntryReadinessCoordinator`

Papel atual provável:

```text
Owner parcial de readiness/entry
```

Papel final:

```text
Pipeline Fact Producer
```

A responsabilidade final deve ser:

- observar fatos relevantes;
- compor snapshot de prontidão;
- carregar identidade explícita;
- publicar fato de readiness;
- nunca decidir entrada final;
- nunca liberar gameplay diretamente;
- nunca acionar GameLoop, Gate ou InputMode por conta própria.

### 2. `GameplayInteractionReadinessService`

Papel atual provável:

```text
Command producer com policy local embutida
```

Papel final:

```text
Pipeline Command Producer / bridge
```

A responsabilidade final deve ser:

- receber decisão do `Session Pipeline`;
- transformar decisão em comando aplicável;
- encaminhar pedido ao executor correto;
- preservar identidade do ciclo;
- rejeitar eventos `foreign/stale`;
- não decidir que o modo `Gameplay` deve ser aplicado.

### 3. `GameplaySessionFlowPrepareOperationalHandoffService`

Papel atual provável:

```text
Pipeline Adapter com retorno silencioso
```

Papel final:

```text
Pipeline Adapter com contrato explícito
```

A responsabilidade final deve ser:

- executar o handoff operacional comandado;
- validar identidade e rota;
- falhar explicitamente quando o contrato obrigatório estiver quebrado;
- registrar `skip/no-content` quando a ausência for válida;
- não encerrar fluxo com retorno mudo.

## Dependências

Esta fase deve preceder IntroStage e GameLoop/Gates porque:

- IntroStage depende do ponto canônico de activation dentro da sessão;
- GameLoop/Gates dependem de comandos de lifecycle e release emitidos pelo pipeline;
- InputModes só deve receber comandos depois que a origem desses comandos estiver clara.

## Critérios de aceite

A Fase 1 só deve ser considerada concluída quando:

- readiness não liberar gameplay;
- readiness não decidir entrada;
- readiness produzir apenas `Pipeline Fact`;
- `Session Pipeline` consumir o fact e decidir o handoff;
- comando de `InputMode Gameplay` nascer do `Session Pipeline`;
- `GameplayInteractionReadinessService` não tiver policy própria;
- todo fact/command relevante carregar identidade explícita;
- eventos `foreign/stale` forem inertes para o pipeline ativo;
- retorno silencioso em rota/handoff obrigatório for eliminado.

## Riscos

| Risco | Mitigação |
|---|---|
| Criar pipeline nominal, mas deixar readiness antigo liberar gameplay | Remover o poder decisório antigo no mesmo bloco de migração |
| Quebrar ordem de activation | Fazer Fase 1 antes de IntroStage |
| Duplicar comandos para InputModes | Definir uma única origem para o comando |
| Tratar ausência obrigatória como no-op | Usar fail-fast ou `skip/no-content` explícito |

---

# Fase 2 — Activation / IntroStage

## Objetivo

Reposicionar IntroStage como `Activation Stage / Pipeline Policy`, sem ownership local de activation.

## Problema atual

A auditoria confirmou que o problema não é ausência de `skip/no-content`.

O problema é onde a decisão nasce.

Atualmente:

- `IntroStageExecutionDecisionService.Decide` decide `Execute` vs `SkipNoContent`;
- a decisão usa `IntroStagePresenterHost.TryEnsureCurrentPresenter`;
- `HasIntroStage` pode ser materializado cedo a partir de presenter scope;
- `TryReleasePendingGameplayIntro` pode liberar pending intro sem comparar identidade suficiente.

Isso mantém activation dependente de resolução local e timing operacional.

## Alvo Base 1.1

```text
Session Pipeline
-> decide Execute / SkipNoContent / Complete activation

IntroStage
-> executa Activation Stage

IntroStage host/presenter
-> resolve instância concreta no momento canônico

IntroStageSessionService
-> produz Pipeline Fact de elegibilidade, com identidade válida
```

## Escopo conceitual

### 1. Decisão `Execute` vs `SkipNoContent`

Papel atual provável:

```text
Decisão local em service/orchestrator
```

Papel final:

```text
Pipeline Policy decidida pelo Session Pipeline
```

A decisão deve sair de `IntroStageExecutionDecisionService.Decide`.

A service pode continuar existindo como suporte operacional, mas não como owner da decisão.

### 2. Elegibilidade de IntroStage

Papel atual provável:

```text
Fact derivado cedo por resolução local de presenter
```

Papel final:

```text
Pipeline Fact produzido no momento canônico
```

A fact de elegibilidade deve ser gerada quando o `Session Pipeline` estiver no ponto correto de activation, com identidade válida.

Presenter local não deve antecipar policy.

### 3. Pending intro release

Papel atual provável:

```text
Executor técnico com guarda insuficiente contra stale events
```

Papel final:

```text
Executor técnico comandado pelo Session Pipeline, com identidade explícita
```

Nenhum `SceneTransitionCompletedEvent` deve liberar pending intro sem validação de identidade.

## Dependências

Esta fase depende da Fase 1 porque:

- activation deve consumir decisão/facts do `Session Pipeline`;
- readiness precisa estar rebaixado para fact;
- o ponto canônico de session entry precisa estar claro.

## Critérios de aceite

A Fase 2 só deve ser considerada concluída quando:

- IntroStage não decidir abrir;
- IntroStage não decidir pular;
- IntroStage não decidir encerrar activation como owner local;
- presenter local não decidir policy;
- presenter local resolver apenas instância concreta;
- `skip/no-content` continuar explícito;
- activation carregar identidade explícita;
- evento stale não puder liberar ou reabrir IntroStage;
- `Session Pipeline` for o único dono da decisão `Execute` / `SkipNoContent`.

## Riscos

| Risco | Mitigação |
|---|---|
| Mover decisão cedo demais sem readiness consolidado | Executar Fase 1 antes |
| Perder suporte a ausência válida de intro | Preservar `skip/no-content` explícito |
| Criar duplicidade de decisão entre pipeline e IntroStage service | Remover poder decisório antigo |
| Reabrir IntroStage por evento tardio | Validar `Pipeline Identity` antes de qualquer release |

---

# Fase 3 — GameLoop / Gates / InputModes

## Objetivo

Rebaixar GameLoop, Gates e InputModes para executores técnicos.

Na Base 1.1, esses blocos não decidem lifecycle. Eles executam estado e efeitos comandados pelos pipelines.

## Problema atual

A auditoria confirmou que:

- `GameLoopStateMachine` decide localmente transições como `Boot`, `Ready`, `Playing`, `Paused`, `RunEnded`;
- `GameplayStateGate` aceita reset/start/end com identidade mínima insuficiente;
- `GameplayStateSnapshot` pode cair em fallback para `Ready`;
- `InputModeService` tolera falta de wiring obrigatório com log e retorno.

Esses comportamentos aproximam executores técnicos de ownership de lifecycle.

## Alvo Base 1.1

```text
Run Pipeline / Session Pipeline
-> decidem lifecycle, pause/resume, start/end/reset, release/block

GameLoop
-> aplica estado operacional

Gates
-> bloqueiam/liberam estado conforme comando

InputModes
-> aplicam modo solicitado
```

## Escopo conceitual

### 1. `GameLoopStateMachine`

Papel atual provável:

```text
Lifecycle owner técnico
```

Papel final:

```text
Executor técnico de estado
```

A state machine deve aplicar transições comandadas, não inferir lifecycle por flags internas.

Ela pode continuar existindo, mas seu papel deve ser de execução, não de decisão.

### 2. `GameplayStateGate`

Papel atual provável:

```text
Executor técnico com aceitação reativa ampla
```

Papel final:

```text
Executor técnico comandado por Pipeline Command
```

O gate deve bloquear/liberar apenas quando receber comando válido do pipeline ativo.

Reset/start/end não devem ser aceitos apenas por reason/frame ou evento genérico sem identidade suficiente.

### 3. `GameplayStateSnapshot`

Papel atual provável:

```text
Snapshot com fallback operacional para Ready
```

Papel final:

```text
Snapshot factual sem fallback silencioso
```

Se uma dependência obrigatória estiver ausente, a resposta correta é fail-fast, não assumir `Ready`.

### 4. `InputModeService`

Papel atual provável:

```text
Executor com tolerância silenciosa a wiring ausente
```

Papel final:

```text
Executor técnico com contrato explícito
```

Se `PlayerInput` ou action map forem obrigatórios para gameplay, ausência deve falhar explicitamente.

Se forem opcionais em algum contexto, o rail chamador deve produzir `skip/no-content` explícito antes de expor a ação.

## Dependências

Esta fase deve vir depois de Session Pipeline e IntroStage porque:

- GameLoop precisa receber comandos de lifecycle já centralizados;
- Gates precisam receber comandos de release/block com identidade;
- InputModes precisa receber comandos cuja origem esteja definida.

## Critérios de aceite

A Fase 3 só deve ser considerada concluída quando:

- `GameLoop` não decidir lifecycle;
- `GameLoop` apenas aplicar estado comandado;
- `GameplayStateGate` não aceitar start/end/reset sem identidade explícita;
- `GameplayStateSnapshot` não mascarar falta de dependência obrigatória;
- `InputModeService` não tolerar wiring obrigatório ausente com log + return;
- evento stale não alterar estado ativo;
- pause/resume não criar lifecycle paralelo;
- Run Pipeline / Session Pipeline forem os únicos owners de lifecycle.

## Riscos

| Risco | Mitigação |
|---|---|
| Quebrar fluxo porque GameLoop ainda era owner implícito | Migrar após Fases 1 e 2 |
| Criar comandos duplicados para start/play/end | Definir origem única no pipeline |
| Transformar gate em policy adapter | Gate deve executar, não decidir |
| Falhar em cenas sem input por ausência opcional | Classificar contexto como obrigatório ou `skip/no-content` explícito |

---

# Fase 4 — Fallbacks e wiring obrigatório

## Objetivo

Remover tolerâncias herdadas da Base 1.0 que mascaram contratos quebrados.

Esta fase trata achados P2 e ajustes de hardening.

## Problema atual

Foram encontrados pontos onde o sistema não decide lifecycle incorretamente, mas ainda suaviza problemas obrigatórios:

- retorno silencioso em handoff;
- fallback implícito em bootstrap;
- no-op com warning em overlay de pause;
- input wiring ausente tolerado.

## Alvo Base 1.1

```text
Contrato obrigatório quebrado -> fail-fast.
Ausência válida -> skip/no-content explícito.
Side-effect opcional -> contrato explícito antes da execução.
```

## Escopo conceitual

### 1. `GameplaySessionFlowPrepareOperationalHandoffService`

Remover retorno mudo.

A decisão entre fail-fast e `skip/no-content` deve depender do contrato:

- rota obrigatória ausente ou incompatível: fail-fast;
- ausência válida de conteúdo: `skip/no-content` explícito.

### 2. `SceneFlowBootstrap.ResolveOrComposeCompletionGate`

Remover fallback implícito de `WorldResetCompletionGate(timeoutMs: 20000)` quando `ISceneTransitionCompletionGate` não existe.

Se completion gate for obrigatório, deve ser declarado e validado.

Se existir modo válido sem completion gate, isso deve estar formalizado como contrato explícito.

### 3. `GamePauseOverlayController`

Não engolir falha de injeção obrigatória.

Se pause/resume UI é obrigatória, falha deve ser explícita.

Se overlay é opcional, o sistema deve registrar `skip/no-content` antes de expor UI ou comandos dependentes.

### 4. `InputModeService`

Concluir a classificação de wiring obrigatório/opcional.

Em gameplay real, `PlayerInput` e action map tendem a ser obrigatórios.

## Critérios de aceite

A Fase 4 só deve ser considerada concluída quando:

- não houver log + return substituindo contrato obrigatório;
- todo fallback operacional tiver sido removido ou formalizado;
- dependências obrigatórias falharem explicitamente;
- ausências válidas forem `skip/no-content`;
- bootstrap não compuser comportamento crítico por conveniência;
- overlay de pause não expuser comando quebrado.

## Riscos

| Risco | Mitigação |
|---|---|
| Fail-fast excessivo em conteúdo opcional | Classificar obrigatório vs opcional antes de alterar |
| Remover fallback que ainda era necessário em dev | Substituir por configuração explícita, não por retorno silencioso |
| Quebrar pause overlay em cenas sem UI | Tratar como `skip/no-content` explícito se for ausência válida |

---

# Fase 5 — Fechamento documental

## Objetivo

Consolidar a Base 1.1 como arquitetura de pipelines determinísticos, com ownership explícito.

## Entregáveis

| Documento | Atualização esperada |
|---|---|
| Matriz de migração | Marcar cada área como migrada, aprovada ou pendente |
| Consolidado de auditorias | Marcar achados resolvidos e decisões finais |
| Checklist de ownership | Confirmar papel final de cada área |
| ADRs Base 1.1 | Ajustar apenas se houver necessidade de clarificação normativa |

## Não criar ADR novo por ansiedade

A Base 1.1 já possui o grupo normativo `ADR-0060` a `ADR-0067`.

Novo ADR só deve existir se uma decisão realmente nova aparecer.

A implementação esperada deve caber nos ADRs existentes.

## Checklist final

A Base 1.1 estará coerente quando:

```text
Run Pipeline decide lifecycle de run.
Session Pipeline decide lifecycle de sessão.
IntroStage é Activation Stage / Policy executada.
Readiness produz Pipeline Fact.
InputMode executa Pipeline Command.
GameLoop executa estado.
Gate executa bloqueio/liberação.
Adapter executa side-effect.
PauseResume não cria lifecycle paralelo.
Save não decide continuidade/progressão.
SceneFlow/Navigation não decidem sessão/run por conveniência operacional.
foreign/stale events não alteram pipeline ativo.
```

## Critério de fechamento

O plano estará concluído quando:

- não houver P1 aberto;
- P2s forem resolvidos ou explicitamente aceitos como conteúdo opcional com contrato;
- nenhum executor técnico decidir lifecycle;
- nenhum module/adapter reescrever identidade;
- nenhum handoff relevante ocorrer sem identidade explícita;
- nenhum fallback silencioso permanecer em contrato obrigatório.

---

# Backlog priorizado

## Prioridade 1 — Ownership central

1. `PhaseEntryReadinessCoordinator`
2. `GameplayInteractionReadinessService`
3. `IntroStageExecutionDecisionService`
4. `IntroStageSessionService`
5. `TryReleasePendingGameplayIntro`

## Prioridade 2 — Executores técnicos

1. `GameLoopStateMachine`
2. `GameplayStateGate`
3. `GameplayStateSnapshot`
4. `GameLoopEvents`
5. `InputModeService`

## Prioridade 3 — Fallbacks e hardening

1. `GameplaySessionFlowPrepareOperationalHandoffService`
2. `SceneFlowBootstrap.ResolveOrComposeCompletionGate`
3. `GamePauseOverlayController`
4. demais pontos de wiring obrigatório que surgirem durante implementação

## Fora do escopo imediato

As áreas abaixo passaram nas auditorias e não devem ser alteradas por ansiedade:

- `ActorsExecution`;
- `Run Pipeline / Deactivation`;
- `RunEndRail`;
- `RunResult`;
- `RunDecision`;
- `PostRun`;
- `PhaseCatalog`;
- `PhaseOrdinalNavigation`;
- `SceneTransitionService`;
- `GameNavigationService`;
- `SaveOrchestrationService`;
- `SceneFlowInputModeBridge`.

Elas só devem ser reabertas se uma implementação futura revelar novo handoff, evento ou ownership conflitante.

---

# Sequência recomendada

A sequência recomendada é:

```text
1. Session Pipeline / readiness
2. Activation / IntroStage
3. GameLoop / Gates / InputModes
4. Fallbacks P2
5. Documentação final
```

## Justificativa

O `Session Pipeline` deve vir primeiro porque ele precisa ser o dono da decisão antes que `IntroStage`, `GameLoop`, `Gates` e `InputModes` sejam rebaixados corretamente.

`IntroStage` vem depois porque depende do ponto canônico de session entry.

`GameLoop/Gates/InputModes` vêm depois porque são executores sensíveis: migrá-los antes pode criar uma arquitetura em que o pipeline existe no nome, mas a decisão real continua espalhada.

Fallbacks P2 vêm por último porque são importantes, mas não definem o owner principal do pipeline.

---

# Resultado esperado da Base 1.1

Ao final deste plano, a Base 1.1 deve estar organizada por ownership lógico, mesmo sem reorganização física:

```text
Run Pipeline
-> lifecycle de run
-> deactivation/continuity
-> run handoffs

Session Pipeline
-> lifecycle de sessão
-> activation policy
-> local/session handoffs
-> readiness decision
-> input command decision

Pipeline Facts
-> readiness
-> eligibility
-> runtime observations
-> actor/materialization facts

Pipeline Commands
-> apply input mode
-> block/release gate
-> apply GameLoop state
-> execute handoff

Pipeline Adapters
-> SceneFlow
-> Navigation
-> InputModes
-> Gates
-> GameLoop
-> Save
-> ActorsExecution

Executores técnicos
-> aplicam estado/efeito
-> não decidem lifecycle
```

A Base 1.1 fica, assim, como forma final de pipeline dos fluxos concretos da Base 1.0, pronta para uma futura generalização sem exigir que os mesmos problemas de ownership sejam resolvidos novamente.
