# ADR-1.2-0007 — Capability Discovery e Activity Capability Inventory

**Status:** Accepted / marco normativo Base 1.2  
**Data:** 2026-05-23  
**Complementa:** ADR-0014 e ADR-1.2-0001 a ADR-1.2-0006; não substitui a Base 1.1  
**Escopo:** Base 1.2 — Actors Convergence / Capability Discovery dentro da fundação Base 1.1

---

## 1. Contexto

A Base 1.1 consolidou a **Pipeline Convergence / Convergência para Pipelines Determinísticos** e validou checkpoints importantes:

- `SessionOperationalPipeline`;
- `SessionActivityPipeline`;
- `ActivityEntryPipeline v0`;
- `PlayerActor readiness`;
- `PlayerInputBinding`;
- `MovementBinding + MovementControl`;
- `PlayerCameraEndpoint + ActivityCamera target binding`;
- `ActivityObject discovery/reset/snapshot/restore MVP`;
- `RouteActivitySave`;
- `SaveRuntime`, `Preferences`, `RuntimeConfigRegistry`, `InputModes`, `CameraPresentation` e demais runtimes técnicos.

A Base 1.2 — **Actors Convergence / Convergência de Atores** tentou migrar atores e capabilities para o shape da Base 1.1. Durante essa migração, ficou evidente um risco estrutural: mesmo quando o pipeline não executa diretamente todos os side-effects, ele tende a acumular conhecimento sobre cada capability concreta.

Exemplos observados:

- `MovementControl` precisou ser migrado para `ActivityCapabilityPermissionRuntime`, mas ainda dependia de registro específico por `MovementBindingAdapter` / `PlayerMovementControlAdapter`.
- A tentativa de resolver autoria via `ActivityCapabilityPermissionBindings` reduziu hardcode, mas introduziu configuração manual redundante no prefab.
- O `SessionActivityPipeline` continuou carregando detalhes de setup, discovery, reset, save, camera, movement, presentation, attributes, non-player actors e objects.

O problema não é a ideia de pipeline determinístico. O problema é o pipeline virar um índice monolítico de todas as capacidades concretas.

---

## 2. Decisão

A partir deste ADR, a direção normativa passa a ser **Capability Discovery + Activity Capability Inventory**.

A regra central é:

```text
Módulo declara o que sabe consumir.
Componente declara o que oferece.
Pipeline faz a triagem determinística.
Inventory registra o resultado por identity.
Stages consomem o inventory.
Adapters executam side-effects comandados.
Componentes reagem localmente.
```

Isso substitui a direção anterior de criar bindings manuais ou stages específicos por capability sempre que uma nova função precisa participar do ciclo da Activity.

---

## 3. Relação normativa com ADRs anteriores

Este ADR **não cria Base 2.0** e não substitui a Base 1.1.

A Base 1.1 permanece fundação normativa congelada para:

- `SessionOperationalPipeline`;
- `SessionActivityPipeline`;
- `ActivityEntryPipeline` v0;
- `ActivityContent` load/release;
- readiness, input, movement, camera, snapshot/restore/release MVP;
- save/load boundary de rota/activity;
- identidade explícita e rejeição de `foreign/stale events`.

Os ADRs Base 1.2 anteriores permanecem vivos para seus domínios. Este ADR apenas esclarece que a convergência de capabilities deve usar `ActivityCapabilityInventory` e runtime references tipadas, evitando novos bindings manuais e novos stages específicos por capability sempre que o inventário resolver o caso.

Precedência:

```text
Base 1.1 congelada prevalece para pipelines e lifecycle canônico.
ADRs Base 1.2 prevalecem para atores/capabilities dentro da Base 1.1.
Este ADR prevalece apenas para o shape de Capability Discovery / ActivityCapabilityInventory dentro da Base 1.2.
```

Em caso de conflito com o arquivo antigo `ADR-2.0-0001`, o arquivo antigo deve ser removido ou tratado como rascunho histórico incorreto de numeração.


## 4. Diagnóstico arquitetural

A Base 1.1 e a Base 1.2 não foram inúteis. Elas provaram pontos importantes:

- ordem determinística importa;
- identity explícita é obrigatória;
- `foreign/stale events` precisam ser rejeitados;
- adapters não devem decidir lifecycle;
- config obrigatória quebrada deve falhar explicitamente;
- componentes locais devem decidir apenas reação local.

Mas a arquitetura de transição mostrou um limite:

```text
Pipelines não podem continuar acumulando conhecimento específico de Movement, Camera, Save, Reset, ActorPresentation, ActorAttributes, Interaction, NPC Brain e demais funções.
```

Se cada nova capability exige um novo stage específico no pipeline ou um novo binding manual centralizado, o sistema volta a se tornar monolítico.

---

## 5. Novo modelo conceitual

### 5.1 Pipeline

O pipeline continua dono de:

- ordem macro;
- lifecycle;
- identity;
- phase boundaries;
- policy macro;
- handoffs;
- rejection of `foreign/stale events`;
- montagem do inventário determinístico.

O pipeline não deve conhecer detalhes concretos como:

- como Movement liga/desliga;
- como Attack cancela input;
- como um NPC pausa AI;
- como um objeto captura transform snapshot;
- como um endpoint reseta seus campos;
- como um objeto restaura estado local.

### 5.2 Módulos

Módulos instalados declaram interesse em tipos de capability.

Exemplos:

```text
Permission Module procura IActivityCapabilityPermissionReactionTarget.
Reset Module procura IActivityResetEndpoint.
Snapshot Module procura IActivitySnapshotProvider e IActivitySnapshotRestoreEndpoint.
Camera Module procura IPlayerCameraEndpoint ou IActivityCameraTargetProvider.
Movement Module procura IPlayerMovementEndpoint.
Interaction Module procura IActivityInteractionEndpoint.
```

O módulo pode fornecer:

- scanner/discoverer;
- validator;
- descriptor builder;
- adapter;
- policy resolver local;
- seção de inventory.

O módulo não decide lifecycle global.

### 5.3 Componentes

Componentes locais declaram o que oferecem por contrato/interface.

Exemplos:

```text
PlayerMovementController implementa capability de reação a permission ou movement endpoint.
ActivityObjectTransformSnapshotProvider implementa snapshot provider.
ActivityObjectDefaultResetEndpoint implementa reset endpoint.
DoorInteractionController implementa interaction endpoint.
NpcBrain implementa gameplay-control reaction target.
```

O componente pode declarar:

- capability type;
- local policy;
- required/optional;
- supported groups;
- priority/order key local;
- schema/version local quando necessário;
- asset reference quando a identidade autoral precisa ser estável.

O componente não se registra sozinho no `Awake`, `Start` ou `OnEnable`.

### 5.4 ActivityCapabilityInventory

O `ActivityCapabilityInventory` é o resultado da triagem por entry.

Ele deve ser associado a:

```text
pipelineId
sessionStateId
activityId
entrySequence
owner runtime identity
component path / stable local key
capability descriptor
```

Ele pode conter seções como:

```text
PermissionTargets
ResetEndpoints
SnapshotProviders
SnapshotRestoreEndpoints
MovementEndpoints
CameraEndpoints
InteractionEndpoints
PresentationEndpoints
AttributeEndpoints
```

O inventory é produzido deterministicamente e consumido pelos stages.

---

## 6. IDs e referências

A nova direção reduz IDs manuais redundantes.

### 6.1 IDs autorais

Devem permanecer como asset/reference quando precisam ser estáveis:

```text
ActorDefinition
ActivityDefinition
PermissionDefinition/PermissionId
ResetGroupId
SnapshotSchemaId
InteractionId
PresentationProfile
AttributeDefinition
```

### 6.2 IDs runtime

Devem ser gerados pelo pipeline/inventory:

```text
pipelineId
sessionStateId
activityId
entrySequence
ownerId
componentPath
capabilityKey
```

Não deve ser necessário configurar manualmente `participantId` quando a identidade pode ser derivada de owner + component + capability + entry.

---

## 7. Sobre ActivityCapabilityPermissionBindings

