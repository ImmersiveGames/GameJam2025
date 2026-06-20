# Raio X — Package `com.immersive.framework`

> Documento consolidado da auditoria isolada do package atual.
> Esta versão não compara com `NewScripts`; a comparação fica para a próxima etapa.

## Status

| Bloco | Status | Observação |
|---|---|---|
| Package atual | Consolidado | Auditoria por leitura estática registrada. |
| NewScripts | Consolidado em documento próprio | Usar apenas como referência na próxima etapa. |
| Comparação | Não iniciada | Deve ser feita depois dos dois raios X isolados. |

## Leitura recomendada

1. Resumo Executivo
2. Dívidas e Riscos Internos
3. Matriz por Escopo
4. Síntese do Package Atual
5. Itens que Devem Virar ADR

---

# Auditoria Profunda — Package `com.immersive.framework`

## 1. Resumo Executivo

Auditei somente o package atual `C:\Projetos\My project\Packages\com.immersive.framework`, sem comparar com `NewScripts` e sem alterar arquivos.

O package já é mais do que um bootstrap mínimo: ele contém `Game Application`, settings, bootstrap runtime, `FrameworkRuntimeHost`, `GameFlowRuntime`, `RouteLifecycleRuntime`, `SceneLifecycleRuntime`, `ActivityFlowRuntime`, triggers de request, bindings locais de Route/Activity, ContentFlow baseline, CameraFlow, QA Canvas, validação editor-only, inspectors e ADRs.

O centro arquitetural atual é:

```text
ImmersiveFrameworkBootstrap
  -> FrameworkRuntimeHost
    -> GameFlowRuntime
      -> RouteLifecycleRuntime
        -> SceneLifecycleRuntime
        -> ActivityFlowRuntime
          -> ActivityContentRuntime
```

Principais achados:

| Severidade | Achado | Evidência |
|---|---|---|
| High | `RouteContentRuntime` existe, mas não está conectado ao fluxo real de Route. | `RouteContentRuntime` tem `EnterRouteContent/ExitRouteContent`; `RouteLifecycleRuntime` não instancia nem chama esse runtime. |
| High | `CameraFlow` está ativo no package apesar de documentação/ADR congelarem CameraFlow fora do baseline atual. | `Runtime/CameraFlow/*`, dependency `com.unity.cinemachine`; README e ADR-0005 dizem que CameraFlow fica fora do trilho atual. |
| Medium | `ContentFlow` expõe contratos públicos antes de consumidores reais suficientes. | `IFrameworkContentMaterializer`, `IFrameworkContentContribution`, `FrameworkContentContributionMarker` existem, mas não há materializer ativo nem discovery/consumer real. |
| Medium | `ActivityContentBinding` é funcional, mas continua sendo Local/visibility adapter, não materialização canônica. | ADR-0002 e runtime confirmam `GameObject.SetActive`. |
| Medium | Runtime assembly é único e Unity-bound; não há runtime puro com `noEngineReferences: true`. | `Immersive.Framework.Runtime.asmdef` usa `noEngineReferences: false` e referencia `Unity.Cinemachine`. |
| Low/Medium | Editor validation cobre o baseline inicial, mas não cobre Route Content Binding, CameraFlow, Route Content Profile adicional nem ContentFlow contributions. | `FrameworkAuthoringValidator` valida settings, app, route, activity e `ActivityContentBinding`. |

Não rodei Unity, build, playmode, batchmode ou smoke, conforme instrução.

## 2. Mapa Estrutural do Package

