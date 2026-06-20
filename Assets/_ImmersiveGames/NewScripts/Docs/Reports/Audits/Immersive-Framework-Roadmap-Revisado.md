# Immersive Framework — Roadmap Revisado

Status: versão revisada a partir do roadmap externo e das auditorias consolidadas  
Modo: planejamento arquitetural / sem patch / sem implementação  
Uso pretendido: orientar ADRs, cortes técnicos e validação incremental do `com.immersive.framework`.

---

## 0. Decisão central

```text
O framework deve nascer como lifecycle/content/contribution framework.
Não como camera framework, audio framework, actor framework ou clone do Base 2.0.
```

A regra prática é:

```text
Primeiro estabilizar owners, identidade, conteúdo e contribuição.
Depois materialização, surface e release.
Só então consumidores como Input, Save, Pause, Camera, Audio, Actor, Pooling e Projectile.
```

---

## 1. Avaliação do roadmap original

O roadmap original está correto na direção macro:

- começa por reconciliação do baseline;
- preserva o que o package já acertou;
- reconhece `CameraFlow`, `RouteContentRuntime`, `ContentFlow`, `RouteContentProfileAsset`, `FrameworkQaCanvas` e `ValidationMode` como decisões obrigatórias;
- mantém camera/audio/actor/save fora do core inicial;
- traz `LocalContributionSet`, `RuntimeRootRegistry`, `Surface` e consumers em fases posteriores.

Mas ele ainda pode gerar retrabalho por quatro motivos.

### 1.1. Fase 0 mistura decisão, documentação, remoção e conexão

A fase 0 diz “nada de código novo”, mas também permite remover código, isolar classes, conectar `RouteContentRuntime`, expandir validator e executar smoke. Isso é muita coisa para uma fase de reconciliação.

Correção:

```text
Separar Fase 0A — decisões/documentos
de Fase 0B — higiene mínima do baseline
e deixar conexão funcional de RouteContentRuntime para uma fase própria.
```

### 1.2. Additive scene support entra cedo demais

No roadmap original, additive scene support aparece dentro de “Runtime roots e Release”. Isso mistura dois problemas diferentes:

```text
Scene composition
≠ Runtime spawned roots
≠ Prefab materialization
```

Correção:

```text
Route scene composition/additive deve ficar em fase própria de Route Content,
depois de RouteContentSet e release semantics.
```

### 1.3. Surface aparece como se dependesse de RuntimeSpawned

Surface é primeiro um contrato authored/local:

```text
SurfaceIdentity
SurfaceRoot
SurfaceSlot
SurfaceAnchor
SurfaceEndpoint
SurfaceSet
```

Ela não precisa começar com prefab spawn. Só a segunda camada, `SurfaceBinding`/`SurfaceContentHandle`, depende de materialização/release.

Correção:

```text
Dividir Surface em:
1. Surface declaration baseline.
2. Surface binding/runtime content depois.
```

### 1.4. LocalContribution já quer substituir discovery global cedo demais

Remover `FindObjectsByType` do fluxo principal é correto, mas só é seguro depois de existir um `ActivityContentSet` real que defina o escopo de busca.

Correção:

```text
ActivityContentSet vem antes de LocalContributionDiscovery.
Local discovery deve operar sobre content sets já conhecidos.
```

---

## 2. Regras de governança do roadmap

### 2.1. Tipos de corte

| Tipo | Pode alterar código? | Finalidade |
|---|---:|---|
| ADR | Não, salvo docs | Decidir ownership, status ou semântica. |
| Hygiene | Sim, mínimo | Remover contradição, renomear, marcar experimental, mover tooling. |
| Foundation | Sim | Criar primitivo usado pelo próximo corte. |
| Integration | Sim | Conectar um primitivo ao fluxo real. |
| Consumer | Sim | Plugar subsistema sem capturar core. |
| Gameplay | Sim | Feature final apoiada em consumers e primitives. |

### 2.2. Critério para avançar fase

Uma fase só fecha quando:

```text
1. O baseline compila.
2. Smoke feliz mínimo passa.
3. Nenhuma surface pública nova fica ambígua.
4. Toda API nova está marcada como Stable, Experimental ou Internal.
5. Não há fallback silencioso para caminho required.
6. O docs/ADR refletem o código.
```

### 2.3. Regra contra abstração prematura

Não criar:

```text
manager global
service locator público
materializer genérico sem primeiro uso real
contribution inventory sem discovery real
surface consumer antes de SurfaceSet
pooling antes de RuntimeContentHandle
camera antes de Surface
actor antes de RuntimeMaterialization
```

---

# Parte I — Fases Revisadas

## Fase 0A — Reconciliação de baseline por ADR

Objetivo: decidir o status oficial do que já existe, sem implementar feature nova.

| ID | Entrega | Decisão |
|---|---|---|
| IF-FW-ROAD-0A1 | ADR: Baseline Reconciliation | Documento único decidindo CameraFlow, RouteContentRuntime, ContentFlow API, RouteContentProfile, QA Canvas e ValidationMode. |
| IF-FW-ROAD-0A2 | ADR: Core vs Consumers | Formalizar que camera/audio/actor/save/input/pause/projectile não ditam o core. |
| IF-FW-ROAD-0A3 | ADR: Public API Status Policy | Definir marcadores: Stable, Experimental, Internal, Deferred. |
| IF-FW-ROAD-0A4 | ADR: QA/Diagnostics Boundary | Definir se `FrameworkQaCanvas` é runtime dev tool, editor tooling ou package separado. |
| IF-FW-ROAD-0A5 | ADR: Dependency Policy | Definir quando dependência como Cinemachine pode entrar no core. |

### Done

- Decisões explícitas registradas.
- Nenhum comportamento novo exigido.
- Cada item ambíguo tem um status: ativo, experimental, congelado, removido ou futuro.

---

## Fase 0B — Higiene mínima do baseline

Objetivo: aplicar somente o necessário para o código não contradizer o baseline decidido.

| ID | Entrega | Regra |
|---|---|---|
| IF-FW-ROAD-0B1 | CameraFlow baseline action | Se removido: apagar código/dependência. Se congelado: excluir do fluxo e marcar claramente. Se ativo: documentar e validar. |
| IF-FW-ROAD-0B2 | RouteContentRuntime baseline action | Não conectar feature nova aqui. Apenas remover/congelar se a decisão for deferir. Se a decisão for conectar, entra na Fase 3. |
| IF-FW-ROAD-0B3 | ContentFlow API status | Marcar materializer/contribution como Experimental/Internal se ainda sem uso real. |
| IF-FW-ROAD-0B4 | RouteContentProfile UX | Marcar como planning-only no Inspector/docs enquanto não executa. |
| IF-FW-ROAD-0B5 | QA Canvas boundary | Aplicar diretiva/dev-only ou documentação de runtime dev tool. |
| IF-FW-ROAD-0B6 | Namespace editor hygiene | Corrigir `Immersive.Framework.Editor.Editor.*` se for corte seguro. |
| IF-FW-ROAD-0B7 | Baseline smoke doc | Criar/atualizar `BASELINE_SMOKE.md` com boot, route switch, activity switch e clear. |

### Done

```text
Código, README e ADRs não se contradizem.
Smoke baseline ainda passa.
Nenhum feature novo foi criado.
```

---

## Fase 1 — Identidade, status de API e diagnostics mínimos

Objetivo: colocar travas antes de expandir ContentFlow/Local/Runtime.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-1A | `FrameworkApiStatus` ou convenção equivalente | Status documental/código para Stable, Experimental, Internal, Deferred. |
| IF-FW-ROAD-1B | ADR: Typed Identity Policy | Strings podem existir como labels/diagnostics; não como chave funcional sem domínio. |
| IF-FW-ROAD-1C | `FrameworkFact` mínimo | Fato estruturado para validation/smoke/diagnostics. Log humano continua separado. |
| IF-FW-ROAD-1D | `ValidationMode` semantics | Definir efeito mínimo: Strict, Standard, Release. |
| IF-FW-ROAD-1E | Content identity ADR | Domínio de identity para handles de content. Sem fallback Guid instável como contrato público. |
| IF-FW-ROAD-1F | Revisar `FrameworkContentHandle` | Aplicar policy de identity ou marcar pontos instáveis como experimental. |

