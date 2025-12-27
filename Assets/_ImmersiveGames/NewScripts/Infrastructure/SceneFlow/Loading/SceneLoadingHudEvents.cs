using _ImmersiveGames.NewScripts.Infrastructure.Events;

namespace _ImmersiveGames.NewScripts.Infrastructure.SceneFlow.Loading
{
    /// <summary>
    /// Disparado quando o HUD de loading é registrado no DI global.
    /// </summary>
    public readonly struct SceneLoadingHudRegisteredEvent : IEvent
    {
    }

    /// <summary>
    /// Disparado quando o HUD de loading é destruído/desregistrado.
    /// </summary>
    public readonly struct SceneLoadingHudUnregisteredEvent : IEvent
    {
    }
}
