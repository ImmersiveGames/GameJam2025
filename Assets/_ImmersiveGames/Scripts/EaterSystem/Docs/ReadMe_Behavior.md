Perfeito. Abaixo está a **versão completamente revisada e reescrita do `ReadMe.md`**, já alinhada com:

* a refatoração em `partial classes`,
* a arquitetura atual do sistema,
* princípios SOLID,
* uso correto por **programadores** e **designers**,
* e sem pressupor nada fora do que existe hoje no código.

Você pode **substituir integralmente** o `ReadMe.md` atual por este conteúdo.

---

# 🦠 Eater System

Sistema de IA modular responsável pelo comportamento da entidade **Eater** no jogo.

O Eater é uma criatura autônoma que:

* vaga pelo espaço próximo aos jogadores,
* desenvolve **desejos dinâmicos por recursos**,
* persegue e consome planetas compatíveis,
* causa dano periódico,
* se cura durante a alimentação,
* reage a morte e revive via eventos,
* e integra-se a sistemas globais (recursos, áudio, animação, HUD).

Este sistema foi projetado para **Unity 6**, multiplayer local, priorizando **modularidade, legibilidade e manutenção**.

---

## 📐 Visão Geral de Arquitetura

O sistema é centrado na classe:

```
EaterBehavior (MonoBehaviour, partial)
```

Ela atua como **orquestradora**, delegando responsabilidades para:

* uma máquina de estados (State Machine),
* um serviço de desejos,
* controladores auxiliares (detecção, animação, áudio),
* sistemas de recursos e AutoFlow.

A classe é dividida em **arquivos parciais**, cada um com uma responsabilidade clara.

---

## 🧩 Organização em Partials

O `EaterBehavior` é uma **classe parcial**, dividida nos seguintes arquivos:

### `EaterBehavior.cs`

* Stub obrigatório para o Unity.
* Contém apenas a declaração da classe.

---

### `EaterBehavior.Core.cs`

**Responsabilidade:** núcleo e ciclo de vida.

Contém:

* `Awake`, `Update`, `OnValidate`, `OnDestroy`
* Inicialização de:

  * `EaterMaster`
  * `EaterConfigSo`
  * serviços e sistemas
* Evento público:

  * `EventDesireChanged`
* Propriedades centrais:

  * `Master`
  * `Config`
  * `CurrentTargetPlanet`
* Atualização da StateMachine e do DesireService.

👉 **Não contém lógica de jogo direta**.

---

### `EaterBehavior.StateMachine.cs`

**Responsabilidade:** comportamento e transições.

Contém:

* Instância da `StateMachine`
* Estados:

  * `EaterWanderingState`
  * `EaterHungryState`
  * `EaterChasingState`
  * `EaterEatingState`
  * `EaterDeathState`
* Predicados:

  * tempo de wandering
  * planeta desmarcado
  * fome
  * morte / revive
* Configuração de transições
* Métodos de debug via `ContextMenu`
* Classe utilitária `FalsePredicate`

👉 Toda a lógica de **decisão comportamental** vive aqui.

---

### `EaterBehavior.DesiresAndWorldHelpers.cs`

**Responsabilidade:** desejos e mundo.

Contém:

* Integração com `EaterDesireService`
* Estado atual do desejo (`EaterDesireInfo`)
* Métodos:

  * `BeginDesires`
  * `EndDesires`
  * `SuspendDesires`
* Disparo de eventos:

  * `EventDesireChanged`
  * `EaterDesireInfoChangedEvent` (EventBus)
* Helpers de mundo:

  * busca do jogador mais próximo
  * limites de distância
  * roaming
  * órbita de planetas
* Helpers de movimento:

  * `Move`
  * `Translate`
  * `RotateTowards`
  * `LookAt`

👉 Este arquivo conecta **IA ↔ mundo ↔ HUD**.

---

### `EaterBehavior.ResourcesAndAutoFlow.cs`

