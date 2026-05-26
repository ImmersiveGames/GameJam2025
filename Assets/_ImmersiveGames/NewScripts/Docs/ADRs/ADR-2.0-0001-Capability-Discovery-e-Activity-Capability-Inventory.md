# ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory

**Status:** Accepted / novo marco normativo / atualizado após B11  
**Data:** 2026-05-23  
**Última atualização:** 2026-05-26  
**Substitui como fonte normativa ativa:** ADR-0001 a ADR-0014 e ADR-1.2-0001 a ADR-1.2-0006  
**Escopo:** Base 2.0 / revisão arquitetural após Base 1.1 e Base 1.2 — Actors Convergence

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

## 3. Reclassificação normativa dos ADRs anteriores

Todos os ADRs anteriores a este passam a ser **históricos para consulta**.

Isso inclui:

- ADR-0001 a ADR-0014 da Base 1.1;
- ADR-1.2-0001 a ADR-1.2-0006 da Base 1.2.

Esses documentos continuam valiosos como:

- evidência de checkpoints funcionais;
- intenção arquitetural;
- vocabulário de transição;
- histórico de riscos e decisões aceitas;
- fonte de contratos já testados que podem ser reaproveitados.

Mas eles **não são mais fonte normativa ativa quando conflitarem com este ADR**.

Em conflito, prevalece:

```text
ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory
```

---

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
runtime reference tipada, quando houver execução local
```

Ele pode conter seções como:

```text
PermissionTargets
ResetEndpoints
SnapshotProviders
SnapshotRestoreEndpoints
ReleaseEndpoints
MovementEndpoints
CameraEndpoints
InteractionEndpoints
PresentationEndpoints
AttributeEndpoints
```

O inventory é produzido deterministicamente e consumido pelos stages.

### 5.5 ActivityCapabilityDescriptor e RuntimeReference

A separação normativa é:

```text
ActivityCapabilityDescriptor descreve.
RuntimeReference executa.
Inventory conecta descriptor e RuntimeReference por capabilityId.
```

`ActivityCapabilityDescriptor` não deve carregar endpoint concreto, provider concreto, controller concreto ou referência executável específica.

Referências executáveis ficam em `RuntimeReferences`, tipadas por capability. Exemplos consolidados:

```text
ActivityObjectResetEndpointReference
ActivityObjectSnapshotProviderReference
ActivityObjectSnapshotRestoreEndpointReference
ActivityObjectReleaseEndpointReference
ActivityCapabilityPermissionReceiverReference
```

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
capabilityId
receiverId
```

Não deve ser necessário configurar manualmente `participantId` quando a identidade pode ser derivada de owner + component + capability + entry.

---

## 7. Sobre ActivityCapabilityPermissionBindings

`ActivityCapabilityPermissionBindings` e os experimentos anteriores com `PlayerMovementPermissionParticipant` / `ActivityCapabilityPermissionParticipant` são classificados como **shape substituído** para o caminho funcional de Permission/Movement.

Eles não representam o shape final.

Motivo:

```text
Se o componente já implementa um contrato de capability, um binding manual que repete essa relação vira redundância autoral e nova fonte de erro.
```

A direção aceita é:

```text
Contrato/interface local + scanner de módulo + inventory determinístico + runtime reference tipada.
```

Bindings manuais podem ser reavaliados no futuro apenas como caso excepcional, não como caminho padrão.

---

## 8. Aplicação por domínio

### 8.1 Permission / Gameplay Control

O caminho funcional consolidado após B11 é:

```text
Permission/Movement scanner descobre o target local.
ActivityCapabilityInventory registra PermissionTarget.
ActivityCapabilityPermissionReceiverReference liga capabilityId ao receiver tipado.
ActivityCapabilityPermissionRuntime aplica identity guard, idempotência e dispatch.
Pipeline publica Allowed/Blocked/Unbound no momento macro.
PlayerMovementController reage localmente.
```

Regras específicas:

- `activity.gameplay.control` é a permission canônica para liberação/bloqueio de controle de gameplay da Activity.
- O pipeline decide **quando** publicar `Allowed`, `Blocked` ou `Unbound`.
- O componente local decide **como** reagir ao estado recebido.
- `Allowed` obrigatório não pode ser tratado como sucesso funcional se não houver receiver obrigatório registrado.
- `Blocked` e `Unbound` devem desligar movimento e limpar estado transitório local.
- O runtime deve rejeitar comandos `foreign/stale`.
- O runtime deve preservar idempotência e evitar side-effect duplicado.

### 8.2 Movement

O estado consolidado após B11 é:

```text
MovementBinding prepara ou retém o endpoint de movement.
Permission/Movement inventory fornece receiver/reaction target.
MovementControl não chama SetMovementEnabled diretamente como ownership principal.
MovementControl publica permission no ponto macro validado.
PlayerMovementController aplica a reação local.
```

