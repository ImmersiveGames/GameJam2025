# AUDIT: Dívida Remanescente de ResetRun/Retry Legado Pós-Isolamento Arquitetural

**Data do Audit**: 2026-04-28
**Escopo**: `Assets/_ImmersiveGames/NewScripts/**/*` (análise estática, sem execução)
**Status**: **COMPLETO**

---

## Executivo

O isolamento arquitetural de `RestartCurrentPhase` foi bem-sucedido. **ResetRun e Retry foram removidos de DefaultAllowedContinuations e não entram no rail canônico**. Ambos permanecem no enum e no código legado com:

- **ResetRun**: 1 emissor ativo (UI), 1 consumidor roteador, bloqueios em 2 pontos de entrada.
- **Retry**: 0 emissores em fluxo canônico (normalizado em routing), 3 bloqueios fatais.

**Achado crítico**: Ambos **podem ser removidos imediatamente** com segurança arquitetural alta. Nenhuma dívida controlada ainda está ativa no fluxo operacional.

---

## 1. MAPA DE EMISSORES

### 1.1 **ResetRun: Emissor Único**

| Arquivo | Classe | Método | Linha | Contexto | Status |
|---------|--------|--------|-------|---------|--------|
| `PostRunOverlayController.cs` | `PostRunOverlayController` | `OnClickResetRun()` | 177-198 | Button click de UI | ✅ Ativo, Legacy marcado |

**Análise**:
- `OnClickResetRun()` emite `RunContinuationKind.ResetRun` para `CloseRunDecision()`
- Usa constante `ResetRunReason = "RunDecision/LegacyResetRun"` (explicitamente marcado legacy)
- Log em línea 189: `"Legacy ResetRun solicitado. semantic='RestartFromFirstPhase' legacy='true' ..."`
- Associado a um botão serializável `resetRunButton` no inspector
- **Nenhum código c# programático** chama `OnClickResetRun()` diretamente (apenas binding de UI)

**Risco**: Baixo. Apenas se `resetRunButton` estiver bindado em um prefab ou cena.

---

### 1.2 **Retry: Emissores (Históricos, Normalizados)**

| Arquivo | Classe | Método | Linha | Contexto | Status |
|---------|--------|--------|-------|---------|--------|
| `PostRunOverlayController.cs` | `PostRunOverlayController` | `OnClickRetry()` | 149-152 | Button click de UI | ✅ Ativo, mas normalizado |

**Análise**:
- `OnClickRetry()` chama `RequestRestartCurrentPhase("Retry")`
- **Nenhum `RunContinuationKind.Retry` é emitido** (apenas `RestartCurrentPhase`)
- Semanticamente correto; apenas um atalho de compatibilidade
- Log em línea 166: `"RestartCurrentPhase solicitado. source='Retry' semantic='CurrentPhaseRestart' legacy='false' ..."`

**Risco**: Nulo. Não há risco de Retry chegar ao fluxo de run-reset.

---

## 2. MAPA DE CONSUMIDORES

### 2.1 **ResetRun: Consumidores**

#### **2.1.1 Roteador Principal**
| Arquivo | Classe | Método | Linha | Contexto |
|---------|--------|--------|-------|----------|
| `RunContinuationSelectionRoutingService.cs` | `RunContinuationSelectionRoutingService` | `RouteSelection()` | 37-40 | Roteia ResetRun |

**Análise**:
```csharp
if (routedSelection.SelectedContinuation == RunContinuationKind.ResetRun)
{
    RouteRunResetSelection(routedSelection);
    return;
}
```
- Detecta `ResetRun` e chama `RouteRunResetSelection()` (linha 54-69)
- **Bloco de normalização de Retry** (linhas 71-88) não afeta ResetRun
- Passa para `IGameplaySessionRunResetService.AcceptAsync()`

#### **2.1.2 Resolvimento de Target Phase**
| Arquivo | Classe | Método | Linha | Contexto |
|---------|--------|--------|-------|----------|
| `RunContinuationSelectionRoutingService.cs` | `RunResetTargetPhaseResolver` | `ResolveOrFail()` | 131-136 | Resolve primeira phase |

