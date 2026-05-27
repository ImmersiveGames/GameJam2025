# ADR-1.2-0008 — Actor Model, Actor Typing e ActorCapabilitySurface

- **Estado:** Aceito / intenção normativa congelada para Base 1.2
- **Base:** Base 1.2 — Actors Convergence / Convergência de Atores
- **Fundação normativa:** Base 1.1 — Pipeline Convergence / Convergência para Pipelines Determinísticos
- **Relacionado:**
  - ADR-1.2-0001 — Actor Presentation System e Migração do Legacy Skin System
  - ADR-1.2-0002 — NonPlayerActor Scene-Authored e ActorPresentation MVP
  - ADR-1.2-0003 — Typed Identity e Authoring References
  - ADR-1.2-0004 — ActorAttributes como ActorCapability
  - ADR-1.2-0005 — SessionActivityPipeline Decomposition e Capability Stages
  - ADR-1.2-0006 — ActivityCapabilityPermission e Reação Local de Capabilities
  - ADR-1.2-0007 — Capability Discovery e Activity Capability Inventory
- **Escopo:** modelo conceitual central de `Actor`, tipagem concreta de atores, superfície local de capabilities/endpoints e fronteira entre pipeline, actor, objeto e gameplay local.
- **Fora do escopo:** alterar código neste corte, reabrir Base 1.1, criar Base 2.0, reorganizar fisicamente o projeto, criar framework genérico de ECS/core reutilizável, migrar `ActorAttributes`, `Movement`, `Interaction`, AI, combat ou `ActivityObject` neste ADR.

---

## 1. Contexto

A Base 1.2 iniciou a **Actors Convergence / Convergência de Atores** a partir da fundação já concluída da Base 1.1.

Durante as primeiras migrações, `ActorPresentation`, `NonPlayerActor`, `ActorAttributes`, `ActivityCapabilityInventory`, `ActivityCapabilityPermission`, `PlayerActor` readiness/input/movement/camera e `ActivityObject` foram estabilizados em cortes incrementais. Esses cortes reduziram ownership incorreto e validaram que capabilities podem ser preparadas, liberadas e observadas sem transformar o `SessionActivityPipeline` em executor direto de tudo.

Ainda assim, surgiu uma ambiguidade de intenção: `PlayerActor`, `NonPlayerActor`, NPCs, enemies, objects e interactives estavam sendo tratados parcialmente como categorias por metadata (`ActorKind`, `ActorRole`, `ActorScope`) e parcialmente como trilhos funcionais separados.

Essa direção é perigosa se `ActorKind` virar contrato funcional central, porque tende a gerar:

```text
if ActorKind == Player
if ActorKind == NonPlayer
switch ActorKind
```

espalhados por pipeline, scanners, setup/release, movement, attributes, presentation e participation.

Este ADR congela a intenção correta: atores são tipados por hierarquia/concreto quando a variação representa semântica real, e capabilities/endpoints expõem o que cada ator pode fazer. Metadata pode existir, mas não deve substituir polimorfismo nem virar switch de lifecycle.

---

## 2. Decisão central

A Base 1.2 adota o seguinte modelo normativo:

```text
Actor é a raiz abstrata de entidades de gameplay.
PlayerActor, NonPlayerActor, NpcActor, EnemyActor, ObjectActor e InteractiveActor são tipos concretos ou especializações de Actor.
ActorCapabilities e ActorEndpoints definem o que cada Actor pode fazer.
SessionActivityPipeline / ActivityEntryPipeline decide lifecycle, timing, readiness, reset, snapshot, release e guardas foreign/stale.
Actor e seus endpoints executam comportamento local e reação local, mas não decidem lifecycle global.
```

`ActorKind`, `ActorRole` e metadata similares podem existir para:

```text
logs
observabilidade
authoring
migração
filtros de UI
policy declarativa
```

Mas não devem ser o mecanismo principal para decidir comportamento funcional por `if/switch` centralizado.

A regra final é:

```text
Tipo concreto informa o que o Actor é.
Capabilities/endpoints informam o que o Actor pode fazer.
Pipeline consome contratos de Actor/Capability/Endpoint.
```

---

## 3. Definição de Actor

Um `Actor` é qualquer entidade de gameplay com:

```text
identidade própria
participation explícita ou potencial
capacidade de expor ActorCapabilities/ActorEndpoints
possibilidade de entrar em setup/readiness/release/snapshot/reset conforme policy
```

Exemplos típicos:

```text
PlayerActor
NpcActor
EnemyActor
ObjectActor
InteractiveActor
CompanionActor
ScriptedActor
```

Um objeto passivo de cenário não precisa ser `Actor`.

Um `GameObject` que possui interação, atributos, presentation, movement, AI, participation, snapshot, reset ou comportamento de gameplay tende a ser `Actor` ou expor uma capability explícita que o torne parte do fluxo.

---

## 4. Actor vs ActivityObject

`Actor` e `ActivityObject` continuam conceitos diferentes.

```text
Actor = entidade de gameplay com identidade, participation e capabilities.
ActivityObject = contributor/objeto da Activity com endpoints locais de reset/snapshot/restore/release.
```

Um `GameObject` pode expor ambos explicitamente:

```text
InteractiveObjectActor + ActivityObjectContributor
```

Mas isso não é automático.

Regras:

```text
ActivityObject não vira Actor por padrão.
Actor não vira ActivityObject por padrão.
Scene discovery não transforma todo GameObject em Actor.
Actor discovery não transforma todo Actor em ActivityObject.
```

---

## 5. Tipagem de atores

A variação de ator deve ser expressa por tipo concreto sempre que a variação representar semântica real.

Direção conceitual:

```text
abstract Actor
  PlayerActor
  NonPlayerActor
    NpcActor
    EnemyActor
  ObjectActor
    InteractiveActor
    MovableObjectActor
```

A hierarquia final pode ser ajustada por implementação, mas a intenção é evitar que `ActorKind` vire substituto para polimorfismo.

### 5.1 Uso permitido de ActorKind/ActorRole

Permitido:

```text
logs
metadata
authoring
filtros de debug
classificação de policy declarativa
migração transitória
```

Não permitido:

```text
switch central de lifecycle
branch principal de setup/release
substituto de tipo concreto
substituto de capability discovery
condição para decidir comportamento local moment-to-moment
```

Quando o código precisa decidir se algo pode se mover, apresentar, receber dano ou reagir a input, deve procurar capability/endpoint, não categoria textual.

---

## 6. PlayerActor

`PlayerActor` é especial por input, player slot, possession e câmera preferencial, não por possuir lifecycle separado.

Regras:

```text
PlayerActor é Actor.
PlayerActor não cria pipeline próprio.
PlayerActor não deve ter trilho global separado de setup/release quando a capability é comum a todo Actor.
Player input é uma capability/fonte de intenção específica do PlayerActor.
```

Exemplo:

```text
PlayerInputReader -> MovementIntent
ActorMovementController consome MovementIntent
```

O mesmo `ActorMovementController` ou contrato equivalente pode ser usado por outros atores se a execução do movimento for comum.

---

## 7. ActorCapabilitySurface

Todo `Actor` deve poder expor uma superfície local de capabilities/endpoints.

Nome conceitual recomendado:

```text
ActorCapabilitySurface
```

A `ActorCapabilitySurface` organiza endpoints locais, mas não decide lifecycle.

Ela pode:

```text
expor endpoints locais
validar configuração local
permitir scan determinístico
resolver endpoint por contrato/tipo
fornecer referências locais para inventory
```

Ela não pode:

```text
auto-bindar input/movement/camera/presentation
auto-resetar por lifecycle
auto-release por lifecycle
auto-salvar
decidir participation
decidir Activity entry
ignorar Pipeline Identity
alterar pipeline ativo
```

---

## 8. Bindings e componentes transitórios

