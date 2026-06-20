# Comparação Controlada — `NewScripts` vs Package `com.immersive.framework`

Status: diagnóstico comparativo consolidado  
Fontes:
- `NewScripts-System-XRay-Consolidated.md`
- `Package-System-XRay-Consolidated.md`

Modo: comparação arquitetural / sem implementação / sem patch  
Uso pretendido: orientar ADRs, correções de baseline e roadmap cíclico do Immersive Framework.

---

## 0. Como ler este documento

Este documento é o terceiro artefato da sequência:

```text
1. NewScripts-System-XRay-Consolidated.md
   → referência funcional e arquitetural extraída do sistema antigo.

2. Package-System-XRay-Consolidated.md
   → estado real atual do package com.immersive.framework.

3. Framework-Comparison-NewScripts-vs-Package.md
   → comparação controlada entre os dois.
```

Regra de interpretação:

```text
NewScripts prova capacidades e problemas.
Package prova o baseline atual.
A comparação define direção, não autoriza copiar código antigo.
```

---

# Parte I — Diagnóstico Executivo

## 1. Leitura comparativa central

O `NewScripts` é funcionalmente rico, mas arquiteturalmente pesado. Ele prova que o modelo por escopos é útil:

```text
Session
→ Route
→ Activity
→ Local
```

e que duas dimensões transversais precisam existir:

```text
RuntimeSpawned
Surface
```

Além disso, ele mostra que subsistemas como camera, audio, pause, input, save, actor, projectile, attributes, damage, pooling, diagnostics e QA devem ser tratados como **consumidores** do lifecycle, não como centro do framework.

O package atual `com.immersive.framework` já possui um baseline operacional real:

```text
Bootstrap
→ FrameworkRuntimeHost
→ GameFlowRuntime
→ RouteLifecycleRuntime
→ SceneLifecycleRuntime
→ ActivityFlowRuntime
→ ActivityContentRuntime
```

Mas ele ainda está em uma fase anterior ao modelo extraído do `NewScripts`. O package tem uma base funcional para boot, route primary scene, activity switching e local activity binding, porém ainda não possui uma modelagem madura de `SessionContent`, `ActivityContentSet`, `LocalContributionSet`, `RuntimeSpawned`, `Surface`, release policy ou contribution inventory.

A conclusão principal:

```text
O package deve evoluir para a gramática do NewScripts, mas não para o shape do NewScripts.
```

---

## 2. Equivalência de maturidade

| Área | NewScripts | Package atual | Leitura |
|---|---|---|---|
| Session | Rico, mas misturado com composition global e service locator. | Boot/host/settings funcionais, sem SessionContent formal. | Package tem baseline melhor; NewScripts dá direção de capacidades futuras. |
| Route | Route operacional rica, com scene composition, handoff, exit, surfaces e consumers. | Route primary scene + state + RouteContentSet parcial. | Package cobre mínimo; falta route materialization/release/contribution. |
| Activity | Activity muito rica, com content profile, entry, local contributors, readiness, reset/snapshot/release. | Activity switching + SetActive local binding. | Package é mínimo; falta ActivityContentSet, discovery e readiness real. |
| Local | Markers, contributors, capability scan, inventory e lifecycle local. | ActivityContentBinding e RouteContentBinding simples. | Package tem Local visibility; falta contribution model. |
| ContentFlow | No NewScripts aparece como conceito distribuído, não módulo único. | Já existe módulo `ContentFlow`, mas ainda sem consumidores fortes. | Package acertou vocabulário inicial, mas publicou cedo demais algumas interfaces. |
| RuntimeSpawned | Rico: actors, pools, projectiles, presentation, pause content, camera rigs. | Ausente. | Não implementar ainda; depende de identity/root/release. |
| Surface | Rico, mas preso a pause/camera/presentation. | Ausente formalmente. | Deve nascer genérico antes de camera/pause. |
| CrossCutting | Muitos consumers já funcionais, mas acoplados. | Logging, events, validation, QA e CameraFlow parcial/ambíguo. | Manter infra mínima; congelar consumers avançados. |
| Editor/Validation | NewScripts tem práticas funcionais dispersas. | Package tem setup/validators iniciais. | Package é melhor base para validators; ampliar após decisões. |
| Diagnostics/QA | NewScripts tem facts, panels e smoke rico. | Package tem logger e QA Canvas. | Preservar QA, mas manter dev/tooling separado do runtime core. |