**Responsabilidade:** recursos, dano e cura.

Contém:

* Integração com:

  * `ResourceAutoFlowBridge`
  * `ResourceSystem`
  * `IDamageReceiver`
* Métodos:

  * `TryApplySelfHealing`
  * `TryRestoreResource`
  * `ResumeAutoFlow`
  * `PauseAutoFlow`
* Logs defensivos para falhas de integração

👉 Toda a lógica de **vida, cura e recursos** do Eater fica aqui.

---

### `EaterBehavior.DetectionAndControllers.cs`

**Responsabilidade:** controladores auxiliares.

Contém:

* Resolução e cache de:

  * `EaterDetectionController`
  * `EaterAnimationController`
  * `EntityAudioEmitter`
* Integração com `DependencyManager`
* Fallback seguro via `GetComponent`

👉 Evita acoplamento direto do Core com sistemas externos.

---

## 🔄 Máquina de Estados (State Machine)

### Estados

| Estado    | Descrição                               |
| --------- | --------------------------------------- |
| Wandering | Movimento livre próximo aos jogadores   |
| Hungry    | Busca ativa por planetas compatíveis    |
| Chasing   | Perseguição direta a um planeta marcado |
| Eating    | Órbita + dano periódico + cura          |
| Death     | Estado inativo após morte               |

### Transições

* Wandering → Hungry (timeout)
* Hungry → Chasing (planeta disponível)
* Chasing → Eating (distância mínima)
* Eating → Hungry (planeta inválido)
* Eating → Wandering (alimentação encerrada)
* Any → Death (evento)
* Death → Wandering (revive)

Todas as transições são **dirigidas por predicados**, não por lógica espalhada.

---

## 🍽️ Sistema de Desejos

O sistema de desejos é controlado por:

```
EaterDesireService
```

Ele:

* seleciona desejos baseados em:

  * disponibilidade de planetas
  * histórico recente
  * pesos configuráveis
* dispara eventos ao mudar o desejo
* controla duração, suspensão e retomada

### Configuração

Toda a configuração vem de:

```
EaterConfigSo
```

Inclui:

* duração base do desejo
* multiplicadores para desejos indisponíveis
* pesos relativos
* sons de seleção
* limites de repetição

O `EaterBehavior` **não decide desejos**, apenas reage a eles.

---

## ❤️ Recursos, Dano e Cura

Durante a alimentação:

* o planeta recebe dano periódico
* o Eater se cura via `IDamageReceiver`
* recursos podem ser restaurados manualmente ou via AutoFlow

O sistema:

* tolera falhas de integração
* registra logs claros
* nunca quebra o fluxo principal do comportamento

---

## 🧠 Boas Práticas

### ✅ Onde colocar código novo

* Novo estado → `EaterBehavior.StateMachine.cs`
* Nova regra de desejo → `EaterDesireService`
* Nova configuração → `EaterConfigSo`
* Nova interação com mundo → `DesiresAndWorldHelpers`

### ❌ O que evitar

* Lógica de decisão no `Core`
* Acesso direto a sistemas globais fora dos partials
* Estados com dependência entre si
* Leitura direta de `Config` fora do Behavior

---

## 🧪 Debug e Testes

* Use os `ContextMenu` do `EaterBehavior` para forçar estados
* Ative `logStateTransitions` para logs verbosos
* Teste:

  * ciclo completo de estados
  * troca de desejos
  * ausência de planetas
  * morte e revive
  * HUD de desejos

---

## 📌 Considerações Finais

O **Eater System** foi estruturado para:

* ser extensível sem refatorações grandes,
* evitar classes monolíticas,
* separar decisão, ação e integração,
* facilitar manutenção e leitura futura.

Ele é um **sistema fechado**, mas preparado para crescimento controlado.

---

Se quiser, no próximo passo posso:

* gerar uma versão **resumida** do ReadMe para designers,
* ou criar um **ADR (Architecture Decision Record)** explicando as decisões dessa refatoração.
