using System;
namespace _ImmersiveGames.NewScripts.Foundation.Core.Events
{
    public interface IEventBinding<T>
    {
        Action<T> OnEvent { get; set; }
        Action OnEventNoArgs { get; set; }
    }
}

