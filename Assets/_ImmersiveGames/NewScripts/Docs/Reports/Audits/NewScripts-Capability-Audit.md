# NewScripts Capability Audit

Status: Draft consolidado com auditorias profundas do NewScripts  
Origem: Auditorias Codex isoladas sobre `NewScripts`  
Modo: Audit-only / leitura estática  
Framework alvo: `com.immersive.framework`  
Última atualização: 2026-06-21

---

## 1. Objetivo do documento

Este documento registra a auditoria funcional e arquitetural do projeto antigo `NewScripts` para orientar a evolução do `Immersive Framework`.

O objetivo não é copiar o `NewScripts`, mas extrair:

- capacidades existentes;
- escopos de lifecycle;
- padrões de materialização;
- dependências conceituais;
- riscos de acoplamento;
- elementos que devem ser preservados, redesenhados ou descartados;
- insumos para um roadmap cíclico `Session -> Route -> Activity -> Local`.

Este documento deve ser tratado como registro vivo. Cada nova auditoria deve acrescentar achados sem apagar decisões relevantes anteriores, salvo quando uma seção for explicitamente marcada como superada.

---

## 2. Status da auditoria

| Fase | Status | Observação |
|---|---:|---|
| Fase 1 — Decomposição macro do `NewScripts` | Completa preliminar | Inventário macro com 24 capacidades. |
| Fase 2 — Classificação por escopo | Parcial | Session/Route/Activity/Local/RuntimeSpawned/Surface/CrossCutting completos preliminarmente. |
| Fase 3 — Classificação por nível | Parcial | Básico/intermediário/avançado atribuído de forma macro. |
| Fase 4 — Dependências e ordem | Parcial | Requer nova rodada focada em dependências entre capacidades. |
| Fase 5 — Preservar/redesenhar/descartar | Parcial | Principais decisões registradas; faltam detalhes por capacidade. |
| Fase 6 — Plano cíclico | Preliminar | Direção `Session -> Route -> Activity -> Local` confirmada. |
| Fase 7 — Comparação com package atual | Não iniciada | Deve ser feita separadamente em workspace do package. |
| Fase 8 — Roadmap definitivo | Não iniciada | Depende das auditorias isoladas anteriores. |



### 2.1. Auditorias profundas por escopo

| Escopo | Status | Fonte | Observação |
|---|---:|---|---|
| Session | Completa preliminar | Auditoria Codex isolada em `NewScripts` | Consolidada neste documento. |
| Route | Completa preliminar | Auditoria Codex isolada em `NewScripts` | Consolidada neste documento. |
| Activity | Completa preliminar | Auditoria Codex isolada em `NewScripts` | Consolidada neste documento. |
| Local | Completa preliminar | Auditoria Codex isolada em `NewScripts` | Consolidada neste documento. |
| RuntimeSpawned | Completa preliminar | Auditoria Codex isolada em `NewScripts` | Consolidada neste documento. |
| Surface | Completa preliminar | Auditoria Codex isolada em `NewScripts` | Consolidada neste documento. |
| CrossCutting | Completa preliminar | Auditoria Codex isolada em `NewScripts` | Consolidada neste documento. |

---

## 3. Resumo executivo

A primeira auditoria confirma que o `NewScripts` tem valor funcional forte, mas não é arquitetura copiável.

O sistema antigo contém capacidades relevantes para sessão, rota, atividade, conteúdo local, runtime spawned, pooling, câmera, áudio, pause, save, input, actors, reset, projectiles, attributes/damage e diagnostics. Porém, várias dessas capacidades estão acopladas a pipelines grandes, service locator, identidades textuais, inventários específicos e estágios monolíticos.

A conclusão principal é:

```text
Preservar capacidades e decisões funcionais.
Redesenhar composição, materialização, identidade, contribution discovery e ownership.
Não copiar pipelines Base 2.0 como API pública do framework.
```

A evolução do framework deve ser top-down e cíclica:

```text
Session
↓
Route
↓
Activity
↓
Local
```

Não devemos avançar por reação a subsistemas isolados, como câmera ou áudio, antes de consolidar o modelo geral de escopos, conteúdo, materialização e contributions.

---

## 4. Achados principais

### 4.1. High — `NewScripts` tem valor funcional forte, mas não é arquitetura copiável

Evidências apontadas pela auditoria:

- `SessionOperational` normaliza Route como pipeline de troca operacional.
- `SessionActivity` aparece como sandbox funcional congelado, mas com pipeline grande e resíduos deferidos.

Decisão:

```text
O framework deve preservar capacidades, não copiar pipelines.
```

Owner futuro:

```text
Framework por escopos: Session, Route, Activity, Local.
```

---

### 4.2. High — Service locator ainda é eixo de composição global

Evidências apontadas:

- `DependencyManager` é singleton/entrypoint de composição.
- `SessionOperationalRuntimeComposer` registra e resolve muitos globais via `DependencyManager.Provider`.

Preservar:

- fail-fast;
- DIP;
- composição explícita;
- bootstrap centralizado.

Redesenhar:

- não expor service locator público;
- não transformar `DependencyManager` em API do framework;
- composition root deve ser interno/encapsulado;
- dependências obrigatórias não devem ter fallback silencioso.

---

### 4.3. High — Identidade textual ainda aparece em authoring e inventory

Evidências apontadas:

- `OperationalRouteAsset.routeIdentity`;
- `handoffSessionStateId`;
- `ActivityObjectContributor.targetId`.

Risco:

```text
string IDs espalhados viram chaves frágeis, difíceis de validar e fáceis de duplicar.
```

Ação recomendada:

```text
Criar ADR de typed references antes de materialização avançada.
```

---

### 4.4. Medium — `SessionActivityPipeline` é grande demais para servir como modelo

Evidências apontadas:

- `SessionActivityPipeline.cs` é muito grande;
- decide activation/running/pause/completion;
- contém resíduos/deferimentos;
- mistura lifecycle, readiness, content, reset, release, contributors e diagnostics.

Decisão:

```text
Extrair conceitos, não shape.
```

Conceitos a preservar:

- Activity lifecycle;
- Entry;
- Content;
- readiness;
- local contributors;
- reset/release boundary;
- snapshot/restore, quando aplicável.

---

### 4.5. Medium — Subsystems avançados já existem, mas são consumidores tardios

Subsistemas identificados:

- câmera;
- áudio;
- pause;
- save;
- input;
- actors;
- projectiles;
- damage/attributes;
- pooling.

Decisão:

```text
Não planejar por subsistema isolado agora.
Esses módulos devem consumir uma base de Session/Route/Activity/Local + ContentFlow + Contributions.
```

---

## 5. Matriz macro por escopo

| Escopo | Capacidades inventariadas | Maturidade macro | Decisão inicial |
|---|---:|---|---|
| Session | 5 | Básico/Intermediário | Preservar boot, diagnostics, participation; redesenhar composition. |
| Route | 5 | Básico/Intermediário | Preservar route lifecycle/handoff; simplificar authoring e materialização. |
| Activity | 6 | Intermediário | Preservar entry/content/readiness; não copiar pipeline. |
| Local | 3 | Intermediário | Preservar contributor/reset/snapshot/release; tipar ids. |
| RuntimeSpawned | 2 | Avançado | Preservar pool como infraestrutura; adiar projectile. |
| CrossCutting/Surface | 3 | Avançado | Audio/camera/pause/save/input entram como consumidores. |

Total macro inicial: 24 capacidades.

---

## 6. Capacidades macro inventariadas

> Esta seção é uma consolidação inicial. Rodadas futuras devem detalhar cada capacidade com arquivos/classes, dependências, nível, riscos e decisões.

### 6.1. Session

| Capacidade | Nível inicial | Papel funcional | Decisão |
|---|---|---|---|
| Bootstrap operacional | Básico | Iniciar runtime e validar configuração inicial. | Preservar conceito; redesenhar composition. |
| Diagnostics/QA runtime | Básico | Permitir smokes e observabilidade. | Preservar, mas manter fora do core de produto. |
| Runtime composition | Intermediário | Registrar/fornecer dependências runtime. | Redesenhar sem service locator público. |
| Player/session participation | Intermediário | Manter participantes/slots/estado de sessão. | Preservar conceito; adiar implementação completa. |
| Runtime persistent objects | Intermediário | Objetos que sobrevivem a rotas. | Centralizar em runtime roots por escopo. |

### 6.2. Route

| Capacidade | Nível inicial | Papel funcional | Decisão |
|---|---|---|---|
| Route lifecycle | Básico | Entrar/sair/trocar rota. | Preservar como camada superior. |
| Scene composition | Intermediário | Carregar/unload cenas da rota. | Recriar de forma menor e explícita. |
| Handoff operacional | Intermediário | Passar estado entre rotas. | Preservar conceito; simplificar. |
| Route surfaces | Intermediário/Avançado | Superfícies de rota, como pause surface. | Adiar até Content/Surface baseline. |
| Route-level contributions | Intermediário | Câmera/áudio/UI/save de rota. | Implementar como contributions futuras. |

### 6.3. Activity

| Capacidade | Nível inicial | Papel funcional | Decisão |
|---|---|---|---|
| Activity lifecycle | Básico/Intermediário | Iniciar/trocar/limpar atividade dentro da rota. | Preservar, mas desacoplar de pipeline gigante. |
| Activity entry | Intermediário | Preparar entrada de activity. | Preservar como conceito. |
| Activity content | Intermediário | Declarar/carregar conteúdo da activity. | Redesenhar sobre ContentFlow. |
| Readiness | Intermediário | Confirmar que objetos/actors estão prontos. | Preservar depois da materialização. |
| Reset/release | Intermediário | Resetar/liberar conteúdo da activity. | Preservar com boundaries claros. |
| Pause/activity contribution | Avançado | Activity contribui conteúdo para surface da rota. | Adiar até Surface/Contribution baseline. |

### 6.4. Local

| Capacidade | Nível inicial | Papel funcional | Decisão |
|---|---|---|---|
| Local authored contributors | Intermediário | Objetos de cena oferecem capabilities. | Preservar como contribution discovery. |
| Snapshot/restore local | Intermediário | Capturar/restaurar estado local. | Redesenhar com typed ids. |
| Local reset/release endpoints | Intermediário | Objetos locais participam de reset/release. | Preservar após Content/Contribution baseline. |

### 6.5. RuntimeSpawned / Pool

| Capacidade | Nível inicial | Papel funcional | Decisão |
|---|---|---|---|
| Runtime spawned actors/projectiles | Avançado | Criar atores/objetos dinâmicos em runtime. | Adiar; depende de roots, identity e lifecycle. |
| Pooling | Avançado | Reutilização de objetos. | Preservar como infraestrutura separada. |

### 6.6. Cross-cutting / Surface / Subsystems

| Capacidade | Nível inicial | Papel funcional | Decisão |
|---|---|---|---|
| Camera | Avançado | Resolver apresentação visual por escopo. | Pausado; consumidor futuro de contributions. |
| Audio | Avançado | Listener/output, BGM, SFX. | Adiar; consumidor futuro de Session/Route/Activity. |
| Save/Input/Pause | Avançado | Persistência, comandos, pause surfaces. | Adiar até base top-down. |

---

## 7. O que preservar

Preservar como conceitos:

- lifecycle por escopos;
- route como camada superior;
- activity dentro da rota;
- materialização explícita;
- loaded sets;
- contribution discovery;
- runtime roots por escopo;
- fail-fast para dependência obrigatória;
- diagnostics/smoke;
- boundaries de reset/release;
- pause surface como route-level com conteúdo contextual de activity;
- player participation como session-scoped;
- pooling como infraestrutura separada;
- câmera/áudio como consumidores de contributions, não donos do lifecycle.

---

## 8. O que redesenhar

Redesenhar:

- `DependencyManager` como locator público;
- pipelines monolíticas;
- IDs textuais soltos;
- inventories amarrados a strings/path;
- `ActivityObjectContributor` como conceito exclusivamente de Activity;
- câmera física como conteúdo de activity/route;
- áudio preso à câmera;
- materialização por `SetActive` como modelo canônico;
- roots runtime criados isoladamente por cada subsistema;
- logs técnicos excessivos como API de validação.

---

## 9. O que não mexer agora

Não avançar agora:

- CameraFlow;
- AudioFlow;
- actor damage;
- projectile;
- pause completo;
- save complexo;
- pooling avançado;
- input/command hub completo;
- presentation avançada;
- Addressables;
- route additive execution sem plano top-down fechado.

Não transformar em API pública:

- `Operational*Stage`;
- `SessionActivityPipeline`;
- `ActivityCapabilityInventory`;
- `DependencyManager`;
- adapters específicos do MVP antigo.

---

## 10. Hipótese de arquitetura-alvo

A arquitetura-alvo deve usar ciclos descendentes:

```text
Session
↓
Route
↓
Activity
↓
Local
```

E uma gramática comum:

```text
Definition
→ Plan
→ Materialization
→ ContentSet
→ Contributions
→ Release
```

Cada escopo deve ter uma versão mínima antes de aprofundar subsistemas.

---

## 11. Próximas auditorias recomendadas

### Auditoria NewScripts 2 — Session profunda

Objetivo:

- decompor capacidades de Session;
- listar arquivos/classes;
- identificar composition, bootstrap, persistent runtime, diagnostics, participation;
- decidir preservar/redesenhar/descartar.

### Auditoria NewScripts 3 — Route profunda

Objetivo:

- decompor route lifecycle, scene composition, handoff, surfaces e route-level contributions;
- mapear dependências com Session e Activity;
- separar materialização, contribution e subsistemas.

### Auditoria NewScripts 4 — Activity profunda

Objetivo:

- decompor Activity lifecycle, entry, content profile, loaded set, readiness, reset/release;
- separar conceito de pipeline antiga.

### Auditoria NewScripts 5 — Local / Contributions profunda

Objetivo:

- decompor local contributors, snapshot, reset endpoints, actor scene discovery;
- propor modelo genérico de contributions.

### Auditoria NewScripts 6 — Subsystems consumidores

Objetivo:

- câmera, áudio, pause, save, input, actor, projectile, damage, pooling;
- mapear em que ciclo entram;
- identificar dependências obrigatórias.

---

## 12. Próximo prompt sugerido para Codex

```text
Audite somente o grupo Session do NewScripts.
Não implemente nada.
Não compare com o package atual.
Não altere arquivos.

Objetivo:
Extrair capacidades de Session com máximo detalhe funcional e arquitetural.

Para cada capacidade, registre:
- nome;
- finalidade funcional;
- arquivos/classes principais;
- tipo: lifecycle, bootstrap, composition, diagnostics, participation, persistent runtime, materialization, contribution, identity, ownership;
- dependências diretas;
- dependências conceituais;
- nível: básico/intermediário/avançado;
- o que preservar;
- o que redesenhar;
- o que descartar;
- riscos;
- ordem relativa de implementação no framework.

Foque em:
- boot operacional;
- composition root;
- DependencyManager/ServiceProvider;
- runtime persistent objects;
- diagnostics/QA;
- session participation;
- qualquer materialização ou ownership que vive acima de Route.

Entregue a resposta em Markdown, para ser incorporada ao documento Docs/Audits/NewScripts-Capability-Audit.md.
```

---

## 13. Observações de validação

Validação da rodada inicial:

```text
Somente leitura/grep estático.
Não rodou Unity.
Não rodou build.
Não rodou tests.
Não rodou playmode.
Não rodou smoke.
Nenhum arquivo foi alterado pelo Codex.
```

---

## 14. Pendências abertas

- Detalhar as 24 capacidades individualmente.
- Transformar matriz macro em inventário completo.
- Executar auditorias profundas por escopo.
- Auditar package atual em workspace separado.
- Comparar NewScripts vs package atual.
- Derivar plano cíclico definitivo.
- Derivar sequência de cortes implementáveis.
- Definir ADRs necessários antes de implementação avançada.

---

## 12. Auditorias profundas por escopo

### 12.1. Session

> Fonte: auditoria Codex isolada no workspace do `NewScripts`.  
> Modo: leitura estática / audit-only.  
> Estado: consolidado preliminar.

# Auditoria Profunda — NewScripts Session

## 1. Resumo Executivo

A `Session` em `NewScripts` não existe como um único módulo nomeado; ela emerge da composição entre `GlobalCompositionRoot`, `DependencyManager`, `RuntimeModeConfig`, `RuntimeConfigRegistry`, `RuntimePersistentScenesComposition`, `SessionOperationalRuntimeComposer`, `SessionOperationalPipeline`, `StartupRequestEmitter/StartupRouteEmitter` e `PlayerParticipationRuntime`.

O desenho tem boas decisões: boot em duas fases, validação fatal para config obrigatória, garantia de cenas persistentes antes da rota inicial, separação declarada entre `SessionOperational` e `SessionActivity`, e contratos explícitos de participação. O principal problema é que a Session está materializada como composição global estática e service locator, com muita responsabilidade acumulada no composer operacional.

Validação executada: leitura estática apenas. Nenhum arquivo foi criado ou alterado.

## 2. Capacidades Session Inventariadas

| Capacidade | Evidência | Dono correto | Classificação |
|---|---|---:|---:|
| Boot global antes da cena | `Foundation/Platform/Composition/GlobalCompositionRoot.Entry.cs` | Session | Básico |
| Config de runtime | `RuntimeModeConfig`, `GlobalCompositionRoot.BootstrapConfig.cs` | Session | Básico |
| Registry imutável de config | `RuntimeConfigRegistry`, `RuntimeConfigSnapshot` | Session | Básico |
| Pipeline de composição em fases | `CompositionPipelineStep`, `CompositionPipelineExecutor` | Session | Intermediário |
| Registro global de serviços | `DependencyManager` | Session infra, mas redesenhar | Básico |
| Política Strict/Release/degraded | `GlobalCompositionRoot.RuntimePolicy.cs`, `IDegradedModeReporter` | Session diagnostics | Intermediário |
| Cenas persistentes | `RuntimePersistentScenesComposition`, `RuntimePersistentScenesPolicyAsset` | Session, com conteúdo persistente como dado | Intermediário |
| Startup route | `StartupRequestEmitter`, `StartupRouteEmitter` | Fronteira Session -> Route | Básico |
| Composição operacional | `SessionOperationalRuntimeComposer` | Fronteira Session -> Route | Intermediário |
| Estado operacional | `SessionOperationalRuntimeState` | SessionOperational | Básico |
| Participação de jogadores | `PlayerParticipationRuntime`, `SessionParticipationContext` | Session -> Activity handoff | Intermediário |
| Hosts persistentes de subsistemas | `OperationalCameraRuntimeComposition`, descriptors de Audio/Input/Save/Camera | Session composition, subsistema executa | Intermediário |

## 3. Fluxo Macro de Session

1. `GlobalCompositionRoot.Initialize()` roda em `BeforeSceneLoad`, condicionado por `NEWSCRIPTS_MODE`.
2. O bootstrap garante logging inicial e `DependencyManager.Provider`.
3. `RuntimeModeConfig` é resolvido por cache, DI global ou `Resources.Load("RuntimeMode/RuntimeModeConfig")`.
4. `RuntimeConfigRegistry.InitializeOrFail()` valida `RuntimeConfigSetAsset` e publica snapshot global.
5. `CompositionPipelineExecutor` executa Fase 1 `Installers` e Fase 2 `Bootstraps`, ordenadas por dependências.
6. Para `Base11Sandbox`, a ordem inclui `RuntimePolicy`, `Pooling`, `Gates`, `Audio`, `Save`, `Preferences`, `InputModes`, `OperationalCameraRuntime`, `CameraPresentation`, `RuntimePersistentScenes`, `SessionOperationalRuntime` e `SceneComposition`.
7. `RuntimePersistentScenesComposition` garante cenas persistentes antes da rota inicial.
8. `StartupRequestEmitter` cria objeto `DontDestroyOnLoad` e emite `BootStartPlanRequestedEvent`.
9. `StartupRouteEmitter` recebe o evento, aguarda persistentes, resolve startup route e chama `SessionOperationalPipeline.RequestOperationalRouteAsync(...)`.
10. `SessionOperationalPipeline` conduz transição de rota; quando há atividade, o handoff passa para `SessionActivity`, que deve possuir ciclo local.

## 4. Padrões Bons a Preservar

- Fail-fast real para config obrigatória: `RuntimeConfigRegistry.ResolveConfigSetOrFail`, `RuntimePersistentScenesComposition.AwaitGuaranteedAsync`, `SessionOperationalRuntimeComposer.GetRequiredGlobal`.
- Pipeline de composição com dependências declaradas e detecção de ciclo: `CompositionPipelineExecutor.ResolvePhasePlan`.
- Separação documentada entre SessionOperational e Activity: `SessionOperational/README.md` e `SessionActivity/Pipeline/README.md`.
- Descriptors por módulo: `AudioCompositionDescriptor`, `InputModesCompositionDescriptor`, `SaveCompositionDescriptor`, `CameraPresentationCompositionDescriptor`.
- Participação como contrato explícito antes da Activity: `SessionParticipationContext`, `SessionParticipantBinding`, `PlayerSlotReservation`.
- Cenas persistentes como política validável: `RuntimePersistentScenesPolicyAsset`.

## 5. Padrões Ruins a Redesenhar ou Descartar

