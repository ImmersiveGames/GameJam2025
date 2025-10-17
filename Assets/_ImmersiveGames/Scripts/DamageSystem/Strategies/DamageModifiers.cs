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
    public class DamageModifiers
    {
        [Serializable]
        public struct Entry
        {
            public DamageType type;
            public float multiplier;
        }

        [SerializeField]
        private List<Entry> entries = new();

        public float GetModifier(DamageType type)
        {
            foreach (var entry in entries)
            {
                if (entry.type == type)
                    return entry.multiplier;
            }
            return 1f; // padrão
        }
    }
}