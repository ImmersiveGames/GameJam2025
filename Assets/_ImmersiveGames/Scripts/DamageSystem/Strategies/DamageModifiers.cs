using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ResourceSystems.Configs;
using UnityEngine;

namespace _ImmersiveGames.Scripts.DamageSystem.Strategies
{
    /// <summary>
    /// Tabela de multiplicadores por tipo de dano.
    /// </summary>
    [Serializable]
    public class DamageModifiers : ISerializationCallbackReceiver
    {
        [Serializable]
        public struct Entry
        {
            public DamageType type;
            public float multiplier;
        }

        [SerializeField]
        private List<Entry> entries = new();

        [NonSerialized]
        private readonly Dictionary<DamageType, float> _cache = new();

        [NonSerialized]
        private bool _isCacheDirty = true;

        public IReadOnlyList<Entry> Entries
        {
            get
            {
                entries ??= new List<Entry>();
                return entries;
            }
        }

        public float GetModifier(DamageType type)
        {
            EnsureCache();
            return _cache.TryGetValue(type, out var multiplier) ? multiplier : 1f;
        }

        public bool TryGetModifier(DamageType type, out float multiplier)
        {
            EnsureCache();

            if (_cache.TryGetValue(type, out multiplier))
                return true;

            multiplier = 1f;
            return false;
        }

        public void SetModifier(DamageType type, float multiplier)
        {
            entries ??= new List<Entry>();
            multiplier = Mathf.Max(0f, multiplier);

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].type != type)
                    continue;

                entries[i] = new Entry { type = type, multiplier = multiplier };
                MarkCacheDirty();
                return;
            }

            entries.Add(new Entry { type = type, multiplier = multiplier });
            MarkCacheDirty();
        }

        public bool RemoveModifier(DamageType type)
        {
            if (entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].type != type)
                    continue;

                entries.RemoveAt(i);
                MarkCacheDirty();
                return true;
            }

            return false;
        }

        private void EnsureCache()
        {
            if (!_isCacheDirty)
                return;

            entries ??= new List<Entry>();
            _cache.Clear();

            foreach (var entry in entries)
            {
                var safeMultiplier = Mathf.Max(0f, entry.multiplier);
                _cache[entry.type] = safeMultiplier;
            }

            _isCacheDirty = false;
        }

        private void MarkCacheDirty()
        {
            _isCacheDirty = true;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            MarkCacheDirty();
        }
    }
}