A Base 1.2 evita proliferar componentes Unity de binding para cada relação transitória.

Regra:

```text
Capability/Endpoint estável pode ser componente do Actor.
Binding runtime transitório deve preferir estado de pipeline/inventory/adapter/runtime local, não componente Unity permanente por feature.
```

Componentes aceitáveis no Actor:

```text
ActorRoot
ActorCapabilitySurface
ActorPresentationEndpoint
ActorMovementController
ActorCameraEndpoint
ActorAttributeEndpoint
ActorInteractionEndpoint
PlayerInputReader
AIIntentProvider
```

Componentes suspeitos como shape final:

```text
MovementBindingComponent
CameraBindingComponent
PresentationBindingComponent
InputBindingRuntimeComponent
PipelineBindingStateComponent
```

Eles só devem existir se representarem capability local real, não apenas memória transitória de um stage.

---

## 9. Movement e fontes de intenção

Movement deve preferir contrato genérico de movimento + fontes de intenção.

Modelo recomendado:

```text
IMovementIntentProvider
  PlayerInputMovementIntentProvider
  AIMovementIntentProvider
  ScriptedMovementIntentProvider

ActorMovementController
  consome MovementIntent
  aplica movimento local
```

O pipeline decide quando a Activity permite ou bloqueia a capability de movement.

O endpoint/controller local decide como aplicar ou reagir localmente.

`PlayerInputReader` é uma fonte de intenção do `PlayerActor`; AI, script, physics ou behavior são fontes de intenção de outros atores.

---

## 10. ActorCapabilities e endpoints

Capabilities devem ser modeladas como contratos locais e descobertas pelo inventory.

Exemplos:

```text
ActorPresentationEndpoint
ActorAttributeEndpoint
ActorMovementEndpoint
ActorCameraTargetEndpoint
ActorInteractionEndpoint
ActorAIEndpoint
ActorInputIntentEndpoint
ActorSnapshotProvider
ActorResetEndpoint
ActorReleaseEndpoint
```

Quando a variação muda apenas dados/configuração, usar profile/config.

Quando a variação muda contrato ou comportamento, usar tipo concreto ou endpoint específico.

---

## 11. Relação com pipelines

`SessionOperationalPipeline`:

```text
orquestra rota, transição operacional, setup operacional, save/load e handoff.
não materializa PlayerActor jogável final.
não executa lifecycle local de actor.
```

`SessionActivityPipeline` / `ActivityEntryPipeline`:

```text
decide entry sequence, lifecycle local de Activity, setup order, readiness, reset, snapshot, release, route-exit e guardas foreign/stale.
```

`Actor` / `ActorCapability` / `ActorEndpoint`:

```text
executa comportamento local
mantém estado local
reage localmente a permissões/comandos válidos
não decide lifecycle global
```

`Adapters`:

```text
executam side-effects Unity comandados por pipeline/stage
```

---

## 12. ActivityCapabilityInventory

O `ActivityCapabilityInventory` deve consumir contratos, não categorias.

Direção final:

```text
ActorCapabilitySurface / Actor endpoints / authorized sources
-> scanners pequenos por módulo
-> ActivityCapabilityInventory
-> runtime references tipadas
-> stages consomem seções do inventory
```

`ActorScanTarget`, `ActorInventoryFeed`, `ActorInstanceRecord` e similares podem existir como ponte de convergência, desde que não sejam owners de lifecycle.

O inventory é uma foto determinística da entry. Ele não é owner de participation, release, reset, save ou readiness.

---

## 13. Regras normativas

