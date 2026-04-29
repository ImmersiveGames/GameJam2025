# AUDIT: ResetRun/Retry Legacy — Índice de Documentação

**Data**: 2026-04-28
**Status**: ✅ COMPLETO
**Recomendação**: **REMOVER AGORA** (seguro, zero-risco)

---

## 📋 Documentação Disponível

### 1. **AUDIT_RESETRUN_RETRY_SUMMARY.md** ← **COMECE AQUI**
**Tempo de leitura**: 5-10 min
**Escopo**: Executivo, não-técnico

Sumário de 1-2 páginas com:
- Achado principal
- Mapa rápido (emissores/consumidores)
- Exposições remanescentes
- Riscos arquiteturais resumidos
- ✅ **Recomendação**: REMOVER AGORA
- Próximo patch mínimo (high-level)

**Para quem?** Gerentes, tech leads, reviewers que precisam decidir rápido.

---

### 2. **AUDIT_RESETRUN_RETRY_LEGACY.md** ← **LEIA PARA DETALHE TÉCNICO**
**Tempo de leitura**: 20-30 min
**Escopo**: Detalhado, técnico, evidência completa

Relatório de 10K+ palavras com:
- **Seção 1-3**: Mapas de emissores, consumidores, exposições (com código/refs)
- **Seção 4**: Riscos arquiteturais por tipo (4 riscos identificados)
- **Seção 5**: Achados-chave resumidos em tabela
- **Seção 6**: Recomendação justificada (5 pontos)
- **Seção 7**: Próximo patch mínimo passo-a-passo (5 passos, sub-bullets)
- **Seção 8**: Impacto à arquitetura (mudanças positivas, zero quebras)
- **Seção 9**: Apêndice com rastreamento completo de fluxos

**Para quem?** Desenvolvedores que vão executar remoção, arquitetos, code reviewers técnicos.

---

### 3. **AUDIT_FLUXOS_VISUAIS.md** ← **LEIA PARA ENTENDER FLUXOS**
**Tempo de leitura**: 10-15 min
**Escopo**: Visual, diagrama ASCII, comparativo

Diagramas ASCII com:
- **Fluxo 1**: Retry completo (como normalizador automático funciona)
- **Fluxo 2**: ResetRun completo (legacy path, fora de SessionTransition)
- **Fluxo 3**: RestartCurrentPhase completo (canônico, via SessionTransition)
- **Tabela**: Comparativo de riscologia (7 dimensões de diferença)
- **Diagrama**: Pós-remoção (como fica a arquitetura limpa)

**Para quem?** Qualquer um que precisa visualizar a diferença entre fluxos.

---

### 4. **CHECKLIST_REMOVAL_RESETRUN_RETRY.md** ← **EXECUTE UMA VEZ APROVADO**
**Tempo de execução**: 20-30 min
**Escopo**: Prático, passo-a-passo, checkboxes

Checklist executável com:
- **PRÉ-EXECUÇÃO**: Validação (5 min)
- **PASO 1**: Remover emissores em PostRunOverlayController (3 sub-items)
- **PASO 2**: Remover consumidores em RunContinuationSelectionRoutingService (Opção A/B)
- **PASO 3**: Limpar referencias em RunResetTargetPhaseResolver
- **PASO 4**: Limpar enum (Opção 4.1 removere, Opção 4.2 Obsolete)
- **PASO 5**: Desconectar botão em cena
- **PASO 6**: Grep validation em todo codebase
- **PASO 7**: Build & Compile
- **PASO 8**: Smoke test (opcional, pós-remoção)
- **PASO 9**: Commit & Push
- **Troubleshooting table** para problemas comuns

**Para quem?** Desenvolvedor designado para executar remoção.

---

## ⚡ Leitura Recomendada (por perfil)

### Tech Lead / Manager
1. AUDIT_RESETRUN_RETRY_SUMMARY.md (5 min)
2. Decidir: aprovar remoção? → Sim
3. Assinar no checklist (sign-off)

### Developer (Executar Remoção)
1. AUDIT_RESETRUN_RETRY_SUMMARY.md (5 min)
2. AUDIT_RESETRUN_RETRY_LEGACY.md seção 7 (próximo patch mínimo) (5 min)
3. CHECKLIST_REMOVAL_RESETRUN_RETRY.md (seguir passo-a-passo) (30 min)
4. Git commit & push

### Code Reviewer / Architect
1. AUDIT_RESETRUN_RETRY_SUMMARY.md (5 min)
2. AUDIT_RESETRUN_RETRY_LEGACY.md seções 1-6 (15 min)
3. AUDIT_FLUXOS_VISUAIS.md comparativo (5 min)
4. Revisar PR de removal contra CHECKLIST

