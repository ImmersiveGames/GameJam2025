# ADR-0005 — Modularização do GlobalCompositionRoot (registro global por Feature Modules)

## Status

- Estado: Em andamento
- Data (decisão): 2026-02-06
- Última atualização: 2026-02-06
- Tipo: Implementação
- Escopo: `Infrastructure/Composition` (DI global) + registro de módulos (SceneFlow, GameLoop, WorldLifecycle, etc.)
- Decisores: Innocenti
- Tags: CompositionRoot, DI, Modularização, OrderMatters, SOLID

## Contexto

`GlobalCompositionRoot` é o entry point do **registro global** (DontDestroyOnLoad) no NewScripts. Hoje ele concentra:

1) **Orquestração** (pipeline; *order matters*).
2) **Implementação de registro** (instanciação/registro de dezenas de serviços).
3) **Registros Dev/QA** (installers, debug GUI) misturados com produção.

O arquivo já está grande e tende a crescer (novos módulos/ADRs). Isso aumenta risco de regressão por:

- Alterações acidentais na **ordem**.
- Dependências implícitas/cíclicas entre seções.
- Dificuldade de localizar ownership e contrato de cada “feature”.
- Acoplamento entre produção e Dev/QA.

Ao mesmo tempo, o sistema exige:

- **Ordem determinística** (Baseline 2.0, SceneFlow/WorldLifecycle gates, IntroStage, etc.).
- **Idempotência** (domain reload desativado / reexecução de bootstrap).
- **Observabilidade** via logs/âncoras estáveis.
- Evitar mecanismos frágeis em IL2CPP/AOT (ex.: *assembly scanning* por reflection como base do pipeline).

## Decisão

### Objetivo de produção (sistema ideal)

Manter um **Composition Root pequeno e legível**, responsável apenas por:

- Garantir pré-condições globais (logging + DI provider).
- Definir a **ordem canônica** do pipeline.
- Delegar o registro real para **módulos por feature** (cohesos, testáveis e com ownership claro).

### Contrato de produção (mínimo)

1) **Entry point único**
- Apenas `GlobalCompositionRoot` contém `[RuntimeInitializeOnLoadMethod]`.
- O root é o único responsável por chamar o pipeline de módulos.

2) **Ordem explícita e determinística (sem reflection scanning)**
- A lista de módulos é **explícita** e ordenada por `Order` (ou por sequência fixa no código).
- Não usar *auto-discovery* por reflection como mecanismo principal.

3) **Módulos são idempotentes**
- `Install(...)` deve ser seguro quando chamado mais de uma vez (ex.: checar `TryGetGlobal` antes de registrar).

4) **Separação Produção vs Dev/QA**
- Registros `UNITY_EDITOR` / `DEVELOPMENT_BUILD` ficam em um módulo isolado (ou arquivo parcial isolado).
- Produção não deve depender de tipos Dev/QA.

5) **Fail-fast + degraded explícito**
- Falhas de contrato crítico devem falhar cedo (especialmente em Strict/Dev).
- Fallbacks em Release só são aceitos se **explicitamente reportados** via `IDegradedModeReporter`.

6) **Observabilidade preservada**
- Âncoras e mensagens canônicas existentes devem permanecer estáveis (Baseline 2.0) sempre que possível.
- Se uma âncora precisar mudar, o motivo deve ser registrado e evidenciado.

### Não-objetivos (resumo)

- Trocar o mecanismo de DI (não migrar para frameworks externos).
- Introduzir “magia” de scanning/atributos para auto-registrar serviços.
- Refatorar escopo de serviços (global vs scene) além do necessário para modularizar o registro.

## Fora de escopo

- Reescrever serviços já existentes (SceneFlow, GameLoop, WorldLifecycle etc.).
- Mudanças funcionais no pipeline (ordem/semântica) além do necessário para extrair registros.
- Catálogo de módulos via ScriptableObject como requisito inicial (fica como evolução opcional).

## Consequências

### Benefícios

- Arquivo de entrada pequeno; menor atrito para manutenção.
- Melhor **ownership**: cada feature contém seu registrador.
- Redução de risco de regressões por conflitos em um arquivo único.
- Possibilita evolução para composição por perfil (startup/frontend/gameplay) de forma controlada.

### Custos / Riscos

- Risco de erro de ordenação ao mover registros para módulos.
- Possibilidade de dependências cíclicas entre módulos (precisa governança por `Order`).
- Mais arquivos/tipos (custo de organização), compensado por legibilidade.

### Política de falhas e fallback (fail-fast)

- Strict (Dev/QA): preferir **falhar cedo** quando pré-condição crítica estiver ausente.
- Release: permitir degradação **somente** quando houver `DEGRADED_MODE` explícito via `IDegradedModeReporter`.

