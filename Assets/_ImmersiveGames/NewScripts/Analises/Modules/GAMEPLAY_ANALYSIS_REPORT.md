> [!NOTE]
> **Status atual confirmado:** `Gameplay` continua dono da semântica de atores, spawn local e rearm por grupo.
>
> **Atualizado pelo estado atual do código e validado em runtime:**
> - `GameplayStateGate` passou a ser o nome canônico do gate de estado;
> - `ActorGroupRearmOrchestrator` foi reduzido e delega resolução/execução;
> - a família de spawn foi consolidada (`ActorSpawnActorIdHelper` + `GameplayStateControllerInjector`);
> - o overlap mais perigoso com `WorldLifecycle` deixou de ser o hotspot principal;
> - o expurgo de `PlayerActorAdapter` e `ActorLifecycleHookBase` foi concluído;
> - referências a `ContentSwap` neste relatório devem ser lidas como histórico, não como estado vigente.
>
---

# 📊 ANÁLISE DO MÓDULO GAMEPLAY - REDUNDÂNCIAS E OTIMIZAÇÕES

**Data:** 22 de março de 2026
**Projeto:** GameJam2025
**Módulo:** Gameplay (`Assets/_ImmersiveGames/NewScripts/Modules/Gameplay`)
**Versão do Relatório:** 1.2
**Status:** ✅ Editado sobre a versão 1.1, preservando a estrutura do relatório e atualizado contra o código validado em runtime após expurgo, reorganização por capability e homogeneização de nomes

---

## 📋 ÍNDICE