| Severidade | Achado | Evidência | Ação recomendada |
|---|---|---|---|
| Alta | Service locator global vaza DIP | `DependencyManager.Provider`, `TryGetGlobal<T>`, reflection injector | Preservar a intenção de composição, mas substituir acesso global por contexto explícito de Session |
| Alta | Composer operacional concentra responsabilidades demais | `SessionOperationalRuntimeComposer` cria pipeline, adapters, participation runtime, camera, save, audio, handoff | Dividir por ownership: Session root, Route runtime composition, adapters de subsistema |
| Alta | Identidade misturada por `string` | `SessionOperationalRuntimeState` usa `RouteIdentity`, `RouteOperationId`, `TransitionId`, `RouteId`; `SessionParticipationContext` usa `SessionId`, `RouteIdentity`, `RouteOperationId` | Criar tipos de identidade por domínio no futuro; não comparar strings entre domínios |
| Média | Fallback de config via `Resources.Load` reduz explicitude | `GlobalCompositionRoot.BootstrapConfig.cs` | Framework futuro deve ter fonte canônica explícita e erro claro quando ausente |
| Média | Startup por `MonoBehaviour` global e evento estático | `StartupRequestEmitter`, `StartupRouteEmitter`, `EventBus<BootStartPlanRequestedEvent>` | Preservar o conceito de start signal, mas tornar o trigger parte do Session runtime explícito |
| Média | Perfil `Base11Sandbox` domina a topologia | `CompositionProfileKind.Base11Sandbox`, `GlobalCompositionRoot.CompositionGraph.cs` | Não levar o nome/shape para framework público |
| Média | Conteúdo persistente e composição de serviço aparecem juntos | `RuntimePersistentSceneRole.ServiceHost`, `GlobalUi`, `Fade`, `Loading` | Separar “Session persistent content” de “service host” como conceitos distintos |

## 6. Classificação Básico / Intermediário / Avançado

**Básico:** boot global, config obrigatória, runtime policy, registro de serviços, rota inicial, estado operacional mínimo.

**Intermediário:** pipeline de composição ordenado, cenas persistentes garantidas, participação de jogadores por contexto/revisão, descriptors modulares, adapters de câmera/audio/save/input.

**Avançado:** policy-driven Session runtime com diagnóstico formal, identidade tipada, lifecycle de sessão independente de Unity static state, validação de graph antes do runtime, telemetry de fatos/trace como contrato público. Em `NewScripts`, estes aparecem parcialmente, mas ainda presos ao service locator e ao composer monolítico.

## 7. Matriz Session

| Área | Status em NewScripts | Preservar | Redesenhar |
|---|---|---|---|
| Bootstrap | Funcional e fail-fast | Ordem explícita | `RuntimeInitialize` + static globals como API principal |
| Config | Forte validação | `RuntimeConfigSetAsset`/snapshot | `Resources.Load` como fallback canônico |
| DI/composição | Centralizada | Registro controlado no boot | `DependencyManager` público como caminho normal |
| Runtime persistente | Bem identificado | Policy asset e garantia antes da rota | Mistura de cena persistente, UI, loading e service host |
| SessionOperational | Rico em capacidades | Pipeline e stages determinísticos | Composer acumulando adapters e ownership |
| Participation | Bom domínio nascente | Contexto de participação e revisão | IDs em string sem tipos fortes |
| Route/Activity boundary | Bem documentado | SessionOperational decide rota; Activity roda local | Session conhecer detalhes demais de adapters de Activity |
| Diagnostics | Presente | Fatal logs e trace/facts | Diagnóstico espalhado por classes estáticas |

## 8. Dependências e Ordem Recomendada

Ordem conceitual correta para uma Session futura:

1. `Session Settings` e fonte explícita de config.
2. `Session Diagnostics` e política Strict/Release.
3. `Session Composition Graph` validado antes de executar.
4. `Session Service Context` explícito, sem service locator público.
5. `Persistent Content Policy`.
6. `Subsystem Hosts`: input, audio, save, preferences, camera.
7. `Player Participation`.
8. `Route Runtime`.
9. `Activity Handoff`.
10. `Session Facts/Trace`.

A ordem atual em `CompositionGraph` está próxima, mas a dependência real fica obscurecida porque muitos componentes resolvem globais dentro de métodos estáticos.

## 9. Recomendações para Framework Futuro

- Tratar `Session` como boundary acima de `Route` e `Activity`, não como sinônimo de `SessionOperational`.
- Preservar o conceito de graph de composição em fases, mas trocar `DependencyManager` por um `SessionRuntimeContext` explícito.
- Manter fail-fast para config obrigatória; não introduzir fallback silencioso.
- Não portar `Base11Sandbox`, `SessionOperationalRuntimeComposer` ou `DependencyManager` como nomes públicos.
- Transformar identidades em tipos: `SessionId`, `RouteId`, `RouteOperationId`, `TransitionId`, `PlayerSlotId`.
- Separar “conteúdo persistente” de “serviços globais”.
- Manter `PlayerParticipation` como capacidade Session-level, antes da Activity.
- Não mudar agora: `com.immersive.foundation`, `com.immersive.logging`, `com.immersive.pooling`; esta auditoria é só de referência NewScripts.

Checklist manual futuro: abrir Unity, importar assets, validar `RuntimeModeConfig`, validar cenas persistentes em Build Settings, iniciar Play Mode e confirmar ordem de boot/logs. Não executado nesta auditoria.

## 10. Perguntas Abertas para Próxima Auditoria

- `Session` deve expor publicamente “Scene Lifetime”, “Route Runtime” ou apenas “Session Runtime”?
- O framework futuro terá uma única Session ativa por processo?
- Participação pertence ao core da Session ou a um módulo interno opcional?
- Câmera, audio listener e loading UI são capacidades de Session ou exemplos de persistent content?
- A rota inicial deve nascer de config, de cena de boot, ou de chamada explícita de API?
- `Facts/Trace` serão diagnóstico interno ou API observável?

## 11. Itens que Devem Virar ADR

- ADR: definição pública de `Session`, `Route` e `Activity`.
- ADR: fonte canônica de config e política de fail-fast.
- ADR: substituição de service locator por contexto explícito.
- ADR: identidade tipada por domínio.
- ADR: política de cenas/conteúdo persistente.
- ADR: participação de jogadores como capacidade Session-level.
- ADR: fronteira entre SessionOperational e SessionActivity.
- ADR: composição de subsistemas globais sem managers genéricos.
- ADR: diagnostics/facts/trace como contrato interno ou público.

---

## 13. Auditoria Profunda — Route

> Fonte: auditoria Codex isolada no workspace `NewScripts`.  
> Modo: audit-only / leitura estática.  
> Status: completa preliminar.  
> Observação: esta seção não compara com `com.immersive.framework`.

[Immersive Framework] Boot succeeded. Application Runtime started. Game Application 'Game Application' resolved. Startup Route 'QA Canonical Route' resolved. Primary Scene 'StartupScene' declared. Game Flow started with Startup Route 'QA Canonical Route'. Route Lifecycle started Route 'QA Canonical Route'. Scene Lifecycle resolved Primary Scene 'StartupScene' and set it active. alreadyLoaded='True'. loadMode='AlreadyLoaded'. Activity Flow started Activity 'QA Primary Content Activity'. Activity Content applied 1 binding(s) for Activity 'QA Primary Content Activity'. activated='0' deactivated='1' unchanged='0'. Validation Mode: Standard.
UnityEngine.Debug:Log (object)
Immersive.Logging.Unity.UnityConsoleLogSink:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Unity/UnityConsoleLogSink.cs:38)
Immersive.Logging.Loggers.Logger:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:62)
Immersive.Logging.Loggers.Logger:Info (string,string,string) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:32)
Immersive.Framework.Diagnostics.FrameworkLogger:Info (string) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkLogger.cs:30)
Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap/<BootAfterSceneLoad>d__0:MoveNext () (at ./Packages/com.immersive.framework/Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs:50)
System.Runtime.CompilerServices.AsyncVoidMethodBuilder:Start<Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap/<BootAfterSceneLoad>d__0> (Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap/<BootAfterSceneLoad>d__0&)
Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap:BootAfterSceneLoad ()

[Immersive Framework] Activity Content Binding diagnostics. activeActivity='QA Primary Content Activity' observations=[object='Activity Content Binding' scene='StartupScene' assignedActivity='QA Secondary Content Activity' action='Deactivate' reason='DifferentActivity'].
UnityEngine.Debug:Log (object)
Immersive.Logging.Unity.UnityConsoleLogSink:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Unity/UnityConsoleLogSink.cs:38)
Immersive.Logging.Loggers.Logger:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:62)
Immersive.Logging.Loggers.Logger:Info (string,string,string) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:32)
Immersive.Framework.Diagnostics.FrameworkLogger:Info (string) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkLogger.cs:30)
Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap:LogActivityContentObservability (Immersive.Framework.Diagnostics.FrameworkLogger,Immersive.Framework.ActivityFlow.ActivityContentApplyResult) (at ./Packages/com.immersive.framework/Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs:68)
Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap/<BootAfterSceneLoad>d__0:MoveNext () (at ./Packages/com.immersive.framework/Runtime/Bootstrap/ImmersiveFrameworkBootstrap.cs:51)
System.Runtime.CompilerServices.AsyncVoidMethodBuilder:Start<Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap/<BootAfterSceneLoad>d__0> (Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap/<BootAfterSceneLoad>d__0&)
Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap:BootAfterSceneLoad ()

[Immersive Framework] Route Request completed. source='FrameworkQaCanvas' reason='qa.route'. Route Lifecycle switched from Route 'QA Canonical Route' to Route 'QA Alternate Route'. Scene Lifecycle resolved Primary Scene 'SecoundScene' and set it active. alreadyLoaded='False'. loadMode='Single'. Activity Flow switched from Activity 'QA Primary Content Activity' to Activity 'QA Secondary Content Activity'.
UnityEngine.Debug:Log (object)
Immersive.Logging.Unity.UnityConsoleLogSink:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Unity/UnityConsoleLogSink.cs:38)
Immersive.Logging.Loggers.Logger:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:62)
Immersive.Logging.Loggers.Logger:Info (string,string,string) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:32)
Immersive.Framework.Diagnostics.FrameworkLogger:Info (string) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkLogger.cs:30)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:LogRouteRequestResult (Immersive.Framework.GameFlow.FrameworkRouteRequestResult) (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:117)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestRouteAsync>d__12:MoveNext () (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:74)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.GameFlow.FrameworkRouteRequestResult>:SetResult (Immersive.Framework.GameFlow.FrameworkRouteRequestResult)
Immersive.Framework.GameFlow.GameFlowRuntime/<RequestRouteAsync>d__4:MoveNext () (at ./Packages/com.immersive.framework/Runtime/GameFlow/GameFlowRuntime.cs:108)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.RouteLifecycle.RouteLifecycleStartResult>:SetResult (Immersive.Framework.RouteLifecycle.RouteLifecycleStartResult)
Immersive.Framework.RouteLifecycle.RouteLifecycleRuntime/<StartRouteAsync>d__18:MoveNext () (at ./Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs:96)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult>:SetResult (Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult)
Immersive.Framework.SceneLifecycle.SceneLifecycleRuntime/<LoadPrimarySceneAsync>d__2:MoveNext () (at ./Packages/com.immersive.framework/Runtime/SceneLifecycle/SceneLifecycleRuntime.cs:79)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult>:SetResult (Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult)
Immersive.Framework.SceneLifecycle.SceneLifecycleRuntime/<TryLoadSceneSingleAsync>d__3:MoveNext () (at ./Packages/com.immersive.framework/Runtime/SceneLifecycle/SceneLifecycleRuntime.cs:110)
UnityEngine.UnitySynchronizationContext:ExecuteTasks ()

[Immersive Framework] Route Request completed. source='FrameworkQaCanvas' reason='qa.route'. Route Lifecycle switched from Route 'QA Alternate Route' to Route 'QA Canonical Route'. Scene Lifecycle resolved Primary Scene 'StartupScene' and set it active. alreadyLoaded='False'. loadMode='Single'. Activity Flow switched from Activity 'QA Secondary Content Activity' to Activity 'QA Primary Content Activity'. Activity Content applied 1 binding(s) for Activity 'QA Primary Content Activity'. activated='0' deactivated='1' unchanged='0'.
UnityEngine.Debug:Log (object)
Immersive.Logging.Unity.UnityConsoleLogSink:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Unity/UnityConsoleLogSink.cs:38)
Immersive.Logging.Loggers.Logger:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:62)
Immersive.Logging.Loggers.Logger:Info (string,string,string) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:32)
Immersive.Framework.Diagnostics.FrameworkLogger:Info (string) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkLogger.cs:30)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:LogRouteRequestResult (Immersive.Framework.GameFlow.FrameworkRouteRequestResult) (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:117)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestRouteAsync>d__12:MoveNext () (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:74)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.GameFlow.FrameworkRouteRequestResult>:SetResult (Immersive.Framework.GameFlow.FrameworkRouteRequestResult)
Immersive.Framework.GameFlow.GameFlowRuntime/<RequestRouteAsync>d__4:MoveNext () (at ./Packages/com.immersive.framework/Runtime/GameFlow/GameFlowRuntime.cs:108)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.RouteLifecycle.RouteLifecycleStartResult>:SetResult (Immersive.Framework.RouteLifecycle.RouteLifecycleStartResult)
Immersive.Framework.RouteLifecycle.RouteLifecycleRuntime/<StartRouteAsync>d__18:MoveNext () (at ./Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs:96)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult>:SetResult (Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult)
Immersive.Framework.SceneLifecycle.SceneLifecycleRuntime/<LoadPrimarySceneAsync>d__2:MoveNext () (at ./Packages/com.immersive.framework/Runtime/SceneLifecycle/SceneLifecycleRuntime.cs:79)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult>:SetResult (Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult)
Immersive.Framework.SceneLifecycle.SceneLifecycleRuntime/<TryLoadSceneSingleAsync>d__3:MoveNext () (at ./Packages/com.immersive.framework/Runtime/SceneLifecycle/SceneLifecycleRuntime.cs:110)
UnityEngine.UnitySynchronizationContext:ExecuteTasks ()

[Immersive Framework] Activity Content Binding diagnostics. activeActivity='QA Primary Content Activity' observations=[object='Activity Content Binding' scene='StartupScene' assignedActivity='QA Secondary Content Activity' action='Deactivate' reason='DifferentActivity'].
UnityEngine.Debug:Log (object)
Immersive.Logging.Unity.UnityConsoleLogSink:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Unity/UnityConsoleLogSink.cs:38)
Immersive.Logging.Loggers.Logger:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:62)
Immersive.Logging.Loggers.Logger:Info (string,string,string) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:32)
Immersive.Framework.Diagnostics.FrameworkLogger:Info (string) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkLogger.cs:30)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:LogActivityContentObservability (Immersive.Framework.ActivityFlow.ActivityContentApplyResult) (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:156)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:LogRouteRequestResult (Immersive.Framework.GameFlow.FrameworkRouteRequestResult) (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:118)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestRouteAsync>d__12:MoveNext () (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:74)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.GameFlow.FrameworkRouteRequestResult>:SetResult (Immersive.Framework.GameFlow.FrameworkRouteRequestResult)
Immersive.Framework.GameFlow.GameFlowRuntime/<RequestRouteAsync>d__4:MoveNext () (at ./Packages/com.immersive.framework/Runtime/GameFlow/GameFlowRuntime.cs:108)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.RouteLifecycle.RouteLifecycleStartResult>:SetResult (Immersive.Framework.RouteLifecycle.RouteLifecycleStartResult)
Immersive.Framework.RouteLifecycle.RouteLifecycleRuntime/<StartRouteAsync>d__18:MoveNext () (at ./Packages/com.immersive.framework/Runtime/RouteLifecycle/RouteLifecycleRuntime.cs:96)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult>:SetResult (Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult)
Immersive.Framework.SceneLifecycle.SceneLifecycleRuntime/<LoadPrimarySceneAsync>d__2:MoveNext () (at ./Packages/com.immersive.framework/Runtime/SceneLifecycle/SceneLifecycleRuntime.cs:79)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult>:SetResult (Immersive.Framework.SceneLifecycle.SceneLifecycleLoadResult)
Immersive.Framework.SceneLifecycle.SceneLifecycleRuntime/<TryLoadSceneSingleAsync>d__3:MoveNext () (at ./Packages/com.immersive.framework/Runtime/SceneLifecycle/SceneLifecycleRuntime.cs:110)
UnityEngine.UnitySynchronizationContext:ExecuteTasks ()

[Immersive Framework] Activity Request ignored. source='FrameworkQaCanvas' reason='qa.activity'. Activity 'QA Primary Content Activity' is already active.
UnityEngine.Debug:LogWarning (object)
Immersive.Logging.Unity.UnityConsoleLogSink:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Unity/UnityConsoleLogSink.cs:31)
Immersive.Logging.Loggers.Logger:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:62)
Immersive.Logging.Loggers.Logger:Warning (string,string,string) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:37)
Immersive.Framework.Diagnostics.FrameworkLogger:Warning (string) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkLogger.cs:35)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:LogActivityRequestResult (Immersive.Framework.GameFlow.FrameworkActivityRequestResult) (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:145)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestActivityAsync>d__13:MoveNext () (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:89)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.GameFlow.FrameworkActivityRequestResult>:Start<Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestActivityAsync>d__13> (Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestActivityAsync>d__13&)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:RequestActivityAsync (Immersive.Framework.Authoring.ActivityAsset,string,string)
Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivityCoreAsync>d__48:MoveNext () (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:655)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<bool>:Start<Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivityCoreAsync>d__48> (Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivityCoreAsync>d__48&)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:RequestActivityCoreAsync (Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost,Immersive.Framework.Authoring.ActivityAsset,string,bool)
Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivity>d__37:MoveNext () (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:371)
System.Runtime.CompilerServices.AsyncVoidMethodBuilder:Start<Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivity>d__37> (Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivity>d__37&)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:RequestActivity (Immersive.Framework.Authoring.ActivityAsset)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:DrawActivityRequests () (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:298)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:DrawWindow (int) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:135)
UnityEngine.GUI:CallWindowDelegate (UnityEngine.GUI/WindowFunction,int,UnityEngine.EntityId,UnityEngine.GUISkin,int,single,single,UnityEngine.GUIStyle)

[Immersive Framework] Activity Request completed. source='FrameworkQaCanvas' reason='qa.activity'. Activity Flow switched from Activity 'QA Primary Content Activity' to Activity 'QA Secondary Content Activity'. Activity Content applied 1 binding(s) for Activity 'QA Secondary Content Activity'. activated='1' deactivated='0' unchanged='0'.
UnityEngine.Debug:Log (object)
Immersive.Logging.Unity.UnityConsoleLogSink:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Unity/UnityConsoleLogSink.cs:38)
Immersive.Logging.Loggers.Logger:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:62)
Immersive.Logging.Loggers.Logger:Info (string,string,string) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:32)
Immersive.Framework.Diagnostics.FrameworkLogger:Info (string) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkLogger.cs:30)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:LogActivityRequestResult (Immersive.Framework.GameFlow.FrameworkActivityRequestResult) (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:136)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestActivityAsync>d__13:MoveNext () (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:89)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.GameFlow.FrameworkActivityRequestResult>:Start<Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestActivityAsync>d__13> (Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestActivityAsync>d__13&)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:RequestActivityAsync (Immersive.Framework.Authoring.ActivityAsset,string,string)
Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivityCoreAsync>d__48:MoveNext () (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:655)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<bool>:Start<Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivityCoreAsync>d__48> (Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivityCoreAsync>d__48&)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:RequestActivityCoreAsync (Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost,Immersive.Framework.Authoring.ActivityAsset,string,bool)
Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivity>d__37:MoveNext () (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:371)
System.Runtime.CompilerServices.AsyncVoidMethodBuilder:Start<Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivity>d__37> (Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivity>d__37&)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:RequestActivity (Immersive.Framework.Authoring.ActivityAsset)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:DrawActivityRequests () (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:298)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:DrawWindow (int) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:135)
UnityEngine.GUI:CallWindowDelegate (UnityEngine.GUI/WindowFunction,int,UnityEngine.EntityId,UnityEngine.GUISkin,int,single,single,UnityEngine.GUIStyle)