| Caminho | Responsabilidade aparente | Principais arquivos/classes | Tipo | Dependências | Maturidade | Riscos |
|---|---|---|---|---|---|---|
| `C:\Projetos\My project\Packages\com.immersive.framework\package.json` | Manifesto UPM | package metadata/deps | package | `foundation`, `logging`, `cinemachine` | Parcial | Git deps não estão pinadas por tag; `pooling` citado no README, mas não é dependência ativa. |
| `...\Runtime\Bootstrap` | Entrada runtime e validação mínima | `ImmersiveFrameworkBootstrap`, `FrameworkBootValidator`, `FrameworkBootResult` | Runtime | Unity `Resources`, Authoring, Diagnostics | Sólido para boot mínimo | `async void` via `RuntimeInitializeOnLoadMethod`; erro vira log, não contrato chamável. |
| `...\Runtime\ApplicationLifecycle` | Runtime persistente de sessão/app | `FrameworkRuntimeHost`, `FrameworkRuntimeState` | Runtime/Session | GameFlow, Authoring, Diagnostics | Parcial sólido | `_current` estático é acesso global controlado, mas ainda é uma autoridade global. |
| `...\Runtime\GameFlow` | Entrada de requests Route/Activity e trigger authored | `GameFlowRuntime`, `RouteRequestTrigger`, `ActivityRequestTrigger`, result/event types, UnityEvent bridges | Runtime/request | Foundation Events, RuntimeHost, RouteLifecycle | Sólido para request básico | Result events são por trigger instance, não por runtime global; bom para boundary local, limitado para observabilidade global. |
| `...\Runtime\RouteLifecycle` | Route identity, primary scene, route content set, route local callbacks | `RouteLifecycleRuntime`, `RouteContentSet`, `RouteContentBinding`, `RouteContentRuntime`, route events | Runtime/lifecycle/local | SceneLifecycle, ActivityFlow, ContentFlow | Misto | `RouteContentRuntime` implementado mas inativo no fluxo. |
| `...\Runtime\SceneLifecycle` | Load/activate primary scene | `SceneLifecycleRuntime`, `SceneLifecycleLoadResult` | Runtime/scene loading | Unity SceneManagement | Sólido mínimo | Apenas `LoadSceneMode.Single`; sem additive/release. |
| `...\Runtime\ActivityFlow` | Activity identity, local visibility, lifecycle callbacks | `ActivityFlowRuntime`, `ActivityContentRuntime`, `ActivityContentBinding`, behaviours/events | Runtime/lifecycle/local | Foundation Events, Unity | Sólido mínimo | Discovery por `FindObjectsByType`; binding local ainda não é content materialization canônica. |
| `...\Runtime\ContentFlow` | Linguagem comum de materialização | scopes/kinds/handles/sets/materializer/contribution marker | Runtime/API | Unity para marker | Parcial/experimental | APIs públicas sem consumidores reais suficientes. |
| `...\Runtime\CameraFlow` | Autoridade de câmera Cinemachine | `FrameworkCameraAuthority`, `FrameworkCameraOutputRig`, `FrameworkCinemachineCameraBinding` | Runtime/subsystem | Cinemachine, Foundation Events, Route/Activity callbacks | Ambíguo | Contradiz freeze docs; route-scoped cameras dependem de `RouteContentRuntime`, que está inativo. |
| `...\Runtime\Diagnostics` | Logging wrapper e QA manual | `FrameworkLogger`, `FrameworkQaCanvas` | Diagnostics/QA runtime | Logging, Unity IMGUI | Parcial útil | QA Canvas é public runtime component, não Editor-only; risco de produto se `allowInPlayerBuild` for usado. |
| `...\Runtime\Authoring` | Assets públicos | `GameApplicationAsset`, `RouteAsset`, `ActivityAsset`, `RouteContentProfileAsset`, settings | Authoring runtime | Unity ScriptableObject | Sólido para baseline | Route profile é planning-only; Activity é identidade sem content profile. |
| `...\Editor` | Project Settings, inspectors, validation | settings provider, custom editors, `FrameworkAuthoringValidator` | Editor-only | UnityEditor, Runtime asmdef | Sólido parcial | Namespace aparece como `Immersive.Framework.Editor.Editor.*`; validação ainda estreita. |
| `...\Documentation~` | README, guides, ADRs | ADR-0001..0005, HTML guides | Docs/ADR | N/A | Boa, mas inconsistente | Docs congelam CameraFlow fora do trilho enquanto código possui CameraFlow ativo. |

## 3. Inventário de Capacidades Existentes

