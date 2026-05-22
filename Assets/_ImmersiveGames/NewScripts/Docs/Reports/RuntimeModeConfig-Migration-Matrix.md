# Matriz de Migração: RuntimeModeConfig → RuntimeConfigSetAsset

**Data**: 2026-05-13
**Fonte**: Auditoria RuntimeModeConfig + ADR-0011 Runtime Configuration Registry
**Propósito**: Guiar implementação Phase 1-3 de forma objetiva
**Status**: Documentação apenas, zero código alterado

---

## Resumo Executivo

**11 campos totais em RuntimeModeConfig**:
- ✅ **2 campos MANTÊM** em RuntimeModeConfig (entry point + mode decision)
- 🔄 **9 campos MIGRAM** para RuntimeConfigSetAsset (5 grupos de config)
- 3 **fases sugeridas** para migração (Phase 2 é maior)

**Risco geral**: Baixo a Médio (campos bem circunscritos, poucos dependentes)

---

## Tabela Completa de Migração

| # | Campo Atual | Tipo | Grupo Destino | Consumidor Atual | Decisão | Fase | Risco | Notas |
|---|------------|------|----------------|-------------------|---------|------|-------|-------|
| 1 | `modeOverride` | `RuntimeModeOverride` | — MANTER em RuntimeModeConfig | Bootstrap, CompositionRoot | **MANTER** | — | Baixo | Define modo de execução (Auto/ForceStrict/ForceRelease). Permanece em RuntimeModeConfig porque é decisão de modo, não config de domínio. |
| 2 | `compositionProfile` | `CompositionProfileKind` | — MANTER em RuntimeModeConfig | Bootstrap, CompositionRoot | **MANTER** | — | Baixo | Define Base11Sandbox vs LegacyCompatible. Permanece porque é decisão de composição global, não config de domínio. |
| 3 | `runtimePersistentScenesPolicy` | `RuntimePersistentScenesPolicyAsset` | **RuntimePolicyConfigGroup** | SceneCompositionAdapter, SessionOperational | **MIGRAR** | 2.5 | Médio | Política de cenas persistentes. Move para RuntimePolicy junto com reporter/strictness. Consumidor: SceneComposition. |
| 4 | `audioDefaults` | `AudioDefaultsAsset` | **AudioRuntimeConfigGroup** | AudioAdapter, AudioSynthesizer | **MIGRAR** | 2.1 | Médio | Audio defaults e profiles. Move para AudioRuntime. Consumidor: AudioAdapter (concreto). Tem TryValidateAudioConfiguration(). |
| 5 | `defaultLoadingMode` | `SessionOperationalRouteLoadingMode` | **SessionOperationalRuntimeConfigGroup** | LoadingAdapter, SessionOperational | **MIGRAR** | 2.4 | Médio | Default loading policy. Move para SessionOperational. Tem TryValidateLoadingConfiguration(). |
| 6 | `defaultLoadingProfile` | `RuntimeLoadingProfileAsset` | **SessionOperationalRuntimeConfigGroup** | LoadingAdapter, SceneComposition | **MIGRAR** | 2.4 | Médio | Default loading profile (condicional se defaultLoadingMode=Profile). Move junto com defaultLoadingMode. |
| 7 | `startupRouteDefinition` | `OperationalRouteAsset` | **SessionOperationalRuntimeConfigGroup** | Bootstrap, SessionOperational | **MIGRAR** | 2.4 | Alto | Rota inicial do profile canônico. Move para SessionOperational. **Risco Alto**: Referenciado no bootstrap, falha cedo se absent. |
| 8 | `reporter` | `DegradedReporterSettings` | **RuntimePolicyConfigGroup** | DegradationReporter, Logging | **MIGRAR** | 2.5 | Baixo | Settings de reporter (dedupe, summary). Move para RuntimePolicy. Consumidor: DegradationReporter (bem isolado). |
| 9 | `strictness` | `StrictnessSettings` | **RuntimePolicyConfigGroup** | StrictnessEnforcer, Logging | **MIGRAR** | 2.5 | Baixo | Settings de strictness (mode enforcement). Move para RuntimePolicy. Consumidor: Strictness handler (bem isolado). |
| 10 | `inputModes` | `InputModesSettings` | **InputModesRuntimeConfigGroup** | InputModeCoordinator, InputModes | **MIGRAR** | 2.3 | Médio | Settings de input (action map names, enableInputModes). Move para InputModes. Consumidor: InputModeCoordinator (concreto). |
| 11 | `saveConfig` | `SaveConfigAsset` | **SaveRuntimeConfigGroup** | SaveService, SaveCore | **MIGRAR** | 2.2 | Alto | Config de save (backend, serialization). Move para SaveRuntime. **Risco Alto**: Afeta persistência de dados. |

