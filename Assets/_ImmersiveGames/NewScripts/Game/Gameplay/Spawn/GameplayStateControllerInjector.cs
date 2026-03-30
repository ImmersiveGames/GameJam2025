using System;
using _ImmersiveGames.NewScripts.Game.Gameplay.State;
using _ImmersiveGames.NewScripts.Game.Gameplay.State.Core;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Game.Gameplay.Spawn
{
    internal static class GameplayStateControllerInjector
    {
        public static void TryInject<TController>(
            GameObject instance,
            IGameplayStateGate gameplayStateService,
            Action<TController, IGameplayStateGate> inject)
            where TController : Component
        {
            if (instance == null || gameplayStateService == null || inject == null)
            {
                return;
            }

            if (!instance.TryGetComponent(out TController controller) || controller == null)
            {
                return;
            }

            inject(controller, gameplayStateService);
        }
    }
}
