using _ImmersiveGames.Scripts.PlanetSystems.Defense.Minions;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense.Tests
{
    [System.Obsolete("Temporário para teste da Etapa 3")]
    public sealed class MinionSpawnTest : MonoBehaviour
    {
        [Header("Prefab com DefenseMinionController")]
        [SerializeField]
        private GameObject minionPrefab;

        [Header("Profile para teste (aplicado no spawn)")]
        [SerializeField]
        private DefenseMinionBehaviorProfileSO behaviorProfile;

        [Header("Posicionamento básico")]
        [SerializeField]
        private Vector3 planetCenter = Vector3.zero;

        [SerializeField]
        private Vector3 orbitPosition = Vector3.forward;

        private void Start()
        {
            if (minionPrefab == null)
            {
                DebugUtility.LogWarning<MinionSpawnTest>("[TestEtapa3] Nenhum prefab configurado para spawn de minion.", this);
                return;
            }

            var instance = Instantiate(minionPrefab, transform.position, Quaternion.identity);
            var controller = instance.GetComponent<DefenseMinionController>();

            if (controller == null)
            {
                DebugUtility.LogWarning<MinionSpawnTest>(
                    "[TestEtapa3] Prefab não possui DefenseMinionController. Abortando teste.",
                    this);
                return;
            }

            controller.ApplyProfile(behaviorProfile, null);

            if (behaviorProfile != null)
            {
                DebugUtility.LogVerbose<MinionSpawnTest>(
                    $"[TestEtapa3] Profile aplicado no spawn: Variant={behaviorProfile.VariantId}, " +
                    $"Entry={behaviorProfile.EntryDuration:0.00}s, Scale={behaviorProfile.InitialScaleFactor:0.00}, " +
                    $"OrbitIdle={behaviorProfile.OrbitIdleSeconds:0.00}s, ChaseSpeed={behaviorProfile.ChaseSpeed:0.00}, " +
                    $"EntryStrategy={(behaviorProfile.EntryStrategy != null ? behaviorProfile.EntryStrategy.name : "NONE")}, " +
                    $"ChaseStrategy={(behaviorProfile.ChaseStrategy != null ? behaviorProfile.ChaseStrategy.name : "NONE")}",
                    this);
            }
            else
            {
                DebugUtility.LogWarning<MinionSpawnTest>(
                    "[TestEtapa3] Nenhum profile configurado; usando valores do prefab.",
                    this);
            }

            controller.BeginEntryPhase(planetCenter, orbitPosition);
        }
    }
}

// Remova este arquivo após validar a etapa 3 no jogo.