---

## Decisões por Tipo

### 🟢 MANTER em RuntimeModeConfig (2 campos)

| Campo | Razão |
|-------|-------|
| `modeOverride` | Define comportamento de execução global (Auto vs Strict vs Release). É **decisão de modo**, não config de domínio. |
| `compositionProfile` | Define qual profile de composição usar (Base11 vs Legacy). É **decisão de composição**, não config de domínio. |

**Estes 2 campos permanecem como entry point canônico de RuntimeModeConfig.**

---

### 🔄 MIGRAR para RuntimeConfigSetAsset (9 campos)

Distribuídos em **5 grupos obrigatórios**:

#### **RuntimePolicy** (3 campos)
- `runtimePersistentScenesPolicy` → moves
- `reporter` → moves
- `strictness` → moves

**Consumidores**:
- SceneCompositionAdapter (persistent scenes)
- DegradationReporter (reporter settings)
- Strictness handler (strictness behavior)

**Status**: Well-isolated, low inter-dependency

---

#### **AudioRuntimeConfigGroup** (1 campo)
- `audioDefaults` → moves

**Consumidor**: AudioAdapter (concreto e well-scoped)

**Status**: Cleanest migration (single field)

---

#### **SessionOperationalRuntimeConfigGroup** (3 campos)
- `defaultLoadingMode` → moves
- `defaultLoadingProfile` → moves (conditional on loading mode)
- `startupRouteDefinition` → moves

**Consumidores**:
- LoadingAdapter (loading defaults)
- SceneCompositionAdapter (default profile)
- Bootstrap / SessionOperational (startup route)

**Status**:
- **Risco Alto**: startupRouteDefinition is bootstrap-critical (fail-fast if absent)
- Validation: TryValidateLoadingConfiguration() existing (Phase 2.4 copy it)

---

#### **SaveRuntimeConfigGroup** (1 campo)
- `saveConfig` → moves

**Consumidor**: SaveService (save backend, serialization policy)

**Status**:
- **Risco Alto**: Afeta integridade de dados (save/load)
- Deve-se testar save/load cycle carefully
- IsValid check na implementação

---

#### **InputModesRuntimeConfigGroup** (1 campo)
- `inputModes` (InputModesSettings with: playerActionMapName, menuActionMapName, enableInputModes, logVerbose)

**Consumidor**: InputModeCoordinator (well-isolated)

**Status**: Low inter-dependency

---

## Ordem Sugerida de Migração (Phase 2)

### Dependency Graph (o que pode migrar em paralelo vs. sequencial)

```
Phase 2.1: Audio (independent, 2-3 days)
           ↓
Phase 2.2: Save (depends on no other migration, 2-3 days)
           ↓
Phase 2.3: InputModes (independent, 2-3 days)
           ↓
Phase 2.4: SessionOperational (requires startupRoute critical, 3-4 days)
           ├── defaultLoadingMode + defaultLoadingProfile
           ├── Copy TryValidateLoadingConfiguration()
           └── **CRITICAL**: startupRouteDefinition (bootstrap fail-point)
           ↓
Phase 2.5: RuntimePolicy (independent, 2-3 days)
           ├── reporter (well-isolated)
           ├── strictness (well-isolated)
           └── runtimePersistentScenesPolicy (SceneComposition)
```

**Critical Path**: StartupRouteDefinition (Phase 2.4) — must be migrated correctly or bootstrap fails.

**Parallel**: Phase 2.1 (Audio) + Phase 2.2 (Save) can happen simultaneously.

---

### Priorização Recomendada

**BAIXO RISCO (começar por aqui)**:
1. ✅ Phase 2.1: `audioDefaults` → AudioRuntimeConfigGroup
   - Single field, concreto consumidor (AudioAdapter)
   - Test: Launch scene, trigger audio playback

2. ✅ Phase 2.3: `inputModes` → InputModesRuntimeConfigGroup
   - Single field aggregating InputModesSettings
   - Test: Switch input mode (Gameplay ↔ Menu)

3. ✅ Phase 2.5: `reporter` + `strictness` → RuntimePolicyConfigGroup
   - Well-isolated dependencies
   - Test: Verify log filtering, error escalation