1. [Resumo Executivo](#resumo-executivo)
2. [Estrutura do Módulo](#estrutura-do-módulo)
3. [Redundâncias Internas](#redundâncias-internas)
4. [Cruzamento com Outros Módulos](#cruzamento-com-outros-módulos)
5. [Análise de Sobreposição](#análise-de-sobreposição)
6. [Recomendações de Consolidação](#recomendações-de-consolidação)
7. [Impacto Total Estimado](#impacto-total-estimado)
8. [Conclusão](#conclusão)

---

## 🎯 Resumo Executivo

### Tamanho e Status

- **Total de linhas:** ~3500-3800 LOC (médio-grande, já com extrações auxiliares)
- **Arquivos:** ~47 arquivos C# (Runtime + Infrastructure + Content)
- **Status:** ✅ **Estruturalmente mais saudável do que na análise original**
- **Redundância Estimada:** ~3-6% (resíduo mais localizado)
- **Cruzamento com outros módulos:** moderado; o boundary com reset/spawn está mais saudável

### Leitura atualizada
O módulo continua grande, mas os hotspots mais críticos da análise original foram reduzidos:

- `GameplayStateGate` deixou de concentrar binding, snapshot e observability no mesmo arquivo;
- `ActorGroupRearmOrchestrator` deixou de concentrar resolução de targets, resolução de componentes e execução das fases no mesmo arquivo;
- a família de spawn deixou de repetir garantia de `ActorId` e injeção do gate de estado;
- o cruzamento com `WorldLifecycle` não é mais a leitura prioritária do módulo.

### Descobertas Principais (estado base da análise original)

| Item | Descrição | Severidade |
|------|-----------|-----------|
| **TryResolve residual** | Ainda existe boilerplate de resolução lazy, mas mais localizado | 🟢 BAIXA |
| **Event Binding no State** | Foi redistribuído para `GameplayStateGateBindings` | 🟢 BAIXA |
| **Mutex/serial no Rearm** | Continua existindo, mas agora em orquestrador mais fino | 🟢 BAIXA |
| **Cruzamento com WorldLifecycle** | Deixou de ser o hotspot principal; hoje é boundary intencional | 🟡 BAIXA |
| **Grandes serviços** | `StateDependentService` e `ActorGroupRearmOrchestrator` foram reduzidos | 🟢 BAIXA |
| **Família de Spawn** | Redundância interna reduzida e validada por log | 🟢 BAIXA |

## 📁 Estrutura do Módulo

```
Gameplay/
├── Actors/
│   ├── Core/
│   │   ├── ActorKind.cs
│   │   ├── ActorRegistry.cs
│   │   ├── IActor.cs
│   │   ├── IActorKindProvider.cs
│   │   ├── IActorLifecycleHook.cs
│   │   └── IActorRegistry.cs
│   ├── Player/
│   │   ├── PlayerActor.cs
│   │   └── Movement/
│   │       ├── PlayerMoveInputReader.cs
│   │       └── PlayerMovementController.cs
│   ├── Eater/
│   │   ├── EaterActor.cs
│   │   └── Movement/
│   │       └── EaterRandomMovementController.cs
│   └── Dummy/
│       └── DummyActor.cs
├── Camera/
│   ├── GameplayCameraBinder.cs
│   ├── GameplayCameraResolver.cs
│   └── IGameplayCameraResolver.cs
├── Rearm/
│   ├── Core/
│   │   ├── ActorGroupRearmContracts.cs
│   │   ├── ActorGroupRearmOrchestrator.cs
│   │   ├── ActorGroupRearmTargetResolver.cs
│   │   ├── ActorGroupRearmComponentResolver.cs
│   │   ├── ActorGroupRearmExecutor.cs
│   │   ├── ActorGroupRearmExecutionModels.cs
│   │   └── ActorKindMatchRules.cs
│   ├── Strategy/
│   │   ├── ActorGroupRearmDefaultTargetClassifier.cs
│   │   ├── ActorGroupRearmRegistryDiscoveryStrategy.cs
│   │   ├── ActorGroupRearmSceneScanDiscoveryStrategy.cs
│   │   ├── IActorGroupRearmDiscoveryStrategy.cs
│   │   └── IActorGroupRearmTargetClassifier.cs
│   └── Integration/
│       ├── IActorGroupRearmWorldParticipant.cs
│       └── PlayerActorGroupRearmWorldParticipant.cs
├── Spawn/
│   ├── ActorSpawnActorIdHelper.cs
│   ├── ActorSpawnServiceBase.cs
│   ├── DummyActorSpawnService.cs
│   ├── EaterSpawnService.cs
│   ├── GameplayStateControllerInjector.cs
│   ├── PlayerSpawnActorResolver.cs
│   ├── PlayerSpawnService.cs
│   └── Definitions/
│       └── WorldDefinition.cs
├── State/
│   ├── GameplayAction.cs
│   ├── GameplayMoveGateDecisionLogger.cs
│   ├── GameplayStateGate.cs
│   ├── GameplayStateGateBindings.cs
│   ├── GameplayStateSnapshot.cs
│   ├── IGameplayStateGate.cs
│   ├── SystemAction.cs
│   └── UiAction.cs
└── Content/
    ├── Prefabs/
    └── Worlds/

TOTAL: módulo reorganizado por capability, com shape mais coerente que o da análise original
```

## 🔴 REDUNDÂNCIAS INTERNAS

### 1️⃣ PADRÃO TRYRESOLVE DUPLICADO (🟡 MÉDIA - 12 LOC)

**Localização atual:** `GameplayStateGate.cs` (resolução lazy residual)

**Estado atual:**
O padrão de `TryResolve...` não é mais o problema dominante do módulo. Ele continua existindo em pontos localizados, mas deixou de ser a parte mais custosa do `Gameplay`.

**Impacto atual:**
- ⚠️ ainda existe boilerplate de resolução lazy
- ✅ porém já não domina a classe como antes
- ✅ o ganho marginal aqui é baixo se comparado ao que já foi consolidado em State/Rearm/Spawn

**Severidade:** 🟢 **BAIXA**

### 2️⃣ EVENT BINDING BOILERPLATE (🟡 MÉDIA - ~60 LOC)

**Localização anterior:** `GameplayStateGate.cs`

**Estado atual:**
O boilerplate de binding do estado foi redistribuído para `GameplayStateGateBindings`. O custo de leitura caiu porque o `StateDependentService` deixou de carregar sozinho a criação/registro/unregister dos eventos.

**Impacto atual:**
- ✅ a duplicação estrutural foi reduzida
- ✅ o binding continua explícito, mas mais localizado
- ⚠️ ainda existe boilerplate no módulo como um todo, porém este ponto deixou de ser hotspot do `Gameplay`

**Severidade:** 🟢 **BAIXA**

### 3️⃣ DEDUPLICAÇÃO DE EVENTOS FRAME-LEVEL (🟢 BAIXA - ~40 LOC)

**Localização anterior:** `GameplayStateGate.cs`

**Estado atual:**
A deduplicação frame-level continua existindo, mas agora está mais contida dentro do bloco de state/bindings e não é mais um problema estrutural principal do módulo.

**Impacto atual:**
- ✅ comportamento preservado no runtime
- ✅ custo de leitura reduzido no arquivo principal
- ⚠️ permanece como detalhe de implementação, não mais como hotspot

**Severidade:** 🟢 **BAIXA**

### 4️⃣ LOGGING VERBOSE SIMILAR (🟡 MÉDIA - ~30 LOC)

**Localização anterior:** `GameplayStateGate.cs`

**Estado atual:**
A observabilidade específica de decisão de `Move` foi extraída para `GameplayMoveGateDecisionLogger`, reduzindo a mistura entre gate evaluation e logging transicional.

**Impacto atual:**
- ✅ melhor separação entre decisão e observabilidade
- ✅ logs `[OBS][GRS]` continuam coerentes no runtime validado
- ⚠️ ainda há espaço para padronização cross-module, mas isso já não é prioridade interna do `Gameplay`

**Severidade:** 🟢 **BAIXA**

### 5️⃣ MUTEX PATTERN SIMILAR (🟢 BAIXA - ~3 LOC)

**Localização atual:** `ActorGroupRearmOrchestrator.cs`

**Estado atual:**
O padrão de mutex/serial continua existindo no orquestrador, mas agora dentro de uma classe mais fina, que deixou de concentrar resolução de targets, resolução de components e execução das fases.

**Impacto atual:**
- ✅ permanece adequado ao papel de coordenação
- ✅ não é mais um ponto de redundância estrutural relevante
- ⚠️ continua sendo detalhe de sincronização do pipeline

**Severidade:** 🟢 **BAIXA**

### 6️⃣ GRANDE CLASSE MONOLÍTICA (🟡 MÉDIA - ~505 LOC)

**Localização:** `GameplayStateGate.cs`

**Problema anterior:**
O serviço misturava:
1. estado do jogo
2. gate validation
3. event binding/unbinding
4. readiness snapshot
5. logging transicional
6. deduplicação frame-level

**Estado atual:**
Esse hotspot foi reduzido com a extração de:
- `GameplayStateGateBindings`
- `GameplayStateSnapshot`
- `GameplayMoveGateDecisionLogger`

O `StateDependentService` permaneceu como coordenador/fachada do contrato público, e o runtime validou que `Move` continua bloqueando/liberando corretamente em `IntroStage`, `Playing`, `PostGame`, restart e saída para menu.

**Impacto atual:**
- ✅ responsabilidade mais distribuída
- ✅ menor acoplamento interno
- ✅ sem regressão funcional aparente pelo log
- ⚠️ ainda existe serviço central, mas não mais no nível monolítico da análise original

**Severidade:** 🟢 **BAIXA**

### 7️⃣ GRANDE CLASSE MONOLÍTICA (🟡 MÉDIA - ~467 LOC)

**Localização:** `ActorGroupRearmOrchestrator.cs`

**Problema anterior:**
O orquestrador misturava:
1. mutex/serial management
2. dependency resolution
3. target building
4. reset execution
5. component discovery
6. logging/policy

**Estado atual:**
Esse hotspot foi reduzido com a extração de:
- `ActorGroupRearmTargetResolver`
- `ActorGroupRearmComponentResolver`
- `ActorGroupRearmExecutor`
- `ActorGroupRearmExecutionModels`

O orquestrador permaneceu como coordenador fino, e o runtime validou que o restart/reset continua fechando sem regressão aparente.

**Impacto atual:**
- ✅ pipeline mais legível
- ✅ responsabilidades mais bem separadas
- ✅ rearm continua íntegro no fluxo real
- ⚠️ ainda pode haver reorganização arquitetural futura, mas a redundância estrutural principal foi reduzida

**Severidade:** 🟢 **BAIXA**

## 🔴 CRUZAMENTO COM OUTROS MÓDULOS

### Análise Critical: Gameplay ↔ WorldLifecycle

**Descoberta atualizada:** o cruzamento com reset/spawn deixou de ser o hotspot principal do módulo.

#### A. Spawn Service Duplication

**Leitura atualizada:**
- `ActorSpawnServiceBase` continua sendo **específico de Gameplay**, mas hoje o spawn local já está integrado ao trilho novo via `IWorldSpawnService`
- a família de spawn foi consolidada internamente com:
    - `ActorSpawnActorIdHelper`
    - `GameplayStateControllerInjector`

**Estado atual:**
- ✅ o spawn local continua sendo responsabilidade do módulo `Gameplay`
- ✅ a integração com reset/spawn externo é intencional
- ✅ o runtime validou spawn/despawn/re-spawn, `ActorId` e registro de serviços sem regressão
- ⚠️ esse ponto já não deve ser lido como “duplicação crítica”

#### B. Reset/Rearm Logic

**Leitura atualizada:**
- `ActorGroupRearmOrchestrator` hoje deve ser lido como **rearm de grupo específico**
- o macro reset pertence ao trilho de `WorldReset` / `SceneReset`
- `PlayerActorGroupRearmWorldParticipant` é uma bridge explícita, não mais sintoma de ownership confuso

**Impacto atual:** 🟡 **BAIXO** — boundary intencional, com resíduo de naming/integração, não mais hotspot estrutural

#### C. Actor Registry Interaction

**Leitura atualizada:**
- `ActorRegistry` continua sendo peça central do runtime de atores do módulo
- o uso por spawn e rearm permanece legítimo
- o log validado não mostrou race/regressão funcional aparente neste eixo

**Conclusão desta seção:**
o relatório original superestimava o overlap com `WorldLifecycle`. No estado atual, o ganho real já não está em reconciliar ownership de spawn/reset, e sim em consolidar e depois reorganizar a arquitetura interna do módulo.

### Análise: Gameplay ↔ GameLoop

**Descoberta atualizada:** a dependência com `GameLoop` continua legítima e mais estável.

- `GameplayStateGate` continua consumindo sinais do ciclo de jogo para decidir `Move`
- o comportamento foi revalidado em runtime durante:
    - `IntroStage`
    - `Playing`
    - `PostGame`
    - restart
    - `ExitToMenu`

**Impacto atual:** 🟢 **BAIXO** — integração saudável; sem novo hotspot cross-module principal

### Análise: Gameplay ↔ Gates

**Descoberta atualizada:** a relação com gates continua correta e não apareceu como hotspot principal após a consolidação do State.

- `Move` continua respeitando gate fechado / pause / `NotPlaying`
- o log validado confirma transições corretas de bloqueio/liberação

**Impacto atual:** 🟢 **BAIXO** — integração funcional, não redundância estrutural principal

## 📊 Análise de Sobreposição

### Matriz de Cruzamento: Gameplay × (GameLoop + WorldLifecycle + Gates)

| Cruzamento | Estado atual | Severidade |
|------------|--------------|------------|
| Gameplay × GameLoop | saudável; necessário para gating de ações | 🟢 BAIXA |
| Gameplay × WorldLifecycle/WorldReset | boundary mais saudável; bridge explícita | 🟡 BAIXA |
| Gameplay × SceneReset/Spawn | integração intencional e validada | 🟡 BAIXA |
| Gameplay × Gates | correto e funcional | 🟢 BAIXA |

**Leitura atualizada:** o módulo já não está em zona de sobreposição crítica. Os hotspots reais estavam internos (State, Rearm, Spawn) e foram reduzidos.

### Análise Quantitativa

- **Redundância interna atual:** ~3-6%
- **Hotspots principais da análise original:** reduzidos
- **Hotspot cross-module com WorldLifecycle:** reduzido para boundary intencional
- **Próximo ganho real:** mais arquitetural/organizacional do que comportamental, salvo nova evidência em log/código

## 💡 RECOMENDAÇÕES DE CONSOLIDAÇÃO

### Fase 1: Consolidação de Padrões

| Item | Estado atual | Economia |
|------|--------------|----------|
| StateDependentService | consolidado e validado | ✅ ganho estrutural real |
| Spawn family | consolidada e validada | ✅ ganho estrutural real |
| Logging/binding boilerplate | reduzido/localizado | ✅ ganho de leitura |

**Leitura atualizada:** esta fase deixou de ser projeção futura e passou a compor o baseline do módulo.

### Fase 2: Refactoring de Responsabilidades

| Item | Estado atual | Economia |
|------|--------------|----------|
| StateDependentService | reduzido internamente | ✅ ganho alto |
| ActorGroupRearmOrchestrator | reduzido internamente | ✅ ganho alto |
| Testabilidade / leitura | melhor distribuída | ✅ ganho relevante |

**Leitura atualizada:** o ganho principal desta fase já foi capturado.

### Fase 3: Consolidação Cross-Module

| Item | Impacto | Status |
|------|---------|--------|
| Boundary com WorldLifecycle | reclassificado como integração intencional | ✅ reduzido |
| Spawn/Reset ownership | não é mais hotspot crítico | ✅ reduzido |
| Próximo ganho | reorganização arquitetural do módulo | ⏳ recomendado |

**Impacto Total (estado atual):** os principais ganhos de redundância comportamental já foram capturados. O módulo agora está mais próximo de uma etapa de reorganização arquitetural do que de nova rodada de correção funcional.

## ✅ CONCLUSÃO

### Status Overall

**Gameplay continua sendo um módulo importante e agora está mais saudável do que no estado descrito pela análise original.**

✅ **Pontos Fortes Atualizados:**
- Estado/ação (`GameplayStateGate`) consolidado e validado
- Rearm por grupo consolidado e validado
- Família de spawn consolidada e validada
- Integração com reset/spawn externo mais clara
- Runtime de atores continua íntegro no ciclo real

⚠️ **Problemas ainda relevantes:**
- a árvore do módulo continua com muita fragmentação por subpasta/intenção
- ainda há drift de naming/organização
- o próximo ganho real parece mais arquitetural do que comportamental

### Recomendação de Ação

| Fase | Ação | Prioridade | Timeline |
|------|------|-----------|----------|
| **1** | Consolidar `GameplayStateGate` | ✅ Feita | Concluído |
| **2** | Consolidar `ActorGroupRearmOrchestrator` | ✅ Feita | Concluído |
| **3** | Consolidar família de spawn | ✅ Feita | Concluído |
| **4** | Atualizar análise e congelar comportamento | 🔴 ALTA | Imediato |
| **5** | Propor reorganização arquitetural do módulo | 🔴 ALTA | Próximo passo |

**Leitura atualizada:** não há indicação forte para continuar atacando comportamento sem nova evidência. O próximo passo coerente é reorganização arquitetural.

### Priorização

**Ordem recomendada agora:**

1. **Primeiro:** fechar a atualização desta análise
2. **Segundo:** propor reorganização arquitetural do módulo
3. **Terceiro:** só voltar a mexer em comportamento se aparecer nova redundância forte em log/código

**Leitura atualizada:** o módulo já saiu da zona crítica de duplicidade comportamental.

### Métricas de Sucesso

- ✅ `GameplayStateGate` distribuído internamente
- ✅ `ActorGroupRearmOrchestrator` reduzido e delegando responsabilidades
- ✅ família de spawn consolidada
- ✅ runtime validado em gameplay, restart e exit-to-menu
- ⏳ próxima métrica: reorganização arquitetural do módulo validada

---

**Relatório gerado:** 22 de março de 2026
**Status:** ✅ Editado e alinhado ao estado validado do módulo
**Próxima ação:** alinhar os docs canônicos ao shape reorganizado e decidir se a raiz `Gameplay` será mantida
**Prioridade Geral:** MÉDIA (comportamento estabilizado; próximo ganho é arquitetural)
