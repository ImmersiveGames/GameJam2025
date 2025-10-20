# 🧩 Damage System Integration Manual (`m.md`)

## 📘 Sumário

1. [Visão Geral](#visão-geral)
2. [Estrutura de Pastas](#estrutura-de-pastas)
3. [Arquivos Principais](#arquivos-principais)
4. [Integração com o Sistema de Áudio](#integração-com-o-sistema-de-áudio)
5. [Fluxo de Eventos](#fluxo-de-eventos)
6. [Guia de Debug e Teste](#guia-de-debug-e-teste)
7. [Modularização do Debugger](#modularização-do-debugger)
8. [Boas Práticas e Extensões Futuras](#boas-práticas-e-extensões-futuras)

---

## 📖 Visão Geral

O sistema de **Damage & Respawn** foi projetado para:

* Gerenciar **recebimento de dano**, **morte** e **ressurgimento (respawn)**.
* Integrar com **recursos de vida** (`ResourceSystem`).
* Emitir **eventos globais e locais** para fácil integração.
* Sincronizar **efeitos visuais e sonoros** de impacto, morte e reviver.
* Facilitar **debugs não invasivos** e testes diretos no Editor Unity.

---

## 🗂️ Estrutura de Pastas

```
_ImmersiveGames/Scripts/
│
├── DamageSystem/
│   ├── DamageSystemBase.cs
│   ├── DamageReceiver.cs
│   ├── DamageDealer.cs
│   ├── Handlers/
│   │   ├── DeathHandler.cs
│   │   ├── RespawnHandler.cs
│   │   ├── DamageValidator.cs
│   │   └── DestructionHandlers/
│   ├── Tests/
│   │   ├── DamageSystemDebugger.cs            ← [Controlador Principal]
│   │   ├── DamageSystemDebugger.Audio.cs      ← [Módulo de Áudio]
│   │   ├── DamageSystemDebugger.Receiver.cs   ← [Módulo de Recebedor de Dano]
│   │   ├── DamageSystemDebugger.Dealer.cs     ← [Módulo de Dealer]
│   │   └── DamageSystemDebugger.System.cs     ← [Módulo de Diagnóstico]
│
├── AudioSystem/
│   ├── Components/
│   │   └── EntityAudioEmitter.cs
│   ├── AudioConfig.cs
│   └── AudioCue.cs
```

---

## ⚙️ Arquivos Principais

### 🧱 `DamageSystemBase.cs`

Define a infraestrutura comum para sistemas de dano:

* Validação de camadas (`LayerMask`)
* Cache de `IDamageable`
* Controle de destruição (`IDestructionHandler`)
* Prevenção de múltiplos danos no mesmo frame

---

### 💀 `DamageReceiver.cs`

Gerencia:

* Aplicação de dano sobre recursos
* Eventos de **morte**, **reviver** e **respawn**
* Integração com o `EntityResourceBridge`
* Emissão de sons diretamente via `EntityAudioEmitter`
* Handlers internos:

    * `DeathHandler`
    * `RespawnHandler`
    * `DamageValidator`

---

### ⚔️ `DamageDealer.cs`

* Aplica dano direto ou em área.
* Emite eventos de:

    * `OnDamageDealt`
    * `OnDamageBlocked`
* Pode ser configurado para destruir o objeto após o dano.

---

## 🔊 Integração com o Sistema de Áudio

### Requisitos:

* Componente `EntityAudioEmitter` no mesmo GameObject do `DamageReceiver`.
* `AudioConfig` apontado no emissor para definir mixer/volume padrão.
* Campos do `DamageReceiver` preenchidos com os `SoundData` adequados:

    * `hitSound`
    * `deathSound`
    * `reviveSound`

### Execução Automática:

* Ao receber dano → toca **Hit Sound**
* Ao morrer → toca **Death Sound**
* Ao reviver → toca **Revive Sound**

### Configuração:

```csharp
var context = AudioContext.Default(transform.position, audioEmitter.UsesSpatialBlend);
audioEmitter.Play(hitSound, context);
```

---

## 🔁 Fluxo de Eventos

### 🧩 Eventos Locais (no DamageReceiver)

| Evento                | Assinatura                      | Dispara Quando  |
| --------------------- | ------------------------------- | --------------- |
| `EventDamageReceived` | `(float damage, IActor source)` | Dano é aplicado |
| `EventDeath`          | `(IActor actor)`                | Vida ≤ 0        |
| `EventRevive`         | `(IActor actor)`                | Entidade revive |

---

### 🌍 Eventos Globais (via EventBus)

| Evento                | Origem         | Utilidade                     |
| --------------------- | -------------- | ----------------------------- |
| `ResourceUpdateEvent` | ResourceSystem | Atualização de valores        |
| `ActorDeathEvent`     | DamageReceiver | Notificação global de morte   |
| `ActorReviveEvent`    | DamageReceiver | Notificação global de reviver |
| `DamageDealtEvent`    | DamageDealer   | Comunicação entre sistemas    |

---

## 🧪 Guia de Debug e Teste

### 1️⃣ Adicionar o `DamageSystemDebugger`

* No mesmo GameObject do `DamageReceiver` ou `DamageDealer`.
* Habilitar as opções de debug desejadas:

    * `logAllEvents`
    * `showVisualDebug`

### 2️⃣ Testes rápidos via **Context Menu**

No Inspector:

```
Right-click → Receiver → Test Receive Damage
Right-click → Receiver → Kill Object
Right-click → Receiver → Revive Object
Right-click → Dealer → Test Damage in Front
Right-click → Audio → Test Hit Sound
```

---

## 🧩 Modularização do Debugger

O `DamageSystemDebugger` foi **refatorado em módulos parciais** para melhor manutenção:

| Arquivo                            | Função                                             |
| ---------------------------------- | -------------------------------------------------- |
| `DamageSystemDebugger.cs`          | Núcleo do sistema, inicialização e logging básico. |
| `DamageSystemDebugger.Audio.cs`    | Controle e testes de áudio (Hit, Death, Revive).   |
| `DamageSystemDebugger.Receiver.cs` | Comandos e debug específicos do `DamageReceiver`.  |
| `DamageSystemDebugger.Dealer.cs`   | Testes e interações com o `DamageDealer`.          |
| `DamageSystemDebugger.System.cs`   | Diagnóstico e visualização de estado global.       |

---

## 🧠 Boas Práticas e Extensões Futuras

✅ **Boas Práticas:**

* Sempre usar `EventBus<T>` para comunicações desacopladas.
* Mantenha os `Handlers` como classes isoladas (SRP).
* Utilize `DebugLevelAttribute` para controlar verbosidade.
* Armazene sons e efeitos em `ScriptableObject` de `AudioConfig`.

🚀 **Futuras Extensões:**

* Adicionar `DamageOverTime` (DOT Handler)
* Suporte a `Armor / Resistência`
* Modo multiplayer com replicação de eventos
* Integração com `UIResourceBars`

---

## 📦 Conclusão

O sistema refatorado agora fornece:

* Uma arquitetura sólida para dano e respawn.
* Eventos claros e bem organizados.
* Debugger modular com menos spam e melhor visibilidade.
* Integração nativa com o `EntityAudioEmitter` compartilhado por cada serviço.

---

🧩 **Próximos Arquivos a Gerar:**

1. `DamageSystemDebugger.cs` (núcleo)
2. `DamageSystemDebugger.Audio.cs`
3. `DamageSystemDebugger.Receiver.cs`
4. `DamageSystemDebugger.Dealer.cs`
5. `DamageSystemDebugger.System.cs`

---

Deseja que eu **gere agora os 5 arquivos refatorados completos** do `DamageSystemDebugger` conforme o guia acima (com logging otimizado e menos spam)?
