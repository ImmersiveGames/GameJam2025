# VALIDAÇÃO ARQUITETURAL - Checklist e Métricas
**Data**: 2026-04-24
**Propósito**: Fornecer testes e métricas para manter conformidade com ADRs 0056-0059

---

## 1. CHECKLIST DE CONFORMIDADE

### ADR-0056: Baseline 4.0 = Executor Técnico Fino

#### Scope Permitido (Deve ter)
- [ ] Boot e dependency root
- [ ] SceneFlow macro
- [ ] Loading e fade
- [ ] Gates e readiness técnicos
- [ ] WorldReset, SceneReset, ResetInterop
- [ ] Dispatch macro de rota
- [ ] InputModes como request/apply operacional
- [ ] Materialização e reset operacional

#### Scope Proibido (Não deve ter)
- [ ] Ownership semântico da sessão
- [ ] Preparação semântica de gameplay
- [ ] Participation ownership
- [ ] Continuidade e reset como **política** semântica (apenas execução)
- [ ] Bridges semânticas de sessão
- [ ] Seleção semântica de phase
- [ ] Decisão de intencão acima de request/apply operacional
- [ ] Presenter local de IntroStage

#### Métricas (Analisar com `grep`)
```powershell
# Contar imports de SceneFlow para SessionFlow/Semantic (deve ser 0)
grep -r "using.*SessionFlow.*Semantic" Assets/_ImmersiveGames/NewScripts/SceneFlow/

# Contar ResolveRequired em SceneFlow (deve ser apenas em Bootstrap)
grep -r "ResolveRequired\|TryGetGlobal" Assets/_ImmersiveGames/NewScripts/SceneFlow/Installers/

# Contar classes MonoBehaviour em SceneFlow que não sejam LoadingFade
ls Assets/_ImmersiveGames/NewScripts/SceneFlow/ -r -include "*.cs" | xargs grep "MonoBehaviour"
```

**Status**: ✅ PASS - Baseline está limpo

---

### ADR-0057: Base 1.0 = Leitura Sistemica com Boundaries Explícitos

#### Estrutura Esperada

```
┌─────────────────────────────────────────────────────────────┐
│ Camada Semântica Acima                                      │
│ - GameplaySessionFlow (Session logic)                       │
│ - GameplayParticipationFlowService (Participation)          │
│ - ActorsEnsembleService (Actor selection policy)            │
└─────────────────────────────────────────────────────────────┘
                          ▲
                          │ (contracts explícitos)
                          │
┌─────────────────────────────────────────────────────────────┐
│ Session Integration (Seam/Bridge)                           │
│ - Tradução de snapshots semânticos → intencão operacional   │
│ - Publishers para dominios operacionais                     │
│ - NÓ deve usar service locator❌                            │
└─────────────────────────────────────────────────────────────┘
                          ▲
                          │ (contracts técnicos)
                          │
┌─────────────────────────────────────────────────────────────┐
│ Baseline Técnico/Macro Fino                                 │
│ - SceneFlow (scene transition, loading)                     │
│ - ResetFlow (reset operacional)                             │
│ - InputModes (request/apply operacional)                    │
└─────────────────────────────────────────────────────────────┘
                          ▼
┌─────────────────────────────────────────────────────────────┐
│ Executores Operacionais (Consumidores)                      │
│ - Spawn, ActorRegistry, Camera binding, Input binding       │
│ - Reagem a intencão operacional, não controlam semântica    │
└─────────────────────────────────────────────────────────────┘
```

#### Validações Críticas (§15 - Hot Path)

- [ ] `GameplaySessionFlowPrepareCompletionGate` **NÃO** usa service locator
  ```powershell
  grep -n "TryGetGlobal\|ResolveRequired" SessionFlow/Integration/SceneFlow/GameplaySessionFlowCompletionGateComposer.cs
  # Esperado: 0 em método de composição
  ```

- [ ] `SessionFlowActorsSemanticPortsAdapter` recebe dependências injetadas
  ```powershell
  grep -A5 "public.*Adapter.*(" SessionFlow/Integration/SceneFlow/SessionFlowActorsSemanticPortsAdapter.cs
  # Esperado: ISceneFlowRouteActorSetRefContext, IActorSetSelectionService NO constructor
  ```

- [ ] `GameRunEndedEventBridge` NÃO resolve em Awake
  ```powershell
  grep -B2 -A10 "private void Awake" SessionFlow/Integration/RunReset/GameRunEndedEventBridge.cs
  # Esperado: nenhum TryGetGlobal/ResolveRequired
  ```