### Não entra

- SessionContentSet.
- Runtime roots.
- Materializer.
- Surface.
- Additive scene.
- Consumers.

### Done

```text
Novas APIs passam a ter status.
Diagnostics estruturado mínimo existe.
Content identity deixa de avançar sem ADR.
ValidationMode deixa de ser enum decorativo.
```

---

## Fase 2 — Session scope e content ownership mínimo

Objetivo: formalizar Session como owner sem criar service locator.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-2A | ADR: Session Scope | `FrameworkRuntimeHost` é owner da Session runtime? Decidir. |
| IF-FW-ROAD-2B | `SessionRuntimeState` explícito | Estado de app/session separado da classe host. |
| IF-FW-ROAD-2C | `SessionContentSet` mínimo | Registro de conteúdo persistente/session-scoped. Inicialmente pode ser vazio ou conter host/diagnostics. |
| IF-FW-ROAD-2D | `SessionContentOwnership` semantics | Distinguir conteúdo registrado, conteúdo owned e conteúdo diagnostic-only. |
| IF-FW-ROAD-2E | Settings source decision | Confirmar `Resources` como temporário ou oficial. |
| IF-FW-ROAD-2F | Session smoke | Boot → Session state válido → Startup route. |

### Não entra

- SessionCompositionContext genérico.
- Service registry.
- Persistent gameplay services.
- Camera/audio/input.

### Done

```text
Session tem owner formal.
SessionContentSet existe sem virar manager global.
RuntimeHost continua simples.
```

---

## Fase 3 — Route baseline e RouteContentRuntime

Objetivo: estabilizar Route sem pular para additive composition.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-3A | `RouteRuntimeState` tipado | Estado da rota ativa com identity própria, não só referência direta a `RouteAsset`. |
| IF-FW-ROAD-3B | `RouteExitResult` mínimo | Resultado explícito da saída anterior, mesmo que ainda simples. |
| IF-FW-ROAD-3C | `RouteContentRuntime` execution decision | Se ativo: conectar ao `RouteLifecycleRuntime` para primary scene/local callbacks. Se deferido: remover do runtime ativo. |
| IF-FW-ROAD-3D | `RouteContentSet` semantics | Definir se é registro, ownership ou ambos com tipos separados. |
| IF-FW-ROAD-3E | Route local callback smoke | Só se `RouteContentRuntime` for ativo. Confirmar route enter/exit callbacks. |
| IF-FW-ROAD-3F | Route validator expansion | Validar o baseline ativo, não features futuras. |

### Não entra

- Additive scene execution.
- Runtime materialization.
- Surface.
- Camera.

### Done

```text
RouteContentRuntime não fica ambíguo.
RouteContentSet tem semântica clara.
Route switch continua funcionando.
```

---

## Fase 4 — Activity content set e readiness mínimo

Objetivo: amadurecer Activity sem copiar `ActivityEntryPipeline`.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-4A | `ActivityRuntimeState` refinado | Estado ativo/none/transition, se necessário. |
| IF-FW-ROAD-4B | `ActivityContentSet` mínimo | Registro do conteúdo local ativo da Activity. |
| IF-FW-ROAD-4C | `ActivityContentLifecycleResult` | Resultado tipado de enter/exit/clear com diagnostics. |
| IF-FW-ROAD-4D | `ActivityReadinessState` mínimo | Inicialmente pode ser sempre ready após bindings aplicados. |
| IF-FW-ROAD-4E | Reclassificar `ActivityContentBinding` | Documentar/nomear como Local Visibility Adapter, não materialização canônica. |
| IF-FW-ROAD-4F | Activity smoke | Activity switch → content set → readiness → clear. |

### Não entra

- ActivityContentProfile.
- Actor/input/camera.
- Reset/snapshot/release.
- Local contribution inventory.

### Done

```text
Activity tem ContentSet e Readiness mínimos.
ActivityContentBinding continua simples, mas não confunde a arquitetura.
```

---

