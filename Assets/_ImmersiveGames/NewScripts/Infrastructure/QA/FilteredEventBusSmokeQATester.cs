using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Infrastructure.QA
{
    /// <summary>
    /// Smoke test para o FilteredEventBus no NewScripts.
    /// </summary>
    public sealed class FilteredEventBusSmokeQaTester : MonoBehaviour
    {
        private const string ScopeA = "SCOPE_A";

        [ContextMenu("QA/EventBus/Run Filtered")]
        public void Run()
        {
            TestFilteredDelivery();
            TestFilteredIsolation();
            DebugUtility.Log(typeof(FilteredEventBusSmokeQaTester), "[QA][EventBus][Filtered] QA complete.");
        }

        private static void TestFilteredDelivery()
        {
            bool received = false;
            var binding = new EventBinding<FilteredSmokeEvent>(_ => received = true);
            FilteredEventBus<string, FilteredSmokeEvent>.Register(ScopeA, binding);

            FilteredEventBus<string, FilteredSmokeEvent>.Raise(ScopeA, new FilteredSmokeEvent());

            if (received)
            {
                DebugUtility.Log(typeof(FilteredEventBusSmokeQaTester),
                    "[QA][EventBus][Filtered][A] PASS - Scoped subscriber received event.",
                    DebugUtility.Colors.Success);
            }
            else
            {
                DebugUtility.LogError(typeof(FilteredEventBusSmokeQaTester),
                    "[QA][EventBus][Filtered][A] FAIL - Scoped subscriber did not receive event.");
            }
        }

        private static void TestFilteredIsolation()
        {
            bool received = false;
            var binding = new EventBinding<FilteredSmokeEvent>(_ => received = true);
            FilteredEventBus<string, FilteredSmokeEvent>.Register("OTHER_SCOPE", binding);

            FilteredEventBus<string, FilteredSmokeEvent>.Raise(ScopeA, new FilteredSmokeEvent());

            if (!received)
            {
                DebugUtility.Log(typeof(FilteredEventBusSmokeQaTester),
                    "[QA][EventBus][Filtered][B] PASS - Other scope did not receive event.",
                    DebugUtility.Colors.Success);
            }
            else
            {
                DebugUtility.LogError(typeof(FilteredEventBusSmokeQaTester),
                    "[QA][EventBus][Filtered][B] FAIL - Other scope received event.");
            }

            FilteredEventBus<string, FilteredSmokeEvent>.Clear("OTHER_SCOPE");
        }

        private sealed class FilteredSmokeEvent : IEvent { }
    }
}