Regras específicas:

- `MovementBindingCompleted controlEnabled=false` permanece o estado seguro de entrada.
- `MovementControlEnabled` só é válido quando houver evidência de aplicação local da permission ou prova equivalente.
- `MovementBindingRetained` deve alimentar o inventory da nova entry quando a capability permanece válida.
- A fonte de retained movement pode ser o `ActivityPlayerActorRegistry`, desde que use API compatível com retained, como `TryResolveInstanceForControl(...)`, e não apenas active actors da entry atual.
- `Unbound` significa release real da capability, não simples transição entre Activities quando a capability é retida.
- Em transição `activity_01 -> activity_02` sem content, o receiver deve ser reidentificado para a nova `Pipeline Identity`.

### 8.3 Reset

Em vez de `ResetAll` ou lógica central por tipo concreto:

```text
Componente implementa IActivityResetEndpoint.
Reset Module descobre endpoints e grupos suportados.
Inventory registra ResetEndpoints.
Reset Stage aplica grupos determinados pela Activity/Policy.
Componente executa reset local.
```

### 8.4 Save / Snapshot / Restore

```text
Componente implementa IActivitySnapshotProvider e/ou IActivitySnapshotRestoreEndpoint.
Snapshot Module descobre providers/receivers.
Inventory registra snapshot participants.
Pipeline decide quando capturar/restaurar.
SaveRuntime persiste somente quando comandado pelo pipeline owner.
```

### 8.5 Camera

```text
PlayerActor/Actor expõe camera endpoint por contrato.
Camera Module descobre endpoints.
Inventory registra endpoints.
CameraBinding Stage consome inventory.
CameraPresentation adapter executa binding.
```

---

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
13. Capability retida deve ser fonte válida de inventory da nova entry quando ainda pertence ao ciclo ativo.
14. `RuntimeReference` tipada deve ficar em arquivo próprio e não dentro do descriptor.
15. `ActivityCapabilityDescriptor` não deve receber provider, endpoint, controller ou receiver concreto.

---

## 10. Consequências

### 10.1 Positivas

- Reduz acoplamento do pipeline a componentes concretos.
- Reduz configuração manual no Inspector.
- Resolve conflitos de ID derivados de participant IDs manuais.
- Aproxima módulos e contratos do código que realmente pertence a eles.
- Permite que Reset, Save, Permission, Camera, Movement, Interaction e AI sigam o mesmo padrão.
- Dá um caminho para decompor o `SessionActivityPipeline` sem perder determinismo.
- Permite transições no-content com capabilities retidas sem depender de actor rematerializado.

### 10.2 Custos

- Exige revisão de contratos da Base 1.1.
- Reclassifica parte da Base 1.2 como transição arquitetural, não como destino final.
- Exige novo desenho de `ActivityCapabilityInventory`.
- Exige migração cuidadosa para não criar um mega discovery monolítico.
- Pode aproximar o projeto de uma Base 2.0 real.

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
RuntimeReferences tipadas executam apenas a ligação local necessária.
```

---

## 12. Direção de migração

A partir deste ADR, novas capabilities devem entrar pelo modelo:

```text
scanner pequeno por módulo
descriptor puro
runtime reference tipada quando houver execução local
inventory por Pipeline Identity
stage consumidor do inventory
adapter executor de side-effect quando necessário
componente local como dono da reação local
```

Não registrar dívida transitória genérica como norma viva. Registrar neste ADR apenas módulos/capabilities quando estiverem consolidados por implementação e smoke.

Próximos passos por domínio devem seguir o mesmo padrão validado por `ActivityObject` e `Permission/Movement`.

---

## 13. Decisão final

A Base 1.1 e a Base 1.2 são tratadas a partir daqui como **baseline histórico funcional**, não como arquitetura final.

A nova direção é:

```text
Base 2.0 — Capability Discovery / Activity Capability Inventory
```

O objetivo não é descartar o que foi validado, mas impedir que a versão de transição se cristalize como arquitetura monolítica.

---

## 14. Estado consolidado ActivityObject (B10)

Status consolidado para `ActivityObject` no trilho Base 2.0:

- `ObjectReset` consome `ResetEndpoint` via `ActivityCapabilityInventory`.
- `SnapshotCapture` consome `SnapshotProvider` via `ActivityCapabilityInventory`.
- `SnapshotRestore` consome `SnapshotRestoreEndpoint` via `ActivityCapabilityInventory` quando há payload aplicável.
- `ObjectRelease` consome `ReleaseEndpoint` via `ActivityCapabilityInventory`.
- `ActivityCapabilityDescriptor` permanece descritivo, sem endpoint/provider concreto.
- `RuntimeReference` executável permanece separada por tipo em arquivos próprios.

Regras de ownership congeladas neste estado:

- `ActivityObjectContributorDiscovery` permanece como fonte de contributors e scan targets.
- `ActivityObjectCapabilityScanTargetAdapter` e `ActivityObjectCapabilityScanner` permanecem no trilho de discovery/inventory.
- `ActivityObjectContributorUnregister` permanece fora desta migração.
- `SnapshotContractValidation` pode manter rediscovery local apenas para diagnóstico/contract view.

Regras de não regressão:

- Não introduzir fallback silencioso para trilho antigo nos stages funcionais migrados.
- Não reintroduzir reflection para resolver capability funcional.
- Não recolocar endpoint/provider concreto em `ActivityCapabilityDescriptor`.
- Não usar `ActivityObjectContributorDiscovery` como owner funcional de reset/capture/restore/release.

Evidência de smoke consolidada:

```text
ActivityCapabilityInventoryValidationPassed
ActivityCapabilityInventoryPreviewObserved
ActivityObjectReset Passed
ActivityObjectSnapshotCapture Passed
ActivityObjectSnapshotRestore Skipped correto sem payload ou Passed quando aplicável
ActivityObjectRelease Passed
ActivityObjectContributorUnregister Passed
RestartCurrentActivity Passed
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

