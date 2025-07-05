using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.SkinSystems;
using _ImmersiveGames.Scripts.Tags;
using UnityEngine;
namespace _ImmersiveGames.Scripts.Utils.SelectorGeneric
{
    public enum SelectionStrategyType
    {
        First,
        Random,
        SpecificIndex,
        Next,
        All
    }
    public enum ObjectRootType
    {
        ModelRoot,
        CanvasRoot,
        FxRoot
    }
    public interface ISelectionStrategy<T>
    {
        IEnumerable<T> Select(List<T> list, int listId, int specificIndex);
    }
    public interface IGameObjectSelector
    {
        ObjectRootType ObjectRootType { get; }
        string SelectorName { get; }
        Vector3 PositionOffset { get; }
        Quaternion RotationOffset { get; }
        Vector3 ScaleOffset { get; }

        IEnumerable<(GameObject prefab, bool active, string tag)> Select(int listId);
    }
}