# ADR-2.0-0001 — SessionOperational Ownership Stabilization e Regra Anti-Deslocamento

## Status

Aceito / congelado.

Checkpoints congelados:

```text
SessionOperational pós-13C Normalization — PASS arquitetural parcial
SessionOperational Camera Presentation Normalization — PASS funcional + PASS arquitetural parcial
SessionOperational Audio + HandoffExit Ownership Normalization — PASS funcional + PASS arquitetural parcial
SessionOperational PlayerPreparation Endpoint Normalization — PASS funcional + PASS arquitetural parcial
SessionOperational suspicious ownership normalization — CLOSED with no known blocking ownership debt
SessionActivity Base 2.0 frozen checkpoint — SA-14E / SA-14B1 last runtime validated cut
```

Escopo do congelamento:

```text
SessionOperationalPipeline ownership stabilization
pós-cortes 11A–13C normalizados
Camera Presentation normalizada
Audio + HandoffExit normalizados
PlayerPreparation Endpoint Normalization concluída
auditoria geral de suspeitos fechada
anti-deslocamento de responsabilidades
não fechamento completo da Base 2.0
```

## Contexto

Durante a decomposição do `SessionOperationalPipeline`, os cortes 11A–11E corrigiram um problema real: `OperationalRouteMaterializationStage` tinha se tornado um composite/mini-pipeline. A ordem voltou para o `SessionOperationalPipeline`, enquanto stages reais permaneceram responsáveis por passos determinísticos.

Após o corte 11, os cortes 12/13 continuaram funcionais, mas expuseram um risco estrutural: mover responsabilidade de uma classe para outra sem estabilizar a fronteira final.

Esse padrão cria um ciclo infinito:

```text
Pipeline grande
-> extrai para Stage/Boundary/Recorder
-> novo componente cresce
-> extrai para outro componente
-> ownership continua ambíguo
```

A Base 2.0 existe para remover seams, bridges transitórias e falso genérico, não para redistribuir complexidade indefinidamente.

A normalização pós-13C corrigiu o principal desvio identificado:

```text
OperationalFactRecorder deixou de iniciar route operation.
OperationalFactRecorder deixou de resetar SessionOperationalRuntimeState.
OperationalFactRecorder deixou de possuir stage-order policy.
OperationalFactRecorder deixou de construir OperationalInputModeRequest.
OperationalFactRecorder deixou de construir SessionOperationalResult.
SessionOperationalPipeline voltou a ser owner de begin/reset/gate de ordem.
SessionOperationalStageOrderPolicy continua sendo a policy explícita de stage-order.
Reveal e Blackout foram achatados: a ordem áudio/fade voltou a ficar explícita no pipeline.
Boundaries permanecem begin/complete puros.
```

A normalização de Camera Presentation removeu o bridge genérico `ConsumerPresentation` do caminho ativo do `SessionOperational` e tornou RouteCamera/ActivityCamera explícitas:

```text
SessionOperationalPipeline mantém a ordem de camera release/prepare.
OperationalRouteCameraPresentationStage decide skip local de RouteCamera.
OperationalActivityCameraPresentationStage decide skip local de ActivityCamera.
SessionOperationalRouteCameraAdapter executa side-effect técnico comandado.
SessionOperationalActivityCameraAdapter executa side-effect técnico comandado.
Adapters de camera não decidem lifecycle/policy/handoff/profile como owner final.
```

A normalização de Audio + HandoffExit fechou os achados `High` restantes da auditoria geral de suspeitos:

```text
AudioAdapter deixou de decidir policy por RouteAudioMode.None.
OperationalRouteAudioStage mantém o skip route_audio_disabled / RouteAudioMode.None.
SessionActivityOperationalRouteHandoffExitAdapter deixou de decidir preflight por CurrentStage, CurrentRailKind ou HasPendingOperation.
OperationalHandoffExitStage passou a classificar o preflight de handoff exit no caminho stage/pipeline.
Adapters permanecem restritos à execução técnica comandada e falhas técnicas reais.
```

A normalização de PlayerPreparation fechou o débito Medium restante da auditoria geral de suspeitos:

```text
OperationalPlayerPreparationStage deixou de chamar PlayerPreparationStage.Execute diretamente.
OperationalPlayerPreparationStage agora chama IRoutePlayerPreparationEndpoint.
RoutePlayerPreparationEndpoint pertence ao domínio Actors.Semantic.Preparation.
PlayerPreparationStage.Execute permanece encapsulado como detalhe interno do endpoint.
SessionOperational continua owner do quando/ordem/lifecycle.
Actors.Semantic.Preparation fica owner da execução semântica por capability/endpoint explícito.
Não foi criado manager/coordinator/processor novo.
Não há fallback silencioso; o endpoint é dependência obrigatória.
```

## Decisão

Congelar a seguinte regra:

> Nenhuma responsabilidade pode ser extraída apenas para reduzir tamanho de arquivo ou aliviar uma classe. Toda extração precisa ter categoria arquitetural final, owner explícito e critério de aceite próprio.

O checkpoint pós-13C normalizado é aceito como **PASS arquitetural parcial** porque:

```text
pipeline mantém ordem/lifecycle/handoff;
stage executa passo determinístico;
boundary marca fronteira, sem chamar sub-stage;
recorder registra facts/traces, sem lifecycle/policy/command/result building;
policy classifica stage-order;
command carrega payload runtime resolvido, sem infraestrutura;
adapter executa side-effect comandado.
```

Este ADR não declara o `SessionOperationalPipeline` finalizado. Ele congela a fronteira mínima para impedir novo ciclo de deslocamento de responsabilidade. Camera Presentation, Audio e HandoffExit ficam aceitos como normalizações parciais; PlayerPreparation Endpoint Normalization fica aceita como normalização parcial: a chamada cross-boundary stage-to-stage foi removida do `SessionOperational` e substituída por endpoint explícito do domínio `Actors.Semantic.Preparation`.

## Fonte normativa local

Para `SessionOperationalPipeline`, as categorias ficam estabilizadas assim:

| Categoria | Responsabilidade |
|---|---|
| Pipeline | Ordem, lifecycle macro, handoff, chamada explícita de stages, decisão de iniciar/finalizar operação, begin/reset de route operation, gate macro de stage-order via policy. |
| Boundary | Marca fronteira de uma fase, normalmente begin/complete. Não decide ordem interna nem executa sub-stages. |
| Stage | Executa um passo determinístico. Pode classificar resultado local e registrar fatos locais, mas não vira mini-pipeline. |
| Policy | Classifica decisões, skips, bloqueios, stage-order, stale/foreign e failures. Não executa side-effect. |
| Command | Carrega payload runtime resolvido. Não carrega `Stage`, `Boundary`, `Adapter`, `Port`, `Func<T>` ou state mutável. |
| Fact | Registra algo ocorrido. Não executa side-effect, não cria command, não decide lifecycle. |
| Recorder | Persiste facts/traces em state técnico. Não decide lifecycle, não reseta lifecycle, não constrói command operacional, não constrói result de domínio e não possui policy de stage-order. |
| Adapter | Executa side-effect comandado. Não decide lifecycle/policy. |
| Snapshot | Captura estado observável/versionado. Não executa side-effect. |
| Authoring data | Declara intenção/configuração. Não executa runtime. |

## Regra de ownership

### `SessionOperationalPipeline`

Deve permanecer owner de:

```text
route operation lifecycle
begin/reset de route operation
ordem dos stages
gate macro de stage-order chamando SessionOperationalStageOrderPolicy
handoffs entre Operational e Activity
decisão de begin/completion da operação
sequência PreviousRouteExit -> Materialization -> Reveal -> Completion
```

Pode chamar stages explicitamente. Isso não é regressão. Regressão seria implementar a lógica dos stages dentro do pipeline ou esconder a ordem em stage/boundary/recorder.

### `SessionOperationalStageOrderPolicy`

Deve permanecer como policy explícita de stage-order.

Pode classificar:

```text
CanStart
CanAdvance
stage-order accepted/rejected
stale/foreign/order violations quando aplicável
```

Não executa side-effect, não grava state diretamente e não substitui o pipeline como owner da decisão de prosseguir.

### Boundaries

Boundaries são permitidos apenas quando forem fronteiras reais:

```text
OperationalPreviousRouteExitBoundary.Begin/Complete
OperationalRouteMaterializationBoundary.Begin/Complete
OperationalTransitionBlackoutStage.Begin/Complete enquanto mantido como stage-boundary nominal
OperationalRouteRevealStage.Begin/Complete enquanto mantido como stage-boundary nominal
```

Boundary não pode conhecer nem chamar sub-stages. Se chamar sub-stages, voltou a ser mini-pipeline.

### Stages

Stages executam passos determinísticos.

Exemplos válidos:

```text
OperationalFadeStage executa fade comandado.
OperationalRouteAudioStage executa áudio de reveal comandado.
OperationalSceneCompositionStage aplica composição de cena via adapter.
OperationalInputPreparationStage prepara input e submete input mode via adapter.
OperationalRouteCompletionStage monta resultado após fact de completion já aceito pelo pipeline.
OperationalRouteCameraPresentationStage classifica/solicita RouteCamera de forma determinística.
OperationalActivityCameraPresentationStage classifica/solicita ActivityCamera de forma determinística.
OperationalRouteAudioStage classifica/solicita RouteAudio de forma determinística.
OperationalHandoffExitStage classifica/solicita handoff exit de forma determinística.
```

Stage não pode virar mini-pipeline. Se um stage começa a chamar outros stages para representar uma sequência macro, a ordem deve voltar ao `SessionOperationalPipeline`.

### Adapters

Adapters executam side-effects comandados. Podem falhar por erro técnico real, por exemplo dependency ausente, executor indisponível, retorno inválido ou falha concreta do runtime chamado.

Adapters não podem decidir:

```text
lifecycle de route operation
policy de rota
skip/fail funcional por handoff/profile/mode
prioridade entre RouteCamera e ActivityCamera
preflight de SessionActivity baseado em estágio/current rail/pending operation
stage-order
fallback silencioso para config obrigatória ausente
```

Casos normalizados e congelados:

```text
SessionOperationalRouteCameraAdapter não decide activity_camera_has_priority / SkipWhenActivityHandoff / RouteCameraPresentationMode como owner final.
SessionOperationalActivityCameraAdapter não decide not_session_activity_entry_handoff / profile missing/disabled como owner final.
AudioAdapter não decide RouteAudioMode.None.
SessionActivityOperationalRouteHandoffExitAdapter não decide CurrentStage / CurrentRailKind / HasPendingOperation como policy de preflight.
```

### `OperationalFactRecorder`

Pode:

```text
AppendFact
AppendTrace
registrar rejeição factual já decidida pelo pipeline/policy
normalizar mensagem de fact
mapear stage para fact kind, se essa regra for estável e local ao registro
persistir facts/traces em SessionOperationalRuntimeState
```

Não pode:

```text
iniciar route operation
resetar lifecycle
possuir SessionOperationalStageOrderPolicy
chamar CanStart/CanAdvance
construir OperationalInputModeRequest
construir SessionOperationalResult
decidir stage order como owner final
executar adapter
chamar stage
classificar skip/failure funcional
```

A existência de métodos de registro chamados por stages é permitida quando o stage está registrando fato local da própria execução e não transferindo lifecycle/policy para o recorder.

### PlayerPreparation Endpoint Normalization — resolvido

A antiga chamada direta `OperationalPlayerPreparationStage -> PlayerPreparationStage.Execute` foi removida do caminho operacional.

Shape aceito:

```text
SessionOperationalPipeline
-> OperationalPlayerPreparationStage
-> IRoutePlayerPreparationEndpoint
-> RoutePlayerPreparationEndpoint
-> PlayerPreparationStage.Execute encapsulado no domínio Actors.Semantic.Preparation
```

Classificação:

```text
problema_anterior='cross-boundary stage-to-stage bridge'
severidade_anterior='Medium'
status='resolved'
owner_decisão='SessionOperationalPipeline'
owner_execução='Actors.Semantic.Preparation via endpoint explícito'
endpoint='IRoutePlayerPreparationEndpoint'
implementação='RoutePlayerPreparationEndpoint'
fallback_silencioso='proibido'
novo_layer_genérico='não criado'
```

Regras:

```text
não reintroduzir chamada direta stage-to-stage;
não usar PlayerPreparation como precedente para outros domínios;
não criar manager/coordinator/processor para esconder a fronteira;
não mover lifecycle operacional para Actors.Semantic.Preparation;
manter o endpoint como fronteira explícita e obrigatória.
```