---

## 15. Estado consolidado Permission/Movement (B11)

Status consolidado para Permission/Movement no trilho Base 2.0:

- `PermissionTarget` de `activity.gameplay.control` é descoberto pelo scanner de Permission/Movement.
- `ActivityCapabilityPermissionReceiverReference` é a runtime reference tipada que conecta descriptor e receiver local.
- `ActivityCapabilityPermissionRuntime` permanece como dispatcher com identity guard, idempotência e rejeição de `foreign/stale events`.
- `PlayerMovementController` permanece dono da reação local de movement.
- `MovementBinding` prepara ou retém o endpoint de movement.
- `MovementControl` publica semantic permission nos pontos macro do lifecycle.
- O caminho funcional não depende mais de `ActivityCapabilityPermissionBindings` como fonte de verdade.
- O caminho funcional não depende de register/unregister hardcoded em adapters como owner paralelo.

Regras de ownership congeladas neste estado:

- Pipeline decide quando a permission muda.
- Permission runtime valida identity e despacha para receivers resolvidos por inventory.
- Receiver local aplica reação no componente local.
- `PlayerMovementController` aplica `SetMovementEnabled` e `ClearMovementState`.
- Actor/player retido entre Activities continua sendo fonte válida para inventory da nova entry.
- `active player actors` da entry atual não é a única fonte válida para Permission/Movement.

Regras funcionais congeladas:

- Após `MovementBinding`, o estado seguro é `Blocked`.
- Após entrada em `ActivityRunning`, o estado esperado é `Allowed`.
- Em deactivation, complete, restart e route-exit, o estado esperado é `Blocked`.
- Em release real da capability, o estado esperado é `Unbound`.
- `MovementBindingRetained` deve reidentificar receiver para a nova entry.
- `MovementControlEnabled` não deve mascarar ausência de aplicação local.

Evidência de smoke consolidada:

```text
activity_02 entrySequence=3
ActivityCapabilityPermissionReceiverRegistered
MovementBindingRetained
MovementBindingCompleted status='RetainedExistingBinding'
CameraBindingSkippedNoRequiredCamera
ActivityCapabilityPermissionPublished state='Allowed'
ActivityCapabilityPermissionApplied state='Allowed'
ActivityCapabilityPermissionReceiverNotified state='Allowed'
PlayerMovementPermissionApplied state='Allowed'
MovementControlEnabled controlEnabled='true'
Activity01ToActivity02 Passed
RouteExitBackToMenu Passed
```

Regras de não regressão:

- Não reintroduzir `ActivityCapabilityPermissionBindings` como caminho funcional.
- Não reintroduzir register/unregister hardcoded em `MovementBindingAdapter`, `PlayerMovementControlAdapter` ou `PlayerActorParticipationAdapter` como owner paralelo.
- Não tratar `Allowed` obrigatório como sucesso se não houver receiver obrigatório.
- Não depender exclusivamente de `ResolveActiveInstanceOrFail(...)` para capabilities retidas.
- Não limpar retained movement em transição entre Activities quando a policy mantém o actor/capability até `RouteExit`.
- Não usar Gate técnico ou InputMode como substituto de semantic permission.
- Não criar fallback silencioso para receiver ausente.

---

## 16. Critério para próximos módulos

Um módulo só deve ser registrado como consolidado neste ADR quando cumprir:

```text
1. Scanner ou fonte de discovery modular existente.
2. ActivityCapabilityDescriptor puro.
3. RuntimeReference tipada, se houver execução local.
4. Consumo funcional via ActivityCapabilityInventory.
5. Sem fallback silencioso para trilho antigo.
6. Sem dois owners ativos.
7. Smoke validando caminho feliz e caminho skip/no-content relevante.
```