| Capacidade | O que permite fazer | Arquivos/classes | Escopo | Tipo | Status | Risco |
|---|---|---|---|---|---|---|
| Framework boot | Resolver settings/app e iniciar Game Flow | `ImmersiveFrameworkBootstrap`, `FrameworkBootValidator` | Session | bootstrap | Sólido | Depende de `Resources.Load`. |
| Active Game Application | Declarar app raiz, startup route e validation mode | `GameApplicationAsset`, `ImmersiveFrameworkSettingsAsset` | Session | authoring/state | Sólido | `ValidationMode` existe, mas quase não altera comportamento real. |
| Runtime host | Manter runtime persistente e estado atual | `FrameworkRuntimeHost`, `FrameworkRuntimeState` | Session | lifecycle/state | Parcial | `_current` global controlado. |
| Game Flow | Aceitar requests Route/Activity e delegar | `GameFlowRuntime` | CrossCutting/Route/Activity | request/trigger | Sólido mínimo | Política de concorrência simples com flags. |
| Route request authored | Botão/UnityEvent solicitar Route | `RouteRequestTrigger`, bridge, event | Route | request/trigger/event bridge | Sólido | Event bus é local ao trigger. |
| Activity request authored | Solicitar/limpar Activity | `ActivityRequestTrigger`, bridge, event | Activity | request/trigger/event bridge | Sólido | Sem validação de relação Activity/Route. |
| Primary scene loading | Carregar/ativar primary scene | `SceneLifecycleRuntime` | Route | scene loading | Sólido mínimo | Apenas single scene. |
| Route state | Guardar route atual e content set | `RouteLifecycleRuntime`, `RouteLifecycleStartResult` | Route | lifecycle/state | Parcial | Publicação de route events existe, mas content callbacks não ligados. |
| Route Content Set | Registrar primary scene como content handle | `RouteContentSet`, `FrameworkContentHandle` | Route/ContentFlow | content set/diagnostics | Parcial sólido | Sem release; additional scenes só planejadas. |
| Route Content Profile | Declarar cenas adicionais planejadas | `RouteContentProfileAsset`, `RouteContentSceneEntry`, plan | Route | authoring/content planning | Placeholder planejado | Requiredness ainda não bloqueia runtime. |
| Route local callbacks | Notificar receivers sob `RouteContentBinding` | `RouteContentRuntime`, `RouteContentBehaviour`, events | Local/Route | local lifecycle | Implementado mas inativo | Não conectado ao `RouteLifecycleRuntime`. |
| Activity state | Ativar/limpar Activity atual | `ActivityFlowRuntime` | Activity | lifecycle/state | Sólido mínimo | Activity não é vinculada explicitamente a uma Route além de startup. |
| Activity local visibility | Ativar/desativar GameObjects por Activity | `ActivityContentRuntime`, `ActivityContentBinding` | Local/Activity | content binding | Sólido mínimo | Usa `FindObjectsByType` e `SetActive`; não é materialização canônica. |
| Activity local callbacks | Receivers/behaviours/UnityEvents de enter/exit | `IActivityContentLifecycleReceiver`, `ActivityContentBehaviour`, `ActivityContentLifecycleEvents` | Local/Activity | local lifecycle/event bridge | Sólido mínimo | Boundaries locais, sem policy de nesting. |
| ContentFlow core | Linguagem scope/kind/requiredness/handle/set | `Runtime/ContentFlow/*` | ContentFlow | content materialization | Parcial/experimental | Materializer/contribution sem consumer real. |
| Camera authority | Resolver virtual camera ativa por escopo/prioridade | `FrameworkCameraAuthority`, `FrameworkCameraOutputRig`, `FrameworkCinemachineCameraBinding` | Session/Route/Activity | subsystem/diagnostics | Ambíguo | Contradição documental e dependência de Route callbacks inativos. |
| QA Canvas | Executar smokes manuais runtime | `FrameworkQaCanvas` | Diagnostics/QA | QA manual | Parcial útil | Runtime public surface; grande classe IMGUI. |
| Editor settings | Criar/assignar settings/app/route/activity/profile | `ImmersiveFrameworkSettingsProvider`, utility | Editor/Validation | authoring/editor | Sólido parcial | Cria assets em caminhos fixos sob `Assets/ImmersiveFramework`. |
| Authoring validation | Validar setup e `ActivityContentBinding` | `FrameworkAuthoringValidator` | Editor/Validation | validation | Parcial | Não cobre vários conceitos já públicos. |

## 4. Fluxo Macro Atual

1. Inicialização: `ImmersiveFrameworkBootstrap.BootAfterSceneLoad` roda `AfterSceneLoad`, carrega `ImmersiveFrameworkSettingsAsset` via `Resources.Load("ImmersiveFrameworkSettings")`, valida e cria `FrameworkRuntimeHost`.
2. Config/assets: `Project Settings > Immersive Framework` cria settings em `Assets/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset`; `GameApplicationAsset` aponta para `StartupRoute`.
3. Route request: scene object chama `RouteRequestTrigger.RequestRoute`; trigger valida target/runtime e chama `FrameworkRuntimeHost.RequestRouteAsync`.
4. Route load/switch: host chama `GameFlowRuntime.RequestRouteAsync`, que chama `RouteLifecycleRuntime.StartRouteAsync`; este carrega primary scene via `SceneLifecycleRuntime.LoadPrimarySceneAsync`.
5. Activity request: `ActivityRequestTrigger.RequestActivity` chama host, GameFlow valida route ativa e delega para `RouteLifecycleRuntime.StartActivityAsync`.
6. Activity activation/clear: `ActivityFlowRuntime` troca `_currentActivity`, publica events internos, `ActivityContentRuntime` liga/desliga `ActivityContentBinding` e despacha callbacks locais.
7. ContentFlow: participa de Route somente registrando a primary scene carregada em `RouteContentSet`; `RouteContentProfile` gera plano diagnóstico para cenas adicionais.
8. SceneLifecycle: carrega primary scene por path/name, usa `LoadSceneMode.Single`, resolve cena carregada e chama `SetActiveScene`.
9. Triggers/eventos: request triggers publicam `RouteRequestTriggerEvent`/`ActivityRequestTriggerEvent` por `Foundation.Events`; bridges convertem para UnityEvent.
10. Diagnostics/logging: `FrameworkLogger` usa `com.immersive.logging` com sink Unity Console; host/bootstrap emitem logs.
11. Side effects fortes: `RuntimeInitializeOnLoadMethod`, `Resources.Load`, criação de `GameObject` `DontDestroyOnLoad`, `SceneManager.LoadSceneAsync(Single)`, `FindObjectsByType`, `GameObject.SetActive`, estáticos em runtime host/camera authority/QA canvas.
12. Acoplamento excessivo: `CameraFlow` acoplado a Activity/Route content receivers; runtime assembly único acopla framework core, authoring, diagnostics, QA, Cinemachine e Unity adapters.

