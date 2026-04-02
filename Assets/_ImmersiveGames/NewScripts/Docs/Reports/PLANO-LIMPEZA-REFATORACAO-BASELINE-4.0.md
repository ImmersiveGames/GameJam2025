# PLANO DE LIMPEZA E REFATORAÇÃO - Baseline 4.0
## Guia Prático de Ações de Código Morto e Obsoleto

**Data:** 2 de abril de 2026
**Baseado em:** SAUDE-SCRIPTS-ANALISE-BASELINE-4.0.md
**Responsável:** Lead Arquiteto

---

## SEÇÃO 1: CÓDIGO MORTO CONFIRMADO

### 1.1 Remover Imediatamente

#### ❌ PoolingQaContextMenuDriver.cs
**Localização:** `Infrastructure/Pooling/QA/PoolingQaContextMenuDriver.cs`

```csharp
// ESTE ARQUIVO PODE SER REMOVIDO
// - É QA-only code (context menu para editor)
// - Sem documentação de propósito
// - Sem consumidores óbvios

// Se ainda necessário:
// 1. Mover para Assets/Tools/ ou Assets/_ImmersiveGames/NewScripts/Infrastructure/Testing/
// 2. Adicionar comentário explicando propósito
// 3. Adicionar link para issue/ADR que o justifica
```

**Ação:**
```bash
# Opção 1: Remover
git rm Assets/_ImmersiveGames/NewScripts/Infrastructure/Pooling/QA/PoolingQaContextMenuDriver.cs

# Opção 2: Se necessário manter, mover e documentar
# Mover para: Assets/_ImmersiveGames/NewScripts/Infrastructure/Testing/
# Adicionar header:
/// <summary>
/// QA/Debug helper for runtime pooling inspection.
/// Used in: [DOCUMENT HERE]
/// Related ADR: [DOCUMENT HERE]
/// </summary>
```

---

### 1.2 Remover Código Comentado Legado

**Localização:** Procurar em todos os arquivos .cs

```bash
# Encontrar multiline comments (/* ... */) que parecem legado
grep -r "/\*.*\*/" Assets/_ImmersiveGames/NewScripts --include="*.cs" | head -20

# Encontrar singleline legado comments (//)
# Procurar por patterns como:
grep -r "// TODO:" Assets/_ImmersiveGames/NewScripts --include="*.cs"
grep -r "// FIXME:" Assets/_ImmersiveGames/NewScripts --include="*.cs"
grep -r "// DEPRECATED:" Assets/_ImmersiveGames/NewScripts --include="*.cs"
grep -r "// UNUSED:" Assets/_ImmersiveGames/NewScripts --include="*.cs"
```

**Ação:**
```
1. Para cada comentário encontrado:
   - Validar se é ainda relevante
   - Se TODO/FIXME ainda não feito:
     * Criar issue no GitHub
     * Mover comentário para issue
     * Remover do código
   - Se é apenas explicação histórica:
     * Se irrelevante, remover
     * Se relevante para ADR, mover para arquivo ADR

2. Exemplo de refatoração:

   ANTES:
   ```csharp
   // Old implementation was using reflection, too slow
   // Now using direct calls, see commit abc123
   public void FastMethod() { ... }
   ```

   DEPOIS:
   ```csharp
   // Performance improvement: direct calls vs reflection (see ADR-0XXX)
   public void FastMethod() { ... }
   ```
```

---

## SEÇÃO 2: COMPAT LAYERS - REMOÇÃO COM PLANO

### 2.1 SceneReset - Prioridade ALTA

**Localização:** `Orchestration/SceneReset/`
**Marking:** `[compat]` - conversa com legado
**Prazo:** Próximo 1-2 meses

#### Fase 1: Auditoria Completa (4-6 horas)

