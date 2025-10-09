using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.AudioSystem.Base
{
    /// <summary>
    /// Base para controllers por entidade. Cada controller pode:
    /// - ter um Pool local (SoundEmitterPoolData) — o controller cria e administra o pool
    /// - usar AudioServiceSettings (via DI, se presente)
    /// - usar AudioMathService (via DI) para calcular volumes consistentes
    /// </summary>
    public abstract class AudioControllerBase : MonoBehaviour
    {
        [Header("Entity Audio Config")]
        [SerializeField] protected AudioConfig audioConfig;
        [SerializeField] protected Pool.SoundEmitterPoolData poolData; // pool local por entidade (opcional)

        private ObjectPool _localPool;
        private IAudioMathService _math;
        private AudioServiceSettings _serviceSettings;

        protected virtual void Awake()
        {
            AudioSystemInitializer.EnsureAudioSystemInitialized();

            if (DependencyManager.Instance != null)
            {
                DependencyManager.Instance.TryGetGlobal(out _math);
                DependencyManager.Instance.TryGetGlobal(out _serviceSettings);
            }

            if (poolData != null)
            {
                CreateLocalPool();
            }
            else
            {
                DebugUtility.LogVerbose<AudioControllerBase>($"{name} sem pool configurado — fallback com fontes temporárias para SFX locais", "yellow", this);
            }
        }

        private void CreateLocalPool()
        {
            if (_localPool != null) return;

            var poolGo = new GameObject($"{gameObject.name}_Pool_{poolData.ObjectName}");
            poolGo.transform.SetParent(transform, false);
            var pool = poolGo.AddComponent<ObjectPool>();
            pool.SetData(poolData);
            pool.Initialize();
            _localPool = pool;
            DebugUtility.LogVerbose<AudioControllerBase>($"Pool local '{poolData.ObjectName}' criado para {name}", "cyan", this);
        }

        /// <summary>Uso principal: tocar SFX local usando pool (se disponível) ou fallback para AudioSource temporário.</summary>
        protected void PlaySoundLocal(SoundData soundData, AudioContext ctx, float fadeInSeconds = 0f)
        {
            if (soundData == null || soundData.clip == null) return;

            if (_localPool != null)
                PlayWithPool(soundData, ctx, fadeInSeconds);
            else
                PlayWithTemporarySource(soundData, ctx, fadeInSeconds);
        }

        private void PlayWithPool(SoundData soundData, AudioContext ctx, float fadeInSeconds)
        {
            var emitter = _localPool.GetObject(ctx.position, null, null, false) as SoundEmitter;
            if (emitter == null)
            {
                DebugUtility.LogWarning<AudioControllerBase>($"[{name}] Falha ao obter SoundEmitter do pool local", this);
                return;
            }

            emitter.Initialize(soundData);
            emitter.SetSpatialBlend(ctx.useSpatial ? soundData.spatialBlend : 0f);

            float maxDist = audioConfig != null ? audioConfig.maxDistance : soundData.maxDistance;
            emitter.SetMaxDistance(maxDist);

            var mixer = soundData.mixerGroup ?? audioConfig?.defaultMixerGroup;
            emitter.SetMixerGroup(mixer);

            // cálculo centralizado
            float master = _serviceSettings != null ? _serviceSettings.masterVolume : 1f;
            float categoryVol = _serviceSettings != null ? _serviceSettings.sfxVolume : 1f;
            float categoryMul = _serviceSettings != null ? _serviceSettings.sfxMultiplier : 1f;
            float configDefault = audioConfig != null ? audioConfig.defaultVolume : 1f;

            float finalVol = _math?.CalculateFinalVolume(soundData.volume, configDefault, categoryVol, categoryMul, master, ctx.volumeMultiplier, ctx.volumeOverride) ?? Mathf.Clamp01(soundData.volume * configDefault * categoryVol * master * ctx.volumeMultiplier);

            // emitter expects multiplier relative to soundData.volume (it multiplies Data.volume * multiplier)
            float multiplier = soundData.volume > 0f ? Mathf.Clamp01(finalVol / soundData.volume) : Mathf.Clamp01(finalVol);
            emitter.SetVolumeMultiplier(multiplier);

            DebugUtility.LogVerbose<AudioControllerBase>($"[SFX Pool] {soundData.clip?.name} finalVol={finalVol:F3} mult={multiplier:F3} (DataVol={soundData.volume:F2}, cfgDef={configDefault:F2})", "cyan", this);

            if (soundData.randomPitch)
                emitter.WithRandomPitch(-soundData.pitchVariation, soundData.pitchVariation);

            emitter.Activate(ctx.position);
            if (fadeInSeconds > 0f) emitter.PlayWithFade(multiplier, fadeInSeconds);
            else emitter.Play();
        }

        private void PlayWithTemporarySource(SoundData soundData, AudioContext ctx, float fadeInSeconds)
        {
            var tempGo = new GameObject($"TempAudio_{soundData.clip?.name}");
            tempGo.transform.SetParent(transform);
            tempGo.transform.position = ctx.position;
            var src = tempGo.AddComponent<AudioSource>();

            src.clip = soundData.clip;
            src.outputAudioMixerGroup = soundData.mixerGroup ?? audioConfig?.defaultMixerGroup;
            src.priority = soundData.priority;
            src.loop = soundData.loop;
            src.playOnAwake = false;
            src.spatialBlend = ctx.useSpatial ? soundData.spatialBlend : 0f;
            src.maxDistance = audioConfig != null ? audioConfig.maxDistance : soundData.maxDistance;

            float master = _serviceSettings != null ? _serviceSettings.masterVolume : 1f;
            float categoryVol = _serviceSettings != null ? _serviceSettings.sfxVolume : 1f;
            float categoryMul = _serviceSettings != null ? _serviceSettings.sfxMultiplier : 1f;
            float configDefault = audioConfig != null ? audioConfig.defaultVolume : 1f;

            float finalVol = _math?.CalculateFinalVolume(soundData.volume, configDefault, categoryVol, categoryMul, master, ctx.volumeMultiplier, ctx.volumeOverride) ?? Mathf.Clamp01(soundData.volume * configDefault * categoryVol * master * ctx.volumeMultiplier);

            src.volume = finalVol;

            if (soundData.randomPitch)
                src.pitch = _math?.ApplyRandomPitch(1f, soundData.pitchVariation) ?? Random.Range(1f - soundData.pitchVariation, 1f + soundData.pitchVariation);

            if (fadeInSeconds > 0f)
            {
                src.volume = 0f;
                src.Play();
                StartCoroutine(FadeVolume(src, 0f, finalVol, fadeInSeconds));
            }
            else
            {
                src.Play();
            }

            if (soundData.loop) return;
            if (soundData.clip != null) Destroy(tempGo, soundData.clip.length + fadeInSeconds + 0.1f);
        }

        private System.Collections.IEnumerator FadeVolume(AudioSource src, float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                src.volume = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            src.volume = to;
        }

        /// <summary>Cria um SoundBuilder que usa o pool local (fluente).</summary>
        protected SoundBuilder CreateSoundBuilder()
        {
            if (_localPool == null)
            {
                DebugUtility.LogWarning<AudioControllerBase>($"[{name}] Pool local não inicializado ao criar SoundBuilder — SoundBuilder requer pool", this);
                return null;
            }

            return new SoundBuilder(_localPool, _math, _serviceSettings, audioConfig);
        }

#if UNITY_EDITOR
        [ContextMenu("Audio/Test Play Local Shoot")]
        private void EditorTestShoot()
        {
            if (audioConfig?.shootSound == null) { Debug.LogWarning("No shoot sound in AudioConfig"); return; }
            PlaySoundLocal(audioConfig.shootSound, AudioContext.Default(transform.position));
            Debug.Log($"[AudioController] Test shoot played: {audioConfig.shootSound.clip?.name}");
        }
#endif
    }
}
