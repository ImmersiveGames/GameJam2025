Perfeito 🎧 — aqui está o **`README_AudioSystem.md`** completo, limpo, bem formatado e pronto para copiar e colar no seu repositório.
Inclui descrição, hierarquia, exemplos práticos e explicações sobre volumes e multiplicadores.

---

```markdown
# 🎧 Immersive Games – Audio System

Sistema modular e extensível de áudio para Unity, com suporte a:
- **BGM (música ambiente)**
- **SFX (efeitos sonoros)**
- **Pooling de emissores**
- **Serviços globais via DependencyManager**
- **Fade, crossfade e controle de volume unificado**

---

## ⚙️ Arquitetura Geral

| Componente | Função |
|-------------|--------|
| **`AudioManager`** | Gerencia BGM (faixas principais), volumes e crossfades. |
| **`EntityAudioEmitter`** | Componente reutilizável por entidade que lida com pools e reprodução de SFX locais. |
| **`SoundEmitter`** | Objeto reutilizável (via pool) responsável por tocar SFX. |
| **`AudioSystemInitializer`** | Garante que o sistema e dependências de áudio estejam prontos no runtime. |
| **`AudioMathUtility`** | Serviço central de cálculos de volume, dB e pitch. |
| **`AudioServiceSettings`** | ScriptableObject global com multiplicadores e volumes padrão. |
| **`AudioConfig`** | Configurações padrão de áudio por entidade. |
| **`SoundData`** | Asset individual com dados de cada som (clip, mixer, volume, etc). |
| **`SoundBuilder`** | API fluente para instanciar e tocar sons de forma controlada. |

---

## 🔊 Hierarquia de Volume

O volume final de um som é calculado levando em conta vários níveis de controle:

```

FinalVolume = SoundData.volume
× AudioConfig.defaultVolume
× AudioServiceSettings.(bgmMultiplier | sfxMultiplier)
× AudioServiceSettings.(bgmVolume | sfxVolume)
× AudioServiceSettings.masterVolume
× AudioContext.volumeMultiplier

````

Cada categoria (BGM ou SFX) respeita seus multiplicadores e volumes globais.

---

## 🎛️ Configuração no Unity

### 1️⃣ Crie os assets necessários:
- `Assets/Audio/Configs/AudioServiceSettings.asset`
- `Assets/Audio/Configs/PlayerAudioConfig.asset`
- `Assets/Audio/Sounds/ShootSound.asset`
- `Assets/Audio/Sounds/BGM_MainTheme.asset`

### 2️⃣ Configure os valores:

#### AudioServiceSettings
| Campo | Descrição | Exemplo |
|--------|------------|---------|
| `masterVolume` | Volume geral global | `1.0` |
| `bgmVolume` | Volume de música ambiente | `0.8` |
| `sfxVolume` | Volume de efeitos sonoros | `1.0` |
| `bgmMultiplier` | Fator adicional fixo aplicado em runtime | `0.5` |
| `sfxMultiplier` | Fator adicional fixo aplicado aos SFX | `1.0` |

#### AudioConfig (por entidade)
| Campo | Descrição |
|--------|-----------|
| `defaultVolume` | Volume base do controlador |
| `defaultMixerGroup` | MixerGroup padrão dos sons |
| `maxDistance` | Distância máxima do som 3D |
| `useSpatialBlend` | Define se o áudio é espacializado |

> **Nota:** Sons específicos (tiro, dano, morte, etc.) agora são atribuídos diretamente nos componentes que os disparam,
sempre usando um `EntityAudioEmitter` compartilhado pela entidade.

---

## 🚀 Exemplos de Uso

### 🎵 Tocando BGM Globalmente

```csharp
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

public class MenuMusicStarter : MonoBehaviour
{
    [SerializeField] private SoundData mainMenuMusic;

    [Inject] private IAudioService _audioService;

    private void Awake()
    {
        // Garante que o AudioManager exista e injeta o serviço global.
        AudioSystemInitializer.EnsureAudioSystemInitialized();
        DependencyManager.Instance.InjectDependencies(this);
    }

    private void Start()
    {
        if (_audioService == null || mainMenuMusic == null)
        {
            return;
        }

        // Toca a música do menu com fade-in de 2s
        _audioService.PlayBGM(mainMenuMusic, true, 2f);
    }

    private void OnDisable()
    {
        // Para a música suavemente ao sair do menu
        _audioService?.StopBGM(1.5f);
    }
}
````

---

### 💥 Tocando SFX Local (ex: tiro, impacto, passo)

```csharp
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private EntityAudioEmitter audioEmitter;
    [SerializeField] private SoundData shootSound;

    private void Awake()
    {
        audioEmitter ??= GetComponent<EntityAudioEmitter>();
    }

    void Fire()
    {
        if (audioEmitter == null || shootSound == null)
        {
            return;
        }

        // Som direto via emissor configurado para o jogador
        var context = AudioContext.Default(transform.position, audioEmitter.UsesSpatialBlend);
        audioEmitter.Play(shootSound, context);

        // Ou via SoundBuilder para ajustes avançados (fade, pitch, etc.)
        audioEmitter.CreateBuilder()
            ?.WithSoundData(shootSound)
            .AtPosition(transform.position)
            .WithRandomPitch()
            .WithFadeIn(0.15f)
            .Play();
    }
}
```

---

### 🧮 Controle de Volume e Mixers em Runtime

```csharp
[Inject] private IAudioService _audioService;