**MÉDIO RISCO**:
4. ⚠️ Phase 2.2: `saveConfig` → SaveRuntimeConfigGroup
   - Afeta persistência de dados
   - Test: Save checkpoint, load, verify data integrity

5. ⚠️ Phase 2.5: `runtimePersistentScenesPolicy` → RuntimePolicyConfigGroup
   - SceneCompositionAdapter dependency
   - Test: Verify persistent scenes stay loaded

**ALTO RISCO (deixar para último ou com suporte)**:
6. 🔴 Phase 2.4: `startupRouteDefinition` → SessionOperationalRuntimeConfigGroup
   - Bootstrap fail-point (bootstrap falha se ausente ou inválido)
   - Múltiplas dependências (SessionOperational, Boot)
   - Test: Full activity cycle from startup

7. 🔴 Phase 2.4: `defaultLoadingMode` + `defaultLoadingProfile` → SessionOperationalRuntimeConfigGroup
   - Validação condicional (Profile mode requer defaultLoadingProfile não-null)
   - Cópia de TryValidateLoadingConfiguration() necessária
   - Test: Load scenes com diferentes profiles

---

## Mapeamento Consumidor-by-Consumidor

### AudioAdapter

| Campo em origem | Acesso | Novo acesso (Phase 2.1) |
|---|---|---|
| `RuntimeModeConfig.AudioDefaults` | `audioDefaults` property | `registry.TryGetAudioRuntime(out cfg) → cfg.AudioDefaults` |

**Modificação necessária**:
```csharp
// ANTES
var defaults = RuntimeModeConfig.AudioDefaults;

// DEPOIS (Phase 2.1)
if (!registry.TryGetAudioRuntime(out var cfg))
    return;
var defaults = cfg.AudioDefaults;
```

---

### SaveService

| Campo em origem | Acesso | Novo acesso (Phase 2.2) |
|---|---|---|
| `RuntimeModeConfig.SaveConfig` | `saveConfig` property | `registry.TryGetSaveRuntime(out cfg) → cfg.SaveConfig` |

**Modificação necessária**:
```csharp
// ANTES
var config = RuntimeModeConfig.SaveConfig;

// DEPOIS (Phase 2.2)
if (!registry.TryGetSaveRuntime(out var cfg))
    return;
var config = cfg.SaveConfig;
```

---

### InputModeCoordinator

| Campo em origem | Acesso | Novo acesso (Phase 2.3) |
|---|---|---|
| `RuntimeModeConfig.inputModes` | `.playerActionMapName`, `.menuActionMapName`, `.enableInputModes` | `registry.TryGetInputModesRuntime(out cfg) → cfg.PlayerActionMapName`, etc. |

**Modificação necessária**:
```csharp
// ANTES
var playerMap = RuntimeModeConfig.inputModes.playerActionMapName;

// DEPOIS (Phase 2.3)
if (!registry.TryGetInputModesRuntime(out var cfg))
    return;
var playerMap = cfg.PlayerActionMapName;
```

---

### LoadingAdapter & SceneCompositionAdapter

| Campo em origem | Acesso | Novo acesso (Phase 2.4) |
|---|---|---|
| `RuntimeModeConfig.DefaultLoadingMode` | property | `registry.TryGetSessionOperationalRuntime(out cfg) → cfg.DefaultLoadingMode` |
| `RuntimeModeConfig.DefaultLoadingProfile` | property | `registry.TryGetSessionOperationalRuntime(out cfg) → cfg.DefaultLoadingProfile` |
| `RuntimeModeConfig.StartupRouteDefinition` | property | `registry.TryGetSessionOperationalRuntime(out cfg) → cfg.StartupRouteDefinition` |

**Modificação necessária**:
```csharp
// ANTES
var mode = RuntimeModeConfig.DefaultLoadingMode;
var profile = RuntimeModeConfig.DefaultLoadingProfile;
var startRoute = RuntimeModeConfig.StartupRouteDefinition;

// DEPOIS (Phase 2.4)
if (!registry.TryGetSessionOperationalRuntime(out var cfg))
    return;
var mode = cfg.DefaultLoadingMode;
var profile = cfg.DefaultLoadingProfile;
var startRoute = cfg.StartupRouteDefinition;
```

---

### SceneCompositionAdapter (Persistent Scenes)

