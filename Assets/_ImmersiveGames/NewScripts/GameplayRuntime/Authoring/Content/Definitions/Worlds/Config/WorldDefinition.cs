using System;
using System.Collections.Generic;
using ImmersiveGames.GameJam2025.Game.Gameplay.Spawn;
using UnityEngine;
namespace ImmersiveGames.GameJam2025.Game.Content.Definitions.Worlds.Config
{
    /// <summary>
    /// Input de authoring/bootstrap de conteudo para a ordem de spawn do mundo.
    /// Utilizado pelo bootstrap de cena como configuracao operacional, nao como ownership semantico do gameplay.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WorldDefinition",
        menuName = "ImmersiveGames/NewScripts/Game/Content/Definitions/Worlds/WorldDefinition",
        order = 30)]
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
            [Tooltip("Notas apenas para depuracao/inspector. Nao utilizado em runtime.")]
            public string notes;

            public WorldSpawnServiceKind Kind => kind;

            public bool Enabled => enabled;

            public GameObject Prefab => prefab;

            public string Notes => notes;
        }
    }
}

