# CHECKLIST: Remoção de ResetRun/Retry Legacy

**Objetivo**: Remover `ResetRun` e `Retry` legacy do codebase após audit.
**Escopo**: Assets/_ImmersiveGames/NewScripts/
**Tempo estimado**: 20-30 minutos
**Risco**: Baixíssimo (zero quebras arquiteturais esperadas)

---

## PRÉ-EXECUÇÃO: Validação

- [ ] **Ler**: `AUDIT_RESETRUN_RETRY_SUMMARY.md` (5 min)
- [ ] **Verificar**: Nenhum build/compile em progresso
- [ ] **Backup**: Último commit está limpo e pushed
- [ ] **Branch**: Criar branch `refactor/remove-resetrun-retry-legacy`

---

## PASO 1: Remover Emissores em PostRunOverlayController

**Arquivo**: `Assets/_ImmersiveGames/NewScripts/SessionFlow/Host/PostRun/Presentation/Bindings/PostRunOverlayController.cs`

### 1.1 Remover Método `OnClickResetRun()` e Constante

- [ ] **Linha 30**: Remover constante
  ```csharp
  // DELETE
  private const string ResetRunReason = "RunDecision/LegacyResetRun";
  ```

- [ ] **Linhas 177-198**: Remover método inteiro
  ```csharp
  // DELETE ENTIRE METHOD
  public void OnClickResetRun()
  {
      if (_actionRequested)
      {
          // ...
      }
      // ... (22 linhas)
  }
  ```

### 1.2 Remover Field Serializado

- [ ] **Linha 42**: Remover field
  ```csharp
  // DELETE
  [SerializeField] private Button resetRunButton;
  ```

### 1.3 Remover Validação de References

- [ ] **Linhas 504-507**: Remover validação
  ```csharp
  // DELETE THESE 4 LINES from ValidateReferences()
  if (resetRunButton == null)
  {
      DebugUtility.LogWarning<IRunDecisionStagePresenter>("[OBS][GameplaySessionFlow][RunDecision] resetRunButton nao configurado no Inspector.");
  }
  ```

### ✅ Checkpoint 1

- [ ] Arquivo compila sem erro
- [ ] Nenhuma referência a `ResetRunReason` em todo o arquivo
- [ ] Nenhuma referência a `resetRunButton` em todo o arquivo
- [ ] Nenhuma referência a `OnClickResetRun` em todo o arquivo
- [ ] Métodos `OnClickRetry()` e `OnClickRestart()` continuam intactos

---

## PASO 2: Remover ou Bloquear Consumidores em RunContinuationSelectionRoutingService

**Arquivo**: `Assets/_ImmersiveGames/NewScripts/SessionFlow/Integration/RunReset/RunContinuationSelectionRoutingService.cs`

### Opção A: **REMOVER TOTALMENTE** (mais agressivo, recomendado)

#### 2.1A Remover Branch ResetRun em RouteSelection()

- [ ] **Linhas 37-41**: Remover bloco inteiro
  ```csharp
  // DELETE
  if (routedSelection.SelectedContinuation == RunContinuationKind.ResetRun)
  {
      RouteRunResetSelection(routedSelection);
      return;
  }
  ```

#### 2.2A Remover Método RouteRunResetSelection()

- [ ] **Linhas 54-69**: Remover método inteiro
  ```csharp
  // DELETE ENTIRE METHOD
  private void RouteRunResetSelection(RunContinuationSelection selection)
  {
      // ... (16 linhas)
  }
  ```

#### 2.3A Remover ResetRun Branch em RunResetTargetPhaseResolver.ResolveOrFail()

**Arquivo**: Mesmo arquivo, classe `RunResetTargetPhaseResolver`

- [ ] **Linhas 131-136**: Remover bloco
  ```csharp
  // DELETE
  if (selection.SelectedContinuation == RunContinuationKind.ResetRun)
  {
      return _phaseDefinitionCatalog.ResolveInitialOrFail();
  }
  ```

- [ ] **Resultado esperado**: Método `ResolveOrFail()` fica apenas com Retry e genérico HardFailFastH1

### Opção B: **DEIXAR COMO BLOQUEIO** (mais defensivo, alternativa)

Se preferir deixar os bloqueios HardFailFastH1 como defesa contra regressão:

- [ ] Deixar HardFailFastH1 em `RunResetTargetPhaseResolver.ResolveOrFail()` linha 138-142 (Retry)
- [ ] Deixar HardFailFastH1 em `GameplaySessionRunResetService.AcceptAsync()` linha 36-40 (Retry)
- [ ] Deixar HardFailFastH1 em `GameplayRunResetRequest.IsValid` linha 26-31 (Retry)

**Escolha recomendada**: **Opção A** (remoção total)

### ✅ Checkpoint 2

