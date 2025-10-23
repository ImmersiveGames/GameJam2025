using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;

namespace _ImmersiveGames.Scripts.DetectionsSystems.Runtime.Internal
{
    /// <summary>
    /// Coleção reutilizável que mantém índices consistentes para detecções ativas
    /// sem gerar alocações, permitindo buscas O(1) por referência.
    /// </summary>
    internal sealed class DetectionSet : IEnumerable<IDetectable>
    {
        private readonly List<IDetectable> _items = new();
        private readonly Dictionary<IDetectable, int> _indices = new();
        private readonly ReadOnlyCollection<IDetectable> _readOnlyView;

        public DetectionSet()
        {
            _readOnlyView = new ReadOnlyCollection<IDetectable>(_items);
        }

        public int Count => _items.Count;
        public ReadOnlyCollection<IDetectable> ReadOnlyItems => _readOnlyView;

        public IDetectable this[int index] => _items[index];

        public bool Contains(IDetectable detectable)
        {
            return detectable != null && _indices.ContainsKey(detectable);
        }

        public bool Add(IDetectable detectable)
        {
            if (detectable == null) throw new ArgumentNullException(nameof(detectable));
            if (_indices.ContainsKey(detectable)) return false;

            _indices[detectable] = _items.Count;
            _items.Add(detectable);
            return true;
        }

        public bool Remove(IDetectable detectable)
        {
            if (detectable == null) return false;
            if (!_indices.TryGetValue(detectable, out int index)) return false;

            RemoveAtInternal(index);
            return true;
        }

        public IDetectable RemoveAt(int index)
        {
            if ((uint)index >= (uint)_items.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return RemoveAtInternal(index);
        }

        private IDetectable RemoveAtInternal(int index)
        {
            var removedItem = _items[index];
            int lastIndex = _items.Count - 1;
            var lastItem = _items[lastIndex];

            _items.RemoveAt(lastIndex);
            _indices.Remove(removedItem);

            if (index < lastIndex)
            {
                _items[index] = lastItem;
                _indices[lastItem] = index;
            }

            return removedItem;
        }

        public void Clear()
        {
            _items.Clear();
            _indices.Clear();
        }

        public List<IDetectable>.Enumerator GetEnumerator() => _items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
