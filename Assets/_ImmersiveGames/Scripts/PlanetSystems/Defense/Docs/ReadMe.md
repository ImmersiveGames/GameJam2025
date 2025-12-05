Perfeito — vou produzir agora a **versão completa e oficial do README**, totalmente reescrita, refletindo **todo o estado atual do sistema**, incluindo:

* Sistema de defesa por planetas
* Pipeline completo de minions v2
* Estratégias de spawn, entrada, perseguição
* Behavior Profiles (V2)
* PoolSystem e integração
* Orquestração
* Facing + rotação + inércia
* Nomenclatura padronizada
* Remoção do legado
* Fluxos detalhados
* Diagramas (ASCII)
* Troubleshooting
* Glossário

Este README será extenso, estruturado, documentado e pronto para versionar no repositório oficial.

---

# **README — Planet Defense & Minion Behavior System (Versão Atualizada)**

**Autor:** Immersive Games
**Versão:** V2.5 (Pipeline de comportamentos unificado + rotação configurável)
**Última atualização:** [DATA ATUAL]

---

# **1. Visão Geral**

Este documento descreve o **sistema completo de Defesa Planetária** e o **pipeline V2 de Minions** utilizado pelos planetas para gerar minions defensivos, orientar entrada em órbita, perseguir ameaças e aplicar estratégias comportamentais.

O sistema é composto por:

* **Orquestrador** da defesa planetária
* **Wave Presets** que definem a cadência de geração
* **Minion Behavior Profiles (V2)**
* **Entry Strategies**
* **Chase Strategies**
* **Spawn Patterns**
* **PoolSystem**
* **Minion Runtime Handlers (Entry, Orbit, Chase)**

Todo o sistema foi refatorado para suportar:

* Configuração por ScriptableObjects
* Arquitetura limpa (cada camada conhece somente o necessário)
* Nenhuma dependência do legado (DefenseMinionConfigSO foi removido)
* Controle total de rotação durante a perseguição
* Estratégias consistentes e substituíveis por perfil
* Suporte a múltiplos roles e behaviors por planeta

---

# **2. Arquitetura Geral**

```
┌─────────────────────────────────────────────┐
│                PlanetDefense                │
└─────────────────────────────────────────────┘
         │
         ▼
┌──────────────────────────────┐
│ DefenseEntryConfigSO (V2)    │  ← por planeta
│  role → WavePreset + Profile │
└──────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ PlanetDefenseOrchestrationService │
│  resolve entry + roleConfig       │
│  monta PlanetDefenseSetupContext  │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ PlanetDefenseSetupContext (V2)     │
│  WavePreset                        │
│  MinionBehaviorProfile             │
│  SpawnOffset / SpawnRadius         │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ RealPlanetDefenseWaveRunner        │
│  Spawn via PoolSystem              │
│  ApplyEntryStrategy                │
│  ApplyChaseStrategy                │
│  ApplyProfile (V2)                 │
└────────────────────────────────────┘
         │
         ▼
┌────────────────────────────────────┐
│ DefenseMinionController            │
│ MinionEntryHandler                 │
│ MinionOrbitWaitHandler             │
│ MinionChaseHandler                 │
└────────────────────────────────────┘
```

---

# **3. Componentes do Sistema**

## **3.1 DefenseEntryConfigSO (V2)**

Define COMO o planeta reage a cada tipo de ameaça (role).

Cada role define:

* **WavePresetSo** (como spawnar e em que ritmo)
* **DefenseMinionBehaviorProfileSO** (COMO o minion se move e se comporta)
* **SpawnOffsetOverride**
* **Modo de seleção (Default / Role-Based)**

**Nenhum conhecimento sobre pool/data antiga permanece aqui.**

---

## **3.2 WavePresetSo**

Define o “padrão de ondas” do minion:

* `PoolData` → de qual pool retornar minions
* `SpawnPattern (DefenseSpawnPatternSO)` → como posicioná-los
* `NumberOfMinionsPerWave`
* `IntervalBetweenWaves`
* `WaveBehaviorProfile (opcional)` → override de comportamento por WAVE

`SpawnPattern` é completamente plugável.

---

## **3.3 DefenseMinionBehaviorProfileSO (V2)**

É o **coração** do sistema de minions.

Cada perfil define:

### **Entrada**

* `EntryDurationSeconds`
* `InitialScaleFactor`
* `OrbitIdleSeconds`
* `EntryStrategy (MinionEntryStrategySo)`

### **Perseguição**

* `ChaseSpeed`
* `ChaseStrategy (MinionChaseStrategySo)`

### **Rotação (V2.5)**

* `SnapFacingOnChaseStart`
* `ChaseRotationLerpFactor` (0..1, controla “inércia”)

---

# **4. Estratégias (Strategies)**

Todas as estratégias são SOs independentes e plugáveis.

---

## **4.1 Entry Strategies (MinionEntryStrategySo)**

Define como o minion sai do **centro do planeta** e chega à órbita.

### StraightEntryStrategySo

* Linha reta
* Easing configurável
* Escala animada

### ArcEntryStrategySo

* Movimento em curva
* Calcula midpoint + offset lateral
* Também anima escala

---

## **4.2 Chase Strategies (MinionChaseStrategySo)**

Define como o minion alcança o alvo.

### DirectChaseStrategySo

* Move diretamente até o alvo
* Duração = dist/speed
* Easing configurável