## Fase 5 — Local Contribution baseline

Objetivo: trazer o melhor do Local do NewScripts sem targetId universal nem scan global.

Sequência obrigatória:

```text
ActivityContentSet / RouteContentSet
→ LocalContentIdentity
→ LocalContributionMarker
→ Scoped discovery
→ LocalContributionSet
→ Required/Optional policy
```

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-5A | ADR: Local Identity | Define `LocalContentIdentity` e proíbe path/name como chave funcional. |
| IF-FW-ROAD-5B | `LocalContentIdentity` | Tipo pequeno, imutável, validável. |
| IF-FW-ROAD-5C | `LocalContributionMarker` | Marker authored mínimo; sem capability runtime ainda. |
| IF-FW-ROAD-5D | `LocalContributionDiscovery` scoped | Descobre apenas dentro de content sets conhecidos. |
| IF-FW-ROAD-5E | `LocalContributionSet` | Set tipado por scope. |
| IF-FW-ROAD-5F | Required/Optional policy | Required ausente falha com `FrameworkFact`; optional ausente gera skip. |
| IF-FW-ROAD-5G | Local validators | Duplicidade, identity vazia, required ausente. |
| IF-FW-ROAD-5H | Local smoke | Activity enter → contribution set populado → required policy validada. |

### Não entra

- Capabilities específicas.
- Runtime references.
- Reset/snapshot/release.
- Surface.
- Actors.

### Done

```text
Local deixa de ser só SetActive.
Contribution existe sem virar capability system completo.
Discovery é scoped, não global.
```

---

## Fase 6 — Route scene composition e release de conteúdo

Objetivo: separar scene composition de runtime spawned/materialization.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-6A | ADR: Route Scene Composition | Plan/result, primary/additive, active scene, ownership. |
| IF-FW-ROAD-6B | `RouteSceneCompositionPlan` | Plano antes de load/unload. |
| IF-FW-ROAD-6C | `RouteSceneCompositionResult` | Resultado depois de load/unload. |
| IF-FW-ROAD-6D | Additive scene support | Agora sim, baseado em plan/result e content ownership. |
| IF-FW-ROAD-6E | `RouteContentProfileAsset` execution | Executar somente se deixou de ser planning-only. Requiredness passa a bloquear. |
| IF-FW-ROAD-6F | `ContentReleasePlan` mínimo | Release explícito de route/activity content, sem depender só de `LoadSceneMode.Single`. |
| IF-FW-ROAD-6G | Scene/release smoke | Route com additive required → enter → exit → unload/release confirmado. |

### Não entra

- Prefab materializer.
- Runtime spawned.
- Pool.
- Surface consumers.

### Done

```text
Route additive não é efeito colateral.
Release de conteúdo tem plano/resultado.
RouteContentProfile deixa de parecer promessa falsa.
```

---

## Fase 7 — Surface declaration baseline

Objetivo: criar Surface como contrato authored/local, sem camera/pause/UI ainda.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-7A | ADR: Surface como contrato de espaço | Surface não é camera, pause, UI ou presentation. |
| IF-FW-ROAD-7B | `SurfaceIdentity` | Identity tipada. |
| IF-FW-ROAD-7C | `SurfaceRoot` | Root com role, sem materialização ainda. |
| IF-FW-ROAD-7D | `SurfaceSlot` | Slot authored tipado. |
| IF-FW-ROAD-7E | `SurfaceAnchor` | Anchor authored tipado. |
| IF-FW-ROAD-7F | `SurfaceEndpoint` | Componente authored que declara roots/slots/anchors. |
| IF-FW-ROAD-7G | `SurfaceSet` por scope | Route/Activity/Local podem expor surfaces descobertas. |
| IF-FW-ROAD-7H | Surface validators | Sem identity, duplicate slot/anchor/root role. |
| IF-FW-ROAD-7I | Surface smoke | Scene com endpoint → discovery scoped → SurfaceSet populado. |

### Não entra

- `SurfaceBindingRequest` com prefab.
- Camera.
- Pause.
- UI.
- Presentation.
- Runtime materializer.

### Done

