// Assets/_ImmersiveGames/NewScripts/Infrastructure/Promotion/PromotionGateInstaller.cs
#nullable enable

using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Runtime.Promotion
{
    public static class PromotionGateInstaller
    {
        private static bool _registered;

        public static void EnsureRegistered(IDependencyProvider provider)
        {
            if (_registered)
            {
                return;
            }

            _registered = true;

            // Use singleton explícito para garantir log único e comportamento determinístico.
            var service = PromotionGateService.CreateFromResourcesOrDefaults();
            provider.RegisterGlobal(service);

            DebugUtility.Log(typeof(PromotionGateInstaller),
                "PromotionGate registrado (global).",
                DebugUtility.Colors.Info);
        }
    }
}
