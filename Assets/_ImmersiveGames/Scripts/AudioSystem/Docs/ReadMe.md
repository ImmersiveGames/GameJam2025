# 🎧 Immersive Games – Audio System (v2.0)

Sistema unificado, modular e extensível de áudio para Unity, com:

* **BGM** (música ambiente)
* **SFX** (efeitos sonoros)
* **Pooling global de SoundEmitter**
* **Serviços globais via DependencyManager**
* **Fade, crossfade, spatial, random pitch**
* **AudioBuilder fluente**
* **Auditoria em runtime + Painel de Preview**

---

# ⚙️ Arquitetura Geral (v2.0)

A arquitetura foi simplificada e centralizada para maior estabilidade, especialmente em trocas de cena.

| Componente                  | Função                                                               |
| --------------------------- | -------------------------------------------------------------------- |
| **AudioManager**            | Gerencia BGM, crossfades, mixer, pause/resume.                       |
| **AudioSfxService**         | Sistema global de SFX, baseado em um **pool único** de SoundEmitter. |
| **EntityAudioEmitter**      | Camada fina por entidade — direciona tudo ao `AudioSfxService`.      |
| **SoundEmitter**            | Emissor real que toca o áudio (instanciado via pool).                |
| **AudioVolumeService**      | Mistura volumes (Master, BGM, SFX, Contexto, Clip).                  |
| **AudioMathUtility**        | Cálculo de volumes lineares/dB, pitch e curvas perceptivas.          |
| **AudioSystemInitializer**  | Garante que tudo esteja inicializado antes de uso.                   |
| **SoundData**               | Configuração individual de cada áudio.                               |
| **AudioContext**            | Dados temporários por chamada (posição, spatial, volume override).   |
| **SoundBuilder**            | API fluente para construir SFX (fade, spatial, random pitch...).     |
| **AudioRuntimeDiagnostics** | Overlay de depuração (BGM + SFX + Emitters).                         |
| **AudioPreviewPanel**       | Preview de SFX e BGM em runtime.                                     |

---

# 🔊 Hierarquia de Volume

O volume final é calculado exclusivamente pelo **AudioVolumeService**, combinando:

```
FinalVolume =
    SoundData.volume
  × AudioConfig.defaultVolume
  × AudioServiceSettings.(sfxVolume ou bgmVolume)
  × AudioServiceSettings.(sfxMultiplier ou bgmMultiplier)
  × AudioServiceSettings.masterVolume
  × AudioContext.volumeMultiplier
 (⊕ VolumeOverride, se definido)
```

Cada camada tem propósito claro e pode ser ajustada individualmente.

---

# 🧱 Estrutura Atualizada do Sistema

### 1. SFX – Novo fluxo simplificado

Toda reprodução de efeito sonoro passa por:

```
IAudioSfxService → (pool global) → SoundEmitter
```

Nenhum Entity cria fonte de áudio, nem instância pool local, nem precisa gerenciar corrotinas.

---

### 2. EntityAudioEmitter – Nova versão (v2.0)

O `EntityAudioEmitter` agora é apenas:

* Um resolvedor de `IAudioSfxService`.
* Uma camada de conveniência para entidades que desejam sons ligados à sua posição.

Exemplo do fluxo atual:

```csharp
var ctx = AudioContext.Default(transform.position, UsesSpatialBlend);
_sfxService.PlayOneShot(soundData, ctx, fadeInSeconds);
```

> Ele **não mantém pools**, não cria fontes e não faz fade manual.
> Tudo isso é responsabilidade do `AudioSfxService`.

---

### 3. Pool Global de SoundEmitter

* Configurado por **SoundEmitterPoolData** localizado em:

  ```
  Resources/Audio/SoundEmitters/PD_SoundEmitter.asset
  ```
* Registrado automaticamente via:

  ```
  PoolManager.Instance.RegisterPool(poolData)
  ```
* Reutilizado por todos os SFX do jogo.

---

# 🧰 Configuração no Unity

### 1️⃣ Assets necessários

```
Assets/Audio/Configs/AudioServiceSettings.asset
Assets/Audio/Configs/<Entity>AudioConfig.asset
Assets/Audio/Configs/PD_SoundEmitter.asset  (Pool)
Assets/Audio/Sounds/*.asset  (SoundData)
```

### 2️⃣ Ajustes importantes em PD_SoundEmitter.asset:

* `InitialPoolSize`: 10–30
* `CanExpand`: true
* `ObjectConfigs` → 1 entrada de `SoundEmitterPoolableData`
* Prefab do SoundEmitter:

  ```
  Resources/Audio/Prefabs/SoundEmitter.prefab
  ```