```text
Surface existe como dado e contrato.
Nenhum consumer concreto ainda capturou o modelo.
```

---

## Fase 8 — Runtime roots, materialization e release

Objetivo: criar materialização runtime genérica, sem actor/projectile/pool.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-8A | ADR: Runtime ownership | Session/Route/Activity/Transient. |
| IF-FW-ROAD-8B | `RuntimeScopeRoot` | Root por escopo. |
| IF-FW-ROAD-8C | `RuntimeRootRegistry` | Registry scope → root, sem `GameObject.Find`. |
| IF-FW-ROAD-8D | `RuntimeContentHandle` | Identity, owner scope, state, release. |
| IF-FW-ROAD-8E | `RuntimeMaterializationRequest` / `Result` | Request/result explícitos. |
| IF-FW-ROAD-8F | `PrefabContentMaterializer` | Primeiro materializer concreto. |
| IF-FW-ROAD-8G | `RuntimeReleasePolicy` | Activity exit, Route exit e Session shutdown. |
| IF-FW-ROAD-8H | Runtime materialization smoke | Prefab → handle → release on exit → zero orphan. |

### Não entra

- Pool.
- Actor.
- Projectile.
- Camera.
- Audio.
- Save.

### Done

```text
Runtime-spawned genérico existe sem feature gameplay.
Materialização não depende de subsistema específico.
```

---

## Fase 9 — Surface binding e content placement

Objetivo: conectar Surface declaration com runtime materialization.

| ID | Entrega | Detalhes |
|---|---|---|
| IF-FW-ROAD-9A | `SurfaceBindingRequest` | Solicita root/slot/anchor por identity. |
| IF-FW-ROAD-9B | `SurfaceBindingResult` | Resultado com diagnostics e handle. |
| IF-FW-ROAD-9C | `SurfaceContentHandle` | Binding + runtime content + release. |
| IF-FW-ROAD-9D | `RuntimeSurfaceBinding` | Materializa prefab em SurfaceSlot/SurfaceRoot. |
| IF-FW-ROAD-9E | Binding release order | Surface binding libera antes de content/root exit. |
| IF-FW-ROAD-9F | Surface binding smoke | Prefab em slot → Activity exit → binding released → content released. |

### Não entra

- Pause overlay.
- Camera rig.
- Actor presentation.
- Pooling.

### Done

```text
Surface pode receber conteúdo runtime sem virar pause/camera.
```

---

## Fase 10 — Consumers intermediários

Objetivo: consumidores úteis, mas ainda sem gameplay pesado.

Ordem recomendada:

```text
Input
Snapshot/Save contract
Pause as Surface consumer
```

| ID | Entrega | Módulo | Regra |
|---|---|---|---|
| IF-FW-ROAD-10A | ADR: Input ownership | Input | Sem action map string como chave funcional. |
| IF-FW-ROAD-10B | `InputModeContract` | Input | Activity/Route declaram requisito, consumer aplica. |
| IF-FW-ROAD-10C | ADR: Snapshot model | Save | Envelope typed: owner, schema, version, payload. |
| IF-FW-ROAD-10D | `ISnapshotParticipant` | Save | Capability-level, sem backend concreto. |
| IF-FW-ROAD-10E | `SnapshotSet` | Save | Captura/restaura participants descobertos. |
| IF-FW-ROAD-10F | ADR: Pause as consumer | Pause | Pause consome Surface/Input/Activity; não possui surface. |
| IF-FW-ROAD-10G | `PauseSurfaceConsumer` | Pause | Usa SurfaceBinding; não instancia diretamente em endpoint local. |

### Done

```text
Input, Snapshot e Pause consomem contextos.
Nenhum deles descobre o mundo sozinho.
```

---

## Fase 11 — Consumers avançados

Objetivo: plugar subsistemas que dependem de Surface + Runtime + Contribution.

Ordem sugerida:

```text
Camera
Audio
Actor
Pooling
```