- [ ] Arquivo compila sem warning
- [ ] Nenhuma referência a `RouteRunResetSelection` em todo o arquivo
- [ ] Se Opção A: Nenhuma referência a `RunContinuationKind.ResetRun` em todo o arquivo
- [ ] Método `RouteSelection()` checa apenas:
  - ✅ `if (routedSelection.SelectedContinuation == RunContinuationKind.ResetRun)` foi removido
  - ✅ `DispatchRunContinuationHandoffAsync()` é chamado por default (resto)

---

## PASO 3: Limpar RunResetTargetPhaseResolver de Referências a ResetRun

**Se Opção A foi escolhida:**

- [ ] **Linhas 131-136** já foram deletadas (passo 2.3A)
- [ ] Método `ResolveOrFail()` agora é:
  ```csharp
  public PhaseDefinitionAsset ResolveOrFail(RunContinuationSelection selection)
  {
      if (selection.SelectedContinuation == RunContinuationKind.Retry)
      {
          HardFailFastH1.Trigger(..., "[FATAL][H1]... Retry is legacy...");
          return null;
      }

      HardFailFastH1.Trigger(..., "[FATAL][H1]... RunContinuationSelection invalida...");
      return null;
  }
  ```

### ✅ Checkpoint 3

- [ ] Arquivo compila
- [ ] Método `ResolveOrFail()` ainda valida Retry (defesa)
- [ ] Nenhuma path com sucesso para ResetRun

---

## PASO 4: Limpar Enum RunContinuationKind (Opcional)

**Arquivo**: `Assets/_ImmersiveGames/NewScripts/SessionFlow/Semantic/PostRun/Contracts/RunContinuationContracts.cs`

### 4.1 Remover ResetRun do Enum (mais agressivo)

- [ ] **Linha 14**: Remover
  ```csharp
  // DELETE
  ResetRun = 5,
  ```

**OU**

### 4.2 Marcar como Obsolete (mais defensivo)

- [ ] **Linha 14**: Marcar como deprecated
  ```csharp
  // CHANGE TO
  [Obsolete("ResetRun is legacy and removed. Use RestartCurrentPhase instead.", error: false)]
  ResetRun = 5,
  ```

**Recomendação**: **4.1** (remover, since zero consumidores)

### 4.3 Retry: Deixar no Enum (necessário para bloqueios)

- [ ] **Linha 15**: MANTER
  ```csharp
  // KEEP THIS - usado em bloqueios defensivos
  Retry = 6,
  ```

### ✅ Checkpoint 4

- [ ] Arquivo compila
- [ ] DefaultAllowedContinuations ainda compila (não referencia enum delete)
- [ ] Retry está marcado como `[Obsolete]` ou bloqueado em múltiplos pontos

---

## PASO 5: Desconectar Botão em Cena/Prefab

**Arquivo**: `Assets/_ImmersiveGames/Scenes/UIGlobalScene.unity`

- [ ] Abrir cena em Editor
- [ ] Pesquisar GameObject `PostRunOverlay` ou semelhante
- [ ] Selecionar componente `PostRunOverlayController`
- [ ] No Inspector:
  - [ ] Campo `ResetRunButton`: desconectar ou deixar null
  - [ ] Campo `RetryButton`: **MANTER CONECTADO** (OnClickRetry() ainda existe e funciona)

### Alternativa: Remover Binding via Search

```powershell
# Se quiser verificar sem abrir Unity:
# Procurar por "resetRunButton" em UIGlobalScene.unity
grep -i "resetRunButton" Assets/_ImmersiveGames/Scenes/UIGlobalScene.unity
```

- [ ] Se encontrar "resetRunButton" bindado: remover a binding manualmente no Inspector

### ✅ Checkpoint 5

- [ ] Cena carrega sem erro de referência missing
- [ ] PostRunOverlayController não mostra warning de "resetRunButton not configured"
- [ ] Botão `Retry` continua funcionando (chama `OnClickRetry()`)

---

## PASO 6: Verificação Final em Toda Base de Código

### 6.1 Procurar Referências Pendentes

```powershell
# No terminal PowerShell, na raiz do projeto:
Get-ChildItem -Recurse -Path "Assets/_ImmersiveGames/NewScripts" -Filter "*.cs" |
  Select-String -Pattern "ResetRun" |
  Where-Object { $_ -notmatch "HardFailFastH1" }  # Ignora bloqueios defensivos

# Esperado: Nenhum resultado
```

- [ ] Executar comando acima
- [ ] Nenhuma referência a "ResetRun" fora dos bloqueios defensivos

### 6.2 Procurar OnClickResetRun

```powershell
Get-ChildItem -Recurse -Path "Assets/_ImmersiveGames/NewScripts" -Filter "*.cs" |
  Select-String -Pattern "OnClickResetRun"

# Esperado: Nenhum resultado
```

- [ ] Executar comando acima
- [ ] Nenhum resultado

### 6.3 Procurar Referências a ResetRunReason

```powershell
Get-ChildItem -Recurse -Path "Assets/_ImmersiveGames/NewScripts" -Filter "*.cs" |
  Select-String -Pattern "ResetRunReason"

# Esperado: Nenhum resultado
```

