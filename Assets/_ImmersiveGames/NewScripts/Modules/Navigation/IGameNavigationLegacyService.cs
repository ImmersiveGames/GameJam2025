using System.ComponentModel;
using System.Threading.Tasks;

namespace _ImmersiveGames.NewScripts.Modules.Navigation
{
    /// <summary>
    /// Compat temporaria para entradas legacy/string-first de Navigation.
    /// Nao faz parte da superficie canonica principal.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IGameNavigationLegacyService
    {
        [System.Obsolete("Compat temporaria apenas. Prefira NavigateAsync(GameNavigationIntentKind, reason) para core intents.")]
        Task NavigateAsync(string routeId, string reason = null);

        [System.Obsolete("Compat temporaria apenas. Use GoToMenuAsync(reason).")]
        Task RequestMenuAsync(string reason = null);

        [System.Obsolete("Compat temporaria apenas. Use RestartAsync(reason) ou ILevelFlowRuntimeService.StartGameplayDefaultAsync(reason, ct).")]
        Task RequestGameplayAsync(string reason = null);
    }
}
