using System.Collections;
using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Configs;
using _ImmersiveGames.Scripts.AudioSystem.Core;
using _ImmersiveGames.Scripts.AudioSystem.Interfaces;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.Scripts.AudioSystem.System;

namespace _ImmersiveGames.Scripts.AudioSystem.Tests
{
    /// <summary>
    /// Runner de cen�rios de teste para o sistema de �udio.
    /// 
    /// Coloque este script em um GameObject vazio em uma cena de teste,
    /// arraste os SoundData no Inspector e d� Play.
    /// 
    /// - Etapa 4: testa SFX avan�ados (spatial/non-spatial, random pitch, fade-in, stress).
    /// - Etapa 5: testa BGM (play/stop, troca de faixa, pause/resume, volume).
    /// 
    /// Controles:
    /// - T:  dispara novamente o cen�rio completo de SFX.
    /// - Y:  dispara novamente o cen�rio completo de BGM.
    /// </summary>
    public class AudioSystemScenarioTester : MonoBehaviour
    {
        [Header("SFX TEST SOUNDS (Etapa 4)")]
        [Tooltip("Som padr�o para teste de one-shot simples.")]
        public SoundData basicSfx;

        [Tooltip("Som para testar spatial vs non-spatial (pode ser o mesmo do basic).")]
        public SoundData spatialSfx;

        [Tooltip("Som curto e repet�vel para testar random pitch (passos, tiro, impacto).")]
        public SoundData randomPitchSfx;

        [Tooltip("Som de dura��o > 1s para testar fade-in percept�vel.")]
        public SoundData fadeInSfx;

        [Tooltip("Som para teste de stress (m�ltiplas inst�ncias). Pode reutilizar o basic.")]
        public SoundData stressSfx;

        [Header("BGM TEST SOUNDS (Etapa 5)")]
        [Tooltip("BGM de menu principal.")]
        public SoundData mainMenuBgm;

        [Tooltip("BGM de gameplay.")]
        public SoundData gameplayBgm;

        [Header("Execu��o Autom�tica")]
        [Tooltip("Se verdadeiro, roda o cen�rio de SFX automaticamente no Start.")]
        public bool autoRunSfxOnStart = true;

        [Tooltip("Se verdadeiro, roda o cen�rio de BGM automaticamente no Start (ap�s SFX).")]
        public bool autoRunBgmOnStart = true;

        [Tooltip("Delay padr�o entre passos de teste (em segundos).")]
        public float stepDelay = 1.0f;

        [Tooltip("Delay extra entre grupos de testes (em segundos).")]
        public float blockDelay = 1.5f;

        private IAudioSfxService _sfxService;
        private IBgmAudioService _bgmAudioService;

        private Coroutine _runningSfxScenario;
        private Coroutine _runningBgmScenario;

        private void Awake()
        {
            // Garante que o sistema de �udio est� inicializado e registrado no DI
            AudioSystemBootstrap.EnsureAudioSystemInitialized();

            if (DependencyManager.Provider != null)
            {
                DependencyManager.Provider.TryGetGlobal(out _sfxService);
                DependencyManager.Provider.TryGetGlobal(out _bgmAudioService);
            }

            if (_sfxService == null)
            {
                Debug.LogWarning("[AudioTest] IAudioSfxService n�o encontrado. Verifique AudioSystemBootstrap / registro de servi�os.");
            }

            if (_bgmAudioService == null)
            {
                Debug.LogWarning("[AudioTest] IBgmAudioService (BGM) n�o encontrado. Testes de BGM ficar�o limitados.");
            }
        }

        private void Start()
        {
            Debug.Log("[AudioTest] AudioSystemScenarioTester inicializado.");
            Debug.Log("[AudioTest] Controles: T = rodar cen�rio SFX, Y = rodar cen�rio BGM.");

            if (autoRunSfxOnStart)
            {
                _runningSfxScenario = StartCoroutine(RunSfxScenario());
            }

            if (autoRunBgmOnStart)
            {
                // roda ap�s um pequeno atraso para n�o conflitar com o in�cio dos SFX
                var delay = autoRunSfxOnStart ? (stepDelay * 8f) : 0f;
                _runningBgmScenario = StartCoroutine(RunBgmScenarioWithDelay(delay));
            }
        }

        private void Update()
        {
            // Triggers manuais para rer rodar cen�rios
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
                Debug.LogError("[AudioTest][SFX] IAudioSfxService indispon�vel, abortando cen�rio de SFX.");
                yield break;
            }

            Debug.Log("====================================================");
            Debug.Log("[AudioTest][SFX] Iniciando cen�rio de testes (Etapa 4).");
            Debug.Log("====================================================");

            var position = transform.position;

