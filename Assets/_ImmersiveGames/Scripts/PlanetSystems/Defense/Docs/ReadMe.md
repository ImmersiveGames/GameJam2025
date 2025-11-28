# Planet Defense Runner – Sistema de Defesa Planetária
**Multiplayer local em Unity 2022.3+ (Unity 6)**  
Projeto desenvolvido com foco em arquitetura limpa, SOLID, injeção de dependências e alta testabilidade.

---

## Sumário
- [Visão Geral](#visão-geral)
- [Estrutura de Pastas](#estrutura-de-pastas)
- [Sistema de Defesa Planetária – v2.1 (Configuração Centralizada & Zero Legado)](#sistema-de-defesa-planetária--v21-configuração-centralizada--zero-legado) ← **ATUALIZADO**
- [Injeção de Dependências](#injeção-de-dependências)
- [EventBus](#eventbus)
- [Pooling](#pooling)
- [Timers (ImprovedTimers)](#timers-improvedtimers)
- [Debug & Logging](#debug--logging)
- [Como Configurar no Inspector](#como-configurar-no-inspector)
- [Boas Práticas & Convenções](#boas-práticas--convenções)

---

## Sistema de Defesa Planetária – v2.1 (Configuração Centralizada & Zero Legado)

### Status Atual: LIMPEZA FINAL CONCLUÍDA

**Versão 2.1** (atual) é o estado **definitivo** do sistema de defesa planetária.

**Zero dependência** de `FrequencyTimer`, `IntervalTimer` ou qualquer timer legado  
**Zero valores hard-coded** de intervalo ou quantidade de minions  
**DefenseWaveProfileSO é a ÚNICA fonte** de configuração de ondas  
Todo o sistema usa **CountdownTimer** (ImprovedTimers) → precisão em segundos, sem corrotinas, fácil controle

### Componentes Principais
| Componente                        | Responsabilidade                                                                                   |
|-----------------------------------|----------------------------------------------------------------------------------------------------|
| `PlanetDefenseController`        | MonoBehaviour que escuta sensores → publica eventos (`Engaged`, `Disengaged`, `Disabled`).        |
| `PlanetDefenseEventHandler`       | Escuta os eventos do EventBus e delega ao `PlanetDefenseSpawnService` (mantém o serviço puro).   |
| `PlanetDefenseSpawnService`       | Orquestrador central – decide quando aquecer pools, iniciar/parar waves e logar defesa.          |
| `RealPlanetDefensePoolRunner`     | Registra e aquece a pool de minions usando o `PoolManager` real (uma única vez por planeta).      |
| `RealPlanetDefenseWaveRunner`     | Gerencia o loop de waves com `CountdownTimer`. Spawna **wave imediata + ondas periódicas**.      |
| `DefenseDebugLogger`              | Log periódico (Verbose) da defesa ativa usando também `CountdownTimer`.                          |
| `DefenseWaveProfileSO`            | **ÚNICA fonte** de configuração de ondas (intervalo, quantidade, raio, altura).                  |
| `PoolData` (PoolableObjectData)   | **ÚNICA fonte** de configuração da pool de minions defensores.                                    |

### Configuração 100% Centralizada

| Parâmetro                   | Fonte Única                                     | Valor Fallback (emergência) |
|-----------------------------|--------------------------------------------------|------------------------------|
| Segundos entre waves        | `DefenseWaveProfileSO.secondsBetweenWaves`      | `1` (Mathf.Max(1, ...))     |
| Minions por wave            | `DefenseWaveProfileSO.enemiesPerWave`           | `1` (Mathf.Max(1, ...))     |
| Raio de spawn               | `DefenseWaveProfileSO.spawnRadius`              | `0`                         |
| Altura de spawn             | `DefenseWaveProfileSO.spawnHeightOffset`        | `0`                         |

> **Se o DefenseWaveProfileSO estiver ausente**, o sistema **não usa mais 5/6** como fallback.  
> Agora ele usa **1 segundo** e **1 minion por onda** como medida de segurança, com **log de warning claro**.

### Fluxo em Runtime

1. Detector entra → `PlanetDefenseController.EngageDefense()`
2. Publica `PlanetDefenseEngagedEvent` (com `IsFirstEngagement`)
3. `PlanetDefenseSpawnService` → aquece pool + inicia waves (se for o primeiro detector)
4. `RealPlanetDefenseWaveRunner`
   - Spawna **uma wave imediata**
   - Inicia `CountdownTimer` com `secondsBetweenWaves` do SO
   - A cada tick → nova wave
5. Último detector sai → `PlanetDefenseDisengagedEvent` (com `IsLastDisengagement`)
   - `StopWaves()` → timer parado e removido
   - Logging finalizado
6. Planeta desativado → `PlanetDefenseDisabledEvent` → pools limpas (opcional)

### Timers (ImprovedTimers)
Todo o controle de cadência usa **CountdownTimer**:

- Cadência exata em segundos
- Sem corrotinas → zero garbage
- Fácil pausa/parada sem leaks

### Mudanças v2.0 → v2.1 (Limpeza Final)

| Antes (v2.0)                          | Agora (v2.1) — CORRIGIDO                     |
|---------------------------------------|-----------------------------------------------|
| Fallbacks com 5s / 6 minions          | Fallbacks reduzidos a 1s / 1 minion (emergência) |
| Algumas partes ainda liam valores fixos | 100% leitura do `DefenseWaveProfileSO`        |
| Possível confusão sobre fonte da verdade | Documentado: **SO é a única fonte**           |
| Referências a FrequencyTimer no código | Totalmente removidas                          |

**Conclusão:**  
O sistema está **100% limpo, previsível, configurável e livre de legados**.  
Basta configurar **um único** `DefenseWaveProfileSO` no Inspector do planeta — tudo flui automaticamente.

**Nunca mais mexa em valores mágicos no código. Nunca mais dependa de FrequencyTimer.**  
**DefenseWaveProfileSO = Verdade Absoluta.**

---

## Injeção de Dependências
Container leve `DependencyManager`:
- `IPlanetDefensePoolRunner` e `IPlanetDefenseWaveRunner` → singletons globais
- `PlanetDefenseSpawnService` → registrado por `ActorId` do planeta

---

## EventBus
Eventos (structs):
- `PlanetDefenseEngagedEvent`
- `PlanetDefenseDisengagedEvent`
- `PlanetDefenseDisabledEvent`
- `PlanetDefenseMinionSpawnedEvent` (telemetria)

---

## Pooling
- Cada planeta tem sua pool registrada no `PoolManager`
- Warm-up ocorre **uma única vez** por planeta
- `Release()` limpa tudo quando o planeta é desativado (opcional)

---

## Como Configurar no Inspector

1. Crie um `DefenseWaveProfileSO` → defina:
   - `secondsBetweenWaves`
   - `enemiesPerWave`
   - `spawnRadius`
   - `spawnHeightOffset`
2. Crie/atribua um `PoolData` do minion defensor
3. No prefab do planeta → `PlanetDefenseController`:
   - **Defense Wave Profile SO** → seu SO
   - **Default Defense Pool** → seu PoolData
4. (Opcional) Implemente estratégias customizadas com `IDefenseStrategy`

Pronto! O planeta defende-se automaticamente.

---

## Boas Práticas & Convenções

- Nunca deduza conteúdo de scripts – sempre peça contexto completo
- Arquivos completos (usings, namespace, classe) em toda alteração
- Comentários e logs em **português**, código em **inglês**
- Princípios SOLID, Clean Architecture e Design Patterns são obrigatórios
- Todo novo recurso de defesa deve seguir o fluxo da v2.1

Qualquer dúvida sobre o sistema de defesa → consulte esta seção **v2.1**.

**Happy defending!**