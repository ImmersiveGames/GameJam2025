using UnityEngine;

namespace _ImmersiveGames.Scripts.Utils.PoolSystems
{
    /// <summary>
    /// Dados de objetos defensivos usados pelas defesas planetárias.
    /// Mantém o contrato de PoolableObjectData para configurar prefab e tempo de vida.
    /// </summary>
    [CreateAssetMenu(fileName = "DefensesObjectData", menuName = "ImmersiveGames/Defenses Object Data")]
    public class DefensesObjectData : PoolableObjectData
    {
    }
}
