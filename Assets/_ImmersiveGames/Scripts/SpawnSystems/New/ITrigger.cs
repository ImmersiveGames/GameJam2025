// ITrigger.cs (mesmo da proposta anterior, reutilizável)

using System;
namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public interface ITrigger
    {
        event Action OnTriggered;
    }
}