## Regra anti-deslocamento

Antes de qualquer novo patch, responder obrigatoriamente:

1. Qual pipeline é dono desta decisão?
2. Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?
3. Isso é comportamento final ou bridge transitória?
4. Essa compatibilidade ainda é necessária?
5. O erro está no sintoma ou na fronteira arquitetural errada?
6. Existe owner duplicado para o mesmo lifecycle?
7. A extração remove owner duplicado ou apenas desloca responsabilidade?
8. O novo componente tem nome concreto e fronteira estável?
9. O novo componente pode ser descrito sem “manager/coordinator/processor” genérico?
10. O smoke/log comprova comportamento, mas a matriz comprova ownership?

Se a resposta for ambígua, a ação permitida é **auditoria**, não implementação.

## Decisão sobre cortes recentes

| Corte | Status arquitetural após normalização |
|---|---|
| 11A–11E | Aceitos. Corrigiram composite/mini-pipeline e consolidaram `OperationalRouteMaterializationBoundary`. |
| 12A | Aceito após reauditoria. `OperationalPreviousRouteExitBoundary` permanece begin/complete puro; ordem explícita voltou ao pipeline. |
| 12B | Aceito após correções de compilação e revisão. `Operational*Command` não deve carregar `Stage`, `Boundary`, `Adapter`, `Port`, `Func<T>` ou state mutável. |
| 12C | Aceito no escopo de command hygiene. Commands não carregam `RuntimeState`; state/facts não podem virar lifecycle dentro do recorder. |
| 13A | Aceito como auditoria. |
| 13B | Aceito somente após normalização. O recorder pode persistir facts/traces, mas não pode possuir lifecycle/policy. |
| 13C | Aceito após normalização como PASS arquitetural parcial. `InputPreparation` e `RouteCompletion` usam recorder sem transformá-lo em owner de lifecycle/policy/result building. |
| 13D | Rejeitado. Não aplicar. O conteúdo válido foi substituído pela normalização que devolveu begin/reset/gate ao pipeline. |
| Camera Presentation | Aceito após normalização como PASS funcional + PASS arquitetural parcial. `ConsumerPresentation` saiu do caminho ativo; RouteCamera/ActivityCamera ficaram explícitas; arbitragem ficou no pipeline/stage path; adapters ficaram técnicos. |
| Audio + HandoffExit | Aceito após normalização como PASS funcional + PASS arquitetural parcial. `AudioAdapter` e `SessionActivityOperationalRouteHandoffExitAdapter` deixaram de decidir policy/lifecycle funcional. |
| PlayerPreparation | Aceito após normalização como PASS funcional + PASS arquitetural parcial. Chamada stage-to-stage removida; `OperationalPlayerPreparationStage` usa `IRoutePlayerPreparationEndpoint`; `PlayerPreparationStage.Execute` fica encapsulado em `RoutePlayerPreparationEndpoint`. |

## Checkpoints congelados

### Nomes

```text
SessionOperational pós-13C Normalization — PASS arquitetural parcial
SessionOperational Camera Presentation Normalization — PASS funcional + PASS arquitetural parcial
SessionOperational Audio + HandoffExit Ownership Normalization — PASS funcional + PASS arquitetural parcial
SessionOperational PlayerPreparation Endpoint Normalization — PASS funcional + PASS arquitetural parcial
SessionOperational suspicious ownership normalization — CLOSED with no known blocking ownership debt
```

### Evidência funcional exigida

