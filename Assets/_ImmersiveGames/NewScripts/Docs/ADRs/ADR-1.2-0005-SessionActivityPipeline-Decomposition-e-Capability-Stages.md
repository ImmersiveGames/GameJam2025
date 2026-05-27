# ADR-1.2-0005 — SessionActivityPipeline Decomposition e Capability Stages

Status: Accepted / Base 1.2  
Área: SessionActivityPipeline / Pipeline Stages / Actor Capabilities  
Atualização: 4D + H4D Hygiene

---

## Contexto

Durante a Base 1.2, houve risco de o `SessionActivityPipeline` virar owner de comportamento local de Actor. A intenção correta é outra:

- pipeline decide lifecycle, ordem, readiness, setup/release e handoff;
- endpoints/capabilities executam efeitos locais;
- ações locais de gameplay não devem subir para o pipeline quando podem ser resolvidas localmente.

---

## Decisão

### 1. Pipeline decide ciclo, não comportamento local

O `SessionActivityPipeline` é owner de:

- entry;
- setup;
- reset;
- placement;
- camera/movement/input binding;
- participation;
- release;
- snapshot;
- route-exit/restart;
- readiness para reveal.

O pipeline não deve virar dispatcher de qualquer ação local de gameplay.

### 2. Capability stages usam inventory

Stages que dependem de objetos/actors devem consumir `ActivityCapabilityInventory` sempre que o caminho já foi migrado.

Caminhos migrados:

- ActorPresentation setup/release;
- ActorAttributes setup/release;
- CameraTarget scanner;
- PermissionTarget scanner;
- ActorParticipation enter/exit.

### 3. Participation é genérico no caminho nominal

`NonPlayerActorParticipationStage` foi removido.

O caminho nominal é `ActorParticipation`, com observabilidade per-actor e aggregates.

### 4. Completed observável é obrigatório

Stages relevantes devem emitir completion observável:

- `ActorPresentationSetupCompleted`;
- `ActorPresentationReleaseCompleted`;
- `ActorAttributeSetupCompleted`;
- `ActorAttributeReleaseCompleted`;
- `ActorParticipationEnterCompleted`;
- `ActorParticipationExitCompleted`.

### 5. Skip explícito é parte do contrato

Ausência opcional ou transição ainda não suportada deve aparecer como skip explícito, não fallback silencioso.

Exemplo aceito atualmente:

- `transitional_non_registry_actor` para PlayerActor fora da registry formal de ActorParticipation.

---

## Proibições

Não criar:

- stage separado por `PlayerActor`, `NonPlayerActor`, `EnemyActor` etc. quando o domínio já é Actor;
- switch funcional por `ActorKind`;
- fallback por string/path/scene;
- compat paralelo;
- novo core genérico fora do escopo Base 1.2.

---

## Débitos

- Migrar PlayerActor para policy/registry genérica de ActorParticipation.
- Reduzir registries transitórios.
- Revisar tamanho do `SessionActivityPipeline` em fase própria sem mudar ownership.
