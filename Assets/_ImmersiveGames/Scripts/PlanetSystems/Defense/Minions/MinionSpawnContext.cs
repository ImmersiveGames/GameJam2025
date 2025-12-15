using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions
{
    /// <summary>
    /// Contexto mínimo enviado pelo planeta ao spawnar um minion.
    /// O planeta só informa onde spawnar e qual role de alvo foi detectado;
    /// a lógica de comportamento (alvo e perseguição) fica no minion.
    /// </summary>
    public struct MinionSpawnContext
    {
        public PlanetsMaster Planet { get; set; }
        public DetectionType DetectionType { get; set; }
        public DefenseRole TargetRole { get; set; }
        public string TargetLabel { get; set; }
        public Vector3 SpawnPosition { get; set; }
        public Vector3 OrbitPosition { get; set; }
        public Vector3 SpawnDirection { get; set; }
    }
}