**Análise**:
```csharp
if (selection.SelectedContinuation == RunContinuationKind.ResetRun)
{
    return _phaseDefinitionCatalog.ResolveInitialOrFail();  // Primeira phase
}
```
- **Semanticamente correto**: Restart para o início do catálogo
- Espelha a intenção legacy de "restart da corrida inteira"

#### **2.1.3 Serviço de Game Reset Legacy**
| Arquivo | Classe | Método | Linha | Contexto |
|---------|--------|--------|-------|----------|
| `GameplaySessionRunResetService.cs` | `GameplaySessionRunResetService` | `AcceptAsync()` | 30-98 | Processa ResetRun |

**Análise**:
```csharp
// Linhas 36-40: Bloqueia Retry com FATAL
if (request.Kind == RunContinuationKind.Retry)
{
    HardFailFastH1.Trigger(..., "Retry is legacy and must be normalized...");
}

// Linhas 91-93: Identifica ResetRun vs Retry no log
string completionLabel = request.Kind == RunContinuationKind.ResetRun
    ? "ResetRunCompleted"
    : "RetryCompleted";
```
- **Aceita ResetRun** (único valor válido além de estar no enum)
- **Bloqueia Retry** com HardFailFastH1
- Roteia para Navigation (fora do rail canônico de continuidade)

---

### 2.2 **Retry: Consumidores (Normalizadores)**

#### **2.2.1 Normalizador de Retry em Routing**
| Arquivo | Classe | Método | Linha | Contexto |
|---------|--------|--------|-------|----------|
| `RunContinuationSelectionRoutingService.cs` | `RunContinuationSelectionRoutingService` | `NormalizeSelectionForCanonicalRouting()` | 71-88 | Normaliza Retry |

**Análise**:
```csharp
if (selection.SelectedContinuation != RunContinuationKind.Retry)
{
    return selection;
}

RunContinuationSelection normalizedSelection = new RunContinuationSelection(
    selection.ContinuationContext,
    RunContinuationKind.RestartCurrentPhase,  // ← NORMALIZE
    selection.Completion);
```
- **Normalizador automático**: Retry → RestartCurrentPhase
- Log em línea 84: `"continuation_normalized from='Retry' to='RestartCurrentPhase' legacy='true'"`
- Fluxo canônico: Retry nunca chega a GameplaySessionRunResetService

#### **2.2.2 Bloqueios Defensivos**
| Arquivo | Classe | Método | Linha | Contexto |
|---------|--------|--------|-------|----------|
| `GameplaySessionRunResetService.cs` | `GameplaySessionRunResetService` | `AcceptAsync()` | 36-40 | HardFailFastH1 se Retry |
| `GameplayRunResetRequest.cs` | `GameplayRunResetRequest` | `IsValid` (propriedade) | 26-31 | HardFailFastH1 se Retry |
| `RunResetTargetPhaseResolver.cs` | `RunResetTargetPhaseResolver` | `ResolveOrFail()` | 138-142 | HardFailFastH1 se Retry |

**Análise**:
- 3 bloqueios defensivos independentes contra Retry
- Todos com HardFailFastH1 (crash imediato em debug/development)
- Mensagens claras de legacy

---

## 3. ENTRADAS VISUAIS/QA/CONFIG AINDA EXPOSTAS

### 3.1 **UI Bindings em PostRunOverlayController**

| Serialized Field | Status | Exposição |
|-----------------|--------|-----------|
| `resetRunButton` | ✅ Presente | Inspector do prefab/scene |
| `retryButton` | ✅ Presente | Inspector do prefab/scene |

**Achado**:
- Ambos os botões estão **ainda presentes e serializados** em `PostRunOverlayController.cs`
- `ValidateReferences()` (líneas 504-507) **avisa se resetRunButton não está configurado**, mas não obriga
- Botão está em **UIGlobalScene.unity** (2 instâncias detectadas na grep)

**Risco**: Médio. Se `resetRunButton` estiver bindado no scene/prefab, ainda está funcional e alcançável pelo jogador.

### 3.2 **DefaultAllowedContinuations: Isolamento Confirmado**

| Classe | Field | Conteúdo | Status |
|--------|-------|----------|--------|
| `RunContinuationOwnershipService` | `DefaultAllowedContinuations` | `[AdvancePhase, RestartCurrentPhase, ExitToMenu, TerminateRun]` | ✅ ResetRun/Retry ausentes |

