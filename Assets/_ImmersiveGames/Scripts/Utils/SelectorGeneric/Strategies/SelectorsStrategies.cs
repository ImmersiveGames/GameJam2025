using System.Collections.Generic;
using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.SelectorGeneric.Strategies
{
    public class FirstStrategy<T> : ISelectionStrategy<T>
    {
        public IEnumerable<T> Select(List<T> list, int listId, int specificIndex)
        {
            if (list == null || list.Count == 0) yield break;
            yield return list[0];
        }
    }

    public class RandomStrategy<T> : ISelectionStrategy<T>
    {
        public IEnumerable<T> Select(List<T> list, int listId, int specificIndex)
        {
            if (list == null || list.Count == 0) yield break;
            yield return list[Random.Range(0, list.Count)];
        }
    }

    public class SpecificIndexStrategy<T> : ISelectionStrategy<T>
    {
        public IEnumerable<T> Select(List<T> list, int listId, int specificIndex)
        {
            if (list == null || list.Count == 0) yield break;
            yield return list[Mathf.Clamp(specificIndex, 0, list.Count - 1)];
        }
    }

    public class NextStrategy<T> : ISelectionStrategy<T>
    {
        private readonly Dictionary<int, int> _indices = new();

        public IEnumerable<T> Select(List<T> list, int listId, int specificIndex)
        {
            if (list == null || list.Count == 0) yield break;

            _indices.TryAdd(listId, 0);

            int index = _indices[listId];
            _indices[listId] = (index + 1) % list.Count;
            yield return list[index];
        }

        public void Reset(int listId)
        {
            if (_indices.ContainsKey(listId))
                _indices[listId] = 0;
        }
    }

    public class AllStrategy<T> : ISelectionStrategy<T>
    {
        public IEnumerable<T> Select(List<T> list, int listId, int specificIndex)
        {
            return list;
        }
    }
}