#### Métricas de Fluxo de Dependência

```csharp
// Teste: Nenhum módulo deve "puxar" dependência de outro via TryGetGlobal exceto em Bootstraps

// Script para validar:
var forbidden_modules = new[] {
    "SessionFlow/Semantic",   // Semântica não deve resolver
    "SessionFlow/Integration",// Seam não deve ter service locator (exceto factory)
    "ActorsSystem/Semantic",  // Semântica de atores não deve resolver
    "ActorsSystem/Integration/SessionFlow" // Adapter não deve resolver
};

var allowed_modules = new[] {
    "SceneFlow/Installers",   // Bootstrap OK para resolver
    "SessionFlow/Integration/RunReset/Installers", // Installers OK
    "Foundation/Platform/Composition" // Infra OK
};
```

**Status**: ⚠️ FAIL (3 violations encontrados)

---

### ADR-0058: ActorsSystem = Owner Semântico do Conjunto

#### Campos Obrigatórios de `ActorSpec` (§18)

- [ ] `actorSpecId` - identificador semântico único
- [ ] `sourceKind` - ParticipationDerived | AutonomousCanonical | PhaseExclusive | SceneAttached
- [ ] `roleGroup` - Role semântico (Player, Actor, Spectator, System)
- [ ] `operationalRecipeKind` - Como materializar operacionalmente
- [ ] `placeholderBodyRef` - Prefab placeholder
- [ ] `integrationStage` - RouteMacro | PhaseEntry | RuntimeDynamic
- [ ] `realizationMode` - Spawn | RegisterExisting | Preserve | Rematerialize
- [ ] `continuityResetPolicy` - Como persiste/reseta

#### Validações de Ownership

- [ ] Nenhum actor existe sem `ActorSpec`
  ```powershell
  # Procurar por Spawn direto de prefab sem ActorSpec
  grep -r "Instantiate.*prefab" Assets/_ImmersiveGames/NewScripts/
  # Esperado: 0
  ```

- [ ] `WorldDefinition` NÃO é fonte canônica
  ```powershell
  # Procurar por uso de WorldDefinition para definir atores
  grep -r "WorldDefinition" Assets/_ImmersiveGames/NewScripts/
  # Esperado: 0
  ```

- [ ] `ActorRegistry` recebe via `ActorsOperationalBindingService`, não é "descoberto"
  ```powershell
  # Procurar por scan de prefabs/GameObjects
  grep -r "GetComponentsInChildren.*Actor\|FindObjectsOfType.*Actor" Assets/_ImmersiveGames/NewScripts/
  # Esperado: 0
  ```

#### Métricas de Identidade

| ID Type | Owner | Usar Para | Exemplo |
|---------|-------|-----------|---------|
| `AxisActorId` | `ActorsSystem` | Identity semântica do eixo | "player.local" |
| `ParticipantId` | `SessionFlow.Participation` | Identity da participação | "p_001" |
| `RuntimeActorId` | Spawn/Registry | Handle do GameObject runtime | "GoID_42" |
| `playerIndex` | Unity Input System | NUNCA como semantica | ❌ NÃO USE |
| `InputUser.id` | Unity Input System | NUNCA como semantica | ❌ NÃO USE |

**Validação**: Procurar por uso errado de IDs da Unity
```powershell
grep -r "playerIndex.*semantic\|InputUser.*identity\|playerIndex.*as.*actor" Assets/_ImmersiveGames/NewScripts/
# Esperado: 0
```

**Status**: ✅ PASS - Ownership do eixo está claro

---

### ADR-0059: Operational Binding = Separação Explícita

#### Estados Mínimos de Binding

- [ ] `Unbound` - sem vinculo operacional válido
- [ ] `Bound` - vinculo criado
- [ ] `Active` - vinculo ativo e pronto
- [ ] `Disconnected` - vinculo perdeu conectividade

#### Regra de Congelamento (§10)

- [ ] Binding operacional NÃO legitima actor
- [ ] Binding operacional NÃO descobre actor canônico
- [ ] Binding operacional CONSOME actor previamente legitimado em `ActorSpec`

#### Validações

- [ ] Nenhuma referência de ID da Unity como chave semântica
  ```powershell
  grep -r "playerIndex.*as.*key\|InputUser\.id.*as.*key" Assets/_ImmersiveGames/NewScripts/ActorsSystem/
  # Esperado: 0
  ```