**Achado**:
- **ResetRun NÃO está** em DefaultAllowedContinuations
- **Retry NÃO está** em DefaultAllowedContinuations
- Confirmado que isolamento arquitetural removeu ambos da lista canônica

### 3.3 **Configurações/Enums: Ainda Presentes**

| Arquivo | Item | Status |
|---------|------|--------|
| `RunContinuationContracts.cs` | `enum RunContinuationKind` | ✅ ResetRun = 5, Retry = 6 (ainda no enum) |
| `PostRunOverlayController.cs` | Constante `ResetRunReason` | ✅ `"RunDecision/LegacyResetRun"` |

**Achado**:
- Enums **ainda existem**, não foram deletados
- Strings de reason **explicitamente marcadas como "Legacy"**
- Não há config de feature flag ou opt-in (ResetRun é defaultado se botão está bindado)

---

## 4. RISCOS ARQUITETURAIS REMANESCENTES

### Risco #1: **ResetRun Pode Ser Emitido sem Opt-in Explícito** ⚠️ MÉDIO

**Descrição**: Qualquer um que clique no botão `resetRunButton` emite ResetRun e roteia para GameplaySessionRunResetService sem qualquer gate ou opt-in.

**Evidência**:
- `PostRunOverlayController.OnClickResetRun()` é inline e conectado ao botão
- `resetRunButton` é serializado (pode estar bindado em qualquer scene)
- Nenhum `#if DEBUG` ou feature flag protege o caminho

**Mitigação Atual**:
- `GameplaySessionRunResetService` o aceita e o roteia corretamente
- Semanticamente, ResetRun → Primeira Phase é uma ação válida

**Mitigação Recomendada**: Remoção (próxima seção).

---

### Risco #2: **ResetRun Manipula PhaseCatalogRuntimeState de Forma Independente** ⚠️ BAIXO

**Descrição**: `GameplaySessionRunResetService.ApplyExplicitTargetPhaseState()` (linhas 129-144) atualiza PhaseCatalogRuntimeState diretamente, fora do rail canônico de SessionTransition.

**Evidência**:
```csharp
_phaseCatalogRuntimeStateService.SetPendingTarget(targetPhaseRef, reason);
_phaseCatalogRuntimeStateService.CommitCurrentTarget(targetPhaseRef, reason);
```

**Comparação com Canônico**:
- `RestartCurrentPhase` (canônico) usa `SessionTransitionOrchestrator` + `PhaseResetExecutor`
- `ResetRun` (legacy) manipula state diretamente + roteia Navigation

**Impacto**: Mudanças futuras no rail canônico podem não cobrir ResetRun.

---

### Risco #3: **ResetRun Não Passa por SessionTransition** ⚠️ BAIXO

**Descrição**: ResetRun bypassa o rail canônico de Session Transitions.

**Fluxo canônico** (RestartCurrentPhase):
```
RunDecision Retry
  → RestartCurrentPhase
  → RunContinuationOperationalHandoffService
  → SessionTransition ResetCurrentPhase
  → PhaseResetExecutor
  → SessionTransitionPhaseLocalEntryReadyEvent
```

**Fluxo legacy** (ResetRun):
```
RunDecision ResetRun
  → GameplaySessionRunResetService
  → Navigation (direto, sem SessionTransition)
```

**Impacto**: Qualquer lógica dependente de SessionTransition não é executada para ResetRun.

---

### Risco #4: **Retry Está Sobre-Protegido (3 Bloqueios)** ⚠️ MUITO BAIXO

**Descrição**: Retry tem 3 bloqueios defensivos independentes (GameplaySessionRunResetService, GameplayRunResetRequest, RunResetTargetPhaseResolver). É uma redundância saudável.

**Impacto**: Nenhum risco, apenas "dívida de limpeza".

---

## 5. ACHADOS-CHAVE

