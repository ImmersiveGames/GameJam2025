using _ImmersiveGames.Scripts.AnimationSystems.Base;
using _ImmersiveGames.Scripts.AudioSystem;
using _ImmersiveGames.Scripts.EaterSystem.Animations;
using _ImmersiveGames.Scripts.EaterSystem.Detections;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.Scripts.AudioSystem.Components;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem.Behavior
{
    /// <summary>
    /// Parte da implementa��o do Eater focada em controladores auxiliares:
    /// - detec��o (<see cref="EaterDetectionController"/>);
    /// - anima��o (<see cref="EaterAnimationController"/> / <see cref="AnimationControllerBase"/>);
    /// - �udio (<see cref="EntityAudioEmitter"/>).
    /// </summary>
    public sealed partial class EaterBehavior : MonoBehaviour
    {
        private EntityAudioEmitter _audioEmitter;
        private EaterDetectionController _detectionController;
        private EaterAnimationController _animationController;

        /// <summary>
        /// Obt�m (e cacheia) o <see cref="EaterDetectionController"/> associado ao Eater.
        /// </summary>
        internal bool TryGetDetectionController(out EaterDetectionController detectionController)
        {
            if (_detectionController == null)
            {
                TryGetComponent(out _detectionController);
            }

            detectionController = _detectionController;
            return detectionController != null;
        }

        /// <summary>
        /// Obt�m (e cacheia) o <see cref="EaterAnimationController"/> associado ao Eater.
        /// Tenta primeiro resolver via <see cref="DependencyManager"/> usando o ActorId,
        /// depois cai para GetComponent como fallback.
        /// </summary>
        internal bool TryGetAnimationController(out EaterAnimationController animationController)
        {
            if (_animationController == null)
            {
                string actorId = Master != null ? Master.ActorId : null;
                if (!string.IsNullOrEmpty(actorId))
                {
                    // Tenta resolver diretamente o EaterAnimationController via DI
                    if (DependencyManager.Provider.TryGetForObject(actorId, out EaterAnimationController resolvedController))
                    {
                        _animationController = resolvedController;

                        DebugUtility.LogVerbose<Behavior.EaterBehavior>(
                            $"EaterAnimationController resolvido via DependencyManager para ActorId={actorId}.");
                    }
                    // Se vier um AnimationControllerBase, tenta converter para EaterAnimationController
                    else if (DependencyManager.Provider.TryGetForObject(actorId, out AnimationControllerBase baseController)
                             && baseController is EaterAnimationController eaterController)
                    {
                        _animationController = eaterController;

                        DebugUtility.LogVerbose<Behavior.EaterBehavior>(
                            $"AnimationControllerBase resolvido e convertido para EaterAnimationController (ActorId={actorId}).");
                    }
                }

                // Fallback: tenta pegar direto no GameObject
                if (_animationController == null && !TryGetComponent(out _animationController))
                {
                    _animationController = null;
                }
            }

            animationController = _animationController;
            return animationController != null;
        }

        /// <summary>
        /// Obt�m (e cacheia) o <see cref="EntityAudioEmitter"/> associado ao Eater.
        /// Tenta resolver via <see cref="DependencyManager"/> usando ActorId e, se n�o conseguir,
        /// procura via GetComponent.
        /// </summary>
        internal bool TryGetAudioEmitter(out EntityAudioEmitter audioEmitter)
        {
            if (_audioEmitter == null)
            {
                if (Master != null)
                {
                    string actorId = Master.ActorId;
                    if (!string.IsNullOrEmpty(actorId)
                        && DependencyManager.Provider.TryGetForObject(actorId, out EntityAudioEmitter resolvedEmitter))
                    {
                        _audioEmitter = resolvedEmitter;
                    }
                }

                if (_audioEmitter == null)
                {
                    TryGetComponent(out _audioEmitter);
                }
            }

            audioEmitter = _audioEmitter;
            return audioEmitter != null;
        }
    }
}

