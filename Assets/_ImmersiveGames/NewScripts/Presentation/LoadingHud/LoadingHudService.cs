using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Runtime.Mode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Presentation.LoadingHud
{
    /// <summary>
    /// Serviço global para garantir o carregamento da cena de HUD de loading (Additive).
    ///
    /// Contrato (ADR-0010):
    /// - Strict (Editor/Dev): falha deve ser evidente (log de erro + Break).
    /// - Release: falha degrada com aviso (DEGRADED_MODE) e o jogo continua sem HUD.
    ///
    /// Observação:
    /// - A cena precisa existir em Build Settings e conter 1x SceneFlowLoadingHudDriver.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LoadingHudService : ILoadingHudService
    {
        private const string FeatureName = "loadinghud";
        private const string LoadingHudSceneName = "LoadingHudScene";

        private const int DefaultResolveFrames = 60;

        private readonly IRuntimeModeProvider _runtime;
        private readonly IDegradedModeReporter _degraded;

        private readonly SemaphoreSlim _ensureGate = new(1, 1);

        private LoadingHudController _controller;
        private bool _disabled;

        private bool _isVisible;
        private string _lastSignature;
        private string _lastPhase;

        public LoadingHudService(IRuntimeModeProvider runtimeModeProvider, IDegradedModeReporter degradedModeReporter)
        {
            _runtime = runtimeModeProvider;
            _degraded = degradedModeReporter;
        }

        public Task EnsureLoadedAsync(string signature)
        {
            if (_disabled)
            {
                return Task.CompletedTask;
            }

            // Validação síncrona (Strict): cena precisa existir no Build Settings.
            if (_runtime != null && _runtime.IsStrict)
            {
                if (!Application.CanStreamedLevelBeLoaded(LoadingHudSceneName))
                {
                    FailStrict(
                        reason: "scene_not_in_build",
                        detail: $"Scene '{LoadingHudSceneName}' não está no Build Settings.",
                        signature: signature);

                    // Importante: FailStrict lança, e aqui estamos antes do primeiro await (fail-fast).
                    return Task.CompletedTask;
                }
            }

            return EnsureLoadedInternalAsync(signature);
        }

        public void Show(string signature, string phase)
        {
            if (_disabled)
            {
                return;
            }

            if (!TryEnsureController(signature))
            {
                FailOrDegrade(
                    reason: "controller_missing",
                    detail: "Show ignorado: controller indisponível.",
                    signature: signature,
                    phase: phase);

                return;
            }

            // Idempotência + redução de spam.
            if (_isVisible && string.Equals(_lastSignature, signature) && string.Equals(_lastPhase, phase))
            {
                return;
            }

            _controller.Show(phase);

            _isVisible = true;
            _lastSignature = signature;
            _lastPhase = phase;

            DebugUtility.LogVerbose<LoadingHudService>(
                $"[OBS][LoadingHUD] ShowApplied signature='{signature}' phase='{phase}'.");
        }

        public void Hide(string signature, string phase)
        {
            if (_disabled)
            {
                return;
            }

            if (!TryEnsureController(signature))
            {
                // Hide é safety: em Release, só logamos uma vez.
                FailOrDegrade(
                    reason: "controller_missing",
                    detail: "Hide ignorado: controller indisponível.",
                    signature: signature,
                    phase: phase,
                    allowDisable: false);

                return;
            }

            if (!_isVisible && string.Equals(_lastSignature, signature))
            {
                return;
            }

            _controller.Hide(phase);

            _isVisible = false;
            _lastSignature = signature;
            _lastPhase = phase;

            DebugUtility.LogVerbose<LoadingHudService>(
                $"[OBS][LoadingHUD] HideApplied signature='{signature}' phase='{phase}'.");
        }

        private async Task EnsureLoadedInternalAsync(string signature)
        {
            if (IsControllerValid(_controller))
            {
                return;
            }

            await _ensureGate.WaitAsync();
            try
            {
                if (_disabled)
                {
                    return;
                }

                if (IsControllerValid(_controller))
                {
                    return;
                }

                try
                {
                    var scene = SceneManager.GetSceneByName(LoadingHudSceneName);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        DebugUtility.LogVerbose<LoadingHudService>(
                            $"[OBS][LoadingHUD] EnsureLoadScene signature='{signature}' scene='{LoadingHudSceneName}' mode='Additive'.");

                        var loadOp = SceneManager.LoadSceneAsync(LoadingHudSceneName, LoadSceneMode.Additive);
                        if (loadOp == null)
                        {
                            FailOrDegrade(
                                reason: "load_op_null",
                                detail: $"LoadSceneAsync retornou null para '{LoadingHudSceneName}'. Verifique Build Settings.",
                                signature: signature);

                            return;
                        }

                        while (!loadOp.isDone)
                        {
                            await Task.Yield();
                        }
                    }

                    // Resolve controller com algumas tentativas (um frame pode não ser suficiente).
                    for (int i = 0; i < DefaultResolveFrames; i++)
                    {
                        TryResolveController();
                        if (IsControllerValid(_controller))
                        {
                            break;
                        }

                        await Task.Yield();
                    }

                    if (!IsControllerValid(_controller))
                    {
                        FailOrDegrade(
                            reason: "controller_not_found",
                            detail: $"Nenhum {nameof(LoadingHudController)} encontrado na cena '{LoadingHudSceneName}'.",
                            signature: signature);

                        return;
                    }

                    DebugUtility.LogVerbose<LoadingHudService>(
                        $"[OBS][LoadingHUD] EnsureReady signature='{signature}' controller='{nameof(LoadingHudController)}'.");
                }
                catch (Exception ex)
                {
                    FailOrDegrade(
                        reason: "exception",
                        detail: $"Falha ao garantir LoadingHudScene/controller. ({ex.Message})",
                        signature: signature);
                }
            }
            finally
            {
                _ensureGate.Release();
            }
        }

        private bool TryEnsureController(string signature)
        {
            if (IsControllerValid(_controller))
            {
                return true;
            }

            TryResolveController();

            if (IsControllerValid(_controller))
            {
                return true;
            }

            // Pode existir uma corrida com EnsureLoadedAsync; em Release, não insistimos aqui.
            if (_runtime != null && _runtime.IsStrict)
            {
                FailStrict(
                    reason: "controller_missing",
                    detail: $"Controller não resolvido. Garanta que '{LoadingHudSceneName}' tenha 1x {nameof(LoadingHudController)}.",
                    signature: signature);
            }

            return false;
        }

        private void TryResolveController()
        {
            _controller = Object.FindAnyObjectByType<LoadingHudController>();
        }

        private static bool IsControllerValid(LoadingHudController controller)
        {
            // Comentário: usa o operador de Unity para tratar objetos destruídos como null.
            return controller != null;
        }

        private void FailOrDegrade(string reason, string detail, string signature, string phase = null, bool allowDisable = true)
        {
            // Strict: erro visível.
            if (_runtime != null && _runtime.IsStrict)
            {
                FailStrict(reason, detail, signature, phase);
                return;
            }

            // Release: reporta degraded uma vez e opcionalmente desabilita o HUD.
            if (allowDisable)
            {
                _disabled = true;
            }

            _degraded?.Report(
                feature: FeatureName,
                reason: reason,
                detail: detail,
                signature: signature,
                profile: null);

            DebugUtility.LogWarning<LoadingHudService>(
                $"[LoadingHUD] Degraded: reason='{reason}', signature='{signature}', phase='{phase}'. {detail}");
        }

        private static void FailStrict(string reason, string detail, string signature, string phase = null)
        {
            DebugUtility.LogError<LoadingHudService>(
                $"[LoadingHUD][STRICT] reason='{reason}' signature='{signature}' phase='{phase}'. {detail}");

            // Comentário: em Editor/Dev, queremos um sinal "gritante".
            Debug.Break();

            throw new InvalidOperationException(
                $"LoadingHUD strict failure: {reason}. {detail} (signature='{signature}', phase='{phase}')");
        }
    }
}
