# SA-ACTOR-1C1-H8-CLOSURE — PlayerParticipation Identity Cleanup

## Status

CLOSED / PASS funcional + PASS arquitetural do corte.

## Objetivo

Fechar a sequência `SA-ACTOR-1C1-H8*` depois do PASS de `SessionScoped structural lifetime`, removendo configurabilidade indevida e corrigindo a propagação de IDs entre `PlayerSetDefinition`, `PlayerParticipation`, `SessionParticipationContext` e `ActivityEntryPipeline`.

## Problema auditado

Após `H7B2`, o player estrutural já sobrevivia corretamente na sessão, mas ainda havia resíduos de ownership incorreto:

```text
PlayerSetDefinition.Entry.actorScope permitia mudar o scope do Player.
ActorDefinitionId era usado como chave para reencontrar SessionParticipantBinding.
SessionParticipantId dependia de índice/ordem de entries.
ActorDefinitionAsset.ActorId alimentava o ActorId do player participante.
```

Esses resíduos violavam a separação Base 2.0:

```text
PlayerSlotId != SessionParticipantId
PlayerSelectionId != ActorDefinitionId
ActorDefinitionId != ActorId
ActorId != ActorInstanceRuntimeId
```

## Decisão congelada

```text
Player estrutural vindo de PlayerParticipation é sempre ActorScope.SessionScoped.
PlayerSetDefinition não expõe actorScope editável.
SessionParticipantId é derivado de PlayerSlotId.
Materialization seed resolution usa PlayerSlotId.
ActorDefinitionId identifica archetype/definition e só valida consistência.
PlayerSetDefinitionEntry.actorId é o ActorId default do participante.
ActorDefinitionAsset não é owner do ActorId do player participante.
```

## Cortes fechados

```text
H8A  — Player scope invariant cleanup.
H8C1 — PlayerSlot materialization resolution.
H8C2 — SessionParticipantId by PlayerSlotId.
H8C3 — Player ActorId owner cleanup.
```

## Matriz final

| Campo/identidade | Owner após H8 | Função |
|---|---|---|
| `playerSlotId` | `PlayerSetDefinitionEntry` / `PlayerParticipation` | assento/input lógico e chave de correlação seed -> participant |
| `playerSelectionId` | `PlayerSetDefinitionEntry` / selection/default policy | seleção/default do slot |
| `actorDefinitionId` | `ActorDefinitionAsset` / selection | archetype/definition selecionada |
| `actorId` | `PlayerSetDefinitionEntry` no corte atual | ActorId semântico do participante default |
| `actorScope` | `PlayerParticipation` runtime policy | sempre `SessionScoped` para player |
| `sessionParticipantId` | `PlayerParticipation` | `participant.{playerSlotId}` |
| `actorInstanceRuntimeId` | `ActivityEntryPipeline` / materialization | identidade runtime concreta da instância |

## Evidência aceita

Smoke canônico:

```text
Boot -> Menu -> Sandbox
Activity 01 entry
CompleteActivationWindow
RestartCurrentActivity
CompleteActivationWindow
CompleteCurrentActivity
Activity 01 -> Activity 02
BackToMenu / ExitToMenu
```

Critérios aceitos:

```text
sem erro CS
sem [ERROR]
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem invalid_required_placement
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
actorIdSource='PlayerSetDefinitionEntry'
seedActorIdSource='PlayerSetDefinitionEntry'
participantIdPolicy='PlayerSlotIdDerived'
sessionParticipantIds='participant.player.slot.1, participant.player.slot.2'
resolutionKey='PlayerSlotIdToSessionParticipantId'
SessionResetCompleted sessionActorCount='0'
```

## Respostas obrigatórias

### Qual pipeline é dono desta decisão?

| Decisão | Owner |
|---|---|
| Player é `SessionScoped` estrutural | `PlayerParticipation` / `OperationalPlayerParticipationStage` |
| SessionParticipantId do player | `PlayerParticipation` |
| Materialization seed resolution | `SessionActivityOperationalRouteConsumerEntryAdapter` como adapter de handoff, usando contexto resolvido |
| Materialização/reuso do Actor | `ActivityEntryPipeline` |
| Teardown estrutural do Actor | `SessionActivityPipeline` / teardown stages |

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

| Item | Categoria |
|---|---|
| `PlayerSetDefinitionEntry` | authoring data |
| `ActorScope.SessionScoped` do player | policy invariável de `PlayerParticipation` |
| `SessionParticipantId` derivado de `PlayerSlotId` | runtime context identity |
| `ActivityParticipantBinding` | runtime payload resolvido |
| `ActorMaterializationPlanEntryResolved` | fact/log de handoff adapter |
| `PlayerActorRuntimeHandle` | runtime handle |

### Isso é comportamento final ou bridge transitória?

Comportamento final:

```text
Player estrutural SessionScoped.
PlayerSlotId como correlação de participação.
ActorDefinitionId separado de ActorId.
ActorInstanceRuntimeId como runtime instance identity.
```

Bridge transitória aceita:

```text
PlayerSetDefinitionEntry.actorId como owner autoral temporário do ActorId default.
```

Essa bridge existe porque seleção/default real ainda é MVP. Ela não deve virar lookup runtime nem substituir `ActorInstanceRuntimeId`.

### Essa compatibilidade ainda é necessária?

Não foi preservada compatibilidade com o shape antigo. O corte removeu o shape incorreto:

```text
PlayerSetDefinition.Entry.actorScope
ActorDefinitionAsset.ActorId no fluxo de player
ActorDefinitionId como resolution key de participant
SessionParticipantId por índice
```

### O erro estava no sintoma ou na fronteira arquitetural errada?

Na fronteira arquitetural errada entre authoring de definition, participação de sessão e materialização de Actor.

### Existe owner duplicado para o mesmo lifecycle?

Para o lifecycle estrutural do player: não após H8A. `PlayerParticipation` é o owner do `SessionScoped`.

Para identidade do participante: não após H8C2/H8C3. `SessionParticipantId` vem de `PlayerSlotId`; `ActorId` default vem do `PlayerSetDefinitionEntry`; `ActorDefinitionAsset` não carrega mais `ActorId` do player participante.

## Fora do escopo

```text
Runtime join real.
Tela/sistema real de PlayerSelection.
Derivação final de ActorId por seleção/loadout futura.
Multiplayer/split-screen.
ActivityObject typed refs.
Activity/Profile authoring refs.
ActorPresentation/ActorAttribute typed authoring refs.
Redução global de logs fora dos pontos tocados.
```
