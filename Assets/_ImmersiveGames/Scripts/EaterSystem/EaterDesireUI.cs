using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterDesireUI : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] private Image desireIcon;
        [SerializeField] private EaterBehavior eaterBehavior;
        [SerializeField, Tooltip("Sprite utilizada quando não houver desejo ativo ou quando o ícone do recurso estiver indisponível.")]
        private Sprite fallbackSprite;
        [SerializeField, Tooltip("Quando verdadeiro, oculta a imagem se não existir desejo ativo.")]
        private bool hideWhenNoDesire = true;

        private bool _subscribed;
        private bool _pendingIconResolve;
        private bool _warnedMissingIcon;
        private bool _warnedMissingBehavior;
        private bool _warnedMissingManager;
        private EaterDesireInfo _currentInfo = EaterDesireInfo.Inactive;

        private bool SharesGameObjectWithIcon =>
            desireIcon != null && desireIcon.gameObject == gameObject;

        private void OnEnable()
        {
            TryResolveBehavior();
            SubscribeToBehavior();
            ApplyCurrentInfo();
        }

        private void OnDisable()
        {
            UnsubscribeFromBehavior();
        }

        private void LateUpdate()
        {
            if (!_subscribed)
            {
                TryResolveBehavior();
                SubscribeToBehavior();
                if (_subscribed)
                {
                    ApplyCurrentInfo();
                }
            }

            if (_pendingIconResolve)
            {
                ApplyCurrentInfo();
            }
        }

        private void SubscribeToBehavior()
        {
            if (eaterBehavior == null)
            {
                if (!_warnedMissingBehavior)
                {
                    DebugUtility.LogWarning<EaterDesireUI>("EaterBehavior não encontrado para atualizar o HUD de desejos.", this);
                    _warnedMissingBehavior = true;
                }
                return;
            }

            if (_subscribed)
            {
                return;
            }

            eaterBehavior.EventDesireChanged += HandleDesireChanged;
            _currentInfo = eaterBehavior.GetCurrentDesireInfo();
            _subscribed = true;
            _warnedMissingBehavior = false;
        }

        private void UnsubscribeFromBehavior()
        {
            if (_subscribed && eaterBehavior != null)
            {
                eaterBehavior.EventDesireChanged -= HandleDesireChanged;
            }

            _subscribed = false;
        }

        private void HandleDesireChanged(EaterDesireInfo info)
        {
            _currentInfo = info;
            ApplyCurrentInfo();
        }

        private void ApplyCurrentInfo()
        {
            if (desireIcon == null)
            {
                if (!_warnedMissingIcon)
                {
                    DebugUtility.LogWarning<EaterDesireUI>("Image do ícone de desejo não configurada.", this);
                    _warnedMissingIcon = true;
                }
                return;
            }

            if (!_currentInfo.ServiceActive || !_currentInfo.HasDesire || !_currentInfo.HasResource)
            {
                ShowNoDesireState();
                return;
            }

            if (!TryGetResourceIcon(_currentInfo.Resource!.Value, out Sprite icon))
            {
                if (_pendingIconResolve)
                {
                    return;
                }

                UseFallbackIcon();
                return;
            }

            desireIcon.sprite = icon;
            SetIconVisibility(true);

            _pendingIconResolve = false;

            DebugUtility.LogVerbose<EaterDesireUI>(
                $"Ícone de desejo atualizado para {_currentInfo.Resource.Value} (disp={_currentInfo.IsAvailable}, planetas={_currentInfo.AvailableCount}).",
                null,
                this);
        }

        private void ShowNoDesireState()
        {
            _pendingIconResolve = false;

            if (desireIcon == null)
            {
                return;
            }

            if (hideWhenNoDesire)
            {
                SetIconVisibility(false);
            }
            else if (fallbackSprite != null)
            {
                desireIcon.sprite = fallbackSprite;
                SetIconVisibility(true);
            }
            else
            {
                SetIconVisibility(false);
            }

            DebugUtility.LogVerbose<EaterDesireUI>("Nenhum desejo ativo para exibir na UI.", null, this);
        }

        private void UseFallbackIcon()
        {
            if (desireIcon == null)
            {
                return;
            }

            if (fallbackSprite != null)
            {
                desireIcon.sprite = fallbackSprite;
                SetIconVisibility(true);

                DebugUtility.LogWarning<EaterDesireUI>(
                    _currentInfo.HasResource
                        ? $"Ícone específico para {_currentInfo.Resource.Value} indisponível. Utilizando fallback."
                        : "Ícone de desejo indisponível. Utilizando fallback.",
                    this);
            }
            else
            {
                SetIconVisibility(false);
                DebugUtility.LogWarning<EaterDesireUI>(
                    _currentInfo.HasResource
                        ? $"Ícone específico para {_currentInfo.Resource.Value} não encontrado e nenhum fallback foi configurado."
                        : "Ícone de desejo não encontrado e nenhum fallback foi configurado.",
                    this);
            }
        }

        private void SetIconVisibility(bool visible)
        {
            if (desireIcon == null)
            {
                return;
            }

            GameObject iconObject = desireIcon.gameObject;
            CanvasRenderer renderer = desireIcon.canvasRenderer;

            if (visible)
            {
                if (!iconObject.activeSelf)
                {
                    iconObject.SetActive(true);
                }

                if (!desireIcon.enabled)
                {
                    desireIcon.enabled = true;
                }

                if (renderer != null)
                {
                    renderer.SetAlpha(1f);
                }

                return;
            }

            if (hideWhenNoDesire && !SharesGameObjectWithIcon)
            {
                if (iconObject.activeSelf)
                {
                    iconObject.SetActive(false);
                }

                return;
            }

            if (!iconObject.activeSelf)
            {
                iconObject.SetActive(true);
            }

            if (desireIcon.enabled)
            {
                desireIcon.enabled = false;
            }

            if (renderer != null)
            {
                renderer.SetAlpha(0f);
            }
        }

        private bool TryGetResourceIcon(PlanetResources resource, out Sprite icon)
        {
            icon = null;

            PlanetsManager manager = PlanetsManager.Instance;
            if (manager == null)
            {
                if (!_warnedMissingManager)
                {
                    DebugUtility.LogVerbose<EaterDesireUI>("PlanetsManager ainda não está disponível. Aguardando para resolver ícone de desejo.", null, this);
                    _warnedMissingManager = true;
                }

                _pendingIconResolve = true;
                return false;
            }

            _warnedMissingManager = false;
            _pendingIconResolve = false;

            if (!manager.TryGetResourceDefinition(resource, out PlanetResourcesSo definition) || definition == null)
            {
                DebugUtility.LogWarning<EaterDesireUI>($"Nenhuma definição encontrada para o recurso {resource}.", this);
                return false;
            }

            icon = definition.ResourceIcon;
            if (icon == null)
            {
                DebugUtility.LogWarning<EaterDesireUI>($"A definição do recurso {resource} não possui sprite configurado.", this);
                return false;
            }

            return true;
        }

        private void TryResolveBehavior()
        {
            if (eaterBehavior != null)
            {
                return;
            }

            Transform eaterTransform = GameManager.Instance != null ? GameManager.Instance.WorldEater : null;
            if (eaterTransform != null)
            {
                if (!eaterTransform.TryGetComponent(out eaterBehavior))
                {
                    eaterBehavior = eaterTransform.GetComponentInChildren<EaterBehavior>(true);
                }
            }

            if (eaterBehavior == null)
            {
                eaterBehavior = FindFirstObjectByType<EaterBehavior>();
            }

            if (eaterBehavior != null)
            {
                DebugUtility.LogVerbose<EaterDesireUI>($"EaterBehavior localizado para UI: {eaterBehavior.name}.", null, this);
            }
        }
    }
}

