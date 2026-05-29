# ADR-2.0-0001 — SessionOperational Ownership Stabilization e Regra Anti-Deslocamento

## Status

Aceito / congelado.

Checkpoints congelados:

```text
SessionOperational pós-13C Normalization — PASS arquitetural parcial
SessionOperational Camera Presentation Normalization — PASS funcional + PASS arquitetural parcial
```

Escopo do congelamento:

```text
SessionOperationalPipeline ownership stabilization
pós-cortes 11A–13C normalizados
Camera Presentation normalizada no SessionOperational
anti-deslocamento de responsabilidades
não fechamento completo da Base 2.0
próximas frentes ainda exigem auditoria de ownership antes de patch
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

A frente de Camera Presentation foi retomada e normalizada depois deste checkpoint. O caminho genérico `ConsumerPresentation` foi removido do fluxo ativo de camera no `SessionOperational`, separando explicitamente `RouteCamera` e `ActivityCamera`.

A normalização de Camera Presentation congelou as seguintes decisões:

```text
RouteCamera e ActivityCamera são passos explícitos do SessionOperational.
A ordem de release/prepare de camera permanece visível no SessionOperationalPipeline.
A arbitragem RouteCamera vs ActivityCamera fica no path pipeline/stage, não nos adapters.
SessionOperationalRouteCameraAdapter executa side-effect técnico comandado.
SessionOperationalActivityCameraAdapter executa side-effect técnico comandado.
Adapters de camera não decidem lifecycle/policy por handoff/profile/mode.
RouteCamera pula em SessionActivityEntry por activity_camera_has_priority.
ActivityCamera prepara em SessionActivityEntry quando há profile válido.
RouteCamera prepara em rota frontend/no handoff quando há profile válido.
ActivityCamera pula/not required em rota frontend/no handoff.
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

Este ADR não declara o `SessionOperationalPipeline` finalizado. Ele congela a fronteira mínima para impedir novo ciclo de deslocamento de responsabilidade.

A normalização de Camera Presentation também é aceita como **PASS funcional + PASS arquitetural parcial** porque:

```text
ConsumerPresentation saiu do caminho ativo de camera;
RouteCamera e ActivityCamera ficaram explícitas;
prioridade/skip/fail de camera presentation saiu dos adapters;
adapters ficaram restritos a side-effect técnico;
pipeline/stage path manteve a arbitragem visível;
smoke confirmou comportamento sem regressão.
```

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
```

Stage não pode virar mini-pipeline. Se um stage começa a chamar outros stages para representar uma sequência macro, a ordem deve voltar ao `SessionOperationalPipeline`.

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

## Checkpoint congelado

### Nome

```text
SessionOperational pós-13C Normalization — PASS arquitetural parcial
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
```

## Checkpoint de Camera Presentation

### Nome

```text
SessionOperational Camera Presentation Normalization — PASS funcional + PASS arquitetural parcial
```

### Evidência funcional exigida

O smoke de aceite deve confirmar:

```text
sem FATAL
sem Exception
sem route_transition_failed
sem foreign/stale indevido
sem error CS
RouteCameraPresentationSkipped reason='activity_camera_has_priority'
ActivityCameraPresentationPrepared
ActivityCameraPresentationStagePrepared
RouteCameraReleased antes do unload da rota anterior quando houver route camera ativa
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

### Evidência arquitetural exigida

A auditoria de ownership deve confirmar:

```text
ConsumerPresentation ausente do caminho ativo de camera no SessionOperational
OperationalRouteCameraPresentationStage não chama sub-stage
OperationalActivityCameraPresentationStage não chama sub-stage
OperationalRouteCameraReleasePreviousStage não chama sub-stage
OperationalActivityCameraReleasePreviousStage não chama sub-stage
SessionOperationalRouteCameraAdapter sem decisão de policy/handoff/profile/mode
SessionOperationalActivityCameraAdapter sem decisão de policy/handoff/profile/mode
adapters de camera restritos a side-effect técnico e falha técnica real
pipeline/stage path decide RouteCamera vs ActivityCamera
commands sem infraestrutura
sem novo manager/coordinator/processor
```

## Último checkpoint recomendado

### Arquiteturais parciais

```text
SessionOperational pós-13C Normalization — PASS arquitetural parcial
SessionOperational Camera Presentation Normalization — PASS funcional + PASS arquitetural parcial
```

### Último ponto rejeitado

```text
Corte 13D — rejeitado / não aplicar
```

### Próximas frentes permitidas

```text
Auditar/normalizar Audio no SessionOperational
Auditar/normalizar Save no SessionOperational
```

Condição: qualquer nova frente deve obedecer este ADR antes de patch. Se houver dúvida de owner, fazer auditoria/matriz primeiro. Camera Presentation está congelada como PASS parcial e não deve ser reaberta sem regressão comprovada por smoke/log ou auditoria de ownership.

## Consequências

- Patches futuros não podem ser motivados por tamanho de arquivo.
- Toda extração precisa ter owner e categoria definidos antes.
- `OperationalFactRecorder` não pode absorver lifecycle, stage-order policy, command building ou result building.
- `SessionOperationalPipeline` pode continuar chamando stages explicitamente; isso é esperado.
- `SessionOperationalPipeline` deve manter begin/reset/gate de route operation.
- `SessionOperationalStageOrderPolicy` classifica stage-order; não executa side-effect.
- Se uma classe nova começar a precisar de outro layer para ficar aceitável, o patch anterior deve ser reavaliado, não empilhado.
- Smoke continua obrigatório, mas não substitui aceite arquitetural.
- PASS funcional não implica PASS arquitetural.
- `ConsumerPresentation` não deve voltar como bridge genérico de camera no `SessionOperational`.
- Adapters de camera não podem voltar a decidir policy por handoff/profile/mode.
- A arbitragem RouteCamera vs ActivityCamera deve permanecer visível no pipeline/stage path.

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
sem adapter de camera decidindo skip/fail por handoff/profile/mode
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
continuação de Audio/Save/Camera apenas com matriz de ownership prévia quando necessário
```

Não é permitido:

```text
criar novo layer para resolver incômodo de tamanho/organização
mover lifecycle para recorder/boundary/stage por conveniência
mover ordem macro para stage composite
criar compat/trilho paralelo sem justificativa explícita
preservar compatibilidade só para evitar quebrar código transitório
aceitar smoke como prova suficiente de ownership
```

## Fechamento

Checkpoints aceitos:

```text
SessionOperational pós-13C Normalization — PASS arquitetural parcial
SessionOperational Camera Presentation Normalization — PASS funcional + PASS arquitetural parcial
```

Decisão congelada:

```text
SessionOperationalPipeline permanece owner de ordem/lifecycle/handoff.
SessionOperationalStageOrderPolicy permanece policy explícita de stage-order.
OperationalFactRecorder permanece recorder factual, sem lifecycle/policy/command/result building.
Boundaries permanecem begin/complete puros.
Stages não podem virar mini-pipelines.
Commands não carregam infraestrutura.
```

Este ADR é a trava normativa para continuar a decomposição do `SessionOperationalPipeline`, especialmente nas próximas frentes de Audio, Save e demais normalizações de adapters/stages.