[Immersive Framework] Activity Content Binding diagnostics. activeActivity='QA Secondary Content Activity' observations=[object='Activity Content Binding' scene='StartupScene' assignedActivity='QA Secondary Content Activity' action='Activate' reason='MatchedActiveActivity'].
UnityEngine.Debug:Log (object)
Immersive.Logging.Unity.UnityConsoleLogSink:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Unity/UnityConsoleLogSink.cs:38)
Immersive.Logging.Loggers.Logger:Write (Immersive.Logging.Records.LogRecord) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:62)
Immersive.Logging.Loggers.Logger:Info (string,string,string) (at ./Library/PackageCache/com.immersive.logging@1fa88443baeb/Runtime/Loggers/Logger.cs:32)
Immersive.Framework.Diagnostics.FrameworkLogger:Info (string) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkLogger.cs:30)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:LogActivityContentObservability (Immersive.Framework.ActivityFlow.ActivityContentApplyResult) (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:156)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:LogActivityRequestResult (Immersive.Framework.GameFlow.FrameworkActivityRequestResult) (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:137)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestActivityAsync>d__13:MoveNext () (at ./Packages/com.immersive.framework/Runtime/ApplicationLifecycle/FrameworkRuntimeHost.cs:89)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<Immersive.Framework.GameFlow.FrameworkActivityRequestResult>:Start<Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestActivityAsync>d__13> (Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost/<RequestActivityAsync>d__13&)
Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost:RequestActivityAsync (Immersive.Framework.Authoring.ActivityAsset,string,string)
Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivityCoreAsync>d__48:MoveNext () (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:655)
System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<bool>:Start<Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivityCoreAsync>d__48> (Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivityCoreAsync>d__48&)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:RequestActivityCoreAsync (Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost,Immersive.Framework.Authoring.ActivityAsset,string,bool)
Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivity>d__37:MoveNext () (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:371)
System.Runtime.CompilerServices.AsyncVoidMethodBuilder:Start<Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivity>d__37> (Immersive.Framework.Diagnostics.FrameworkQaCanvas/<RequestActivity>d__37&)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:RequestActivity (Immersive.Framework.Authoring.ActivityAsset)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:DrawActivityRequests () (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:298)
Immersive.Framework.Diagnostics.FrameworkQaCanvas:DrawWindow (int) (at ./Packages/com.immersive.framework/Runtime/Diagnostics/FrameworkQaCanvas.cs:135)
UnityEngine.GUI:CallWindowDelegate (UnityEngine.GUI/WindowFunction,int,UnityEngine.EntityId,UnityEngine.GUISkin,int,single,single,UnityEngine.GUIStyle)

---

## 14. Auditoria Profunda — Activity

> Fonte: auditoria Codex isolada no workspace do `NewScripts`.  
> Modo: leitura estática / audit-only.  
> Estado: consolidado preliminar.

# Auditoria Profunda — NewScripts Activity

## 1. Resumo Executivo

`Activity` em `NewScripts` é uma unidade de execução dentro de uma `Route`, materializada por três blocos principais:

- `SessionActivityPipeline`: orquestra o ciclo macro da Activity.
- `ActivityEntryPipeline`: prepara entrada, conteúdo, participantes, objetos locais, readiness e bindings.
- Assets/contratos de Activity: `ActivityAsset`, `ActivityContentProfileAsset`, contributors, inventories, reset/snapshot/release.

O modelo tem bons conceitos para preservar como inventário: Activity declarativa, perfil de conteúdo, loaded set, handoff explícito da Route, contributors locais, inventory de setup/capabilities, readiness antes de gameplay e contratos de reset/snapshot/release.

O que não deve ser copiado para o framework futuro é a forma atual: pipelines grandes demais, muitos bridges, identidade baseada em strings compostas, `ActivityEntryPipeline` conhecendo actor/input/movement/camera/pause/reset/save, e `ActivityAsset` acumulando conteúdo, pause, transition e next activity.

## 2. Capacidades Activity Inventariadas

| Capacidade | Evidência | Owner atual | Avaliação |
|---|---|---:|---|
| Definição declarativa de Activity | `SessionActivity/Authoring/ActivityAsset.cs` | Activity authoring | Preservar o conceito, reduzir escopo público |
| Perfil de conteúdo | `ActivityContentProfileAsset.cs`, `ActivityContentSceneEntry.cs` | Activity content | Preservar como `ActivityContentProfile` |
| Identidade/ciclo | `SessionActivityIdentity`, `SessionActivityCycleKey` | Activity runtime | Redesenhar com IDs tipados |
| Handoff Route → Activity | `SessionActivityEntryHandoff`, `StartFromPreparedHandoff` | Route/Activity boundary | Preservar boundary explícito |
| Entry pipeline | `ActivityEntryPipeline.cs` | Activity entry | Preservar ordem conceitual, não a classe |
| Load/unload de cenas | `ActivityContentLoadContracts.cs`, `UnityActivityContentSceneAdapter.cs` | Activity content + Unity adapter | Preservar plano/resultado; separar handles |
| Contributors locais | `ActivityObjectContributor.cs` | Activity → Local | Preservar contributor explícito |
| Discovery local | `ActivityEntryObjectContributorDiscoveryStage.cs` | Activity local discovery | Preservar, mas evitar identity por path/string |
| Setup inventory | `ActivitySetupInventoryBuilder.cs` | Activity setup | Preservar inventory, reduzir mistura de concerns |
| Capability inventory | `ActivityObjectCapabilityScanner.cs` | Activity capabilities | Preservar como contribuição local |
| Participant binding | `ActivityEntryParticipantBindingStage` | Activity/Actor boundary | Redesenhar ownership |
| Reset/snapshot/restore/release | `ActivityObjectResetContracts.cs`, `ActivityObjectSnapshotContracts.cs`, `ActivityObjectReleaseContracts.cs` | Activity local lifecycle | Preservar contratos conceituais |
| Readiness | `ActivityParticipantReadinessStage`, `ISessionActivityVisualReadinessBoundary` | Activity readiness | Preservar como gate explícito |
| Pause content/surface | `ActivityPauseContentRuntimeState`, `ActivityEntryPauseContentStage` | Activity/Route UI boundary | Reavaliar owner |
| Diagnostics/facts | `SessionActivityFact`, `SessionActivitySnapshot`, `SessionActivityStage` | Activity diagnostics | Manter interno; não virar API pública |

## 3. Fluxo Macro de Activity

1. `Route` entrega um `SessionActivityEntryHandoff`.
2. `SessionActivityPipeline.StartFromPreparedHandoff(...)` valida sessão, estado e definição inicial.
3. A pipeline resolve `ActivityAsset` para uma `SessionActivityDefinition`.
4. Cria identidade de ciclo com `SessionActivityIdentity`.
5. `ActivityEntryPipeline.PrepareEntry(...)` limpa estado anterior e prepara inventários.
6. O conteúdo da Activity é planejado a partir de `ActivityContentProfileAsset`.
7. Cenas são carregadas via `IActivityContentSceneAdapter`.
8. O resultado vira `ActivityContentLoadedSet`.
9. Contributors locais são descobertos nas cenas carregadas.
10. O setup inventory é construído.
11. Participantes/actors são vinculados ou materializados.
12. Capabilities locais são escaneadas.
13. Reset/restore, presentation, attributes, input, movement, camera e permissions são aplicados.
14. Readiness é emitido.
15. Activity roda.
16. Completion/route exit aciona snapshot, release, unload e teardown.

O fluxo macro é útil como referência, mas hoje está concentrado em classes grandes demais.

## 4. Materialização de Activity

| Item materializado | Como ocorre hoje | Risco |
|---|---|---|
| Conteúdo de cena | `UnityActivityContentSceneAdapter` carrega cenas additive | Forte acoplamento a Unity scene como única forma de conteúdo |
| Loaded set | `ActivityContentLoadedSet` registra cenas carregadas | Bom conceito, precisa handles mais fortes |
| Objetos locais | `ActivityObjectContributor` em cenas carregadas | Discovery por scan de cena |
| Capabilities locais | `ActivityObjectCapabilityScanner` coleta providers | IDs e paths frágeis |
| Participantes/actors | Entry pipeline usa adapters/registries de actor | Activity conhece demais sobre Actor |
| Pause content | `ActivityEntryPauseContentStage` | Boundary Route/Activity/UI misturado |
| Reset/restore/release | Endpoints e contribution providers | Bom conceito, API atual extensa |
| Snapshot | `ActivityObjectSnapshot` | Snapshot transform-only é limitado |

## 5. Relação Activity → Route

`Route` prepara e entrega contexto para `Activity` por handoff. A Activity assume a responsabilidade de:

- resolver definição atual;
- carregar conteúdo;
- preparar participantes;
- preparar objetos locais;
- emitir readiness;
- executar teardown no fim ou route exit.

Boa decisão: existe um boundary explícito de handoff.

Problema: o handoff e a pipeline carregam contexto demais, incluindo surface de pause, transition, loaded snapshot payload, actor materialization e route exit teardown. Para o framework futuro, `Route` deveria entregar intenção e contexto mínimo; `Activity` deveria devolver readiness, snapshot/exit result e status de lifecycle.

## 6. Relação Activity → Local

O Local aparece por `ActivityObjectContributor`.

Hoje o vínculo funciona assim:

- cena de conteúdo é carregada;
- root objects são varridos;
- `ActivityObjectContributor` é localizado;
- contributor declara `targetId`, `roleId`, kind, requiredness e release/reset eligibility;
- behaviours implementando providers expõem reset/snapshot/restore/release;
- scanners criam descriptors e runtime references.

Conceito a preservar: `Activity` não deve conhecer detalhes de cada objeto local; deve consumir contribuições declaradas.

Conceito a redesenhar: identidade local não deve depender de `targetId` string, transform path ou component path como contrato funcional. Esses dados devem ser diagnósticos, não identidade canônica.

## 7. Padrões Bons a Preservar

- `ActivityAsset` como definição declarativa.
- `ActivityContentProfileAsset` separado da Activity.
- `ActivityContentMode.None/Profile`.
- `ActivityContentLoadedSet`.
- Handoff explícito Route → Activity.
- Entry preparation antes de setup/readiness.
- Contributors locais explícitos.
- Setup inventory.
- Capability inventory.
- Requiredness explícita.
- Readiness antes de gameplay.
- Contratos de reset/snapshot/restore/release.
- Separação entre contratos puros e adapters Unity.
- Validação fail-fast em assets.

## 8. Padrões Ruins a Redesenhar ou Descartar

- `SessionActivityPipeline` como macro-orquestrador gigante.
- `ActivityEntryPipeline` acumulando setup, actors, input, movement, camera, permission, pause e reset.
- Bridges muito largos como superfície de integração.
- Identidade baseada em strings compostas.
- `CycleSignature` por interpolação de strings.
- Transform path/component path como identidade funcional.
- `ActivityAsset` conhecendo pause, transition e next activity.
- Requiredness espalhada entre content scene, setup requirement, contributor e capability.
- Logs/facts/stage enum virando contrato implícito.
- Criação de dependências concretas dentro da pipeline.
- Comentários de “ponte transitória” indicando migração incompleta.

## 9. Classificação Básico / Intermediário / Avançado

**Básico**

- `ActivityAsset`
- `ActivityIdentity`
- `ActivityEntryContext`
- `ActivityContentProfile`
- `ActivityContentPlan`
- `ActivityContentLoadedSet`
- `ActivityRuntimeState`
- readiness mínima

**Intermediário**

- Local contributors
- setup inventory
- capability inventory
- participant binding
- reset/restore local
- content unload
- snapshot capture
- release plan
- route exit result

**Avançado**

- actor materialization
- player input binding
- movement binding
- camera binding
- permissions
- pause surface
- save/progression snapshots
- rich diagnostics/facts
- multi-activity sequencing

## 10. Matriz Activity

| Área | Básico | Intermediário | Avançado |
|---|---|---|---|
| Definição | `ActivityAsset` mínimo | content/profile policies | sequencing/transition profile |
| Conteúdo | content plan | loaded set/unload | streaming/addressables |
| Local | contributor marker | capability inventory | dynamic contribution graph |
| Participantes | participant set | actor binding | actor presentation/attributes |
| Readiness | required content ready | participant/local ready | visual/readiness diagnostics |
| Lifecycle | enter/run/exit | reset/snapshot/release | save/progression integration |
| Route boundary | handoff/result | route exit teardown | multi-route policies |
| Diagnostics | simple status | facts/snapshots | tooling/validators |

## 11. Dependências e Ordem Recomendada

1. Definir `ActivityIdentity` tipado.
2. Definir `ActivityAsset` mínimo.
3. Definir `ActivityEntryContext`.
4. Definir `ActivityContentProfile`.
5. Definir `ActivityContentPlan`.
6. Definir `ActivityContentLoadedSet`.
7. Definir `ActivityRuntimeState`.
8. Definir `ActivityLocalContribution`.
9. Definir `ActivitySetupInventory`.
10. Definir `ActivityParticipantSet`.
11. Definir `ActivityReadinessState`.
12. Definir reset/restore local.
13. Definir snapshot.
14. Definir release/exit.
15. Só depois integrar camera, input, movement, audio, save e pause como contribuições.

## 12. Recomendações para Framework Futuro

Criar conceitos públicos pequenos:

- `ActivityAsset`
- `ActivityId`
- `ActivityEntryContext`
- `ActivityRuntimeState`
- `ActivityContentProfile`
- `ActivityContentPlan`
- `ActivityContentSet`
- `ActivityContributionSet`
- `ActivityParticipantSet`
- `ActivityReadinessState`
- `ActivitySnapshotSet`
- `ActivityExitPlan`
- `ActivityExitResult`

Não portar diretamente:

- `SessionActivityPipeline`
- `ActivityEntryPipeline`
- bridge adapters atuais
- stage enum gigante
- identidade por string/path
- setup monolítico
- route exit teardown misturado com entry setup

Modelo recomendado: `Activity` deve ser dona do ciclo de execução da atividade; `Route` deve ser dona de navegação/entrada/saída macro; `Local` deve declarar contribuições; subsistemas como camera/input/movement/audio/save devem entrar por contratos específicos, não por conhecimento direto da pipeline.

## 13. Perguntas Abertas para Próxima Auditoria

- Uma `Activity` sempre pertence a uma única `Route`?
- `ActivityContent` precisa ser sempre cena Unity ou pode ser prefab/addressable/content set?
- Quem deve materializar actor: Activity, Actor module ou Route?
- Qual é a identidade estável de um objeto local?
- Qual é o readiness mínimo para liberar gameplay?
- Pause surface pertence a Activity, Route ou UI/Game Flow?
- Snapshot deve ser transform-only ou estado arbitrário versionado?
- Release precisa ser síncrono, assíncrono ou planejado?
- `nextActivity` é responsabilidade de Activity ou de futuro Game Flow?
- Quais facts são diagnóstico interno e quais viram contrato público?

## 14. Itens que Devem Virar ADR

- Boundary `Route → Activity → Local`.
- Modelo de identidade tipada para Activity.
- Modelo de `ActivityContentProfile`, `ActivityContentPlan` e `ActivityContentSet`.
- Modelo de contribuição local.
- Regras de discovery e identidade local.
- Ownership de participant/actor materialization.
- Contrato de readiness.
- Contrato de reset/snapshot/restore/release.
- Modelo de Activity exit e route exit.
- Política de diagnostics/facts.
- Política para integrações avançadas: camera, input, movement, audio, save, pause.

Nenhum arquivo foi modificado.

---

## 15. Auditoria Profunda — Local

### 1. Resumo Executivo

`Local` em `NewScripts` aparece como objetos authored em cenas/prefabs que expõem capacidades para `Activity`, `Route` ou consumidores específicos por meio de markers, endpoints, providers, slots, anchors e surfaces.

O núcleo mais claro está em `SessionActivity`:

- `ActivityObjectContributor` declara um objeto local relevante para uma Activity.
- `ActivityEntryObjectContributorDiscoveryStage` encontra contributors nas cenas de conteúdo carregadas.
- `ActivityObjectCapabilityScanner` converte contributors em capabilities e runtime references.
- Stages de reset, snapshot, restore, release e unregister executam lifecycle local usando esse inventory.

Há bons conceitos para preservar: contributor marker explícito, discovery por escopo, inventory antes da execução, requiredness local, runtime references, lifecycle por componente e fail-fast quando capability obrigatória falta.

O que deve ser redesenhado: identidade local baseada em `targetId`, `roleId`, scene name, transform path, component path e strings derivadas. Esses dados são úteis para diagnóstico, mas frágeis como chave funcional.

### 2. Capacidades Local Inventariadas

| Capacidade | O que permite | Arquivos/classes | Tipo | Escopo | Avaliação |
|---|---|---|---|---|---|
| Activity object marker | Declarar objeto local participante de Activity | `SessionActivity/Authoring/ActivityObjectContributor.cs` | local authoring marker | Activity → Local | Preservar marker; redesenhar identidade |
| Contributor discovery | Encontrar contributors em cenas carregadas | `ActivityEntryObjectContributorDiscoveryStage.cs` | discovery | Activity → Local | Preservar discovery por escopo; evitar scan duplicado |
| Contribution report | Registrar contributor encontrado | `ActivityObjectContributionReport` | inventory/report | Activity → Local | Preservar report; tipar IDs |
| Capability scan target | Resolver report para `GameObject` vivo | `ActivityObjectCapabilityScanTargetAdapter.cs` | discovery adapter | Activity → Local | Útil, mas revarre cena por `targetId` |
| Capability scanner | Coletar lifecycle providers locais | `ActivityObjectCapabilityScanner.cs` | capability provider discovery | Local → Activity | Preservar; separar identity de observabilidade |
| Capability inventory | Unificar owners, descriptors e runtime refs | `ActivityCapabilityInventory.cs`, `ActivityCapabilityInventoryBuilder.cs` | inventory | Activity runtime | Preservar conceito |
| Runtime references | Guardar endpoint/provider vivo | `RuntimeReferences/*.cs` | endpoint reference | Local lifecycle | Útil no ciclo atual; perigoso após unload/reload |
| Setup requirements | Declarar requirements locais e de bindings | `ActivitySetupRequirementsAuthoring.cs` | authoring requirements | Activity → Local/subsystems | Preservar requiredness; reduzir dispersão |
| Setup inventory | Converter requirements em plano runtime | `ActivitySetupInventoryBuilder.cs` | inventory | Activity setup | Preservar inventory; tipar `inventoryId` |
| Reset endpoint | Reset local por intent/profile | `ActivityObjectDefaultResetEndpoint.cs`, `ActivityObjectResetContracts.cs` | reset endpoint | Local lifecycle | Preservar contrato; endpoint default ainda é observacional |
| Snapshot provider | Capturar transform local | `ActivityObjectTransformSnapshotProvider.cs` | snapshot provider | Local lifecycle/save | Preservar provider; payload deve ser extensível |
| Restore endpoint | Restaurar transform local | `ActivityObjectTransformSnapshotRestoreEndpoint.cs` | restore endpoint | Local lifecycle/save | Preservar; evitar `targetId` string como match |
| Release endpoint | Liberar/unbind local | `ActivityObjectDefaultReleaseEndpoint.cs`, `ActivityObjectReleaseStage.cs` | release endpoint | Local lifecycle | Preservar; release kind precisa ser policy tipada |
| Exit correlation freeze | Congelar discovery/inventory para exit | `ActivityObjectExitCorrelationFreezeStage.cs`, `ActivityObjectExitRuntimeState.cs` | lifecycle state | Activity exit | Bom padrão contra stale state |
| Contributor unregister | Encerrar registro lógico de contributors | `ActivityObjectContributorUnregisterStage.cs` | release/cleanup | Activity exit | Preservar conceito; hoje é mais diagnóstico |
| Placement marker | Resolver posição local authored | `PlayerActorPlacementMarker.cs`, `ActivityEntryPlacementMarkerLookup.cs` | scene-authored object | Activity → Local | Preservar marker; não usar busca ampla sem escopo |
| Scene-authored actor | Actor já presente na cena | `SceneAuthoredActor.cs`, `SceneAuthoredActorInstanceSource.cs` | scene-authored object | Activity → Local/Actor | Preservar como contribuição especializada |
| Actor capability surface | Surface local de capabilities do actor | `ActorCapabilitySurface.cs` | capability surface | Local → subsystem | Bom padrão; ainda tem trilhos concretos |
| Route pause surface | Surface local para pause overlay/content | `RoutePauseSurfaceEndpoint.cs`, `RoutePauseSurfaceSlot.cs` | surface/slot | Route → Local / Activity → Route surface | Preservar surface/slot; isolar de Activity |
| Pause content binding | Instanciar conteúdo de pause em slot local | `ActivityPauseContentAdapter.cs`, `RoutePauseSurfaceEndpoint.cs` | surface receiver | Activity → Local surface | Útil; acoplado à surface de Route |
| Pause overlay endpoint | Mostrar/esconder overlay local | `PauseOverlayAdapter.cs`, `RoutePauseSurfaceEndpoint.cs` | endpoint | Route/Activity → Local | Preservar endpoint; resolver por handle tipado |
| Camera anchor host | Resolver anchors locais por id | `ActivityCameraAnchorHost.cs`, `SurfaceCameraAnchorHost.cs` | surface/slot | Local → camera consumer | Preservar anchor host; ids devem ser typed refs |
| Poolable spawn origin | Resolver origins locais de presentation | `ActorPresentationEndpoint.cs`, `PoolableSpawnOriginAnchor.cs` | local slot/anchor | Local → presentation/projectile | Preservar anchor; fallback deve ser explícito |

### 3. Fluxo Macro de Local

