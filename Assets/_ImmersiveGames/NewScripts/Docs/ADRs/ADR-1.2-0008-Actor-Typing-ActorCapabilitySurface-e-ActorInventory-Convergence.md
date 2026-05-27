# ADR-1.2-0008 — Actor Typing, ActorCapabilitySurface e Actor Inventory Convergence

Status: Accepted / Base 1.2  
Área: Actors Convergence / Convergência de Atores  
Base normativa: Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos  
Atualização: 4A–4D + H4D Hygiene — CLOSED / PASS

---

## Contexto

A Base 1.2 existe para migrar o legado de atores para o shape arquitetural da Base 1.1 sem preservar trilhos ruins da Base 1.0.

Durante a convergência inicial, o projeto tinha sinais de fragmentação:

- `PlayerActor` e `NonPlayerActor` eram tratados como rails separados;
- scanners de capabilities ainda dependiam de targets específicos de player;
- `ActorPresentation`, `ActorAttributes` e `ActorParticipation` tinham sobras específicas de `NonPlayer`;
- alguns caminhos usavam metadata como se fosse decisão funcional;
- identity player-specific possuía fallback implícito para `target.ActorId`;
- stores de attributes ainda usavam chave nominal de `NonPlayer`.

Esse ADR congela a intenção e o checkpoint atual após as fases:

- 4A — Actor Typing Foundation;
- 4B — ActorCapabilitySurface como fonte primária de scanners;
- 4C — ActorAttributes genérico;
- 4D — ActorParticipation genérico;
- H4D — Hygiene pós-convergência.

---

## Decisão

### 1. Actor é a raiz abstrata

`Actor` é a raiz runtime abstrata para entidades que participam ou interagem com gameplay.

`PlayerActor`, `NonPlayerActor`, `ObjectActor`, `EnemyActor`, `NpcActor` e outros futuros tipos são especializações concretas de `Actor`, não rails paralelos de lifecycle.

A tipagem concreta deve ser preferida para expressar variação estrutural. `ActorKind`, `ActorRole` e `ActorScope` podem existir como metadata/log/transição, mas não devem dirigir lifecycle central por `switch`, `if` ou parsing.

### 2. ActorCapabilitySurface é a fonte local primária de endpoints

`ActorCapabilitySurface` é a superfície local de capabilities do Actor.

Ela deve funcionar como cache/local discovery no root do Actor, sem ser owner de lifecycle global.

Scanners de Activity devem consumir:

- `ActorScanTarget`;
- `ActorCapabilitySurface`;
- endpoint local exposto pela surface.

Não é permitido que scanners migrados voltem a fazer varredura externa global para endpoints cobertos pela surface.

### 3. ActorInventoryFeed normaliza fontes transitórias

`ActorInventoryFeed` é o feed read-only que converte fontes runtime atuais em registros genéricos:

- `ActorInstanceRecord`;
- `ActorEntryRecord`;
- `ActorParticipationRecord`;
- `ActorScanTarget`.

Ele não decide lifecycle, não materializa e não substitui pipelines.

### 4. Capabilities migradas

As seguintes capabilities já usam o caminho canônico de Actor + Surface + Inventory:

- `ActorPresentation`;
- `ActorAttributes`;
- `CameraTarget`;
- `PermissionTarget`;
- `ActorParticipation` nominal.

### 5. ActorAttributes usa store genérico

O store ativo de attributes não deve ser nominal por `NonPlayerId`.

Checkpoint H4D-B3:

- store ativo por `ActorInstanceId`;
- readiness de participation consulta attributes por chave genérica;
- setup/release de attributes preservados;
- sem decisão por `ActorKind`.

### 6. ActorParticipation é genérico no caminho nominal

`NonPlayerActorParticipation*` saiu do caminho nominal.

O caminho nominal agora emite:

- `ActorParticipationEnterStarted`;
- `ActorParticipationEntered`;
- `ActorReady`;
- `ActorParticipationEnterSkipped`;
- `ActorParticipationEnterFailed`;
- `ActorParticipationEnterCompleted`;
- `ActorParticipationExitStarted`;
- `ActorParticipationExited`;
- `ActorParticipationExitSkipped`;
- `ActorParticipationExitFailed`;
- `ActorParticipationExitCompleted`.

Observabilidade por actor é obrigatória. Remover rails específicos não significa remover evidência per-actor.

### 7. Identity obrigatória não pode ser fabricada

Scanners player-specific de Camera/Permission não podem usar fallback para `target.ActorId` quando `PlayerActorIdentity` está ausente.

Se a capability exige identidade player-specific e a identity não existe:

- a capability não deve ser emitida;
- o caso deve ser observável;
- não pode haver fallback silencioso.

### 8. Strings e IDs são opacos

Não usar `StartsWith`, `EndsWith`, `Contains`, `Split`, `ToLowerInvariant + switch`, Regex ou parse de `actorId`, `activityId`, `capabilityId`, `routeOperationId`, `transitionId` ou `actorInstanceRuntimeId` para decidir lifecycle, setup, release, participation ou readiness.

Strings podem ser usadas para log, debug, persistência ou display.

---

## Consequências

### Positivas

- `ActorPresentation`, `ActorAttributes` e `ActorParticipation` deixam de ser rails `Player`/`NonPlayer`.
- Scanners convergem para `ActorCapabilitySurface`.
- Falhas de identity deixam de virar fallback silencioso.
- Attributes passam a ter store por `ActorInstanceId`.
- Participation tem observabilidade genérica por actor.
- `NonPlayerActorParticipationStage` e outcomes legados foram removidos.

### Negativas / trade-offs

- Ainda existem registries transitórios.
- PlayerActor ainda aparece como `transitional_non_registry_actor` na participation formal.
- `playerActorId/playerSlotId` permanecem em Camera/Permission/Movement por compatibilidade de contratos atuais.
- `NonPlayerActorDiscovery` ainda alimenta o feed transitório.

---

## Débitos não bloqueantes

Mover para 4E/4F ou hygiene posterior:

1. Integrar PlayerActor à policy/registry genérica de ActorParticipation.
2. Remover `transitional_non_registry_actor`.
3. Migrar registries/stores transitórios para `ActorInstanceId` quando houver contrato final.
4. Reduzir `playerActorId/playerSlotId` em referências player-centric quando Camera/Permission/Movement forem generalizados.
5. Rever nomes `NonPlayerActorDiscovery` quando o discovery de actors for unificado.
6. Consolidar warnings passivos do validator sem usar `ActivityCapabilityKind.Custom` como diagnóstico operacional.

---

## Critérios de aceite do checkpoint

O checkpoint 4A–4D + H4D Hygiene é aceito quando o smoke confirma:

- `RestartCurrentActivity PASS`;
- `Activity01ToActivity02 PASS`;
- `RouteExitBackToMenu PASS`;
- `PresentationEndpoint 3/3/2`;
- `AttributeEndpoint 2/2/1`;
- `CameraTarget 1/1/1`;
- `PermissionTarget 1/1/1`;
- `ActorPresentationSetupCompleted 3/3/2`;
- `ActorAttributeSetupCompleted 2/2/1`;
- `ActorParticipationEntered`, `ActorReady`, `ActorParticipationExited`;
- `ActorParticipationEnterCompleted`, `ActorParticipationExitCompleted`;
- sem `NonPlayerActorParticipation*`;
- sem `NonPlayerActorAttribute*`;
- sem `NonPlayerActorPresentation*`;
- sem `capability_kind_unsupported`;
- sem `required_presentation_not_ready`;
- sem `CameraTargetIdentityUnresolved` em cenário válido;
- sem `PermissionTargetIdentityUnresolved` em cenário válido;
- sem `FATAL`, `Exception` ou `error CS`.

---

## Status do checkpoint

4A–4D + H4D Hygiene: CLOSED / PASS.
