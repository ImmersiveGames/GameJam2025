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

        private readonly int IsHappy = Animator.StringToHash("Happy");
        private readonly int IsSad = Animator.StringToHash("Sad");
        private readonly int IsGameOver = Animator.StringToHash("Sad");
        private readonly int GetHit = Animator.StringToHash("GetHit");
        private readonly int Die = Animator.StringToHash("Die");

        private const float TransitionDuration = 0.1f;

        private EventBinding<PlayerDiedEvent> _playerDiedEvent;       
        private EventBinding<EaterSatisfactionEvent> _eaterSatisfactionBinding;
        private EventBinding<EaterDeathEvent> _eaterDeathBinding;

        private void Awake()
        {
            TryGetComponent(out _playerMaster);
            if (_playerMaster.GetModelRoot().TryGetComponentInChildren(out _animator)) return;
            DebugUtility.LogError<EaterAnimationController>("Animator n�o encontrado no GameObject!", this);
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
            _animator.CrossFadeInFixedTime(Die, TransitionDuration);
        }

        private void OnGetHit() 
        {
            _animator.CrossFadeInFixedTime(GetHit, TransitionDuration);
        }

        private void OnEaterDeath(EaterDeathEvent obj)
        {
            _animator.CrossFadeInFixedTime(IsGameOver, TransitionDuration);
        }

        private void OnEaterConsumePlanet(EaterSatisfactionEvent obj)
        {            
            if (obj.IsSatisfected)
                _animator.CrossFadeInFixedTime(IsHappy, TransitionDuration);
            else if(!obj.IsSatisfected)
                _animator.CrossFadeInFixedTime(IsSad, TransitionDuration);
        }
    }
}

