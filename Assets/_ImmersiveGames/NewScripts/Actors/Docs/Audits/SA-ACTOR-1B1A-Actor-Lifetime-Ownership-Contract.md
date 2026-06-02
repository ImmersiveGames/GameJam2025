# SA-ACTOR-1B1A — Actor lifetime ownership contract + migration plan

Status: AUDIT / DOCUMENTAL ONLY

## Resumo executivo

`ActorScope` já é o contrato canônico correto para decidir lifetime de `Actor`.

Hoje o repositório já trata corretamente:

- `ActorScope.ActivityScoped`;
- `ActorScope.RouteScoped`;
- `ActorInstanceId` derivado do scope;
- discovery/inventory genérico de `Actor`.

O problema remanescente não é o contrato base. O problema é ownership duplicado em trilhos player-specific:

- `ActorMaterializationPolicyKind.RetainRouteScoped`;
- `ActivityPlayerActorRegistry` como storage real de retenção;
- `PlayerActorParticipationState` como estado local de retenção;
- `PlayerActorRetainedForRoute` como fact semântica de decisão;
- `ActivityActorExitRuntimeState` preservando bindings player-specific para exit.

Decisão congelada neste corte:

- `ActorScope` decide lifetime/retention/release.
- `PlayerParticipation` decide slot/selection/participant.
- `MaterializationPolicy` pode decidir como materializar/reusar, mas não pode duplicar a regra de lifetime quando `ActorScope` já a define.
- registry é índice técnico, não owner final de lifecycle.
- facts observam resultado; não decidem retenção.

Este corte não altera runtime pesado.
Este corte não introduz `SessionScoped`.
Este corte não cria bridge, alias, seam, compat legado ou `ActivityExitPipeline`.

## Tabela canônica de lifetime

| ActorScope | Evento | Resultado canônico | Owner | Observação |
|---|---|---|---|---|
| `ActivityScoped` | `ActivityExit` | `Release` | actor lifecycle orchestration | Actor local da activity não atravessa exit local. |
| `ActivityScoped` | `RouteExit` | `Release` | actor lifecycle orchestration | Mesmo que ainda exista indevidamente no exit da rota, deve ser liberado. |
| `RouteScoped` | `ActivityExit` | `Retain` | actor lifecycle orchestration | Retenção entre activities da mesma rota. |
| `RouteScoped` | `RouteExit` | `Release` | actor lifecycle orchestration | Fim do lifetime da rota. |
| `Unknown` | qualquer evento | `Fail-fast` | validação/config/runtime guards | `Unknown` não pode participar do ciclo. |

### Fonte canônica atual

- `ActorScope` é definido em `Actors/Foundation/ActorModelContracts.cs`.
- `ActorInstanceId.FromScopedRuntimeActorIdentity(...)` já separa:
  - `RouteScoped` => identidade por `pipeline/session/route`;
  - `ActivityScoped` => identidade por `pipeline/session/activity/entry`.
- `ActorSceneDiscoveryStage` já valida `SceneActorScope` contra a fonte de discovery.
- `PlayerActorInstanceSource` já lê `runtimeActor.ActorScopeMetadata` em vez de decidir scope por especialização.

## Classificação dos conceitos atuais

| Conceito atual | Papel hoje | Classificação | Decisão | Justificativa |
|---|---|---|---|---|
| `ActorScope.RouteScoped` | contrato canônico de retenção entre activities da rota | contrato canônico | `manter` | Já é a forma correta de expressar lifetime route-level. |
| `ActorMaterializationPolicyKind.RetainRouteScoped` | policy em contrato de participação/materialização que repete retention de rota | duplicação semântica | `remover` | Duplica `ActorScope.RouteScoped`. `MaterializationPolicy` deve falar de materialização/reuso, não de ownership de lifetime. |
| `ActorParticipationRecord.RetainedForRoute` | flag derivada no inventory/feed | derivado observável | `transformar em fact observável` futuramente | Pode continuar temporariamente como dado derivado técnico, mas não como fonte de decisão. O owner real é `ActorScope`. |
| `PlayerActorRetentionKind.RetainedForRoute` | estado local serializado em componente player-specific | duplicação player-specific | `remover` | Duplicação direta do lifetime canônico. Specialization não deve own retention. |
| `PlayerActorRetainedForRoute` fact | fact emitido no exit player-specific | fact semântica errada | `remover` | Registra uma decisão de retention no trilho player-specific. O observável correto deve derivar de `ActorScope` + evento de exit. |
| `ActivityPlayerActorRegistry` route retention | storage real de handles retidos entre activities | índice técnico com ownership indevido | `transformar em índice técnico` | Pode continuar temporariamente para lookup de handles, mas não deve decidir retention/release. |
| `ActivitySceneActorRegistry` route retention | subíndice técnico de actors route-scoped descobertos | índice técnico | `manter` | Já está próximo do papel correto: índice técnico derivado de `ActorScope`. Não deve ganhar ownership adicional. |
| `ActivityActorExitRuntimeState` player exit bindings | preserva binding player-specific para resolver exit/retention correlata | state factual com acoplamento indevido | `migrar para ActorLifetimePolicy futuramente` | Enquanto houver exit player-specific, ainda precisa de correlação técnica. No desenho alvo, a regra de retention deve sair deste state e convergir no contrato canônico de lifetime. |