| ID | Entrega | Módulo | Regra |
|---|---|---|---|
| IF-FW-ROAD-11A | ADR: Camera as consumer | Camera | Camera consome Surface/Anchor; não define lifecycle core. |
| IF-FW-ROAD-11B | Camera consumer baseline | Camera | Sem `FrameworkCameraAuthority` static global. |
| IF-FW-ROAD-11C | ADR: Audio as consumer | Audio | Audio recebe lifecycle context; não possui route/activity. |
| IF-FW-ROAD-11D | Audio lifecycle consumer | Audio | Port/adapter, sem global service locator. |
| IF-FW-ROAD-11E | ADR: Actor runtime boundary | Actor | Actor é runtime content/contribution, não lifecycle core. |
| IF-FW-ROAD-11F | Actor materialization baseline | Actor | Request/result/handle; contribui capabilities depois. |
| IF-FW-ROAD-11G | ADR: Pooling package boundary | Pooling | `com.immersive.pooling` é técnico, não projectile-first. |
| IF-FW-ROAD-11H | Pooled materializer | Pooling | Rent/return por `RuntimeContentHandle`. |

### Done

```text
Consumers avançados entram sem statics globais e sem controlar lifecycle.
```

---

## Fase 12 — Gameplay capabilities

Objetivo: recursos de gameplay apoiados na base.

Ordem:

```text
Projectile
Impact/Damage
Attributes
Advanced Actor capabilities
```

| ID | Entrega | Regra |
|---|---|
| IF-FW-ROAD-12A | Projectile as RuntimeSpawned | Pooled/Prefab runtime content; return/release explícito. |
| IF-FW-ROAD-12B | Impact as capability | Impact não controla projectile lifecycle diretamente. |
| IF-FW-ROAD-12C | Damage as actor capability | Damage aplica mutation por contrato. |
| IF-FW-ROAD-12D | Attributes as snapshot-capable capability | Attributes participam de SnapshotSet. |

### Done

```text
Gameplay capabilities não dependem de pipeline monolítica.
Cada capability entra por contribution/consumer.
```

---

# Parte II — Mapa de dependências revisado

```text
F0A  Baseline ADRs
  ↓
F0B  Baseline hygiene
  ↓
F1   API status + Identity + Diagnostics
  ↓
F2   Session scope
  ↓
F3   Route baseline
  ↓
F4   Activity content/readiness
  ↓
F5   Local contribution
  ↓
F6   Route scene composition + release
  ↓
F7   Surface declaration
  ↓
F8   Runtime roots/materialization
  ↓
F9   Surface binding/runtime placement
  ↓
F10  Consumers intermediários
  ↓
F11  Consumers avançados
  ↓
F12  Gameplay capabilities
```

Paralelismo permitido:

```text
ADRs de F10/F11 podem ser escritos antes.
Código de F10/F11 não deve entrar antes de F7/F8/F9.
Hygiene editorial pode ocorrer junto com qualquer fase se não alterar runtime.
```

---

# Parte III — Correções específicas sobre o roadmap original

## 1. CameraFlow

Original: decidir e executar em Fase 0.

Revisado:

```text
F0A decide.
F0B só remove/congela se a decisão for remover/congelar.
Se for manter ativo, não expandir até F11.
```

## 2. RouteContentRuntime

Original: decidir e talvez conectar em Fase 0/Fase 2.

Revisado:

```text
F0A decide.
F0B remove/congela se deferido.
F3 conecta se ativo, porque F3 é a fase Route baseline.
```

## 3. ContentFlow

Original: `SessionContentSet`, identity, ownership e materializer avançam próximos.

Revisado:

```text
F1 primeiro define identity/status.
F2 define SessionContent.
F3/F4 estabilizam Route/Activity content.
F8 só então cria materializer concreto.
```

## 4. Additive scene support

Original: aparece em Runtime roots/release.

Revisado:

```text
F6, junto de RouteSceneCompositionPlan/Result.
```

## 5. Surface

Original: Surface baseline depois de Runtime roots.

Revisado:

```text
F7 declara Surface sem runtime binding.
F9 conecta Surface com RuntimeContent.
```

## 6. LocalContribution

Original: já remove global `FindObjectsByType` como done da Fase 3.

Revisado:

```text
Só remover do fluxo principal depois de ActivityContentSet/RouteContentSet definirem escopo de discovery.
```