## 5. Session no Package Atual

Existe:

- `ImmersiveFrameworkSettingsAsset`: active app + editor startup mode.
- `GameApplicationAsset`: application name, startup route, validation mode.
- `ImmersiveFrameworkBootstrap`: entrada runtime.
- `FrameworkRuntimeHost`: objeto persistente `DontDestroyOnLoad`.
- `FrameworkRuntimeState`: snapshot do estado atual.
- `FrameworkCameraOutputRig` e `FrameworkCameraAuthority`: conceitos session-like de câmera.
- `FrameworkQaCanvas`: QA persistente opcional.

Sólido:

- Fail-fast para settings/app/startup route/primary scene ausentes.
- Bootstrap não carrega cena diretamente; delega para GameFlow/RouteLifecycle.
- `Current Scene Only` é editor-only por diretiva.

Incompleto/implícito:

- Não há `SessionContentSet`.
- Não há runtime root/scope formal para conteúdo persistente.
- `ValidationMode` existe como authoring enum, mas seu efeito operacional é mínimo.
- `FrameworkRuntimeHost.TryGetCurrent` é uma forma global controlada de acesso ao runtime.
- Camera output rig é session-scoped de fato, mas não integrado ao ContentFlow.

Preservar:

- `Game Application` como raiz pública.
- Fail-fast de configuração obrigatória.
- Host persistente como owner de diagnostics de requests após troca de cena.

## 6. Route no Package Atual

Existe:

- `RouteAsset`: identidade, primary scene, optional content profile, startup activity.
- `RouteLifecycleRuntime`: owner da route ativa.
- `SceneLifecycleRuntime`: owner de scene loading.
- `RouteContentSet`: registra primary scene como content handle.
- `RouteContentProfileAsset`/`RouteContentSceneEntry`: planejamento de cenas adicionais.
- `RouteContentBinding`, `IRouteContentLifecycleReceiver`, `RouteContentBehaviour`, `RouteContentLifecycleEvents`.
- `RouteEnteredEvent`/`RouteExitedEvent` internos.

Route hoje é principalmente:

- primary scene loading;
- route active identity;
- route content set diagnóstico da primary scene;
- planning-only para cenas adicionais;
- local callback surface implementada, mas sem conexão ativa confirmada.

Riscos/lacunas internas:

- `RouteContentRuntime` não é consumido por `RouteLifecycleRuntime`; portanto `RouteContentBinding`/receivers parecem inativos no fluxo real.
- `RouteEnteredEvent`/`RouteExitedEvent` são emitidos por um EventBus interno de `RouteLifecycleRuntime`; sem consumer real no package.
- `RouteContentProfile.Requiredness` ainda não tem efeito; required additional scene não bloqueia startup.
- Release/clear de route content é implícito pelo `LoadSceneMode.Single`, não por ContentFlow/release policy.
- Route state é referência direta a `RouteAsset`; não há identity estável separada.

## 7. Activity no Package Atual

Existe:

- `ActivityAsset`: identidade e descrição.
- `ActivityFlowRuntime`: owner da Activity ativa.
- `ActivityContentRuntime`: aplica bindings e callbacks.
- `ActivityContentBinding`: binding local GameObject -> Activity.
- `ActivityContentLifecycleContext`, receiver interface, behaviour base e UnityEvent bridge.
- `ActivityEnteredEvent`/`ActivityExitedEvent` internos.

Sólido:

