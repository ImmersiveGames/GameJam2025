using System;
using ImmersiveGames.GameJam2025.Game.Gameplay.State.Core;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Game.Gameplay.Spawn
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

