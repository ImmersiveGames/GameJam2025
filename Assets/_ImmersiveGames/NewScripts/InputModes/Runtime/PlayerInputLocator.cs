using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.ActorsSystem.Contracts.Inbound;
using _ImmersiveGames.NewScripts.ActorsSystem.Models;
using UnityEngine.InputSystem;

namespace _ImmersiveGames.NewScripts.InputModes.Runtime
{
    internal sealed class PlayerInputLocator : IPlayerInputLocator
    {
        private readonly IActorsOperationalBindingQueryPort _bindingQueryPort;
        private readonly List<ActorsOperationalBindingEntry> _entries = new(16);

        public PlayerInputLocator(IActorsOperationalBindingQueryPort bindingQueryPort)
        {
            _bindingQueryPort = bindingQueryPort ?? throw new ArgumentNullException(nameof(bindingQueryPort));
        }

        public PlayerInput[] GetActivePlayerInputs()
        {
            _entries.Clear();
            if (!_bindingQueryPort.TryGetAll(_entries) || _entries.Count == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            var resolvedInputs = new List<PlayerInput>(_entries.Count);
            var seenInstanceIds = new HashSet<int>();

            for (int i = 0; i < _entries.Count; i += 1)
            {
                ActorsOperationalBindingEntry entry = _entries[i];
                if (!IsOperationallyBindable(entry))
                {
                    continue;
                }

                if (!TryResolvePlayerInput(entry.UnityHandles, out PlayerInput playerInput))
                {
                    continue;
                }

                if (playerInput == null || !playerInput.enabled || !playerInput.gameObject.activeInHierarchy)
                {
                    continue;
                }

                int instanceId = playerInput.GetInstanceID();
                if (!seenInstanceIds.Add(instanceId))
                {
                    continue;
                }

                resolvedInputs.Add(playerInput);
            }

            if (resolvedInputs.Count == 0)
            {
                return Array.Empty<PlayerInput>();
            }

            var result = resolvedInputs.ToArray();
            Array.Sort(result, CompareByInstanceId);
            return result;
        }

        private static bool IsOperationallyBindable(ActorsOperationalBindingEntry entry)
        {
            if (entry.FlowStep != ActorsOperationalBindingFlowStep.UnityOperationalBound)
            {
                return false;
            }

            if (entry.State != ActorsOperationalBindingState.Bound && entry.State != ActorsOperationalBindingState.Active)
            {
                return false;
            }

            return entry.UnityHandles.HasPlayerIndex;
        }

        private static bool TryResolvePlayerInput(ActorsUnityOperationalHandles handles, out PlayerInput playerInput)
        {
            playerInput = null;
            if (!handles.HasPlayerIndex)
            {
                return false;
            }

            playerInput = PlayerInput.GetPlayerByIndex(handles.PlayerIndex.Value);
            if (playerInput == null)
            {
                return false;
            }

            if (handles.HasInputUserId)
            {
                ulong actualUserId = (ulong)playerInput.user.id;
                if (actualUserId != handles.InputUserId.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static int CompareByInstanceId(PlayerInput left, PlayerInput right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return left.GetInstanceID().CompareTo(right.GetInstanceID());
        }
    }
}