## Leitura objetiva item a item

### `ActorScope.RouteScoped`

Manter.

É o owner canônico de retenção entre activities dentro da mesma rota.
Não depende de `PlayerActor`.
Não depende de `PlayerParticipation`.

### `ActorMaterializationPolicyKind.RetainRouteScoped`

Remover.

Esse símbolo hoje duplica a semântica de `ActorScope.RouteScoped`.
Se uma activity quer reusar o actor já retido, isso é consequência do scope e do estado técnico disponível, não uma segunda regra de ownership em `PlayerParticipation`.

`MaterializationPolicy` pode continuar existindo apenas para decidir:

- materializar novo;
- reusar existente quando compatível;
- falhar quando reuso obrigatório não for possível.

Ele não deve reexpressar `retain/release`.

### `ActorParticipationRecord.RetainedForRoute`

Migrar para observação derivada.

Hoje a flag é útil no feed/inventory, mas ela não deve ser tratada como owner da retenção.
O valor correto é derivado de:

- `ActorScope.RouteScoped`;
- evento de saída atual;
- permanência válida no ciclo da rota.

No curto prazo pode permanecer por necessidade técnica no inventory.
No desenho alvo deve virar derivação observável, não contrato primário.

### `PlayerActorRetentionKind.RetainedForRoute`

Remover.

É duplicação local e player-specific de um conceito que já existe no contrato canônico.
Também empurra retenção para um componente de specialization, violando ownership.

### `PlayerActorRetainedForRoute` fact

Remover.

Facts devem registrar o que ocorreu, não introduzir uma semântica paralela de ownership.
Esse fact amarra retenção ao trilho player-specific.
No desenho alvo, a observabilidade deve ser emitida em termos de actor lifetime genérico.

### `ActivityPlayerActorRegistry` route retention

Transformar em índice técnico.

Hoje ele faz mais do que indexar:

- guarda os handles retidos;
- resolve retained/current;
- destrói instâncias em `ClearAllRouteRetained`.

Isso é ownership indevido.
No próximo corte ele deve permanecer apenas como suporte técnico temporário para lookup de handles, sem semântica própria de retenção.

### `ActivitySceneActorRegistry` route retention

Manter como índice técnico.

Ele já está mais próximo do shape correto:

- indexa entries descobertas;
- deriva route retention de `ActorScope.RouteScoped`;
- não cria regra paralela de player.

O cuidado é não promover esse registry a owner de release/lifetime.

### `ActivityActorExitRuntimeState` player exit bindings

Migrar futuramente para `ActorLifetimePolicy`.

Hoje ainda existe necessidade técnica porque o exit real de player depende de correlação entre:

- `ActorParticipationExitActorResult`;
- `ActivityParticipantBinding`;
- `PlayerActorId` / `PlayerSlotId`.

Mas isso é dívida transitória.
O state não deve continuar definindo retenção por `route_scoped_player_exit_bindings`.

## Arquivos relevantes auditados

- `Actors/Foundation/ActorModelContracts.cs`
- `SessionActivity/Pipeline/Stages/ActorSceneDiscoveryStage.cs`
- `Actors/Players/ActivitySetup/PlayerActorInstanceSource.cs`
- `Actors/Players/ActivitySetup/ActivityPlayerActorRegistry.cs`
- `Actors/ActivitySetup/ActivitySceneActorRegistry.cs`
- `SessionActivity/Pipeline/Stages/ActivityEntryParticipantBindingStage.cs`
- `PlayerParticipation/Contracts/ActivityParticipationContracts.cs`
- `PlayerParticipation/Contracts/SessionParticipationContracts.cs`
- `PlayerParticipation/Contracts/PlayerParticipationPolicyContracts.cs`
- `Actors/Players/Runtime/PlayerActorParticipationState.cs`
- `Actors/Players/ActivitySetup/PlayerActorParticipationAdapter.cs`
- `SessionActivity/Pipeline/Runtime/ActivityActorExitRuntimeState.cs`
- `SessionActivity/Pipeline/Stages/ActivityExitActorTeardownStage.cs`
- `Actors/ActivitySetup/ActorParticipationExitStageExecutor.cs`
- `SessionActivity/Pipeline/Stages/ActivityHandoffRuntimeResetStage.cs`

