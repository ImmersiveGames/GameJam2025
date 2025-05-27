using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems
{
    public class PartBreaker : MonoBehaviour
    {
        [SerializeField] GameObject[] partesDoPlaneta;

        public void DestroyPlanetPiece(float vidaAtual, float vidaMaxima)
        {
            int totalPartes = partesDoPlaneta.Length;
            int partesAtivas = Mathf.CeilToInt((vidaAtual / vidaMaxima) * totalPartes);

            for (int i = 0; i < totalPartes; i++)
            {
                partesDoPlaneta[i].SetActive(i < partesAtivas);
            }
        }
    }
}