---

## 3. Diagnóstico de convergência

### 3.1. O package já acertou

| Decisão no package | Por que está correta |
|---|---|
| `GameApplicationAsset` como raiz pública | Evita começar direto por pipeline ou subsistema. |
| Bootstrap + host persistente | Dá boundary de Session inicial sem copiar `DependencyManager`. |
| `GameFlowRuntime` separado | Cria ponto único para requests Route/Activity. |
| `RouteLifecycleRuntime` + `SceneLifecycleRuntime` | Separa route identity de carregamento físico de cena. |
| `ActivityFlowRuntime` separado | Activity já é lifecycle próprio, não subfunção da Route. |
| `ActivityContentBinding` simples | Bom baseline local mínimo, desde que não seja tratado como materialização canônica. |
| `FrameworkLogger` usando package de logging | Direção correta contra `Debug.Log` direto. |
| Editor setup/validation | Bom trilho de bootstrap canônico. |

### 3.2. O package antecipou coisas cedo demais

| Item | Problema |
|---|---|
| `CameraFlow` | Existe no runtime package e adiciona dependência Cinemachine antes de Surface/RuntimeSpawned/ContentFlow estarem maduros. |
| `IFrameworkContentMaterializer` | Interface pública existe sem materializer concreto e sem ownership/release model. |
| `IFrameworkContentContribution` / `FrameworkContentContributionMarker` | Contribution existe como vocabulário, mas sem discovery/inventory real. |
| `RouteContentRuntime` | Implementado, mas não conectado ao fluxo real; isso cria superfície pública ambígua. |
| `RouteContentProfileAsset` | Planejamento existe, mas requiredness e cenas adicionais não executam ainda. |
| `FrameworkQaCanvas` runtime público | Útil, mas precisa ser tratado como dev/QA tooling, não API de produto. |

### 3.3. O package ainda não tem

| Lacuna | Consequência |
|---|---|
| `SessionContentSet` | Conteúdo persistente/session-scoped ainda não tem owner formal. |
| `RouteContentRuntime` ativo ou decisão de remoção | Route Local fica ambíguo. |
| `ActivityContentSet` | Activity content ainda é SetActive/local binding, não materialização. |
| `LocalContributionSet` | Não há inventário de capabilities locais. |
| `RuntimeRootRegistry` | RuntimeSpawned não pode nascer com ownership confiável. |
| `SurfaceIdentity/SurfaceSet` | Pause/camera/presentation não devem avançar sem isso. |
| Release policy | ContentSet registra, mas não governa liberação. |
| Identity model consolidado | Handles e content ids ainda usam strings/path/name/fallback instável. |
| Validator cobrindo tudo que já existe | O package expõe mais superfície do que valida. |

---

# Parte II — Comparação por Escopo

## 4. Session

### 4.1. NewScripts

O `NewScripts` mostra uma Session rica, mas espalhada:

```text
GlobalCompositionRoot
DependencyManager
RuntimeConfigRegistry
RuntimePersistentScenesComposition
SessionOperationalRuntimeComposer
SessionOperationalPipeline
PlayerParticipationRuntime
```

Valor extraído:

- boot determinístico;
- runtime config;
- cenas persistentes;
- serviços persistentes;
- player participation;
- diagnostics/facts;
- fail-fast.

Problema extraído:

- service locator;
- composition global;
- Session operacional muito grande;
- subsistemas persistentes misturados com lifecycle.

### 4.2. Package

O package possui:

```text
ImmersiveFrameworkBootstrap
FrameworkRuntimeHost
FrameworkRuntimeState
GameApplicationAsset
ImmersiveFrameworkSettingsAsset
FrameworkBootValidator
FrameworkQaCanvas
```

Está melhor que o NewScripts em simplicidade e ausência de service locator público, mas ainda não formalizou:

- `SessionContentSet`;
- `SessionRuntimeRoot`;
- `SessionCompositionContext`;
- boundaries para consumers persistentes;
- política de QA/dev runtime.

### 4.3. Decisão

```text
Manter o package como base.
Não importar GlobalCompositionRoot/DependencyManager.
Adicionar SessionContent/Root/Diagnostics gradualmente.
```