1. Objetos locais existem inicialmente em cenas de conteúdo, cenas de Route, prefabs de actor, prefabs de UI ou hierarquias authored.
2. Activity carrega conteúdo e registra `ActivityContentLoadedSet`.
3. Discovery percorre cenas carregadas com `SceneManager.GetSceneByName(...)`, `GetRootGameObjects()` e `GetComponentsInChildren(...)`.
4. Markers como `ActivityObjectContributor`, `SceneAuthoredActor`, `RoutePauseSurfaceEndpoint`, `PlayerActorPlacementMarker` e anchor hosts classificam objetos locais.
5. Reports e scan targets conectam authoring local ao runtime.
6. Scanners coletam providers/endpoints e geram `ActivityCapabilityDescriptor`.
7. `ActivityCapabilityInventory` guarda owners, capabilities e runtime references.
8. Requiredness vem de contributor, setup requirements, pause content entries e capability descriptors.
9. Reset/snapshot/restore/release são associados por `targetId`, policy metadata e runtime reference.
10. Activity consome Local por stages de setup/readiness/lifecycle.
11. Route consome Local por pause surface, camera surface e markers autorizados.
12. Side effects fortes aparecem em scene scan, `Instantiate`, `Destroy`, `SetActive`, transform mutation e registro scene-scoped.
13. Acoplamento excessivo aparece quando pipelines resolvem diretamente endpoints específicos e fazem matching por strings.

### 4. Discovery e Inventory Local

| Mecanismo | Descobre | Quem descobre | Identity/key | Estado produzido | Consumo | Falha |
|---|---|---|---|---|---|---|
| Contributor discovery | `ActivityObjectContributor` | `ActivityEntryObjectContributorDiscoveryStage` | `sceneName`, `targetId`, `roleId`, `entrySequence` | `ActivityObjectContributorDiscoveryResult` | capability scan, reset, snapshot, release | Fail-fast se cena esperada não carregada |
| Scan target adapter | `GameObject` do contributor | `ActivityObjectCapabilityScanTargetAdapter` | `sceneName + targetId` | `ActivityObjectCapabilityScanTarget` | `ActivityObjectCapabilityScanner` | unresolved report, sem throw imediato |
| Object capability scanner | lifecycle providers em MonoBehaviours | `ActivityObjectCapabilityScanner` | ownerId/capabilityId derivado | owners, descriptors, runtime refs | inventory builder | ignora contribution inválida/null |
| Inventory builder | resultados de scanners | `ActivityCapabilityInventoryBuilder` | `ActivityCapabilityInventoryId.Signature` | `ActivityCapabilityInventory` | setup/lifecycle stages | throw se descriptor/ref inválido |
| Setup inventory | requirements authored | `ActivitySetupInventoryBuilder` | `activityId|entrySequence|activity_setup_inventory` | `ActivitySetupInventory` | stages consumidores | build failed fatal |
| Placement marker lookup | `PlayerActorPlacementMarker` | `ActivityEntryPlacementMarkerLookup` | `placementId` | posição/rotação | actor materialization/placement | false com reason; duplicate é falha |
| Route pause surface resolution | `RoutePauseSurfaceEndpoint` | `PauseOverlayAdapter`, `ActivityPauseContentAdapter` | `surfaceId`, `overlayRootId`, `activityContentRootId` | endpoint resolvido | pause overlay/content | fatal se ausente/duplicado |
| Pause slots | `RoutePauseSurfaceSlot` | `RoutePauseSurfaceEndpoint.BuildSlotMapOrFail` | `slotId` | slot map | bind pause content | fatal se slot required ausente/duplicado |
| Camera anchors | `ActivityCameraAnchorHost`, `SurfaceCameraAnchorHost` | resolvers de camera | `anchorId` | `Transform` | camera requirement | false/fail reason |
| Actor capability surface | endpoints/providers em actor root | `ActorCapabilitySurface` | actor root + provider type/path | providers/endpoints | actor scanners | throw se endpoint único duplicado |

Não há fallback silencioso nos pontos principais. Há skips explícitos para optional/missing. O caso mais sensível é `ActorPresentationEndpoint.TryResolve(...)`, que pode aplicar fallback para visual root quando a resolução permite; isso é logado, mas deve virar policy explícita em framework limpo.

### 5. Identidade Local

| Padrão | Onde aparece | Uso | Estabilidade | Avaliação |
|---|---|---|---|---|
| `targetId` | `ActivityObjectContributor`, reset/snapshot/release, setup requirements | chave funcional principal | Manual e frágil | Deve virar `LocalContentIdentity`/typed ref |
| `roleId` | `ActivityObjectContributor`, reports | classificação/diagnóstico | Opcional, string | Útil como metadata, não chave primária |
| `contributorKind` | `ActivityObjectContributorKind` | classificar scene/runtime/proxy | Enum estável | Preservar |
| `contributionId` | lifecycle contributions | dedupe/capability id | String derivada | Tipar ou gerar por provider estável |
| `ownerId` | `ActivityCapabilityOwnerDescriptor` | owner no inventory | Derivado de várias strings | Não deve ser chave pública |
| `capabilityId` | descriptors/runtime refs | lookup de runtime ref | String composta | Precisa typed id |
| `componentPath` | descriptors/runtime refs | observabilidade/ordenação | Frágil a rename/hierarchy | Somente diagnóstico |
| `ownerPath` | owner descriptor | observabilidade | Frágil | Somente diagnóstico |
| `sceneName` | reports, adapters, lookup | escopo de busca | Razoável como contexto, frágil como identity | Usar handle de content/scene |
| `placementId` | `PlayerActorPlacementMarker` | resolver marker | Manual | Typed marker id |
| `slotId` | `RoutePauseSurfaceSlot` | bind UI/content | Manual | Typed slot id |
| `anchorId` | camera anchor hosts | resolver transform | Manual | Typed anchor id |
| actor id/runtime id | `SceneAuthoredActor`, actor scan | identidade de actor local | Melhor modelado que `targetId` | Preservar separação actor/local |

O principal risco é `targetId` virar “cola universal”. Ele aparece em requirements, contributor, endpoint `Supports`, payload snapshot e policy metadata. Isso cria acoplamento por convenção textual.

### 6. Reset / Snapshot / Restore / Release Local

**Reset**

- Declarado por `IActivityObjectLifecycleContributionProvider`.
- Coletado por `ActivityObjectCapabilityScanner`.
- Referenciado por `ActivityObjectResetEndpointReference`.
- Executado por `ActivityEntryObjectResetStage`.
- Escopo vem de `ActivityResetScopePlan` e `ActivityResetBoundaryPolicy`.
- Requiredness vem do contributor/report.
- Falha fatal se contributor required não tem endpoint válido.
- Preservar: intents separados por entry/local/activity/activity transition/route transition.
- Redesenhar: dispatch por interfaces múltiplas pode virar policy/handler table tipada.

**Snapshot**

- Declarado por `ActivityObjectTransformSnapshotProvider`.
- Coletado como `SnapshotProvider`.
- Executado por `ActivityObjectSnapshotCaptureStage`.
- Salva `ActivityCapabilitySnapshotEnvelope` com payload JSON.
- Estado salvo hoje é transform world/local, não estado arbitrário.
- Preservar: snapshot por provider e envelope.
- Redesenhar: schema/payload deve ser contrato de capability, não montagem manual de JSON no stage.

**Restore**

- Declarado por `ActivityObjectTransformSnapshotRestoreEndpoint`.
- Executado por `ActivityEntryObjectSnapshotRestoreStage`.
- Associa payload a objeto por `targetId`.
- Rejeita payload foreign/stale por session/activity/schema.
- Preservar: validação de payload e verificação de restore.
- Redesenhar: matching por `targetId` e payload transform-only.

**Release**

- Declarado por `ActivityObjectDefaultReleaseEndpoint`.
- Executado por `ActivityObjectReleaseStage`.
- Usa `SupportedReleaseKinds` do contributor e `IActivityObjectReleaseEndpoint.Supports`.
- Falha se required não tem endpoint ou release kind não suportado.
- Preservar: release kind explícito.
- Redesenhar: supported kinds deveriam sair de policy/capability descriptor, não lista solta no marker.

**Cleanup / Exit**

- `ActivityObjectExitCorrelationFreezeStage` congela discovery/inventory em `ActivityObjectExitRuntimeState`.
- `ActivityObjectContributorUnregisterStage` emite unregister por contributor.
- Preservar: freeze antes de exit.
- Redesenhar: garantir que runtime refs vivas não sejam usadas após unload.

### 7. Relação Local ↔ Activity

Activity usa Local assim:

- `ActivityContentProfileAsset` declara cenas de conteúdo.
- Após load, `ActivityEntryObjectContributorDiscoveryStage` procura `ActivityObjectContributor`.
- `ActivityObjectContributionReport` registra `targetId`, `roleId`, scene, requiredness, reset eligibility e release kinds.
- `ActivityObjectCapabilityScanTargetAdapter` resolve o `GameObject`.
- `ActivityObjectCapabilityScanner` coleta providers/endpoints.
- `ActivityCapabilityInventory` alimenta reset, snapshot, restore, release e outros bindings.
- `ActivitySetupInventory` registra requirements que também referenciam objetos locais por `targetId`.
- `ActivityEntryPlacementMarkerLookup` busca placement markers em Activity content e active Route scene.

Onde Activity depende demais de detalhe local:

- pipelines e stages conhecem `targetId`, `roleId`, policy metadata, endpoint types e capability kinds;
- snapshot/restore sabe que o payload é transform;
- release sabe supported release kinds do contributor;
- adapters revarrem cena para resolver endpoints.

Conceito futuro recomendado: `ActivityLocalContributionSet`, contendo handles typed para contributors, capabilities, lifecycle participants, requirements e diagnostics.

### 8. Relação Local ↔ Route

Route usa Local em três padrões principais:

- pause surface local: `RoutePauseSurfaceEndpoint`, `RoutePauseSurfaceSlot`, `PauseOverlayAdapter`, `ActivityPauseContentAdapter`;
- camera/surface anchors: `SurfaceCameraAnchorHost`, `ActivityCameraAnchorHost`, resolvers de camera;
- placement autorizado: `ActivityEntryPlacementMarkerLookup` busca também na active Route scene.

Hipótese controlada: não foi confirmada instância textual em `.prefab`/`.unity` para todos esses componentes dentro do escopo lido; a auditoria confirma classes/contratos e alguns prefabs, não a composição real de cenas.

Onde Route deveria só descobrir contribution:

- pause overlay/content deveria ser `RouteLocalContributionSet` ou `RouteSurfaceContributionSet`;
- camera anchors deveriam ser surface contributions tipadas;
- placement marker deveria ser contribution de spawn/placement, não busca direta por componente.

### 9. Padrões Bons a Preservar

| Padrão | Onde aparece | Por que é útil | Reinterpretação futura |
|---|---|---|---|
| Contributor marker explícito | `ActivityObjectContributor` | Authoring claro | `LocalContentContributionMarker` |
| Discovery por conteúdo carregado | `ActivityEntryObjectContributorDiscoveryStage` | Escopo limitado | Discovery por `LocalContentHandle` |
| Inventory antes de execução | `ActivityCapabilityInventory` | Evita execução oportunista | `LocalContributionSet` imutável por ciclo |
| Runtime references | `RuntimeReferences/*.cs` | Execução direta sem service locator | Handles válidos por lifecycle |
| Requiredness local | contributor/setup requirements | Falha correta para obrigatório | Policy única de requiredness |
| Endpoint por lifecycle | reset/snapshot/restore/release contracts | ISP razoável | Interfaces menores e typed |
| Exit freeze | `ActivityObjectExitCorrelationFreezeStage` | Protege teardown | Snapshot de handles antes de exit |
| Anchor/slot authored | `RoutePauseSurfaceSlot`, anchor hosts | UX clara para designers | `LocalSlot`/`LocalAnchor` typed |
| Duplicate detection | slots, anchors, surfaces | Evita ambiguidade | Validador de authoring |
| Stale/foreign checks | reset/restore/release stages | Protege ciclo errado | Identity typed obrigatória |

### 10. Padrões Ruins a Redesenhar ou Descartar

| Padrão | Onde aparece | Risco | Alternativa |
|---|---|---|---|
| `targetId` como chave universal | contributors, requirements, snapshot, restore | colisão e coupling textual | `LocalContentIdentity` typed |
| `ownerId`/`capabilityId` por concatenação | `ActivityCapabilityInventoryId` | frágil e difícil de migrar | IDs estruturados |
| transform path como metadata funcional | descriptors/runtime refs | quebra por rename/hierarchy | observabilidade apenas |
| scan duplicado de cena | discovery + target adapter | custo e stale refs | discovery produzir handle direto |
| runtime refs vivas após freeze | exit state | pode apontar para objeto destruído | handles com validade/lifecycle |
| requiredness espalhada | contributor + setup + profiles | decisão inconsistente | requiredness policy central |
| stage conhece endpoint específico | reset/snapshot/release stages | Activity sabe demais de Local | executor por capability contract |
| JSON manual no stage | snapshot capture | payload frágil | serializer/schema por capability |
| fallback visual-root | `ActorPresentationEndpoint` | pode mascarar authoring errado | fallback opt-in explícito |
| registry/DependencyManager para local anchor | `ActivityCameraAnchorHost` | global/scene registry acoplado | scene-local contribution registry controlado |

### 11. Classificação Básico / Intermediário / Avançado

**Básico**

- `LocalContentIdentity`: precisa existir antes de qualquer inventory.
- `LocalContentContributionMarker`: desbloqueia objetos locais simples.
- `LocalContributionDiscovery`: encontra markers em escopo controlado.
- `LocalContributionReport`: registra o que foi encontrado.
- `LocalContributionSet`: inventory mínimo por ciclo.
- `LocalRequiredness`: define required/optional sem ambiguidade.

**Intermediário**

- `LocalCapabilityEndpoint`: permite reset/release/snapshot por componente.
- `LocalResetParticipant`: necessário para jogos reais.
- `LocalSnapshotParticipant`: necessário para save/restore.
- `LocalReleaseParticipant`: necessário para cleanup.
- `LocalSlot`/`LocalAnchor`: necessário para UI, camera e spawn points.
- `ActivityLocalContributionSet`: integra Local com Activity.

**Avançado**

- `RouteLocalContributionSet`: surfaces de route, pause/camera.
- `LocalCapabilitySnapshotEnvelope`: save/progression robusto.
- `LocalRestoreCompatibility`: restore versionado.
- `LocalContributionDiagnostics`: tooling/Inspector validation.
- Actor/presentation/projectile capability contribution: consumidores tardios.
- Fallback policies: só depois de contratos básicos sólidos.

### 12. Matriz Local

| Capacidade | Tipo | Arquivos principais | Nível | Preservar | Redesenhar | Descartar | Dependências | Observações |
|---|---|---|---|---|---|---|---|---|
| Activity object marker | local authoring marker | `ActivityObjectContributor.cs` | Básico | marker explícito | `targetId` typed | strings soltas | Activity content | Base do Local |
| Contributor discovery | discovery | `ActivityEntryObjectContributorDiscoveryStage.cs` | Básico | scan por escopo | handle direto | re-scan duplicado | loaded set | Fail-fast em cena ausente |
| Contribution report | inventory | `ActivityObjectContributorDiscoveryContracts.cs` | Básico | report runtime | IDs estruturados | scene/name como chave | identity | Bom para diagnóstico |
| Capability inventory | inventory | `ActivityCapabilityInventory*.cs` | Básico | inventory antes de execução | id model | concatenação | scanners | Conceito central |
| Runtime references | endpoint reference | `RuntimeReferences/*.cs` | Intermediário | reference por ciclo | validade explícita | uso após unload | inventory | Risco stale |
| Setup requirements | authoring | `ActivitySetupRequirementsAuthoring.cs` | Intermediário | requiredness | ownership por capability | lista monolítica | Activity profile | Mistura muitos subplanos |
| Reset | endpoint/lifecycle | `ActivityObjectResetContracts.cs`, `ActivityEntryObjectResetStage.cs` | Intermediário | intents | dispatch | endpoint default observacional como real | inventory | Bom boundary |
| Snapshot | snapshot provider | `ActivityObjectSnapshot*` | Intermediário | provider/envelope | payload schema | JSON manual no stage | inventory/save | Transform-only hoje |
| Restore | restore endpoint | `ActivityEntryObjectSnapshotRestoreStage.cs` | Intermediário | stale checks | matching typed | `targetId` match | snapshot payload | Boa validação |
| Release | release endpoint | `ActivityObjectReleaseStage.cs` | Intermediário | release kind | policy typed | supported list solta | inventory | Required falha corretamente |
| Exit freeze | lifecycle state | `ActivityObjectExitRuntimeState.cs` | Intermediário | freeze | handle validity | refs vivas long-lived | exit flow | Bom padrão |
| Placement marker | scene object | `PlayerActorPlacementMarker.cs` | Intermediário | marker | typed placement id | busca ampla | content/route scenes | Detecta duplicate |
| Scene-authored actor | scene object | `SceneAuthoredActor.cs` | Avançado | authored actor | separar Actor/Local | ActivityAsset refs diretas | Actor | Local especializado |
| Actor capability surface | surface/provider | `ActorCapabilitySurface.cs` | Avançado | provider aggregation | remover trilho concreto | endpoint concreto oculto | Actor root | Boa direção |
| Pause surface | surface/slot | `RoutePauseSurfaceEndpoint.cs` | Avançado | endpoint/slot | contribution set | Activity resolver direto | Route surface | Route-local |
| Camera anchors | anchor host | `ActivityCameraAnchorHost.cs`, `SurfaceCameraAnchorHost.cs` | Avançado | anchors | typed anchor refs | global registry implícito | camera | Local consumer |
| Spawn origin anchor | slot/anchor | `PoolableSpawnOriginAnchor.cs` | Avançado | typed origin | fallback policy | fallback implícito | presentation | Consumidor tardio |

### 13. Dependências e Ordem Recomendada

1. Definir `LocalContentIdentity`.
2. Definir `LocalContentHandle`.
3. Definir `LocalContentContributionMarker`.
4. Definir `LocalContributionDiscoveryScope`.
5. Definir `LocalContributionReport`.
6. Definir `LocalContributionSet`.
7. Definir `LocalRequirednessPolicy`.
8. Definir `LocalCapabilityEndpoint`.
9. Definir `LocalResetParticipant`.
10. Definir `LocalSnapshotParticipant`.
11. Definir `LocalRestoreParticipant`.
12. Definir `LocalReleaseParticipant`.
13. Definir `ActivityLocalContributionSet`.
14. Definir `RouteLocalContributionSet`.
15. Adicionar slots/anchors/surfaces.
16. Só depois integrar actors, camera, pause, presentation, projectile e save avançado.

### 14. Recomendações para Framework Futuro

Deveria existir:

- `LocalContentIdentity`
- `LocalContentHandle`
- `LocalContentContribution`
- `LocalContentContributionMarker`
- `LocalContributionDiscovery`
- `LocalContributionSet`
- `LocalCapabilityEndpoint`
- `LocalCapabilityDescriptor`
- `LocalCapabilityRuntimeReference`
- `LocalResetParticipant`
- `LocalSnapshotParticipant`
- `LocalRestoreParticipant`
- `LocalReleaseParticipant`
- `LocalSlot`
- `LocalAnchor`
- `ActivityLocalContributionSet`
- `RouteLocalContributionSet`

Não deveria existir como API pública:

- `targetId` string como chave universal;
- `ownerId`/`capabilityId` por concatenação;
- transform path como identity;
- scanner que precisa revarrer cena para resolver report;
- pipelines conhecendo endpoints concretos;
- fallback de Local sem policy explícita;
- requiredness duplicada em múltiplas fontes sem regra de precedência;
- route/activity controlando diretamente GameObjects locais.

O que deve ficar para ciclos futuros:

- actor capability surface avançada;
- camera anchors;
- pause content slots;
- projectile/spawn origin anchors;
- save/progression local versionado;
- tooling visual de validation no Inspector.

## 15. Perguntas Abertas para Próxima Auditoria

- Qual é a unidade canônica de identidade local: scene object, prefab instance, contributor ou capability?
- `targetId` deve existir apenas como authoring label ou como typed asset/reference?
- Um Local pode contribuir para múltiplas Activities simultaneamente?
- Local pertence ao content scene, à Activity, à Route ou ao próprio prefab?
- Como invalidar runtime references após unload?
- Requiredness do contributor vence requiredness do setup requirement, ou o contrário?
- Snapshot local deve ser capability-specific e versionado desde o início?
- Restore deve exigir compatibility check antes de aplicar?
- Route-local e Activity-local usam o mesmo modelo de contribution?
- Anchors/slots devem ser um tipo genérico de Local ou APIs especializadas?

### 16. Itens que Devem Virar ADR

- Identidade canônica de Local.
- Boundary `Activity → Local`.
- Boundary `Route → Local`.
- Modelo de `LocalContributionDiscovery`.
- Modelo de `LocalContributionSet`.
- Política de required/optional local.
- Modelo de runtime references e validade por lifecycle.
- Reset/snapshot/restore/release local.
- Snapshot envelope e schema versionado.
- Política para slots/anchors/surfaces.
- Regras para scene-authored actors como Local especializado.
- Política de fallback em endpoints locais.
- O que é diagnóstico versus chave funcional.

Nenhum arquivo foi modificado; a auditoria foi feita por leitura estática.

---

## 16. Auditoria Profunda — RuntimeSpawned

### 1. Resumo Executivo