```bash
# 1. Encontrar todos os consumidores de SceneResetFacade
grep -r "SceneResetFacade" Assets/_ImmersiveGames/NewScripts --include="*.cs"

# 2. Encontrar uso via namespace
grep -r "using.*SceneReset" Assets/_ImmersiveGames/NewScripts --include="*.cs"

# 3. Procurar em eventos relacionados a scene reset
grep -r "SceneResetCompleted\|SceneResetStarted\|OnSceneReset" Assets/_ImmersiveGames --include="*.cs"

# DOCUMENTAR RESULTADO:
# - Lista de consumidores encontrados
# - Tipo de consumo (evento, método direto, etc)
# - Módulo consumidor
# - Facilidade de migração (1-5)
```

#### Fase 2: Criar Deprecation Path (4-6 horas)

```csharp
// Em SceneResetFacade.cs
[System.Obsolete(
    "SceneResetFacade is deprecated. Use WorldReset or SceneFlow directly. " +
    "See ADR-00XX for migration path. Planned removal: Q3 2026",
    error: false)]
public sealed class SceneResetFacade
{
    // KEEP IMPLEMENTATION AS-IS
    // Just warn users
}

// Em cada método público:
[System.Obsolete("Use WorldReset.ResetScene() instead. See migration guide.")]
public void ResetScene(string sceneName)
{
    Debug.LogWarning("[Deprecation] SceneResetFacade.ResetScene is deprecated");
    // ... existing implementation
}
```

#### Fase 3: Criar ADR de Migração

```markdown
# ADR-00XX - Deprecation of SceneResetFacade

## Status
- Estado: Proposto
- Data: 2026-04-02

## Contexto
SceneResetFacade was a [compat] layer for legacy consumers.
The canonical owner is now WorldReset and SceneFlow.

## Decision
SceneResetFacade will be deprecated and removed.

## Migration Path
1. Consumers of ResetScene(name) -> WorldReset.ResetScene(name)
2. Consumers of ResetCurrentScene() -> SceneFlow.ResetCurrentScene()
3. Consumers of scene lifecycle events -> Use WorldReset events

## Timeline
- 2026-04-02: Mark as [Obsolete]
- 2026-05-31: Audit completion deadline
- 2026-06-30: Migration deadline
- 2026-07-31: Removal (if all migrated)

## See Also
- Structural-Xray-NewScripts.md (section 6)
- ADR-0030-Fronteiras-Canonicas-do-Stack
```

#### Fase 4: Migração de Consumidores (8-12 horas)

```csharp
// ANTES (usando SceneResetFacade)
public class OldConsumer : MonoBehaviour
{
    private SceneResetFacade _resetFacade;

    private void OnResetRequested()
    {
        _resetFacade.ResetScene("GameplayScene");
    }
}

// DEPOIS (usando WorldReset canonicamente)
public class NewConsumer : MonoBehaviour
{
    [Inject] private IWorldResetService _worldReset;

    private void OnResetRequested()
    {
        _worldReset.ResetScene("GameplayScene");
    }
}

// Checklist de migração:
// [ ] Found all consumers in codebase
// [ ] Updated each consumer
// [ ] Tested behavior is equivalent
// [ ] Updated unit tests if any
// [ ] Removed SceneResetFacade reference
// [ ] Validated no remaining usages
```

---

### 2.2 LevelFlow/Runtime - Prioridade ALTA

**Localização:** `Orchestration/LevelFlow/Runtime/`
**Marking:** `[transição]` - apenas segura consumidores antigos
**Prazo:** Próximo 1-2 meses

#### Fase 1: Documentar Consumidores (2-3 horas)

```bash
# Encontrar todos os consumidores de LevelFlow/Runtime
grep -r "LevelFlow\|LevelFlowRuntime" Assets/_ImmersiveGames/NewScripts --include="*.cs" | grep -v "LevelLifecycle"

# Resultado esperado: lista de consumidores + tipo de consumo
```

#### Fase 2: Criar Shim em LevelLifecycle (4-6 horas)

