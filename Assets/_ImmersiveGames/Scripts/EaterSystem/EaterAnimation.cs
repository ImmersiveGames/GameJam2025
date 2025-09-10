using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.EaterSystem.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Warning)]
    public class EaterAnimation : MonoBehaviour
    {
        private EaterMaster _eaterMaster;
        private Animator _animator;

        private readonly int _isEating = Animator.StringToHash("isEating");
        private readonly int _isHappy = Animator.StringToHash("Happy");
        private readonly int _isMad = Animator.StringToHash("Mad");
        private readonly int _getHit = Animator.StringToHash("GetHit");
        private readonly int _isDead = Animator.StringToHash("Dead");

        private const float TransitionDuration = 0.1f;

        private EventBinding<EaterDeathEvent> _eaterDeathBinding;

        private void Awake()
        {
            TryGetComponent(out _eaterMaster);
            if (_eaterMaster.ModelTransform.TryGetComponentInChildren(out _animator)) return;
            DebugUtility.LogError<EaterAnimation>("Animator não encontrado no Actor!", this);
            enabled = false;
        }

        private void OnEnable()
        {
            _eaterMaster.EventStartEatPlanet += OnStartedEating;
            _eaterMaster.EventEndEatPlanet += OnFinishedEating;
            _eaterMaster.EventConsumeResource += OnConsumeResource;

            _eaterDeathBinding = new EventBinding<EaterDeathEvent>(OnDeath);
            EventBus<EaterDeathEvent>.Register(_eaterDeathBinding);
        }

        private void OnDisable()
        {
            _eaterMaster.EventStartEatPlanet -= OnStartedEating;
            _eaterMaster.EventEndEatPlanet -= OnFinishedEating;

            EventBus<EaterDeathEvent>.Unregister(_eaterDeathBinding);
        }
        private void OnFinishedEating(IDetectable obj)
        {
            _animator.SetBool(_isEating, false);
            DebugUtility.LogVerbose<EaterAnimation>("Animação de comer finalizada.");
        }
        private void OnStartedEating(IDetectable obj)
        {
            _animator.SetBool(_isEating, true);
            DebugUtility.LogVerbose<EaterAnimation>("Animação de comer iniciada.");
        }

        private void OnConsumeResource(IDetectable obj, bool isHappy, IActor byActor)
        {
            if (byActor is not EaterMaster) return;
            _animator.CrossFadeInFixedTime(isHappy ? _isHappy : _isMad, TransitionDuration);
        }

        private void OnGetHit() 
        {
            _animator.CrossFadeInFixedTime(_getHit, TransitionDuration);
        }

        private void OnDeath(EaterDeathEvent obj)
        {
            _animator.CrossFadeInFixedTime(_isDead, TransitionDuration);
        }
    }
}