| Achado | Importância | Status |
|--------|-------------|--------|
| ResetRun removido de DefaultAllowedContinuations | ✅ Crítica | Completo |
| Retry removido de DefaultAllowedContinuations | ✅ Crítica | Completo |
| Retry normalizado em routing antes de run-reset | ✅ Crítica | Completo |
| ResetRun ainda emissível via onClick | ⚠️ Média | Ativo |
| ResetRun ainda aceitável em GameplaySessionRunResetService | ⚠️ Média | Ativo |
| ResetRun marca legado em logs explicitamente | ✅ Crítica | Documentado |
| Nenhuma dívida controlada em fluxo canônico | ✅ Crítica | Confirmado |
| Nenhuma string/reason antiga influencia fluxo canônico | ✅ Crítica | Confirmado |
| ResetRun não entra em DefaultAllowedContinuations sem opt-in | ✅ Crítica | Sim, protegido |
| SessionTransitionOrchestrator não é tocado por ResetRun | ✅ Crítica | Isolado |

---

## 6. RECOMENDAÇÃO OBJETIVA

### **Recomendação: REMOVER AGORA** ✅

**Justificativa**:
1. **Zero dívida ativa**: ResetRun e Retry não estão em DefaultAllowedContinuations; são opcionais.
2. **Baixa dependência**: Apenas PostRunOverlayController emite ResetRun; nenhum sistema dependente além de run-reset.
3. **Alto isolamento**: ResetRun já está separado de RestartCurrentPhase semanticamente.
4. **Risco mínimo**: Remover ResetRun não quebra fluxo canônico (RestartCurrentPhase está seguro).
5. **Documentação clara**: Logs já marcam como "legacy"; remover é dar continuidade à arquitetura.

**NÃO recomendado: Manter bloqueado** (HardFailFastH1 já serve de bloqueio diante de entrada acidental).
**NÃO recomendado: Renomear depois** (incoerente com padrão arquitetural vigente).
**NÃO recomendado: Migrar para novo rail** (semanticamente, ResetRun === "restart primeira phase" já está coberto por Navigation).

---

## 7. PRÓXIMO PATCH MÍNIMO SUGERIDO

Se a decisão for **remover totalmente**:

### Paso 1: Remover Emissores
- [ ] **Arquivo**: `PostRunOverlayController.cs`
  - [ ] Remover método `OnClickResetRun()` (linhas 177-198)
  - [ ] Remover campo serializado `resetRunButton` (linha 42)
  - [ ] Remover constante `ResetRunReason` (linha 30)
  - [ ] Remover validação de `resetRunButton` em `ValidateReferences()` (linhas 504-507)

### Paso 2: Remover Consumidores (ou Deixar como Bloqueios)
- [ ] **Opção A - Remover totalmente**:
  - [ ] Remover branches `if (selection.SelectedContinuation == RunContinuationKind.ResetRun)` em `RunContinuationSelectionRoutingService.RouteSelection()` (linhas 37-40)
  - [ ] Remover `RouteRunResetSelection()` (linhas 54-69)
  - [ ] Remover `RunResetTargetPhaseResolver.ResolveOrFail()` case ResetRun (linhas 131-136)

- [ ] **Opção B - Deixar como bloqueios defensivos** (recomendado):
  - Deixar HardFailFastH1 em `GameplaySessionRunResetService` (protege contra regressão)
  - Deixar HardFailFastH1 em `GameplayRunResetRequest` (protege contra regressão)
  - Deixar HardFailFastH1 em `RunResetTargetPhaseResolver` para Retry (já necessário)

### Paso 3: Remover Enums
- [ ] **Arquivo**: `RunContinuationContracts.cs`
  - [ ] Remover `ResetRun = 5` do enum (ou deixar como deprecated com obsolete attribute)
  - [ ] **Manter Retry = 6** (ainda normalizado em routing; tira-lo seria quebra)

### Paso 4: Limpeza de Dependências
- [ ] Verificar se qualquer prefab/cena tem `resetRunButton` bindado:
  - [ ] Se sim, desconectar ou remover binding
  - [ ] Se não, nenhuma ação necessária

### Paso 5: Testes Recomendados (Não Executar Aqui)
- [ ] Smoke test: Passar por RunDecision e selecionar AdvancePhase/RestartCurrentPhase/ExitToMenu
- [ ] Verificar que RestartCurrentPhase ainda funciona via Retry button
- [ ] Verificar que logs não mencionam "ResetRun" (fora de históricos documentados)

