using System;
namespace ImmersiveGames.GameJam2025.Core.Events
{
    public interface IEventBinding<T>
    {
        Action<T> OnEvent { get; set; }
        Action OnEventNoArgs { get; set; }
    }
}