            yield return RunOneShotBasicTest();
            yield return RunSpatialVsNonSpatialTest(position);
            yield return RunRandomPitchTest(position);
            yield return RunFadeInTest(position);
            yield return RunStressTest(position);

            Debug.Log("====================================================");
            Debug.Log("[AudioTest][SFX] Cen�rio de SFX (Etapa 4) conclu�do.");
            Debug.Log("====================================================");
        }

        private IEnumerator RunOneShotBasicTest()
        {
            // 4.1 � Teste de one-shot b�sico
            if (basicSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.1 - OneShot b�sico (non-spatial).");
                var ctx = AudioContext.NonSpatial();
                _sfxService.PlayOneShot(basicSfx, ctx);
            }
            else
            {
                Debug.LogWarning("[AudioTest][SFX] 4.1 - basicSfx n�o configurado.");
            }

            yield return new WaitForSeconds(stepDelay);
        }

        private IEnumerator RunSpatialVsNonSpatialTest(Vector3 position)
        {
            // 4.2 � Spatial vs. Non-Spatial
            if (spatialSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.2 - Spatial SFX (posicione a c�mera em volta para perceber pan/volume).");
                var spatialCtx = AudioContext.Default(position, useSpatial: true);
                _sfxService.PlayOneShot(spatialSfx, spatialCtx);

                yield return new WaitForSeconds(stepDelay);

                Debug.Log("[AudioTest][SFX] 4.2 - Non-Spatial SFX (igual ao anterior, mas sem spatial).");
                var nonSpatialCtx = AudioContext.NonSpatial();
                _sfxService.PlayOneShot(spatialSfx, nonSpatialCtx);
            }
            else
            {
                Debug.LogWarning("[AudioTest][SFX] 4.2 - spatialSfx n�o configurado.");
            }

            yield return new WaitForSeconds(blockDelay);
        }

        private IEnumerator RunRandomPitchTest(Vector3 position)
        {
            // 4.3 � Random Pitch (m�ltiplas inst�ncias)
            if (randomPitchSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.3 - Random Pitch (10 inst�ncias r�pidas).");
                for (int i = 0; i < 10; i++)
                {
                    var ctx = AudioContext.Default(position, useSpatial: false, volMult: 1f);
                    _sfxService.PlayOneShot(randomPitchSfx, ctx);
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                Debug.LogWarning("[AudioTest][SFX] 4.3 - randomPitchSfx n�o configurado.");
            }

            yield return new WaitForSeconds(blockDelay);
        }

        private IEnumerator RunFadeInTest(Vector3 position)
        {
            // 4.4 � Fade-in
            if (fadeInSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.4 - Fade-in SFX (0.5s).");
                var ctx = AudioContext.Default(position, useSpatial: false);
                _sfxService.PlayOneShot(fadeInSfx, ctx, fadeInSeconds: 0.5f);
            }
            else
            {
                Debug.LogWarning("[AudioTest][SFX] 4.4 - fadeInSfx n�o configurado.");
            }

            yield return new WaitForSeconds(blockDelay);
        }

        private IEnumerator RunStressTest(Vector3 position)
        {
            // 4.5 � Stress test (m�ltiplos SFX em sequ�ncia)
            if (stressSfx != null)
            {
                Debug.Log("[AudioTest][SFX] 4.5 - Stress test (30 inst�ncias r�pidas).");
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
                Debug.LogWarning("[AudioTest][SFX] 4.5 - stressSfx n�o configurado.");
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
                Debug.LogError("[AudioTest][BGM] IBgmAudioService indispon�vel, abortando cen�rio de BGM.");
                yield break;
            }

            Debug.Log("====================================================");
            Debug.Log("[AudioTest][BGM] Iniciando cen�rio de testes (Etapa 5).");
            Debug.Log("====================================================");

            // 5.1 � Play/Stop BGM de menu
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
                Debug.LogWarning("[AudioTest][BGM] 5.1 - mainMenuBgm n�o configurado.");
            }

            yield return new WaitForSeconds(blockDelay);

            // 5.2 � Troca de BGM (menu -> gameplay)
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
                Debug.LogWarning("[AudioTest][BGM] 5.2 - gameplayBgm n�o configurado.");
            }

            yield return new WaitForSeconds(blockDelay);

            // 5.3 � Teste simples de volume BGM
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
                Debug.LogWarning("[AudioTest][BGM] 5.3 - mainMenuBgm n�o configurado, pulando teste de volume.");
            }

            Debug.Log("====================================================");
            Debug.Log("[AudioTest][BGM] Cen�rio de BGM (Etapa 5) conclu�do.");
            Debug.Log("====================================================");
        }

        #endregion
    }
}

