using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Tags;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorSystems
{
    public interface IEntity
    {
        Transform Transform { get; }
        bool IsActive { get; set; }
    }
    public interface IActor : IEntity, IDefenseRoleProvider
    {
        string ActorName { get; }
        string ActorId { get; }
    }
    public interface IHasSkin
    {
        ModelRoot ModelRoot { get; }
        Transform ModelTransform { get; }
        void SetSkinActive(bool active);
    }
    public interface IResettable
    {
        void Reset();
    }
}