```csharp
// Em Orchestration/LevelLifecycle/Runtime/LevelLifecycleService.cs
// Adicionar método compatibilidade:

/// <summary>
/// [DEPRECATED] Compatibility bridge from old LevelFlow/Runtime API.
/// Direct consumers should use the canonical LevelLifecycle APIs.
/// See ADR-00XX for migration path.
/// </summary>
[System.Obsolete("Use LevelLifecycle canonical APIs directly. Migration path in ADR-00XX")]
public void LegacySetLevel(LevelDefinition levelDef)
{
    Debug.LogWarning("[Deprecated] Using legacy LevelFlow API. Please migrate to LevelLifecycle.");
    // Delegate to canonical method
    this.SelectLevel(levelDef);
}
```

#### Fase 3: Migração (8-12 horas)

```csharp
// ANTES (LevelFlow/Runtime)
public class OldGameplaySetup : MonoBehaviour
{
    private ILevelFlowService _levelFlow;

    public void Setup()
    {
        var level = _levelFlow.GetSelectedLevel();
        _levelFlow.PrepareLevelRun(level);
    }
}

// DEPOIS (LevelLifecycle canonical)
public class NewGameplaySetup : MonoBehaviour
{
    [Inject] private ILevelLifecycleService _levelLifecycle;

    public void Setup()
    {
        var level = _levelLifecycle.CurrentLevelDefinition;
        _levelLifecycle.PrepareLevelRuntime(level);
    }
}
```

---

## SEÇÃO 3: RESÍDUOS ESTRUTURAIS

### 3.1 GameplayReset/Core - Prioridade MÉDIA

**Localização:** `Game/Gameplay/GameplayReset/Core/`
**Marking:** Residual de refatoração anterior
**Prazo:** Próximo 1 mês

#### Ação:

```bash
# 1. Listar todos os arquivos em Core/
ls -la Assets/_ImmersiveGames/NewScripts/Game/Gameplay/GameplayReset/Core/

# 2. Para cada arquivo, validar:
#    - Ainda é usado?
#    - Pode ser movido para Coordination/Policy/Execution?
#    - É redundante com outro arquivo?

# 3. Possível resultado:
#    - Mover xxxCoordination para ../Coordination/
#    - Mover xxxPolicy para ../Policy/
#    - Mover xxxExecution para ../Execution/
#    - Remover Core/ subpasta
```

#### Exemplo de Refatoração:

```
ANTES:
Game/Gameplay/GameplayReset/
├── Core/
│   ├── GameplayResetCoordinator.cs
│   ├── GameplayResetPolicy.cs
│   └── GameplayResetExecution.cs
├── Coordination/ (vazio ou parcial)
├── Policy/ (vazio ou parcial)
└── Execution/ (vazio ou parcial)

DEPOIS:
Game/Gameplay/GameplayReset/
├── Coordination/
│   └── GameplayResetCoordinator.cs
├── Policy/
│   └── GameplayResetPolicy.cs
├── Execution/
│   └── GameplayResetExecution.cs
├── Discovery/
├── Integration/
└── (Core/ removida)
```

---

### 3.2 RuntimeMode/DegradedKeys - Prioridade BAIXA

**Localização:** `Infrastructure/RuntimeMode/DegradedKeys.cs`
**Marking:** Possível obsolescência parcial
**Prazo:** Próximo 1-2 meses

#### Ação:

```bash
# 1. Mapear todas as chaves em DegradedKeys.cs
grep -n "public static.*=" Assets/_ImmersiveGames/NewScripts/Infrastructure/RuntimeMode/DegradedKeys.cs

# 2. Para cada chave, procurar consumo:
grep -r "DegradedKeys.SomeKey" Assets/_ImmersiveGames --include="*.cs"

# 3. Se não encontrar consumo:
#    - Marcar como [Obsolete]
#    - Remover em cleanup futuro
```

---

## SEÇÃO 4: OBSERVABILIDADE EM POLLING PATHS

### 4.1 SimulationGate Audit - Prioridade ALTA

**Localização:** `Infrastructure/SimulationGate/`
**Issue:** Possível polling desnecessário em observability paths
**Prazo:** Próximo 1-2 semanas

