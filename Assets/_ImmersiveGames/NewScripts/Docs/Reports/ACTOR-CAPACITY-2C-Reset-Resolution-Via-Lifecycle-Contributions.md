# ACTOR-CAPACITY-2C — Actor reset resolution via lifecycle contributions

## Status

`IMPLEMENTED / PENDING COMPILE + SMOKE`

## Objetivo

Trocar a resolução ativa de endpoints de reset de Actor para a fronteira homogênea de lifecycle contributions.

O corte remove do `PlayerActorResetEndpointResolver` o scan direto por:

```csharp
GetComponentsInChildren<MonoBehaviour>()
behaviour is IActorResetEndpoint
```

A resolução agora parte de:

```text
Actor -> ActorCapabilitySurface -> ResetContributionProviders -> IActorResetEndpoint executável
```

## Decisão arquitetural

`IActorResetEndpoint` continua sendo o contrato de execução local do reset neste corte.

O que muda é a fonte de resolução:

```text
Antes:
resolver varria todos os MonoBehaviours do Actor e aceitava qualquer IActorResetEndpoint.

Depois:
resolver usa ActorCapabilitySurface.ResetContributionProviders.
Cada provider precisa declarar uma IActorResetContribution válida.
O provider precisa ser executável via IActorResetEndpoint enquanto o adapter ainda executa esse contrato.
```

Isso mantém o runtime behavior, mas remove o trilho paralelo de scan direto no resolver.

## Owner correto

| Responsabilidade | Owner |
|---|---|
| Quando resetar | `ActivityEntryPipeline` / stage que monta e executa o reset |
| Quais grupos resetar | plano/comando de reset da entry |
| Como resetar estado local | capability específica |
| Como descobrir capabilities resetáveis locais | `ActorCapabilitySurface` |
| Execução física local | endpoint/capability local |

## Arquivo alterado

```text
NewScripts/Actors/Players/ActivitySetup/PlayerActorResetEndpointResolver.cs
```

## Mudança aplicada

`ResolveEndpointsOrFail(...)` agora:

1. Resolve o `Actor` runtime.
2. Resolve a `ActorCapabilitySurface` do actor.
3. Atualiza a surface com `RefreshFromLocalActorRoot()`.
4. Lê `ResetContributionProviders`.
5. Para cada provider:
   - cria `ActorCapabilityContributionContext`;
   - chama `TryCreateResetContribution(...)`;
   - valida contribution;
   - valida grupos suportados;
   - exige que o provider também seja `IActorResetEndpoint` enquanto o `ActorResetAdapter` ainda executa endpoint;
   - valida que o endpoint suporta os grupos que declarou.
6. Retorna os endpoints únicos produzidos por contributions válidas.

## O que foi removido

Foi removido o scan direto por `MonoBehaviour` dentro do resolver.

Não há mais resolução por:

```text
qualquer componente no Actor que implemente IActorResetEndpoint
```

Agora o componente precisa participar do modelo homogêneo:

```text
IActorResetContributionProvider + IActorResetEndpoint
```

## O que não foi alterado

```text
ActorResetAdapter
IActorResetEndpoint
IActorResetAdapter
PlayerActor
PlayerActorParticipationState
PlayerMovementController
Snapshot
Restore
Release
SaveRuntime
ActivityObject
Foundation/Platform/Pooling
```

## Compatibilidade transitória restante

`ActorResetAdapter` ainda executa `IActorResetEndpoint`.

Isso é aceitável para este corte porque a troca atual é apenas da fonte de resolução. O adapter ainda não recebe `IActorResetContribution` diretamente.

A próxima etapa pode avaliar se o adapter deve passar a consumir uma estrutura explícita de contribution/endpoint resolvida, mas isso não é necessário para fechar o 2C.

## Critério de aceite

Compile:

```text
sem erro CS
sem missing script
```

Smoke mínimo:

```text
Boot -> Menu -> Sandbox
CompleteActivationWindow
FirePrimary em ActivityRunning
RestartCurrentActivity
```

Critérios:

```text
sem FATAL
sem Exception
sem route_transition_failed
ActivityParticipantResetApplied preservado
resetGroups='Placement,ActivityParticipation' preservado
MovementBindingCompleted preservado
MovementControlEnabled preservado
CameraBindingCompleted preservado
ActorCommandDispatchAccepted preservado
RestartCurrentActivity preservado
```

## Perguntas obrigatórias

### Qual pipeline é dono desta decisão?

`ActivityEntryPipeline` continua dono do quando/ordem do reset. Este corte só corrige a fronteira de resolução local de capabilities resetáveis.

### Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data?

É normalização de `endpoint resolution` via `surface/contribution`. O endpoint continua local. Não é policy, snapshot nem save.

### Isso é comportamento final ou bridge transitória?

A resolução via `ActorCapabilitySurface.ResetContributionProviders` é direção final. A execução ainda por `IActorResetEndpoint` é transitória aceitável até o adapter receber shape mais explícito.

### Essa compatibilidade ainda é necessária?

A compatibilidade com execução por `IActorResetEndpoint` ainda é necessária neste corte para evitar trocar adapter e stage ao mesmo tempo. O scan direto por `MonoBehaviour` não é mais necessário e foi removido.

### O erro está no sintoma ou na fronteira arquitetural errada?

Estava na fronteira: o resolver ignorava a surface/contributions e criava um trilho paralelo de discovery.

### Existe owner duplicado para o mesmo lifecycle?

Menos do que antes. O reset local segue na capability; o pipeline decide quando. O resolver não escolhe capabilities arbitrárias por scan direto.