- Activity lifecycle básico existe.
- Clear explícito existe.
- Request já diferencia success/ignored/failed.
- Binding sem Activity gera warning runtime e erro editor.

Limitações:

- Activity é lifecycle real de identidade, mas o content ainda é local visibility simples.
- Não há `ActivityContentSet`.
- Não há Activity content profile.
- Activity não declara pertencimento a Route.
- Discovery é global por cena carregada via `FindObjectsByType<ActivityContentBinding>`.
- Nested bindings são detectados no editor como warning, mas não há policy runtime específica.

## 8. Local no Package Atual

Existe Local authored content via:

- `ActivityContentBinding`;
- `RouteContentBinding`;
- `ActivityContentBehaviour`/`RouteContentBehaviour`;
- lifecycle receivers;
- UnityEvent bridges;
- `FrameworkContentContributionMarker`.

Como conecta:

- Activity Local conecta ao fluxo real por `ActivityContentRuntime`.
- Route Local tem runtime próprio, mas não está conectado ao fluxo real.
- Camera binding tenta se conectar via Activity/Route content receivers.

Identity local:

- Activity Local usa referência direta a `ActivityAsset`.
- Route Local usa `RouteAsset` opcional ou matching por scene path/name.
- Content contribution marker expõe `ContributionId`, `ContributionKind`, `Requiredness`, mas sem discovery real.

Ausente:

- contribution inventory;
- stable local identity model;
- surface/slot identity;
- local content set;
- clear/release inventory.

## 9. ContentFlow no Package Atual

Conceitos existentes:

- `FrameworkContentScope`: escopos.
- `FrameworkContentKind`: tipo de conteúdo.
- `FrameworkContentRequiredness`: required/optional.
- `FrameworkContentHandle`: identidade imutável de conteúdo.
- `FrameworkContentSet`: coleção imutável.
- `IFrameworkContentMaterializer`: contrato público.
- `IFrameworkContentContribution`: contrato público.
- `FrameworkContentContributionMarker`: marker authored.

Uso real:

- `RouteContentSet.FromPrimaryScene` cria um handle `Route/Scene/Required` para primary scene.
- `RouteContentMaterializationPlan` transforma `RouteContentProfileAsset` em plano diagnóstico.
- Não encontrei materializer ativo.
- Não encontrei consumer de `IFrameworkContentContribution`.
- `FrameworkContentContributionMarker` é fundação sem consumidor.

Nomenclatura:

- `FrameworkContentScope/Kind/Requiredness/Handle/Set` é clara para API, mas não necessariamente Inspector-facing.
- `ContentFlow` como módulo é aceitável internamente, mas pode ficar técnico demais se exposto ao usuário final.

Ownership/lifetime:

- Handles registram `Active`, `Source`, `Reason`, `Message`, mas não têm release owner real.
- `FrameworkContentSet` é registro/diagnóstico, não controla release.
- `ContentId` em `RoutePrimaryScene` usa string baseada em owner/path/name ou Guid fallback; o fallback Guid cria identidade instável quando owner/name faltam.

## 10. Surface / RuntimeSpawned no Package Atual

Não existe Surface/RuntimeSpawned formal.

Evidência de ausência:

- Não há arquivos/classes com `Surface`, `Slot`, `Anchor`, `RuntimeSpawned`, `Spawn`, `Prefab`, `Pool`, `InstanceIdentity`.
- `com.immersive.pooling` não está em `package.json`.
- `IFrameworkContentMaterializer` existe, mas nenhum materializer concreto.
- `FrameworkContentContributionMarker` parece precursor de contribution/discovery, não de spawn.

Risco:

- CameraFlow já introduz runtime authority antes de existir runtime root/surface model.
- ContentFlow já possui interfaces públicas que podem pressionar um modelo de spawn antes de ownership/lifetime estarem fechados.

## 11. CrossCutting no Package Atual

Existe:

- Logging: `FrameworkLogger` usando `com.immersive.logging`.
- Foundation events: route/activity trigger events, internal activity/route events, camera events.
- UnityEvent bridges: request triggers, activity/route lifecycle events.
- Validation: `FrameworkBootValidator`, `FrameworkAuthoringValidator`.
- QA: `FrameworkQaCanvas`.
- Editor tooling: settings provider e inspectors.
- Docs/ADRs: README, guide HTML, ADR-0001..0005.

Core vs QA/tooling:

- Core: bootstrap, host, GameFlow, RouteLifecycle, ActivityFlow, SceneLifecycle.
- QA/tooling: QA Canvas, editor validators, settings provider, custom inspectors.
- Misturado: QA Canvas fica no runtime assembly público; CameraFlow está no runtime core assembly; diagnostics wrapper é runtime-only e criado em múltiplos owners.

