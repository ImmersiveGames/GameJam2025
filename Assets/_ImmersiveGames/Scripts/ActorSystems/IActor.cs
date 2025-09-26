using _ImmersiveGames.Scripts.Tags;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    public interface IEntity
    {
        Transform Transform { get; }
        bool IsActive { get; set; }
    }
    public interface IActor : IEntity
    {
        string ActorName { get; }
    }
    public interface IHasSkin
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
    public interface IResettable
    {
        void Reset(bool resetSkin = false);
    }
}