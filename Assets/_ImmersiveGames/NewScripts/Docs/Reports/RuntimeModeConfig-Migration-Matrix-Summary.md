# RuntimeModeConfig → RuntimeConfigSetAsset: Matriz de Migração — Sumário

**Data**: 2026-05-13
**Documentação**: Gabarito para implementation
**Arquivo Principal**: `RuntimeModeConfig-Migration-Matrix.md`

---

## 📊 Visão Geral em Uma Página

### Campos Auditados: 11 Total

| Decisão | Contagem | Campos |
|---------|----------|--------|
| **MANTER** em RuntimeModeConfig | 2 | `modeOverride`, `compositionProfile` |
| **MIGRAR** para RuntimeConfigSetAsset | 9 | `audioDefaults`, `saveConfig`, `inputModes`, `defaultLoadingMode`, `defaultLoadingProfile`, `startupRouteDefinition`, `runtimePersistentScenesPolicy`, `reporter`, `strictness` |

---

## 🎯 Destinos (5 Config Groups)

| Grupo | Campos | Fase | Status |
|-------|--------|------|--------|
| **RuntimePolicyConfigGroup** | reporter, strictness, runtimePersistentScenesPolicy | 2.5 | Bem isolado, baixo risco |
| **AudioRuntimeConfigGroup** | audioDefaults | 2.1 | Cleanest migration |
| **SessionOperationalRuntimeConfigGroup** | defaultLoadingMode, defaultLoadingProfile, startupRouteDefinition | 2.4 | **CRÍTICO**: startupRouteDefinition é fail-point do bootstrap |
| **SaveRuntimeConfigGroup** | saveConfig | 2.2 | **CRÍTICO**: Afeta integridade de dados (save/load) |
| **InputModesRuntimeConfigGroup** | inputModes | 2.3 | Bem isolado |

---

## 📅 Cronograma Sugerido (Phase 2)

```
Semana 2-3:

Phase 2.1 (Dias 8-10):   Audio           — audioDefaults
Phase 2.2 (Dias 11-13):  Save            — saveConfig
Phase 2.3 (Dias 14-16):  InputModes      — inputModes
Phase 2.4 (Dias 17-19):  SessionOp       — defaultLoadingMode, defaultLoadingProfile, startupRouteDefinition
Phase 2.5 (Dias 20-21):  RuntimePolicy   — reporter, strictness, runtimePersistentScenesPolicy

[Parallelização possível: 2.1 + 2.2 + 2.3 podem rodar simultaneamente]
[Sequencial obrigatório: 2.4 antes de 2.5; 2.4 crítico para bootstrap]
```

---

## ⚠️ Risco Summary

| Risco | Campos | Mitigação |
|-------|--------|-----------|
| **🟢 Baixo** (4 campos) | `audioDefaults`, `inputModes`, `reporter`, `strictness` | Consumidores bem isolados, fallback pattern simples |
| **🟡 Médio** (4 campos) | `defaultLoadingMode`, `defaultLoadingProfile`, `runtimePersistentScenesPolicy` | Validação condicional ou múltiplos consumidores — Phase 2 fallback, Phase 3 test |
| **🔴 Alto** (2 campos) | `startupRouteDefinition`, `saveConfig` | Bootstrap fail-point / Data integrity — deixar para último, test completo |

---

## 👥 Consumidores: Quem Lê O Quê?

| Consumidor | Campo(s) | Fase | Como |
|-----------|----------|------|------|
| **AudioAdapter** | audioDefaults | 2.1 | `registry.TryGetAudioRuntime(out cfg) → cfg.AudioDefaults` |
| **SaveService** | saveConfig | 2.2 | `registry.TryGetSaveRuntime(out cfg) → cfg.SaveConfig` |
| **InputModeCoordinator** | inputModes | 2.3 | `registry.TryGetInputModesRuntime(out cfg) → cfg.PlayerActionMapName` etc. |
| **LoadingAdapter** | defaultLoadingMode, defaultLoadingProfile | 2.4 | `registry.TryGetSessionOperationalRuntime(out cfg) → cfg.DefaultLoadingMode` |
| **SceneCompositionAdapter** | defaultLoadingProfile, startupRouteDefinition, runtimePersistentScenesPolicy | 2.4 / 2.5 | `registry.TryGetSessionOperationalRuntime()` + `registry.TryGetRuntimePolicy()` |
| **DegradationReporter** | reporter | 2.5 | `registry.TryGetRuntimePolicy(out cfg) → cfg.Reporter.dedupStrategy` |
| **Strictness Handler** | strictness | 2.5 | `registry.TryGetRuntimePolicy(out cfg) → cfg.Strictness.degradedAsError` |
| **Bootstrap** | startupRouteDefinition | 2.4 | `registry.TryGetSessionOperationalRuntime(out cfg) → cfg.StartupRouteDefinition` |

