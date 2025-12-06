using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;

namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class GameAudioManager : Singleton<GameAudioManager>
    {
        [SerializeField] private SoundData mainMenuBGM;
        [Header("Settings")]
        [SerializeField] private float bgmFadeDuration = 2f;

        [Inject] private IAudioService _audioService;

        protected override void Awake()
        {
            base.Awake();

            // Garantir que o sistema global de áudio esteja pronto antes da injeção.
            AudioSystemInitializer.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.InjectDependencies(this);
            }
            else
            {
                DebugUtility.LogWarning<GameAudioManager>("DependencyManager indisponível — áudio global não será injetado.");
            }
        }

        private void Start()
        {
            if (!IsServiceValid(_audioService))
            {
                TryResolveAudioService();

                if (!IsServiceValid(_audioService))
                {
                    DebugUtility.LogWarning<GameAudioManager>("IAudioService não injetado — verifique a inicialização do AudioManager.");
                    return;
                }
            }

            if (mainMenuBGM != null && mainMenuBGM.clip != null)
            {
                _audioService.PlayBGM(mainMenuBGM, true, bgmFadeDuration);
            }
            else
            {
                DebugUtility.LogWarning<GameAudioManager>("BGM não configurado — verifique SoundData.");
            }
        }

        private void TryResolveAudioService()
        {
            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.TryGetGlobal(out _audioService);
            }
        }

        private static bool IsServiceValid(IAudioService service)
        {
            if (service == null) return false;

            if (service is Object unityObj)
            {
                return unityObj;
            }

            return true;
        }

#if UNITY_EDITOR
        [ContextMenu("Test Play BGM (Editor Preview)")]
        private void TestPlayBGMEditor()
        {
            if (mainMenuBGM == null || mainMenuBGM.clip == null)
            {
                DebugUtility.LogWarning<GameAudioManager>("Nenhum BGM configurado para preview.");
                return;
            }

            // Simula runtime: Volume com multipliers (assuma defaults se sem settings)
            const float simulatedMultiplier = 1f; // Simule settings.bgmMultiplier
            const float simulatedBgmVol = 1f; // Simule settings.bgmVolume
            float simulatedVol = Mathf.Clamp01(mainMenuBGM.volume * simulatedMultiplier * simulatedBgmVol);
            float dB = 20f * Mathf.Log10(simulatedVol + 0.0001f); // Evita log0
            AudioSource.PlayClipAtPoint(mainMenuBGM.clip, Vector3.zero, simulatedVol);
            DebugUtility.LogVerbose<GameAudioManager>($"Preview BGM: {mainMenuBGM.clip.name} (Volume linear: {simulatedVol}, dB simulado: {dB}, Loop: {mainMenuBGM.loop}, Fade simulada: {bgmFadeDuration}s, Mixer: {mainMenuBGM.mixerGroup?.name ?? "None"})");
        }
#endif
    }
}