`RuntimeSpawned` em `NewScripts` não é um subsistema único. É um conjunto de capacidades espalhadas por materialização de atores, pooling, projéteis, presentation, camera rigs, pause content e áudio. O melhor valor arquitetural está nos conceitos, não na forma concreta atual.

O que deve ser preservado como inventário funcional:

- comandos/resultados/handles de materialização;
- identidade explícita de instância runtime;
- escopos `Session`, `Route`, `Activity` e `RuntimeTransient`;
- metadados de origem de spawn;
- pool definido por asset;
- retorno ao pool em falha;
- tracking de objetos runtime por owner;
- release explícito por escopo.

O que não deve ser copiado para `com.immersive.framework`:

- roots criados diretamente por adapters;
- `GameObject.Find` e nomes de objetos como identidade funcional;
- IDs compostos por string como contrato primário;
- materializers que também fazem root, identity, setup, validation, registry e cleanup;
- `Destroy` direto espalhado em stages/adapters;
- acoplamento de `RuntimeSpawned` ao caso projectile;
- lifecycle implícito escondido em pool/global singleton/root.

Severidade geral: **Alta para arquitetura futura**, porque a capacidade existe e é rica, mas a ownership está distribuída demais para virar framework core sem redesenho.

---

### 2. Capacidades RuntimeSpawned Inventariadas

| Capacidade | Evidência | Owner atual | O que faz | Avaliação |
|---|---|---:|---|---|
| Materialização de player actors | `Actors/Players/ActivitySetup/PlayerActorMaterializationAdapter.cs` | `Actors/Players` + `SessionActivity` | Instancia prefab, cria root por escopo, aplica identity, registra estado e exige reset endpoint | Preservar conceito; redesenhar ownership/root |
| Contratos de materialização de player | `Actors/Players/ActivitySetup/PlayerActorSetupContracts.cs` | `Actors/Players` | Define command, entry plan, record, runtime handle | Bom padrão |
| Identidade runtime de actor | `Actors/Foundation/ActorIdentityContracts.cs` | `Actors/Foundation` | `ActorId`, `ActorInstanceRuntimeId`, factories de IDs scoped/runtime-spawned | Preservar wrappers; substituir composição string como contrato |
| Component identity debug/runtime | `Actors/Players/Runtime/PlayerActorIdentity.cs` | `Actors/Players` | Guarda session/activity/player ids em componente | Útil como espelho; não como identidade canônica |
| Store de actors session-scoped | `Actors/ActivitySetup/SessionActorRuntimeStore.cs` | `Actors/ActivitySetup` | Indexa actor runtime por session/actor/participant | Bom índice técnico; não deve ser lifecycle owner sozinho |
| Registry de player actors ativos/route | `Actors/Players/ActivitySetup/ActivityPlayerActorRegistry.cs` | `Actors/Players` | Indexa active handles e route-scoped handles | Útil; risco de virar owner de lifecycle |
| Release de actors runtime | `SessionActivity/Pipeline/Stages/SessionActivityActorRuntimeReleaseStage.cs` | `SessionActivity` | Executa release contribution e destrói instâncias | Preservar ordem; redesenhar cleanup policy |
| Actor runtime spawned poolable | `Actors/Runtime/RuntimeSpawnedActor.cs` | `Actors/Runtime` | Actor com metadata runtime, origem, pool hooks e layer bootstrap | Conceito forte; responsabilidades misturadas |
| Origem de spawn | `Actors/Foundation/RuntimeSpawnOriginContracts.cs` | `Actors/Foundation` | Guarda owner actor, owner runtime id, profile, pool definition, sequência | Preservar |
| Políticas de spawn/lifetime/reset/snapshot | `Actors/Foundation/ActorSpawnabilityContracts.cs` | `Actors/Foundation` | Define `RuntimeSpawned`, `RuntimeTransient`, return/reset/snapshot policies | Conceito bom; ainda passivo |
| Contratos de return/reset | `Actors/Foundation/ActorSpawnedResetReleaseContracts.cs` | `Actors/Foundation` | Comandos/fatos passivos para retorno/reset de spawned actor | Bom vocabulário futuro |
| Spawn pooled projectile | `Actors/Projectile/Runtime/PooledActorProjectileSpawnAdapter.cs` | `Actors/Projectile` | Renta pool, posiciona, cria runtime id, bind metadata, motion, impact, layer | Funcional; monolítico |
| Tracking de spawned projectile | `Actors/Projectile/Runtime/ActorProjectileSpawnRuntimeState.cs` | `Actors/Projectile` | Rastreia instâncias spawned por owner, valida foreign/stale e retorna ao pool | Excelente conceito; deve ser genericizado |
| Return por impacto | `Actors/Projectile/Runtime/ActorProjectileImpactReturnHandler.cs` | `Actors/Projectile` | Retorna spawned actor ao pool quando impacto ocorre | Bom padrão evento → return |
| Profile de spawn projectile | `Actors/Projectile/Authoring/ActorProjectileSpawnProfileAsset.cs` | `Actors/Projectile` | Liga profile, pool, materialization kind, lifetime, reset, snapshot | Bom inventário de authoring |
| Pool definition | `Foundation/Platform/Pooling/Config/PoolDefinitionAsset.cs` | `Foundation/Platform/Pooling` | Prefab, tamanho, expansão, lifetime scope, registration mode, prewarm, auto return | Bom, mas escopo ainda limitado |
| Pool service | `Foundation/Platform/Pooling/Runtime/PoolService.cs` | `Foundation/Platform/Pooling` | Registra pools, renta, retorna, libera por scope, shutdown | Bom motor técnico; root global precisa redesenho |
| GameObject pool | `Foundation/Platform/Pooling/Runtime/GameObjectPool.cs` | `Foundation/Platform/Pooling` | Instancia, prewarm, rent/return, hooks, cleanup | Bom básico técnico |
| Pool host/root | `Foundation/Platform/Pooling/Runtime/PoolRuntimeHost.cs` | `Foundation/Platform/Pooling` | Cria root do pool e child `Available` | Útil; ownership fraca por nome |
| Auto return | `Foundation/Platform/Pooling/Runtime/PoolAutoReturnTracker.cs` | `Foundation/Platform/Pooling` | Coroutine para retorno automático | Útil técnico; perigoso como gameplay lifetime |
| Preparação de pool na Activity | `SessionActivity/Pipeline/Stages/ActivityEntryPoolPreparationStage.cs` | `SessionActivity` | Coleta dependencies dos actors e registra pools `ActivityEntry` | Bom boundary |
| Release de pool Activity | `SessionOperational/Pipeline/OperationalActivityPoolReleaseStage.cs` | `SessionOperational` | Libera pools `Activity` em transição operacional | Bom conceito; scope genérico demais |
| Pause content runtime | `SessionActivity/Adapters/RoutePauseSurfaceEndpoint.cs` | `SessionActivity` | Instancia prefabs em slots e destrói no release | Bom modelo slot/content; endpoint tem lifecycle demais |
| Presentation runtime | `Actors/Presentation/Adapters/UnityActorPresentationMaterializationAdapter.cs` | `Actors/Presentation` | Instancia visual prefab em slot/container; release por policy | Bom padrão; cleanup direto |
| Presentation em pooled actor | `Actors/Presentation/Runtime/ActorPooledPresentationPreparer.cs` | `Actors/Presentation` | Materializa presentation em pool create/rent | Bom caso advanced; usa IDs fixos/string |
| Activity camera rig | `CameraPresentation/Runtime/CinemachineActivityCameraDirector.cs` | `CameraPresentation` | Instancia rig, valida, conecta target, destrói no release | RuntimeSpawned claro; release direto |
| Audio SFX pool boot | `AudioRuntime/Playback/Bootstrap/AudioSfxPoolPreparationStage.cs` | `AudioRuntime` | Prepara pools `GlobalBoot` vindos de `AudioDefaultsAsset` | Pool global funcional; não é core de runtime actor |

---

### 3. Fluxo Macro de RuntimeSpawned

Fluxo comum observado:

1. **Request / comando**
   - `PlayerActorMaterializationCommand`
   - `ActorProjectileFireCommand`
   - `ActorPresentationMaterializationCommand`
   - `ActivityCameraBindingCommand`
   - pause content bind command
   - audio/pool preparation via defaults/profile/dependencies

2. **Resolução de plano/profile**
   - player actor usa `PlayerActorEntryPlan`;
   - projectile usa spawn profile + `PoolDefinitionAsset`;
   - presentation usa resolved plan e slot/container;
   - camera usa requirement com `CameraRigPrefab`;
   - pause content usa profile contributions + surface slots.

3. **Materialização**
   - `Object.Instantiate` para player actors, pause content, presentation visual e camera rig;
   - `IPoolService.Rent` para projectile/runtime pooled actors;
   - `GameObjectPool` instancia/prewarm internamente.

4. **Parent/root**
   - player actor session-scoped: root `__SessionActorsRuntimeRoot::{sessionId}` com `DontDestroyOnLoad`;
   - player actor não session-scoped: root `__ActivityPlayerActorsRuntimeRoot::{sessionId}::{activityId}::{entrySequence}`;
   - pool: root global `NewScripts_PoolingRuntime`, hosts `Pool_{label}`, child `Available`;
   - projectile rented: parent opcional `_spawnParent`;
   - pause content: `RoutePauseSurfaceSlot`;
   - presentation: primary slot/container;
   - camera rig: instanciado sem root explícito dedicado.

5. **Identidade**
   - `ActorInstanceRuntimeId.FromScopedRuntimeActorIdentity`;
   - `ActorInstanceRuntimeId.FromRuntimeSpawnedActorIdentity`;
   - `RuntimeSpawnOriginMetadata`;
   - `PlayerActorIdentity`;
   - `ActorPresentationRuntimeHandle`;
   - `ActivityCameraBindingHandle`;
   - pool identity por referência de `PoolDefinitionAsset`.

6. **Registro / tracking**
   - `ActivityPlayerActorRegistry`;
   - `SessionActorRuntimeStore`;
   - `ActorProjectileSpawnRuntimeState`;
   - dicionários internos de `GameObjectPool`;
   - listas internas de pause content;
   - handles/facts de presentation/camera.

7. **Setup pós-materialização**
   - bind metadata;
   - participação ativa;
   - reset endpoint obrigatório;
   - motion endpoint;
   - impact endpoints;
   - layer bootstrap;
   - camera target;
   - presentation endpoint rebuild;
   - pool hooks.

8. **Release / return**
   - `Object.Destroy` para player actor, pause content, presentation, camera rig;
   - `IPoolService.Return` para spawned pooled actors;
   - `ReleasePoolsForScope(Activity)`;
   - `Shutdown` para global pools;
   - `ActorReleaseContributionStage` antes de destroy.

---

### 4. Roots e Ownership Runtime

| Root / Container | Criador atual | Owner lógico atual | Evidência | Problema |
|---|---|---|---|---|
| `__SessionActorsRuntimeRoot::{sessionId}` | `PlayerActorMaterializationAdapter` | Session | `EnsureSessionRuntimeRoot`, `DontDestroyOnLoad` | Adapter cria root e usa nome como lookup |
| `__ActivityPlayerActorsRuntimeRoot::{sessionId}::{activityId}::{entrySequence}` | `PlayerActorMaterializationAdapter` | Activity | `EnsureActivityRuntimeRoot` | Route-scoped actors também caem no caminho não-session; ambíguo |
| `NewScripts_PoolingRuntime` | `PoolService` | Pool service/global | constructor de `PoolService` | Root global dentro de serviço técnico |
| `Pool_{label}` | `PoolRuntimeHost` | Pool instance | `PoolRuntimeHost` | Nome/label como diagnóstico e estrutura |
| `Available` | `PoolRuntimeHost` | Pool instance | inactive queue | Correto tecnicamente |
| rented parent | `PoolRuntimeHost.AttachAsRented` | Caller/spawn owner | `GameObjectPool.Rent(parent)` | Parent opcional pode esconder ownership |
| pause surface slots | `RoutePauseSurfaceEndpoint` / scene | Route/Activity surface | `RoutePauseSurfaceEndpoint` | Endpoint materializa e destrói conteúdo diretamente |
| presentation slot/container | `ActorPresentationEndpoint` / plan | Actor presentation | `UnityActorPresentationMaterializationAdapter` | Adapter destrói diretamente |
| camera rig instance | `CinemachineActivityCameraDirector` | Activity camera binding | linhas 94-127 e 159 | Sem root registry; destroy direto |

Owner correto no framework futuro: **um Runtime Root Registry / Runtime Scope Root Service** dentro de `com.immersive.framework`, consumido por materializers. Materializer não deve criar roots por conta própria.

---

### 5. Identidade de Instâncias Runtime

Identidades fortes observadas:

- `ActorId` em `Actors/Foundation/ActorIdentityContracts.cs`;
- `ActorInstanceRuntimeId` em `Actors/Foundation/ActorIdentityContracts.cs`;
- `RuntimeSpawnProfileId` em `Actors/Foundation/RuntimeSpawnOriginContracts.cs`;
- `RuntimeSpawnOriginMetadata` com owner actor, owner runtime id, pool definition e sequence;
- `PlayerActorIdentityRecord`;
- `PlayerActorRuntimeHandle`;
- `ActorPresentationRuntimeHandle`;
- `ActivityCameraBindingHandle`.

Identidades frágeis observadas:

- `PlayerActorId` montado como string com `sessionId|actorId`;
- `ActorInstanceRuntimeId` internamente composto por string com separadores;
- spawned projectile id `actor.projectile.runtime.spawn.{sequence}`;
- nomes de GameObjects usados como diagnóstico e, em alguns casos, lookup/root identity;
- pool host por label;
- `ActorPooledPresentationPreparer` usa strings fixas como `actor.pool.runtime_spawned_actor.presentation` e `actor.projectile.runtime.spawn.pool`.

Avaliação:

- Os wrappers tipados são bons.
- A composição string deve virar detalhe interno ou ser substituída por structs com campos reais.
- Nome de GameObject deve ser apenas diagnóstico.
- Pool identity por referência de `PoolDefinitionAsset` é aceitável dentro de Unity, mas precisa de uma política para package/distribution/versioning.

---

### 6. Release / Return / Cleanup

Caminhos existentes:

| Caminho | Evidência | Estratégia atual | Avaliação |
|---|---|---|---|
| Session-scoped actors | `SessionActivityActorRuntimeReleaseStage.cs` | `ActorReleaseContributionStage`, `Destroy`, remove store | Ordem boa; cleanup direto |
| Route-scoped player actors | `SessionActivityActorRuntimeReleaseStage.cs` | release contribution + `Destroy` | Registry ajuda, mas ownership de root é fraca |
| Runtime spawned projectile | `ActorProjectileSpawnRuntimeState.cs` | valida owner/origin e `IPoolService.Return` | Melhor padrão observado |
| Impact return | `ActorProjectileImpactReturnHandler.cs` | evento de impacto chama return no runtime state | Bom |
| Pool scope release | `PoolService.ReleasePoolsForScope` | `ReturnAllRentedObjects`, cleanup, remove pool | Bom técnico; scope pouco granular |
| Pool shutdown | `PoolService.Shutdown` | cleanup all + destroy global root | Bom global |
| RuntimeSpawnedActor | `RuntimeSpawnedActor.cs` | pool hooks limpam metadata/layer e emitem eventos | Bom, mas mistura responsabilidades |
| Pause content | `RoutePauseSurfaceEndpoint.cs` | `Destroy` das instâncias bound | Funcional; endpoint com ownership demais |
| Presentation | `UnityActorPresentationMaterializationAdapter.cs` linhas 82-118 | release policy, `Destroy` se não `KeepBound` | Bom contrato; cleanup direto |
| Camera rig | `CinemachineActivityCameraDirector.cs` linhas 131-166 | `Destroy` do rig | Funcional; sem cleanup policy central |

Risco principal: **não existe uma única política de release para conteúdo runtime**. Cada família decide entre `Destroy`, `Return`, `KeepBound`, `Shutdown` ou release por escopo.

---

### 7. Relação RuntimeSpawned ↔ Session

Relações claras:

- Player actors podem ser `SessionScoped`.
- `PlayerActorMaterializationAdapter` cria root persistente com `DontDestroyOnLoad`.
- `SessionActorRuntimeStore` indexa apenas actors `SessionScoped`.
- `SessionActivityActorRuntimeReleaseStage` limpa actors session-scoped para uma session ou todos.

O que Session deveria possuir no framework futuro:

- handles de runtime content com `Session` lifetime;
- root session-scoped;
- cleanup ordenado antes de encerramento de session;
- stores/indexes por identidade tipada.

O que Session não deveria possuir:

- projectiles `RuntimeTransient`;
- pause content activity-bound;
- camera rig activity-bound;
- pool técnico global;
- audio global boot pools, exceto como dependência de framework/app bootstrap.

Risco arquitetural: o root session-scoped é criado pelo adapter e localizado por string. O owner real é Session, mas o código que materializa actor também cria infraestrutura de Session.

---

### 8. Relação RuntimeSpawned ↔ Route

Relações observadas:

- `ActivityPlayerActorRegistry` mantém handles route-scoped.
- `SessionActivityActorRuntimeReleaseStage.ReleaseIndexedRouteScopedPlayerActors` limpa route-scoped player actors.
- `RoutePauseSurfaceEndpoint` fornece surface/slots para materialização de pause content.
- `SessionOperational/Pipeline/OperationalActivityPoolReleaseStage.cs` decide quando liberar pools `Activity` ao sair de uma atividade operacional.

Ambiguidade importante:

- `PlayerActorMaterializationAdapter` usa root de Activity para qualquer actor que não seja `SessionScoped`. Isso inclui o caso route-scoped, se materializado por esse caminho. Portanto, o lifetime pode ser `Route`, mas o root físico é `Activity`.

Severidade: **Alta** para framework futuro.

Owner correto:

- Route deve possuir roots/surfaces/handles route-bound.
- Activity deve possuir runtime transient/activity-bound.
- Route-scoped não deve depender de root nomeado por activity entry.

---

### 9. Relação RuntimeSpawned ↔ Activity

Activity é o maior consumidor de runtime materialization:

- materializa player actors activity-bound;
- prepara pools `ActivityEntry`;
- cria/usa projectiles runtime transient durante atividade;
- instancia pause content para activity identity;
- instancia camera rigs activity-bound;
- aciona presentation setup/release;
- release de pools `Activity` é feito em transição operacional.

Evidências principais:

- `ActivityEntryPoolPreparationStage.cs`;
- `OperationalActivityPoolReleaseStage.cs`;
- `PooledActorProjectileSpawnAdapter.cs`;
- `ActorProjectileSpawnRuntimeState.cs`;
- `RoutePauseSurfaceEndpoint.cs`;
- `CinemachineActivityCameraDirector.cs`;
- `UnityActorPresentationMaterializationAdapter.cs`.

Owner correto no futuro:

- Activity deve possuir `RuntimeSpawnedContentSet`;
- Activity deve possuir um cleanup plan ordenado;
- Activity deve consumir pool/presentation/camera/materializer por contratos explícitos;
- Activity não deve criar roots por convenção de nome.

---

### 10. Relação RuntimeSpawned ↔ Local

Local aparece como **ponto de ancoragem**, não como owner macro.

Exemplos:

- slots de pause surface em `RoutePauseSurfaceSlot`;
- containers de presentation via endpoint/plan;
- camera tracking/look-at targets em activity camera requirements;
- spawn parent/origin para projectile;
- markers/placements de player actor.

Papel correto de Local:

- expor anchors, slots, surfaces e endpoints tipados;
- validar presença de slots obrigatórios;
- não decidir Session/Route/Activity lifetime;
- não possuir cleanup macro;
- não fabricar identidade runtime.

Risco atual:

- alguns endpoints locais materializam e destroem conteúdo diretamente, como `RoutePauseSurfaceEndpoint`.
- isso funciona como gameplay glue, mas não deve virar padrão de framework core.

---

### 11. Padrões Bons a Preservar

- `Command` / `Result` / `Record` / `Handle`.
- Fail-fast para prefab, pool, endpoint, target e command inválidos.
- `ActorInstanceRuntimeId` como tipo explícito.
- `RuntimeSpawnOriginMetadata`.
- Pool identity por `PoolDefinitionAsset`.
- `PoolRegistrationMode`.
- `PoolLifetimeScope`.
- `IPoolableObject` hooks.
- Return ao pool em falhas intermediárias de spawn.
- Tracking de spawned instances por owner.
- Checks de foreign/stale owner antes de return.
- Release contribution antes de destroy.
- Separação entre pool preparation e pool release.
- Slot-based materialization com required/optional.
- Release policy como conceito (`KeepBound` em presentation).
- Camera rig validation antes de aceitar binding.
- Logs com source/reason.

---

### 12. Padrões Ruins a Redesenhar ou Descartar

- Root criado dentro de materialization adapter.
- `GameObject.Find` para root runtime.
- `DontDestroyOnLoad` decidido por adapter de actor.
- Nome de GameObject como identidade ou lookup.
- IDs funcionais compostos por string.
- Route-scoped actor sem root route-scoped próprio.
- Adapter que faz instantiate, identity, registry, reset validation e root management.
- Projectile spawn adapter que também configura motion, impact, visual observation e layer bootstrap.
- Runtime state genérico escondido em namespace projectile.
- `Destroy` direto espalhado em stage/adapter/director.
- Pool global root decidindo estrutura de escopos Activity.
- Auto return como configuração que pode simular regra de gameplay.
- `IPoolableObject` coletado só no root pelo pool.
- Endpoint local que instancia e destrói conteúdo de activity sem runtime owner externo.
- Strings fixas em `ActorPooledPresentationPreparer` para activity/actor fake.

