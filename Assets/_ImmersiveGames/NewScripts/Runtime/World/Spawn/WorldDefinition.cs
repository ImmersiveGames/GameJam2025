using System;
using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.Runtime.World.Spawn
{
    /// <summary>
    /// Define a ordem de spawn do mundo, utilizada pelo bootstrap de cena para registrar serviços.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WorldDefinition",
        menuName = "ImmersiveGames/World Definition",
        order = 0)]
    public sealed class WorldDefinition : ScriptableObject
    {
        [SerializeField]
        public List<SpawnEntry> spawnEntries = new();

        public IReadOnlyList<SpawnEntry> Entries => spawnEntries;

        [Serializable]
        public sealed class SpawnEntry
        {
            [SerializeField]
            private WorldSpawnServiceKind kind = WorldSpawnServiceKind.DummyActor;

            [SerializeField]
            private bool enabled = true;

            [SerializeField]
            private GameObject prefab;

            [SerializeField]
            [Tooltip("Notas apenas para depuração/inspector. Não utilizado em runtime.")]
            public string notes;

            public WorldSpawnServiceKind Kind => kind;

            public bool Enabled => enabled;

            public GameObject Prefab => prefab;

            public string Notes => notes;
        }
    }
}

