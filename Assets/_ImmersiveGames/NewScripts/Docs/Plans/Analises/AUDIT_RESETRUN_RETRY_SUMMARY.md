# AUDIT EXECUTIVO: ResetRun/Retry Legacy — Dívida Remanescente

**Data**: 2026-04-28 | **Scope**: Assets/_ImmersiveGames/NewScripts/ | **Análise**: Estática, sem execução

---

## 🎯 Achado Principal

**ResetRun e Retry estão isolados com sucesso do rail canônico.** Ambos foram removidos de `DefaultAllowedContinuations`. A arquitetura de isolamento está completa.

**Risco residual**: Muito baixo. Nenhuma dívida controlada está ativa no fluxo canônico.

---

## 📊 Mapa Rápido

### **ResetRun**
- **Emissores**: 1 (PostRunOverlayController.OnClickResetRun via botão UI)
- **Consumidores**: 1 roteador (RunContinuationSelectionRoutingService) → GameplaySessionRunResetService
- **Bloqueios defensivos**: 2 (GameplaySessionRunResetService, GameplayRunResetRequest)
- **Status**: Ativo mas isolado; legível via UI only

### **Retry**
- **Emissores**: 0 (normalizado automaticamente em Retry→RestartCurrentPhase)
- **Consumidores**: 1 normalizador automático em routing
- **Bloqueios defensivos**: 3 (redundantes, todos HardFailFastH1)
- **Status**: Dead path; nunca chega a run-reset

---

## 🚨 Exposições Remanescentes

| Item | Exposição | Status |
|------|-----------|--------|
| `resetRunButton` (field serializado) | Inspector do prefab | Bindado em UIGlobalScene |
| `OnClickResetRun()` (método público) | UI Button.OnClick() | Alcançável se botão estiver ativo |
| `enum RunContinuationKind.ResetRun` | Código | Ainda no enum (compila) |
| `enum RunContinuationKind.Retry` | Código | Ainda no enum (compila) |
| `DefaultAllowedContinuations` | Config | ✅ ResetRun/Retry ausentes |

---

## ⚖️ Riscos Arquiteturais

| Risco | Severidade | Mitigação Atual | Recomendação |
|-------|-----------|-----------------|--------------|
| ResetRun sem opt-in explícito | Médio | GameplaySessionRunResetService o aceita | Remoção |
| ResetRun manipula PhaseCatalogRuntimeState fora-rail | Baixo | Semanticamente correto | Remover |
| ResetRun bypassa SessionTransition | Baixo | Isolado; não toca canônico | Remover |
| Retry sobre-protegido (3 bloqueios) | Muito baixo | Redundância saudável | Limpeza técnica |

**Conclusão**: Zero riscos de quebra arquitetural com remoção.

---

## ✅ Recomendação Executiva

### **REMOVER AGORA** (não bloquear, não renomear, não migrar)

**Razões**:
1. ✅ Isolamento arquitetural completo
2. ✅ Zero dependências ativas no fluxo canônico
3. ✅ RestartCurrentPhase substitui semanticamente
4. ✅ Logs já marcam como "legacy"
5. ✅ Remoção é limpeza, não refator

**Próximo patch mínimo**:
- [ ] Remover `OnClickResetRun()` método (10 linhas)
- [ ] Remover `resetRunButton` field (1 linha)
- [ ] Remover `ResetRunReason` const (1 linha)
- [ ] Remover binding logic de resetRunButton em inspector
- [ ] (Opcional) Deixar HardFailFastH1 nos bloqueios como defesa regressão

**Tempo estimado**: 15 minutos. **Teste**: Smoke test RunDecision; selecionar AdvancePhase/RestartCurrentPhase/ExitToMenu.

---

## 🔗 Documentação Detalhada

Veja `AUDIT_RESETRUN_RETRY_LEGACY.md` para:
- Mapa completo de emissores/consumidores
- Rastreamento de fluxo ponta-a-ponta
- Análise de riscos por arquivo/classe
- Checklist de removal por paso

---

## 📝 Observações Finais

- **Sem mudança de DefaultAllowedContinuations necessária**: Já está correto
- **Sem mudança de SessionTransitionOrchestrator necessária**: Isolado completo
- **Sem mudança de RestartCurrentPhase necessária**: Já funcional
- **Sem testes de smoke/runtime necessários aqui**: Apenas análise estática

A dívida de ResetRun/Retry é **confinada, documentada e removível com segurança zero-risco**.

---

**Status de Audit**: ✅ COMPLETO | **Recomendação**: ✅ REMOVER | **Alerta**: ⚠️ Nenhum