`ActivityCapabilityPermissionBindings` e os experimentos anteriores com `PlayerMovementPermissionParticipant` / `ActivityCapabilityPermissionParticipant` são classificados como **transitórios**.

Eles não representam o shape final.

Motivo:

```text
Se o componente já implementa um contrato de capability, um binding manual que repete essa relação vira redundância autoral e nova fonte de erro.
```

A direção preferida é:

```text
Contrato/interface local + scanner de módulo + inventory determinístico.
```

Bindings manuais podem ser reavaliados no futuro apenas como caso excepcional, não como caminho padrão.

---

## 8. Aplicação por domínio

### 8.1 Permission / Gameplay Control

Em vez de:

```text
MovementBindingAdapter registra receiver específico de movement.
ActivityCapabilityPermissionBindings aponta manualmente para PlayerMovementController.
```

Preferir:

```text
PlayerMovementController implementa IActivityCapabilityPermissionReactionTarget ou capability equivalente.
Permission Module descobre esse target.
ActivityCapabilityInventory registra o target.
Permission Stage registra receivers a partir do inventory.
Pipeline publica Allowed/Blocked/Unbound no momento macro.
Componente reage localmente.
```

### 8.2 Reset

Em vez de `ResetAll` ou lógica central por tipo concreto:

```text
Componente implementa IActivityResetEndpoint.
Reset Module descobre endpoints e grupos suportados.
Inventory registra ResetEndpoints.
Reset Stage aplica grupos determinados pela Activity/Policy.
Componente executa reset local.
```

### 8.3 Save / Snapshot / Restore

```text
Componente implementa IActivitySnapshotProvider e/ou IActivitySnapshotRestoreEndpoint.
Snapshot Module descobre providers/receivers.
Inventory registra snapshot participants.
Pipeline decide quando capturar/restaurar.
SaveRuntime persiste somente quando comandado pelo pipeline owner.
```

### 8.4 Camera

```text
PlayerActor/Actor expõe camera endpoint por contrato.
Camera Module descobre endpoints.
Inventory registra endpoints.
CameraBinding Stage consome inventory.
CameraPresentation adapter executa binding.
```

### 8.5 Movement

```text
Movement endpoint é capability local.
Movement module descobre endpoint.
Permission module descobre reaction target.
Pipeline não deve manter conhecimento direto de SetMovementEnabled.
```

---


## 8.6 Escopo Activity / Actor / ActivityObject no Inventory

`ActivityCapabilityInventory` não transforma todo GameObject em Actor e não transforma todo Actor em ActivityObject.

Classificação obrigatória:

| Fonte | Pode gerar | Não implica |
|---|---|---|
| ActivityContent scene | contributors, scan targets e object capabilities | Actor automaticamente |
| RouteScene | route-scoped actors ou contributors quando explicitamente marcados | Activity ownership automática |
| Actor endpoint | ActorCapability / ActorEndpoint runtime reference | ActivityObject automático |
| ActivityObject contributor | Reset/Snapshot/Restore/Release endpoints | Actor identity automática |
| PlayerActor/NonPlayerActor | ActorParticipation + ActorCapabilities | trilhos de lifecycle separados |

O inventory é uma foto determinística das capabilities disponíveis para a entry. Ele não é owner de lifecycle. O pipeline/stage decide quando consumir o inventory; endpoints e runtimes locais executam comportamento local.


## 9. Regras normativas

1. Não criar novo stage específico no pipeline para cada capability concreta.
2. Não criar binding manual quando a relação puder ser inferida por contrato local.
3. Não deixar componente se registrar sozinho em runtime global.
4. Não deixar módulo executar lifecycle global por conta própria.
5. Não usar Gate técnico como substituto de semantic permission.
6. Não usar InputMode como permission local de gameplay.
7. Não criar fallback silencioso para capability obrigatória ausente.
8. Não manter trilhos paralelos após migração.
9. Não preservar shape ruim por compatibilidade.
10. Todo descriptor descoberto deve carregar identity suficiente para rejeitar `foreign/stale events`.
11. O pipeline monta inventory e decide ordem; o componente decide reação local.
12. Assets ficam como referências autorais estáveis, não como substituto para discovery de runtime.

