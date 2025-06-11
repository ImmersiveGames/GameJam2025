using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
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
        private EaterMaster _eaterMaster;
        private Animator _animator;

        private static readonly int IsEating = Animator.StringToHash("IsEating");

        private void Awake()
        {
            TryGetComponent(out _eaterMaster);
            if (_eaterMaster.GetModelRoot().TryGetComponentInChildren(out _animator)) return;
            DebugUtility.LogError<EaterAnimationController>("Animator não encontrado no GameObject!", this);
            enabled = false;
        }

        private void OnEnable()
        {
            _eaterMaster.StartEatPlanetEvent += OnStartedEating;
            _eaterMaster.StopEatPlanetEvent += OnFinishedEating;
        }

        private void OnDisable()
        {
            _eaterMaster.StartEatPlanetEvent -= OnStartedEating;
            _eaterMaster.StopEatPlanetEvent -= OnFinishedEating;
        }
        private void OnFinishedEating(IPlanetInteractable obj)
        {
            _animator.SetBool(IsEating, false);
            DebugUtility.LogVerbose<EaterAnimationController>("Animação de comer finalizada.");
        }
        private void OnStartedEating(IPlanetInteractable obj)
        {
            _animator.SetBool(IsEating, true);
            DebugUtility.LogVerbose<EaterAnimationController>("Animação de comer iniciada.");
        }
    }
}