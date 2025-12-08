using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.Scripts.FadeSystem
{
    /// <summary>
    /// Implementação padrão do IFadeService usando apenas Task/async-await.
    /// - Carrega a FadeScene uma única vez (lazy ou via PreloadAsync).
    /// - Instancia um FadeController persistente (DontDestroyOnLoad).
    /// - Sincroniza chamadas concorrentes com SemaphoreSlim.
    /// </summary>
    public class FadeService : IFadeService
    {
        private const string FadeSceneName = "FadeScene";

        private FadeController _fadeController;
        private readonly SemaphoreSlim _fadeLock = new SemaphoreSlim(1, 1);
        private Task _initializationTask;

        private readonly EventBinding<FadeInRequestedEvent> _fadeInBinding;
        private readonly EventBinding<FadeOutRequestedEvent> _fadeOutBinding;

        // --- Integração com SceneTransitionProfile (Fase 2) ---

        private SceneTransitionProfile _currentProfile;

        private bool _defaultsCaptured;
        private float _defaultFadeInDuration;
        private float _defaultFadeOutDuration;
        private AnimationCurve _defaultFadeInCurve;
        private AnimationCurve _defaultFadeOutCurve;

        private static FieldInfo _fiFadeInDuration;
        private static FieldInfo _fiFadeOutDuration;
        private static FieldInfo _fiFadeInCurve;
        private static FieldInfo _fiFadeOutCurve;

        public FadeService()
        {
            _fadeInBinding = new EventBinding<FadeInRequestedEvent>(_ => RequestFadeIn());
            _fadeOutBinding = new EventBinding<FadeOutRequestedEvent>(_ => RequestFadeOut());

            EventBus<FadeInRequestedEvent>.Register(_fadeInBinding);
            EventBus<FadeOutRequestedEvent>.Register(_fadeOutBinding);
        }

        ~FadeService()
        {
            EventBus<FadeInRequestedEvent>.Unregister(_fadeInBinding);
            EventBus<FadeOutRequestedEvent>.Unregister(_fadeOutBinding);
        }

        #region IFadeService (API pública)

        public void RequestFadeIn()
        {
            // Fire-and-forget para código legado baseado em eventos ou input.
            _ = FadeInAsync();
        }

        public void RequestFadeOut()
        {
            _ = FadeOutAsync();
        }

        public Task FadeInAsync() => RunFadeAsync(1f);
        public Task FadeOutAsync() => RunFadeAsync(0f);

        #endregion

        /// <summary>
        /// Método explícito para pré-carregar a FadeScene e inicializar o FadeController,
        /// sem executar nenhum fade. Usado pelo bootstrap para evitar custo na primeira transição.
        /// </summary>
        public Task PreloadAsync()
        {
            return EnsureInitializedAsync();
        }

        /// <summary>
        /// Configura o serviço de Fade com base em um SceneTransitionProfile.
        /// - Se profile for null, o serviço volta para os valores padrão do FadeController.
        /// - Se profile.UseFade = false, as durações/curvas voltam para os defaults (o SceneTransitionService já decide se usa fade ou não).
        /// </summary>
        public void ConfigureFromProfile(SceneTransitionProfile profile)
        {
            _currentProfile = profile;
        }

        #region Internals

        private async Task RunFadeAsync(float targetAlpha)
        {
            await _fadeLock.WaitAsync();
            try
            {
                await EnsureInitializedAsync();

                if (_fadeController != null)
                {
                    // Aplica (ou restaura) os parâmetros de fade com base no perfil atual.
                    ApplyProfileToControllerIfNeeded();

                    if (targetAlpha >= 1f - 0.0001f)
                    {
                        await _fadeController.FadeInAsync();
                    }
                    else if (targetAlpha <= 0f + 0.0001f)
                    {
                        await _fadeController.FadeOutAsync();
                    }
                    else
                    {
                        await _fadeController.FadeToAsync(targetAlpha);
                    }
                }
                else
                {
                    Debug.LogWarning("[FadeService] Fade solicitado, mas FadeController é nulo.");
                }
            }
            finally
            {
                _fadeLock.Release();
            }
        }

        private async Task EnsureInitializedAsync()
        {
            if (_fadeController != null)
                return;

            if (_initializationTask != null)
            {
                await _initializationTask;
                return;
            }

            _initializationTask = InitializeControllerAsync();
            await _initializationTask;
        }

        private async Task InitializeControllerAsync()
        {
            Debug.Log("[FadeService] Carregando FadeScene para inicialização do fade.");

            AsyncOperation loadOp;
            try
            {
                loadOp = SceneManager.LoadSceneAsync(FadeSceneName, LoadSceneMode.Additive);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FadeService] Erro ao iniciar load da cena '{FadeSceneName}': {e}");
                return;
            }

            if (loadOp == null)
            {
                Debug.LogError($"[FadeService] LoadSceneAsync retornou null para '{FadeSceneName}'.");
                return;
            }

            while (!loadOp.isDone)
                await Task.Yield();

            var scene = SceneManager.GetSceneByName(FadeSceneName);
            if (!scene.IsValid())
            {
                Debug.LogError("[FadeService] Cena FadeScene não encontrada ou inválida.");
                return;
            }

            FadeController prefabController = null;
            GameObject prefabRoot = null;

            foreach (var root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<FadeController>(true);
                if (controller != null)
                {
                    prefabController = controller;
                    prefabRoot = controller.gameObject;
                    break;
                }
            }

            if (prefabController == null || prefabRoot == null)
            {
                Debug.LogError("[FadeService] FadeController não encontrado na FadeScene.");
                return;
            }

            var instance = Object.Instantiate(prefabRoot);
            Object.DontDestroyOnLoad(instance);

            _fadeController = instance.GetComponent<FadeController>();
            if (_fadeController == null)
            {
                Debug.LogError("[FadeService] FadeController do clone persistente é nulo.");
            }
            else
            {
                Debug.Log("[FadeService] FadeController persistente inicializado com sucesso.");
            }

            Debug.Log("[FadeService] Descarregando FadeScene container após inicialização.");
            try
            {
                var unloadOp = SceneManager.UnloadSceneAsync(scene);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone)
                        await Task.Yield();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FadeService] Erro ao descarregar FadeScene: {e}");
            }
        }

        /// <summary>
        /// Usa reflexão para capturar os valores default do FadeController na primeira vez
        /// e, a cada chamada, aplica overrides vindos do SceneTransitionProfile (se houver).
        /// </summary>
        private void ApplyProfileToControllerIfNeeded()
        {
            if (_fadeController == null)
                return;

            EnsureFieldInfos();

            if (!_defaultsCaptured)
            {
                _defaultFadeInDuration = ReadFloatField(_fiFadeInDuration, _fadeController, 0.5f);
                _defaultFadeOutDuration = ReadFloatField(_fiFadeOutDuration, _fadeController, 0.5f);
                _defaultFadeInCurve = ReadCurveField(_fiFadeInCurve, _fadeController) ?? AnimationCurve.Linear(0, 0, 1, 1);
                _defaultFadeOutCurve = ReadCurveField(_fiFadeOutCurve, _fadeController) ?? AnimationCurve.Linear(0, 0, 1, 1);

                _defaultsCaptured = true;
            }

            // Se não há perfil, volta para defaults.
            if (_currentProfile == null)
            {
                WriteFloatField(_fiFadeInDuration, _fadeController, _defaultFadeInDuration);
                WriteFloatField(_fiFadeOutDuration, _fadeController, _defaultFadeOutDuration);
                WriteCurveField(_fiFadeInCurve, _fadeController, _defaultFadeInCurve);
                WriteCurveField(_fiFadeOutCurve, _fadeController, _defaultFadeOutCurve);
                return;
            }

            // Se o perfil explicitamente desabilita fade, voltamos para defaults.
            if (!_currentProfile.UseFade)
            {
                WriteFloatField(_fiFadeInDuration, _fadeController, _defaultFadeInDuration);
                WriteFloatField(_fiFadeOutDuration, _fadeController, _defaultFadeOutDuration);
                WriteCurveField(_fiFadeInCurve, _fadeController, _defaultFadeInCurve);
                WriteCurveField(_fiFadeOutCurve, _fadeController, _defaultFadeOutCurve);
                return;
            }

            // Aplica overrides se fornecidos; caso contrário, usa defaults.
            float fadeInDuration = _currentProfile.FadeInDuration > 0f
                ? _currentProfile.FadeInDuration
                : _defaultFadeInDuration;

            float fadeOutDuration = _currentProfile.FadeOutDuration > 0f
                ? _currentProfile.FadeOutDuration
                : _defaultFadeOutDuration;

            var fadeInCurve = _currentProfile.FadeInCurve != null
                ? _currentProfile.FadeInCurve
                : _defaultFadeInCurve;

            var fadeOutCurve = _currentProfile.FadeOutCurve != null
                ? _currentProfile.FadeOutCurve
                : _defaultFadeOutCurve;

            WriteFloatField(_fiFadeInDuration, _fadeController, fadeInDuration);
            WriteFloatField(_fiFadeOutDuration, _fadeController, fadeOutDuration);
            WriteCurveField(_fiFadeInCurve, _fadeController, fadeInCurve);
            WriteCurveField(_fiFadeOutCurve, _fadeController, fadeOutCurve);
        }

        private static void EnsureFieldInfos()
        {
            if (_fiFadeInDuration != null) return;

            var t = typeof(FadeController);
            _fiFadeInDuration = t.GetField("fadeInDuration", BindingFlags.Instance | BindingFlags.NonPublic);
            _fiFadeOutDuration = t.GetField("fadeOutDuration", BindingFlags.Instance | BindingFlags.NonPublic);
            _fiFadeInCurve = t.GetField("fadeInCurve", BindingFlags.Instance | BindingFlags.NonPublic);
            _fiFadeOutCurve = t.GetField("fadeOutCurve", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static float ReadFloatField(FieldInfo fi, object target, float fallback)
        {
            if (fi == null || target == null) return fallback;
            var value = fi.GetValue(target);
            return value is float f ? f : fallback;
        }

        private static AnimationCurve ReadCurveField(FieldInfo fi, object target)
        {
            if (fi == null || target == null) return null;
            return fi.GetValue(target) as AnimationCurve;
        }

        private static void WriteFloatField(FieldInfo fi, object target, float value)
        {
            if (fi == null || target == null) return;
            fi.SetValue(target, value);
        }

        private static void WriteCurveField(FieldInfo fi, object target, AnimationCurve curve)
        {
            if (fi == null || target == null) return;
            fi.SetValue(target, curve);
        }

        #endregion
    }
}
