using System.Collections;
using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Core;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DependencySystems;

namespace _ImmersiveGames.Scripts.AudioSystem.Tests
{
    /// <summary>
    /// Runner de cenários de teste para o sistema de áudio.
    /// 
    /// Coloque este script em um GameObject vazio em uma cena de teste,
    /// arraste os SoundData no Inspector e dê Play.
    /// 
    /// - Etapa 4: testa SFX avançados (spatial/non-spatial, random pitch, fade-in, stress).
    /// - Etapa 5: testa BGM (play/stop, troca de faixa, pause/resume, volume).
    /// 
    /// Controles:
    /// - T:  dispara novamente o cenário completo de SFX.
    /// - Y:  dispara novamente o cenário completo de BGM.
    /// </summary>
    public class AudioSystemScenarioTester : MonoBehaviour
    {
        [Header("SFX TEST SOUNDS (Etapa 4)")]
        [Tooltip("Som padrão para teste de one-shot simples.")]
        public SoundData basicSfx;

        [Tooltip("Som para testar spatial vs non-spatial (pode ser o mesmo do basic).")]
        public SoundData spatialSfx;

        [Tooltip("Som curto e repetível para testar random pitch (passos, tiro, impacto).")]
        public SoundData randomPitchSfx;

        [Tooltip("Som de duração > 1s para testar fade-in perceptível.")]
        public SoundData fadeInSfx;

        [Tooltip("Som para teste de stress (múltiplas instâncias). Pode reutilizar o basic.")]
        public SoundData stressSfx;

        [Header("BGM TEST SOUNDS (Etapa 5)")]
        [Tooltip("BGM de menu principal.")]
        public SoundData mainMenuBgm;

        [Tooltip("BGM de gameplay.")]
        public SoundData gameplayBgm;

        [Header("Execução Automática")]
        [Tooltip("Se verdadeiro, roda o cenário de SFX automaticamente no Start.")]
        public bool autoRunSfxOnStart = true;

        [Tooltip("Se verdadeiro, roda o cenário de BGM automaticamente no Start (após SFX).")]
        public bool autoRunBgmOnStart = true;

        [Tooltip("Delay padrão entre passos de teste (em segundos).")]
        public float stepDelay = 1.0f;

        [Tooltip("Delay extra entre grupos de testes (em segundos).")]
        public float blockDelay = 1.5f;

        private IAudioSfxService _sfxService;
        private IBgmAudioService _bgmAudioService;

        private Coroutine _runningSfxScenario;
        private Coroutine _runningBgmScenario;

        private void Awake()
        {
            // Garante que o sistema de áudio está inicializado e registrado no DI
            AudioSystemBootstrap.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.TryGetGlobal(out _sfxService);
                DependencyManager.Provider.TryGetGlobal(out _bgmAudioService);
            }

            if (_sfxService == null)
            {
                Debug.LogWarning("[AudioTest] IAudioSfxService não encontrado. Verifique AudioSystemBootstrap / registro de serviços.");
            }

            if (_bgmAudioService == null)
            {
                Debug.LogWarning("[AudioTest] IBgmAudioService (BGM) não encontrado. Testes de BGM ficarão limitados.");
            }
        }

        private void Start()
        {
            Debug.Log("[AudioTest] AudioSystemScenarioTester inicializado.");
            Debug.Log("[AudioTest] Controles: T = rodar cenário SFX, Y = rodar cenário BGM.");

            if (autoRunSfxOnStart)
            {
                _runningSfxScenario = StartCoroutine(RunSfxScenario());
            }

            if (autoRunBgmOnStart)
            {
                // roda após um pequeno atraso para não conflitar com o início dos SFX
                var delay = autoRunSfxOnStart ? (stepDelay * 8f) : 0f;
                _runningBgmScenario = StartCoroutine(RunBgmScenarioWithDelay(delay));
            }
        }

