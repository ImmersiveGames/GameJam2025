using _ImmersiveGames.Scripts.Utils.DebugSystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _ImmersiveGames.Scripts.ResourceSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class ResourceUISlot : MonoBehaviour, IResourceUISlot
    {
        [SerializeField] private string expectedActorId;
        [SerializeField] private ResourceType expectedType;
        
        [Header("UI Components")]
        [SerializeField] private Image fillImage;           // Barra da FRENTE (verde - vida atual)
        [SerializeField] private Image pendingFillImage;    // Barra de TRÁS (vermelha - dano pendente)
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject rootPanel;

        [Header("Animation Settings")]
        [SerializeField] private float quickAnimationDuration = 0.2f;
        [SerializeField] private float slowAnimationDuration = 0.8f;
        [SerializeField] private float delayBeforeSlowAnimation = 0.3f;
        [SerializeField] private Color normalColor = Color.green;
        [SerializeField] private Color pendingColor = Color.red;

        [Header("Debug")]
        [SerializeField] private bool showAnimationDebug = false;

        public string SlotId => $"{expectedActorId}_{expectedType}";
        public string ExpectedActorId => expectedActorId;
        public ResourceType ExpectedType => expectedType;

        private float _currentFill = 1f;        // Barra da frente (verde)
        private float _currentPending = 1f;     // Barra de trás (vermelha)
        private float _targetFill = 1f;
        private float _animationTimer = 0f;
        private AnimationPhase _currentPhase = AnimationPhase.Idle;

        private enum AnimationPhase
        {
            Idle,
            QuickAnimation,    // Barra verde vai rápido para o destino
            WaitingDelay,      // Espera antes da animação lenta
            SlowAnimation      // Barra vermelha vai lentamente
        }

        private void Awake()
        {
            // Validar componentes
            if (fillImage == null) fillImage = GetComponentInChildren<Image>();
            if (rootPanel == null) rootPanel = gameObject;

            // Configurar cores SEM transparência
            if (fillImage != null)
            {
                fillImage.color = normalColor;
                fillImage.fillAmount = 1f;
                _currentFill = 1f;
            }

            // Configurar barra pending
            if (pendingFillImage != null)
            {
                pendingFillImage.color = pendingColor;
                pendingFillImage.fillAmount = 1f;
                _currentPending = 1f;
                pendingFillImage.gameObject.SetActive(true);
            }

            DebugUtility.LogVerbose<ResourceUISlot>($"🔧 Slot inicializado: {SlotId}");
        }

        private void Update()
        {
            if (_currentPhase != AnimationPhase.Idle)
            {
                AnimateFill();
            }
        }

        public void Configure(IResourceValue data)
        {
            _targetFill = data.GetPercentage();

            // Atualizar texto imediatamente
            if (valueText != null)
            {
                valueText.text = $"{data.GetCurrentValue():0}/{data.GetMaxValue():0}";
            }

            // Verificar se precisa animar
            if (Mathf.Abs(_currentFill - _targetFill) > 0.01f)
            {
                StartAnimation();
            }
            else
            {
                // Sem animação necessária
                ApplyImmediate(_targetFill);
            }

            SetVisible(true);

            if (showAnimationDebug)
            {
                DebugUtility.LogVerbose<ResourceUISlot>($"🎯 Slot configurado: {data.GetCurrentValue():0}/{data.GetMaxValue():0} (Target: {_targetFill:F2})");
            }
        }

        private void StartAnimation()
        {
            _animationTimer = 0f;
            _currentPhase = AnimationPhase.QuickAnimation;

            if (showAnimationDebug)
            {
                bool isDamage = _targetFill < _currentFill;
                string changeType = isDamage ? "DANO" : "CURA";
                DebugUtility.LogVerbose<ResourceUISlot>($"🎬 Iniciando animação: {changeType} de {_currentFill:F2} para {_targetFill:F2}");
            }
        }

        private void AnimateFill()
        {
            _animationTimer += Time.deltaTime;

            switch (_currentPhase)
            {
                case AnimationPhase.QuickAnimation:
                    AnimateQuickPhase();
                    break;
                    
                case AnimationPhase.WaitingDelay:
                    AnimateWaitPhase();
                    break;
                    
                case AnimationPhase.SlowAnimation:
                    AnimateSlowPhase();
                    break;
            }
        }

        private void AnimateQuickPhase()
        {
            float progress = Mathf.Clamp01(_animationTimer / quickAnimationDuration);
            float easedProgress = EaseOutCubic(progress);

            // Barra VERDE vai rapidamente para o novo valor
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, easedProgress);
            
            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
            }

            // Barra VERMELHA permanece no valor antigo
            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = _currentPending;
            }

            if (progress >= 1f)
            {
                // Próxima fase: esperar delay
                _animationTimer = 0f;
                _currentPhase = AnimationPhase.WaitingDelay;

                if (showAnimationDebug)
                {
                    DebugUtility.LogVerbose<ResourceUISlot>($"⚡ Fase rápida concluída: Verde={_currentFill:F2}, Vermelha={_currentPending:F2}");
                }
            }
        }

        private void AnimateWaitPhase()
        {
            // Apenas espera o delay - barras mantêm suas posições
            if (_animationTimer >= delayBeforeSlowAnimation)
            {
                _animationTimer = 0f;
                _currentPhase = AnimationPhase.SlowAnimation;

                if (showAnimationDebug)
                {
                    DebugUtility.LogVerbose<ResourceUISlot>($"⏳ Delay concluído, iniciando animação lenta");
                }
            }
        }

        private void AnimateSlowPhase()
        {
            float progress = Mathf.Clamp01(_animationTimer / slowAnimationDuration);
            float easedProgress = EaseOutCubic(progress);

            // Barra VERMELHA vai lentamente até encontrar a VERDE
            _currentPending = Mathf.Lerp(_currentPending, _currentFill, easedProgress);
            
            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = _currentPending;
            }

            // Barra VERDE já está no lugar certo
            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
                // SEM mudança de cor - mantém verde normal
            }

            if (progress >= 1f)
            {
                FinishAnimation();
            }
        }

        private void FinishAnimation()
        {
            // Garantir valores exatos
            _currentPending = _currentFill;
            
            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = _currentPending;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
            }

            _currentPhase = AnimationPhase.Idle;

            if (showAnimationDebug)
            {
                DebugUtility.LogVerbose<ResourceUISlot>($"🏁 Animação concluída: {_currentFill:F2}");
            }
        }

        private void ApplyImmediate(float targetFill)
        {
            _currentFill = targetFill;
            _currentPending = targetFill;
            _targetFill = targetFill;

            if (fillImage != null)
            {
                fillImage.fillAmount = _currentFill;
            }

            if (pendingFillImage != null)
            {
                pendingFillImage.fillAmount = _currentPending;
            }

            _currentPhase = AnimationPhase.Idle;
        }

        private float EaseOutCubic(float x)
        {
            return 1f - Mathf.Pow(1f - x, 3f);
        }

        public void Clear()
        {
            _currentPhase = AnimationPhase.Idle;
            ApplyImmediate(0f);

            if (valueText != null) 
                valueText.text = "";

            SetVisible(false);
            DebugUtility.LogVerbose<ResourceUISlot>($"🔓 Slot limpo: {SlotId}");
        }

        public void SetVisible(bool visible)
        {
            if (rootPanel != null) 
                rootPanel.SetActive(visible);
        }

        [ContextMenu("Test Damage Animation")]
        public void TestDamageAnimation()
        {
            // Simula: 100 → 90 (10 de dano)
            _currentFill = 1f;
            _currentPending = 1f;
            _targetFill = 0.9f;
            StartAnimation();
        }

        [ContextMenu("Test Big Damage Animation")]
        public void TestBigDamageAnimation()
        {
            // Simula: 100 → 50 (50 de dano)
            _currentFill = 1f;
            _currentPending = 1f;
            _targetFill = 0.5f;
            StartAnimation();
        }

        [ContextMenu("Test Heal Animation")]
        public void TestHealAnimation()
        {
            // Simula: 50 → 80 (30 de cura)
            _currentFill = 0.5f;
            _currentPending = 0.5f;
            _targetFill = 0.8f;
            StartAnimation();
        }

        [ContextMenu("Test Full Heal Animation")]
        public void TestFullHealAnimation()
        {
            // Simula: 30 → 100 (cura completa)
            _currentFill = 0.3f;
            _currentPending = 0.3f;
            _targetFill = 1f;
            StartAnimation();
        }
    }
}