---

## 10. Consequências

### 10.1 Positivas

- Reduz acoplamento do pipeline a componentes concretos.
- Reduz configuração manual no Inspector.
- Resolve conflitos de ID derivados de participant IDs manuais.
- Aproxima módulos e contratos do código que realmente pertence a eles.
- Permite que Reset, Save, Permission, Camera, Movement, Interaction e AI sigam o mesmo padrão.
- Dá um caminho para decompor o `SessionActivityPipeline` sem perder determinismo.

### 10.2 Custos

- Exige revisão de contratos da Base 1.1.
- Reclassifica parte da Base 1.2 como transição arquitetural, não como destino final.
- Exige novo desenho de `ActivityCapabilityInventory`.
- Exige migração cuidadosa para não criar um mega discovery monolítico.
- Pode aproximar o projeto de uma futura Base 2.0 real, mas esta decisão permanece no escopo Base 1.2.

---

## 11. Risco principal

O risco é trocar um pipeline monolítico por um discovery monolítico.

Mitigação:

```text
Discovery core só coordena.
Módulos fornecem scanners pequenos.
Scanners geram descriptors próprios.
Inventory é composto por seções.
Stages consomem seções do inventory.
Adapters executam side-effects.
```

---

## 12. Direção de migração

A partir deste ADR, pausar novas implementações de `ActivityCapabilityPermissionBindings` como caminho final.

Próximos passos recomendados:

1. Registrar este ADR como novo marco normativo.
2. Atualizar README para declarar ADRs anteriores como históricos.
3. Auditar quais stages atuais do `SessionActivityPipeline` seriam consumidores de inventory.
4. Desenhar contratos mínimos:
   - `IActivityCapabilityScanner`;
   - `ActivityCapabilityInventory`;
   - `ActivityCapabilityDescriptor`;
   - seções de inventory por módulo.
5. Só depois migrar Movement para esse modelo.
6. Remover trilhos transitórios após cada migração validada.

---

## 13. Decisão final

A Base 1.1 e a Base 1.2 são tratadas a partir daqui como **baseline histórico funcional**, não como arquitetura final.

A direção dentro da Base 1.2 é:

```text
Base 1.2 — Capability Discovery / Activity Capability Inventory
```

O objetivo não é descartar o que foi validado, mas impedir que a versão de transição se cristalize como arquitetura monolítica.

---

## 14. Estado consolidado ActivityObject (B10)

Status consolidado para `ActivityObject` no trilho Base 1.2:

- `ObjectReset` consome `ResetEndpoint` via `ActivityCapabilityInventory`.
- `SnapshotCapture` consome `SnapshotProvider` via `ActivityCapabilityInventory`.
- `SnapshotRestore` consome `SnapshotRestoreEndpoint` via `ActivityCapabilityInventory` quando ha payload aplicavel.
- `ObjectRelease` consome `ReleaseEndpoint` via `ActivityCapabilityInventory`.
- `ActivityCapabilityDescriptor` permanece descritivo (sem endpoint/provider concreto).
- `RuntimeReference` executavel permanece separada por tipo em arquivos proprios.

Regras de ownership congeladas neste estado:

- `ActivityObjectContributorDiscovery` permanece como fonte de contributors e scan targets.
- `ActivityObjectCapabilityScanTargetAdapter` e `ActivityObjectCapabilityScanner` permanecem no trilho de discovery/inventory.
- `ActivityObjectContributorUnregister` permanece fora desta migracao.
- `SnapshotContractValidation` pode manter rediscovery local apenas para diagnostico/contract view.

Regras de nao regressao:

- Nao introduzir fallback silencioso para trilho antigo nos stages funcionais migrados.
- Nao reintroduzir reflection para resolver capability funcional.
- Nao recolocar endpoint/provider concreto em `ActivityCapabilityDescriptor`.
- Nao migrar neste corte: `Permission`, `Movement`, `Camera`, `SaveRuntime`, `RouteActivitySave`.