### 4.4. Próximas decisões

| Decisão | Status recomendado |
|---|---|
| `FrameworkRuntimeHost` é o owner de Session? | ADR obrigatório. |
| Settings em `Resources` é definitivo? | ADR ou dívida documentada. |
| QA Canvas fica runtime ou dev-only? | ADR/tooling decision. |
| SessionContentSet entra antes de RuntimeSpawned? | Sim. |

---

## 5. Route

### 5.1. NewScripts

Route no `NewScripts` inclui:

- route asset;
- route identity;
- route plan;
- scene composition;
- previous route exit;
- route snapshot;
- startup route;
- Route → Activity handoff;
- route surfaces;
- route contributions;
- audio/camera/input/save/pause consumers.

Valor extraído:

```text
Route = contexto operacional materializado e estabilizado antes da Activity.
```

Problema extraído:

```text
Route virou pipeline monolítica que conhece subsistemas demais.
```

### 5.2. Package

Package atual:

- `RouteAsset`;
- `RouteLifecycleRuntime`;
- `SceneLifecycleRuntime`;
- `RouteContentSet`;
- `RouteContentProfileAsset`;
- `RouteContentMaterializationPlan`;
- `RouteContentBinding`;
- `RouteContentRuntime` inativo/ambíguo.

O package ainda trata Route majoritariamente como:

```text
Route identity + primary scene loading + optional startup activity.
```

### 5.3. Decisão

Não avançar para route additive execution ainda. Antes:

1. decidir `RouteContentRuntime`;
2. alinhar `RouteContentBinding`;
3. definir se `RouteContentSet` é diagnóstico ou ownership;
4. definir release semantics;
5. decidir identity de content handles.

### 5.4. Matriz Route

| Conceito | NewScripts | Package | Decisão |
|---|---|---|---|
| RouteAsset | Rico demais/god asset | Simples e bom | Preservar package; expandir por profiles. |
| Scene composition | Forte, mas acoplada | Primary scene single | Evoluir depois de ContentSet/release. |
| RouteContentSet | Implícito em snapshots/loaded scenes | Existe parcial | Manter e amadurecer. |
| RouteContentRuntime | NewScripts tem route local consumers | Existe mas inativo | Conectar ou remover/deferir. |
| Route → Activity handoff | Explícito e rico demais | StartupActivity simples | Evoluir com payload mínimo. |
| Route contributions | Muitos consumers | Ausente | Só após contribution model. |

---

## 6. Activity

### 6.1. NewScripts

Activity no `NewScripts` é rica:

- `ActivityAsset`;
- content profile;
- loaded set;
- entry context;
- local discovery;
- setup inventory;
- capability inventory;
- participant binding;
- readiness;
- reset/snapshot/restore/release.

Problema:

- `SessionActivityPipeline` e `ActivityEntryPipeline` grandes demais;
- identity textual;
- Activity conhece subsistemas concretos.

### 6.2. Package

Package atual:

- `ActivityAsset`;
- `ActivityFlowRuntime`;
- `ActivityContentRuntime`;
- `ActivityContentBinding`;
- lifecycle receivers;
- UnityEvent bridges;
- request/clear events.

O package possui uma Activity real, mas mínima:

```text
Activity = identidade ativa + Local SetActive bindings.
```

### 6.3. Decisão

`ActivityContentBinding` deve ser preservado como **adapter local simples**, não como canonical Activity materialization.

O próximo amadurecimento correto não é criar actor/input/camera. É criar:

```text
ActivityContentSet
ActivityContentLifecycleResult
ActivityReadinessState mínimo
ActivityLocalContributionSet depois
```

### 6.4. Matriz Activity

| Conceito | NewScripts | Package | Decisão |
|---|---|---|---|
| ActivityAsset | Rico e acumulado | Simples | Package está melhor para baseline. |
| ActivityContentProfile | Existe forte | Ausente | Adiar até Route/ContentFlow claro. |
| ActivityContentSet | LoadedSet existe | Ausente | Próximo conceito importante. |
| ActivityLocalBinding | Contributor/capability rico | SetActive simples | Preservar simples; não promover a inventory ainda. |
| Readiness | Forte | Ausente | Criar mínimo antes de consumers. |
| Reset/snapshot/release | Forte | Ausente | Depois de LocalContributionSet. |

