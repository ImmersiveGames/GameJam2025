using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.DI;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Serviço global para orquestrar HUD de loading durante o Scene Flow.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneFlowLoadingService
    {
        private readonly EventBinding<SceneTransitionStartedEvent> _transitionStartedBinding;
        private readonly EventBinding<SceneTransitionScenesReadyEvent> _transitionScenesReadyBinding;
        private readonly EventBinding<SceneTransitionBeforeFadeOutEvent> _transitionBeforeFadeOutBinding;
        private readonly EventBinding<SceneTransitionCompletedEvent> _transitionCompletedBinding;
        private readonly EventBinding<SceneLoadingHudRegisteredEvent> _hudRegisteredBinding;
        private readonly EventBinding<SceneLoadingHudUnregisteredEvent> _hudUnregisteredBinding;

        private ISceneLoadingHud _hud;
        private bool _isLoading;
        private string _activeSignature;
        private string _pendingSignature;
        private PendingStage _pendingStage;
        private string _currentTitle;
        private string _currentDetails;
        private float _currentProgress01;

        public SceneFlowLoadingService()
        {
            _transitionStartedBinding = new EventBinding<SceneTransitionStartedEvent>(OnTransitionStarted);
            _transitionScenesReadyBinding = new EventBinding<SceneTransitionScenesReadyEvent>(OnTransitionScenesReady);
            _transitionBeforeFadeOutBinding = new EventBinding<SceneTransitionBeforeFadeOutEvent>(OnTransitionBeforeFadeOut);
            _transitionCompletedBinding = new EventBinding<SceneTransitionCompletedEvent>(OnTransitionCompleted);
            _hudRegisteredBinding = new EventBinding<SceneLoadingHudRegisteredEvent>(OnHudRegistered);
            _hudUnregisteredBinding = new EventBinding<SceneLoadingHudUnregisteredEvent>(OnHudUnregistered);

            EventBus<SceneTransitionStartedEvent>.Register(_transitionStartedBinding);
            EventBus<SceneTransitionScenesReadyEvent>.Register(_transitionScenesReadyBinding);
            EventBus<SceneTransitionBeforeFadeOutEvent>.Register(_transitionBeforeFadeOutBinding);
            EventBus<SceneTransitionCompletedEvent>.Register(_transitionCompletedBinding);
            EventBus<SceneLoadingHudRegisteredEvent>.Register(_hudRegisteredBinding);
            EventBus<SceneLoadingHudUnregisteredEvent>.Register(_hudUnregisteredBinding);

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                "[Loading] SceneFlowLoadingService registrado nos eventos de Scene Flow.");
        }

        public void AttachHud(ISceneLoadingHud hud)
        {
            if (hud == null)
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    "[Loading] AttachHud chamado com referência nula. Ignorando.");
                return;
            }

            if (!IsHudReferenceValid(hud))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    "[Loading] AttachHud recebeu referência destruída. Ignorando.");
                return;
            }

            _hud = hud;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                "[Loading] HUD anexado ao SceneFlowLoadingService.");

            ApplyPendingState("attach");
        }

        private void OnTransitionStarted(SceneTransitionStartedEvent evt)
        {
            var signature = evt.Context.ToString();

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] Nova transição detectada. Substituindo pendências. old='{_pendingSignature}', new='{signature}'.");
            }

            _isLoading = true;
            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingStage = PendingStage.Started;

            UpdateHudState(
                title: "Carregando...",
                details: null,
                progress01: 0.1f);

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] SceneTransitionStarted → HUD loading ativo. signature='{signature}'.");

            ApplyPendingState("started");
        }

        private void OnTransitionScenesReady(SceneTransitionScenesReadyEvent evt)
        {
            var signature = evt.Context.ToString();

            if (!_isLoading)
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] SceneTransitionScenesReady recebido sem loading ativo. signature='{signature}'.");
            }

            if (IsSignatureMismatch(signature))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] ScenesReady com assinatura diferente. Substituindo pendências. old='{_pendingSignature}', new='{signature}'.");
            }

            _isLoading = true;
            _activeSignature = signature;
            _pendingSignature = signature;
            _pendingStage = PendingStage.ScenesReady;

            UpdateHudState(
                title: "Preparando...",
                details: null,
                progress01: 0.8f);

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] SceneTransitionScenesReady → HUD atualizado. signature='{signature}'.");

            ApplyPendingState("scenes_ready");
        }

        private void OnTransitionBeforeFadeOut(SceneTransitionBeforeFadeOutEvent evt)
        {
            var signature = evt.Context.ToString();
            if (!IsActiveSignatureMatch(signature))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] BeforeFadeOut ignorado (assinatura não corresponde). active='{_activeSignature}', incoming='{signature}'.");
                return;
            }

            _isLoading = false;
            ClearPending();

            SafeHide("before_fade_out");

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] SceneTransitionBeforeFadeOut → HUD oculto. signature='{signature}'.");
        }

        private void OnTransitionCompleted(SceneTransitionCompletedEvent evt)
        {
            var signature = evt.Context.ToString();
            if (!IsActiveSignatureMatch(signature))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] Completed ignorado (assinatura não corresponde). active='{_activeSignature}', incoming='{signature}'.");
                return;
            }

            _isLoading = false;
            ClearPending();

            SafeHide("completed");

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] SceneTransitionCompleted → HUD forçado a ocultar. signature='{signature}'.");
        }

        private void OnHudRegistered(SceneLoadingHudRegisteredEvent evt)
        {
            TryResolveHud();
            ApplyPendingState("hud_registered");
        }

        private void OnHudUnregistered(SceneLoadingHudUnregisteredEvent evt)
        {
            if (_hud == null)
            {
                return;
            }

            _hud = null;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                "[Loading] HUD desregistrado. Referência limpa no SceneFlowLoadingService.");
        }

        private void UpdateHudState(string title, string details, float progress01)
        {
            _currentTitle = title;
            _currentDetails = details;
            _currentProgress01 = Clamp01(progress01);
        }

        private void ApplyPendingState(string reason)
        {
            if (!_isLoading || _pendingStage == PendingStage.None)
            {
                return;
            }

            if (!TryEnsureHudAvailable())
            {
                DebugUtility.LogVerbose<SceneFlowLoadingService>(
                    $"[Loading] HUD ainda não registrado. Mantendo pendência='{_pendingStage}' (reason='{reason}').");
                return;
            }

            SafeShow(reason);
            SafeSetProgress(reason);

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                $"[Loading] Pendência aplicada após '{reason}'. stage='{_pendingStage}', signature='{_pendingSignature}'.");
        }

        private void SafeShow(string reason)
        {
            if (!TryEnsureHudAvailable())
            {
                return;
            }

            try
            {
                _hud.Show(_currentTitle, _currentDetails);
            }
            catch (MissingReferenceException)
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] HUD destruído durante Show (reason='{reason}'). Limpando referência.");
                _hud = null;
            }
        }

        private void SafeSetProgress(string reason)
        {
            if (!TryEnsureHudAvailable())
            {
                return;
            }

            try
            {
                _hud.SetProgress01(_currentProgress01);
            }
            catch (MissingReferenceException)
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] HUD destruído durante SetProgress (reason='{reason}'). Limpando referência.");
                _hud = null;
            }
        }

        private void SafeHide(string reason)
        {
            if (!TryEnsureHudAvailable())
            {
                DebugUtility.LogVerbose<SceneFlowLoadingService>(
                    $"[Loading] Hide ignorado: HUD indisponível (reason='{reason}').");
                return;
            }

            try
            {
                _hud.Hide();
            }
            catch (MissingReferenceException)
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    $"[Loading] HUD destruído durante Hide (reason='{reason}'). Limpando referência.");
                _hud = null;
            }
        }

        private bool TryEnsureHudAvailable()
        {
            if (IsHudReferenceValid(_hud))
            {
                return true;
            }

            if (_hud != null)
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    "[Loading] Referência do HUD inválida (provável destruction). Limpando.");
                _hud = null;
            }

            TryResolveHud();
            return IsHudReferenceValid(_hud);
        }

        private void TryResolveHud()
        {
            if (!DependencyManager.Provider.TryGetGlobal<ISceneLoadingHud>(out var hud) || hud == null)
            {
                DebugUtility.LogVerbose<SceneFlowLoadingService>(
                    "[Loading] HUD pronto sinalizado, porém ISceneLoadingHud não encontrado no DI global.");
                return;
            }

            if (!IsHudReferenceValid(hud))
            {
                DebugUtility.LogWarning<SceneFlowLoadingService>(
                    "[Loading] DI retornou HUD destruído. Mantendo pendência até nova instância.");
                return;
            }

            if (_hud == hud)
            {
                return;
            }

            _hud = hud;

            DebugUtility.LogVerbose<SceneFlowLoadingService>(
                "[Loading] ISceneLoadingHud resolvido via DI após HUD pronto.");
        }

        private static bool IsHudReferenceValid(ISceneLoadingHud hud)
        {
            if (hud == null)
            {
                return false;
            }

            if (hud is Object unityObject && unityObject == null)
            {
                return false;
            }

            return true;
        }

        private bool IsSignatureMismatch(string signature)
        {
            if (string.IsNullOrWhiteSpace(_pendingSignature))
            {
                return false;
            }

            return !string.Equals(_pendingSignature, signature);
        }

        private bool IsActiveSignatureMatch(string signature)
        {
            if (string.IsNullOrWhiteSpace(_activeSignature))
            {
                return false;
            }

            return string.Equals(_activeSignature, signature);
        }

        private void ClearPending()
        {
            _pendingSignature = null;
            _pendingStage = PendingStage.None;
            _activeSignature = null;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private enum PendingStage
        {
            None,
            Started,
            ScenesReady
        }
    }
}