O smoke de aceite deve confirmar:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem error CS
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
InputCapabilityPrepared
InputModeRequestSubmitted
OperationalRouteCompleted
OperationalPreviousRouteExitBoundary ativo
OperationalRouteMaterializationBoundary ativo
RouteCameraReleased antes do unload da rota anterior quando houver route camera ativa
RouteCameraPresentationSkipped reason='activity_camera_has_priority' quando ActivityCamera tiver prioridade
ActivityCameraPresentationPrepared quando rota for SessionActivityEntry com profile válido
RouteRevealAudioSubmitted ou RouteRevealAudioSkipped preservado conforme policy da rota
OperationalHandoffExitSkipped/Completed preservado conforme cenário
```

### Evidência arquitetural exigida

A auditoria de ownership deve confirmar:

```text
OperationalFactRecorder sem TryBeginRouteOperation
OperationalFactRecorder sem _stageOrderPolicy
OperationalFactRecorder sem CanAcceptStage/CanStart/CanAdvance
OperationalFactRecorder sem _state.Reset
OperationalFactRecorder sem command/result building
SessionOperationalPipeline com begin/reset explícito da operação
SessionOperationalPipeline chamando SessionOperationalStageOrderPolicy
OperationalRouteRevealStage sem chamada interna a OperationalRouteAudioStage ou OperationalFadeStage
OperationalTransitionBlackoutStage sem chamada interna a OperationalFadeStage
Boundaries sem sub-stage interno
Stages sem mini-pipeline novo
Commands sem infraestrutura
sem novo manager/coordinator/processor
SessionOperationalRouteCameraAdapter sem policy de handoff/profile/mode como owner final
SessionOperationalActivityCameraAdapter sem policy de handoff/profile/missing profile como owner final
AudioAdapter sem RouteAudioMode.None como policy
SessionActivityOperationalRouteHandoffExitAdapter sem CurrentStage/CurrentRailKind/HasPendingOperation como policy
OperationalPlayerPreparationStage sem chamada direta a PlayerPreparationStage.Execute; execução encapsulada em RoutePlayerPreparationEndpoint
```

## Último checkpoint recomendado

### Arquitetural parcial

```text
SessionOperational suspicious ownership normalization — CLOSED with no known blocking ownership debt
```

### Último ponto rejeitado

```text
Corte 13D — rejeitado / não aplicar
```

### Débitos restantes conhecidos

```text
nenhum débito bloqueante conhecido no SessionOperational após PlayerPreparation Endpoint Normalization
```

### PlayerPreparation resolvido

```text
OperationalPlayerPreparationStage -> IRoutePlayerPreparationEndpoint -> RoutePlayerPreparationEndpoint
status='resolved / endpoint boundary accepted'
PlayerPreparationStage.Execute encapsulado no domínio Actors.Semantic.Preparation
```

### Próximas frentes permitidas

```text
seguir decomposição do SessionOperational apenas com auditoria/matriz quando houver mudança de fronteira;
não reabrir PlayerPreparation sem regressão factual ou nova evidência arquitetural;
não reabrir Camera/Audio/HandoffExit sem regressão factual ou nova evidência arquitetural.
Próxima frente runtime deve ser escolhida fora de SessionActivity salvo regressão concreta.
```

Condição: qualquer frente nova deve obedecer este ADR antes de patch. Se houver dúvida de owner, fazer auditoria/matriz primeiro.

## Consequências

- Patches futuros não podem ser motivados por tamanho de arquivo.
- Toda extração precisa ter owner e categoria definidos antes.
- `OperationalFactRecorder` não pode absorver lifecycle, stage-order policy, command building ou result building.
- `SessionOperationalPipeline` pode continuar chamando stages explicitamente; isso é esperado.
- `SessionOperationalPipeline` deve manter begin/reset/gate de route operation.
- `SessionOperationalStageOrderPolicy` classifica stage-order; não executa side-effect.
- Adapters de Camera, Audio e HandoffExit ficam restritos a side-effect técnico comandado.
- A antiga bridge `OperationalPlayerPreparationStage -> PlayerPreparationStage.Execute` está resolvida e não pode ser reintroduzida.
- Se uma classe nova começar a precisar de outro layer para ficar aceitável, o patch anterior deve ser reavaliado, não empilhado.
- Smoke continua obrigatório, mas não substitui aceite arquitetural.
- PASS funcional não implica PASS arquitetural.

## Critério de aceite arquitetural

Um corte de `SessionOperational` só pode ser aceito como PASS arquitetural quando:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem fallback silencioso
sem trilho paralelo novo
sem command carregando infraestrutura
sem boundary chamando sub-stages
sem stage virando mini-pipeline
sem recorder decidindo lifecycle/policy
sem recorder construindo command/result
sem adapter decidindo lifecycle/policy
sem owner duplicado para mesmo lifecycle
pipeline mantém ordem/lifecycle/handoff
policy classifica decisão/skip/failure/stage-order
stage executa passo determinístico
fact registra ocorrido
adapter executa side-effect comandado
matriz de ownership aprovada quando houver mudança de fronteira
smoke/log aprovado quando houver mudança runtime
```