### Critérios de pronto (DoD)

- [ ] `GlobalCompositionRoot` reduzido para um arquivo “orquestrador” (preferência: < 300 linhas).
- [ ] Extração de módulos de registro por feature (mínimo: RuntimePolicy, Gates, SceneFlow, GameLoop, WorldLifecycle, Navigation, ContentSwap, Levels, Dev/QA).
- [ ] `Install()` idempotente em todos os módulos.
- [ ] Sem reflection scanning para discovery de módulos.
- [ ] Build compila em Release e Dev.
- [ ] Evidência: log de baseline confirma que as âncoras canônicas permanecem (ou mudanças justificadas).

## Implementação (arquivos impactados)

### Passo 0 (quick win): dividir o arquivo em `partial`

Antes de migrar para módulos, dividir `GlobalCompositionRoot` em arquivos parciais **sem alterar comportamento**.

Sugestão de arquivos (mesma classe):

- `GlobalCompositionRoot.Entry.cs` — `Initialize()`, logging, provider.
- `GlobalCompositionRoot.Pipeline.cs` — `RegisterEssentialServicesOnly()` (apenas a sequência de chamadas).
- `GlobalCompositionRoot.EventSystems.cs` — `PrimeEventSystems()`.
- `GlobalCompositionRoot.RuntimePolicy.cs` — `RegisterRuntimePolicyServices()`.
- `GlobalCompositionRoot.InputModes.cs` — `RegisterInputModesFromRuntimeConfig()`, `ReportInputModesDegraded()`, `RegisterInputModeSceneFlowBridge()`.
- `GlobalCompositionRoot.SceneFlow.cs` — `RegisterSceneFlowFadeModule()`, `RegisterSceneFlowNative()`, `RegisterSceneFlowSignatureCache()`, `RegisterSceneFlowLoadingIfAvailable()`.
- `GlobalCompositionRoot.Gates.cs` — `RegisterIfMissing<T>()`, `RegisterPauseBridge()`, `InitializeReadinessGate()`.
- `GlobalCompositionRoot.GameLoop.cs` — GameLoop + IntroStage + commands/outcome/status + coordinator.
- `GlobalCompositionRoot.Navigation.cs` — `RegisterGameNavigationService()` + bridges.
- `GlobalCompositionRoot.ContentSwapLevels.cs` — ContentSwap + Levels (+ QA installers de Levels).
- `GlobalCompositionRoot.StateDependentCamera.cs` — `RegisterStateDependentService()`, `ICameraResolver`.
- `GlobalCompositionRoot.Dev.cs` — installers Dev/QA e debug GUI.

> Observação: este passo é puramente estrutural; o objetivo é facilitar a migração incremental.

### Passo 1: registrar por Feature Modules (direção alvo)

Introduzir um contrato simples para módulos globais:

```csharp
namespace _ImmersiveGames.NewScripts.Infrastructure.Composition
{
    public interface IGlobalCompositionModule
    {
        int Order { get; }
        void Install(GlobalCompositionContext context);
    }

    // Comentário: contexto pequeno para evitar múltiplos resolves e padronizar entradas.
    public sealed class GlobalCompositionContext
    {
        public IDependencyProvider Provider { get; }
        public GlobalCompositionContext(IDependencyProvider provider)
        {
            Provider = provider;
        }
    }
}
```

E o root passa a manter uma lista explícita:

```csharp
// Comentário: ordem explícita; sem reflection scanning.
private static readonly IGlobalCompositionModule[] Modules =
{
    new EventSystemsModule(),
    new RuntimePolicyModule(),
    new InputModesModule(),
    new GatesModule(),
    new SceneFlowModule(),
    new GameLoopModule(),
    new WorldLifecycleModule(),
    new NavigationModule(),
    new ContentSwapModule(),
    new LevelsModule(),
    new StateDependentAndCameraModule(),
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    new DevInstallersModule(),
#endif
#if NEWSCRIPTS_BASELINE_ASSERTS
    new BaselineAssertsModule(),
#endif
};
```

> Nota: `Order` pode ser usado para ordenar, mas a lista explícita já funciona como contrato. Se `Order` existir, usar sorting defensivo e logar conflitos.

### Passo 2 (opcional): catálogo via ScriptableObject

Evolução futura (não obrigatória): permitir um `GlobalCompositionCatalogAsset` em `Resources` para configurar módulos por build/profile sem recompilar.

## Apêndice A — Mapeamento do GlobalCompositionRoot atual

Fonte: `GlobalCompositionRoot.cs` (snapshot 2026-02-06).

### Sugestão de módulos (feature) e responsabilidades

