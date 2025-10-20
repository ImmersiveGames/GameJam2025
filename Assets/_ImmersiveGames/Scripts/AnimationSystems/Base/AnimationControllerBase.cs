using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AnimationSystems.Components;
using _ImmersiveGames.Scripts.AnimationSystems.Config;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AnimationSystems.Base
{
    [DebugLevel(DebugLevel.Verbose)]
    public abstract class AnimationControllerBase : MonoBehaviour
    {
        [SerializeField] protected AnimationConfig animationConfig;

        protected Animator animator;
        protected AnimationResolver animationResolver;
        protected ActorMaster Actor { get; private set; }
        private bool _hasStarted;
        private bool _dependencyRegistered;

        // Propriedades para hashs
        protected int IdleHash => animationConfig?.IdleHash ?? Animator.StringToHash("Idle");
        protected int HitHash => animationConfig?.HitHash ?? Animator.StringToHash("GetHit");
        protected int DeathHash => animationConfig?.DeathHash ?? Animator.StringToHash("Die");
        protected int ReviveHash => animationConfig?.ReviveHash ?? Animator.StringToHash("Revive");

        protected virtual void Awake()
        {
            DependencyManager.Instance.InjectDependencies(this);

            animationResolver = GetComponent<AnimationResolver>();
            if (animationResolver == null)
            {
                DebugUtility.LogError<AnimationControllerBase>(
                    $"AnimationResolver não encontrado em {name}. O componente será desativado.");
                enabled = false;
                return;
            }

            Actor = GetComponent<ActorMaster>();
            if (Actor == null)
            {
                DebugUtility.LogError<AnimationControllerBase>(
                    $"ActorMaster não encontrado em {name}. O componente será desativado.");
                enabled = false;
                return;
            }

            animationResolver.OnAnimatorChanged += OnAnimatorChanged;

            if (animationConfig == null)
            {
                DependencyManager.Instance.TryGetGlobal<AnimationConfigProvider>(out var configProvider);
                if (configProvider != null)
                {
                    string configKey = GetType().Name;
                    animationConfig = configProvider.GetConfig(configKey);
                }
            }

            if (animationConfig == null)
            {
                animationConfig = ScriptableObject.CreateInstance<AnimationConfig>();
                DebugUtility.LogWarning<AnimationControllerBase>(
                    $"Usando AnimationConfig padrão para {name}.");
            }
        }

        protected virtual void Start()
        {
            InitializeAnimator();
            RegisterDependencies();
            _hasStarted = true;
        }

        protected virtual void OnEnable()
        {
            if (_hasStarted && animator == null)
            {
                InitializeAnimator();
            }

            if (_hasStarted && !_dependencyRegistered)
            {
                RegisterDependencies();
            }
        }

        private void InitializeAnimator()
        {
            animator = animationResolver.GetAnimator();

            if (animator == null)
            {
                DebugUtility.LogError<AnimationControllerBase>(
                    $"Nenhum Animator encontrado via AnimationResolver em {name}. O componente será desativado.");
                enabled = false;
                return;
            }

            DebugUtility.LogVerbose<AnimationControllerBase>(
                $"Animator obtido via AnimationResolver em {name}.", "green");
        }

        private void RegisterDependencies()
        {
            if (Actor == null)
            {
                DebugUtility.LogError<AnimationControllerBase>(
                    $"ActorMaster não configurado em {name}. Dependências não serão registradas.");
                return;
            }

            if (string.IsNullOrEmpty(Actor.ActorId))
            {
                DebugUtility.LogWarning<AnimationControllerBase>(
                    $"ActorId inválido em {name}. Dependências não serão registradas.");
                return;
            }

            DebugUtility.LogVerbose<AnimationControllerBase>(
                $"Registrando controlador de animação para ID: {Actor.ActorId}.", "cyan");

            DependencyManager.Instance.RegisterForObject(Actor.ActorId, this);
            _dependencyRegistered = true;
        }

        protected virtual void OnDisable()
        {
            if (_dependencyRegistered && Actor != null && !string.IsNullOrEmpty(Actor.ActorId))
            {
                DependencyManager.Instance.ClearObjectServices(Actor.ActorId);
                DebugUtility.LogVerbose<AnimationControllerBase>(
                    $"Serviços removidos do objeto {Actor.ActorId}.", "yellow");
            }

            _dependencyRegistered = false;
        }

        public virtual void OnAnimatorChanged(Animator newAnimator)
        {
            animator = newAnimator;
            DebugUtility.LogVerbose<AnimationControllerBase>(
                Actor != null
                    ? $"Animator atualizado para {Actor.ActorId}"
                    : "Animator atualizado para objeto desconhecido",
                "orange");
        }

        protected virtual void OnDestroy()
        {
            if (animationResolver != null)
            {
                animationResolver.OnAnimatorChanged -= OnAnimatorChanged;
            }
        }

        protected void PlayHash(int hash)
        {
            if (animator != null && gameObject.activeInHierarchy)
            {
                animator.Play(hash);
            }
        }
    }
}