## Política para próximas ações

A partir deste ADR aceito, a implementação em `SessionOperational` é permitida quando obedecer uma das categorias:

```text
correção de compilação
correção de regressão factual comprovada por smoke/log
documentação/auditoria
extração com owner/categoria final explícitos
normalização que remove owner duplicado sem criar novo layer
manutenção da fronteira PlayerPreparation via capability/port/endpoint explícito
continuação de outras frentes com matriz de ownership prévia quando necessário
```

Não é permitido:

```text
criar novo layer para resolver incômodo de tamanho/organização
mover lifecycle para recorder/boundary/stage por conveniência
mover ordem macro para stage composite
criar compat/trilho paralelo sem justificativa explícita
preservar compatibilidade só para evitar quebrar código transitório
aceitar smoke como prova suficiente de ownership
reintroduzir a bridge PlayerPreparation stage-to-stage
usar PlayerPreparation como precedente para chamadas stage-to-stage cross-boundary
```

## Fechamento

Checkpoints aceitos:

```text
SessionOperational pós-13C Normalization — PASS arquitetural parcial
SessionOperational Camera Presentation Normalization — PASS funcional + PASS arquitetural parcial
SessionOperational Audio + HandoffExit Ownership Normalization — PASS funcional + PASS arquitetural parcial
SessionOperational PlayerPreparation Endpoint Normalization — PASS funcional + PASS arquitetural parcial
SessionOperational suspicious ownership normalization — CLOSED with no known blocking ownership debt
```

Decisão congelada:

```text
SessionOperationalPipeline permanece owner de ordem/lifecycle/handoff.
SessionOperationalStageOrderPolicy permanece policy explícita de stage-order.
OperationalFactRecorder permanece recorder factual, sem lifecycle/policy/command/result building.
Boundaries permanecem begin/complete puros.
Stages não podem virar mini-pipelines.
Commands não carregam infraestrutura.
Adapters não decidem lifecycle/policy.
PlayerPreparation stage-to-stage foi removido do caminho operacional; o shape aceito é endpoint explícito de `Actors.Semantic.Preparation`.
```

Este ADR é a trava normativa para retomar a decomposição do `SessionOperationalPipeline` sem reintroduzir seams, bridges genéricas ou deslocamento de responsabilidade. Camera Presentation, Audio, HandoffExit e PlayerPreparation ficam congelados como normalizações aceitas em nível arquitetural parcial.
### SA-15C â€” RouteActivitySave contributor scope normalization

Status:

```text
CLOSED / PASS funcional + PASS arquitetural parcial
```

Regra normativa:

```text
RouteActivitySaveContributorScopePolicy Ã© a policy normativa de save-on-exit.
CurrentActivityObjectSnapshot Ã© o scope funcional ativo hoje.
CurrentRouteSaveContributors e RouteAndActivitySaveContributors existem como contrato/policy futura, sem infraestrutura ativa.
ActivityContent == null nÃ£o significa no-save; significa apenas ausÃªncia de contributors de ActivityContent.
activity_02 no-content deve classificar como NoActivityContentContributors / no_activity_content_contributors.
SnapshotPayloadExpectedButMissing fica reservado para contributors esperados com payload ausente.
NÃ£o criar LastUsefulActivityPayload como fallback.
NÃ£o criar scene scan para contributors.
Route/session contributors futuros exigem registry/inventory canÃ´nico prÃ³prio.
```
## SA-16F closure

- `SA-16F` - `Route/session save contributor inventory audit`: `CLOSED / Backlog controlado`.
  - The active `RouteActivitySave` flow remains limited to `CurrentActivityObjectSnapshot`.
  - `SessionActivityPipeline` provides the payload via `ISessionActivitySnapshotPayloadProvider`.
  - `ActivityObjectExitRuntimeState` remains the technical store for the payload.
  - `SaveRuntime` continues to execute technical storage only.
  - No real route-scoped or session-scoped save contributors exist yet.
  - Do not create generic save-contributor infrastructure until a real capability and a clear owner exist.