| Campo em origem | Acesso | Novo acesso (Phase 2.5) |
|---|---|---|
| `RuntimeModeConfig.RuntimePersistentScenesPolicy` | property | `registry.TryGetRuntimePolicy(out cfg) → cfg.PersistentScenesPolicy` |

**Modificação necessária**:
```csharp
// ANTES
var policy = RuntimeModeConfig.RuntimePersistentScenesPolicy;

// DEPOIS (Phase 2.5)
if (!registry.TryGetRuntimePolicy(out var cfg))
    return;
var policy = cfg.PersistentScenesPolicy;
```

---

### DegradationReporter & Strictness Handler

| Campo em origem | Acesso | Novo acesso (Phase 2.5) |
|---|---|---|
| `RuntimeModeConfig.reporter` | `.dedupStrategy`, `.cooldownSeconds`, etc. | `registry.TryGetRuntimePolicy(out cfg) → cfg.Reporter.*` |
| `RuntimeModeConfig.strictness` | `.degradedAsError`, `.degradedAsException` | `registry.TryGetRuntimePolicy(out cfg) → cfg.Strictness.*` |

**Modificação necessária**:
```csharp
// ANTES
var dedupStrat = RuntimeModeConfig.reporter.dedupStrategy;

// DEPOIS (Phase 2.5)
if (!registry.TryGetRuntimePolicy(out var cfg))
    return;
var dedupStrat = cfg.Reporter.dedupStrategy;
```

---

## Validação Strategy por Campo

### Validações Existentes em RuntimeModeConfig that Move

```csharp
// MOVE para RuntimeConfigSetAsset.TryValidate()

// Atual em RuntimeModeConfig.TryValidateLoadingConfiguration()
if (defaultLoadingMode == RuntimeDefault)          // Error
if (defaultLoadingMode == Profile && !profile)    // Error
if (defaultLoadingMode == Profile && !profile.IsValid) // Error

// Atual em RuntimeModeConfig.TryValidateAudioConfiguration()
if (compositionProfile == Base11 && !audioDefaults)    // Error
// (compositionProfile stays in RuntimeModeConfig, so this validation moves but with different source)

// Nenhuma outra validação complexa encontrada
```

### Novas Validações em RuntimeConfigSetAsset

```csharp
// Para cada group:
public bool TryValidate(out string error)
{
    // RuntimePolicyConfigGroup
    if (runtimePolicy == null) return false; // All fields required

    // AudioRuntimeConfigGroup
    if (audioDefaults == null) return false; // Required

    // SessionOperationalRuntimeConfigGroup
    if (defaultLoadingMode == RuntimeDefault) error = "invalid";
    if (defaultLoadingMode == Profile && !defaultLoadingProfile) error = "missing";
    if (startupRouteDefinition == null) error = "required";

    // SaveRuntimeConfigGroup
    if (saveConfig == null) return false; // Required

    // InputModesRuntimeConfigGroup
    if (enableInputModes == false) error = "must be true";
    if (string.IsNullOrEmpty(playerActionMapName)) error = "required";
}
```

---

## Tipo de Risco por Campo

### 🟢 Baixo Risco

| Campo | Razão |
|-------|-------|
| `audioDefaults` | Single ref, concreto consumidor (AudioAdapter), TryGet pattern safe |
| `inputModes` | Single settings struct, InputModeCoordinator isolado |
| `reporter` | Settings struct, DegradationReporter isolado |
| `strictness` | Settings struct, Strictness handler isolado |

**Migração**: Pode começar imediatamente, fallback pattern simples

---

### 🟡 Médio Risco

| Campo | Razão |
|-------|-------|
| `runtimePersistentScenesPolicy` | SceneComposition dependency, mas bem-scoped |
| `defaultLoadingMode` + `defaultLoadingProfile` | Condicional validation (Profile mode), mas padrão claro |

**Migração**: Requer fallback durante Phase 2, teste completo em Phase 3

---

### 🔴 Alto Risco

| Campo | Razão | Mitigação |
|-------|-------|-----------|
| `startupRouteDefinition` | **Bootstrap fail-point**: Se absent/invalid, bootstrap falha no boot | MUST be present in RuntimeConfigSetAsset; fail-fast in Initialize() |
| `saveConfig` | **Data integrity**: Afeta save/load cycle | Unit test save/load após migrate; manual test checkpoint cycle |

**Migração**: Deixar para último (Phase 2.4 late), suporte de integração teste completo

---

## Checklist por Fase

### Phase 2.1: AudioRuntime

