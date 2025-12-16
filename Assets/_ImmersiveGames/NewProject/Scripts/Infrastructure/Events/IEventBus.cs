using System;

namespace _ImmersiveGames.NewProject.Infrastructure.Events
{
    /// <summary>
    /// Contrato para publicar/assinar eventos tipados. Uso apenas para notificação.
    /// </summary>
    public interface IEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler, string scope = null);
        void Publish<TEvent>(TEvent payload, string scope = null);
    }
}
