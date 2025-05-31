using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class SkinBreaker : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] planetsParts;

        public void DestroyPlanetPiece(float actualHealth, float maxHealth)
        {
            int total = planetsParts.Length;
            int parts = Mathf.CeilToInt((actualHealth / maxHealth) * total);

            for (int i = 0; i < total; i++)
            {
                planetsParts[i].SetActive(i < parts);
            }
        }
    }
}