```
[ ] Create AudioRuntimeConfigGroup (audioDefaults field + TryValidate)
[ ] Create IAudioRuntimeConfigReadOnly interface
[ ] Add audioDefaults to RuntimeConfigSetAsset
[ ] Create test ConfigSet with audio group populated
[ ] Update AudioAdapter with registry.TryGetAudioRuntime() + fallback
[ ] Manual test: Audio playback works same as before
[ ] Log: "Audio config loaded from registry"
[ ] OLD refs: RuntimeModeConfig.AudioDefaults still accessible (Phase 3 removes)
```

---

### Phase 2.2: SaveRuntime

```
[ ] Create SaveRuntimeConfigGroup (saveConfig field + TryValidate)
[ ] Create ISaveRuntimeConfigReadOnly interface
[ ] Add saveConfig to RuntimeConfigSetAsset
[ ] Create test ConfigSet with save group populated
[ ] Update SaveService with registry.TryGetSaveRuntime() + fallback
[ ] Manual test: Save checkpoint, load, verify data integrity
[ ] Log: "Save config loaded from registry"
[ ] OLD refs: RuntimeModeConfig.SaveConfig still accessible (Phase 3 removes)
```

---

### Phase 2.3: InputModesRuntime

```
[ ] Create InputModesRuntimeConfigGroup (InputModesSettings + TryValidate)
[ ] Create IInputModesRuntimeConfigReadOnly interface
[ ] Add inputModes to RuntimeConfigSetAsset
[ ] Create test ConfigSet with inputmodes group populated
[ ] Update InputModeCoordinator with registry.TryGetInputModesRuntime() + fallback
[ ] Manual test: Switch input mode (Gameplay ↔ Menu)
[ ] Log: "InputModes config loaded from registry"
[ ] OLD refs: RuntimeModeConfig.inputModes still accessible (Phase 3 removes)
```

---

### Phase 2.4: SessionOperationalRuntime

```
[ ] ✅ COPY TryValidateLoadingConfiguration() to SessionOperationalRuntimeConfigGroup
[ ] Create SessionOperationalRuntimeConfigGroup (3 fields + TryValidate)
[ ] Create ISessionOperationalRuntimeConfigReadOnly interface
[ ] Add defaultLoadingMode, defaultLoadingProfile, startupRouteDefinition to RuntimeConfigSetAsset
[ ] Create test ConfigSet with sessionOp group fully populated
[ ] Update LoadingAdapter with registry.TryGetSessionOperationalRuntime() + fallback
[ ] Update SceneCompositionAdapter with registry.TryGetSessionOperationalRuntime() + fallback
[ ] **CRITICAL**: Verify startupRouteDefinition is accessible, not null
[ ] Manual test: Load routes with different profiles, startup scene loads correctly
[ ] Log: "SessionOperational config loaded from registry"
[ ] OLD refs: RuntimeModeConfig defaults still accessible (Phase 3 removes)
```

---

### Phase 2.5: RuntimePolicy

```
[ ] Create RuntimePolicyConfigGroup (3 fields: reporter, strictness, persistentScenesPolicy + TryValidate)
[ ] Create IRuntimePolicyConfigReadOnly interface
[ ] Add all 3 to RuntimeConfigSetAsset
[ ] Create test ConfigSet with policy group fully populated
[ ] Update DegradationReporter with registry.TryGetRuntimePolicy() + fallback
[ ] Update Strictness handler with registry.TryGetRuntimePolicy() + fallback
[ ] Update SceneCompositionAdapter with registry.TryGetRuntimePolicy() (persistent scenes) + fallback
[ ] Manual test: Log filtering, error escalation, persistent scenes stay loaded
[ ] Log: "RuntimePolicy config loaded from registry"
[ ] OLD refs: RuntimeModeConfig.reporter, strictness, runtimePersistentScenesPolicy still accessible (Phase 3 removes)
```

---

## Mapa Inverso: Qual Consumidor Afeta Qual Campo

| Consumidor | Campos que Lê | Fase de Migração | Risco |
|-----------|-----------------|------------------|-------|
| **AudioAdapter** | audioDefaults | 2.1 | Baixo |
| **SaveService** | saveConfig | 2.2 | Alto |
| **InputModeCoordinator** | inputModes | 2.3 | Baixo |
| **LoadingAdapter** | defaultLoadingMode, defaultLoadingProfile | 2.4 | Médio |
| **SceneCompositionAdapter** | defaultLoadingProfile, startupRouteDefinition, runtimePersistentScenesPolicy | 2.4 (routes) + 2.5 (policy) | Alto + Médio |
| **DegradationReporter** | reporter (dedupStrategy, cooldownSeconds, etc.) | 2.5 | Baixo |
| **Strictness handler** | strictness (degradedAsError, degradedAsException) | 2.5 | Baixo |
| **Bootstrap** | startupRouteDefinition | 2.4 | Alto |

