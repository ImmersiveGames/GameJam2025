# ADR-1.2-0007 — Capability Discovery e Activity Capability Inventory

Status: Accepted / Base 1.2  
Área: SessionActivity / Actor Capabilities / Activity Inventory  
Atualização: pós 4B–4D + H4D Hygiene

---

## Contexto

A Base 1.2 exige que capabilities de actors, activity objects e outros participantes sejam descobertas de forma determinística, sem scanners paralelos por tipo específico quando o domínio já possui uma superfície canônica.

O inventory existe para consolidar a descoberta e validação antes de stages de setup, binding, readiness, participation e release.

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

Caminhos migrados:

- `PresentationEndpoint`;
- `AttributeEndpoint`;
- `CameraTarget`;
- `PermissionTarget`.

### 3. Scanners migrados não devem usar PlayerActorTargets como fonte primária

`ActivityCapabilityPlayerActorScanTarget` e `PlayerActorTargets` podem permanecer transitórios enquanto Camera/Permission/Movement ainda mantêm contratos player-centric, mas não são a fonte primária dos scanners já migrados.

### 4. Missing obrigatório não pode ser silencioso

Quando uma capability obrigatória não resolve seu endpoint/identity, o sistema deve gerar falha explícita ou unresolved report/log.

Quando a capability é opcional, a ausência deve gerar skip explícito quando necessário.

### 5. `ActivityCapabilityKind.Custom` não é canal de erro operacional

`Custom` pode existir como metadata/warning passivo, mas não deve ser usado para mascarar ausência obrigatória de capability/surface.

`capability_kind_unsupported` não deve aparecer no caminho nominal validado.

---

## Resultado atual

Checkpoint H4D:

- inventory preview passa sem warnings/errors no caminho nominal;
- `PresentationEndpoint` observado em 3/3/2;
- `AttributeEndpoint` observado em 2/2/1;
- `CameraTarget` observado em 1/1/1;
- `PermissionTarget` observado em 1/1/1;
- unresolved reports igual a 0 no smoke nominal.

---

## Débitos

- Unificar discovery de actors para substituir `NonPlayerActorDiscovery` quando a fonte genérica estiver pronta.
- Reduzir dependências transitórias de `PlayerActorTargets`.
- Revisar validator para separar diagnostics não-capability de `ActivityCapabilityKind.Custom`, se o warning virar ruído real.
