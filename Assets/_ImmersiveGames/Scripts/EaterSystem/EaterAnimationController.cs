using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.EaterSystem.EventBus;
using _ImmersiveGames.Scripts.GameManagerSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.Extensions;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterAnimationController : MonoBehaviour
    {
        private ActorMaster _actorMaster;
        private Animator _animator;
        private EventBinding<EaterStartedEatingEvent> _startedEatingBinding;
        private EventBinding<EaterFinishedEatingEvent> _finishedEatingBinding;

        private static readonly int IsEating = Animator.StringToHash("IsEating");

        private void Awake()
        {
            TryGetComponent(out _actorMaster);
            if (_actorMaster.GetModelRoot().TryGetComponentInChildren(out _animator)) return;
            DebugUtility.LogError<EaterAnimationController>("Animator não encontrado no GameObject!", this);
            enabled = false;
        }

        private void OnEnable()
        {
            _startedEatingBinding = new EventBinding<EaterStartedEatingEvent>(OnStartedEating);
            _finishedEatingBinding = new EventBinding<EaterFinishedEatingEvent>(OnFinishedEating);
            EventBus<EaterStartedEatingEvent>.Register(_startedEatingBinding);
            EventBus<EaterFinishedEatingEvent>.Register(_finishedEatingBinding);
        }

        private void OnDisable()
        {
            EventBus<EaterStartedEatingEvent>.Unregister(_startedEatingBinding);
            EventBus<EaterFinishedEatingEvent>.Unregister(_finishedEatingBinding);
        }

        private void OnStartedEating(EaterStartedEatingEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _animator.SetBool(IsEating, true);
            DebugUtility.LogVerbose<EaterAnimationController>("Animação de comer iniciada.");
        }

        private void OnFinishedEating(EaterFinishedEatingEvent evt)
        {
            if (!GameManager.Instance.ShouldPlayingGame()) return;
            _animator.SetBool(IsEating, false);
            DebugUtility.LogVerbose<EaterAnimationController>("Animação de comer finalizada.");
        }
    }
}