### ZigZagChaseStrategySo

* Movimento blendado com DOBlendableMoveBy
* Parâmetros:

    * Amplitude
    * ZigZagCount
    * LateralBlendFactor

---

## **4.3 Spawn Patterns**

### RadialEvenSpawnPatternSO

* Distribui minions uniformemente num círculo
* Muito usado para defesas orbitais
* Opcional: heightOffset

---

# **5. Minion Runtime Handlers**

## **5.1 DefenseMinionController**

Recebe o profile e distribui:

* EntryDuration
* InitialScaleFactor
* OrbitIdleSeconds
* ChaseSpeed
* EntryStrategy
* ChaseStrategy
* **SnapFacingOnChaseStart**
* **ChaseRotationLerpFactor**

---

## **5.2 MinionEntryHandler**

* Executa sequências DOTween de entrada
* Aguarda `OrbitIdleDelaySeconds`
* Aciona a mudança de estado para chasing

---

## **5.3 MinionOrbitWaitHandler**

* Temporiza idle antes da perseguição
* Usa DOTween.Sequence com callback

---

## **5.4 MinionChaseHandler (V2.5)**

Agora totalmente configurável pelo perfil, sem SerializeFields internos.

* `_snapFacingOnChaseStart` → se olha direto para o alvo no início
* `_rotationLerpFactor` → controla inércia do giro
* Atualiza facing a cada frame do chase:

```
transform.forward = Lerp(current, direction, factor)
```

* Lógica robusta para:

    * Alvo movendo
    * Reset de tween
    * Fuga/imprecisão

---

# **6. PlanetDefense Pipeline**

## **6.1 Orquestração**

`PlanetDefenseOrchestrationService`:

1. Identifica ameaça (role).
2. Busca definição no `DefenseEntryConfigSO`.
3. Resolve `RoleDefenseConfig`.
4. Monta `PlanetDefenseSetupContext` contendo:

    * WavePreset
    * MinionBehaviorProfile
    * SpawnOffset
    * SpawnRadius

Nenhum uso de pool ou minion config legado.

---

## **6.2 Wave Runner (RealPlanetDefenseWaveRunner)**

Responsável por:

* Determinar pool → `WavePreset.PoolData`
* Aplicar spawn pattern → `WavePreset.SpawnPattern`
* Criar minions via PoolSystem
* Configurar Entry + Chase via ApplyProfile
* Aplicar estratégias
* Lançar eventos do PlanetDefenseEventService

`ApplyBehaviorProfile` removeu completamente qualquer dependência do sistema V1.

---

# **7. Pool System**

Integração via `PoolData`:

* `WavePreset.PoolData` define qual pool o planeta usa
* Cada minion no pool é `DefenseMinionPoolable`
* Profile aplicado **após** o retorno do pool
* Cada instância é configurada dinamicamente pelo `DefenseMinionController`

Nenhuma configuração de comportamento é feita no pool.

---

# **8. Recomendações de Uso**

### Para criar um novo tipo de minion:

1. Crie um **Behavior Profile**
2. Escolha Entry Strategy
3. Escolha Chase Strategy
4. Configure rotação conforme desejado (inércia vs instantâneo)
5. Associe à `WavePreset` + Pool
6. Adicione ao `DefenseEntryConfigSO` do planeta

---

# **9. Troubleshooting**

### Minion não está virando para o alvo

* Ajuste `ChaseRotationLerpFactor`
* Confira `SnapFacingOnChaseStart`

### Minion gira instantaneamente (não tem inércia)

* Use valores entre `0.05` e `0.25` para LerpFactor

### Minion teleporta durante Entry

* Verifique `EntryDurationSeconds`
* Proteções impedem duration zero

### Minion não sai do planeta

* Cheque WavePreset.PoolData
* Cheque Pattern/Sphere radius

---

# **10. Glossário**

| Termo            | Significado                                         |
| ---------------- | --------------------------------------------------- |
| Entry Strategy   | Movimento de saída do planeta até órbita            |
| Chase Strategy   | Movimento usado durante perseguição                 |
| Behavior Profile | Pacote completo de comportamento do minion          |
| WavePreset       | Define cadência/quantidade de minions               |
| Spawn Pattern    | Distribuição espacial dos minions                   |
| PoolData         | Configuração do objeto dentro do sistema de pooling |
| Role             | Tipo de ameaça detectada (Eater, Player etc.)       |

---

# **11. Changelog dos Refactors Importantes**

### V2

* Entrada, órbita e perseguição padronizadas
* Behavior Profile substitui MinionConfig (legado removido)

### V2.5

* Rotação configurável via Behavior Profile
* SnapFacingOnChaseStart
* ChaseRotationLerpFactor
* Entry/Chase strategies unificadas
* Nomenclatura padronizada
* Remoção total do legacy

---

# **12. Conclusão**

O sistema agora está:

* Modular
* Totalmente configurável via ScriptableObjects
* Sem dependência do legado
* Com comportamento físico mais natural (inércia, facing, entrada suave)
* Extensível sem alterar código
* Preparado para designers e programadores

Se quiser, posso gerar também:

* **Versão compacta (B)** para designers
* **UML oficial (D)** para documentação arquitetural
* **Diagrama de sequência do spawn/chase**
* **Exemplos de profiles (“glass cannon”, “heavy bruiser”, “tracker drone”)**

Basta pedir.