---

## 7. Local

### 7.1. NewScripts

Local no `NewScripts` é um sistema real de contribuição:

```text
ActivityObjectContributor
→ discovery
→ contribution report
→ capability scan
→ inventory
→ reset/snapshot/restore/release
```

Valor:

- contributor marker explícito;
- discovery por escopo;
- requiredness;
- runtime references;
- lifecycle por componente;
- exit freeze.

Problema:

- `targetId` como chave universal;
- paths como identity;
- scanners específicos demais;
- requiredness espalhada.

### 7.2. Package

Local no package é:

- `ActivityContentBinding`;
- `RouteContentBinding`;
- content lifecycle receivers;
- UnityEvent bridges;
- `FrameworkContentContributionMarker` precursor.

Não há ainda:

- Local identity;
- discovery;
- inventory;
- capability scan;
- requiredness policy;
- reset/release local.

### 7.3. Decisão

O package não deve saltar direto para o modelo rico do NewScripts. A sequência correta é:

```text
LocalContentIdentity
→ LocalContentContributionMarker
→ LocalContributionDiscovery
→ LocalContributionSet
→ Local lifecycle participants
```

Antes disso, `ActivityContentBinding` continua como adapter simples e explícito.

---

## 8. ContentFlow

### 8.1. NewScripts

No `NewScripts`, ContentFlow não é um módulo único. Ele aparece como uma gramática distribuída:

```text
Definition
→ Plan
→ Materialization
→ ContentSet
→ Contributions
→ Release
```

### 8.2. Package

O package já criou um módulo `ContentFlow` com:

- `FrameworkContentScope`;
- `FrameworkContentKind`;
- `FrameworkContentRequiredness`;
- `FrameworkContentHandle`;
- `FrameworkContentSet`;
- `IFrameworkContentMaterializer`;
- `IFrameworkContentContribution`;
- `FrameworkContentContributionMarker`.

Isso é uma boa direção, mas parcialmente prematura.

### 8.3. Risco

O package pode cristalizar APIs antes de ter:

- owner;
- release;
- discovery;
- materializer concreto;
- contribution inventory;
- typed identity.

### 8.4. Decisão

Manter `ContentFlow`, mas reclassificar internamente:

| Item | Decisão |
|---|---|
| Scope/Kind/Requiredness | Preservar. |
| Handle/Set | Preservar, mas revisar identity/release. |
| Materializer interface | Congelar como experimental até uso real. |
| Contribution interface/marker | Congelar ou internalizar até discovery/inventory. |
| RouteContentSet | Amadurecer como primeiro consumer real. |

---

## 9. RuntimeSpawned

### 9.1. NewScripts

RuntimeSpawned é rico e espalhado:

- player actors;
- projectiles;
- pooling;
- actor presentation;
- pause content;
- camera rigs;
- audio runtime objects.

Valor extraído:

- runtime handles;
- typed-ish actor runtime identity;
- runtime origin metadata;
- pool return;
- release by scope;
- owner tracking.

Problema:

- roots criados por adapters;
- `GameObject.Find`;
- string identity;
- `Destroy` espalhado;
- pooling acoplado a projectile.

### 9.2. Package

Ausente formalmente.

### 9.3. Decisão

Não implementar RuntimeSpawned agora.

Pré-requisitos:

```text
SessionContentSet
RouteContentSet ownership/release
ActivityContentSet
LocalContributionSet
RuntimeRootRegistry
RuntimeContentIdentity
RuntimeReleasePolicy
```

---

## 10. Surface

### 10.1. NewScripts

Surface aparece forte, mas preso aos consumers:

- pause surface;
- camera anchors;
- actor presentation containers;
- spawn origins;
- overlay/content roots.

Valor extraído:

```text
Surface = contrato de espaço, não pause/camera/UI/presentation.
```

### 10.2. Package

Ausente formalmente.

### 10.3. Decisão

Não continuar CameraFlow/Pause/UI antes de Surface baseline.

Ordem recomendada:

```text
SurfaceIdentity
→ SurfaceRoot
→ SurfaceSlot
→ SurfaceAnchor
→ SurfaceSet
→ SurfaceBindingRequest/Result
→ SurfaceContentHandle
```

---

