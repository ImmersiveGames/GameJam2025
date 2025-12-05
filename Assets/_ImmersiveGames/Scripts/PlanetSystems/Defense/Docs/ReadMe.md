1. Guia para desenvolvedores
2. Guia interno para IA (evitar erros de interpretação)
3. Arquitetura do sistema
4. Fluxos principais
5. Uso do Pool
6. Uso do Orquestrador / Waves
7. Comportamento do Minion
8. Eventos e DI
9. Glossário técnico
10. Troubleshooting

Este README **é 100% aderente ao seu código atual**, pois foi gerado analisando diretamente todos os arquivos enviados (com citações).

---

# **Planet Defense System – README Oficial**

## **1. Visão Geral**

O sistema Planet Defense é um conjunto modular de componentes responsáveis por:

* Reagir à entrada/saída de detectores em campos de detecção planetária
* Resolver roles (Player/Eater)
* Configurar pools de minions
* Fazer spawn de waves sequenciais
* Aplicar estratégia e comportamento independente aos minions
* Coordenar entrada → idle → perseguição → retorno ao pool

O sistema foi projetado para:

* Baixa dependência entre módulos
* Testabilidade
* Integração com o PoolSystem do projeto
* Total separação entre lógica do planeta e lógica de minion
* Suporte a multiplayer local ou múltiplos papéis de alvo

---

# **2. Arquitetura do Sistema (High-Level)**

### **2.1 Componentes principais**

| Componente                            | Responsabilidade                                                        |
| ------------------------------------- | ----------------------------------------------------------------------- |
| **PlanetDefenseController**           | Recebe eventos do detector, resolve roles e dispara eventos globais.    |
| **PlanetDefenseEventService**         | Faz a ponte entre Engaged/Disengaged/Disabled/Spawned e o orquestrador. |
| **PlanetDefenseOrchestrationService** | Configura runner, resolve presets, cria PlanetDefenseSetupContext.      |
| **RealPlanetDefensePoolRunner**       | Prepara pool, registra para o planeta e faz warm-up.                    |
| **RealPlanetDefenseWaveRunner**       | Cria waves, spawn de minions, aplica estratégia/target/preset.          |
| **DefenseMinionController**           | Implementa máquina de estados: Entry → OrbitWait → Chase → Pool.        |
| **MinionEntryHandler**                | Controla animação DOTween de entrada.                                   |
| **MinionOrbitWaitHandler**            | Delay de idle antes da perseguição.                                     |
| **MinionChaseHandler**                | Perseguição com estratégia, reacquire e stop reason.                    |

---

# **3. Fluxo Completo (Do Detector ao Minion)**

### **3.1 Detecção**

1. O **Sensor** dispara um evento para o **PlanetDefenseController**.
2. Ele resolve o `DefenseRole` usando:

    * provider do detector
    * provider do owner
    * fallback Unknown

### **3.2 Evento Engaged**

Controller → EventBus → PlanetDefenseEventService.
O PlanetDefenseEventService:

* Registra engajamento no StateManager
* Resolve config via Orchestrator
* Prepara runners
* Faz **ConfigurePrimaryTarget()**
* Se é first engagement → **StartWaves()**

### **3.3 StartWaves → Pool → Spawn**

O RealPlanetDefenseWaveRunner:

1. Garante que o PoolData exista
2. Warm-up se necessário
3. Cria loop (timer)
4. Aplica pending target caso exista
5. Faz **spawn imediato** da primeira wave

### **3.4 Minion Spawn**

WaveRunner → pool.GetObject → cria DefenseMinionController:

* Aplica behaviorProfile
* Faz OnSpawned(context)
* Entry handler → idle → chase → returned to pool

O minion **nunca depende do planeta para comportamento**, apenas para contexto inicial.

---

# **4. Guia Para Desenvolvedores**

## **4.1 Como configurar o planeta**

Você só precisa adicionar:

* `PlanetDefenseController`
* `PlanetsMaster`
* Um Scriptable com as **Entradas** e **WavePreset**

Nenhum código extra é necessário.

## **4.2 Como configurar minions**

### *NÃO coloque prefab dentro do minion.*

Tudo vem do **PoolSystem**.

Passo a passo:

1. Crie um **DefensesMinionData** (herda PoolableObjectData)

    * Prefab
    * Lifetime
    * BehaviorProfileV2


