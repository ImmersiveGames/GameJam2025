using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Bindings;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Loading.Hud
{
    /// <summary>
    /// Servico global para garantir o carregamento da cena de HUD de loading (Additive).
    ///
    /// Contrato (ADR-0010):
    /// - Strict (Editor/Dev): scene_not_in_build deve ser evidente (log de erro + Break).
    /// - Strict: controller_missing/controller_not_found degradam com erro (sem exception).
    /// - Release: falha degrada com aviso (DEGRADED_MODE) e o jogo continua sem HUD.
    ///
    /// Observacao:
    /// - A cena precisa existir em Build Settings e conter 1x LoadingHudController.
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

            DebugUtility.LogVerbose<LoadingHudService>(
                $"[LoadingHudEnsure] signature='{signature}'.",
                DebugUtility.Colors.Info);

            // Validacao sincrona (Strict): cena precisa existir no Build Settings.
            if (_runtime != null && _runtime.IsStrict)
            {
                if (!Application.CanStreamedLevelBeLoaded(LoadingHudSceneName))
                {
                    _disabled = true;
                    FailStrict(
                        reason: "scene_not_in_build",
                        detail: $"Scene '{LoadingHudSceneName}' nao esta no Build Settings.",
                        signature: signature);

                    // Importante: FailStrict lanca, e aqui estamos antes do primeiro await (fail-fast).
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
                    detail: "Show ignorado: controller indisponivel.",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);

                return;
            }

            try
            {
                _controller.Show(phase);
            }
            catch (Exception ex)
            {
                _controller = null;
                FailOrDegrade(
                    reason: "controller_exception",
                    detail: $"Show falhou: {ex.Message}",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);
                return;
            }

            DebugUtility.LogVerbose<LoadingHudService>(
                $"[LoadingHudShow] signature='{signature}' phase='{phase}'.",
                DebugUtility.Colors.Info);
        }

        public void Hide(string signature, string phase)
        {
            if (_disabled)
            {
                return;
            }

            if (!TryEnsureController(signature))
            {
                // Hide e safety: em Release, so logamos uma vez.
                FailOrDegrade(
                    reason: "controller_missing",
                    detail: "Hide ignorado: controller indisponivel.",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);

                return;
            }

            try
            {
                _controller.Hide(phase);
            }
            catch (Exception ex)
            {
                _controller = null;
                FailOrDegrade(
                    reason: "controller_exception",
                    detail: $"Hide falhou: {ex.Message}",
                    signature: signature,
                    phase: phase,
                    allowStrictThrow: false);
                return;
            }

            DebugUtility.LogVerbose<LoadingHudService>(
                $"[LoadingHudHide] signature='{signature}' phase='{phase}'.",
                DebugUtility.Colors.Info);
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
                    if (!await EnsureSceneLoadedIfNeededAsync(signature))
                    {
                        return;
                    }

                    if (!await TryResolveControllerWithRetriesAsync())
                    {
                        FailOrDegrade(
                            reason: "controller_not_found",
                            detail: $"Nenhum {nameof(LoadingHudController)} encontrado na cena '{LoadingHudSceneName}'.",
                            signature: signature,
                            allowStrictThrow: false);

                        return;
                    }
                }
                catch (Exception ex)
                {
                    FailOrDegrade(
                        reason: "exception",
                        detail: $"Falha ao garantir LoadingHudScene/controller. ({ex.Message})",
                        signature: signature,
                        allowStrictThrow: false);
                }
            }
            finally
            {
                _ensureGate.Release();
            }
        }

        private async Task<bool> EnsureSceneLoadedIfNeededAsync(string signature)
        {
            var scene = SceneManager.GetSceneByName(LoadingHudSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                return true;
            }

            var loadOp = SceneManager.LoadSceneAsync(LoadingHudSceneName, LoadSceneMode.Additive);
            if (loadOp == null)
            {
                FailOrDegrade(
                    reason: "load_op_null",
                    detail: $"LoadSceneAsync retornou null para '{LoadingHudSceneName}'. Verifique Build Settings.",
                    signature: signature,
                    allowStrictThrow: false);

                return false;
            }

            while (!loadOp.isDone)
            {
                await Task.Yield();
            }

            return true;
        }

        private async Task<bool> TryResolveControllerWithRetriesAsync()
        {
            // Resolve controller com algumas tentativas (um frame pode nao ser suficiente).
            for (int i = 0; i < DefaultResolveFrames; i++)
            {
                TryResolveController();
                if (IsControllerValid(_controller))
                {
                    return true;
                }

                await Task.Yield();
            }

            return IsControllerValid(_controller);
        }

        private bool TryEnsureController(string signature)
        {
            _ = signature;
            if (IsControllerValid(_controller))
            {
                return true;
            }

            TryResolveController();

            return IsControllerValid(_controller);
        }

        private void TryResolveController()
        {
            _controller = Object.FindAnyObjectByType<LoadingHudController>();
        }

        private static bool IsControllerValid(LoadingHudController controller)
        {
            // Comentario: usa o operador de Unity para tratar objetos destruidos como null.
            return controller != null;
        }

        private void FailOrDegrade(
            string reason,
            string detail,
            string signature,
            string phase = null,
            bool allowDisable = true,
            bool allowStrictThrow = false)
        {
            // Strict: erro visivel, mas sem quebrar o fluxo de transicao.
            if (_runtime != null && _runtime.IsStrict)
            {
                if (allowStrictThrow)
                {
                    FailStrict(reason, detail, signature, phase);
                    return;
                }

                DebugUtility.LogError<LoadingHudService>(
                    $"[LoadingHUD][STRICT] reason='{reason}' signature='{signature}' phase='{phase}'. {detail}");
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
                $"[LoadingDegraded] reason='{reason}', signature='{signature}', phase='{phase}'. {detail}");
        }

        private static void FailStrict(string reason, string detail, string signature, string phase = null)
        {
            DebugUtility.LogError<LoadingHudService>(
                $"[LoadingHUD][STRICT] reason='{reason}' signature='{signature}' phase='{phase}'. {detail}");

            // Comentario: em Editor/Dev, queremos um sinal gritante.
            Debug.Break();

            throw new InvalidOperationException(
                $"LoadingHUD strict failure: {reason}. {detail} (signature='{signature}', phase='{phase}')");
        }
    }
}