#### Procedimento de Auditoria:

```bash
# 1. Encontrar todos os Update/Tick métodos
grep -n "Update\|Tick\|LateUpdate" \
    Assets/_ImmersiveGames/NewScripts/Infrastructure/SimulationGate/**/*.cs

# 2. Para cada método:
#    - Qual é o propósito?
#    - Poderia ser event-driven?
#    - Por que polling é necessário?

# 3. Documentar resultado em:
#    Assets/_ImmersiveGames/NewScripts/Infrastructure/SimulationGate/AUDIT_POLLING_PATHS.md
```

#### Exemplo de Refatoração:

```csharp
// ANTES (polling)
public class SimulationGateMonitor : MonoBehaviour
{
    private void Update()
    {
        if (_gate.IsReady != _lastReady)
        {
            Debug.Log($"Gate state changed: {_gate.IsReady}");
            _lastReady = _gate.IsReady;
        }
    }
}

// DEPOIS (event-driven)
public class SimulationGateMonitor : MonoBehaviour
{
    [Inject] private ISimulationGate _gate;

    private void Awake()
    {
        _gate.ReadinessChanged += OnGateReadinessChanged;
    }

    private void OnGateReadinessChanged(bool isReady)
    {
        Debug.Log($"Gate state changed: {isReady}");
    }

    private void OnDestroy()
    {
        _gate.ReadinessChanged -= OnGateReadinessChanged;
    }
}
```

---

## SEÇÃO 5: CHECKLIST DE EXECUÇÃO

### Fase 1: Limpeza Rápida (Semana 1)

```
[ ] Remover PoolingQaContextMenuDriver.cs
[ ] Documentar todas as bridges legítimas (ResetInterop, Audio/Bridges, GameLoop/Bridges)
[ ] Procurar e documentar código comentado legado
[ ] Mapear consumidores de SceneResetFacade (grep + manual)
[ ] Mapear consumidores de LevelFlow/Runtime
[ ] Mapear consumidores de GameplayReset/Core arquivos
```

**Tempo estimado:** 4-6 horas
**Responsável:** Developer sênior
**Validação:** Code review + teste

---

### Fase 2: Refatorações (Próximas 2-4 semanas)

```
[ ] SimulationGate event-driven refactor
    [ ] Auditoria completa de Update/Tick
    [ ] Refatorar para event-driven
    [ ] Testes validando comportamento
    [ ] Documentação em ADR

[ ] Auditoria completa de fallbacks silenciosos
    [ ] Mapear todos os TryGet
    [ ] Adicionar logging apropriado
    [ ] Teste de observabilidade

[ ] Consolidar GameplayReset/Core
    [ ] Mover arquivos para Coordination/Policy/Execution
    [ ] Remover subpasta Core
    [ ] Validar imports
    [ ] Testes

[ ] Marcar SceneResetFacade como [Obsolete]
    [ ] Adicionar atributo
    [ ] Adicionar mensagem de migração
    [ ] Criação de ADR
```

**Tempo estimado:** 20-30 horas
**Responsável:** Developer sênior + 1 junior
**Validação:** Full test suite + integration tests

---

### Fase 3: Planos de Migração (Próximo 1-2 meses)

```
[ ] Executar Plano de Migração SceneReset
    [ ] Auditoria completa (4-6h)
    [ ] Deprecation path (4-6h)
    [ ] ADR de migração (2h)
    [ ] Migração de consumidores (8-12h)
    [ ] Remoção (1h)

[ ] Executar Plano de Migração LevelFlow/Runtime
    [ ] Documentar consumidores (2-3h)
    [ ] Criar shim em LevelLifecycle (4-6h)
    [ ] Migração (8-12h)
    [ ] Remoção (1h)

[ ] Audit e limpeza de DegradedKeys
    [ ] Mapear chaves (1h)
    [ ] Procurar consumo (1h)
    [ ] Remover não-usadas (1h)
```