Riscos:

- `FrameworkLogger.Create()` cria logger/sink/policy repetidamente.
- Foundation events são usados localmente, mas não há política clara de escopo/event lifetime para todos os eventos.
- Editor namespace duplicado `Immersive.Framework.Editor.Editor.*`.
- Validator editor não cobre todo o pacote ativo.

## 12. API Pública e Boundaries

| API/conceito | Namespace | Responsabilidade | Consumidor esperado | Estabilidade | Risco |
|---|---|---|---|---|---|
| `GameApplicationAsset` | `Immersive.Framework.Authoring` | App root | Usuário Unity/Project Settings | Estável | Baixo. |
| `ImmersiveFrameworkSettingsAsset` | `Authoring` | Backing settings | Editor/runtime | Estável parcial | Recurso via `Resources`. |
| `RouteAsset` | `Authoring` | Route authoring | Usuário Unity | Estável parcial | Profile planning pode virar contrato cedo. |
| `ActivityAsset` | `Authoring` | Activity identity | Usuário Unity | Estável | Baixo. |
| `RouteContentProfileAsset` | `Authoring` | Planejar conteúdo Route | Usuário avançado | Provisório | Pode parecer execução real. |
| `ActivityContentBinding` | `ActivityFlow` | Local visibility | Scene authoring | Estável mínimo | Pode ser confundido com materialization. |
| `RouteContentBinding` | `RouteLifecycle` | Local route callbacks | Scene authoring | Provisório | Inativo se runtime não conectado. |
| `ActivityContentBehaviour` / `RouteContentBehaviour` | Activity/Route | Base local lifecycle | Gameplay scripts | Parcial | Route side depende de conexão ausente. |
| `RouteRequestTrigger` / `ActivityRequestTrigger` | `GameFlow` | Request authored | UI/scene objects | Estável mínimo | Evento local ao trigger. |
| Request UnityEvent bridges | `GameFlow` | Inspector callback | Scene authoring | Estável mínimo | Sem payload. |
| `FrameworkContent*` | `ContentFlow` | Linguagem materialization | Framework/modules futuros | Experimental | Public API prematura. |
| `IFrameworkContentMaterializer` | `ContentFlow` | Materialization contract | Futuro | Experimental | Sem consumer/concrete impl. |
| `FrameworkCameraAuthority` | `CameraFlow` | Camera request resolution | Camera bindings/code | Ambíguo | Static global e contradiz freeze. |
| `FrameworkQaCanvas` | `Diagnostics` | QA manual runtime | Dev/QA | Experimental | Public runtime surface. |
| `FrameworkBootResult` | `Bootstrap` | Resultado de validação | Runtime/editor | Parcial | Público, mas boot real é interno. |

## 13. Dívidas e Riscos Internos

| Severidade | Categoria | Evidência | Por que é risco | Recomendação conceitual |
|---|---|---|---|---|
| High | lifecycle | `RouteContentRuntime` não referenciado fora dele mesmo. | Route local callbacks, Route Content events e route-scoped camera podem nunca executar. | Registrar como dívida antes de qualquer novo corte; decidir se conectar, remover ou congelar como inativo. |
| High | package/docs | ADR-0005 diz CameraFlow fora do trilho; `Runtime/CameraFlow` existe e `package.json` depende de Cinemachine. | O package tem baseline contraditório. | ADR de reconciliação: CameraFlow ativo ou removido/deferido. |
| Medium | ownership | `FrameworkCameraAuthority` é `public static` com dicionário global. | Authority de Session existe sem Session scope formal. | Amarrar a Session/runtime root antes de estabilizar como API pública. |
| Medium | content materialization | `FrameworkContentSet` não controla release. | “Content” pode virar só log, não lifecycle. | Separar registro diagnóstico de ownership/release. |
| Medium | identity | `FrameworkContentHandle.RoutePrimaryScene` fabrica id por string/path/name e fallback Guid. | Identidade instável se dados faltam; string path/name mistura domínios. | Definir identity domain antes de expandir ContentFlow. |
| Medium | validation | Editor validator ignora RouteContentBinding, RouteContentProfile required entries, CameraFlow e contributions. | Authoring pode parecer válido com superfícies quebradas. | Expandir validator somente após decidir baseline ativo. |
| Medium | editor/runtime separation | QA Canvas em runtime public assembly. | Superfície de dev pode entrar em produto. | Manter como dev-only explícito ou mover para package/editor/QA separado no futuro. |
| Medium | dependency | `com.unity.cinemachine` no runtime asmdef. | Todo framework passa a depender de Cinemachine por CameraFlow. | Confirmar se CameraFlow é baseline ativo; se não, remover dependency. |
| Low/Medium | naming | Editor namespace `Immersive.Framework.Editor.Editor.*`. | Ruído público/interno e organização torta. | Corrigir em corte próprio se não quebrar asmdef/editor scripts. |
| Low | logging | `FrameworkLogger.Create()` em vários runtimes. | Sem policy central; repetição de sink/policy. | Só revisar quando diagnostics/settings de logging forem escopo. |
| Low | overengineering | Interfaces `IFrameworkContentMaterializer`/`Contribution` sem consumer. | Contrato público pode cristalizar cedo. | Marcar experimental ou internalizar até uso real. |