---

## 🔄 Padrão de Migração (Mesmo para Todos)

### Step A: Create Config Group
```csharp
[Serializable]
public sealed class AudioRuntimeConfigGroup : IAudioRuntimeConfigReadOnly
{
    [SerializeField] private AudioDefaultsAsset audioDefaults;
    public AudioDefaultsAsset AudioDefaults => audioDefaults;

    public bool TryValidate(out string error) { ... }
}

public interface IAudioRuntimeConfigReadOnly
{
    AudioDefaultsAsset AudioDefaults { get; }
}
```

### Step B: Add to RuntimeConfigSetAsset
```csharp
[SerializeField] private AudioRuntimeConfigGroup audio;

public bool TryValidate(out string error)
{
    if (!audio.TryValidate(out error)) return false;
    // ... validate other groups
}
```

### Step C: Add to RuntimeConfigRegistry
```csharp
public bool TryGetAudioRuntime(out IAudioRuntimeConfigReadOnly config)
{
    if (_snapshot?.Audio != null)
    {
        config = _snapshot.Audio;
        return true;
    }
    config = null;
    return false;
}
```

### Step D: Update Adapter (with Fallback Phase 2, no Fallback Phase 3)
```csharp
// PHASE 2: With fallback
public void Initialize()
{
    if (RuntimeConfigRegistry.Instance != null &&
        RuntimeConfigRegistry.Instance.TryGetAudioRuntime(out var cfg))
    {
        _audioConfig = cfg;
        return;
    }

    // Fallback to old path
    var modeConfig = RuntimeModeConfigLoader.LoadOrNull();
    if (modeConfig?.AudioDefaults != null)
        _audioConfig = new AudioConfigFallbackWrapper(modeConfig.AudioDefaults);
}

// PHASE 3: No fallback
public void Initialize()
{
    if (!RuntimeConfigRegistry.Instance.TryGetAudioRuntime(out _audioConfig))
        throw new InvalidOperationException("Audio config not found");
}
```

### Step E: Manual Test
```
Launch scene → Trigger command → Observe behavior matches old path → Log shows registry used
```

---

## ✅ Checklist de Preparação

### Antes de Começar Phase 2

- [ ] Phase 1 completo (Registry skeleton ready)
- [ ] RuntimeConfigSetAsset criado com 5 group stubs
- [ ] RuntimeConfigRegistry com TryGet* methods working
- [ ] Bootstrap integrado
- [ ] Todos os tests Phase 1 passing
- [ ] Esta matrix revisada e aprovada

### Start of Each Phase 2.X

- [ ] Criar `[BranchName]` para a migração
- [ ] Follow padrão do Step A-E acima
- [ ] Create test ConfigSet para este domínio
- [ ] Implementar fallback (Phase 2 only)
- [ ] Manual test de comportamento
- [ ] Review & merge

### End of Phase 2.5

- [ ] All 5 groups populated
- [ ] All adapters reading from registry (with fallback)
- [ ] Test ConfigSet covers all 5 domains
- [ ] Manual tests pass por domínio
- [ ] Comportamento idêntico ao original
- [ ] Ready for Phase 3 (remove old refs)

---

## 🚀 Quick Start

**Arquivo principal completo**: `RuntimeModeConfig-Migration-Matrix.md`

