using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AnimationSystems.Base;
using _ImmersiveGames.Scripts.AnimationSystems.Interfaces;
using _ImmersiveGames.Scripts.SkinSystems;
using _ImmersiveGames.Scripts.Tags;
using _ImmersiveGames.Scripts.Utils;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.AnimationSystems.Components
{
    [DefaultExecutionOrder(-50)]
    [DebugLevel(DebugLevel.Verbose)]
    public class AnimationResolver : MonoBehaviour,IAnimatorProvider
    {
        [Inject] private IUniqueIdFactory _idFactory;
        
        private Animator _cachedAnimator;
        private ModelRoot _modelRoot;
        private IActor _actor;
        private EventBinding<SkinUpdateEvent> _skinBinding;
        private string _objectId;


        public event System.Action<Animator> OnAnimatorChanged;

        public Animator GetAnimator() => _cachedAnimator ??= ResolveAnimator();

        private void Awake()
        {
            DependencyManager.Instance.InjectDependencies(this);
            InitializeDependencyRegistration();
        }

        private void OnEnable()
        {
            _actor = GetComponent<IActor>();
            _skinBinding = new EventBinding<SkinUpdateEvent>(OnSkinUpdated);
            FilteredEventBus<SkinUpdateEvent>.Register(_skinBinding, _actor);
        }

        private void OnDisable()
        {
            FilteredEventBus<SkinUpdateEvent>.Unregister(_actor);
            
            if (!string.IsNullOrEmpty(_objectId))
            {
                DependencyManager.Instance.ClearObjectServices(_objectId);
            }
        }

        private Animator ResolveAnimator()
        {
            _modelRoot ??= GetComponent<ActorMaster>()?.ModelRoot;
            if (_modelRoot != null)
            {
                _cachedAnimator = _modelRoot.GetComponentInChildren<Animator>(true);
                DebugUtility.LogVerbose<AnimationResolver>(
                    $"Animator resolvido para {_objectId}: {_cachedAnimator}", "cyan");
            }
            return _cachedAnimator;
        }

        private void InitializeDependencyRegistration()
        {
            if (_idFactory == null)
            {
                DebugUtility.LogError<AnimationResolver>(
                    $"IUniqueIdFactory não injetado em {name}.");
                return;
            }

            _objectId = _idFactory.GenerateId(gameObject);

            if (!string.IsNullOrEmpty(_objectId))
            {
                DependencyManager.Instance.RegisterForObject(_objectId, this);
                DebugUtility.LogVerbose<AnimationResolver>(
                    $"AnimationResolver registrado para ID: {_objectId}", "green");
            }
        }

        private void OnSkinUpdated(SkinUpdateEvent evt)
        {
            _cachedAnimator = null;
            var newAnimator = ResolveAnimator();
        
            // Notifica via evento
            OnAnimatorChanged?.Invoke(newAnimator);
        
            // Notifica via DependencyManager
            if (DependencyManager.Instance.TryGetForObject<AnimationControllerBase>(_objectId, out var controller))
            {
                controller.OnAnimatorChanged(newAnimator);
            }
        }
    }
}