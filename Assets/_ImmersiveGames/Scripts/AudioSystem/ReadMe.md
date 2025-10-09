# m.md — Guia de Integração do Audio System Consolidado

## 🎯 Objetivo
Centralizar a reprodução de áudio com coerência: cada entidade (player, NPC, arma, etc.)
possui seu `AudioControllerBase` e seu próprio pool local de `SoundEmitter`.
O `AudioManager` é responsável por BGM e controles globais (mixer), não por pools de SFX.

---

## 🧭 Arquivos/Componentes principais
- AudioManager.cs — serviço global (BGM, mixer)
- AudioControllerBase.cs — controlador base por entidade (dono do pool local)
- PlayerAudioController.cs — controller de exemplo (player)
- SoundEmitter.cs — emitter poolable (deriva de `PooledObject`)
- SoundBuilder.cs — builder fluente (usa pool local quando fornecido)
- AudioConfig.cs — config de entidade (sound defaults)
- SoundData.cs — dados do som (clip, volume, spatial, pitchVariation etc.)
- SoundEmitterPoolData.cs / SoundEmitterPoolableData.cs — configurações de pool

---

## 🔌 Como configurar no inspector (passo a passo)
1. Coloque o prefab `AudioManager` em `Resources/Audio/Prefabs/AudioManager.prefab` (opcional:
   `AudioSystemInitializer` cria a partir de Resources se necessário).
2. No prefab `AudioManager` configure o `AudioMixer` e `BGM AudioSource` (ou deixe o sistema criar).
3. Crie um `SoundEmitter` prefab que implemente `PooledObject` (já provido). Atribua o `AudioSource`.
4. Crie `SoundEmitterPoolableData` apontando para o prefab do emitter.
5. Crie `SoundEmitterPoolData` com `initialPoolSize` adequado e adicione o `SoundEmitterPoolableData`.
6. Em cada entidade que precise emitir SFX frequentemente:
    - Adicione `PlayerAudioController` (ou derive de `AudioControllerBase`).
    - No inspector do controlador, atribua:
        - `AudioConfig` (defaults de som)
        - `SoundEmitterPoolData` (pool local para esse controlador)
7. Em cenários eventuais (sons raros), você pode chamar `AudioManager.PlaySound(...)` via `AudioSystemHelper` ou serviço, que fará fallback criando um `AudioSource` temporário.

---

## 🧠 Fluxo de reprodução (ex.: Player tiro)
1. `InputSpawnerComponent` ou lógica de tiro chama: `playerAudioController.PlayShootSound(strategySound)`
2. `PlayerAudioController.PlayShootSound` chama `AudioControllerBase.PlaySound(sound, ...)`.
3. `AudioControllerBase` cria `AudioContext` e:
    - tenta `GetObject` do pool local (via `_localPool`) e usar `SoundEmitter` local para tocar.
    - se pool local não existir ou estiver esgotada, chama `AudioManager.PlaySound(...)` como fallback.
4. `SoundEmitter` é inicializado com `SoundData`, ajusta `AudioSource` e `Play()`.
5. Ao fim do som, `SoundEmitter` retorna ao pool via `ObjectPool.ReturnObject(...)`.

---

## 🔁 Regras importantes
- Pools de `SoundEmitter` são **locais por controlador**. Não centralize pools no `AudioManager`.
- `AudioManager` **não** deve instanciar/gerenciar `SoundEmitter` fixos — apenas BGM e mixer.
- `SoundEmitter` usa `PooledObject` para integração com `LifetimeManager` e `ObjectPool`.

---

## 🛠 Debug e tuning
- Ative `settings.debugEmitters` (se você adicionou tal flag) no `AudioServiceSettings` para logs.
- Ajuste `SoundEmitterPoolData.InitialPoolSize` de acordo com a entropia de sons por entidade.
- Use `AudioMixer` snapshots para controlar BGM/SFX via UI (opções do jogador).

---

## ❗ Remover/Alterar (opcional)
- Remova a referência ao pool global de emitters do `AudioManager` (já removido nessa consolidação).
- Remova cópias antigas de `SoundEmitter` que não herdarem `PooledObject`.
- Certifique-se de que `PoolManager` e `ObjectPool` estão compilando com a interface `IPoolable` usada aqui.

---

## 📌 Boas práticas
- Sons muito raros (UI clicks, efeitos únicos) podem ser tocados via `AudioManager.PlaySound` (fallback).
- Sons de alta taxa/frequência (tiros, passos, tiros de arma rápida) devem ser configurados com pool local.
- Use `SoundData.randomPitch` e `pitchVariation` para reduzir repetição sonora.

---
