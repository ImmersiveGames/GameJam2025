using System;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _ImmersiveGames.NewScripts.UI
{
    /// <summary>
    /// Base comum para binders de botões do Frontend.
    /// - Não registra listeners via código: use OnClick() no Inspector.
    /// - Fornece click-guard (anti auto submit), opcionalmente limpa seleção do EventSystem,
    ///   e (opcional) desabilita o botão durante a ação.
    ///
    /// Sem coroutines: qualquer "fallback" deve ser resolvido por evento real (quando aplicável)
    /// ou simplesmente não desabilitando o botão (recomendado).
    /// </summary>
    public abstract class FrontendButtonBinderBase : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] protected Button button;

        [Header("Config")]
        [SerializeField] protected string reason = "Frontend/Button";

        [Header("Safety")]
        [Tooltip("Ignora cliques por X segundos ao habilitar (mitiga Submit/Enter preso ao entrar/voltar no Frontend).")]
        [SerializeField] private float ignoreClicksForSecondsAfterEnable = 0.25f;

        [Tooltip("Quando true, limpa o Selected do EventSystem ao habilitar (mitiga Submit automático).")]
        [SerializeField] private bool clearEventSystemSelectionOnEnable = true;

        [Tooltip("Quando true, desabilita o botão ao iniciar a ação. " +
                 "Se a ação falhar para iniciar (ex.: serviço indisponível), o botão é reabilitado automaticamente.")]
        [SerializeField] private bool disableButtonDuringAction;

        private float _ignoreClicksUntilUnscaledTime;
        private bool _clickGuardArmedThisEnable;

        protected virtual void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button == null)
            {
                DebugUtility.LogWarning<FrontendButtonBinderBase>(
                    "[FrontendButton] Button não atribuído e GetComponent<Button>() falhou. OnClick() (Inspector) pode não funcionar.");
            }

            // IMPORTANT: Não armar click-guard no Awake.
            // O cooldown deve ser calculado no momento do OnEnable, para não expirar antes do menu ficar interagível.
        }

        protected virtual void OnEnable()
        {
            ArmClickGuardOncePerEnable(ignoreClicksForSecondsAfterEnable, "OnEnable/Guard");

            if (clearEventSystemSelectionOnEnable)
            {
                TryClearEventSystemSelection();
            }
        }

        protected virtual void OnDisable()
        {
            _clickGuardArmedThisEnable = false;
        }

        /// <summary>
        /// Deve ser associado no Button.OnClick() no Inspector.
        /// </summary>
        public void OnClick()
        {
            if (Time.unscaledTime < _ignoreClicksUntilUnscaledTime)
            {
                DebugUtility.LogVerbose<FrontendButtonBinderBase>(
                    $"[FrontendButton] Clique ignorado (cooldown). remaining={(_ignoreClicksUntilUnscaledTime - Time.unscaledTime):0.000}s",
                    DebugUtility.Colors.Warning);
                return;
            }

            if (button != null && !button.interactable)
            {
                return;
            }

            if (disableButtonDuringAction)
            {
                SetButtonInteractable(false, "ActionStarted");
            }

            bool started;

            try
            {
                started = OnClickCore(reason);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<FrontendButtonBinderBase>(
                    $"[FrontendButton] Exceção ao executar ação. reason='{reason}'. ex='{ex.GetType().Name}: {ex.Message}'");
                started = false;
            }

            // Sem coroutines: se não iniciou, reabilita imediatamente para não travar UI.
            if (disableButtonDuringAction && !started)
            {
                SetButtonInteractable(true, "ActionNotStarted");
            }
        }

        /// <summary>
        /// Retorne true quando a ação foi iniciada com sucesso.
        /// Retorne false quando não foi possível iniciar (ex.: serviço nulo), para evitar UI travada.
        /// </summary>
        protected abstract bool OnClickCore(string actionReason);

        private void ArmClickGuard(float seconds, string label)
        {
            if (seconds <= 0f)
            {
                return;
            }

            _ignoreClicksUntilUnscaledTime = Mathf.Max(_ignoreClicksUntilUnscaledTime, Time.unscaledTime + seconds);
            string buttonName = button != null ? button.name : "<null>";

            DebugUtility.LogVerbose<FrontendButtonBinderBase>(
                $"[FrontendButton] Click-guard armado por {seconds:0.000}s (label='{label}', go='{gameObject.name}', btn='{buttonName}').",
                DebugUtility.Colors.Info,
                context: this);
        }

        private void ArmClickGuardOncePerEnable(float seconds, string label)
        {
            if (_clickGuardArmedThisEnable)
            {
                return;
            }

            _clickGuardArmedThisEnable = true;
            ArmClickGuard(seconds, label);
        }

        private void TryClearEventSystemSelection()
        {
            var es = EventSystem.current;
            if (es == null)
            {
                return;
            }

            if (button != null && es.currentSelectedGameObject == button.gameObject)
            {
                es.SetSelectedGameObject(null);

                DebugUtility.LogVerbose<FrontendButtonBinderBase>(
                    "[FrontendButton] EventSystem selection limpa (evitar Submit automático).",
                    DebugUtility.Colors.Info);
            }
        }

        private void SetButtonInteractable(bool value, string reasonLabel)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = value;

            DebugUtility.LogVerbose<FrontendButtonBinderBase>(
                $"[FrontendButton] Button interactable={(value ? "ON" : "OFF")} (reason='{reasonLabel}').",
                DebugUtility.Colors.Info);
        }
    }
}