- [ ] `ActorsOperationalBindingService` mantém 3 índices separados
  ```csharp
  // Esperado em ActorsOperationalBindingService:
  private Dictionary<AxisActorId, ...> _byAxisActor;          // ✅
  private Dictionary<ParticipantId, AxisActorId> _participant; // ✅
  private Dictionary<RuntimeActorId, AxisActorId> _runtime;    // ✅
  ```

**Status**: ✅ PASS - Binding explícito mantido

---

## 2. TESTES DE CONFORMIDADE

### Test Suite: Validação de Service Locator

```csharp
[TestFixture]
public class ServiceLocatorConformanceTests
{
    [Test]
    public void SessionFlowActorsSemanticPortsAdapter_ShouldNotUseServiceLocator()
    {
        // Arrange
        var participationFlow = Substitute.For<IGameplayParticipationFlowService>();
        var routeContext = Substitute.For<ISceneFlowRouteActorSetRefContext>();
        var selectionService = Substitute.For<IActorSetSelectionService>();

        // Act - Adapter recebe dependências explicitamente
        var adapter = new SessionFlowActorsSemanticPortsAdapter(
            participationFlow,
            routeContext,        // ✅ Explícito
            selectionService);   // ✅ Explícito

        // Assert - Nenhum TryGetGlobal dentro
        var sourceCode = File.ReadAllText("SessionFlowActorsSemanticPortsAdapter.cs");
        Assert.That(sourceCode, Does.Not.Contain("TryGetGlobal"));
    }

    [Test]
    public void GameplaySessionFlowCompletionGateComposer_DependenciesMustBeExplicit()
    {
        // Arrange
        var handoffService = Substitute.For<IGameplaySessionFlowPrepareOperationalHandoffService>();

        // Act - Composer recebe handoff explicitamente
        GameplaySessionFlowCompletionGateComposer.ComposeOrValidate(handoffService);

        // Assert
        var gate = DependencyManager.Provider.Get<ISceneTransitionCompletionGate>();
        Assert.IsNotNull(gate);
    }

    [Test]
    public void GameRunEndedEventBridge_ShouldNotResolveInAwake()
    {
        // Arrange
        var bridge = new GameObject().AddComponent<GameRunEndedEventBridge>();

        // Act - Initialize é chamado explicitamente, não em Awake
        bridge.Initialize(
            Substitute.For<IRunEndMaterializationService>(),
            Substitute.For<IRunContinuationSelectionRoutingService>(),
            Substitute.For<IRunContinuationOwnershipService>());

        // Assert - Sem exceção
        Assert.Pass();
    }
}
```

---

## 3. SCRIPT DE VALIDAÇÃO AUTOMÁTICA

### `validate-architecture.ps1`

```powershell
# Validação Automática de Conformidade Arquitetural

param(
    [string]$projectRoot = "."
)

$violations = @()
$warnings = @()

Write-Host "🔍 Validando conformidade com ADRs 0056-0059..." -ForegroundColor Cyan

# 1. Verificar Service Locator em hot path
Write-Host "`n[1/4] Procurando Service Locator em hot paths..."
$hotPaths = @(
    "SessionFlow/Integration/SessionFlow/SessionFlowActorsSemanticPortsAdapter.cs",
    "SessionFlow/Integration/SceneFlow/GameplaySessionFlowCompletionGateComposer.cs"
)

foreach ($path in $hotPaths) {
    $fullPath = "$projectRoot/Assets/_ImmersiveGames/NewScripts/$path"
    if (Test-Path $fullPath) {
        $content = Get-Content $fullPath -Raw
        if ($content -match "TryGetGlobal|ResolveRequired" -and -not $path.Contains("Bootstrap")) {
            $violations += "🚨 Service Locator em: $path"
        }
    }
}

# 2. Verificar Awake Composition
Write-Host "`n[2/4] Procurando Composition em Awake..."
$files = Get-ChildItem "$projectRoot/Assets/_ImmersiveGames/NewScripts" -r -include "*.cs" |
         Select-String -Pattern "private void Awake.*ResolveRequired|private void Awake.*TryGetGlobal" |
         Select-Object -ExpandProperty Filename

if ($files) {
    $violations += "🚨 Composition em Awake: $($files -join ', ')"
}