---

### 13. Classificação Básico / Intermediário / Avançado

**Básico**

- Runtime identity.
- Runtime handle.
- Runtime root.
- Prefab materialization.
- Destroy release.
- Scope enum.
- Required/optional prefab.
- Slot/container materialization.

**Intermediário**

- Pool definition.
- Pool rent/return.
- Pool hooks.
- Activity pool preparation.
- Activity pool release.
- Owner runtime store.
- Registry/index by identity.
- Release contribution.
- Runtime origin metadata.
- Scoped cleanup plan.

**Avançado**

- Runtime spawned actor with origin + pool lifecycle.
- Owner-level spawned content tracking.
- Stale/foreign validation.
- Projectile pooled spawn.
- Impact-triggered return.
- Presentation prepared on pool creation/rent.
- Activity camera rig materialization.
- Audio global pool boot.
- Snapshot/reset policies for runtime transient content.
- Cross-scope cleanup ordering.

---

### 14. Matriz RuntimeSpawned

| Área | Básico | Intermediário | Avançado | Recomendação |
|---|---|---|---|---|
| Identity | `ActorId`, handle | scoped runtime id | origin metadata owner/spawn | Manter tipos, remover string como contrato |
| Roots | GameObject parent | session/activity root | registry por scope | Criar `RuntimeRootRegistry` |
| Materialization | prefab instantiate | command/result | policy-based materializer | Separar materializer de setup específico |
| Pooling | rent/return | scope release | auto return + hooks | Manter técnico, mover ownership para framework |
| Player actors | instantiate actor | registry/store | session/route/activity lifetime | Redesenhar root/lifetime |
| Projectiles | spawn prefab/pool | owner tracking | impact return/stale checks | Genericizar como runtime spawned content |
| Presentation | visual prefab slot | release policy | pooled actor preparation | Manter como consumidor, não core |
| Camera | rig instantiate | binding handle | validation/rebind | Consumidor activity-bound |
| Pause content | slot instantiate | required/optional | activity identity binding | Consumidor slot-based |
| Audio | pool prepare | global boot catalog | voice pool lifecycle | Consumidor de pooling, não runtime actor core |
| Release | destroy | release contribution | scoped cleanup ordering | Centralizar policy |
| Diagnostics | names/logs | source/reason | runtime facts | Manter logs, não usar nomes como identidade |

---

### 15. Dependências e Ordem Recomendada

1. Definir identidade runtime tipada sem string como contrato público.
2. Definir `RuntimeOwnership` / `RuntimeScope`.
3. Definir `RuntimeRoot` e `RuntimeRootRegistry`.
4. Definir `RuntimeMaterializationRequest`.
5. Definir `RuntimeMaterializationResult`.
6. Definir `RuntimeContentHandle`.
7. Definir `RuntimeReleasePolicy`.
8. Definir `RuntimeCleanupPlan`.
9. Definir prefab materializer básico.
10. Definir pooled materializer básico.
11. Integrar pool definition/service como dependência, não como owner macro.
12. Definir `RuntimeSpawnOriginMetadata`.
13. Definir `RuntimeSpawnedContentSet` por owner/scope.
14. Definir Local slots/anchors como endpoints tipados.
15. Reintegrar consumers: actor, projectile, presentation, camera, pause content, audio.
16. Só depois discutir authoring/Inspector UX.

---

### 16. Recomendações para Framework Futuro

Criar no `com.immersive.framework` conceitos mínimos:

- `RuntimeContentIdentity`
- `RuntimeContentHandle`
- `RuntimeMaterializationRequest`
- `RuntimeMaterializationResult`
- `RuntimeOwnership`
- `RuntimeScopeRoot`
- `RuntimeRootRegistry`
- `RuntimeReleasePolicy`
- `RuntimeCleanupPlan`
- `RuntimeSpawnOrigin`
- `RuntimeSpawnedContentSet`
- `PrefabRuntimeMaterializer`
- `PooledRuntimeMaterializer`

Não criar agora:

- `RuntimeSpawnedManager`;
- `RuntimeSpawnedCoordinator`;
- global singleton de materialização;
- lifecycle runtime completo;
- port direto de `SessionActivity` / `SessionOperational`;
- projectile-first API;
- presentation/camera/audio como core;
- compat rail com nomes antigos.

Direção correta:

- `foundation` fornece tipos/validation/events se necessário;
- `pooling` fornece pool técnico;
- `framework` possui ownership runtime, roots, cleanup e policies;
- módulos como projectile/presentation/camera/audio consomem isso depois.

---

### 17. Perguntas Abertas para Próxima Auditoria

- Route-scoped actor deve ter root route-scoped próprio?
- Quem destrói `__SessionActorsRuntimeRoot` quando a última instância session-scoped é removida?
- Pool `Activity` deve viver sob root global ou sob root de Activity?
- `RuntimeTransient` deve participar de save/snapshot ou sempre ser descartado?
- `PoolDefinitionAsset` como referência é identidade suficiente entre packages?
- Auto return pertence ao pool técnico ou à policy de gameplay?
- Presentation materializada em pooled actor deve ser criada no pool create ou no rent?
- Camera rig activity-bound deve usar pool ou instantiate/destroy?
- Pause content é Route-owned surface com Activity content, ou Activity-owned runtime content sob Route surface?
- O que acontece quando return ao pool falha mas a instância ainda está ativa?
- Deve existir release ordering formal entre actor spawned content, presentation, camera e audio?
- Como o framework deve expor isso no Inspector sem termos como `RuntimeSpawned` para usuário final?

---

### 18. Itens que Devem Virar ADR

- ADR: Modelo de ownership runtime `Session` / `Route` / `Activity` / `RuntimeTransient`.
- ADR: `RuntimeRootRegistry` e proibição de root por nome/`GameObject.Find`.
- ADR: Identidade runtime tipada e limites de strings diagnósticas.
- ADR: Contrato `Request` / `Result` / `Handle` para materialização.
- ADR: Diferença entre instantiate runtime e pooled runtime.
- ADR: Relação entre `PoolDefinitionAsset`, lifetime scope e registration mode.
- ADR: Cleanup ordering por scope.
- ADR: `RuntimeSpawnOriginMetadata` como contrato de auditoria/debug.
- ADR: Políticas de release: `Destroy`, `ReturnToPool`, `KeepBound`, `Detach`, `Shutdown`.
- ADR: Papel de Local slots/anchors/surfaces.
- ADR: Presentation/camera/audio como consumers, não core runtime ownership.
- ADR: Tratamento de orphan/stale/foreign runtime objects.
- ADR: O que pode aparecer no Inspector como linguagem pública.

---

## 17. Auditoria Profunda — Surface

### 1. Resumo Executivo

`Surface` no `NewScripts` aparece como contrato de espaço: cena, root, slot, anchor, container ou endpoint onde outro conteúdo será apresentado, vinculado, ativado ou materializado.

As melhores capacidades estão em:

- `RoutePauseSurfaceProfileAsset` + `RoutePauseSurfaceEndpoint` + `RoutePauseSurfaceSlot`;
- `SurfaceCameraAnchorHost` e `ActivityCameraAnchorHost`;
- `ActorPresentationEndpoint`, `ActorPresentationContainer` e `PoolableSpawnOriginAnchor`;
- contexts/commands/results que transportam Surface entre Route, Activity e consumidores.

O risco central é que `Surface` ainda não é um conceito isolado: pause, camera e presentation carregam suas próprias versões de surface, slot, anchor, root, discovery e lifecycle. Há bons padrões, mas eles estão presos aos consumidores.

Severidade arquitetural: **Alta**. O conceito é forte e reutilizável, mas precisa ser redesenhado como contrato próprio antes de virar framework.

---

### 2. Capacidades Surface Inventariadas

| Capacidade | Tipo | Arquivos/classes | Escopo | Avaliação |
|---|---|---|---|---|
| Route pause surface profile | surface profile | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Authoring\RoutePauseSurfaceProfileAsset.cs` | Route → Surface | Bom conceito declarativo; acoplado a pause/input |
| Route pause surface contracts | surface context / binding | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Contracts\RoutePauseSurfaceContracts.cs` | Route → Activity | Preservar structs tipados; redesenhar strings internas |
| Route pause surface context handoff | surface context | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Contracts\SessionActivityEntryHandoff.cs` | Route → Activity boundary | Bom handoff; muito específico de pause |
| Route pause surface endpoint | surface endpoint / overlay root / content root | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Adapters\RoutePauseSurfaceEndpoint.cs` | Surface → subsystem boundary | Muito útil; endpoint faz materialização e release demais |
| Route pause surface slot | slot | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Adapters\RoutePauseSurfaceSlot.cs` | Local → Surface boundary | Bom marker local; `slotId` string frágil |
| Activity pause content profile | content-to-surface binding profile | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Authoring\ActivityPauseContentProfileAsset.cs` | Activity → Surface | Bom mapping content/slot; consumidor específico |
| Activity pause content adapter | surface discovery / binding | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Adapters\ActivityPauseContentAdapter.cs` | Activity → Route Surface | Resolve endpoint por cena e bind; duplicado com overlay adapter |
| Pause overlay adapter | surface discovery / overlay binding | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Adapters\PauseOverlayAdapter.cs` | Activity → Route Surface | Bom fail-fast; discovery duplicado |
| Surface presentation profile | camera surface profile | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Authoring\SurfacePresentationProfileAsset.cs` | Route → Camera Surface | Bom profile; surface acoplada à camera |
| Surface camera anchor host | camera anchor | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Authoring\SurfaceCameraAnchorHost.cs` | Local → Surface boundary | Bom host de anchors; `anchorId` string |
| Surface camera requirement resolver | surface binding | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Runtime\SurfaceCameraPresentationRequirementResolver.cs` | Surface → Camera | Bom resolver puro |
| Activity presentation profile | activity camera profile | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Authoring\ActivityPresentationProfileAsset.cs` | Activity → Camera Surface | Bom profile; nome mistura presentation/camera |
| Activity camera anchor host | camera anchor / scene-scoped host | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Authoring\ActivityCameraAnchorHost.cs` | Activity → Local Surface | Bom registro por cena; depende de `DependencyManager` |
| Activity camera anchor resolver | surface discovery | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Runtime\SceneScopedActivityCameraAnchorHostResolver.cs` | Activity → Surface | Bom boundary |
| Actor presentation endpoint | presentation root / endpoint / spawn-origin surface | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Presentation\Runtime\ActorPresentationEndpoint.cs` | Local → Surface / RuntimeSpawned → Surface | Melhor exemplo de Surface local explícita |
| Actor presentation container | presentation root / slot | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Presentation\Runtime\ActorPresentationContainer.cs` | Local → Surface | Bom container tipado por kind + id |
| Actor presentation container resolver | surface validation / binding | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Presentation\Runtime\ActorPresentationContainerResolver.cs` | Surface binding | Bom resolver puro |
| Poolable spawn origin anchor | anchor | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Presentation\Runtime\PoolableSpawnOriginAnchor.cs` | RuntimeSpawned → Surface | Bom anchor para spawn; fallback perigoso |
| Actor presentation materializer | runtime surface binding | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Presentation\Adapters\UnityActorPresentationMaterializationAdapter.cs` | RuntimeSpawned → Surface | Instancia em container; cleanup direto |
| Route camera adapter | surface discovery / camera consumer | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Adapters\SessionOperationalRouteCameraAdapter.cs` | Route → Surface → Camera | Resolve anchor host por scene/provider; bom, mas consumidor sabe discovery |
| Activity camera adapter | surface binding | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Adapters\SessionOperationalActivityCameraAdapter.cs` | Activity → Surface → Camera | Bom comando/resultado; surface escondida em camera |
| Actor attribute UI target binding | UI consumer, não Surface pura | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Attributes\UI\ActivityEntryActorAttributeUiTargetResolver.cs` | Activity → UI consumer | Target de UI, não root surface genérico |

---

### 3. Fluxo Macro de Surface

1. **Declaração**
   - Route declara pause surface em `OperationalRouteAsset` via `RoutePauseSurfaceProfileAsset`.
   - Route declara camera surface via `SurfacePresentationProfileAsset`.
   - Activity declara camera anchors via `ActivityPresentationProfileAsset`.
   - Actor/local declara presentation surface via `ActorPresentationEndpoint`.

2. **Existência física**
   - Pause surface existe em cena additive referenciada por `SceneKeyAsset`.
   - Pause endpoint aponta `overlayRoot` e `activityContentRoot`.
   - Camera anchors existem como `Transform` em `SurfaceCameraAnchorHost` ou `ActivityCameraAnchorHost`.
   - Presentation roots existem como `ActorPresentationContainer`.

3. **Discovery / resolução**
   - Pause adapters varrem `SceneManager.GetSceneByName(...).GetRootGameObjects()` e `GetComponentsInChildren<RoutePauseSurfaceEndpoint>(true)`.
   - Route camera tenta `DependencyProvider.TryGetForScene<SurfaceCameraAnchorHost>` e depois scan em scene roots.
   - Activity camera usa `IActivityCameraAnchorHostResolver`, implementado por `SceneScopedActivityCameraAnchorHostResolver`.
   - Actor presentation resolve containers por endpoint local.

4. **Validação**
   - Pause profile valida `surfaceId`, scene, `overlayRootId`, `activityContentRootId`, input mode/policy.
   - Pause endpoint valida surface/root divergence e duplicate slots.
   - Camera anchor hosts validam anchors nulos, `anchorId` vazio e duplicados.
   - Presentation endpoint valida duplicate containers e duplicate spawn origins.

5. **Binding de conteúdo**
   - Pause content instancia prefab em `RoutePauseSurfaceSlot`.
   - Overlay show/hide ativa/desativa `overlayRoot`.
   - Camera rig usa anchors como targets.
   - Actor presentation instancia visual prefab em primary container.

6. **RuntimeSpawned dentro de Surface**
   - `UnityActorPresentationMaterializationAdapter` instancia visual em `primarySlot.Container`.
   - `ActorPooledPresentationPreparer` prepara presentation em pooled runtime actor.
   - `PoolableSpawnOriginAnchor` resolve origem para spawn runtime.
   - Pause content também é materialização runtime em slot.

7. **Route usando Surface**
   - Route carrega cena de surface com plano de cenas.
   - Route monta context e passa para Activity no handoff.
   - Route camera usa surface profile/anchors antes do reveal.

8. **Activity usando Surface**
   - Activity recebe `SessionActivityRoutePauseSurfaceContext`.
   - Activity usa surface para pause overlay, pause content e input mode.
   - Activity camera resolve anchors da cena ativa.

9. **Local expondo Surface**
   - Local expõe endpoint, slot, anchor e container.
   - Local valida duplicidade e requiredness.
   - Local não deveria possuir lifecycle macro.

10. **Cleanup**
   - Pause content destrói instâncias por identity.
   - Presentation destrói instância salvo `KeepBound`.
   - Camera director destrói rig.
   - Surface scene unload é herdado do plano de cenas da Route.

11. **Side effects fortes**
   - `Instantiate`, `Destroy`, `SetActive`, scene scan, dependency registration, scene load/unload.

12. **Acoplamento excessivo**
   - `RoutePauseSurfaceEndpoint` é Surface, overlay controller, content materializer e content lifecycle owner.
   - Pause overlay e pause content duplicam discovery.
   - Camera surface é representada por classes de camera, não por contrato Surface genérico.

---

### 4. Surface como Contrato, não Subsistema

**Surface como contrato de espaço**

Aparece melhor em `ActorPresentationEndpoint`, `ActorPresentationContainer`, `RoutePauseSurfaceEndpoint`, `RoutePauseSurfaceSlot`, `SurfaceCameraAnchorHost` e `ActivityCameraAnchorHost`.

Deveria possuir:

- identidade de surface;
- roots;
- slots;
- anchors;
- validação;
- discovery;
- lifecycle do espaço.

Não deveria possuir:

- conteúdo específico;
- pause behavior;
- camera rig behavior;
- presentation materialization;
- input policy.

**Pause como consumidor de Surface**

Aparece em `PauseOverlayAdapter`, `ActivityPauseContentAdapter`, `ActivityEntryPauseContentStage`.

Pause deveria consumir:

- `SurfaceEndpoint`;
- `SurfaceRoot`;
- `SurfaceSlot`;
- `SurfaceBindingRequest`.

Mistura atual:

- `RoutePauseSurfaceEndpoint` faz show/hide overlay e instancia pause content.

**Camera como consumidor de Surface/Anchor**

Aparece em `SurfaceCameraPresentationRequirementResolver`, `ActivityCameraPresentationRequirementResolver`, `CinemachineRouteCameraDirector`, `CinemachineActivityCameraDirector`.

Camera deveria consumir anchors e roots, não definir o contrato geral de Surface.

Mistura atual:

- `SurfacePresentationProfileAsset` é uma surface profile, mas específica de camera presentation.

**UI como conteúdo ou consumidor**

UI aparece mais como sink/target em `Actors\Attributes\UI`, não como Surface raiz genérica.

Hipótese: UI roots existem em prefabs/cenas, mas não há contrato genérico de `UISurface` evidente nos arquivos lidos.

**Presentation como conteúdo ou consumidor**

Presentation usa `ActorPresentationEndpoint` como surface local e `UnityActorPresentationMaterializationAdapter` como materializer.

Bom corte: endpoint não materializa. Materializer consome container.

**RuntimeSpawned como conteúdo materializado dentro de Surface**

Aparece em actor presentation, pause content e poolable spawn origin. RuntimeSpawned deve ser conteúdo; Surface deve ser destino/contrato.

---

### 5. Slots, Anchors e Roots

| Mecanismo | Representa | Declaração | Resolução | Required/optional | Falha | Avaliação |
|---|---|---|---|---|---|---|
| `surfaceId` | identidade de pause surface | `RoutePauseSurfaceProfileAsset`, `RoutePauseSurfaceEndpoint` | string compare em `Matches` | required | fail-fast | Deve virar `SurfaceIdentity` genérico |
| `overlayRootId` | root visual do overlay | profile + endpoint | string compare + `overlayRoot` reference | required | fail-fast | Bom conceito, preso a pause |
| `activityContentRootId` | root para conteúdo de Activity no pause | profile + endpoint | string compare + `activityContentRoot` | required para content | fail-fast | Bom conceito, nome específico demais |
| `RoutePauseSurfaceSlot.slotId` | slot de conteúdo pause | `RoutePauseSurfaceSlot` | map por string em `BuildSlotMapOrFail` | contribution required/optional | required explode, optional skip | Bom padrão; string frágil |
| `ActivityPauseContentEntry.slotId` | destino de prefab | `ActivityPauseContentProfileAsset` | confronta slot map | per-entry required | fail/skip | Bom mapping content→slot |
| `SurfaceCameraAnchorHost.anchorId` | anchor route camera | host local | `TryResolve(anchorId)` | tracking required; lookAt optional | false reason | Bom, mas camera-specific |
| `ActivityCameraAnchorHost.anchorId` | anchor activity camera | host local | scene-scoped resolver | tracking required; lookAt optional | fail-fast no caller | Bom, mas depende de registration |
| `ActorPresentationContainer.slotKind+slotId` | presentation root/slot | endpoint local | container resolver | por requirement | failed/skipped | Melhor shape para typed slot |
| `PoolableSpawnOriginAnchor.originId` | origem de spawn | child do presentation endpoint | dictionary rebuild | required conforme mode | fail ou fallback | Bom; fallback deve ser policy explícita |
| `VisualRoot` fallback | container default | `ActorPresentationEndpoint` | `TryGetVisualRootFallback` | fallback mode | warning + fallback | Útil, mas perigoso se implícito |

---

### 6. Surface Identity

Padrões de identidade encontrados:

| Identidade | Onde aparece | Chave funcional? | Estabilidade | Recomendação |
|---|---|---:|---|---|
| `RoutePauseSurfaceId` | `RoutePauseSurfaceContracts.cs` | Sim | Média | Reusar conceito como `SurfaceIdentity` |
| `RoutePauseSurfaceRootId` | `RoutePauseSurfaceContracts.cs` | Sim | Média | Separar `SurfaceRootId` genérico |
| `surfaceId` string | profile/endpoint/context | Sim | Média/baixa | Evitar string solta |
| `slotId` string | pause/presentation | Sim | Média | Virar typed id |
| `anchorId` string | camera anchors | Sim | Média | Virar typed id |
| `originId` / `PoolableSpawnOriginId` | poolable spawn origin | Sim | Boa wrapper, string interna | Preservar wrapper |
| `profileId` | pause/presentation/camera profiles | Sim/diagnóstico | Média | Typed profile id |
| `sceneName` | pause context, camera resolver | Sim | Baixa se renomear cena | Usar scene key/scene reference como fonte |
| `GameObject.name` | logs, rig/content names | Diagnóstico | Baixa | Observabilidade apenas |
| transform path | capability logs | Diagnóstico | Baixa | Observabilidade apenas |
| `RouteIdentity`, `RouteOperationId`, `TransitionId`, `RouteSequence` | context/handles | Sim | Boa como runtime identity | Manter separado de surface id |
| `OperationalSurfaceKind` | `OperationalRouteAsset` campo | Hipótese pouco ativa | Incerta | Auditar depois se virar API pública |

