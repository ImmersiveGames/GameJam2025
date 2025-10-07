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
        [SerializeField] protected AnimationConfig _animationConfig;
        
        protected Animator _animator;
        protected AnimationResolver _animationResolver;
        protected string _objectId;

        // Propriedades para hashs
        protected int IdleHash => _animationConfig?.IdleHash ?? Animator.StringToHash("Idle");
        protected int HitHash => _animationConfig?.HitHash ?? Animator.StringToHash("GetHit");
        protected int DeathHash => _animationConfig?.DeathHash ?? Animator.StringToHash("Die");
        protected int ReviveHash => _animationConfig?.ReviveHash ?? Animator.StringToHash("Revive");

        protected virtual void Awake()
        {
            DependencyManager.Instance.InjectDependencies(this);
            
            // Busca o AnimationResolver
            _animationResolver = GetComponent<AnimationResolver>();
            
            if (_animationResolver == null)
            {
                DebugUtility.LogError<AnimationControllerBase>(
                    $"AnimationResolver não encontrado em {name}. O componente será desativado.");
                enabled = false;
                return;
            }

            // Tenta obter config via Provider
            if (_animationConfig == null)
            {
                DependencyManager.Instance.TryGetGlobal<AnimationConfigProvider>(out var configProvider);
                if (configProvider != null)
                {
                    // Usa o tipo do controller como chave para a config
                    string configKey = GetType().Name;
                    _animationConfig = configProvider.GetConfig(configKey);
                }
            }
            // Fallback para config padrão
            if (_animationConfig == null)
            {
                _animationConfig = ScriptableObject.CreateInstance<AnimationConfig>();
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
            _animator = _animationResolver.GetAnimator();

            if (_animator == null)
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

            _objectId = _idFactory.GenerateId(gameObject);

            if (string.IsNullOrEmpty(_objectId))
            {
                DebugUtility.LogWarning<AnimationControllerBase>(
                    $"Falha ao gerar ObjectId para {name}. Dependências não serão registradas.");
                return;
            }

            DebugUtility.LogVerbose<AnimationControllerBase>(
                $"Registrando controlador de animação para ID: {_objectId}.", "cyan");

            DependencyManager.Instance.RegisterForObject(_objectId, this);
        }

        protected virtual void OnDisable()
        {
            if (!string.IsNullOrEmpty(_objectId))
            {
                DependencyManager.Instance.ClearObjectServices(_objectId);
                DebugUtility.LogVerbose<AnimationControllerBase>(
                    $"Serviços removidos do objeto {_objectId}.", "yellow");
            }
        }

        // Método chamado quando o Animator muda (ex: troca de skin)
        public virtual void OnAnimatorChanged(Animator newAnimator)
        {
            _animator = newAnimator;
            DebugUtility.LogVerbose<AnimationControllerBase>(
                $"Animator atualizado para {_objectId}", "orange");
        }

        protected void PlayHash(int hash)
        {
            if (_animator != null && gameObject.activeInHierarchy)
                _animator.Play(hash);
        }
    }
}