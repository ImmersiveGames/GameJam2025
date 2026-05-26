# ADRs — Base 2.0

Este diretório mantém a fonte normativa viva da arquitetura atual do projeto.

A norma ativa atual é:

1. **ADR-2.0-0001** — Capability Discovery e Activity Capability Inventory

ADRs da Base 1.1 e Base 1.2 permanecem como histórico funcional e evidência de checkpoints, mas não são mais fonte normativa ativa quando conflitarem com o ADR-2.0-0001.

---

## Fonte normativa atual

### Base 2.0

- **ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory**
  - Define `ActivityCapabilityInventory` como direção normativa.
  - Reclassifica Base 1.1 e Base 1.2 como histórico funcional.
  - Congela a regra: descriptor descreve, runtime reference executa, inventory conecta por `capabilityId`.
  - Congela `ActivityObject` consolidado via inventory.
  - Congela `Permission/Movement` consolidado via inventory.

---

## Histórico funcional preservado

Os ADRs abaixo continuam úteis como evidência e intenção funcional, mas são históricos:

### Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos

1. **ADR-0001** — Base 1.1: Pipeline Convergence, Identidade Explícita e Isolamento contra Foreign Events
2. **ADR-0002** — Run Pipeline Canonical e Deactivation/Continuity
3. **ADR-0003** — Session Operational Pipeline e Session Transition Envelope
4. **ADR-0004** — Session Activity Pipeline
5. **ADR-0005** — Modules Produzem Facts/Commands, Adapters Executam Side-Effects
6. **ADR-0006** — Route, Scene Composition, Fade, Loading e Audio Adapters
7. **ADR-0007** — Gates, InputModes e Simulation Executors
8. **ADR-0008** — SaveSystem Canonical
9. **ADR-0009** — Session Player Slots e Operational Input Runtime
10. **ADR-0010** — Player Preparation Flow, Player Slots e Unity PlayerInput
11. **ADR-0011** — Runtime Configuration Registry e Config Sets
12. **ADR-0012** — Operational Camera Runtime e Future Activity Camera Binding
13. **ADR-0013** — Camera Presentation Runtime e Activity Camera Director
14. **ADR-0014** — ActivityContent, WindowTemplateLibrary e ActivityEntryPipeline

### Base 1.2 — Actors Convergence / Convergência de Atores

1. **ADR-1.2-0001** — Actor Presentation System e Migração do Legacy Skin System
2. **ADR-1.2-0002** — NonPlayerActor Scene-Authored e ActorPresentation MVP
3. **ADR-1.2-0003** — Typed Identity e Authoring References
4. **ADR-1.2-0004** — ActorAttributes como ActorCapability
5. **ADR-1.2-0005** — SessionActivityPipeline Decomposition e Capability Stages

---

## Precedência normativa

Em conflito, prevalece:

```text
ADR-2.0-0001 — Capability Discovery e Activity Capability Inventory
```

Regras atuais:

- Base 1.1 e Base 1.2 são baseline histórico funcional.
- `SessionOperationalPipeline` e `SessionActivityPipeline` continuam importantes como evidência de lifecycle validado, mas não devem crescer como índice monolítico de capabilities.
- O pipeline decide ordem macro, lifecycle, identity, policies e handoffs.
- Módulos fornecem scanners/validators/adapters pequenos por domínio.
- Componentes locais declaram capabilities por contrato/interface.
- `ActivityCapabilityInventory` registra capabilities por `Pipeline Identity`.
- `ActivityCapabilityDescriptor` descreve.
- `RuntimeReference` tipada executa.
- Componentes reagem localmente.
- Não criar fallback silencioso.
- Não manter dois owners ativos para a mesma capability.
- `foreign/stale events` não podem alterar pipeline ativo.

---

## Modelo conceitual ativo

```text
Módulo declara o que sabe consumir.
Componente declara o que oferece.
Pipeline faz a triagem determinística.
Inventory registra o resultado por identity.
Stages consomem o inventory.
Adapters executam side-effects comandados.
Componentes reagem localmente.
```

---

## Estado consolidado dos módulos migrados

### ActivityObject — consolidado no B10

`ActivityObject` usa `ActivityCapabilityInventory` como fonte funcional para:

- `ResetEndpoint`;
- `SnapshotProvider`;
- `SnapshotRestoreEndpoint`;
- `ReleaseEndpoint`.

Estado validado:

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

Regras congeladas:

- `ActivityCapabilityDescriptor` não carrega endpoint/provider concreto.
- `RuntimeReferences` executáveis ficam em arquivos próprios.
- `ActivityObjectContributorDiscovery` é fonte de contributors e scan targets, não owner funcional de reset/capture/restore/release.
- `ActivityObjectContributorUnregister` permanece separado.
- Rediscovery local só pode permanecer para diagnóstico/contract view, não como fonte funcional dos stages migrados.

### Permission/Movement — consolidado no B11

`Permission/Movement` usa `ActivityCapabilityInventory` como fonte funcional para `PermissionTarget` de `activity.gameplay.control`.

Estado validado:

```text
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

Regras congeladas:

- `activity.gameplay.control` é a permission canônica de controle local de gameplay da Activity.
- Pipeline decide quando publicar `Allowed`, `Blocked` e `Unbound`.
- `ActivityCapabilityPermissionRuntime` valida identity, rejeita `foreign/stale events`, preserva idempotência e despacha para receivers resolvidos por inventory.
- `PlayerMovementController` aplica a reação local.
- `MovementBindingRetained` deve alimentar o inventory da nova entry quando a capability permanece válida.
- `active player actors` da entry atual não é a única fonte válida para Permission/Movement.
- Não reintroduzir `ActivityCapabilityPermissionBindings` como caminho funcional.
- Não reintroduzir register/unregister hardcoded em adapters como owner paralelo.
- Não usar Gate técnico ou InputMode como substituto de semantic permission.

---

## Critério para registrar novos módulos como consolidados

Um módulo só deve ser registrado neste README quando cumprir:

```text
1. Scanner ou fonte modular de discovery existente.
2. ActivityCapabilityDescriptor puro.
3. RuntimeReference tipada, se houver execução local.
4. Consumo funcional via ActivityCapabilityInventory.
5. Sem fallback silencioso para trilho antigo.
6. Sem dois owners ativos.
7. Smoke validando caminho feliz e skip/no-content relevante.
```

Até cumprir esses critérios, o módulo não deve ser registrado como consolidado na norma viva.

---

## Gatilhos de reabertura

Reabrir ou expandir a documentação normativa apenas quando houver implementação + smoke de um módulo completo.

Candidatos futuros naturais:

- Camera via inventory;
- ActorPresentation via inventory;
- ActorAttributes via inventory;
- NonPlayerActor lifecycle/inventory;
- SaveRuntime / RouteActivitySave depois que snapshot/inventory estiverem estáveis.

---

## Regra para próximos trabalhos

Antes de criar componente/config/adapter novo:

1. localizar o que já existe;
2. confirmar ownership atual;
3. classificar se o caso deve virar scanner, descriptor, runtime reference, adapter ou reação local;
4. implementar em cortes pequenos;
5. validar por smoke/log;
6. só então atualizar ADR/README como módulo consolidado.

Prompts para Codex devem continuar curtos, começar por auditoria quando houver risco de reinventar infraestrutura e não devem pedir Unity build, Play Mode, batchmode ou validação funcional automatizada.