- **EventSystemsModule**
  - `PrimeEventSystems()`

- **RuntimePolicyModule**
  - `RegisterRuntimePolicyServices()`

- **InputModesModule**
  - `RegisterInputModesFromRuntimeConfig()`
  - `ReportInputModesDegraded(...)`
  - `RegisterInputModeSceneFlowBridge()`

- **GatesModule**
  - `RegisterIfMissing<IUniqueIdFactory>(...)`
  - `RegisterIfMissing<ISimulationGateService>(...)`
  - `RegisterPauseBridge(...)`
  - `InitializeReadinessGate(...)`

- **SceneFlowModule**
  - `RegisterSceneFlowFadeModule()`
  - `RegisterSceneFlowNative()`
  - `RegisterSceneFlowSignatureCache()`
  - `RegisterSceneFlowLoadingIfAvailable()`

- **WorldLifecycleModule**
  - `RegisterIfMissing(() => new WorldLifecycleSceneFlowResetDriver())`
  - `RegisterIfMissing(() => new WorldResetService())`
  - `RegisterIfMissing<IWorldResetRequestService>(...)`

- **GameLoopModule**
  - `RegisterGameLoop()`
  - `RegisterIntroStageCoordinator()`
  - `RegisterIntroStageControlService()`
  - `RegisterGameplaySceneClassifier()`
  - `RegisterIntroStagePolicyResolver()`
  - `RegisterDefaultIntroStageStep()`
  - `RegisterGameRunEndRequestService()`
  - `RegisterGameCommands()`
  - `RegisterGameRunStatusService(...)`
  - `RegisterGameRunOutcomeService(...)`
  - `RegisterGameRunOutcomeEventInputBridge()`
  - `RegisterPostPlayOwnershipService()`
  - `RegisterGameLoopSceneFlowCoordinatorIfAvailable()`

- **NavigationModule**
  - `RegisterGameNavigationService()`
  - `RegisterExitToMenuNavigationBridge()`
  - `RegisterRestartNavigationBridge()`

- **StateDependentAndCameraModule**
  - `RegisterStateDependentService()`
  - `RegisterIfMissing<ICameraResolver>(...)`

- **ContentSwapModule**
  - `RegisterIfMissing<IContentSwapContextService>(...)`
  - `RegisterContentSwapChangeService()`

- **LevelsModule**
  - `RegisterLevelServices()`
  - (Dev/QA) `RegisterLevelQaInstaller()`

- **DevInstallersModule** *(UNITY_EDITOR / DEVELOPMENT_BUILD)*
  - `RegisterIntroStageQaInstaller()`
  - `RegisterContentSwapQaInstaller()`
  - `RegisterSceneFlowQaInstaller()`
  - `RegisterIntroStageRuntimeDebugGui()`

- **BaselineAssertsModule** *(condicional: NEWSCRIPTS_BASELINE_ASSERTS)*
  - `RegisterBaselineAsserter()`

### Observação sobre ordem

O *baseline atual* já define uma sequência que funciona. A migração deve preservar a ordem, movendo apenas o corpo do registro (implementação) para os módulos, mantendo o root como “roteiro” do pipeline.

## Notas de implementação (governança)

- Módulos devem manter **cohesão**: um módulo = um domínio/feature.
- Cada módulo mantém os logs `[<Feature>] ...` existentes (quando aplicável).
- Dependências entre módulos devem ser explícitas via **ordem** (ou validação no `Install`).
- Evitar `Resources.Load` “espalhado”: preferir concentrar em módulos específicos (ex.: Navigation, RuntimePolicy).

## Evidência

- Última evidência (log bruto): TBD
- Fonte canônica atual: `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Evidence/LATEST.md` (quando existir)
- Âncoras/assinaturas relevantes: preservar âncoras atuais do bootstrap (ex.: `[EventBus]`, `[RuntimePolicy]`, `[Fade]`, `[SceneFlow]`, `[GameLoop]`, `[Navigation]`, `[ContentSwap]`, `[Level]`).
- Contrato de observabilidade: `Standards.md#observability-contract`

## Referências

- ADR-0007 — InputModes
- ADR-0008 — RuntimeModeConfig
- ADR-0009 — Fade + SceneFlow
- ADR-0010 — LoadingHud + SceneFlow
- ADR-0013 — Ciclo de Vida do Jogo
- ADR-0016 — ContentSwap + WorldLifecycle
- ADR-0017 — LevelManager + Config Catalog
- Baseline 2.0 — `Assets/_ImmersiveGames/NewScripts/Docs/Reports/Baseline-2.0-Spec.md`
- Standards — `Assets/_ImmersiveGames/NewScripts/Docs/Standards/Standards.md`
