using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace _ImmersiveGames.NewScripts.Core.Infrastructure.InputModes.Runtime
{
    internal sealed class PlayerInputLocator : IPlayerInputLocator
    {
        public PlayerInput[] GetActivePlayerInputs()
        {
            PlayerInput[] all = Object.FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            int count = all.Count(pi => pi != null && pi.enabled && pi.gameObject.activeInHierarchy);
            if (count == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            var result = new PlayerInput[count];
            int idx = 0;
            foreach (var pi in all)
            {
                if (pi != null && pi.enabled && pi.gameObject.activeInHierarchy)
                {
                    result[idx++] = pi;
                }
            }

            return result;
        }
    }
}
