# ADR-1.2-0007 — Capability Discovery e Activity Capability Inventory

Status: Accepted / Base 1.2  
Área: SessionActivity / Actor Capabilities / Activity Inventory  
Atualização: pós 4B–4D + H4D Hygiene + ActorReset-1B

> Historical Base 1.2 reference. For the active Base 2.0 boundary, see `ADR-2.0-0002-SessionActivity-Ownership-Decomposition.md` and `ENTRY-BOUNDARY-DOC-0` inside it. Any inventory-backed setup/binding wording below is historical and does not authorize the Base 2.0 shape.

---

## Contexto

A Base 1.2 exige que capabilities de actors, activity objects e outros participantes sejam descobertas de forma determinística, sem scanners paralelos por tipo específico quando o domínio já possui uma superfície canônica.

O inventory existe para consolidar a descoberta e validação antes de stages de setup, binding, readiness, participation e release.

Durante a estabilização de reset, foi necessário distinguir duas coisas:

- preview canônico persistido no state para a entry;
- snapshot local transitório usado por `ActivityObjectReset` quando o reset precisa rodar antes do preview observado por stages posteriores.

---

## Decisão

### 1. ActivityCapabilityInventory é o inventário canônico de capabilities da Activity

O inventory consolida descriptors e runtime references para a entry corrente.

Ele pode receber fontes diferentes, mas deve expor um shape homogêneo para stages:

- descriptors puros;
- runtime references tipadas;
- reports/unresolved explícitos;
- validation result determinístico.

### 2. Actor capabilities entram via ActorScanTarget + ActorCapabilitySurface

Para capabilities de Actor, scanners devem consumir:

- `ActorScanTarget`;
- `ActorCapabilitySurface`;
- endpoint local da capability.

Caminhos migrados historicamente:

- `PresentationEndpoint`;
- `AttributeEndpoint`;
- `CameraTarget`;
- `PermissionTarget`.

Esses caminhos são históricos para Base 1.2. Em Base 2.0, capability local não implica inventory, e scanner não decide requiredness por `ActorSourceKind`, `Player`, `Participation`, `Scope` ou simples presença de componente.

### 3. Scanners migrados não devem usar PlayerActorTargets como fonte primária

`ActivityCapabilityPlayerActorScanTarget` e `PlayerActorTargets` podem permanecer transitórios enquanto Camera/Permission/Movement ainda mantêm contratos player-centric, mas não são a fonte primária dos scanners já migrados.

### 4. Missing obrigatório não pode ser silencioso

Quando uma capability obrigatória não resolve seu endpoint/identity, o sistema deve gerar falha explícita ou unresolved report/log.

Quando a capability é opcional, a ausência deve gerar skip explícito quando necessário.

Esse requisito continua válido como política de stage, mas não autoriza scanner a inferir requiredness para o shape Base 2.0.

### 5. `ActivityCapabilityKind.Custom` não é canal de erro operacional

`Custom` pode existir como metadata/warning passivo, mas não deve ser usado para mascarar ausência obrigatória de capability/surface.

`capability_kind_unsupported` não deve aparecer no caminho nominal validado.

### 6. ObjectReset pode usar snapshot local transitório

`ActivityObjectReset` pode construir um snapshot local de inventory para a entry corrente usando o mesmo coordinator/scanner.

Esse snapshot local:

- deve ser validado com a mesma identity de entry;
- deve servir apenas ao contexto do reset;
- não deve sobrescrever `_state.CurrentActivityCapabilityInventoryPreview`;
- não deve virar preview canônico;
- deve produzir reasons explícitos como `NoCommands`, `NoApplicableGroups`, `SkippedOptional` ou `InventoryInvalidOrStale`.

O preview canônico completo continua pertencendo ao `ActivityCapabilityInventoryPreviewStage`.

### 7. ActorReset não está totalmente modelado como inventory genérico final

O checkpoint ActorReset-1B congelou contrato neutro de reset de Actor:

- `ActorResetCommand`;
- `IActorResetAdapter`;
- `IActorResetEndpoint`.

Mas o resolver concreto atual ainda é transitório para PlayerActor.

Portanto, não congelar ainda como verdade final que todo `ActorReset` nasce do `ActivityCapabilityInventory`. A direção final é resolver por Actor/ActorInstance de forma genérica.

---

## Resultado atual

Checkpoint H4D + ActorReset-1B:

- inventory preview passa sem warnings/errors no caminho nominal;
- `PresentationEndpoint` observado em 3/3/2;
- `AttributeEndpoint` observado em 2/2/1;
- `CameraTarget` observado em 1/1/1;
- `PermissionTarget` observado em 1/1/1;
- unresolved reports igual a 0 no smoke nominal;
- `ActivityObjectReset` em `activity_01` classificado como `PassedApplied`;
- `ActivityObjectReset` em no-content classificado como `PassedNoCommands`;
- `ActorResetQaApplied` observado sem quebrar preview/inventory;
- preview canônico não é substituído pelo snapshot local de reset.

---

## Débitos

- Unificar discovery de actors para substituir `NonPlayerActorDiscovery` quando a fonte genérica estiver pronta.
- Reduzir dependências transitórias de `PlayerActorTargets`.
- Revisar validator para separar diagnostics não-capability de `ActivityCapabilityKind.Custom`, se o warning virar ruído real.
- Generalizar resolução de `ActorReset` por `ActorInstanceId`/inventory quando a registry genérica estiver pronta.