---

## Questões Especiais

### Q1: BootstrapConfigAsset não deve ser fonte canônica?

**Resposta**: Correto. RuntimeModeConfig continua sendo **entrada única** no Resources. BootstrapConfigAsset continuará apenas como um wrapper de DI se necessário, mas não é source-of-truth de config.

**Consequência**: Ignore BootstrapConfigAsset durante migração; migre direto de RuntimeModeConfig → RuntimeConfigSetAsset.

---

### Q2: RuntimeModeConfig.configSet reference — quando é criado?

**Resposta**: Na Phase 1 (Foundation). RuntimeModeConfig recebe novo field:
```csharp
[SerializeField] private RuntimeConfigSetAsset configSet; // Phase 1
```

Todos os 9 campos migram para este configSet, referenciado por RuntimeModeConfig.

---

### Q3: Os 2 campos que ficam (modeOverride, compositionProfile) — ficam pra sempre?

**Resposta**: Sim. Estes são **decisions**, não config. Pertencem ao entry point RuntimeModeConfig porque decidem como runtime se comporta globalmente (strict vs release, which composition). Não migram,

---

### Q4: Na Phase 3, como remover old refs?

**Resposta**:
1. Todos adapters lêem de registry (sem fallback)
2. Remove campo de RuntimeModeConfig
3. Remove property getter

Exemplo:
```csharp
// REMOVE
[SerializeField] private AudioDefaultsAsset audioDefaults;
public AudioDefaultsAsset AudioDefaults => audioDefaults;

// Clients já estão usando registry, não vão quebrar
```

---

## Resumo de Migração por Tipo de Dado

| Tipo | Campos | Destino | Padrão | Risco |
|------|--------|---------|--------|-------|
| **Asset ref** | audioDefaults, saveConfig, startupRouteDefinition, runtimePersistentScenesPolicy, defaultLoadingProfile | ConfigGroup | Serialized field, TryValidate() | Médio (refs podem dangling) |
| **Enum** | defaultLoadingMode | ConfigGroup | Enum field, conditional validation | Médio (validation logic copia) |
| **Struct** | reporter, strictness, inputModes | ConfigGroup | [Serializable] struct, copy content | Baixo (no refs, isolated) |

---

## Dependências Between Migrations (Topological Order)

```
Phase 2.1 (Audio)          [Independent]
   ↓ (parallel allowed)
Phase 2.2 (Save)           [Independent]
   ↓ (parallel allowed)
Phase 2.3 (InputModes)     [Independent]
   ↓ (must sequence)
Phase 2.4 (SessionOp)      [**CRITICAL**: startupRouteDefinition]
   ↓ (must sequence)
Phase 2.5 (RuntimePolicy)  [Semi-independent, but needs SceneComp ready]
   ↓
Phase 3 (Consolidation)    [Remove old refs from RuntimeModeConfig]
```

**Parallel Possible**: 2.1 + 2.2 + 2.3 can happen simultaneously (no cross-dependencies)
**Must Sequence**: 2.4 before full Scene operations; 2.5 before runtime logging/strictness fully migrated

---

## Conclusão & Próximos Passos

### ✅ Este documento fornece:

1. **Mapa exato** de 9 campos → 5 config groups
2. **Ordem recomendada** (Phase 2.1 → 2.5)
3. **Risco por campo** (Baixo/Médio/Alto)
4. **Consumidor por campo** (para coordenação)
5. **Checklist por fase** (para tracking)
6. **Validações** que migram
7. **Questões especiais** respondidas

### 🚀 Próximo: Implementação Phase 1 (Foundation)

1. Create RuntimeConfigSetAsset + 5 group stubs
2. Create RuntimeConfigRegistry + TryGet* methods
3. Update RuntimeModeConfig com configSet reference
4. Create interfaces read-only
5. Bootstrap integration

**Tempo estimado**: 7 dias (Phase 1)
**Início**: A partir de quando Phase 1 skeleton estiver pronto

---

**Documento Criado**: 2026-05-13
**Status**: Documentação apenas
**Próxima Ação**: Aprovar ordem sugerida, começar Phase 1