---

# Parte IV — Backlog de ADRs revisado

| Fase | ADR | Tema |
|---|---|---|
| F0A | ADR-BL-001 | Baseline Reconciliation: CameraFlow, RouteContentRuntime, ContentFlow, RouteContentProfile, QA Canvas, ValidationMode. |
| F0A | ADR-BL-002 | Core vs Consumers. |
| F0A | ADR-BL-003 | Public API Status Policy. |
| F0A | ADR-BL-004 | QA/Diagnostics Boundary. |
| F0A | ADR-BL-005 | Dependency Policy. |
| F1 | ADR-ID-001 | Typed Identity Policy. |
| F1 | ADR-DIAG-001 | FrameworkFact vs human log. |
| F1 | ADR-CONTENT-001 | Content identity domain. |
| F2 | ADR-SESSION-001 | Session Scope and owner. |
| F2 | ADR-SESSION-002 | SessionContent ownership semantics. |
| F3 | ADR-ROUTE-001 | RouteRuntimeState and RouteContentRuntime status. |
| F3 | ADR-ROUTE-002 | RouteContentSet semantics. |
| F4 | ADR-ACTIVITY-001 | ActivityContentSet and Readiness baseline. |
| F5 | ADR-LOCAL-001 | Local identity. |
| F5 | ADR-LOCAL-002 | Local contribution discovery and requiredness. |
| F6 | ADR-SCENE-001 | Route scene composition plan/result. |
| F6 | ADR-RELEASE-001 | Content release plan by scope. |
| F7 | ADR-SURFACE-001 | Surface as space contract. |
| F8 | ADR-RUNTIME-001 | Runtime ownership and roots. |
| F8 | ADR-RUNTIME-002 | Materialization request/result/handle. |
| F9 | ADR-SURFACE-002 | Surface binding and content placement. |
| F10 | ADR-INPUT-001 | Input ownership. |
| F10 | ADR-SAVE-001 | Snapshot envelope and schema. |
| F10 | ADR-PAUSE-001 | Pause as Surface/Input/Activity consumer. |
| F11 | ADR-CAMERA-001 | Camera as Surface consumer. |
| F11 | ADR-AUDIO-001 | Audio as lifecycle consumer. |
| F11 | ADR-ACTOR-001 | Actor runtime boundary. |
| F11 | ADR-POOL-001 | Pooling package boundary. |

---

# Parte V — Próximo passo recomendado

## Ação imediata

Criar um documento único:

```text
ADR-BL-001 — Baseline Reconciliation
```

Ele deve responder:

1. `CameraFlow` fica ativo, experimental congelado ou removido?
2. `RouteContentRuntime` fica ativo, experimental congelado ou removido?
3. `ContentFlow` público fica estável, experimental ou internal?
4. `RouteContentProfileAsset` é planning-only ou execução futura explícita?
5. `FrameworkQaCanvas` é runtime dev tool, editor tooling ou package separado?
6. `ValidationMode` terá semântica concreta agora ou será placeholder documentado?

## Primeiro corte técnico depois do ADR

Se o ADR não mandar remover muita coisa, o primeiro corte técnico deve ser:

```text
F0B — Baseline hygiene
```

Não `SessionContentSet` ainda.

Motivo:

```text
Antes de criar novos conceitos, o package precisa parar de mentir sobre o que já existe.
```

Depois da higiene, seguir para:

```text
F1 — API status + Identity + Diagnostics
```

---

# Parte VI — Sumário executivo

O roadmap original é bom como visão macro. A versão revisada melhora a execução porque:

```text
1. separa decisão de implementação;
2. evita conectar feature nova na fase de reconciliação;
3. não mistura additive scene com runtime spawned;
4. divide Surface em declaração e binding;
5. coloca identity/API status antes de expandir ContentFlow;
6. mantém consumers fora até existirem Surface/Runtime/Contribution;
7. preserva a simplicidade atual do package por mais tempo.
```

Frase operacional:

```text
Não avançar para feature enquanto o baseline ativo ainda for ambíguo.
Não avançar para consumer enquanto owner, identity, content set e release ainda forem ambíguos.
```