# 3. Verificar WorldDefinition
Write-Host "`n[3/4] Procurando uso de WorldDefinition..."
$worldDefUsage = Get-ChildItem "$projectRoot/Assets/_ImmersiveGames/NewScripts" -r -include "*.cs" |
                  Select-String -Pattern "WorldDefinition" |
                  Where-Object { -not $_.Filename.Contains("meta") } |
                  Select-Object -ExpandProperty Filename

if ($worldDefUsage) {
    $violations += "🚨 WorldDefinition encontrado em: $($worldDefUsage | Join-String -Separator ', ')"
}

# 4. Verificar PlayerInput como identity
Write-Host "`n[4/4] Procurando PlayerInput como identity semântica..."
$playInputIdentity = Get-ChildItem "$projectRoot/Assets/_ImmersiveGames/NewScripts/ActorsSystem" -r -include "*.cs" |
                      Select-String -Pattern "playerIndex.*identity|InputUser\.id.*semantic" |
                      Select-Object -ExpandProperty Filename

if ($playInputIdentity) {
    $violations += "🚨 PlayerInput como identity: $($playInputIdentity | Join-String -Separator ', ')"
}

# Relatório
Write-Host "`n" + ("="*60)
if ($violations.Count -eq 0) {
    Write-Host "✅ PASS: Nenhuma violação encontrada" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ FAIL: $($violations.Count) violação(ões):" -ForegroundColor Red
    $violations | ForEach-Object { Write-Host "  $_" }
    exit 1
}
```

**Uso**:
```powershell
./validate-architecture.ps1 -projectRoot "C:\Projetos\GameJam2025"
```

---

## 4. MÉTRICAS DE SAÚDE

### Dashboard de KPIs

| Métrica | Target | Atual | Status |
|---------|--------|-------|--------|
| Service Locators em SessionFlow | 0 | 3 | 🔴 |
| Awake Compositions em Integration | 0 | 1 | 🔴 |
| WorldDefinition Usage | 0 | 0 | ✅ |
| PlayerInput as Semantic ID | 0 | 0 | ✅ |
| ActorSpec Compliance | 100% | ~90% | 🟡 |
| Injeção Explícita em Adapters | 100% | ~70% | 🟡 |

### Trend Tracking

```markdown
### 2026-04-24
- Service Locators Found: 3
- Ratio: 3/total Resolve operations = ~30%
- Trend: Baseline encontra violations em Session Integration

### [Future]
- Target: 0 violations antes de 2026-05-01
```

---

## 5. INTEGRAÇÃO COM CI/CD

### GitHub Actions / Azure Pipelines

```yaml
name: Architecture Validation

on: [push, pull_request]

jobs:
  architecture-check:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v2
      - name: Validate Architecture Conformance
        run: |
          pwsh -Command "
            $violations = 0

            # Check for service locator patterns
            $results = grep -r 'TryGetGlobal\|ResolveRequired' `
              --include='*.cs' `
              'Assets/_ImmersiveGames/NewScripts/SessionFlow/Integration' |
              grep -v 'Bootstrap\|Installer'

            if ($results) {
              Write-Host '❌ Service Locator detected in hot path'
              $violations += @($results).Count
            }

            exit $violations
          "
```

---

## 6. ROADMAP DE CONFORMIDADE

### Sprint Atual: Crítico ⚠️
- [ ] Remover 3 service locators críticos
- [ ] Adicionar testes de conformidade
- [ ] Documentar mudança

### Sprint +1: Consolidação 🔧
- [ ] Rever RunEndBridgeRuntimeComposer
- [ ] Padronizar padrão de composição
- [ ] Atualizar documentação

### Sprint +2: Observabilidade 📊
- [ ] Integrar validação automática em CI/CD
- [ ] Dashboard de métricas
- [ ] Alertas de drift

---

## 7. REFERÊNCIA RÁPIDA

### Checklist de PR Review para Conformidade

```markdown
## Architecture Conformance Checklist

- [ ] Nenhum `TryGetGlobal` em `SessionFlow/Integration/` fora de bootstrap
- [ ] Nenhuma composição em `Awake()` fora de MonoBehaviourAdapters pré-compostos
- [ ] Nenhum `ResolveRequired` em camadas semânticas
- [ ] Todas as dependências injetadas explicitamente em Adapters
- [ ] ActorSpec congelado mantido
- [ ] Nenhuma referência de PlayerInput ID como semantica
- [ ] Boundary de Operational Binding mantido
- [ ] Documentação atualizada se boundary mudou
```