void Awake()
{
    AudioSystemInitializer.EnsureAudioSystemInitialized();
    DependencyManager.Instance.InjectDependencies(this);
}

void ReduzirVolume()
{
    // Reduz o volume geral da música
    _audioService?.SetBGMVolume(0.5f);
}

void PausarRetomar()
{
    // Pausa e retoma
    _audioService?.PauseBGM();
    _audioService?.ResumeBGM();
}

void PararImediato()
{
    // Para imediatamente (sem fade)
    _audioService?.StopBGMImmediate();
}
```

---

### 🎚️ Ajustando o equilíbrio BGM / SFX

Se o **BGM** estiver mais alto que os **SFX**, ajuste os multiplicadores no asset `AudioServiceSettings`:

| Campo           | Descrição                          | Valor sugerido |
| --------------- | ---------------------------------- | -------------- |
| `bgmMultiplier` | Reduz todas as músicas globalmente | `0.5`          |
| `sfxMultiplier` | Mantém o nível dos efeitos         | `1.0`          |
| `bgmVolume`     | Volume runtime controlável por UI  | `0.8`          |
| `masterVolume`  | Volume global (master)             | `1.0`          |

💡 **Dica:** É comum deixar `bgmMultiplier` menor que `sfxMultiplier` para dar mais destaque aos efeitos.

---

## 🧰 Depuração

Para ver logs detalhados de áudio (com volumes e dB):

* Ative `debugEmitters = true` em `AudioServiceSettings`.

O sistema exibirá no console:

```
[SFX Pool] Clip=GunShot | Linear=0.80 | dB=-1.9 | Mixer=SFX
[BGM] Clip=MainTheme | Linear=0.40 | dB=-7.9 | Mixer=Music
```

---

## 🧩 Fluxo de Inicialização

1. **`AudioSystemInitializer`** é executado automaticamente (`RuntimeInitializeOnLoad`).
2. Verifica e instancia o **`AudioManager`** (se necessário, via `Resources/Audio/Prefabs/AudioManager`).
3. Registra globalmente os serviços:

    * `IAudioService` (gerenciamento de BGM)
    * `IAudioMathService` (conversões e cálculos)
4. Controllers e SoundEmitters usam esses serviços automaticamente.

---

## ✅ Boas Práticas

* Use **`SoundData` ScriptableObjects** para todos os sons reutilizáveis.
* Centralize volumes no **`AudioServiceSettings`** — nunca direto no `AudioSource`.
* Use **`SoundBuilder`** para criar sons temporários de forma fluida.
* Prefira **pools locais** (`SoundEmitterPoolData`) para entidades que reproduzem sons frequentemente.
* Use **mixer groups** (`SFX`, `Music`, `UI`) para maior controle no mixer global.

---

## 🧩 Exemplo de Estrutura no Projeto

```
Assets/
 ├── Audio/
 │   ├── Prefabs/
 │   │   └── AudioManager.prefab
 │   ├── Configs/
 │   │   ├── AudioServiceSettings.asset
 │   │   └── PlayerAudioConfig.asset
 │   ├── Sounds/
 │   │   ├── ShootSound.asset
 │   │   └── BGM_MainTheme.asset
 │   └── Mixers/
 │       ├── MasterMixer.mixer
 │       ├── Music.mixerGroup
 │       └── SFX.mixerGroup
 └── Scripts/
     └── AudioSystem/
        ├── AudioManager.cs
        ├── Components/
        │   ├── EntityAudioEmitter.cs
        │   └── SoundEmitter.cs
        ├── AudioSystemInitializer.cs
         ├── AudioMathUtility.cs
         ├── Configs/
         │   ├── AudioConfig.cs
         │   ├── SoundData.cs
         │   └── AudioServiceSettings.cs
         └── Services/
             └── Interfaces/
```

---

## 🧠 Observação

O sistema foi projetado para ser **escalável e desacoplado**.
O `AudioMathUtility` centraliza toda a lógica de volume/pitch, permitindo ajustes perceptuais futuros sem alterar controladores.

---

## 🪄 Exemplo de Extensão (novo tipo de som)

```csharp
public class EnemyAudioFeedback : MonoBehaviour
{
    [SerializeField] private EntityAudioEmitter emitter;
    [SerializeField] private SoundData alertSound;
    [SerializeField] private SoundData deathSound;

    private void Awake()
    {
        emitter ??= GetComponent<EntityAudioEmitter>();
    }

    public void PlayAlert()
    {
        if (emitter == null || alertSound == null) return;
        emitter.Play(alertSound, AudioContext.Default(transform.position, emitter.UsesSpatialBlend));
    }

    public void PlayDeath()
    {
        if (emitter == null || deathSound == null) return;
        emitter.Play(deathSound, AudioContext.Default(transform.position, emitter.UsesSpatialBlend));
    }
}
```

---

## 📘 Conclusão

Com este sistema, o áudio fica:

✅ Centralizado
✅ Controlável por configuração e UI
✅ Balanceado entre BGM e SFX
✅ Otimizado com pooling
✅ Fácil de depurar e expandir

---

> **Autor:** Equipe Immersive Games
> **Versão:** 1.0
> **Compatível com:** Unity 2022+
> **Licença:** Interna / Proprietária

```