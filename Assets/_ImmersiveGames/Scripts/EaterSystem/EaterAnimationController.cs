using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterAnimationController : MonoBehaviour
    {
        private EaterMaster _eaterMaster;
        private Animator _animator;

        private static readonly int IsEating = Animator.StringToHash("isEating");
        private readonly int IsHappy = Animator.StringToHash("Happy");
        private readonly int IsMad = Animator.StringToHash("Mad");
        private readonly int GetHit = Animator.StringToHash("GetHit");
        private readonly int IsDead = Animator.StringToHash("Dead");

        private const float TransitionDuration = 0.1f;

        private EventBinding<EaterDeathEvent> _eaterDeathBinding;

        private void Awake()
        {
            TryGetComponent(out _eaterMaster);
            if (_eaterMaster.GetModelRoot().TryGetComponentInChildren(out _animator)) return;
            DebugUtility.LogError<EaterAnimationController>("Animator não encontrado no GameObject!", this);
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
            _animator.SetBool(IsEating, false);
            DebugUtility.LogVerbose<EaterAnimationController>("Animação de comer finalizada.");
        }
        private void OnStartedEating(IDetectable obj)
        {
            _animator.SetBool(IsEating, true);
            DebugUtility.LogVerbose<EaterAnimationController>("Animação de comer iniciada.");
        }

        private void OnConsumeResource(IDetectable obj, bool isHappy) 
        {
            if (isHappy)
                _animator.CrossFadeInFixedTime(IsHappy, TransitionDuration);
            else if (!isHappy)
                _animator.CrossFadeInFixedTime(IsMad, TransitionDuration);
        }

        private void OnGetHit() 
        {
            _animator.CrossFadeInFixedTime(GetHit, TransitionDuration);
        }

        private void OnDeath(EaterDeathEvent obj)
        {
            _animator.CrossFadeInFixedTime(IsDead, TransitionDuration);
        }
    }
}