## 11. CrossCutting / Consumers

### 11.1. NewScripts

Subsistemas ricos:

- Camera;
- Audio;
- Input;
- Save/Snapshot;
- Pause;
- Actor/Player;
- Attributes/Damage;
- Projectile;
- Pooling;
- Logging/Diagnostics/QA;
- Runtime config;
- Event bus;
- typed IDs.

Padrão bom:

```text
Contracts + request/result + ports/adapters + facts.
```

Padrão ruim:

```text
Consumers resolvendo composition/global services/scene scan sozinhos.
```

### 11.2. Package

Atualmente possui:

- logging;
- foundation events;
- validation;
- QA Canvas;
- CameraFlow ativo/ambíguo;
- no save/input/pause/actor/audio/pooling concrete package integration.

### 11.3. Decisão

Manter core pequeno. Consumers entram depois:

```text
Input
→ Snapshot/Save contract
→ Pause as Surface consumer
→ Camera as Surface/Presentation consumer
→ Audio
→ Actor
→ Pooling
→ Projectile/Damage/Attributes
```

---

# Parte III — Decisões de Preservar / Revisar / Remover / Adiar

## 12. Preservar do package atual

| Item | Motivo |
|---|---|
| `GameApplicationAsset` | Raiz pública correta. |
| `ImmersiveFrameworkSettingsAsset` | Backing setting simples. |
| `ImmersiveFrameworkBootstrap` | Boot mínimo funcional. |
| `FrameworkRuntimeHost` | Bom início de Session owner. |
| `GameFlowRuntime` | Bom boundary de requests. |
| `RouteLifecycleRuntime` | Bom owner de route ativa. |
| `SceneLifecycleRuntime` | Boa separação física de scene loading. |
| `ActivityFlowRuntime` | Activity já tem lifecycle próprio. |
| `ActivityContentRuntime` | Bom adapter local simples. |
| Request triggers + UnityEvent bridges | Boa UX authored sem expor runtime internals. |
| `FrameworkLogger` com logging package | Direção correta. |
| Editor settings/validators | Base canônica do bootstrap. |
| ADRs existentes | Manter, mas atualizar com reconciliações. |

## 13. Revisar antes de avançar

| Item | Revisão necessária |
|---|---|
| `RouteContentRuntime` | Conectar ao fluxo real, remover ou marcar experimental/deferido. |
| `RouteContentBinding` | Confirmar se é baseline ativo ou futuro. |
| `ContentFlow` public API | Revisar estabilidade antes de criar consumers. |
| `FrameworkContentHandle` | Revisar identity e fallback Guid. |
| `FrameworkContentSet` | Decidir se é registro ou ownership/release. |
| `RouteContentProfileAsset` | Manter planning-only ou executar? |
| `FrameworkQaCanvas` | Runtime tool ou dev-only package? |
| `ValidationMode` | Definir efeito real. |
| `FrameworkLogger.Create()` repetido | Aceitável por enquanto, revisar em diagnostics pass. |
| Editor namespace duplicado | Corrigir em hygiene futura. |

## 14. Remover ou congelar

| Item | Decisão recomendada |
|---|---|
| `CameraFlow` | Congelar/remover do baseline até Surface/RuntimeSpawned. |
| Cinemachine como dependência core | Remover se CameraFlow for congelado. |
| Route additive execution | Não avançar agora. |
| RuntimeSpawned/pooling | Adiar até roots/release. |
| Surface consumers concretos | Adiar até Surface baseline. |
| Activity advanced content/profile | Adiar até Route/ContentFlow claro. |
| Save/input/actor/projectile/audio | Adiar como consumers. |

## 15. Adicionar depois, em ordem

| Ordem | Conceito |
|---:|---|
| 1 | ADR de reconciliação do baseline. |
| 2 | SessionContentSet mínimo. |
| 3 | RouteContentRuntime decisão/conexão ou remoção. |
| 4 | ContentFlow identity/release semantics. |
| 5 | ActivityContentSet mínimo. |
| 6 | LocalContentIdentity. |
| 7 | LocalContributionMarker/Discovery/Set. |
| 8 | RuntimeRootRegistry. |
| 9 | RuntimeContentHandle/ReleasePolicy. |
| 10 | SurfaceIdentity/Root/Slot/Anchor/Set. |
| 11 | Capability inventory. |
| 12 | Reset/release local. |
| 13 | Snapshot/save contract. |
| 14 | Input consumer. |
| 15 | Pause/Camera/Audio/Actor/Pooling/Projectile. |