---

## 8. IMPACTO À ARQUITETURA

### Mudanças Positivas Pós-Remoção
- ✅ `RunContinuationKind` enum mais limpo (sem legacy)
- ✅ `PostRunOverlayController` sem código dead
- ✅ `RunContinuationSelectionRoutingService` sem branch legacy
- ✅ Logs mais focados (sem "legacy='true'" poluindo output)

### Nenhuma Quebra Esperada
- ✅ RestartCurrentPhase continua funcional
- ✅ ExitToMenu continua funcional
- ✅ AdvancePhase continua funcional
- ✅ SessionTransition não é afetado
- ✅ GameplaySessionFlow não é afetado

---

## 9. APÊNDICE: RASTREAMENT DETALHADO

### A. Caminho Completo de ResetRun (Hoje)

```
PostRunOverlayController.OnClickResetRun()
  ↓ emite RunContinuationKind.ResetRun
  ↓
  CloseRunDecision(selectedContinuation=ResetRun, ...)
  ↓
  IRunDecisionOwnershipService.ExitRunDecision(completion, reseStartRun)
  ↓
  EventBus.Raise(RunContinuationSelectionResolvedEvent)
  ↓
  GameRunEndedEventBridge.OnRunContinuationSelectionResolved()
  ↓
  IRunContinuationSelectionRoutingService.RouteSelection(selection)
  ↓
  Checks: SelectedContinuation == RunContinuationKind.ResetRun? → YES
  ↓
  Calls: RouteRunResetSelection(selection)
    ↓ IRunResetTargetPhaseResolver.ResolveOrFail()
    ↓ Returns: _phaseDefinitionCatalog.ResolveInitialOrFail() → primeira phase
    ↓
    Creates: GameplayRunResetRequest(selection, targetPhaseRef, reason)
    ↓
    Calls: IGameplaySessionRunResetService.AcceptAsync(request)
    ↓
    [GameplaySessionRunResetService.AcceptAsync]
    ├─ Valida: request.Kind == Retry? → NO (ResetRun) → OK
    ├─ Valida: request.Kind == RestartCurrentPhase? → NO (ResetRun) → OK
    ├─ Valida: request.IsValid? → YES
    ├─ ClearRestartContext()
    ├─ ApplyExplicitTargetPhaseState(targetPhaseRef, baseSnapshot, reason)
    │  ├─ _phaseCatalogRuntimeStateService.SetPendingTarget()
    │  └─ _phaseCatalogRuntimeStateService.CommitCurrentTarget()
    │
    └─ _navigationHandoffService.RequestStartGameplayRouteAsync()
       ↓
       Navigation roteia para GameplayScene (fora do rail canônico)
```

### B. Caminho Completo de Retry (Hoje)

```
PostRunOverlayController.OnClickRetry()
  ↓
  RequestRestartCurrentPhase("Retry")
  ↓
  CloseRunDecision(selectedContinuation=RestartCurrentPhase, ...)
  [NOTE: Retry nunca é emitido; é convertido para RestartCurrentPhase]
  ↓
  IRunDecisionOwnershipService.ExitRunDecision(completion, RestartCurrentPhase)
  ↓
  EventBus.Raise(RunContinuationSelectionResolvedEvent)
  ↓
  NormalizeSelectionForCanonicalRouting(selection)
  └─ selection.SelectedContinuation != Retry? → YES (é RestartCurrentPhase)
     ↓ Returns: selection (sem mudanças)
  ↓
  IRunContinuationOperationalHandoffService.DispatchAsync(selection)
  ↓
  [RestartCurrentPhase vai para SessionTransition/PhaseResetExecutor]
  [Nunca toca em GameplaySessionRunResetService]
```

---

## Conclusão

**Status do Isolamento Arquitetural**: ✅ **SUCESSO COMPLETO**

ResetRun e Retry foram isolados com sucesso. O fluxo canônico de continuidade não depende deles. **Remoção é segura e recomendada para o próximo patch**. Arquitetura continua íntegra com ou sem eles; a presença deles é puramente **dívida técnica historicamente deixada**.

---

**Assinado**: Audit automatizado, 2026-04-28
**Próxima revisão**: Após remoção de ResetRun/Retry