---

# 🚀 Exemplos Atualizados

## 🎵 Tocar BGM (via AudioManager)

```csharp
[Inject] private IAudioService _audio;

private void Start()
{
    AudioSystemInitializer.EnsureAudioSystemInitialized();
    DependencyManager.Instance.InjectDependencies(this);

    _audio.PlayBGM(mainMenuBgm, loop: true, fadeInDuration: 1f);
}
```

---

## 🔫 SFX via EntityAudioEmitter

```csharp
audioEmitter.Play(shootSound, 
    AudioContext.Default(transform.position, audioEmitter.UsesSpatialBlend));
```

---

## ⚡ SFX Avançado via SoundBuilder

```csharp
audioEmitter.CreateBuilder()
    ?.WithSoundData(explosion)
    .AtPosition(transform.position)
    .WithRandomPitch()
    .WithFadeIn(0.15f)
    .Play();
```

---

# 🎚️ Ajuste de Volumes em Runtime

```csharp
_audio.SetBGMVolume(0.5f);    // Música
_audioServiceSettings.sfxVolume = 0.8f; // SFX global
_audioServiceSettings.masterVolume = 1f; // Master
```

---

# 🧵 Debug – Nova Seção (v2.0)

O novo sistema conta com duas ferramentas essenciais:

---

## 1) **AudioRuntimeDiagnostics.cs** – Overlay Completo

Mostra em tempo real:

### BGM

* Clip atual
* Volume final
* Playing / Paused / Stopped

### SFX

* Emitters ativos (ex.: 5/18 tocando)
* Lista dos emissores
* Nome do clip
* Posição
* Ativo/Idle

### Funções

| Ação                   | Tecla  |
| ---------------------- | ------ |
| Ligar/desligar overlay | **F9** |

### Como usar

Adicionar o componente a um GameObject que exista em todas as cenas:

```csharp
gameObject.AddComponent<AudioRuntimeDiagnostics>();
```

---

## 2) **AudioPreviewPanel.cs** – Preview de SFX e BGM

Permite testar qualquer `SoundData` em runtime.

### Recursos:

* Lista de SFX navegáveis (`<< Play >>`)
* Lista de BGM navegáveis (`<< Play/Loop Stop >>`)
* Fade configurável
* Volume multiplier para preview
* Modo Non-spatial automático para SFX de teste

### Tecla

| Função                 | Tecla   |
| ---------------------- | ------- |
| Mostrar/Ocultar painel | **F10** |

### Como usar

1. Adicione ao GameObject:

   ```
   AudioPreviewPanel
   ```
2. Preencha no Inspector:

   * `sfxClips[]`
   * `bgmClips[]`

Agora você pode testar **qualquer áudio do jogo** sem precisar criar scripts temporários.

---

# 🧪 Testes Automatizados – AudioSystemScenarioTester

Para validar a arquitetura completa, o projeto já conta com:

* Testes de SFX:

   * OneShot
   * Spatial/Non-spatial
   * Random Pitch (10x)
   * Fade-in
   * Stress test (30 sons)
* Testes de BGM:

   * Play/Stop
   * Fade-in/out
   * Pause/Resume
   * Volumes (1.0 → 0.5 → 0.2 → 1.0)

Basta rodar a cena com:

```csharp
AudioSystemScenarioTester
```

---

# 🛠️ Fluxo de Inicialização Atual (v2.0)

1. **AudioSystemInitializer** é chamado (RuntimeInitializeOnLoad).
2. Garante:

   * AudioManager
   * Pool de SoundEmitter
   * Registro de:

      * IAudioSfxService
      * IAudioService
      * IAudioVolumeService
      * IAudioMathService
3. Dependências disponíveis globalmente via `DependencyManager`.

---

# ✔️ Boas Práticas (atualizadas)

* Use sempre `SoundData` (nada de clipes “nus”).
* Utilize `EntityAudioEmitter` ou `SoundBuilder` — **não toque SFX direto no AudioSource**.
* Ajuste volumes no `AudioServiceSettings`, não no AudioSource.
* Use mixers (`SFX`, `Music`, `UI`) para mixagem profissional.
* Para debug de cena:

   * **F9** → Diagnostics
   * **F10** → Preview

---

# 📘 Conclusão

A nova versão (v2.0) oferece:

* **Estabilidade total em troca de cena**
* **Pooling único, sem leaks**
* **API fluida para SFX complexos**
* **Depuração profissional em tempo real**
* **Preview runtime poderoso para level/audio design**
* **Arquitetura limpa, sustentável, SOLID**

---
