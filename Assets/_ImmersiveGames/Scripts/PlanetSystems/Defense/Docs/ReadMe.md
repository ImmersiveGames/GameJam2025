# 🛡️ Planet Defense System — Documentação Oficial (v1.0)

## 📚 Índice

1. [Visão Geral](#visão-geral)
2. [Arquitetura e Fluxo](#arquitetura-e-fluxo)
3. [Componentes Principais](#componentes-principais)
4. [Sistema de Roles (Duplo Check + Overrides)](#sistema-de-roles-duplo-check--overrides)
5. [Integração com PoolSystem](#integração-com-poolsystem)
6. [Timers com IntervalTimer](#timers-com-intervaltimer)
7. [Eventos e Telemetria](#eventos-e-telemetria)
8. [Configuração via Inspector](#configuração-via-inspector)
9. [Extensibilidade e Estratégias](#extensibilidade-e-estratégias)
10. [Debug e Troubleshooting](#debug-e-troubleshooting)

---

## 🎯 Visão Geral

O **Planet Defense System** protege planetas no multiplayer local com alta previsibilidade e baixo custo, separando claramente o que é detecção, orquestração e execução de spawns. A pilha segue SOLID, usa **Observer** para eventos, **Strategy** para comportamento de minions e **Dependency Injection** para runners configuráveis.

---

## 🧭 Arquitetura e Fluxo

```
Detections (Player/Eater) → PlanetDefenseController (Resolve DefenseRole)
        ↓                                      ↓
    EventBus (Enter/Exit)            PlanetDefenseDetectable
                                            ↓
                                 PlanetDefenseSpawnService
                          ├─ DefenseStateManager (estado)
                          ├─ DefenseDebugLogger (telemetria)
                          ├─ IPlanetDefensePoolRunner (pool)
                          └─ IPlanetDefenseWaveRunner (waves)
                                            ↓
                               PoolManager / IntervalTimer
                                            ↓
                                     Minions (IPoolable)
```

1. Detectores publicam `PlanetDefenseEngagedEvent`/`Disengaged` no EventBus.
2. `PlanetDefenseController` resolve o `DefenseRole` e repassa ao `PlanetDefenseDetectable`.
3. `PlanetDefenseSpawnService` coordena runners, estado e debug com DI.
4. `RealPlanetDefensePoolRunner` registra/aquece pools de minions; `RealPlanetDefenseWaveRunner` dispara waves com `IntervalTimer` sem `Update` ou `Coroutine` globais.

---

## 🧩 Componentes Principais

### `PlanetDefenseController`
- Recebe eventos de detecção e resolve o `DefenseRole` em ordem de prioridade (Detector → Owner → Config).
- Publica eventos de engajar/desengajar/disable para o serviço de spawn.
- Logs verbosos opcionais indicam a fonte da resolução.

### `PlanetDefenseDetectable`
- Interface entre o controlador e o serviço de spawn.
- Mantém compatibilidade com detectores legados, mas prioriza providers explícitos.

### `PlanetDefenseSpawnService`
- Orquestra runners e estado via DI.
- Liga/desliga timers por planeta, garante `WarmUp`, `StartWaves`, `StopWaves` e `Release` conforme engajamento.

### `DefenseStateManager`
- Guarda dicionários de contagem de detectores, timers e contexto por planeta.
- Evita reprocessamento e facilita diagnósticos.

### `DefenseDebugLogger`
- Usa `IntervalTimer` dedicado por planeta para logs periódicos (verboses) sem `Update`.
- Pode ser desligado em produção mantendo código de telemetria isolado.

### Runners (Pool/Wave)
- **RealPlanetDefensePoolRunner:** registra pools reais no `PoolManager` usando `PoolData` pré-configurado via Editor (sem criar `PoolData` em runtime), mantendo validação via `PoolData.Validate`.
- **RealPlanetDefenseWaveRunner:** coordena `IntervalTimer` por planeta para spawn periódico, configurando minions via `PlanetDefenseSetupContext` + `IDefenseStrategy` e consumindo `ObjectPool.GetObject` conforme exemplos do PoolSystem.

---

## 🛡️ Sistema de Roles (Duplo Check + Overrides)

O `DefenseRole` é definido em duas camadas complementares:
1. **Principal:** `IDefenseRoleProvider` no `ActorMaster` (prefab/GameObject) — fonte confiável e primária.
2. **Fallback/Override:** `DefenseRoleConfig` (opcional) — permite mapear `identifier → role` no Editor para forçar/complementar roles.

Isso habilita combinações ou forçar papéis especiais (Player possuído, boss com fase defensiva, NPC neutro). O config só é consultado quando não há provider ou quando se deseja sobrescrever um papel específico.

| Cenário | Provider (ActorMaster) | DefenseRoleConfig Override | Resultado Final | Observações |
| --- | --- | --- | --- | --- |
| Player 1 padrão | Player | (sem binding) | Player | Usa apenas provider. |
| Player possuído pelo Eater | Player | identifier `PlayerPossuido` → `Eater` | Eater | Override força comportamento agressivo temporário. |
| Boss com fase defensiva | Eater | identifier `BossFase2` → `Player` | Player | Troca para postura defensiva na fase 2. |
| NPC neutro | Unknown | identifier `NPCNeutro` → `Neutral` | Neutral | Sem provider, config define papel neutro. |
| Detector legado sem provider | Unknown | identifier `DetectorX` → `Player` | Player | Config atua como fallback quando não há provider. |

> Nota: “Com o sistema atual (ActorMaster com selector), o `DefenseRoleConfig` é uma ferramenta poderosa de balanceamento e exceções, não uma dependência obrigatória.”

---

## 🪣 Integração com PoolSystem

- Usa `PoolManager`, `PoolData` e `IPoolable` para evitar instâncias extras.
- Cada planeta referencia um `PoolData` configurado no Editor (nome, tamanho inicial, expansão e lista de `PoolableObjectData`); o runner não cria `PoolData` em runtime.
- `RealPlanetDefensePoolRunner.WarmUp` valida o `PoolData` e chama `PoolManager.Instance.RegisterPool(poolData)` seguindo o fluxo descrito no guia do PoolSystem.
- Spawn ocorre via `ObjectPool.GetObject(position, spawner)` dentro do tick do `IntervalTimer` no runner de waves, permitindo rastrear o `IActor` que disparou o spawn.

---

## ⏱️ Timers com IntervalTimer

- `IntervalTimer` substitui `Update` e `Coroutine` para waves e debug, trabalhando com cadência em segundos.
- Cada planeta possui um timer dedicado; `OnInterval` dispara spawns ou logs conforme o runner responsável.
- Timers são iniciados em `OnDefenseEngaged` (primeiro detector), pausados/limpos em `OnDefenseDisengaged` (último detector) ou `OnDefenseDisabled`.
- Intervalo é configurado em segundos diretamente no construtor, sem conversões intermediárias.

---

## 📢 Eventos e Telemetria

| Evento | Quando ocorre | Consumidores típicos |
| --- | --- | --- |
| `PlanetDefenseEngagedEvent` | Primeiro detector ativo no planeta | SpawnService inicia pools/timers. |
| `PlanetDefenseDisengagedEvent` | Último detector saiu do planeta | SpawnService para timers/waves. |
| `PlanetDefenseDisabledEvent` | Planeta desativado (morte, reset) | Libera pools e timers. |
| `PlanetDefenseWaveStartedEvent` | Wave iniciada pelo runner | HUD, FX, áudio. |
| `PlanetDefenseMinionSpawnedEvent` | Minion foi spawnado pelo pool | Contadores, telemetria de performance. |

> Logs verbosos podem ser habilitados no `DebugUtility.DebugLevel.Verbose` para rastrear fonte de role e cadence de waves.

---

## 🛠️ Configuração via Inspector

1. **ActorMaster (prefabs do Player/Eater/Boss):** defina `DefenseRole` primário.
2. **DefenseRoleConfig (opcional):** crie o asset via `Create → Defense → DefenseRoleConfig`, configure `Fallback Role` e `Role Mappings`.
3. **PoolData (Defense):** crie o asset `PoolData` no Editor com os `PoolableObjectData` (ex.: `DefensesMinionData`) e configure `ObjectName`, tamanho inicial e expansão.
4. **DefensesMinionData:** associe prefabs de minions, quantidades e intervalos de wave (referenciados pelo `PoolData`).
5. **PlanetDefenseSpawnService:** injete runners reais no bootstrap (já configurado) e referencie o `PoolData` default + `DefenseRoleConfig` se desejar overrides.
6. **Planetas na cena:** adicionem `PlanetDefenseDetectable` + `PlanetDefenseController` e conectem ao EventBus padrão.

---

## 🧠 Extensibilidade e Estratégias

- **Strategy Pattern:** implemente novas `IDefenseStrategy` para variar comportamento dos minions (agressivo, defensivo, suporte) e injete via `PlanetDefenseSetupContext`.
- **Novos tipos de minion:** adicione entradas em `DefensesMinionData` e novos prefabs `IPoolable`.
- **Eventos customizados:** observe `PlanetDefenseMinionSpawnedEvent` para métricas ou sistemas de progressão.
- **Balanceamento rápido:** use `DefenseRoleConfig` para forçar roles temporários sem duplicar prefabs.

---

## 🩺 Debug e Troubleshooting

- **Role Unknown?** Verifique se há `IDefenseRoleProvider` no `ActorMaster` ou binding correspondente no `DefenseRoleConfig`.
- **Waves não iniciam?** Confirme se o evento `PlanetDefenseEngagedEvent` está sendo publicado e se o timer de waves está ativo (logs verboses ajudam).
- **Pools não criam instâncias?** Cheque `DefensesMinionData` (prefab válido, tamanho inicial) e se o `PoolManager` está registrado no bootstrapper.
- **Flicker de timers?** Certifique-se de que detectores múltiplos no mesmo planeta incrementam/decrementam corretamente a contagem no `DefenseStateManager`.

> Dica: habilite Verbose no `DebugUtility` apenas durante testes; os timers de debug são isolados e fáceis de desativar em produção.
