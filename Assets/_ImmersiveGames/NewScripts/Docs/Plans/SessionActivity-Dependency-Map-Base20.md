# Base 2.0 — SessionActivity / ActivityEntry Dependency Map

> Documento de dependências para permitir avançar outras frentes sem perder o estado arquitetural atual.
>
> Status base: `RESET-INVENTORY-1..6 — CLOSED`.
>
> Escopo: `SessionActivityPipeline`, `ActivityEntryPipeline`, reset inventory-driven, bridges remanescentes, exit/release, QA, composition e próximos cortes.

---

## 1. Baseline congelado

### Bloco fechado

```text
RESET-INVENTORY-1..6 — CLOSED
```

### Shape validado

```text
ActivityCapabilityInventory
-> ActorCapabilityResetEndpointReference
-> ActivityEntryParticipantResetStage / QA inventory path
-> ActorResetAdapter
-> concrete Actor reset contribution
```

### Garantias já validadas por smoke

- `ActivityEntryParticipantBindingStage` não executa reset/placement.
- `ActivityEntryParticipantResetStage` é owner da execução de reset participante.
- `ActivityParticipantResetCommand.ResetGroups` usa `ActorResetGroup` diretamente.
- `ActivityStateResetGroup` fica restrito ao reset de `ActivityObject`.
- QA reset usa `ActivityCapabilityInventory` tipado.
- `ActorProjectileSpawnRuntimeTracker` retorna projéteis ao pool via `SpawnedRuntimeObjects`.
- `MovementTransient` permanece no domínio de Actor reset; não é convertido para `RuntimeTransient`.
- `activity_02` continua sendo no-content esperado.
- Checkpoints macro validados:
  - `RestartCurrentActivity`;
  - `Activity01ToActivity02`;
  - `RouteExitBackToMenu`.

### Não reabrir sem evidência nova

- `PlayerActorResetEndpointResolver`.
- `ActivityResetScopeResolver`.
- `ActorResetScopeResolver`.
- `ActorResetGroup[]` hardcoded em call site.
- `ActivityParticipantPlacementCommand`.
- `ActivityParticipantPlacementApplied`.
- `ActivityParticipantPlacementCommandIssued`.
- `ResetCommands` no `ActivityParticipantCommandPlan`.

---

## 2. Regra de separação atual

### Actor reset

Owner de discovery:

```text
ActivityCapabilityInventory / ActivityCapabilityActorLifecycleScanner
```

Owner de execução:

```text
ActivityEntryParticipantResetStage
```

Executor técnico:

```text
ActorResetAdapter
```

Contrato de grupos:

```text
ActorResetGroup
```

### ActivityObject reset

Owner de discovery:

```text
ActivityObjectCapabilityScanner / ActivityCapabilityInventory
```

Owner de execução:

```text
ActivityObjectReset stage path
```

Contrato de grupos:

```text
ActivityStateResetGroup
```

### Proibição de mistura

```text
ActorResetGroup não deve ser convertido para ActivityStateResetGroup.
ActivityStateResetGroup não deve aparecer no reset participante.
Actor reset não deve redescobrir endpoints fora do inventory.
```

---

## 3. Dependências arquiteturais remanescentes

## 3.1 SA-BRIDGE-1 — Remove aggregate `IActivityEntryRuntimeBridge`

### Problema

`ActivityEntryPipeline` ainda possui um bridge agregado transitório:

```csharp
private readonly IActivityEntryRuntimeBridge _runtimeBridge;
```

Mesmo já existindo bridges menores:

```csharp
private readonly IActivityEntryIdentityRuntimeBridge _identityBridge;
private readonly IActivityEntryFactRuntimeBridge _factBridge;
private readonly IActivityEntryContentPendingOperationRuntimeBridge _contentPendingOperationBridge;
private readonly IActivityEntryPreparationRuntimeBridge _preparationBridge;
```

### Depende de

- Reset inventory-driven fechado.
- Stages de setup/readiness funcionando após `RESET-INVENTORY-6`.

### Bloqueia

- Redução de interfaces implementadas por `SessionActivityPipeline`.
- Separação mais limpa entre macro pipeline e entry pipeline.
- Futuro `ActivityEntryPipeline` menos dependente do macro.

### Pode avançar agora?

```text
Sim, mas não é obrigatório antes de frentes funcionais independentes.
```

### Risco

```text
Médio
```

### Não misturar com

- `ActivityExitPipeline`.
- QA object reset/snapshot.
- Composition/DI cleanup.
- Actor/Projectile features.

### Critério de aceite

- `ActivityEntryPipeline` não recebe mais o bridge agregado se os sub-bridges cobrirem os usos.
- Se algum uso permanecer, deve ser justificado e isolado.
- Smoke mínimo: Boot -> Activity 01 -> CompleteActivationWindow -> RestartCurrentActivity.

---

