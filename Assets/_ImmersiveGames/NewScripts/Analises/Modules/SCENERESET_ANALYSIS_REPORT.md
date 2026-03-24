# 📊 ANÁLISE DO MÓDULO SCENERESET - REDUNDÂNCIAS INTERNAS E COESÃO DO RESET LOCAL

**Data:** 23 de março de 2026
**Projeto:** GameJam2025
**Módulo:** SceneReset
**Versão do Relatório:** 1.0
**Status:** ✅ Análise Inicial Derivada da antiga análise de WorldLifecycle, atualizada para a divisão atual

---

## 📋 ÍNDICE

1. [Resumo Executivo](#resumo-executivo)
2. [Estrutura do Módulo](#estrutura-do-módulo)
3. [Redundâncias Internas no SceneReset](#redundâncias-internas-no-scenereset)
4. [Cruzamento com WorldReset](#cruzamento-com-worldreset)
5. [Análise de Sobreposição](#análise-de-sobreposição)
6. [Recomendações de Consolidação](#recomendações-de-consolidação)
7. [Impacto Total Estimado](#impacto-total-estimado)
8. [Conclusão](#conclusão)

---

## 🎯 Resumo Executivo

### Descoberta Principal: **O PIPELINE LOCAL JÁ EXISTE, MAS O MÓDULO AINDA ESTÁ PESADO**

A maior melhoria da divisão atual é que o reset local deixou de ficar escondido dentro do antigo `WorldLifecycle` e passou a ter um módulo próprio.

**Hoje o `SceneReset` é claramente responsável por:**
- controller e serialização local
- runner/facade local
- pipeline local explícito
- fases do reset local
- hooks e infraestrutura de spawn local

**Estatísticas atuais:**
- `SceneReset`: ~2210 linhas / 25 arquivos
- Maior arquivo: `SceneResetContext.cs` (~543 linhas)
- Segundo maior: `SceneResetController.cs` (~416 linhas)
- Terceiro maior: `SceneResetHookRunner.cs` (~219 linhas)

**Leitura atual:**
- a arquitetura melhorou muito com o pipeline explícito
- o maior problema agora é **peso interno do contexto e do controller**, não boundary externo

---

## 📁 Estrutura do Módulo

```text
SceneReset/
├── Bindings/
│   ├── SceneResetController.cs (~416)
│   └── SceneResetRunner.cs (~102)
├── Hooks/
│   ├── ISceneResetHook.cs
│   ├── ISceneResetHookOrdered.cs
│   ├── SceneResetHookBase.cs
│   └── SceneResetHookRegistry.cs
├── Runtime/
│   ├── ISceneResetPhase.cs
│   ├── SceneResetContext.cs (~543)
│   ├── SceneResetControllerLocator.cs (~132)
│   ├── SceneResetFacade.cs (~98)
│   ├── SceneResetHookRunner.cs (~219)
│   ├── SceneResetPipeline.cs (~82)
│   └── Phases/
│       ├── AcquireResetGatePhase.cs
│       ├── BeforeDespawnHooksPhase.cs
│       ├── DespawnPhase.cs
│       ├── AfterDespawnHooksPhase.cs
│       ├── ScopedParticipantsResetPhase.cs (~108)
│       ├── SpawnPhase.cs
│       └── AfterSpawnHooksPhase.cs
└── Spawn/
    ├── IWorldSpawnContext.cs
    ├── IWorldSpawnService.cs
    ├── IWorldSpawnServiceRegistry.cs
    ├── WorldSpawnContext.cs
    ├── WorldSpawnServiceFactory.cs (~163)
    └── WorldSpawnServiceRegistry.cs
```

---

## 🔴 Redundâncias Internas no SceneReset

### 1️⃣ `SceneResetContext` está grande demais

**Problema:**
O contexto virou uma estrutura central com estado demais:
- request
- runtime state
- hooks state
- spawn state
- logs/counters
- caches auxiliares

**Impacto:**
- 543 linhas para um context é muito alto
- risco de virar “sacola de tudo” do pipeline local
- dificulta leitura do que cada phase realmente usa

**Severidade:** 🔴 **CRÍTICA**

---

### 2️⃣ `SceneResetController` ainda é pesado

**Problema:**
Mesmo com pipeline e runner, o controller continua grande para o papel esperado de um binding/local controller.

**Impacto:**
- mistura lifecycle + fila + coordenação local demais
- continua sendo ponto de concentração no módulo

**Severidade:** 🔴 **ALTA**

---

### 3️⃣ `SceneResetHookRunner` ainda concentra muita lógica de hook

**Problema:**
A extração dos hooks foi um acerto, mas o runner ainda ficou robusto demais.

**Impacto:**
- uma parte considerável da complexidade do reset local ainda está aqui
- qualquer ajuste em ordering/caching de hooks tende a cair nesse arquivo

**Severidade:** 🟡 **ALTA**

---

### 4️⃣ Spawn local ainda está denso

**Problema:**
`WorldSpawnServiceFactory` continua relativamente pesado para uma infraestrutura de spawn local.

**Impacto:**
- o pipeline local já está separado, mas o bloco de spawn ainda pode ser mais enxuto

**Severidade:** 🟡 **MÉDIA**

---

## 🔄 Cruzamento com WorldReset

A fronteira hoje está correta:
- `WorldReset` decide macro
- `SceneReset` executa local

O que resta não é “sobreposição grave”, e sim acoplamento natural de pipeline.

**Isso é importante:** o antigo problema de `WorldLifecycle × Gameplay` não deve mais ser carregado automaticamente para o `SceneReset` atual.

---

## 📊 Análise de Sobreposição

| Área | SceneReset | WorldReset | Situação |
|---|---|---|---|
| Pipeline local determinístico | ✅ | ❌ | Correto em `SceneReset` |
| Queue / lifecycle local | ✅ | ❌ | Correto em `SceneReset` |
| Decisão macro de reset | ❌ | ✅ | Correto em `WorldReset` |
| Pós-condição macro | ❌ | ✅ | Correto em `WorldReset` |
| Spawn local | ✅ | ⚠️ | Ainda relativamente denso |

---

## 🛠️ Recomendações de Consolidação

### Prioridade 1
Quebrar `SceneResetContext` por responsabilidade interna.

### Prioridade 2
Enxugar `SceneResetController` para deixá-lo mais próximo de:
- queue
- lifecycle
- delegação

### Prioridade 3
Revisar `SceneResetHookRunner` e o bloco de spawn para reduzir concentração.

---

## 📈 Impacto Total Estimado

- Redundância/overlap interna: **12-18%**
- Hotspots principais: `SceneResetContext`, `SceneResetController`, `SceneResetHookRunner`
- Complexidade atual: **alta**, mas já localizada no lugar certo

---

## ✅ Conclusão

O `SceneReset` é o maior ganho estrutural da divisão atual.
Ele finalmente torna explícito o que antes estava enterrado no antigo `WorldLifecycle`.

O problema que sobra não é de ownership externo, mas de **peso interno**.

**Resumo:** módulo correto, pipeline correto, naming correto; próximo passo é reduzir concentração dentro do próprio `SceneReset`.
