# SA-ACTOR-1B1B — Remove RetainRouteScoped materialization lifetime duplication

Status: IMPLEMENTED / DOCUMENTED / awaiting compile + smoke

## Resumo objetivo

Este corte remove `ActorMaterializationPolicyKind.RetainRouteScoped` do domínio de `PlayerParticipation`.

Decisão congelada após o patch:

- `ActorScope` continua sendo o owner canônico de lifetime.
- retenção entre activities agora é derivada explicitamente de `ActorScope.RouteScoped`.
- `MaterializationPolicy` continua existindo apenas como contrato de materialização/reuso, sem expressar retention de rota.
- os valores numéricos remanescentes do enum foram preservados para evitar remapeamento silencioso de dados antigos.

Este corte não:

- cria `SessionScoped`;
- altera ordering de `RouteExit`;
- remove `ActivityPlayerActorRegistry`;
- remove `ActivityActorExitRuntimeState` player exit bindings;
- remove `ActorParticipationRecord.RetainedForRoute`.

## Arquivos alterados

- `PlayerParticipation/Contracts/PlayerParticipationPolicyContracts.cs`
- `SessionActivity/Pipeline/Stages/ActivityEntryParticipantBindingStage.cs`
- `Actors/Docs/Audits/SA-ACTOR-1B1B-Remove-RetainRouteScoped-Materialization-Lifetime-Duplication.md`

## Símbolos removidos

- `ActorMaterializationPolicyKind.RetainRouteScoped`

## Como a decisão passou a derivar de ActorScope

Antes deste corte, o contrato ainda permitia uma duplicação semântica:

- `ActorScope.RouteScoped`
- `ActorMaterializationPolicyKind.RetainRouteScoped`

Depois deste corte:

- `RouteScoped` continua sendo o único contrato de retenção entre activities;
- `ActivityEntryParticipantBindingStage` só tenta reusar actor retido quando `participant.ActorScope == ActorScope.RouteScoped`;
- `MaterializationPolicy` deixa de oferecer uma segunda forma de dizer "reter na rota".

Em termos práticos:

- `ActivityScoped` não consulta retenção de rota no rebind de participant.
- `RouteScoped` continua elegível para reuso técnico via registry de retained handles.

## Pontos mantidos temporariamente

### `ActivityPlayerActorRegistry`

Mantido temporariamente porque ainda é o índice técnico que localiza handles retidos/reutilizáveis no ciclo atual.

Não foi removido neste corte porque a meta aqui era remover a duplicação contratual, não substituir o storage técnico.

### `ActivityActorExitRuntimeState` player exit bindings

Mantido temporariamente porque o exit player-specific ainda depende de correlação entre:

- actor result;
- binding de participação;
- `PlayerActorId` / `PlayerSlotId`.

### `ActorParticipationRecord.RetainedForRoute`

Mantido temporariamente porque ainda pode ser necessário como dado derivado técnico no inventory/feed.

Ele não foi promovido a owner de lifetime neste corte.

## Riscos

### Risco 1

Se algum fluxo externo dependia numericamente do enum antigo, o compile apontará esse problema.

### Risco 2

`RouteScoped` continua sendo reutilizado por registry técnico.
Se existir actor com `ActorScope.ActivityScoped` indevidamente persistido em registry, o novo guard do stage agora bloqueia esse reuso.

Esse risco é aceitável porque o comportamento correto para `ActivityScoped` é não atravessar activities.

### Risco 3

`MaterializeOnActivityEntry` e `ReuseExistingIfAvailable` continuam pouco diferenciados no runtime atual.
Este corte remove a duplicação de lifetime, mas não fecha ainda toda a semântica fina de materialização/reuso.

## Smoke necessário

Não executar neste corte.

Quando validar o patch:

- activity A -> activity B na mesma rota deve reusar actor `RouteScoped`;
- actor `ActivityScoped` não deve ser reutilizado entre activities;
- `RouteScoped` deve continuar sendo liberado no `RouteExit`;
- `ActivityScoped` deve continuar sendo liberado no `ActivityExit`;
- não deve existir dependência restante de `RetainRouteScoped`;
- não deve surgir `FATAL`, `foreign/stale` ou regressão de `ActorParticipationEntered` / `ActorPresentationReady`.

## O que não mudar agora

- não introduzir `ActorScope.SessionScoped`;
- não criar `ActivityExitPipeline`;
- não criar bridge/alias/seam;
- não remover ainda os trilhos técnicos temporários de registry e exit bindings.
