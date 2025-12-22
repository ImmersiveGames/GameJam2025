using System;
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
            int passes = 0;
            int fails = 0;

            TestSubscribeAndPublish(ref passes, ref fails);
            TestUnsubscribe(ref passes, ref fails);

            DebugUtility.Log(typeof(EventBusSmokeQATester),
                $"[QA][EventBus] QA complete. Passes={passes} Fails={fails}",
                fails == 0 ? DebugUtility.Colors.Success : DebugUtility.Colors.Warning);

            if (fails > 0)
            {
                throw new InvalidOperationException($"EventBusSmokeQATester detected {fails} failures.");
            }
        }

        private static void TestSubscribeAndPublish(ref int passes, ref int fails)
        {
            bool received = false;
            var binding = new EventBinding<SmokeTestEvent>(_ => { received = true; });

            EventBus<SmokeTestEvent>.Register(binding);
            EventBus<SmokeTestEvent>.Raise(new SmokeTestEvent());
            EventBus<SmokeTestEvent>.Unregister(binding);

            if (received)
            {
                DebugUtility.Log(typeof(EventBusSmokeQATester), "[QA][EventBus][A] PASS - Subscriber received event.", DebugUtility.Colors.Success);
                passes++;
            }
            else
            {
                DebugUtility.LogError(typeof(EventBusSmokeQATester), "[QA][EventBus][A] FAIL - Subscriber did not receive event.");
                fails++;
            }
        }

        private static void TestUnsubscribe(ref int passes, ref int fails)
        {
            bool received = false;
            var binding = new EventBinding<SmokeTestEvent>(_ => { received = true; });
            EventBus<SmokeTestEvent>.Register(binding);
            EventBus<SmokeTestEvent>.Unregister(binding);

            EventBus<SmokeTestEvent>.Raise(new SmokeTestEvent());

            if (!received)
            {
                DebugUtility.Log(typeof(EventBusSmokeQATester), "[QA][EventBus][B] PASS - Unsubscribed listener did not receive event.", DebugUtility.Colors.Success);
                passes++;
            }
            else
            {
                DebugUtility.LogError(typeof(EventBusSmokeQATester), "[QA][EventBus][B] FAIL - Unsubscribed listener received event.");
                fails++;
            }
        }

        private sealed class SmokeTestEvent : IEvent { }
    }
}