- [ ] Executar comando acima
- [ ] Nenhum resultado

### ✅ Checkpoint 6

- [ ] Grep de "ResetRun" retorna 0 (ou apenas bloqueios defensivos)
- [ ] "OnClickResetRun" retorna 0
- [ ] "ResetRunReason" retorna 0

---

## PASO 7: Build & Compile

- [ ] Abrir Unity Editor
- [ ] Forçar recompilação: `Assets → Reimport All`
- [ ] Verificar Console por erros/warnings

```
Esperado:
✅ 0 errors
✅ 0 warnings (ou apenas warnings não-relacionados)
✅ Nenhuma referência a "ResetRun" ou "OnClickResetRun"
```

- [ ] Se houver erro: aparar e voltar ao passo responsável

### ✅ Checkpoint 7

- [ ] Editor compila sem erro
- [ ] Nenhum script vermelho
- [ ] Console limpo de referencias a ResetRun

---

## PASO 8: Smoke Test (Optional, não executar aqui per restrições)

**Se fizer later (fora do escopo deste audit):**

- [ ] Launch Game
- [ ] Passar por uma run até fim
- [ ] Trigger RunDecision overlay
- [ ] Clique em "Retry" button → Deve reiniciar phase atual ✅
- [ ] Clique em "Exit to Menu" → Deve ir para menu ✅
- [ ] Nenhum crash/HardFailFastH1 inesperado ✅

---

## PASO 9: Commit & Push

```bash
# Stage changes
git add Assets/_ImmersiveGames/NewScripts/

# Commit
git commit -m "refactor: remove legacy ResetRun/Retry from PostRun flow

- Remove OnClickResetRun() and resetRunButton from PostRunOverlayController
- Remove ResetRunReason constant
- Remove RouteRunResetSelection() from RunContinuationSelectionRoutingService
- Remove ResetRun branch from RunResetTargetPhaseResolver
- Remove ResetRun from RunContinuationKind enum
- Keep Retry enum value for defensive HardFailFastH1 checks
- Disconnect resetRunButton binding from UIGlobalScene.unity

**Rationale**: ResetRun/Retry removed from DefaultAllowedContinuations
per architecture audit. RestartCurrentPhase covers semantics.
Zero breaking changes; canonical flow (SessionTransition) unaffected.

Fixes: #[audit-issue] (ref: AUDIT_RESETRUN_RETRY_LEGACY.md)"

# Push
git push origin refactor/remove-resetrun-retry-legacy
```

- [ ] Commit criado
- [ ] Push completo
- [ ] PR criado (se necessário)

---

## Resultado Final

| Item | Status |
|------|--------|
| `OnClickResetRun()` | ❌ Removido |
| `resetRunButton` field | ❌ Removido |
| `ResetRunReason` const | ❌ Removido |
| `RouteRunResetSelection()` | ❌ Removido |
| `ResetRun` enum | ❌ Removido |
| `Retry` enum | ✅ Mantido (bloqueios) |
| `OnClickRetry()` | ✅ Mantido (normaliza para RestartCurrentPhase) |
| Bloqueios Retry/RestartCurrentPhase em GameplaySessionRunResetService | ✅ Mantido |
| Canonical rail (SessionTransition) | ✅ Intacto |
| RestartCurrentPhase functionality | ✅ Intacto |

**Arquitetura pós-remoção**:
```
DefaultAllowedContinuations:
  1. AdvancePhase
  2. RestartCurrentPhase (canônico, via SessionTransition)
  3. ExitToMenu
  4. TerminateRun

PostRunOverlayController buttons:
  1. Retry Button → OnClickRetry() → RestartCurrentPhase (normalizado)
  2. ExitToMenu Button → OnClickExitToMenu() → ExitToMenu
  [ResetRun Button: REMOVED]
```

---

## Troubleshooting

| Problema | Solução |
|----------|---------|
| "Type or namespace name ResetRun does not exist" | Remover referência obsoleta, check Paso 6 |
| OnClickResetRun still being called | resetRunButton ainda está bindado em cena; desconectar no Inspector |
| "OnClickRetry not found" | **NÃO remover OnClickRetry()**, apenas OnClickResetRun(). Retry normaliza para RestartCurrentPhase. |
| Build falha | Executar "Reimport All" em Assets → Clean rebuild |
| Teste de Smoke falha | Check Paso 7 (compile), então check binding do Retry button em Inspector |

---

## Sign-Off

- [ ] Todos os passos completados
- [ ] Arquivo compilou sem erro
- [ ] Grep validation passou
- [ ] Commit & push completo
- [ ] PR criado e pronto para review

**Data de execução**: ___________
**Executado por**: ___________
**Reviewado por**: ___________

---

**Próximos passos**: Aguardar aprovação de PR. Após merge, considerar:
- Code review de peers
- Merge para main branch
- Delete branch feature
- Close issue/audit task