        private void Update()
        {
            // Triggers manuais para rer rodar cenários
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (_runningSfxScenario != null)
                {
                    StopCoroutine(_runningSfxScenario);
                }
                _runningSfxScenario = StartCoroutine(RunSfxScenario());
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                if (_runningBgmScenario != null)
                {
                    StopCoroutine(_runningBgmScenario);
                }
                _runningBgmScenario = StartCoroutine(RunBgmScenario());
            }
        }

        #region SFX Scenario (Etapa 4)

        private IEnumerator RunSfxScenario()
        {
            if (_sfxService == null)
            {
                Debug.LogError("[AudioTest][SFX] IAudioSfxService indisponível, abortando cenário de SFX.");
                yield break;
            }

            Debug.Log("====================================================");
            Debug.Log("[AudioTest][SFX] Iniciando cenário de testes (Etapa 4).");
            Debug.Log("====================================================");

            var position = transform.position;

            yield return RunOneShotBasicTest();
            yield return RunSpatialVsNonSpatialTest(position);
            yield return RunRandomPitchTest(position);
            yield return RunFadeInTest(position);
            yield return RunStressTest(position);

            Debug.Log("====================================================");
            Debug.Log("[AudioTest][SFX] Cenário de SFX (Etapa 4) concluído.");
            Debug.Log("====================================================");
        }

        private IEnumerator RunOneShotBasicTest()
        {
            // 4.1 – Teste de one-shot básico
            if (basicSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.1 - OneShot básico (non-spatial).");
                var ctx = AudioContext.NonSpatial();
                _sfxService.PlayOneShot(basicSfx, ctx);
            }
            else
            {
                Debug.LogWarning("[AudioTest][SFX] 4.1 - basicSfx não configurado.");
            }

            yield return new WaitForSeconds(stepDelay);
        }

        private IEnumerator RunSpatialVsNonSpatialTest(Vector3 position)
        {
            // 4.2 – Spatial vs. Non-Spatial
            if (spatialSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.2 - Spatial SFX (posicione a câmera em volta para perceber pan/volume).");
                var spatialCtx = AudioContext.Default(position, useSpatial: true);
                _sfxService.PlayOneShot(spatialSfx, spatialCtx);

                yield return new WaitForSeconds(stepDelay);

                Debug.Log("[AudioTest][SFX] 4.2 - Non-Spatial SFX (igual ao anterior, mas sem spatial).");
                var nonSpatialCtx = AudioContext.NonSpatial();
                _sfxService.PlayOneShot(spatialSfx, nonSpatialCtx);
            }
            else
            {
                Debug.LogWarning("[AudioTest][SFX] 4.2 - spatialSfx não configurado.");
            }

            yield return new WaitForSeconds(blockDelay);
        }

        private IEnumerator RunRandomPitchTest(Vector3 position)
        {
            // 4.3 – Random Pitch (múltiplas instâncias)
            if (randomPitchSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.3 - Random Pitch (10 instâncias rápidas).");
                for (int i = 0; i < 10; i++)
                {
                    var ctx = AudioContext.Default(position, useSpatial: false, volMult: 1f);
                    _sfxService.PlayOneShot(randomPitchSfx, ctx);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                Debug.LogWarning("[AudioTest][SFX] 4.3 - randomPitchSfx não configurado.");
            }

            yield return new WaitForSeconds(blockDelay);
        }

        private IEnumerator RunFadeInTest(Vector3 position)
        {
            // 4.4 – Fade-in
            if (fadeInSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.4 - Fade-in SFX (0.5s).");
                var ctx = AudioContext.Default(position, useSpatial: false);
                _sfxService.PlayOneShot(fadeInSfx, ctx, fadeInSeconds: 0.5f);
            }
            else
            {
                Debug.LogWarning("[AudioTest][SFX] 4.4 - fadeInSfx não configurado.");
            }

            yield return new WaitForSeconds(blockDelay);
        }

        private IEnumerator RunStressTest(Vector3 position)
        {
            // 4.5 – Stress test (múltiplos SFX em sequência)
            if (stressSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.5 - Stress test (30 instâncias rápidas).");
                for (int i = 0; i < 30; i++)
                {
                    var offset = Random.insideUnitSphere * 2f;
                    var ctx = AudioContext.Default(position + offset, useSpatial: true);
                    _sfxService.PlayOneShot(stressSfx, ctx);
                    yield return new WaitForSeconds(0.05f);
                }
            }
            else
            {
                Debug.LogWarning("[AudioTest][SFX] 4.5 - stressSfx não configurado.");
            }
        }

        #endregion

        #region BGM Scenario (Etapa 5)

        private IEnumerator RunBgmScenarioWithDelay(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            yield return RunBgmScenario();
        }

        private IEnumerator RunBgmScenario()
        {
            if (_bgmAudioService == null)
            {
                Debug.LogError("[AudioTest][BGM] IBgmAudioService indisponível, abortando cenário de BGM.");
                yield break;
            }

            Debug.Log("====================================================");
            Debug.Log("[AudioTest][BGM] Iniciando cenário de testes (Etapa 5).");
            Debug.Log("====================================================");

            // 5.1 – Play/Stop BGM de menu
            if (mainMenuBgm != null)
            {
                Debug.Log("[AudioTest][BGM] 5.1 - Play mainMenuBgm com fade-in de 1.0s (loop ON).");
                _bgmAudioService.PlayBGM(mainMenuBgm, loop: true, fadeInDuration: 1.0f);
                yield return new WaitForSeconds(3.0f);

                Debug.Log("[AudioTest][BGM] 5.1 - Stop mainMenuBgm com fade-out de 1.0s.");
                _bgmAudioService.StopBGM(fadeOutDuration: 1.0f);
                yield return new WaitForSeconds(2.0f);
            }
            else
            {
                Debug.LogWarning("[AudioTest][BGM] 5.1 - mainMenuBgm não configurado.");
            }

            yield return new WaitForSeconds(blockDelay);

            // 5.2 – Troca de BGM (menu -> gameplay)
            if (gameplayBgm != null)
            {
                Debug.Log("[AudioTest][BGM] 5.2 - Play gameplayBgm com fade-in de 1.0s (loop ON).");
                _bgmAudioService.PlayBGM(gameplayBgm, loop: true, fadeInDuration: 1.0f);
                yield return new WaitForSeconds(3.0f);

                Debug.Log("[AudioTest][BGM] 5.2 - Pause gameplayBgm por 2.0s.");
                _bgmAudioService.PauseBGM();
                yield return new WaitForSeconds(2.0f);

                Debug.Log("[AudioTest][BGM] 5.2 - Resume gameplayBgm.");
                _bgmAudioService.ResumeBGM();
                yield return new WaitForSeconds(3.0f);

                Debug.Log("[AudioTest][BGM] 5.2 - Stop gameplayBgm com fade-out de 1.0s.");
                _bgmAudioService.StopBGM(fadeOutDuration: 1.0f);
                yield return new WaitForSeconds(2.0f);
            }
            else
            {
                Debug.LogWarning("[AudioTest][BGM] 5.2 - gameplayBgm não configurado.");
            }

            yield return new WaitForSeconds(blockDelay);

            // 5.3 – Teste simples de volume BGM
            if (mainMenuBgm != null)
            {
                Debug.Log("[AudioTest][BGM] 5.3 - Teste de volume BGM (1.0 -> 0.5 -> 0.2).");
                _bgmAudioService.PlayBGM(mainMenuBgm, loop: true, fadeInDuration: 0.5f);
                yield return new WaitForSeconds(2.0f);

                Debug.Log("[AudioTest][BGM] 5.3 - BGM volume = 0.5 (deve soar ~metade).");
                _bgmAudioService.SetBGMVolume(0.5f);
                yield return new WaitForSeconds(2.0f);

                Debug.Log("[AudioTest][BGM] 5.3 - BGM volume = 0.2 (bem mais baixo).");
                _bgmAudioService.SetBGMVolume(0.2f);
                yield return new WaitForSeconds(2.0f);

                Debug.Log("[AudioTest][BGM] 5.3 - BGM volume = 1.0 (volta ao normal) e Stop com fade 0.5s.");
                _bgmAudioService.SetBGMVolume(1.0f);
                _bgmAudioService.StopBGM(fadeOutDuration: 0.5f);
                yield return new WaitForSeconds(1.0f);
            }
            else
            {
                Debug.LogWarning("[AudioTest][BGM] 5.3 - mainMenuBgm não configurado, pulando teste de volume.");
            }

            Debug.Log("====================================================");
            Debug.Log("[AudioTest][BGM] Cenário de BGM (Etapa 5) concluído.");
            Debug.Log("====================================================");
        }

        #endregion
    }
}