Problema principal: há wrappers em alguns pontos, mas o handoff para `SessionActivityRoutePauseSurfaceContext` degrada para strings.

---

### 7. Surface Lifecycle

| Lifecycle | Declara | Resolve | Consome | Limpa | Risco |
|---|---|---|---|---|---|
| Route pause surface scene | `RoutePauseSurfaceProfileAsset` | route plan resolver | Activity pause/content | scene unload pelo route plan | Surface lifecycle misturado com route scene plan |
| Pause overlay root | `RoutePauseSurfaceEndpoint` | `PauseOverlayAdapter` | pause/resume | `SetActive(false)` | consumidor controla visual state |
| Activity content root | `RoutePauseSurfaceEndpoint` | `ActivityPauseContentAdapter` | pause content | destroy instances | endpoint vira content owner |
| Route camera anchors | `SurfaceCameraAnchorHost` | route camera adapter | route camera rig | rig destroy; anchors ficam com cena | discovery duplicado por provider/scan |
| Activity camera anchors | `ActivityCameraAnchorHost` | scene-scoped resolver | activity camera rig | rig destroy; host fica com cena | registration no `Awake` pode falhar cedo |
| Actor presentation containers | `ActorPresentationEndpoint` | container resolver | visual materializer | materializer destroy/keep | bom boundary |
| Poolable spawn origins | `PoolableSpawnOriginAnchor` | presentation endpoint rebuild | projectile/spawn consumer | anchors ficam com owner | fallback pode esconder config errada |

Ordem observada:

1. Route resolve plano.
2. Route adiciona pause surface scene ao load plan.
3. Route passa context para Activity no handoff.
4. Activity usa context para pause overlay/input/content.
5. Consumers materializam conteúdo.
6. Activity/Route release destrói conteúdo específico ou unload de cena remove authored surface.

Riscos:

- orphan content se release não rodar antes de unload;
- stale context se route mudou mas Activity mantém context;
- duplicate endpoint em cena quebra runtime;
- surface scene declarada em local errado é rejeitada, mas só em validação/route resolve;
- roots são strings + referências Unity, não typed references unificadas.

---

### 8. Relação Surface ↔ Route

Route usa Surface de três formas:

1. **Declaração em route asset**
   - `OperationalRouteAsset` possui `SurfacePresentationProfileAsset`, `ActivityPresentationProfileAsset` e `RoutePauseSurfaceProfileAsset`.

2. **Planejamento de cena**
   - `SessionOperationalRoutePlanResolver.AppendRoutePauseSurfaceSceneToLoadPlan` adiciona a scene da pause surface ao load plan.
   - Também impede que pause surface scene seja persistent scene, active route scene ou duplicada em `scenesToLoad`.

3. **Handoff para Activity**
   - `OperationalConsumerEntryAndReadinessStage.ResolveRoutePauseSurfaceContextOrFail` cria `SessionActivityRoutePauseSurfaceContext`.
   - `SessionActivityOperationalRouteConsumerEntryAdapter` encaminha para SessionActivity.

Route deveria possuir:

- route surface set;
- route surface scene ownership;
- route-scoped surface identity;
- handoff de context.

Route deveria apenas registrar contribution quando:

- a surface é local à Activity;
- o consumo é camera/pause/presentation específico.

Route sabe demais quando:

- profile de pause inclui input mode/policy;
- surface profile é acoplado a camera presentation;
- handoff usa campos específicos de pause (`OverlayRootId`, `ActivityContentRootId`) em vez de `SurfaceRootSet`.

---

### 9. Relação Surface ↔ Activity

Activity usa Surface como consumidor:

- pause/resume exige `RoutePauseSurfaceContext`;
- pause overlay usa `PauseOverlayAdapter`;
- pause content usa `ActivityEntryPauseContentStage`;
- input mode usa context para pause/resume;
- activity camera usa `ActivityCameraAnchorHost`;
- actor presentation usa containers locais de actor.

Activity deveria consumir surface de Route quando:

- a surface é route-scoped, como pause overlay surface;
- o conteúdo é activity-specific mas o espaço pertence à Route.

Activity deveria possuir surface local quando:

- anchors estão na activity scene;
- presentation roots pertencem a actors da activity;
- UI targets são activity-local.

Activity sabe demais da Route quando:

- pipeline guarda `_routePauseSurfaceContext`;
- pause command depende diretamente de route surface context;
- adapters de Activity escaneiam scene da Route para achar endpoint.

---

### 10. Relação Surface ↔ Local

Local expõe Surface por components authoring:

- `RoutePauseSurfaceEndpoint`;
- `RoutePauseSurfaceSlot`;
- `SurfaceCameraAnchorHost`;
- `ActivityCameraAnchorHost`;
- `ActorPresentationEndpoint`;
- `ActorPresentationContainer`;
- `PoolableSpawnOriginAnchor`.

Boas características:

- authoring explícito;
- duplicate detection;
- required/optional;
- fail-fast;
- scene-scoped registration no caso de activity camera;
- containers não buscam objeto externo por fallback amplo.

O que deveria virar `LocalSurfaceContribution`:

- endpoint id;
- surface id;
- root ids;
- slots;
- anchors;
- capabilities oferecidas;
- escopo local (`RouteScene`, `ActivityScene`, `ActorRoot`, `RuntimeSpawnedActor`);
- requiredness;
- diagnostics.

Risco atual:

- cada subsystem tem seu próprio modelo de Local Surface.
- não há inventário único de surfaces locais descobertas numa cena.

---

### 11. Relação Surface ↔ RuntimeSpawned

RuntimeSpawned usa Surface de quatro modos:

1. **Prefab instanciado em slot**
   - `RoutePauseSurfaceEndpoint.BindActivityPauseContent` faz `Instantiate(contribution.Prefab, slot.transform, false)`.

2. **Presentation instanciada em root**
   - `UnityActorPresentationMaterializationAdapter.Materialize` instancia `VisualPrefab` em `primarySlot.Container`.

3. **Camera rig usando anchors**
   - `CinemachineRouteCameraDirector` e `CinemachineActivityCameraDirector` instanciam rigs e vinculam targets resolvidos por anchors.

4. **Spawn origin**
   - `ActorPresentationEndpoint` implementa `IPoolableSpawnOriginSurface`;
   - `PoolableSpawnOriginAnchor` expõe origem para conteúdo poolable/runtime-spawned.

Ownership correta:

- Surface possui espaço/anchor/root.
- RuntimeSpawned possui instância materializada.
- Consumer possui binding/handle.
- Lifecycle macro pertence ao owner de escopo (`Route`, `Activity`, actor owner), não ao endpoint local isolado.

Mistura atual:

- pause endpoint destrói conteúdo materializado;
- camera director destrói rig diretamente;
- presentation adapter destrói prefab;
- poolable origin fallback pode trocar origem sem uma policy de surface explícita.

---

### 12. Padrões Bons a Preservar

| Padrão | Onde aparece | Por que preservar | Reinterpretação futura |
|---|---|---|---|
| Endpoint explícito | `RoutePauseSurfaceEndpoint`, `ActorPresentationEndpoint` | Surface visível em cena/prefab | `SurfaceEndpoint` genérico |
| Profile declarativo | `RoutePauseSurfaceProfileAsset`, `SurfacePresentationProfileAsset` | Config separada do runtime | `SurfaceProfile` + consumer profiles |
| Slot authored | `RoutePauseSurfaceSlot`, `ActorPresentationContainer` | Evita busca implícita por hierarchy | `SurfaceSlot` |
| Anchor authored | `SurfaceCameraAnchorHost`, `ActivityCameraAnchorHost`, `PoolableSpawnOriginAnchor` | Contrato claro para targets | `SurfaceAnchor` |
| Duplicate detection | slots, anchors, containers | Evita ambiguidade runtime | obrigatório |
| Required/optional | pause content, presentation slots, camera profiles | Bom authoring UX | policy explícita |
| Context handoff | `SessionActivityRoutePauseSurfaceContext` | Route entrega surface para Activity | `SurfaceContext` genérico |
| Resolver puro | camera requirement resolvers, container resolver | SRP boa | manter |
| Fail-fast | quase todos os paths | Config errada não passa silenciosa | manter |
| Overlay root separado de content root | pause surface | Bom modelo visual/conteúdo | generalizar como root roles |

---

### 13. Padrões Ruins a Redesenhar ou Descartar

| Padrão | Onde aparece | Perigo | Alternativa |
|---|---|---|---|
| Surface acoplada a pause | `RoutePauseSurface*` | Surface vira feature, não contrato | `RouteSurfaceProfile` + `PauseSurfaceConsumerProfile` |
| Discovery duplicado | `PauseOverlayAdapter`, `ActivityPauseContentAdapter` | Bugs divergentes | `SurfaceResolver` compartilhado |
| Strings soltas | `surfaceId`, `slotId`, `anchorId` | Identidade frágil | typed ids |
| Scene name como chave | pause/camera resolvers | Renome de cena quebra | `SceneKeyAsset`/scene reference |
| Endpoint materializa conteúdo | `RoutePauseSurfaceEndpoint` | SRP quebrado | endpoint expõe roots/slots; materializer externo |
| Consumer controla lifetime da surface | pause/camera/presentation | orphan/stale | lifecycle owner por scope |
| Direct `Destroy` espalhado | pause, presentation, camera | cleanup policy ausente | `SurfaceContentHandle.Release()` |
| Fallback visual root | `ActorPresentationEndpoint` | Config errada pode passar | fallback só se policy explícita |
| DependencyManager em `Awake` | `ActivityCameraAnchorHost` | ordem de bootstrap frágil | scene surface scan/registration stage |
| Profile de surface com input policy | `RoutePauseSurfaceProfile` | mistura Surface e Input | separate consumer policy |

---

### 14. Classificação Básico / Intermediário / Avançado

**Básico**

- `SurfaceIdentity`: necessário para qualquer surface simples.
- `SurfaceEndpoint`: endpoint local authored.
- `SurfaceRoot`: root transform/GameObject por papel.
- `SurfaceSlot`: destino para conteúdo.
- `SurfaceAnchor`: transform nomeado.
- `SurfaceResolver`: resolve endpoint/slot/anchor.
- `SurfaceValidation`: required/duplicate/fail-fast.

Desbloqueia: pause simples, UI root, presentation root, camera anchor básico.

**Intermediário**

- `SurfaceProfile`: configuração declarativa.
- `SurfaceSet`: coleção por Route/Activity/Local.
- `SurfaceContext`: handoff entre Route e Activity.
- `SurfaceBindingRequest/Result`: binding de conteúdo.
- `SurfaceContentHandle`: handle de conteúdo materializado.
- `SurfaceLifecyclePolicy`: cleanup por scope.
- `LocalSurfaceContribution`: inventário de surfaces locais.

Desbloqueia: route-owned pause surface, activity-owned anchors, content binding limpo.

**Avançado**

- `RuntimeSurfaceBinding`: RuntimeSpawned em surface.
- `PooledSurfaceContent`: conteúdo pooled dentro de surface.
- `CameraSurfaceConsumer`: camera rig por anchors.
- `PresentationSurfaceConsumer`: visuals por containers.
- `UISurfaceConsumer`: UI/HUD contextual.
- `SurfaceHandoffPolicy`: route/activity/local handoff.
- `SurfaceSnapshot/Restore`: se conteúdo surface precisar persistência.

Desbloqueia: runtime-spawned complexo, camera/presentation/audio/UI integrados sem acoplamento.

---

### 15. Matriz Surface

| Capacidade | Tipo | Arquivos principais | Nível | Preservar | Redesenhar | Descartar | Dependências | Observações |
|---|---|---|---|---|---|---|---|---|
| Route pause profile | surface profile | `...\SessionOperational\Authoring\RoutePauseSurfaceProfileAsset.cs` | Intermediário | profile declarativo | separar pause/input de surface | nomes pause como core | SceneKey, ids | Route-owned |
| Route pause contracts | context/result | `...\SessionOperational\Contracts\RoutePauseSurfaceContracts.cs` | Intermediário | command/result/context | generalizar | strings como API | Route identity | bom handoff |
| Pause endpoint | endpoint/root | `...\SessionActivity\Adapters\RoutePauseSurfaceEndpoint.cs` | Básico/Intermediário | roots/slots/fail-fast | tirar materialização | endpoint como content owner | Unity scene | rico, mas misturado |
| Pause slot | slot | `...\SessionActivity\Adapters\RoutePauseSurfaceSlot.cs` | Básico | marker local | typed id | string solta | Transform | simples |
| Pause content profile | binding profile | `...\SessionActivity\Authoring\ActivityPauseContentProfileAsset.cs` | Intermediário | content→slot mapping | consumer profile genérico | pause como surface | prefab, slot | bom |
| Pause overlay adapter | discovery/binding | `...\SessionActivity\Adapters\PauseOverlayAdapter.cs` | Intermediário | fail-fast | resolver comum | scan duplicado | scene loaded | consumidor |
| Activity pause adapter | discovery/materialization | `...\SessionActivity\Adapters\ActivityPauseContentAdapter.cs` | Intermediário | command/result | resolver comum | scan duplicado | endpoint | consumidor |
| Surface camera anchors | anchor host | `...\CameraPresentation\Authoring\SurfaceCameraAnchorHost.cs` | Básico | anchors/duplicates | generic anchor host | camera-specific naming | Transform | route surface |
| Activity camera anchors | anchor host | `...\CameraPresentation\Authoring\ActivityCameraAnchorHost.cs` | Intermediário | scene-scoped host | registration stage | `Awake` hard dependency | DependencyManager | activity surface |
| Surface camera resolver | binding | `...\CameraPresentation\Runtime\SurfaceCameraPresentationRequirementResolver.cs` | Avançado | pure resolver | generic surface resolver | camera owns surface language | profile, anchor host | bom SRP |
| Activity camera resolver | binding | `...\CameraPresentation\Runtime\ActivityCameraPresentationRequirementResolver.cs` | Avançado | pure resolver | generic anchor request | camera-specific profile | profile, host | bom SRP |
| Actor presentation endpoint | endpoint/root/anchor | `...\Actors\Presentation\Runtime\ActorPresentationEndpoint.cs` | Intermediário | endpoint local explícito | separar spawn-origin surface | fallback implícito | containers, anchors | melhor modelo local |
| Actor presentation container | slot/root | `...\Actors\Presentation\Runtime\ActorPresentationContainer.cs` | Básico | slot kind + id + transform | typed ids | string id | Transform | bom |
| Container resolver | validation/binding | `...\Actors\Presentation\Runtime\ActorPresentationContainerResolver.cs` | Básico | resolver puro | ids tipados | string key | endpoint | bom |
| Poolable spawn origin | anchor | `...\Actors\Presentation\Runtime\PoolableSpawnOriginAnchor.cs` | Avançado | origin typed wrapper | policy para fallback | unknown/default solto | Transform | RuntimeSpawned |
| Presentation materializer | runtime binding | `...\Actors\Presentation\Adapters\UnityActorPresentationMaterializationAdapter.cs` | Avançado | materialize/release result | release policy central | direct destroy | endpoint plan | consumidor |
| Route camera adapter | surface discovery | `...\SessionOperational\Adapters\SessionOperationalRouteCameraAdapter.cs` | Avançado | provider + scene scan | mover discovery para SurfaceResolver | camera-specific discovery | scene/provider | consumidor |
| UI target binding | UI consumer | `...\Actors\Attributes\UI\ActivityEntryActorAttributeUiTargetResolver.cs` | Avançado | target resolver | não tratar como surface core | UI como Surface sem root | actor registry | consumidor |

---

### 16. Dependências e Ordem Recomendada

1. Definir `SurfaceIdentity`, `SurfaceSlotId`, `SurfaceAnchorId`, `SurfaceRootId`.
2. Definir `SurfaceEndpoint` como contrato local sem conteúdo específico.
3. Definir `SurfaceRoot` com role: overlay, content, visual, camera, spawn origin.
4. Definir `SurfaceSlot` e `SurfaceAnchor`.
5. Definir validação de duplicidade e required/optional.
6. Definir `SurfaceSet` por escopo.
7. Definir `LocalSurfaceContribution`.
8. Definir `SurfaceResolver` por scene/local owner.
9. Definir `SurfaceContext` para handoff Route → Activity.
10. Definir `SurfaceBindingRequest` e `SurfaceBindingResult`.
11. Definir `SurfaceContentHandle`.
12. Definir `SurfaceLifecyclePolicy`.
13. Integrar pause como consumidor.
14. Integrar camera como consumidor.
15. Integrar presentation como consumidor.
16. Integrar RuntimeSpawned como conteúdo.
17. Avaliar UI roots em rodada própria.

---

### 17. Recomendações para Framework Futuro

Deveria existir:

- `SurfaceIdentity`;
- `SurfaceEndpoint`;
- `SurfaceProfile`;
- `SurfaceSet`;
- `SurfaceSlot`;
- `SurfaceAnchor`;
- `SurfaceRoot`;
- `SurfaceBindingRequest`;
- `SurfaceBindingResult`;
- `SurfaceContentHandle`;
- `SurfaceLifecyclePolicy`;
- `RouteSurfaceSet`;
- `ActivitySurfaceSet`;
- `LocalSurfaceContribution`;
- `RuntimeSurfaceBinding`.

Não deveria existir:

- `PauseSurfaceEndpoint` como contrato base;
- `CameraSurface` como contrato base;
- `SurfaceManager`;
- strings públicas como chave funcional;
- endpoint local destruindo conteúdo de consumidor;
- resolver duplicado por consumidor;
- fallback silencioso para root/anchor/slot;
- scene name como identidade principal;
- `GameObject.name` ou transform path como chave.

Deve ficar para ciclos futuros:

- pooling dentro de surface;
- snapshot/restore de surface content;
- UI surface formal;
- audio surface, se houver espacialização/emitters;
- visual authoring UX;
- Surface diagnostics dashboard;
- cross-scene surface registry.

---

### 18. Perguntas Abertas para Próxima Auditoria

- Surface deve ser sempre authored ou pode ser runtime-spawned?
- Route-owned surface pode viver na active route scene ou deve sempre ser scene separada?
- Activity pode consumir surface de Route diretamente ou deve receber somente handle/context?
- `overlayRoot` e `activityContentRoot` são roles genéricas ou específicas de pause?
- `SurfaceCameraAnchorHost` deveria registrar por scene como `ActivityCameraAnchorHost`?
- Fallback para `visual.root` deve existir ou ser proibido por padrão?
- `OperationalSurfaceKind` ainda é conceito ativo ou resíduo?
- UI/HUD precisa de `UISurface` própria?
- Surface content deve ser salvo/restaurado ou sempre rematerializado?
- Quem é owner do cleanup quando a surface é Route-owned e o conteúdo é Activity-owned?
- Como Inspector deve nomear `Surface`, `Slot`, `Anchor` sem jargão técnico excessivo?

---

### 19. Itens que Devem Virar ADR

- ADR: Surface como contrato de espaço, não subsistema.
- ADR: Separação entre Surface owner e Surface consumer.
- ADR: Identidade tipada de surface/slot/anchor/root.
- ADR: Surface roots e root roles.
- ADR: RouteSurfaceSet e handoff para Activity.
- ADR: ActivitySurfaceSet e surfaces locais.
- ADR: LocalSurfaceContribution e discovery em cena/prefab.
- ADR: Binding de conteúdo em Surface.
- ADR: Lifecycle e cleanup de SurfaceContent.
- ADR: RuntimeSpawned dentro de Surface.
- ADR: Camera como consumer de Surface/Anchor.
- ADR: Pause como consumer de Surface.
- ADR: Presentation como consumer de Surface/Container.
- ADR: Política de fallback para anchors/roots/slots.
- ADR: Diferença entre identidade funcional e diagnóstico (`GameObject.name`, transform path, scene name).

---

## 18. Auditoria Profunda — CrossCutting / Subsystem Consumers

### 1. Resumo Executivo

`NewScripts` contém vários subsistemas transversais úteis como inventário arquitetural, mas eles não devem virar o centro do framework futuro. Camera, Audio, Input, Save, Pause, Actor, Projectile, Damage, Attributes, Pooling, Logging e QA aparecem como consumidores de lifecycle, surface, materialização, runtime identity, capability inventory e composition.

O padrão mais valioso é a separação por contratos, requests, results, facts, stages e adapters. Isso aparece bem em `SessionOperational`, `SessionActivity`, Camera, Input, Audio, Save e Actor setup.

O maior risco recorrente é consumidores acessando composition/global services diretamente via `DependencyManager`, `RuntimeConfigRegistry`, scene scan ou serviços globais. Isso funciona no MVP, mas não deve ser copiado como arquitetura pública.

A síntese correta é: o framework futuro precisa oferecer contratos, identidade, diagnostics, lifecycle hooks, surface/runtime spawned primitives e extension points. Os subsistemas devem plugar nesses pontos como consumidores.

