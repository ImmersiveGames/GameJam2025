using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.PlayerControllerSystem.EventBus;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlayerControllerSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class PlayerAnimationController : MonoBehaviour
    {
        private PlayerMaster _playerMaster;
        private Animator _animator;

        private readonly int _isHappy = Animator.StringToHash("Happy");
        private readonly int _isSad = Animator.StringToHash("Sad");
        private readonly int _isGameOver = Animator.StringToHash("Sad");
        private readonly int _getHit = Animator.StringToHash("GetHit");
        private readonly int _die = Animator.StringToHash("Die");

        private const float TransitionDuration = 0.1f;

        private EventBinding<PlayerDiedEvent> _playerDiedEvent;       
        private EventBinding<EaterSatisfactionEvent> _eaterSatisfactionBinding;
        private EventBinding<EaterDeathEvent> _eaterDeathBinding;

        private void Awake()
        {
            TryGetComponent(out _playerMaster);
            if (_playerMaster.ModelTransform.TryGetComponentInChildren(out _animator)) return;
            DebugUtility.LogError<EaterAnimationController>("Animator n�o encontrado no Actor!", this);
            enabled = false;
        }

        private void OnEnable()
        {
            _playerMaster.EventPlayerTakeDamage += OnGetHit;
            
            _playerDiedEvent = new EventBinding<PlayerDiedEvent>(OnPlayerDeath);
            EventBus<PlayerDiedEvent>.Register(_playerDiedEvent);

            _eaterSatisfactionBinding = new EventBinding<EaterSatisfactionEvent>(OnEaterConsumePlanet);
            EventBus<EaterSatisfactionEvent>.Register(_eaterSatisfactionBinding);

            _eaterDeathBinding = new EventBinding<EaterDeathEvent>(OnEaterDeath);
            EventBus<EaterDeathEvent>.Register(_eaterDeathBinding);
        }

        private void OnDisable()
        {
            _playerMaster.EventPlayerTakeDamage -= OnGetHit;

            EventBus<PlayerDiedEvent>.Unregister(_playerDiedEvent);
            EventBus<EaterSatisfactionEvent>.Unregister(_eaterSatisfactionBinding);
            EventBus<EaterDeathEvent>.Unregister(_eaterDeathBinding);
        }

        private void OnPlayerDeath() 
        {
            _animator.CrossFadeInFixedTime(_die, TransitionDuration);
        }

        private void OnGetHit(IActor byActor) 
        {
            _animator.CrossFadeInFixedTime(_getHit, TransitionDuration);
        }

        private void OnEaterDeath(EaterDeathEvent obj)
        {
            _animator.CrossFadeInFixedTime(_isGameOver, TransitionDuration);
        }

        private void OnEaterConsumePlanet(EaterSatisfactionEvent obj)
        {
            switch (obj.IsSatisfied)
            {
                case true:
                    _animator.CrossFadeInFixedTime(_isHappy, TransitionDuration);
                    break;
                case false:
                    _animator.CrossFadeInFixedTime(_isSad, TransitionDuration);
                    break;
            }
        }
    }
}

