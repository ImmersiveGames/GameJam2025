# ACTIVITY-OBJECT-CAPACITY-1A — Object Lifecycle Contribution Provider

## Status

`IMPLEMENTED / PENDING COMPILE + SMOKE`

## Objetivo

Mover o discovery de lifecycle de `ActivityObject` para um contrato homogêneo de contributions, evitando que o scanner/stage procure diretamente cada endpoint nominal:

```text
IActivityObjectResetEndpoint
IActivityObjectSnapshotProvider
IActivityObjectSnapshotRestoreEndpoint
IActivityObjectReleaseEndpoint
```

O comportamento runtime permanece igual: stages continuam decidindo quando resetar, capturar snapshot, restaurar ou liberar. Endpoints continuam executando somente a operação local comandada.

## Decisão

`ActivityObject` passa a declarar lifecycle por provider:

```text
IActivityObjectLifecycleContributionProvider
```

O provider publica contributions:

```text
IActivityObjectResetContribution
IActivityObjectSnapshotContribution
IActivityObjectSnapshotRestoreContribution
IActivityObjectReleaseContribution
```

Cada contribution aponta para o endpoint local executável já existente. Isso cria a fronteira intermediária correta sem alterar ainda o envelope de snapshot/save.

## Ownership

| Item | Owner correto |
|---|---|
| Discovery local de lifecycle | `IActivityObjectLifecycleContributionProvider` no próprio objeto/capability |
| Índice/inventory técnico | `ActivityObjectCapabilityScanner` |
| Ordem/fail/skip de reset/restore | `ActivityEntryPipeline` / entry stages |
| Snapshot/release de saída | `SessionActivityPipeline` / exit stages atuais |
| Persistência física | `SaveRuntime` / adapters, fora deste corte |

## Alterações

### Contratos adicionados

Arquivo:

```text
NewScripts/SessionActivity/Contracts/ActivityObjectLifecycleContributionContracts.cs
```

Tipos adicionados:

```text
ActivityObjectLifecycleContributionKind
ActivityObjectLifecycleContributionContext
IActivityObjectLifecycleContribution
IActivityObjectResetContribution
IActivityObjectSnapshotContribution
IActivityObjectSnapshotRestoreContribution
IActivityObjectReleaseContribution
IActivityObjectLifecycleContributionProvider
ActivityObjectResetContribution
ActivityObjectSnapshotContribution
ActivityObjectSnapshotRestoreContribution
ActivityObjectReleaseContribution
```

### Endpoints migrados para provider

Arquivos:

```text
NewScripts/SessionActivity/Authoring/ActivityObjectDefaultResetEndpoint.cs
NewScripts/SessionActivity/Authoring/ActivityObjectDefaultReleaseEndpoint.cs
NewScripts/SessionActivity/Authoring/ActivityObjectTransformSnapshotProvider.cs
NewScripts/SessionActivity/Authoring/ActivityObjectTransformSnapshotRestoreEndpoint.cs
```

Cada um agora implementa `IActivityObjectLifecycleContributionProvider` e declara sua contribution local.

### Scanner migrado

Arquivo:

```text
NewScripts/SessionActivity/Capabilities/Inventory/ActivityObjectCapabilityScanner.cs
```

Antes:

```text
behaviour is IActivityObjectResetEndpoint
behaviour is IActivityObjectSnapshotProvider
behaviour is IActivityObjectSnapshotRestoreEndpoint
behaviour is IActivityObjectReleaseEndpoint
```

Agora:

```text
behaviour is IActivityObjectLifecycleContributionProvider
provider.CollectActivityObjectLifecycleContributions(...)
```

O scanner continua criando `ActivityCapabilityDescriptor` e runtime references existentes para manter o runtime ativo estável.

### Snapshot contract validation migrada

Arquivos:

```text
NewScripts/SessionActivity/Pipeline/Stages/ActivityEntryObjectSetupStages.cs
NewScripts/SessionActivity/Pipeline/SessionActivityPipeline.cs
```

Os helpers locais que resolviam snapshot provider/restore endpoint por scan direto agora resolvem via lifecycle contributions.

## Fora do escopo

```text
SaveRuntime
snapshot envelope modular
ActivityObject runtime reference types
ActivityObjectCapabilityKind enum
ActivityObject release/reset execution stages
Actor lifecycle contracts
```

## Compatibilidade transitória mantida

Os endpoints antigos ainda existem porque continuam sendo o executor local comandado:

```text
IActivityObjectResetEndpoint
IActivityObjectSnapshotProvider
IActivityObjectSnapshotRestoreEndpoint
IActivityObjectReleaseEndpoint
```

O que deixa de ser canônico é o discovery direto desses endpoints. O canônico passa a ser a contribution declarada pelo provider.

## Perguntas obrigatórias

| Pergunta | Resposta |
|---|---|
| Qual pipeline é dono desta decisão? | `ActivityEntryPipeline` para setup/reset/restore; `SessionActivityPipeline`/exit stages para snapshot/release atual. |
| Isso é stage, policy, command, fact, adapter, endpoint, snapshot ou authoring data? | Contrato de contribution + scanner/inventory técnico. |
| Isso é comportamento final ou bridge transitória? | Provider/contribution é direção final. Runtime references por endpoint são transição até envelope/plan modular. |
| Compatibilidade necessária? | Apenas para manter execução atual dos endpoints enquanto não existe plan/envelope modular. |
| Erro no sintoma ou na fronteira? | Fronteira: scanner/stage conheciam endpoints concretos em vez de lifecycle contributions. |
| Existe owner duplicado? | Reduzido: discovery fica no provider; execution continua no endpoint; policy/ordem continuam no stage. |

## Critério de validação

Compile:

```text
sem erro CS
```

Smoke:

```text
sem FATAL
sem Exception
sem route_transition_failed
ActivityObjectContributorDiscovery Passed
ActivityObjectSnapshotContractValidation Passed
ActivityObjectReset PassedApplied / PassedNoCommands
ActivityObjectSnapshotCapture Passed
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```
