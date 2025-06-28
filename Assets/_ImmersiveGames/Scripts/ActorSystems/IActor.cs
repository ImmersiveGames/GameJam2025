using _ImmersiveGames.Scripts.Tags;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    public interface IActor
    {
        bool IsActive { get; set; }
        string Name { get; }
        Transform Transform { get; }
    }
    public interface IHasSkin : IActor
    {
        ModelRoot ModelRoot { get; }
        Transform ModelTransform { get; }
        void SetSkinActive(bool active);
    }
    public interface IHasCanvas
    {
        CanvasRoot CanvasRoot { get;  }
        Transform CanvasTransform { get; }
    }
    
    public interface IHasFx
    {
        FxRoot FxRoot { get;  }
        Transform FxTransform { get; }
        void SetFxActive(bool active);
    }
}