1. `Actor` é raiz abstrata para entidades de gameplay.
2. `PlayerActor`, `NonPlayerActor`, `NpcActor`, `EnemyActor`, `ObjectActor` e `InteractiveActor` são especializações concretas ou direção de especialização.
3. `ActorKind`/`ActorRole` são metadata, não switch funcional central.
4. Capabilities/endpoints definem comportamento e disponibilidade funcional.
5. Pipeline não deve decidir behavior por categoria textual de actor.
6. Pipeline decide timing, lifecycle, readiness, reset, snapshot, release e foreign/stale guard.
7. Actor não decide lifecycle global.
8. Actor pode decidir comportamento local/moment-to-moment por endpoints/capabilities.
9. `ActorCapabilitySurface` expõe endpoints locais, mas não executa lifecycle por conta própria.
10. Runtime bindings transitórios não devem proliferar como componentes Unity separados por feature.
11. Player é especial por input/possession/player slot, não por lifecycle separado.
12. Movement deve preferir fonte de intenção + controller/endpoint genérico quando o comportamento permitir.
13. `ActivityObject` e `Actor` são conceitos separados; um GameObject pode expor ambos explicitamente.
14. Ausência obrigatória de capability é erro/fail-fast; ausência opcional é skip explícito.
15. Não criar fallback silencioso.
16. Não criar pipelines separados por Player/NPC/Enemy/Object.
17. Não reabrir Base 1.1.

---

## 14. Relação com ADRs existentes

### ADR-1.2-0001

`ActorPresentation` permanece capability de `Actor`. A partir deste ADR, o setup/release de presentation deve operar sobre `Actor` + `PresentationEndpoint`, não sobre trilhos separados de `PlayerActor`/`NonPlayerActor`.

### ADR-1.2-0002

`NonPlayerActor` é MVP concreto/transitório de especialização de `Actor`, não modelo geral de lifecycle separado.

### ADR-1.2-0003

Typed identity permanece obrigatória. `ActorKind`/`ActorRole` não substituem identidade tipada e não devem ser parseados/interpretados como lifecycle.

### ADR-1.2-0004

`ActorAttributes` deve convergir para `ActorAttributeEndpoint` genérico de `Actor`, não `NonPlayerActorAttribute*` como shape final.

### ADR-1.2-0005

A decomposição do `SessionActivityPipeline` deve evitar trilhos paralelos de actor type. Stages consomem capabilities e endpoints.

### ADR-1.2-0006

`ActivityCapabilityPermission` continua sendo permissão semântica macro; a reação é local no endpoint/capability.

### ADR-1.2-0007

`ActivityCapabilityInventory` deve preferir `ActorCapabilitySurface` e endpoints descobertos por contrato, não por switch de `ActorKind`.

---

## 15. Consequências

### Positivas

- Reduz `if/switch` por categoria de actor.
- Evita trilhos paralelos Player/NPC/Enemy/Object.
- Dá base para `ActorAttributes`, `Movement`, `Interaction`, AI e combat sem repetir erros de ownership.
- Mantém pipelines como owners de lifecycle e não de comportamento local.
- Permite que objetos de gameplay sejam `ObjectActor`/`InteractiveActor` quando fizer sentido.

### Custos

- Exige revisão de nomenclatura e contratos transitórios que ainda usam `NonPlayerActor*`.
- Exige cuidado para não confundir tipo concreto com lifecycle separado.
- Pode exigir migração gradual de registries atuais para `ActorInstance`/`ActorCapabilitySurface`.
- Não resolve sozinho a organização física do `SessionActivityPipeline.cs`.

---

## 16. Migração recomendada

1. Registrar este ADR como fonte normativa do modelo central de Actor.
2. Atualizar ADRs Base 1.2 relacionados para referenciar este ADR.
3. Antes de migrar `ActorAttributes`, remover a intenção de `NonPlayerActorAttribute*` como shape final.
4. Introduzir ou adaptar `ActorCapabilitySurface` quando houver necessidade concreta.
5. Convergir capabilities existentes para scanners por endpoint/capability, não por actor category.
6. Só depois avaliar organização física dos stages.

---

## 17. Checkpoint aceito

```text
Actor Model / Actor Typing / ActorCapabilitySurface — INTENÇÃO NORMATIVA CONGELADA
```

Este checkpoint não altera código.

Ele congela a direção para próximas migrações de Actors Convergence.