## 3.2 SA-COMPOSITION-1 — Move concrete construction out of `ActivityEntryPipeline`

### Problema

`ActivityEntryPipeline` ainda instancia dependências concretas internamente, como:

```csharp
new ActorPresentationPlanResolver();
new UnityActorPresentationMaterializationAdapter();
new PlayerInputBindingAdapter(...);
new ActorCommandBindingAdapter();
new ActivitySetupInventoryBuilder();
new ActivityCapabilityInventoryCoordinator();
```

### Depende de

- Nenhuma dependência funcional direta do reset.
- Idealmente depois de `SA-BRIDGE-1`, mas pode ser auditado antes.

### Bloqueia

- Testabilidade de `ActivityEntryPipeline`.
- Troca limpa de adapters.
- Redução de responsabilidade de composition dentro do pipeline.

### Pode avançar agora?

```text
Pode auditar agora; implementar depois de SA-BRIDGE-1 é mais seguro.
```

### Risco

```text
Médio/Alto
```

### Não misturar com

- Mudança de lifecycle.
- Mudança de ordem dos stages.
- Snapshot/release.

### Critério de aceite

- Pipeline recebe dependências resolvidas pelo installer/composition.
- Sem lookup tardio via `DependencyManager.Provider` dentro do pipeline.
- Sem fallback silencioso se dependency obrigatória estiver ausente.

---

## 3.3 SA-EXIT-1 — Audit exit/release ownership

### Problema

`SessionActivityPipeline` ainda chama diretamente stages internos de exit/release/snapshot, como:

```text
ActivityObjectSnapshotCaptureStage
ActivityObjectReleaseStage
ActivityObjectContributorUnregisterStage
ActivityContentSceneUnloadDispatchStage
ActivityContentReleaseFinalizationStage
ActivityExitActorTeardownStage
```

### Depende de

- Reset inventory-driven fechado.
- Entry setup estável.

### Bloqueia

- Decisão sobre `ActivityExitPipeline` ou boundary de exit.
- Redução real de `SessionActivityPipeline` como executor de detalhes internos.
- Organização final de release/teardown.

### Pode avançar agora?

```text
Sim, como auditoria. Implementação deve esperar decisão explícita de owner.
```

### Risco

```text
Alto
```

### Decisão pendente

Escolher entre:

```text
A) manter exit stages sob SessionActivityPipeline, mas encapsular em boundary/stage wrapper claro;
B) criar ActivityExitPipeline se houver lifecycle próprio suficiente.
```

### Não misturar com

- `SA-BRIDGE-1`.
- Composition cleanup.
- QA cleanup.
- Novas features de actor/projectile.

### Critério de aceite futuro

- Um único owner para exit/release.
- `SessionActivityPipeline` não conhece detalhes internos de todos os stages.
- Release físico/side-effects ficam em adapters/stages corretos.
- Sem registry usado como fonte de verdade após teardown.

---

## 3.4 SA-QA-1 — Normalize QA object reset/snapshot paths

### Problema

QA de participante já usa inventory typed references. Ainda há QA macro para object reset/snapshot:

```text
TryQaResetCurrentActivityObjects
TryQaCaptureCurrentActivitySnapshotPayload
```

Eles chamam stages canônicos, mas ainda montam contexto dentro do macro pipeline.

### Depende de

- ActivityObject reset/snapshot estável.
- Exit/snapshot contract não estar em migração ativa.

### Bloqueia

- Redução de rails especiais de QA.
- Menor superfície no `SessionActivityPipeline`.

### Pode avançar agora?

```text
Pode, mas não é prioritário se o objetivo imediato for avançar features.
```

### Risco

```text
Médio
```

### Não misturar com

- Actor reset.
- Exit/release ownership.
- Snapshot provider macro boundary.

### Critério de aceite

- QA chama command/stage canônico por boundary fino.
- QA não decide lifecycle/policy.
- Sem trilho paralelo.

---

## 3.5 SA-ENTRY-ORDER-1 — Split `ExecuteSetupAndReadiness` sem mudar ownership

### Problema

`ActivityEntryPipeline.ExecuteSetupAndReadiness` ainda concentra a sequência de setup/readiness.

A ordem atual é aproximadamente:

```text
setup infrastructure
participant binding
capability object setup
participant reset
presentation setup
attribute setup
participation enter
player input binding
actor command binding
permission/gate binding
movement binding
camera binding
readiness completion
```

### Depende de

- Bridges minimamente estáveis.
- Idealmente após `SA-BRIDGE-1`.

### Bloqueia

- Leitura/manutenção do pipeline.
- Auditoria de ordem de stages.

### Pode avançar agora?

```text
Não recomendado antes de SA-BRIDGE-1.
```

### Risco

```text
Médio
```

### Regra

Não criar core genérico. Não criar novo pipeline. Apenas extrair blocos privados ou stages já existentes se houver ganho real.

---