## 14. Matriz por Escopo

| Escopo | O que existe | Maturidade | Arquivos principais | Lacunas internas | Riscos | Observações |
|---|---|---|---|---|---|---|
| Session | Settings, GameApplication, bootstrap, runtime host/state | Parcial sólido | `Bootstrap/*`, `ApplicationLifecycle/*`, `Authoring/*Settings*` | Session content/root scope | Static current e Resources | Centro real do package. |
| Route | Route asset, route lifecycle, scene loading, content set, profile planning | Parcial | `RouteLifecycle/*`, `SceneLifecycle/*` | Route content runtime ativo, additive/release | Callbacks route inativos | Route é mais scene loading + identity. |
| Activity | Activity asset, flow, local content activation, clear | Sólido mínimo | `ActivityFlow/*` | Activity content set/profile, route membership | Local binding pode ser confundido com materialization | Melhor área operacional. |
| Local | Activity/Route bindings, receivers, UnityEvents | Misto | `ActivityContent*`, `RouteContent*` | Discovery/inventory/identity | Route local não conectado | Local é authored scene content. |
| ContentFlow | scopes/kinds/handles/sets/interfaces/marker | Parcial/experimental | `ContentFlow/*`, `RouteContentSet` | Release, consumers, materializers | Public API prematura | Uso real só em Route primary scene. |
| RuntimeSpawned | Ausente | Ausente | N/A | spawn/prefab/pool/instance identity | Nenhum modelo ainda | Não forçar agora. |
| Surface | Ausente | Ausente | N/A | slots/anchors/surface ids | Pode surgir via ContentFlow/Camera | Registrar como lacuna. |
| CrossCutting | logging/events/validation/docs | Parcial | `Diagnostics/*`, `Editor/Validation/*`, ADRs | Policy global, validator completo | Docs/code contraditórios | Foundation/logging usados corretamente. |
| Editor/Validation | settings provider, inspectors, validators | Parcial sólido | `Editor/*` | Validar novas superfícies | Namespace duplicado | Bom para setup inicial. |
| Diagnostics/QA | logs, QA Canvas, README smoke docs | Parcial | `FrameworkLogger`, `FrameworkQaCanvas` | Separação dev/prod | QA runtime público | Útil, mas deve permanecer dev. |

## 15. Matriz de Capacidades

| Capacidade | Escopo | Tipo | Arquivos principais | Status | Preservar | Revisar | Remover/Adiar | Observações |
|---|---|---|---|---|---|---|---|---|
| Boot mínimo | Session | bootstrap | `Bootstrap/*` | Sólido | Sim | Pouco | Não | Coerente com ADR-0001. |
| Game Application | Session | authoring | `GameApplicationAsset` | Sólido | Sim | Não | Não | Raiz pública correta. |
| Runtime Host | Session | lifecycle/state | `FrameworkRuntimeHost` | Parcial | Sim | Sim | Não | Formalizar Session scope depois. |
| Game Flow | CrossCutting | request | `GameFlowRuntime` | Sólido | Sim | Sim | Não | Delegação boa. |
| Route primary scene | Route | scene loading | `SceneLifecycleRuntime` | Sólido mínimo | Sim | Sim | Não | Single-only. |
| Route Content Set | ContentFlow/Route | content set | `RouteContentSet` | Parcial | Sim | Sim | Não | Registro, não release. |
| Route Content Profile | Route | authoring/planning | profile/plan files | Placeholder | Sim | Sim | Adiar execução | Planning-only claro. |
| Route local lifecycle | Route/Local | lifecycle callback | `RouteContentRuntime` | Ambíguo | A decidir | Sim | Talvez | Implementado mas inativo. |
| Activity flow | Activity | lifecycle/state | `ActivityFlowRuntime` | Sólido | Sim | Sim | Não | Owner claro. |
| Activity content binding | Local/Activity | content binding | `ActivityContentRuntime`, binding | Sólido mínimo | Sim | Sim | Não | Não promover a canonical materialization. |
| Request trigger events | GameFlow | event bridge | trigger/event/bridge files | Sólido | Sim | Pouco | Não | Boa separação trigger vs UnityEvent bridge. |
| ContentFlow interfaces | ContentFlow | materialization | `IFrameworkContent*` | Experimental | Talvez | Sim | Talvez | Sem consumers reais. |
| CameraFlow | Session/Activity/Route | subsystem | `CameraFlow/*` | Ambíguo | A decidir | Sim | Talvez | Contradiz freeze; depende de route callbacks. |
| QA Canvas | Diagnostics/QA | manual QA | `FrameworkQaCanvas` | Parcial | Sim | Sim | Não agora | Deve permanecer dev-only. |
| Editor validation | Editor | validation | `FrameworkAuthoringValidator` | Parcial | Sim | Sim | Não | Escopo menor que package atual. |

