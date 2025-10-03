using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    public interface ISkinConfig
    {
        string ConfigName { get; }
        ModelType ModelType { get; }
        List<GameObject> GetSelectedPrefabs();
    }
    public interface ISkinCollection
    {
        string CollectionName { get; }
        ISkinConfig GetConfig(ModelType modelType);
    }
    public enum ModelType
    {
        ModelRoot,
        CanvasRoot,
        FxRoot,
        SoundRoot
    }
    public enum InstantiationMode
    {
        All,
        First,
        Random,
        Specific
    }
}