**Seções mais usadas durante implementação**:
1. "Tabela Completa de Migração" — Ver qual field vai pra qual grupo
2. "Mapeamento Consumidor-by-Consumidor" — Ver como adapters mudam
3. "Checklist por Fase" — Track progress
4. "Padrão de Migração" — Copy-paste template

**Para PM/Lead**:
- Ler: "Resumo Executivo" + "Cronograma Sugerido" (5 min)
- Aprovar: Ordem Phase 2.1 → 2.5

**Para Engenheiro Phase 2.X**:
- Ler: Section correspondente à sua fase (Ex.: "2.2 SaveRuntimeConfigGroup")
- Reference: "Padrão de Migração" (Step A-E)
- Checklist: "Checklist por Fase" / seu phase

---

## ⚖️ Decisões Arquiteturais Confirmadas

✅ RuntimeModeConfig permanece entrada canônica (modeOverride + compositionProfile)
✅ RuntimeConfigSetAsset agrupa 9 campos em 5 groups
✅ RuntimeConfigRegistry valida + expõe read-only
✅ BootstrapConfigAsset não é fonte canônica (ignorar)
✅ Validações existentes (TryValidateLoadingConfiguration) movem para groups
✅ Fallback em Phase 2, remove em Phase 3
✅ 4 campos baixo risco (2.1, 2.3, 2.5 reporter/strictness) podem ser Phase 2 early wins
✅ 2 campos alto risco (startupRouteDefinition, saveConfig) deixar para 2.4 (late) com suporte

---

## 📞 Questões Frequentes Respondidas

| Q | A |
|---|---|
| **Posso migrar 2.1 + 2.2 + 2.3 ao mesmo tempo?** | Sim! Nenhuma cross-dependency. Pode paralelizar (3 branches, 3 times). |
| **2.4 é blocante?** | Sim. startupRouteDefinition é bootstrap fail-point. Must be done carefully. |
| **Preciso manter fallback em Phase 3?** | Não. Phase 3 remove old refs, adapters TryGet sem fallback (throw se absent). |
| **BootstrapConfigAsset — preciso migrar?** | Não é fonte canônica. Ignore. Se DI-registered, continua como wrapper apenas. |
| **E se alguém ainda ler RuntimeModeConfig.audioDefaults em Phase 2?** | Compila (field ainda lá), mas adapter já lê de registry. Ambas funcional = no break. |
| **Quando remover old fields de RuntimeModeConfig?** | Phase 3 (depois que adapters 100% usarem registry). |

---

## 🎓 Como Esta Matrix Conecta com ADR-0011

**ADR-0011** (documento único):
- ✅ Define 5 config groups + invariants + 4 fases
- ✅ Propõe migration roadmap 4-5 semanas

**RuntimeModeConfig-Migration-Matrix** (este documento):
- ✅ Auditoria aplicada: Quais campos concretos vão pra onde?
- ✅ Cronograma específico: Phase 2.1-2.5 em Dias 8-21
- ✅ Risco por campo: Baixo/Médio/Alto
- ✅ Consumidor-by-consumidor: Como cada adapter muda
- ✅ Checklists: Tracking fino

**Como usar juntos**:
1. Ler ADR-0011 para arquitetura geral
2. Ler Migration-Matrix para detalhes concretos de implementação
3. Seguir checklists Phase-by-Phase

---

## 📋 Arquivos Relacionados

| Arquivo | Propósito |
|---------|-----------|
| `ADR-0011-Runtime-Configuration-Registry-Completo.md` | Decisão formal (14 seções, 2.000 linhas) |
| `RuntimeModeConfig-Migration-Matrix.md` | Matriz detalhada (11 campos, 5 groups, consumidores) |
| `ADRs/README.md` | Index de ADRs (atualizado com ADR-0011) |

---

## ✅ Status

- ✅ Auditoria RuntimeModeConfig feita (11 campos identified)
- ✅ ADR-0011 completo (decision + roadmap + invariants)
- ✅ Migration Matrix criada (campos → groups, fases, risco)
- ✅ Nenhum código alterado (docs only)
- ✅ Pronto para aprovação e implementação Phase 1

---

**Criado**: 2026-05-13
**Atualização**: Matriz criada do zero
**Status**: ✅ Documentação Pronta para Implementação