## 4. Frentes que podem avançar sem depender da limpeza de bridges

Estas frentes podem avançar antes de `SA-BRIDGE-1`, desde que não mexam no reset participante nem no exit lifecycle.

### 4.1 Actor command / projectile refinements

Pode avançar se respeitar:

- Não colocar lógica de projectile no `SessionActivityPipeline`.
- Não criar tracker global por `Awake`.
- Não criar `ReturnAll`.
- Não redescobrir endpoints fora do inventory quando for lifecycle.
- Usar `ActorCommandHub` / endpoints / adapters.

### 4.2 Permission/gate refinements

Pode avançar se respeitar:

- Permission continua como capability/gate runtime.
- Receivers/endpoints tipados.
- Sem branch player/non-player onde Actor/capability resolve.
- Sem misturar permission com reset.

### 4.3 Actor Presentation / Attribute refinements

Pode avançar se respeitar:

- Presentation/Attribute continuam stages próprios.
- Sem side-effect no fact.
- Release deve respeitar owner de exit atual até SA-EXIT.

### 4.4 Documentation/status cleanup

Pode avançar se for apenas documentação UTF-8 e fechamento de cortes.

Não deve alterar runtime.

---

## 5. Frentes que NÃO devem avançar antes de decisão de dependência

### 5.1 Criar `ActivityExitPipeline`

Não criar ainda sem `SA-EXIT-1`.

Motivo:

```text
Pode criar owner duplicado para exit/release.
```

### 5.2 Remover `SessionActivityPipeline` bridges em bloco grande

Não fazer em bloco único.

Motivo:

```text
Risco alto de quebrar entry, exit, QA e snapshot ao mesmo tempo.
```

### 5.3 Reorganização estética de pastas

Não fazer.

Motivo:

```text
Não reduz owner nem lifecycle; aumenta ruído de diff.
```

### 5.4 Novo core genérico de pipeline

Não fazer sem necessidade concreta.

Motivo:

```text
Base 2.0 rejeita falso genérico.
```

---

## 6. Ordem recomendada dos cortes arquiteturais pendentes

### Ordem segura

1. `SA-BRIDGE-1` — remover/reduzir `IActivityEntryRuntimeBridge` agregado.
2. `SA-COMPOSITION-1` — mover construção concreta do `ActivityEntryPipeline` para composition/installer.
3. `SA-EXIT-1` — auditar ownership de exit/release.
4. `SA-EXIT-2` — implementar boundary escolhido para exit/release.
5. `SA-QA-1` — normalizar QA object reset/snapshot.
6. `SA-ENTRY-ORDER-1` — dividir `ExecuteSetupAndReadiness` sem mudar ownership.

### Ordem se quiser avançar features antes

1. Congelar `RESET-INVENTORY-1..6` como baseline.
2. Avançar feature isolada em Actor/Command/Permission/Presentation.
3. Exigir smoke com:
   - `RestartCurrentActivity`;
   - `Activity01ToActivity02`;
   - `RouteExitBackToMenu`;
   - QA reset se tocar Actor/projectile/reset.
4. Retomar `SA-BRIDGE-1` depois.

---

## 7. Smoke base para qualquer frente futura

### Smoke mínimo

```text
Boot -> Menu -> Sandbox -> Activity 01
CompleteActivationWindow
RestartCurrentActivity
Activity01ToActivity02
RouteExitBackToMenu
```

### Smoke adicional se tocar Actor/Projectile/Reset

```text
FirePrimary 3 vezes
QaResetCurrentPlayerActor
FirePrimary 2 vezes
RestartCurrentActivity
```

### Critérios negativos

```text
sem FATAL
sem Exception
sem route_transition_failed
sem checkpointStatus='Failed'
sem foreign/stale indevido
sem fallback silencioso
sem trilho paralelo novo
```

### Critérios positivos para reset fechado

```text
ActivityCapabilityInventoryPreviewObserved
actorLifecycleCapabilityCount='4' ou equivalente
ActivityParticipantResetAppliedFromInventory
resetGroups='ActivityParticipation,SpawnedRuntimeObjects,MovementTransient,Placement'
ActorResetQaAppliedFromInventory
ActorProjectileSpawnedRuntimeObjectReturnedToPool
```

---

## 8. Resumo executivo

O sistema está em um ponto seguro para avançar outras frentes, desde que o bloco de reset não seja reaberto.

O próximo débito arquitetural real é o agregado `IActivityEntryRuntimeBridge` dentro de `ActivityEntryPipeline`, mas ele não bloqueia necessariamente features independentes.

Prioridade de arquitetura:

```text
1. SA-BRIDGE-1
2. SA-COMPOSITION-1
3. SA-EXIT-1
```

Prioridade se for avançar feature:

```text
Manter reset fechado.
Não mexer em exit/release.
Não criar rails paralelos.
Exigir smoke completo antes de aceitar PASS.
```