### 2. Inventário de Subsistemas Consumidores

| Subsistema | Evidência principal | Escopos consumidos | Classificação |
|---|---|---|---|
| Camera | `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\...` | Route, Activity, Surface, anchors | Consumidor visual |
| Audio | `...\SessionOperational\Adapters\AudioAdapter.cs`, `...\AudioRuntime\...` | Route, RuntimeConfig, Pooling, global services | Consumidor audiovisual |
| Input | `...\InputModes\Runtime\InputModeService.cs`, `...\SessionOperational\Pipeline\OperationalInputPreparationStage.cs` | Route, Activity, Player Actor, Pause | Consumidor de modo/runtime |
| Save/Snapshot | `...\SaveRuntime\...`, `...\SessionOperational\Adapters\SessionOperationalActivitySaveAdapter.cs` | Activity, Progression, Snapshot, Route | Consumidor persistente |
| Pause | `...\SessionOperational\Contracts\RoutePauseSurfaceContracts.cs`, `...\SessionActivity\Adapters\RoutePauseSurfaceEndpoint.cs` | Surface, Input, Activity content | Consumidor de surface |
| Actor/Player | `...\Actors\Players\ActivitySetup\...` | Activity, participants, runtime identity | Consumidor materializado |
| Attributes/Damage | `...\Actors\Attributes\...`, `...\Actors\Damage\...` | Actor identity, UI, impact, facts | Consumidores gameplay |
| Projectile | `...\Actors\Projectile\Runtime\...` | RuntimeSpawned, Pooling, Actor command, Audio | Consumidor avançado |
| Pooling | `...\Foundation\Platform\Pooling\...` | RuntimeSpawned, Activity scope, Global scope | Infra técnica consumida |
| Logging/Diagnostics | `...\Foundation\Core\Logging\...`, `...\SessionOperational\Pipeline\OperationalFactRecorder.cs` | Todos os escopos | Infra transversal |
| QA/Debug | `...\SessionActivity\Pipeline\SessionActivityDebugPanel.cs`, `...\Actors\...\QA\...` | Pipeline, Actor, Pooling, Damage | Ferramenta opcional |
| Runtime config | `...\Foundation\Platform\RuntimeMode\...` | Audio, Save, bootstrap, composition | Infra de configuração |
| Event bus | `...\Foundation\Core\Events\...` | Input, operational events | Infra de comunicação |
| Typed IDs / IDREF | Vários `*Id`, `*Identity`, `*RuntimeId` | Todos os escopos | Primitivo transversal |

### 3. Separação entre Core e Consumidores

Core futuro deve possuir:

- Identidade canônica de lifecycle, route, activity, surface e runtime spawned.
- Contracts de lifecycle, facts, diagnostics e contribution sets.
- Composition explícita, sem service locator como API pública.
- Configuração obrigatória com fail-fast.
- Extension points para consumidores.
- Diagnostics e validation como infraestrutura.

Consumidores devem ser mantidos fora do core inicial:

- Camera concreta, especialmente Cinemachine.
- Audio BGM/SFX concreto.
- Input mode concreto e `PlayerInput`.
- Save backend concreto.
- Pause surface concreta.
- Player actor, projectile, damage, attributes.
- QA/debug panels.
- Concrete pooling policies específicas de gameplay.

O framework não deve nascer como soma desses subsistemas. Ele deve nascer como superfície de integração para que esses subsistemas possam existir sem capturar ownership.

### 4. Camera como Consumidor

Camera aparece como consumidor de Surface, Route e Activity.

Evidências:

- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Authoring\SurfacePresentationProfileAsset.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Authoring\SurfaceCameraAnchorHost.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Runtime\SurfaceCameraPresentationRequirementResolver.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Adapters\SessionOperationalRouteCameraAdapter.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\CameraPresentation\Runtime\CinemachineRouteCameraDirector.cs`

Padrões bons:

- Profiles authoring descrevem intenção: prefab, anchors, priority, timing.
- Anchor hosts separam authoring scene-bound de resolução runtime.
- Adapter operacional converte route/activity lifecycle em requirement de camera.
- Director concreto fica isolado como executor visual.

Riscos:

- `ActivityCameraAnchorHost` registra dependência em `Awake`, misturando scene object com composition.
- Adapters ainda fazem fallback por scene scan.
- Cinemachine não deve aparecer no core.
- Instantiate/destroy de rigs deve ser detalhe de adapter, não contrato de framework.

Owner correto futuro: módulo consumidor `Camera Presentation`, integrado via lifecycle/surface contracts.

O que não mudar agora: não promover Cinemachine, rig prefab ou anchor host concreto para core.

### 5. Audio como Consumidor

Audio é consumidor de Route lifecycle, RuntimeConfig, Pooling e serviços globais.

Evidências:

- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Contracts\OperationalRouteAudioContracts.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Pipeline\OperationalRouteAudioStage.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Adapters\AudioAdapter.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\AudioRuntime\Playback\Runtime\Core\IGlobalAudioService.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\AudioRuntime\Playback\Bootstrap\AudioSfxPoolPreparationStage.cs`

Padrões bons:

- `IOperationalRouteAudioPort` isola pipeline de execução concreta.
- `OperationalRouteAudioStage` registra facts e exige resultado completo.
- Audio pool preparation trata pool como requisito de runtime, não como detalhe solto.
- Resultado de audio distingue completed, skipped e failure.

Riscos:

- `AudioAdapter` usa `DependencyManager.Provider.TryGetGlobal`, acoplando adapter a service locator.
- Adapter consulta `RuntimeConfigRegistry` diretamente.
- Route audio conhece tipos concretos de cue e modo de execução.
- SFX pooling e route audio podem capturar lifecycle se não forem mantidos como consumidores.

Owner correto futuro: módulo consumidor `Audio`, com port explícito injetado pelo framework.

O que não mudar agora: não transformar `IGlobalAudioService`, cue assets ou pool preparation em core.

### 6. Input como Consumidor

Input aparece como consumidor de Route, Activity, Player Actor e Pause.

Evidências:

- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Contracts\OperationalInputModeRequestContracts.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Pipeline\OperationalInputPreparationStage.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\InputModes\Runtime\InputModeService.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\InputModes\Runtime\InputModeCoordinator.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Players\ActivitySetup\PlayerInputBindingAdapter.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Runtime\ActorCommandContracts.cs`

Padrões bons:

- `OperationalInputModeRequest` preserva identidade operacional e política.
- Stage prepara facts antes de solicitar modo inicial.
- `InputModeService` concentra aplicação de action maps.
- Actor command envelope separa input bruto de comando gameplay.

Riscos:

- `InputModeCoordinator` usa event bus global e service locator.
- `InputModeService` usa scene-wide lookup de `PlayerInput`.
- Action map names ainda são strings.
- `PlayerInputBindingAdapter` reconfigura `PlayerInput.actions` e consulta `IInputModeService` global.
- `ActorCommandBindingAdapter` conhece projectile, pool e audio, acoplando input/actor a subsystem específico.

Owner correto futuro: `Input` como consumidor de lifecycle + actor command binding, não dono de lifecycle.

O que não mudar agora: não copiar `InputModeCoordinator` global como padrão de framework.

### 7. Save / Snapshot / Progression como Consumidor

Save aparece como consumidor de Activity lifecycle e snapshot payload.

Evidências:

- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SaveRuntime\Contracts\ISaveService.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SaveRuntime\Contracts\ISaveBackend.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SaveRuntime\Core\SaveCoreService.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SaveRuntime\Models\SaveIdentity.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Adapters\SessionOperationalActivitySaveAdapter.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Contracts\ActivityCapabilitySnapshotEnvelopeContracts.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Contracts\SessionActivitySnapshotPayloadContracts.cs`

Padrões bons:

- `ISaveService` e `ISaveBackend` separam API e armazenamento.
- `SaveAddress` e metadata validada reduzem risco de load estrangeiro.
- Snapshot envelope preserva owner kind, owner id, capability id, schema e payload.
- Save on enter/exit opera como adapter de lifecycle, não como core da activity.

Riscos:

- `SaveIdentity` usa strings para profile e slot.
- `SaveCoreService` usa dicionário string/string e metadata string.
- `SessionOperationalActivitySaveAdapter` detecta payload kind por busca textual em JSON.
- Chaves como `activity:{identity}` fabricam namespace por string.
- Progression e Snapshot ainda misturam persistência, identity e payload schema.

Owner correto futuro: `Save/Snapshot` como consumidor de lifecycle e capability snapshots.

O que não mudar agora: não tornar o formato string dictionary nem payload detection textual em contrato público.

### 8. Pause como Consumidor

Pause é consumidor de Surface, Input e Activity content.

Evidências:

- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Contracts\RoutePauseSurfaceContracts.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Authoring\RoutePauseSurfaceProfileAsset.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Pipeline\SessionOperationalRoutePlanResolver.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Contracts\SessionActivityEntryHandoff.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Adapters\RoutePauseSurfaceEndpoint.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Pipeline\Stages\ActivityEntryPauseContentStage.cs`

Padrões bons:

- Route plan inclui additive scene de pause surface.
- `RoutePauseSurfaceProfileAsset` explicita surface id, root ids, preload policy e input modes.
- Handoff leva contexto de route para activity.
- Pause/resume aplica ordem visual/input em pipeline.

Riscos:

- `RoutePauseSurfaceEndpoint` acumula overlay controller, slot resolver, content materializer e lifecycle owner.
- Adapters resolvem endpoint via scene scan.
- Strings ainda aparecem em surface id, scene name, root id e slot id.
- Pipeline de Activity conhece detalhes demais do pause surface.

Owner correto futuro: Pause como consumidor de `Surface` + `Input Mode` + `Activity Content`.

O que não mudar agora: não transformar pause overlay concreto em core.

### 9. Actor / Player / Attributes / Damage como Consumidores

Actor e Player são consumidores de Activity lifecycle, participant identity e runtime materialization. Attributes e Damage são consumidores adicionais de Actor identity.

Evidências:

- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Players\ActivitySetup\PlayerActorMaterializationAdapter.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Players\ActivitySetup\PlayerActorSetupContracts.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Players\ActivitySetup\ActivityPlayerActorRegistry.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Runtime\ActorCommandContracts.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Attributes\Runtime\ActorAttributeEndpoint.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Damage\Runtime\ActorDamageableEndpoint.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Impact\Runtime\ActorImpactEndpoint.cs`

Padrões bons:

- Actor materialization usa command/result/records.
- Runtime handles carregam actor id, instance id e participant relation.
- Registry guarda handles ativos sem necessariamente ser lifecycle owner.
- Attribute endpoint e damage endpoint modelam capabilities concretas.
- Command envelope evita tratar input raw como ação gameplay.

Riscos:

- Player actor adapter materializa roots e bindings em excesso.
- Actor setup, input binding, projectile binding e command dispatch estão próximos demais.
- `ActorCommandKind` hardcoded limita extensibilidade.
- Attributes/Damage/Impact podem capturar Actor core se não forem tratados como capabilities.
- UI binding de attributes depende fortemente do contexto de Activity.

Owner correto futuro: `Actor Runtime` mínimo como consumidor de materialização; Attributes/Damage como capabilities opcionais.

O que não mudar agora: não promover Player, Damage, Attributes ou Impact para framework core.

### 10. Projectile / Pooling como Consumidores

Pooling é infraestrutura técnica consumida por RuntimeSpawned. Projectile é consumidor avançado de Actor command, Pooling, Impact, Damage e Audio.

Evidências:

- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Foundation\Platform\Pooling\Contracts\IPoolService.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Foundation\Platform\Pooling\Config\PoolDefinitionAsset.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Foundation\Platform\Pooling\Runtime\PoolService.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Pipeline\Stages\ActivityEntryPoolPreparationStage.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Pipeline\OperationalActivityPoolReleaseStage.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Projectile\Runtime\PooledActorProjectileSpawnAdapter.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Projectile\Runtime\ActorProjectileSpawnRuntimeState.cs`

Padrões bons:

- Pool definitions têm scope, registration mode, prewarm e limits.
- Activity entry prepara pools por dependency providers.
- Operational exit libera activity pools.
- Runtime spawned state possui checagens stale/foreign.
- Projectile spawn adapter faz rollback ao falhar.

Riscos:

- `PoolService` cria global root e controla lifecycle global.
- Pool identity por asset reference não é suficiente como contrato público.
- Projectile adapter é grande e mistura spawn, motion, impact, layer, pool return e cleanup.
- Generic actor command binding instancia executor de projectile com pool/audio.

Owner correto futuro: Pooling como technical package/infra; Projectile como módulo consumidor avançado.

O que não mudar agora: não fazer RuntimeSpawned depender diretamente de projectile.

### 11. Logging / Diagnostics / QA

Logging, diagnostics e QA são transversais, mas não devem virar gameplay architecture.

Evidências:

- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Foundation\Core\Logging\DebugUtility.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Foundation\Core\Logging\HardFailFastH1.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionOperational\Pipeline\OperationalFactRecorder.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Pipeline\SessionActivityDebugPanel.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\SessionActivity\Pipeline\SessionActivityHostQaCommandSurface.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Actors\Attributes\QA\ActorAttributeRuntimeCommandQaProbe.cs`
- `C:\Projetos\GameJam2025\Assets\_ImmersiveGames\NewScripts\Foundation\Platform\Pooling\QA\PoolingQaContextMenuDriver.cs`

Padrões bons:

- Fail-fast explícito.
- Fact recorder cria trilha auditável por stage.
- QA probes facilitam validação manual dirigida.
- Debug panels expõem estado operacional.

Riscos:

- Logs podem virar contrato implícito.
- QA depende de classes concretas de pipeline/host.
- `DebugUtility` centralizado pode virar global invisível.
- Context menu QA não substitui contrato de validation.

Owner correto futuro: diagnostics core mínimo; QA/debug como pacote/ferramenta opcional.

O que não mudar agora: não tratar painéis QA como parte obrigatória do runtime.

### 12. Identidade e Referências Transversais

| Família | Uso | Estado |
|---|---|---|
| `SessionOperationalIdentity` | Route/operation/transition/sequence | Bom conceito; deve ser formalizado |
| `SessionActivityIdentity` | Activity lifecycle | Bom conceito; precisa boundary claro |
| `RoutePauseSurfaceId`, `RoutePauseSurfaceRootId` | Pause surface | Útil, mas ainda convive com strings |
| `PlayerSlotId`, `SessionParticipantId` | Player/participants/input | Útil para Actor consumers |
| `ActorId`, `ActorInstanceRuntimeId` | Runtime actor | Essencial para spawned/actor capabilities |
| `ActorCommandId`, `ActorCommandBindingId` | Input to actor command | Bom envelope; enum hardcoded é risco |
| `SaveIdentity`, `SaveAddress` | Persistência | Conceito útil; strings internas são risco |
| `ActivityCapabilitySnapshotRecord` | Snapshot capability | Forte candidato conceitual |
| `ActorAttributeId` | Attributes | Consumidor específico |
| `PoolDefinitionAsset` identity | Pooling | Funcional no MVP; não suficiente como ID público |
| Anchor/slot/root ids | Camera/Pause/Surface | Precisam virar typed references consistentes |

Regra para o futuro: identidade deve ser explícita, tipada e de domínio único. Não comparar IDs de domínios diferentes. Não fabricar identidade por string concatenada.

### 13. Padrões Bons a Preservar

- Contracts com request/result claros.
- Ports/adapters entre pipeline e subsistema.
- Facts antes de side effects sensíveis.
- Fail-fast para config obrigatória.
- Authoring profiles separados de runtime execution.
- Stale/foreign checks em handles e bindings.
- Runtime handles explícitos.
- Snapshot envelopes com schema e owner.
- Capability records em vez de dependência implícita.
- Route/Activity handoff explícito.
- Pool scopes por Activity/Global.
- QA como ferramenta manual separada do contrato runtime.

### 14. Padrões Ruins a Redesenhar ou Descartar

- Service locator em adapters e coordinators.
- Scene scan duplicado como resolução padrão.
- Strings para identity, payload kind, slot/root/scene/action map sem typed boundary.
- Pipeline conhecendo detalhes concretos de subsistemas.
- Generic adapters instanciando executores específicos.
- Logs como fonte de verdade.
- Global roots criados implicitamente por infra.
- Hardcoded commands como `Move` e `FirePrimary` em contratos genéricos.
- Payload detection por busca textual.
- Fallback silencioso ou comportamento state-only não declarado.
- Monólitos escondidos atrás de nomes genéricos como adapter/endpoint.

### 15. Matriz CrossCutting

| Subsistema | Core? | Consumidor? | Reutilizável? | Risco principal | Ação futura |
|---|---:|---:|---:|---|---|
| Camera | Não | Sim | Médio | Concrete Cinemachine/scene scan | Adapter opcional |
| Audio | Não | Sim | Médio | Service locator/runtime config direto | Port explícito |
| Input | Não | Sim | Alto | Global scan/action map strings | Input consumer contract |
| Save/Snapshot | Parcial | Sim | Alto | Payload string/identity string | Snapshot contract |
| Pause | Não | Sim | Médio | Surface endpoint acumulando papéis | Surface consumer |
| Actor/Player | Não | Sim | Alto | Player-specific core leak | Actor runtime boundary |
| Attributes | Não | Sim | Médio | UI/activity coupling | Capability module |
| Damage/Impact | Não | Sim | Médio | Entanglement with projectile | Capability module |
| Projectile | Não | Sim | Baixo/Médio | Monolithic spawn adapter | Advanced consumer |
| Pooling | Infra | Sim | Alto | Global lifecycle hidden | Technical package |
| Logging | Infra | Sim | Alto | Global debug utility | Diagnostics core mínimo |
| QA | Não | Sim | Médio | Concrete host dependency | Tooling package |
| Runtime config | Infra | Sim | Alto | Registry global | Explicit config context |
| Event bus | Infra | Sim | Alto | Global event behavior | Scoped event contract |
| Typed IDs | Sim | Sim | Alto | Mixed string domains | Core identity ADR |

### 16. Ordem Recomendada de Integração Futura

1. Identidade, references e diagnostics mínimos.
2. Lifecycle consumer contracts: route, activity, surface, runtime spawned.
3. Composition/config explícitas, sem service locator como API pública.
4. Capability inventory e contribution sets.
5. Input mode como primeiro consumidor intermediário.
6. Snapshot/save como consumidor de lifecycle.
7. Surface/Pause como consumidor de route/activity handoff.
8. Camera como presentation consumer.
9. Pooling técnico e runtime spawned primitives.
10. Actor materialization básica.
11. Audio como consumidor route/activity/spawned.
12. Projectile, damage, impact e attributes como consumidores avançados.
13. QA/debug panels como tooling opcional.

Essa ordem reduz risco porque estabiliza identity/lifecycle antes de plugar subsistemas concretos.

### 17. Recomendações para Framework Futuro

O framework futuro deve expor conceitos como:

- `LifecycleConsumer`
- `RouteConsumer`
- `ActivityConsumer`
- `SurfaceConsumer`
- `RuntimeSpawnedConsumer`
- `CapabilityConsumer`
- `CapabilitySnapshot`
- `FrameworkFact`
- `DiagnosticsSink`
- `TypedReference`
- `RuntimeConfigContext`
- `CompositionContext`

O framework futuro não deve expor como core:

- Cinemachine directors.
- Global audio services.
- Concrete `PlayerInput`.
- Projectile firing.
- Damage/impact endpoints.
- Pause overlay endpoint.
- Save backend concreto.
- QA panels.
- Scene scan helpers como mecanismo principal.
- `DependencyManager.Provider` como dependência pública de consumers.

A regra arquitetural deve ser: subsistemas declaram requisitos e recebem contexto; não descobrem o mundo sozinhos.

### 18. Perguntas Abertas para Síntese Final do NewScripts

- Qual é o menor conjunto de lifecycle events que consumers realmente precisam?
- `CapabilitySnapshot` deve ser core ou módulo intermediário?
- `Pooling` fica como technical package independente ou infra interna do framework?
- `Input Mode` é conceito de framework ou adapter Unity/InputSystem?
- `Pause Surface` é caso específico ou primeira forma de `Surface Consumer`?
- `Actor` deve existir como conceito genérico do framework ou apenas como consumer sample?
- Como separar `RuntimeSpawned` genérico de projectile/damage/impact?
- Qual formato canônico substitui payload string em save snapshots?
- Qual é a política para typed IDs authoring vs runtime?
- Diagnostics devem registrar facts estruturados, logs humanos ou ambos?

### 19. Itens que Devem Virar ADR

- ADR: Core vs Consumer boundary.
- ADR: Typed identity and cross-domain reference policy.
- ADR: Lifecycle consumer contract model.
- ADR: Capability inventory and contribution set model.
- ADR: Snapshot/save payload model.
- ADR: Surface consumer model.
- ADR: RuntimeSpawned and pooling ownership.
- ADR: Input mode ownership.
- ADR: Diagnostics/facts/logging boundary.
- ADR: Service locator prohibition for public consumers.
- ADR: Scene discovery policy.
- ADR: Optional subsystem integration order.
- ADR: QA/debug tooling outside runtime core.