## 16. Síntese do Package Atual

O package hoje é um framework lifecycle incremental já funcional para boot, Route primary scene, Activity switching e local Activity visibility.

Ele ainda não é:

- um sistema completo de Session content;
- um sistema de materialização homogênea;
- um sistema de Surface/RuntimeSpawned;
- um runtime de Route additive composition;
- um Activity content profile/runtime;
- um framework com CameraFlow inequivocamente alinhado ao baseline documental.

O centro arquitetural atual é `FrameworkRuntimeHost + GameFlowRuntime + RouteLifecycleRuntime + ActivityFlowRuntime`.

Conceitos mais maduros:

- `Game Application`;
- bootstrap fail-fast;
- Route primary scene loading;
- Activity switching/clear;
- Activity local binding;
- request triggers + typed events + UnityEvent bridges;
- editor setup inicial.

Conceitos frágeis:

- Route local lifecycle;
- ContentFlow beyond primary scene handle;
- CameraFlow;
- Session scope;
- validation coverage;
- release/lifetime ownership.

Decisões implícitas que precisam virar ADR ou reconciliação:

- CameraFlow está ativo ou fora do baseline?
- `RouteContentRuntime` deve ser conectado agora ou removido/deferido?
- `ContentFlow` público deve permanecer público antes de materializers reais?
- Session scope/root é representado por `FrameworkRuntimeHost`, por ContentFlow, ou por outro owner?
- QA Canvas é parte do runtime package ou dev-only tooling?

O que não deveria avançar antes de revisão:

- Route additive execution;
- Activity content profiles;
- RuntimeSpawned/prefab/pool;
- Surface/slots;
- CameraFlow expansion;
- novos materializers.

## 17. Perguntas Abertas para Comparação Posterior

- O baseline oficial considera `CameraFlow` ativo, removido ou experimental congelado?
- `RouteContentBinding` deve ser funcional agora ou apenas uma superfície planejada?
- `RouteContentRuntime` deveria ser owner real chamado por `RouteLifecycleRuntime`?
- `RouteEnteredEvent`/`RouteExitedEvent` devem ser públicos para consumidores externos ou internos?
- `ContentFlow` deve definir identity por asset GUID, path, object reference ou outro domínio?
- `FrameworkContentContributionMarker` deve continuar público sem discovery?
- `ValidationMode.Strict/Standard/Release` deve afetar quais regras concretas?
- QA Canvas deve ficar no runtime asmdef ou virar tooling separado?
- Cinemachine deve ser dependência obrigatória do framework core?
- Settings em `Resources` é o mecanismo definitivo ou baseline temporário?

## 18. Itens que Devem Virar ADR

1. Reconciliação de `CameraFlow` com ADR-0005 e README.
2. Decisão sobre ativar, remover ou congelar `RouteContentRuntime`.
3. Baseline de `Session Content Scope` e runtime root.
4. Política de API pública para `ContentFlow` antes de materializers reais.
5. Identity model para content handles e owner ids.
6. Critério de release/lifetime para `FrameworkContentSet`.
7. Separação QA/runtime/editor para `FrameworkQaCanvas`.
8. Validação authoring mínima para RouteContent, CameraFlow e ContentFlow contributions.
9. Política de dependências UPM, incluindo Cinemachine e pinagem de Git dependencies.
10. Semântica concreta de `ValidationMode`.

Validação realizada: leitura estática por `rg`/`Get-Content` dos manifests, asmdefs, Runtime, Editor, README e ADRs. Validação manual necessária: abrir Unity, confirmar import/compile, entrar em Play Mode com app/route/activity mínimos, validar logs de boot/request/activity, testar se `RouteContentLifecycleEvents` disparam, e confirmar se CameraFlow deve compilar/rodar no baseline aceito.
