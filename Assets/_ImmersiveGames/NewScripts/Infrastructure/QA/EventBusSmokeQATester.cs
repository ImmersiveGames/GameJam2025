using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Smoke test mínimo para o EventBus em modo NewScripts.
    /// Verifica subscribe/publish/unsubscribe com logs de PASS/FAIL.
    /// </summary>
    public sealed class EventBusSmokeQATester : MonoBehaviour
    {
        [ContextMenu("QA/EventBus/Run")]
        public void Run()
        {
            TestSubscribeAndPublish();
            TestUnsubscribe();
            DebugUtility.Log(typeof(EventBusSmokeQATester), "[QA][EventBus] QA complete.");
        }

        private static void TestSubscribeAndPublish()
        {
            var binding = new EventBinding<SmokeTestEvent>(_ => { Received = true; });
            EventBus<SmokeTestEvent>.Register(binding);

            Received = false;
            EventBus<SmokeTestEvent>.Raise(new SmokeTestEvent());

            if (Received)
            {
                DebugUtility.Log(typeof(EventBusSmokeQATester), "[QA][EventBus][A] PASS - Subscriber received event.", DebugUtility.Colors.Success);
            }
            else
            {
                DebugUtility.LogError(typeof(EventBusSmokeQATester), "[QA][EventBus][A] FAIL - Subscriber did not receive event.");
            }
        }

        private static void TestUnsubscribe()
        {
            var binding = new EventBinding<SmokeTestEvent>(_ => { Received = true; });
            EventBus<SmokeTestEvent>.Register(binding);
            EventBus<SmokeTestEvent>.Unregister(binding);

            Received = false;
            EventBus<SmokeTestEvent>.Raise(new SmokeTestEvent());

            if (!Received)
            {
                DebugUtility.Log(typeof(EventBusSmokeQATester), "[QA][EventBus][B] PASS - Unsubscribed listener did not receive event.", DebugUtility.Colors.Success);
            }
            else
            {
                DebugUtility.LogError(typeof(EventBusSmokeQATester), "[QA][EventBus][B] FAIL - Unsubscribed listener received event.");
            }
        }

        private static bool Received { get; set; }

        private sealed class SmokeTestEvent : IEvent { }
    }
}