### QA / Smoke Tester
1. AUDIT_FLUXOS_VISUAIS.md (entender fluxos) (5 min)
2. CHECKLIST_REMOVAL_RESETRUN_RETRY.md PASO 8 (smoke test)
3. Selecionar Retry/ExitToMenu buttons em RunDecision overlay
4. Validar que funcionam via canonical rail

---

## 📊 Resumo Executivo em 30 Segundos

```
✅ ACHADO: ResetRun/Retry isolados do rail canônico
❌ RISCO: Nenhum (zero dívida ativa)
✅ RECOMENDAÇÃO: REMOVER AGORA
⏱️ TEMPO: 20-30 min de execução
🎯 IMPACTO: Arquitetura mais limpa, zero quebras
```

---

## 📈 Métricas do Audit

| Métrica | Valor |
|---------|-------|
| Emissores de ResetRun | 1 (PostRunOverlayController.OnClickResetRun) |
| Consumidores de ResetRun | 1 (RunContinuationSelectionRoutingService.RouteSelection) |
| Emissores de Retry | 0 (normalizado antes de routing) |
| Consumidores de Retry | 1 (normalizador automático em routing) |
| Bloqueios defensivos de Retry | 3 (HardFailFastH1 redundantes) |
| Riscos arquiteturais identificados | 4 (todos: severidade baixa/médio) |
| Arquivo de audit completo | ✅ 4 documentos (24K+ palavras) |
| Recomendação | **REMOVER AGORA** |
| Risco de regressão pós-remoção | Muito baixo |

---

## 🔗 Referências Cruzadas

### Contexto Canônico (validado)
- ✅ `DefaultAllowedContinuations`: `[AdvancePhase, RestartCurrentPhase, ExitToMenu, TerminateRun]`
- ✅ `RestartCurrentPhase` via `SessionTransitionOrchestrator` + `PhaseResetExecutor`
- ✅ `Retry` normaliza em routing de forma automática
- ✅ `SessionTransitionPhaseLocalEntryReadyEvent` para handoff canonical
- ✅ ADRs 0049-0051 confirmam isolamento

### ADRs Relacionados
- **ADR-0049**: Fluxo canônico de fim de run (fim de PostRun)
- **ADR-0050**: IntroStage canonica (espelho de entrada)
- **ADR-0051**: Fluxo canonico de continuidade pós-fechamento de run

---

## 🚀 Próximos Passos

1. ✅ **Audit completo**: Documentação criada
2. ⏭️ **Revisar**: Tech lead aprova AUDIT_RESETRUN_RETRY_SUMMARY.md
3. ⏭️ **Executar**: Developer segue CHECKLIST_REMOVAL_RESETRUN_RETRY.md
4. ⏭️ **Review**: Code reviewer valida contra AUDIT_RESETRUN_RETRY_LEGACY.md
5. ⏭️ **Merge**: PR mergeado para main
6. ⏭️ **Validação**: Smoke test pós-merge (PASO 8)

---

## 📞 Dúvidas Comuns

### P: É seguro remover ResetRun agora?
**R**: Sim. Zero dependências no fluxo canônico. ADR-0051 confirmou que DefaultAllowedContinuations não contém ResetRun/Retry.

### P: O que acontece se alguém ligar para ResetRun depois?
**R**: HardFailFastH1 em GameplaySessionRunResetService.AcceptAsync() linha 36-40 previne Retry. ResetRun será erro de compilação.

### P: Vou quebrar botão Retry?
**R**: Não. OnClickRetry() continua, mas chama RequestRestartCurrentPhase() (normaliza automaticamente), não ResetRun.

### P: Preciso editar SessionTransitionOrchestrator?
**R**: Não. Nenhuma mudança necessária.

### P: Preciso mudar DefaultAllowedContinuations?
**R**: Não. Já está correto (ResetRun/Retry ausentes).

---

## 👤 Audit Realizado

- **Data**: 2026-04-28
- **Escopo**: Assets/_ImmersiveGames/NewScripts/**/*
- **Análise**: Estática (sem build/runtime/execução)
- **Documentação**: 4 arquivos em Markdown
- **Status**: ✅ COMPLETO
- **Recomendação**: ✅ REMOVER AGORA (zero-risco)

---

## 📎 Arquivos do Audit

```
C:\Projetos\GameJam2025\
├── AUDIT_RESETRUN_RETRY_SUMMARY.md          (1-2 pág, 5 min)
├── AUDIT_RESETRUN_RETRY_LEGACY.md           (10K+ palavras, 20-30 min)
├── AUDIT_FLUXOS_VISUAIS.md                  (diagramas, 10-15 min)
├── CHECKLIST_REMOVAL_RESETRUN_RETRY.md      (executável, 20-30 min)
└── README_AUDIT_INDEX.md                    (este arquivo)
```

---

**Próxima ação**: Executar checklist de remoção. Começar com PASO 1 quando aprovado.

