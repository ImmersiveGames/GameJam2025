using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AnimationSystems.Components;
using _ImmersiveGames.Scripts.AnimationSystems.Config;
using _ImmersiveGames.Scripts.AnimationSystems.Services;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.AnimationSystems.Base
{
    public abstract class AnimationControllerBase : MonoBehaviour
    {
        [SerializeField] protected AnimationConfig animationConfig;

        protected Animator animator;
        private AnimationResolver _animationResolver;
        protected ActorMaster Actor { get; private set; }
        private bool _dependencyRegistered;

        protected int IdleHash => animationConfig?.IdleHash ?? Animator.StringToHash("Idle");
        protected int HitHash => animationConfig?.HitHash ?? Animator.StringToHash("GetHit");
        protected int DeathHash => animationConfig?.DeathHash ?? Animator.StringToHash("Die");
        protected int ReviveHash => animationConfig?.ReviveHash ?? Animator.StringToHash("Revive");

        protected virtual void Awake()
        {
            // Injeção sem ActorId — como no original que funcionava
            DependencyManager.Provider.InjectDependencies(this);

            _animationResolver = GetComponent<AnimationResolver>();
            if (_animationResolver == null)
            {
                DebugUtility.LogError<AnimationControllerBase>($"AnimationResolver não encontrado em {name}");
                enabled = false;
                return;
            }

            Actor = GetComponent<ActorMaster>();
            if (Actor == null)
            {
                DebugUtility.LogError<AnimationControllerBase>($"ActorMaster não encontrado em {name}");
                enabled = false;
                return;
            }

            _animationResolver.OnAnimatorChanged += OnAnimatorChanged;

            if (animationConfig == null)
            {
                if (DependencyManager.Provider.TryGetGlobal<AnimationConfigProvider>(out var provider))
                {
                    animationConfig = provider.GetConfig(GetType().Name);
                }
            }

            if (animationConfig == null)
            {
                animationConfig = ScriptableObject.CreateInstance<AnimationConfig>();
                DebugUtility.LogWarning<AnimationControllerBase>($"Config padrão para {name}");
            }

            animator = _animationResolver.GetAnimator();
            RegisterDependencies();
        }

        private void RegisterDependencies()
        {
            if (string.IsNullOrEmpty(Actor.ActorId)) return;

            DependencyManager.Provider.RegisterForObject(Actor.ActorId, this);
            _dependencyRegistered = true;
        }

        protected virtual void OnDisable()
        {
            if (_dependencyRegistered && !string.IsNullOrEmpty(Actor.ActorId))
            {
                DependencyManager.Provider.ClearObjectServices(Actor.ActorId);
                _dependencyRegistered = false;
            }
        }

        public virtual void OnAnimatorChanged(Animator newAnimator)
        {
            animator = newAnimator;
        }

        protected virtual void OnDestroy()
        {
            if (_animationResolver != null)
            {
                _animationResolver.OnAnimatorChanged -= OnAnimatorChanged;
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