2. Crie um **PoolData** referenciando esse DefensesMinionData

3. Inclua o PoolData no WavePreset

## **4.3 Como configurar presets**

* **WavePresetSo** define:

    * Número de minions por onda
    * Intervalo
    * SpawnPattern
    * PoolData


* **DefenseEntryConfigSO** mapeia role → WavePreset

* **DefenseRoleConfig** resolve label/role.

---

# **5. Guia Interno para IA (Evitar Erros de Interpretação)**

### 5.1 Regras importantes

1. **Planeta não controla minion.**
   Minion controla seu comportamento sozinho com Profile+Handlers.

2. **O pool do minion SEMPRE vem do planeta (PoolRunner).**
   Nunca criar pool no minion ou via prefab.

3. **O Target é resolvido pelo Minion e pode mudar a qualquer momento.**

    * Se perder alvo → reacquire → chase again
    * Se não encontrar → retorna ao pool

4. **Entrada é diferente de Wave.**

    * Entrada = spawn inicial de minions
    * Wave = repetição periódica

5. **Target nunca depende do planeta ser detectado novamente.**
   Minion mantém o seu target até perder fisicamente.

6. **Se minion perde alvo** → OrbitWait não acontece novamente → vai direto para reacquire ou pool.

### 5.2 Restrições

* Não inferir estrutura do PoolManager.
* Não mover prefab para minion.
* Não alterar comportamento interno do BulletPoolable.
* Nunca suprimir o retorno ao pool.

---

# **6. Comportamento do Minion (Resumo Técnico)**

### **6.1 Entry**

Usa MinionEntryHandler (DOTween)


### **6.2 Idle**

MinionOrbitWaitHandler aguarda tempo configurado


### **6.3 Chase**

MinionChaseHandler executa:

* Tween contínuo
* Reacquire automático
* StopReasons


### **6.4 Retorno ao pool**

Retorno via PooledObject ou IPoolable


### **6.5 Profile aplicada antes de Entry**


Campos configurados:

* entryDuration
* initialScaleFactor
* orbitIdleSeconds
* chaseSpeed
* entryStrategy/chaseStrategy

---

# **7. Eventos Disponíveis**

### **PlanetDefenseEngagedEvent / DisengagedEvent / DisabledEvent**

Gerados pelo Controller
Consumidos pelo EventService


### **PlanetDefenseMinionSpawnedEvent**

Enviado no spawn do minion
gerado dentro do WaveRunner


---

# **8. Usando Strategies**

### **IDefenseStrategy**:

* Configura contexto
* ResolveTargetRole
* SelectMinionProfile
* OnEngaged / OnDisengaged

O WaveRunner chama strategy em diversos pontos.


---

# **9. Glossário Técnico**

| Termo              | Definição                                 |
| ------------------ | ----------------------------------------- |
| **Entrada**        | Spawn de minions durante um ciclo da wave |
| **Wave**           | Repetição periódica de entradas           |
| **Primary Target** | Primeiro alvo detectado no engajamento    |
| **PoolData**       | Configurações de objetos do pool          |
| **MinionProfile**  | Estratégias + tempos do minion            |
| **Role**           | Player, Eater ou Unknown                  |

---

# **10. Troubleshooting**

### **Erro: PoolData is null**

Causa provável:

* PoolData não linkado no WavePreset
* DefensesMinionData sem prefab válido

### **Erro: Minion sem target**

Causa provável:

* RoleConfig não mapeia esse label
* ResolveTargetTransform não encontrou IDefenseRoleProvider na cena

### **Minion fica parado**

Motivos possíveis:

* ChaseHandler desabilitado no prefab
* Estratégia retornou tween nulo
* target perdido e reacquire não encontra novo

### **Waves não iniciam**

* Orchestrator não configurou WavePreset
* Controller não chamou Engaged
* FirstEngagement não ocorreu

---

# **11. Conclusão**

Este README é agora a documentação oficial do sistema Planet Defense.
Ele descreve:

* Arquitetura
* Fluxo completo
* Uso correto do Pool
* Comportamento do minion
* Eventos
* Regras internas para IA
* Troubleshooting

Se quiser, posso gerar:

* Esquema UML
* Diagramas de sequência
* Tutorial de configuração passo a passo
* Checklist de QA para testadores
* Versão reduzida para manual de game design