**Tempo estimado:** 40-50 horas
**Responsável:** Developer sênior + architect review
**Validação:** Full regression test + architecture review

---

### Fase 4: Documentação (Contínuo)

```
[ ] Criar Anti-Patterns-Baseline-4.0.md
[ ] Documentar cada bridge legítima
[ ] Atualizar estrutura README files
[ ] Criar migration guides para cada deprecation
[ ] Adicionar exemplos de "correto" vs "incorreto"
```

**Tempo estimado:** 6-8 horas
**Responsável:** Tech writer + architect

---

## SEÇÃO 6: CRITÉRIOS DE ACEITE

### Para cada tarefa de limpeza:

```
✓ Código foi removido ou refatorado
✓ Nenhum novo warning em compilation
✓ Testes passam (100% de cobertura na área)
✓ Code review aprovado
✓ Se há breaking change, ADR foi criado
✓ Documentação foi atualizada
✓ Metrics (saúde de código) melhoraram
```

### Para cada tarefa de refatoração:

```
✓ Funcionalidade é idêntica antes/depois
✓ Performance não piorou (idealmente melhorou)
✓ Observabilidade foi mantida ou melhorada
✓ Testes de integração passam
✓ Architecture review aprovado
✓ ADR foi criado se há decisão nova
✓ Documentação de uso foi atualizada
```

### Para cada deprecation:

```
✓ [Obsolete] foi adicionado com mensagem clara
✓ ADR de migração foi criado
✓ Consumidores foram identificados
✓ Plano de migração foi criado
✓ Data de remoção foi definida
✓ Documentação de migration path foi criada
```

---

## SEÇÃO 7: FERRAMENTAS RECOMENDADAS

### Para Encontrar Código Morto:

```bash
# ReSharper (built into Rider)
# - Run Inspect Code
# - Look for "Unused" warnings
# - Filter by module

# dotnet analyzer
dotnet add package Microsoft.CodeAnalysis.NetAnalyzers

# Custom grep searches (veja seção anterior)
```

### Para Validar Alterações:

```bash
# Unit tests
dotnet test Assets/_ImmersiveGames.Scripts.csproj

# Integration tests
dotnet test Tests.csproj

# Code coverage
dotnet test /p:CollectCoverage=true

# Architecture validation
# - Manual check against ADRs
# - Manual check of dependency graphs
```

---

## SEÇÃO 8: TIMELINE RECOMENDADA

| Fase | Período | Horas | Responsável |
|---|---|---|---|
| Limpeza Rápida | Semana 1 (Apr 7-11) | 4-6 | Developer sênior |
| Refatorações | Semanas 2-4 (Apr 14-May 2) | 20-30 | Dev sênior + 1 junior |
| Migrações | Semanas 5-8 (May 5-30) | 40-50 | Dev sênior + architect |
| Documentação | Contínuo | 6-8 | Tech writer + architect |
| **Total** | **Apr-May** | **70-94** | **Equipe focada** |

---

## SEÇÃO 9: MÉTRICAS DE SUCESSO

### Antes (Hoje, 2 de abril):
- Score de limpeza: 79%
- Compat layers: 3
- Resíduos estruturais: 2
- Polling desnecessário: Desconhecido
- Fallbacks silenciosos: Desconhecido

### Depois (Meta para 1 de junho):
- Score de limpeza: 90%+ (target)
- Compat layers: 1 (apenas aqueles que requerem plano maior)
- Resíduos estruturais: 0
- Polling desnecessário: Documentado e refatorado
- Fallbacks silenciosos: 100% com logging

### Depois (Meta para 30 de junho):
- Score de limpeza: 95%
- Compat layers: 0 (todos com plano de remoção ou bem documentados)
- Resíduos estruturais: 0
- Polling paths: 0
- Fallbacks silenciosos: 0

---

## Conclusão

Este plano fornece um caminho claro para remover código morto e melhorar a limpeza do Baseline 4.0.

**Recomendação:** Começar com "Quick Wins" imediatamente, escalonar para refatorações maiores após sucesso inicial.

