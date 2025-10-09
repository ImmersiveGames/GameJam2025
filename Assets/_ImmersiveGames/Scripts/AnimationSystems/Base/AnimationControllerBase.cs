using _ImmersiveGames.Scripts.AnimationSystems.Components;
using _ImmersiveGames.Scripts.AnimationSystems.Config;
using _ImmersiveGames.Scripts.AnimationSystems.Services;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.AnimationSystems.Base
{
    [DebugLevel(DebugLevel.Verbose)]
    public abstract class AnimationControllerBase : MonoBehaviour
    {
        [Inject] private IUniqueIdFactory _idFactory;
        [SerializeField] protected AnimationConfig animationConfig;
        
        protected Animator animator;
        protected AnimationResolver animationResolver;
        protected string objectId;

        // Propriedades para hashs
        protected int IdleHash => animationConfig?.IdleHash ?? Animator.StringToHash("Idle");
        protected int HitHash => animationConfig?.HitHash ?? Animator.StringToHash("GetHit");
        protected int DeathHash => animationConfig?.DeathHash ?? Animator.StringToHash("Die");
        protected int ReviveHash => animationConfig?.ReviveHash ?? Animator.StringToHash("Revive");

        protected virtual void Awake()
        {
            DependencyManager.Instance.InjectDependencies(this);
            
            // Busca o AnimationResolver
            animationResolver = GetComponent<AnimationResolver>();
            
            if (animationResolver == null)
            {
                DebugUtility.LogError<AnimationControllerBase>(
                    $"AnimationResolver não encontrado em {name}. O componente será desativado.");
                enabled = false;
                return;
            }

            // Tenta obter config via Provider
            if (animationConfig == null)
            {
                DependencyManager.Instance.TryGetGlobal<AnimationConfigProvider>(out var configProvider);
                if (configProvider != null)
                {
                    // Usa o tipo do controller como chave para a config
                    string configKey = GetType().Name;
                    animationConfig = configProvider.GetConfig(configKey);
                }
            }
            // Fallback para config padrão
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
            InitializeDependencyRegistration();
        }

        private void InitializeAnimator()
        {
            // Usa o AnimationResolver para obter o Animator
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

        private void InitializeDependencyRegistration()
        {
            if (_idFactory == null)
            {
                DebugUtility.LogError<AnimationControllerBase>(
                    $"IUniqueIdFactory não injetado em {name}. Verifique o DependencyBootstrapper.");
                return;
            }

            objectId = _idFactory.GenerateId(gameObject);

            if (string.IsNullOrEmpty(objectId))
            {
                DebugUtility.LogWarning<AnimationControllerBase>(
                    $"Falha ao gerar ObjectId para {name}. Dependências não serão registradas.");
                return;
            }

            DebugUtility.LogVerbose<AnimationControllerBase>(
                $"Registrando controlador de animação para ID: {objectId}.", "cyan");

            DependencyManager.Instance.RegisterForObject(objectId, this);
        }

        protected virtual void OnDisable()
        {
            if (!string.IsNullOrEmpty(objectId))
            {
                DependencyManager.Instance.ClearObjectServices(objectId);
                DebugUtility.LogVerbose<AnimationControllerBase>(
                    $"Serviços removidos do objeto {objectId}.", "yellow");
            }
        }

        // Método chamado quando o Animator muda (ex: troca de skin)
        public virtual void OnAnimatorChanged(Animator newAnimator)
        {
            animator = newAnimator;
            DebugUtility.LogVerbose<AnimationControllerBase>(
                $"Animator atualizado para {objectId}", "orange");
        }

        protected void PlayHash(int hash)
        {
            if (animator != null && gameObject.activeInHierarchy)
                animator.Play(hash);
        }
    }
}