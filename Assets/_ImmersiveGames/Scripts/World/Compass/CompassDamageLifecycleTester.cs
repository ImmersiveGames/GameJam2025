using System.Linq;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.World.Compass
{
    /// <summary>
    /// Ferramenta de depuração para validar se o ciclo de vida (morte/revive/reset)
    /// está sincronizando corretamente os alvos na bússola.
    /// </summary>
    public class CompassDamageLifecycleTester : MonoBehaviour
    {
        [Header("References")]
        public ActorMaster actor;
        public MonoBehaviour trackableComponent;

        private ICompassTrackable _trackable;

        private void Start()
        {
            actor ??= GetComponent<ActorMaster>();
            _trackable = ResolveTrackable();

            LogTrackables("Estado inicial");
        }

        private void Update()
        {
            if (actor == null || _trackable == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TriggerDeath();
                LogTrackables("Após DeathEvent (Alpha1)");
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TriggerRevive();
                LogTrackables("Após ReviveEvent (Alpha2)");
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TriggerReset();
                LogTrackables("Após ResetEvent (Alpha3)");
            }
        }

        private void TriggerDeath()
        {
            var evt = new DeathEvent(actor.ActorId, ResourceType.Health);
            FilteredEventBus<DeathEvent>.RaiseFiltered(evt, actor.ActorId);
        }

        private void TriggerRevive()
        {
            var evt = new ReviveEvent(actor.ActorId);
            FilteredEventBus<ReviveEvent>.RaiseFiltered(evt, actor.ActorId);
        }

        private void TriggerReset()
        {
            var evt = new ResetEvent(actor.ActorId);
            FilteredEventBus<ResetEvent>.RaiseFiltered(evt, actor.ActorId);
        }

        private ICompassTrackable ResolveTrackable()
        {
            if (trackableComponent is ICompassTrackable trackable)
            {
                return trackable;
            }

            return GetComponentInChildren<ICompassTrackable>();
        }

        private static void LogTrackables(string context)
        {
            var names = CompassRuntimeService.Trackables
                .Where(t => t != null)
                .Select(t => t.Transform?.name ?? t.ToString())
                .ToArray();

            DebugUtility.LogVerbose<CompassDamageLifecycleTester>(
                $"{context}: {names.Length} trackable(s) → [{string.Join(", ", names)}]");
        }
    }
}