---

# Parte IV — Riscos Críticos de Convergência

## 16. Riscos High

| Risco | Origem | Impacto | Mitigação |
|---|---|---|---|
| CameraFlow ativo contra docs | Package | Baseline contraditório | ADR: remover/congelar/reativar oficialmente. |
| RouteContentRuntime inativo | Package | API pública que não executa | Decisão imediata. |
| ContentFlow público prematuro | Package | Contratos errados cristalizam | Marcar experimental/revisar antes de ampliar. |
| Service locator como tentação de port | NewScripts | Recria problema antigo | Proibir em ADR público. |
| IDs textuais retornando via Content/Local | Ambos | Stale/foreign/collision | Typed identity ADR antes de avançar. |
| Subsystems dominando core | Ambos | Framework vira soma de features | Consumers só após scope baseline. |

## 17. Riscos Medium

| Risco | Impacto | Mitigação |
|---|---|---|
| `ActivityContentBinding` ser entendido como materialização | Modelagem errada de Activity | Documentar como LocalVisibilityAdapter. |
| `RouteContentProfileAsset` parecer executável | Confusão de authoring | UI/Inspector/Docs: planning-only. |
| QA Canvas virar runtime feature | Superfície de produto inflada | Dev-only/tooling policy. |
| Validator incompleto | Config inválida passa | Expandir depois da reconciliação. |
| Runtime assembly único | Consumers contaminam core | Separar asmdefs/pacotes quando necessário. |
| Logging repetido sem policy | Ruído e inconsistência | Diagnostics pass futuro. |

---

# Parte V — Roadmap de Convergência

## 18. Fase 0 — Reconciliação documental e de baseline

Objetivo: impedir que o package avance sobre contradições conhecidas.

Decisões:

1. CameraFlow está ativo, experimental ou removido?
2. RouteContentRuntime fica ativo ou deferido?
3. ContentFlow APIs são estáveis ou experimentais?
4. QA Canvas é runtime público ou tooling?
5. ValidationMode tem semântica concreta ou placeholder?

Resultado esperado:

```text
Baseline oficial do package fica claro.
Nada novo avança antes disso.
```

## 19. Fase 1 — Session e Content primitives

Objetivo: criar base comum sem subsistemas.

Possíveis cortes:

```text
IF-FW-COMP-1A — Baseline Reconciliation ADR/docs
IF-FW-COMP-1B — SessionContentSet mínimo
IF-FW-COMP-1C — FrameworkContentHandle identity review
IF-FW-COMP-1D — FrameworkContentSet ownership vs diagnostics decision
```

Não incluir:

- camera;
- audio;
- actor;
- pause;
- route additive;
- runtime spawned.

## 20. Fase 2 — Route e Activity ContentSet

Objetivo: amadurecer o que já existe.

Possíveis cortes:

```text
IF-FW-COMP-2A — RouteContentRuntime decision
IF-FW-COMP-2B — RouteContentSet release semantics baseline
IF-FW-COMP-2C — ActivityContentSet mínimo
IF-FW-COMP-2D — ActivityContentBinding reclassified as LocalVisibilityAdapter docs/API
```

Não incluir:

- Activity content profile avançado;
- additional route scene execution;
- contribution inventory.

## 21. Fase 3 — Local Contribution baseline

Objetivo: trazer o melhor do NewScripts sem importar pipeline.

Possíveis cortes:

```text
IF-FW-COMP-3A — LocalContentIdentity
IF-FW-COMP-3B — LocalContributionMarker baseline
IF-FW-COMP-3C — LocalContributionDiscovery scope
IF-FW-COMP-3D — LocalContributionSet
IF-FW-COMP-3E — Required/Optional policy
```

Não incluir ainda:

- reset/snapshot/release;
- actors;
- surface;
- pooling.

## 22. Fase 4 — Runtime roots e release

Objetivo: preparar RuntimeSpawned sem spawn feature concreta.

Possíveis cortes:

```text
IF-FW-COMP-4A — RuntimeScopeRoot
IF-FW-COMP-4B — RuntimeRootRegistry
IF-FW-COMP-4C — RuntimeContentHandle
IF-FW-COMP-4D — RuntimeReleasePolicy
IF-FW-COMP-4E — RuntimeCleanupPlan
```

Não incluir ainda:

- pool service;
- projectile;
- actor materialization;
- presentation.

## 23. Fase 5 — Surface baseline

Objetivo: criar Surface antes de camera/pause.

Possíveis cortes:

```text
IF-FW-COMP-5A — SurfaceIdentity
IF-FW-COMP-5B — SurfaceRoot
IF-FW-COMP-5C — SurfaceSlot
IF-FW-COMP-5D — SurfaceAnchor
IF-FW-COMP-5E — SurfaceSet
IF-FW-COMP-5F — SurfaceBindingResult
```

Não incluir:

- Cinemachine;
- pause overlay;
- UI surface concrete;
- actor presentation.

## 24. Fase 6 — Consumers intermediários

Objetivo: plugar consumidores só depois dos contratos.

Ordem:

```text
Input
Snapshot/Save contract
Pause as Surface consumer
Camera as Surface consumer
Audio
Actor runtime
Pooling
Projectile/Damage/Attributes
```

---

# Parte VI — ADRs Necessários

## 25. ADRs imediatos

| ADR | Motivo |
|---|---|
| Baseline Reconciliation | Resolver CameraFlow/RouteContentRuntime/ContentFlow status. |
| Core vs Consumers | Impedir que camera/audio/actor/save ditem arquitetura. |
| Session Scope | Definir owner de Session, SessionContent e persistent roots. |
| ContentFlow Public API Policy | Separar estável, experimental e internal. |
| Content Identity | Definir ids de content handles sem path/name como contrato frágil. |
| RouteContentRuntime Status | Ativo, removido ou deferido. |
| QA/Diagnostics Boundary | Runtime, tooling ou editor-only. |

## 26. ADRs de médio prazo

| ADR | Momento |
|---|---|
| RouteContentSet ownership/release | Antes de route additive/materialization. |
| ActivityContentSet | Antes de content profile avançado. |
| LocalContributionSet | Antes de reset/snapshot/local lifecycle. |
| RuntimeRootRegistry | Antes de RuntimeSpawned. |
| Surface baseline | Antes de camera/pause/presentation. |
| Capability inventory | Antes de actors/damage/attributes. |
| Snapshot/save contract | Antes de save backend. |
| Input consumer contract | Antes de player/actor commands. |

---

# Parte VII — Sequência de Próxima Ação

## 27. Próximo passo recomendado

Antes de qualquer código novo:

```text
Criar ADR/documento de reconciliação do baseline atual do package.
```

Esse ADR deve decidir explicitamente:

1. `CameraFlow`: remover/congelar/reativar.
2. `RouteContentRuntime`: conectar/remover/deferir.
3. `ContentFlow`: público estável ou experimental.
4. `RouteContentProfile`: planning-only ou execução futura.
5. `QA Canvas`: runtime dev tool ou API pública.
6. `ValidationMode`: placeholder ou contrato real.

## 28. Próximo corte técnico só depois disso

Se a decisão for seguir com base limpa, o primeiro corte técnico recomendado é:

```text
SessionContentSet mínimo
```

Justificativa:

- é top-down;
- não depende de camera;
- não depende de actor;
- prepara RuntimeSpawned futuro;
- cria paralelismo com `RouteContentSet`;
- ajuda a separar content ownership de diagnostics.

---

# Parte VIII — Sumário Final

## 29. Síntese

```text
NewScripts mostra o sistema completo e seus problemas.
Package mostra uma base menor e mais limpa, mas ainda contraditória.
A convergência correta é preservar a simplicidade do package e absorver a gramática funcional do NewScripts.
```

A direção correta não é “portar o NewScripts”.

A direção correta é:

```text
1. Corrigir baseline/documentação do package.
2. Consolidar scopes e identity.
3. Amadurecer ContentFlow de forma controlada.
4. Criar contribution e surface antes de subsistemas.
5. Integrar consumers somente depois.
```

## 30. Frase de decisão

```text
O framework deve nascer como lifecycle/content/contribution framework.
Não como camera framework, audio framework, actor framework ou clone do Base 2.0.
```