## Plano mínimo para SA-ACTOR-1B1B

### Objetivo

Executar o menor patch possível para começar a remover ownership duplicado sem refatoração pesada de runtime.

### Arquivos a alterar primeiro

- `PlayerParticipation/Contracts/PlayerParticipationPolicyContracts.cs`
- `PlayerParticipation/Contracts/ActivityParticipationContracts.cs`
- `PlayerParticipation/Contracts/SessionParticipationContracts.cs`
- `SessionActivity/Pipeline/Stages/ActivityEntryParticipantBindingStage.cs`
- `Actors/Players/Runtime/PlayerActorParticipationState.cs`
- `Actors/Players/ActivitySetup/PlayerActorParticipationAdapter.cs`
- `SessionActivity/Pipeline/Stages/ActivityExitActorTeardownStage.cs`

### Símbolo a remover primeiro

Remover primeiro:

- `ActorMaterializationPolicyKind.RetainRouteScoped`

Razão:

- é a duplicação mais explícita do contrato canônico;
- está no domínio errado (`PlayerParticipation`);
- sua remoção reduz ambiguidade sem exigir reescrever todo registry/state de uma vez.

### Símbolo a manter temporariamente por necessidade técnica

Manter temporariamente:

- `ActivityPlayerActorRegistry`
- `ActorParticipationRecord.RetainedForRoute`
- `ActivityActorExitRuntimeState` player exit bindings

Condição:

- somente como suporte técnico de lookup/correlação;
- não como owner semântico de retention/release;
- sem expandir API ou semântica.

### Sequência mínima sugerida

1. Remover `RetainRouteScoped` do enum e dos pontos que o propagam como regra de lifetime.
2. Fazer `ActivityParticipationContracts` e `SessionParticipationContracts` deixarem de tratar materialization policy como expressão de retention.
3. Ajustar `ActivityEntryParticipantBindingStage` para decidir reuso apenas a partir de `ActorScope` + disponibilidade técnica do actor.
4. Parar de emitir `PlayerActorRetainedForRoute`.
5. Parar de serializar/manter `PlayerActorRetentionKind.RetainedForRoute` como semântica de ownership.

## Riscos

### Risco 1 — Remover policy e perder reuso técnico atual

Se o código atual estiver lendo `RetainRouteScoped` como gate para rebind/reuse, a remoção precisa ser acompanhada de decisão direta por `ActorScope.RouteScoped`.

### Risco 2 — Exit player-specific ainda depender de binding de participação

Enquanto `ActivityExitActorTeardownStage` ainda abrir trilho player-specific, a correlação via `ActivityActorExitRuntimeState` continua necessária.
Por isso esse state não deve ser removido no primeiro patch.

### Risco 3 — Confundir release físico com decisão de lifetime

`ClearAllRouteRetained` ainda executa `Destroy` em player handles.
No próximo corte, a prioridade é separar:

- decisão de release;
- índice técnico;
- side-effect técnico.

### Risco 4 — Tentar introduzir `SessionScoped` cedo demais

Não fazer isso no SA-ACTOR-1B1B.

Os pontos atuais de identity, registry e reset ainda estão todos modelados em:

- current activity;
- retained for route.

## Smoke necessário para SA-ACTOR-1B1B

Não executar neste corte.

Quando o patch mínimo SA-ACTOR-1B1B existir, o smoke a validar deve confirmar:

- transição de activity dentro da mesma rota reutiliza actor `RouteScoped` sem nova regra player-specific de retention;
- `ActivityScoped` é liberado no `ActivityExit`;
- `RouteScoped` é liberado no `RouteExit`;
- não existe dependência restante de `RetainRouteScoped`;
- não existe `FATAL`;
- não existe `foreign/stale`;
- não existe regressão de `ActorPresentationReady`, `ActorParticipationEntered` e exit/back-to-menu.

Marcadores esperados no smoke futuro:

- actor route-scoped reutilizado entre activities da mesma rota;
- actor activity-scoped liberado ao sair da activity;
- actor route-scoped liberado ao sair da rota;
- ausência de emissão de `PlayerActorRetainedForRoute`;
- ausência de branch de retention baseada em `PlayerActor` como owner.

## O que não mudar agora

- não criar `ActorScope.SessionScoped`;
- não criar `ActivityExitPipeline`;
- não criar bridge/alias/seam;
- não preservar `RetainRouteScoped` como compatibilidade se ele só duplica `ActorScope.RouteScoped`;
- não mover ainda o runtime inteiro para um novo serviço pesado de lifetime.
